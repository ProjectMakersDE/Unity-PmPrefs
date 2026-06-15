using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

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

      /// <summary>
      /// True when the most recent read could not enumerate the platform store and fell back to
      /// the PmPrefs tracked-key list (which cannot see regular PlayerPrefs). The editor window
      /// surfaces this so the user knows the list may be incomplete.
      /// </summary>
      public bool UsedFallback { get; private set; }

#if UNITY_EDITOR_WIN
      // Validates Unity's registry value-name suffix: "<keyName>_h<hash>".
      private static readonly Regex WindowsSuffixPattern = new Regex(@"^(?<name>.+)_h\d+$", RegexOptions.Compiled);
#endif

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

            // Skip the internal key list.
            if (keyName == PmPrefs.KeyListKey) continue;

            // Skip the legacy registry slot only while it still holds the un-migrated key list
            // (plaintext JSON starting with '{'). A real user key named "KeyList" stores encrypted
            // data (never starting with '{'), so it is still shown.
            if (keyName == PmPrefs.LegacyKeyListKey)
            {
               string raw = PlayerPrefs.GetString(keyName);
               if (raw != null && raw.TrimStart().StartsWith("{")) continue;
            }

            string strValue = ConvertValueToString(value);

            if (keyName.StartsWith(PmPrefs.Prefix))
            {
               // This is a PmPrefs key
               string cleanKey = keyName.Substring(PmPrefs.Prefix.Length);

               if (_editorWindow.ShowDecrypted)
               {
                  strValue = PmPrefs.Decrypt(PlayerPrefs.GetString(keyName));
               }
               else
               {
                  strValue = PlayerPrefs.GetString(keyName);
               }

               _editorWindow.PmPrefsList.Add(new PmPrefsListItem(cleanKey, strValue));
            }
            else
            {
               // Regular PlayerPrefs key - read using Unity PlayerPrefs API for correct values.
               // Platform readers (plist/registry) may return raw/encoded data that differs
               // from the actual value. The PlayerPrefs API properly decodes the stored data.
               string displayValue = strValue;
               if (PlayerPrefs.HasKey(keyName))
               {
                  string apiValue = PlayerPrefs.GetString(keyName, "");
                  if (!string.IsNullOrEmpty(apiValue))
                  {
                     displayValue = apiValue;
                  }
               }
               _editorWindow.PlayerPrefsList.Add(new PmPrefsListItem(keyName, displayValue));
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

         UsedFallback = false;

#if UNITY_EDITOR_WIN
         _cachedKeys = GetKeysFromWindowsRegistry();
#elif UNITY_EDITOR_OSX
         _cachedKeys = GetKeysFromMacOSPlist();
#elif UNITY_EDITOR_LINUX
         _cachedKeys = GetKeysFromLinuxPrefs();
#else
         UsedFallback = true;
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

      private Dictionary<string, object> UseFallback(string reason)
      {
         if (!string.IsNullOrEmpty(reason))
         {
            Debug.LogWarning($"[PmPrefs] {reason} Falling back to the tracked PmPrefs key list; regular PlayerPrefs may not be shown.");
         }
         UsedFallback = true;
         return GetKeysFromTrackedList();
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
                  // Unity appends a suffix like "_h12345" to registry value names. Strip it only
                  // when it matches that exact pattern; otherwise keep the raw name.
                  var match = WindowsSuffixPattern.Match(valueName);
                  string cleanName = match.Success ? match.Groups["name"].Value : valueName;
                  if (string.IsNullOrEmpty(cleanName)) continue;

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
            return UseFallback($"Failed to read Windows Registry: {ex.Message}.");
         }

         return result;
      }
#endif

#if UNITY_EDITOR_OSX
      private Dictionary<string, object> GetKeysFromMacOSPlist()
      {
         try
         {
            // Unity stores prefs in ~/Library/Preferences/unity.[companyname].[productname].plist
            // using the company/product names verbatim.
            string plistPath = Path.Combine(
               Environment.GetFolderPath(Environment.SpecialFolder.Personal),
               "Library/Preferences",
               $"unity.{PlayerSettings.companyName}.{PlayerSettings.productName}.plist"
            );

            if (!File.Exists(plistPath))
            {
               return UseFallback($"macOS plist not found at: {plistPath}.");
            }

            string xml = ReadPlistAsXml(plistPath);
            if (string.IsNullOrEmpty(xml))
            {
               return UseFallback("Could not convert macOS plist to XML.");
            }

            var parsed = ParsePlistXml(xml);
            if (parsed == null)
            {
               return UseFallback("Could not parse macOS plist XML.");
            }

            return parsed;
         }
         catch (Exception ex)
         {
            return UseFallback($"Failed to read macOS plist: {ex.Message}.");
         }
      }

      /// <summary>
      /// Reads a plist file as XML. Converts binary plists (the modern macOS default) via plutil.
      /// </summary>
      private string ReadPlistAsXml(string plistPath)
      {
         // Peek the magic bytes to decide whether conversion is needed.
         bool isBinary = false;
         try
         {
            using (var fs = File.OpenRead(plistPath))
            {
               byte[] magic = new byte[6];
               int read = fs.Read(magic, 0, magic.Length);
               isBinary = read == 6 && Encoding.ASCII.GetString(magic) == "bplist";
            }
         }
         catch { /* fall through to direct read */ }

         if (!isBinary)
         {
            return File.ReadAllText(plistPath);
         }

         // Binary plist: convert to XML on stdout via plutil (available on all macOS).
         try
         {
            var psi = new ProcessStartInfo
            {
               FileName = "/usr/bin/plutil",
               Arguments = $"-convert xml1 -o - \"{plistPath}\"",
               UseShellExecute = false,
               RedirectStandardOutput = true,
               RedirectStandardError = true,
               CreateNoWindow = true
            };

            using (var process = Process.Start(psi))
            {
               if (process == null) return null;
               string output = process.StandardOutput.ReadToEnd();
               process.WaitForExit(5000);
               return process.ExitCode == 0 ? output : null;
            }
         }
         catch (Exception ex)
         {
            Debug.LogWarning($"[PmPrefs] plutil conversion failed: {ex.Message}");
            return null;
         }
      }
#endif

      /// <summary>
      /// Parses XML while ignoring any DOCTYPE/DTD (Apple plists declare an external DTD that must
      /// not be fetched, and DtdProcessing defaults to Prohibit on modern runtimes).
      /// </summary>
      private static XDocument LoadXmlIgnoringDtd(string xml)
      {
         var settings = new XmlReaderSettings
         {
            DtdProcessing = DtdProcessing.Ignore,
            XmlResolver = null
         };
         using (var stringReader = new StringReader(xml))
         using (var reader = XmlReader.Create(stringReader, settings))
         {
            return XDocument.Load(reader);
         }
      }

      /// <summary>
      /// Parses an Apple plist XML document's top-level &lt;dict&gt; into a key/value map.
      /// Handles string/integer/real/true/false/date/data value elements and decodes XML entities.
      /// </summary>
      private static Dictionary<string, object> ParsePlistXml(string xml)
      {
         try
         {
            var doc = LoadXmlIgnoringDtd(xml);
            var dict = doc.Root?.Element("dict");
            var result = new Dictionary<string, object>();
            if (dict == null) return result;

            XElement pendingKey = null;
            foreach (var el in dict.Elements())
            {
               if (el.Name.LocalName == "key")
               {
                  pendingKey = el;
                  continue;
               }

               if (pendingKey == null) continue;

               string key = pendingKey.Value; // entity-decoded by LINQ to XML
               string value;
               switch (el.Name.LocalName)
               {
                  case "true": value = "true"; break;
                  case "false": value = "false"; break;
                  default: value = el.Value; break;
               }

               if (!result.ContainsKey(key))
               {
                  result[key] = value;
               }
               pendingKey = null;
            }

            return result;
         }
         catch (Exception ex)
         {
            Debug.LogWarning($"[PmPrefs] Failed to parse plist XML: {ex.Message}");
            return null;
         }
      }

#if UNITY_EDITOR_LINUX
      private Dictionary<string, object> GetKeysFromLinuxPrefs()
      {
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
               return UseFallback($"Linux prefs file not found at: {prefsPath}.");
            }

            string content = File.ReadAllText(prefsPath);
            var parsed = ParseLinuxPrefs(content);
            if (parsed == null)
            {
               return UseFallback("Could not parse Linux prefs XML.");
            }

            return parsed;
         }
         catch (Exception ex)
         {
            return UseFallback($"Failed to read Linux prefs: {ex.Message}.");
         }
      }
#endif

      /// <summary>
      /// Parses Unity's Linux prefs XML: a &lt;preferences&gt; root with
      /// &lt;pref name="..." type="..."&gt;value&lt;/pref&gt; children. Uses XDocument so attribute
      /// order and XML entities are handled correctly.
      /// </summary>
      private static Dictionary<string, object> ParseLinuxPrefs(string content)
      {
         try
         {
            var doc = LoadXmlIgnoringDtd(content);
            var result = new Dictionary<string, object>();

            foreach (var pref in doc.Descendants("pref"))
            {
               var nameAttr = pref.Attribute("name");
               if (nameAttr == null) continue;
               string name = nameAttr.Value;
               if (!result.ContainsKey(name))
               {
                  result[name] = pref.Value;
               }
            }

            return result;
         }
         catch (Exception ex)
         {
            Debug.LogWarning($"[PmPrefs] Failed to parse Linux prefs: {ex.Message}");
            return null;
         }
      }

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
