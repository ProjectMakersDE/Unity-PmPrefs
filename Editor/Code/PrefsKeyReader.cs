using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR_WIN
using Microsoft.Win32;
#endif

namespace PM.Plugins
{
   /// <summary>
   /// Cross-platform PlayerPrefs key reader.
   /// Supports Windows (Registry), macOS (plist), and Linux (prefs file).
   /// </summary>
   public class PrefsKeyReader
   {
      private readonly PmPrefsEditorWindow _editorWindow;
      private Dictionary<string, object> _cachedKeys;
      private List<KeyValuePair<string, object>> _cachedSortedKeys;
      private DateTime _lastCacheTime;
      private static readonly TimeSpan CacheTimeout = TimeSpan.FromSeconds(2);

      // Compiled Regex patterns for performance optimization
      private static readonly Regex PlistKeyValuePattern = new Regex(@"""([^""]+)""\s*=>\s*(.+)", RegexOptions.Compiled);
      private static readonly Regex AlphanumericOnlyPattern = new Regex(@"[^a-zA-Z0-9]", RegexOptions.Compiled);
      private static readonly Regex LinuxPrefPattern = new Regex(@"<pref\s+name=""([^""]+)""[^>]*>([^<]*)</pref>", RegexOptions.Compiled);
      private static readonly Regex LinuxKeyPattern = new Regex(@"<key\s+name=""([^""]+)""[^>]*value=""([^""]*)""", RegexOptions.Compiled);

      public PrefsKeyReader(PmPrefsEditorWindow editorWindow)
      {
         _editorWindow = editorWindow;
      }

      /// <summary>
      /// Retrieves all PlayerPrefs keys and populates the editor window lists.
      /// Works on Windows, macOS, and Linux.
      /// </summary>
      public void GetKeys()
      {
         GetAllPlayerPrefsKeys();

         foreach (var kvp in _cachedSortedKeys)
         {
            string keyName = kvp.Key;
            object value = kvp.Value;

            // Skip the internal key list
            if (keyName == "PmPrefs__KeyList") continue;

            string strValue = ConvertValueToString(value);

            if (keyName.StartsWith(PmPrefs.Prefix))
            {
               // This is a PmPrefs key
               string cleanKey = keyName.Substring(PmPrefs.Prefix.Length);

               if (_editorWindow.ShowEncrypted)
               {
                  // Show decrypted value
                  string encrypted = PlayerPrefs.GetString(keyName);
                  strValue = PmPrefs.Decrypt(encrypted);
               }
               else
               {
                  // Show encrypted value
                  strValue = PlayerPrefs.GetString(keyName);
               }

               _editorWindow.PmPrefsList.Add(new PmPrefsListItem(cleanKey, strValue));
            }
            else
            {
               // Regular PlayerPrefs key
               _editorWindow.PlayerPrefsList.Add(new PmPrefsListItem(keyName, strValue));
            }
         }
      }

      private string ConvertValueToString(object value)
      {
         if (value == null) return string.Empty;

         if (value is byte[] bytes)
         {
            // Unity stores strings as byte arrays in some platforms
            return Encoding.UTF8.GetString(bytes).TrimEnd('\0');
         }

         return value.ToString();
      }

      /// <summary>
      /// Gets all PlayerPrefs keys from the platform-specific storage.
      /// </summary>
      private Dictionary<string, object> GetAllPlayerPrefsKeys()
      {
         // Check cache
         if (_cachedKeys != null && DateTime.Now - _lastCacheTime < CacheTimeout)
         {
            return _cachedKeys;
         }

#if UNITY_EDITOR_WIN
         _cachedKeys = GetKeysFromWindowsRegistry();
#elif UNITY_EDITOR_OSX
         _cachedKeys = GetKeysFromMacOSPlist();
#elif UNITY_EDITOR_LINUX
         _cachedKeys = GetKeysFromLinuxPrefs();
#else
         _cachedKeys = GetKeysFromTrackedList();
#endif

         // Create sorted cache to avoid repeated OrderBy operations
         _cachedSortedKeys = _cachedKeys.OrderBy(k => k.Key).ToList();

         _lastCacheTime = DateTime.Now;
         return _cachedKeys;
      }

      /// <summary>
      /// Invalidates the cached keys, forcing a fresh read on next access.
      /// </summary>
      public void InvalidateCache()
      {
         _cachedKeys = null;
         _cachedSortedKeys = null;
      }

#if UNITY_EDITOR_WIN
      private Dictionary<string, object> GetKeysFromWindowsRegistry()
      {
         var result = new Dictionary<string, object>();

         try
         {
            string registryPath = $@"Software\Unity\UnityEditor\{PlayerSettings.companyName}\{PlayerSettings.productName}";
            using (var key = Registry.CurrentUser.OpenSubKey(registryPath))
            {
               if (key == null) return result;

               foreach (var valueName in key.GetValueNames())
               {
                  // Unity adds a suffix like "_h12345" to registry keys
                  int lastUnderscore = valueName.LastIndexOf('_');
                  if (lastUnderscore <= 0) continue;

                  string cleanName = valueName.Substring(0, lastUnderscore);
                  object value = key.GetValue(valueName);

                  // Avoid duplicates (same key with different hash suffixes)
                  if (!result.ContainsKey(cleanName))
                  {
                     result[cleanName] = value;
                  }
               }
            }
         }
         catch (Exception ex)
         {
            Debug.LogWarning($"[PmPrefs] Failed to read Windows Registry: {ex.Message}");
            return GetKeysFromTrackedList();
         }

         return result;
      }
#endif

#if UNITY_EDITOR_OSX
      private Dictionary<string, object> GetKeysFromMacOSPlist()
      {
         var result = new Dictionary<string, object>();

         try
         {
            // Unity stores prefs in ~/Library/Preferences/unity.[companyname].[productname].plist
            string companyName = SanitizeForPlist(PlayerSettings.companyName);
            string productName = SanitizeForPlist(PlayerSettings.productName);
            string plistPath = Path.Combine(
               Environment.GetFolderPath(Environment.SpecialFolder.Personal),
               $"Library/Preferences/unity.{companyName}.{productName}.plist"
            );

            if (!File.Exists(plistPath))
            {
               Debug.Log($"[PmPrefs] Plist not found at: {plistPath}");
               return GetKeysFromTrackedList();
            }

            // Read plist XML directly (no process spawn)
            string content = File.ReadAllText(plistPath);

            // Detect binary plist format (starts with "bplist")
            if (content.Length > 6 && content.Substring(0, 6) == "bplist")
            {
               Debug.LogWarning("[PmPrefs] Binary plist format detected. XML format required for direct parsing. Using fallback method.");
               return GetKeysFromTrackedList();
            }

            // Parse plist XML format: <key>name</key> followed by <string>value</string> (or <integer>, <real>, etc.)
            // Match key-value pairs in the plist dict structure
            var keyPattern = @"<key>([^<]+)</key>\s*<(string|integer|real)>([^<]*)</(string|integer|real)>";
            var keyMatches = Regex.Matches(content, keyPattern);

            foreach (Match match in keyMatches)
            {
               string key = match.Groups[1].Value;
               string value = match.Groups[3].Value;
               result[key] = value;
            }

            // Also handle boolean values (<true/> and <false/>)
            var boolPattern = @"<key>([^<]+)</key>\s*<(true|false)\s*/>";
            var boolMatches = Regex.Matches(content, boolPattern);

            foreach (Match match in boolMatches)
            {
               string key = match.Groups[1].Value;
               string value = match.Groups[2].Value;
               if (!result.ContainsKey(key))
               {
<<<<<<< HEAD
                  var match = PlistKeyValuePattern.Match(line);
                  if (match.Success)
                  {
                     string key = match.Groups[1].Value;
                     string value = match.Groups[2].Value.Trim().Trim('"');
                     result[key] = value;
                  }
=======
                  result[key] = value;
>>>>>>> auto-claude/014-avoid-process-spawn-for-macos-plist-reading-use-na
               }
            }
         }
         catch (Exception ex)
         {
            Debug.LogWarning($"[PmPrefs] Failed to read macOS plist: {ex.Message}");
            return GetKeysFromTrackedList();
         }

         return result;
      }

      private string SanitizeForPlist(string input)
      {
         if (string.IsNullOrEmpty(input)) return "DefaultCompany";
         // Unity replaces spaces and special chars
         return AlphanumericOnlyPattern.Replace(input, "").ToLower();
      }
#endif

#if UNITY_EDITOR_LINUX
      private Dictionary<string, object> GetKeysFromLinuxPrefs()
      {
         var result = new Dictionary<string, object>();

         try
         {
            // Unity stores prefs in ~/.config/unity3d/[CompanyName]/[ProductName]/prefs
            string prefsPath = Path.Combine(
               Environment.GetFolderPath(Environment.SpecialFolder.Personal),
               ".config/unity3d",
               PlayerSettings.companyName,
               PlayerSettings.productName,
               "prefs"
            );

            if (!File.Exists(prefsPath))
            {
               Debug.Log($"[PmPrefs] Linux prefs file not found at: {prefsPath}");
               return GetKeysFromTrackedList();
            }

            // Linux prefs is an XML file
            string content = File.ReadAllText(prefsPath);
            var keyMatches = LinuxPrefPattern.Matches(content);

            foreach (Match match in keyMatches)
            {
               string key = match.Groups[1].Value;
               string value = match.Groups[2].Value;
               result[key] = value;
            }

            // Also check for unity.* keys format
            var unityKeyMatches = LinuxKeyPattern.Matches(content);
            foreach (Match match in unityKeyMatches)
            {
               string key = match.Groups[1].Value;
               string value = match.Groups[2].Value;
               if (!result.ContainsKey(key))
               {
                  result[key] = value;
               }
            }
         }
         catch (Exception ex)
         {
            Debug.LogWarning($"[PmPrefs] Failed to read Linux prefs: {ex.Message}");
            return GetKeysFromTrackedList();
         }

         return result;
      }
#endif

      /// <summary>
      /// Fallback method using PmPrefs tracked key list.
      /// This works on all platforms but only shows keys that PmPrefs knows about.
      /// </summary>
      private Dictionary<string, object> GetKeysFromTrackedList()
      {
         var result = new Dictionary<string, object>();

         // Get PmPrefs keys from tracked list
         var pmPrefsKeys = PmPrefs.GetAllKeys();
         foreach (var key in pmPrefsKeys)
         {
            string fullKey = PmPrefs.Prefix + key;
            if (PlayerPrefs.HasKey(fullKey))
            {
               result[fullKey] = PlayerPrefs.GetString(fullKey);
            }
         }

         return result;
      }
   }
}
