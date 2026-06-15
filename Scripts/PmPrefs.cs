using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace PM.Plugins
{
   /// <summary>
   /// PmPrefs provides encrypted PlayerPrefs storage for Unity.
   /// Save and load any serializable object with automatic AES encryption.
   /// </summary>
   /// <remarks>
   /// <para><b>Performance Optimization:</b></para>
   /// <para>
   /// PmPrefs uses an internal key list to track all stored keys. When you call Save() or DeleteKey(),
   /// the key list is marked as dirty but NOT immediately written to disk. This batching behavior
   /// improves performance during bulk operations by avoiding repeated disk writes.
   /// </para>
   /// <para>
   /// Call FlushKeyList() after batch operations to ensure the key list is persisted, or call
   /// SaveAll() which flushes the key list and then persists PlayerPrefs. Note that Unity's own
   /// <see cref="PlayerPrefs.Save"/> does NOT flush the PmPrefs key list (PmPrefs cannot hook into
   /// it) - always use PmPrefs.SaveAll() / FlushKeyList() instead.
   /// </para>
   /// </remarks>
   /// <example>
   /// <code>
   /// // Basic usage
   /// PmPrefs.Save("playerName", "John");
   /// PmPrefs.Save("settings", mySettingsObject);
   ///
   /// // Load data
   /// string name = PmPrefs.Load&lt;string&gt;("playerName", "DefaultName");
   /// MySettings settings = PmPrefs.Load&lt;MySettings&gt;("settings");
   ///
   /// // Batch operations with manual flush
   /// for (int i = 0; i &lt; 100; i++)
   /// {
   ///     PmPrefs.Save($"item_{i}", itemData[i]);
   /// }
   /// PmPrefs.SaveAll(); // Flush key list + persist all PlayerPrefs data
   /// </code>
   /// </example>
   public static class PmPrefs
   {
      /// <summary>
      /// Wrapper class for JSON serialization of the key list.
      /// Uses List&lt;string&gt; for serialization compatibility with Unity's JsonUtility.
      /// A separate HashSet is maintained at runtime for O(1) lookups.
      /// </summary>
      [Serializable]
      private class StringListWrapper
      {
         public List<string> items = new List<string>();
      }

      private static StringListWrapper _listWrapper;
      private static HashSet<string> _keySet;
      private static bool _isKeyListDirty;

      private const string SaltKey = "F1m5eJVO9ASPxGW7B3KP9t8iNd5Edpb48LAGNlWcLHeNkeH6PNYf3BCztZB7D3ch";

      // IV used by the legacy (v1) on-disk format. Kept only to decrypt data written by
      // older PmPrefs versions; new data uses a random per-value IV (see Encrypt).
      private const string LegacyIv = "NiB3KP9VksfNf3Bi";

      // Marker prefix that identifies the current (v2) encrypted format: random IV prepended
      // to the ciphertext. ':' is not a Base64 character, so a legacy Base64 string can never
      // be mistaken for v2.
      private const string V2Prefix = "PMv2:";

      // PBKDF2 iteration count for the v2 key. The legacy key uses the framework default (1000).
      private const int V2Iterations = 100000;

      /// <summary>
      /// The built-in fallback encryption key, used when no <see cref="PmPrefsKeyAsset"/> with a
      /// non-empty key is present. Prefer setting a project-specific key via the editor window
      /// (Configuration &gt; Secure Key) instead of relying on this default.
      /// </summary>
      public const string DefaultSecureKey = "LoKo1Nibu75XXzu";

      private static string _activeKey;
      private static bool _activeKeyResolved;

      /// <summary>
      /// The encryption key currently in effect. Resolved from a <see cref="PmPrefsKeyAsset"/> in
      /// any Resources folder; falls back to <see cref="DefaultSecureKey"/> when none is configured.
      /// Call <see cref="RefreshSecureKey"/> after changing the configured key.
      /// </summary>
      public static string SecureKey
      {
         get
         {
            if (!_activeKeyResolved)
            {
               _activeKey = ResolveSecureKey();
               _activeKeyResolved = true;
            }
            return _activeKey;
         }
      }

      private static string ResolveSecureKey()
      {
         try
         {
            var configs = Resources.LoadAll<PmPrefsKeyAsset>("");
            if (configs != null)
            {
               foreach (var config in configs)
               {
                  if (config != null && !string.IsNullOrEmpty(config.secureKey))
                  {
                     return config.secureKey;
                  }
               }
            }
         }
         catch (Exception ex)
         {
            Debug.LogWarning($"[PmPrefs] Failed to resolve secure key from config asset: {ex.Message}");
         }

         return DefaultSecureKey;
      }

      /// <summary>
      /// Forces the active <see cref="SecureKey"/> and derived key material to be re-resolved on
      /// next use. Call this after editing the configured key (e.g. from the editor window).
      /// </summary>
      public static void RefreshSecureKey()
      {
         _activeKeyResolved = false;
         _activeKey = null;
         _keyBytes = null;
         _keyBytesLegacy = null;
         _currentKeyForBytes = null;
      }

      /// <summary>
      /// The key under which the internal key list is stored. Chosen so it can never collide with
      /// any user key (a user key would be stored under <see cref="Prefix"/> + key).
      /// </summary>
      public const string KeyListKey = "PmPrefsMeta_KeyList";

      /// <summary>
      /// The registry slot used by older versions, which collided with a user key named "KeyList".
      /// Read once for migration, then removed. Exposed so the editor reader can recognize it.
      /// </summary>
      public const string LegacyKeyListKey = "PmPrefs__KeyList";

      /// <summary>
      /// Prefix added to all PmPrefs keys to distinguish from regular PlayerPrefs.
      /// </summary>
      public const string Prefix = "PmPrefs__";

      private static byte[] _keyBytes;        // v2 key (high iteration count)
      private static byte[] _keyBytesLegacy;  // v1 key (framework default iterations)
      private static string _currentKeyForBytes;

      /// <summary>
      /// Gets the runtime HashSet for O(1) key lookups.
      /// Backed by a List&lt;string&gt; in StringListWrapper for JsonUtility serialization.
      /// The HashSet is rebuilt from the serialized List on first access.
      /// </summary>
      private static HashSet<string> List
      {
         get
         {
            if (_keySet == null)
            {
               _listWrapper = null;

               string json = null;

               if (PlayerPrefs.HasKey(KeyListKey))
               {
                  json = PlayerPrefs.GetString(KeyListKey);
               }
               else if (PlayerPrefs.HasKey(LegacyKeyListKey))
               {
                  // Migrate the registry from the old (collision-prone) slot, but only if it
                  // actually parses as a key list. If it does not, the old slot likely holds a
                  // user value for a key named "KeyList" - leave it untouched.
                  string legacy = PlayerPrefs.GetString(LegacyKeyListKey);
                  StringListWrapper parsed = null;
                  try { parsed = JsonUtility.FromJson<StringListWrapper>(legacy); }
                  catch { parsed = null; }

                  if (parsed != null && parsed.items != null)
                  {
                     json = legacy;
                     PlayerPrefs.SetString(KeyListKey, legacy);
                     PlayerPrefs.DeleteKey(LegacyKeyListKey);
                  }
               }

               if (!string.IsNullOrEmpty(json))
               {
                  try
                  {
                     _listWrapper = JsonUtility.FromJson<StringListWrapper>(json);
                  }
                  catch (Exception ex)
                  {
                     Debug.LogWarning($"[PmPrefs] Failed to load key list: {ex.Message}");
                  }
               }

               if (_listWrapper == null)
               {
                  _listWrapper = new StringListWrapper();
               }
               if (_listWrapper.items == null)
               {
                  _listWrapper.items = new List<string>();
               }

               _keySet = new HashSet<string>(_listWrapper.items);
            }

            return _keySet;
         }
      }

      /// <summary>
      /// Ensures the derived key bytes are initialized for the active <see cref="SecureKey"/>.
      /// Derives both the current (v2) key and the legacy key (needed to read old data).
      /// </summary>
      private static byte[] GetKeyBytes()
      {
         string key = SecureKey;
         if (_keyBytes == null || _currentKeyForBytes != key)
         {
            byte[] saltBytes = Encoding.UTF8.GetBytes(SaltKey);

            // v2 key: stronger iteration count.
            using (var derive = new Rfc2898DeriveBytes(key, saltBytes, V2Iterations))
            {
               _keyBytes = derive.GetBytes(32);
            }

            // Legacy key: must match the original derivation (ASCII salt, framework default
            // iteration count) so data written by older versions still decrypts.
            using (var deriveLegacy = new Rfc2898DeriveBytes(key, Encoding.ASCII.GetBytes(SaltKey)))
            {
               _keyBytesLegacy = deriveLegacy.GetBytes(32);
            }

            _currentKeyForBytes = key;
         }
         return _keyBytes;
      }

      private static byte[] GetLegacyKeyBytes()
      {
         GetKeyBytes(); // ensures both keys are derived
         return _keyBytesLegacy;
      }

      private static void AddKeyToList(string key)
      {
         if (string.IsNullOrEmpty(key)) return;

         if (List.Add(key))
         {
            _isKeyListDirty = true;
         }
      }

      private static void RemoveKeyFromList(string key)
      {
         if (string.IsNullOrEmpty(key)) return;

         if (List.Remove(key))
         {
            _isKeyListDirty = true;
         }
      }

      private static void SaveKeyList()
      {
         _listWrapper.items = new List<string>(_keySet);
         string json = JsonUtility.ToJson(_listWrapper);
         PlayerPrefs.SetString(KeyListKey, json);
         _isKeyListDirty = false;
      }

      /// <summary>
      /// Flushes pending key list changes to disk.
      /// Call this after batch operations to persist the key list.
      /// </summary>
      /// <remarks>
      /// <para><b>When to call:</b></para>
      /// <list type="bullet">
      /// <item><description>After saving or deleting multiple keys in a batch operation</description></item>
      /// <item><description>Before critical checkpoints where data must be guaranteed on disk</description></item>
      /// <item><description>At the end of initialization routines that create many keys</description></item>
      /// <item><description>Before application quit if you've made changes since the last Save()</description></item>
      /// </list>
      /// <para><b>Performance Note:</b></para>
      /// <para>
      /// This method is very lightweight - it only writes to disk if changes have been made.
      /// Each call checks a dirty flag and skips the write operation if nothing has changed.
      /// </para>
      /// <para>
      /// The key list is flushed automatically by <see cref="SaveAll"/>. Unity's own
      /// <see cref="PlayerPrefs.Save"/> does NOT flush it, so prefer SaveAll() / FlushKeyList().
      /// </para>
      /// </remarks>
      public static void FlushKeyList()
      {
         if (_isKeyListDirty)
         {
            SaveKeyList();
         }
      }

      /// <summary>
      /// Encrypts the given plain text using AES-256-CBC with a random IV.
      /// </summary>
      /// <param name="plainText">The text to encrypt. Null is treated as an empty string.</param>
      /// <returns>An encrypted, format-tagged Base64 string. Empty input produces a valid (non-empty) ciphertext.</returns>
      public static string Encrypt(string plainText)
      {
         plainText = plainText ?? string.Empty;

         var plainTextBytes = Encoding.UTF8.GetBytes(plainText);

         using (var aes = Aes.Create())
         {
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = GetKeyBytes();
            aes.GenerateIV();
            byte[] iv = aes.IV;

            using (var memoryStream = new MemoryStream())
            using (var encryptor = aes.CreateEncryptor())
            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
            {
               cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
               cryptoStream.FlushFinalBlock();

               byte[] cipher = memoryStream.ToArray();
               byte[] combined = new byte[iv.Length + cipher.Length];
               Buffer.BlockCopy(iv, 0, combined, 0, iv.Length);
               Buffer.BlockCopy(cipher, 0, combined, iv.Length, cipher.Length);

               return V2Prefix + Convert.ToBase64String(combined);
            }
         }
      }

      /// <summary>
      /// Decrypts the given encrypted text. Supports both the current (v2) format and the legacy
      /// fixed-IV format written by older versions.
      /// </summary>
      /// <param name="encryptedText">Encrypted string produced by <see cref="Encrypt"/>.</param>
      /// <returns>Decrypted plain text, or empty string if input is invalid or decryption fails.</returns>
      public static string Decrypt(string encryptedText)
      {
         return TryDecrypt(encryptedText, out string result) ? result : string.Empty;
      }

      /// <summary>
      /// Attempts to decrypt the given text, distinguishing a genuinely-empty stored value
      /// (returns true, result == "") from a decryption failure (returns false).
      /// </summary>
      internal static bool TryDecrypt(string encryptedText, out string result)
      {
         result = string.Empty;

         if (string.IsNullOrEmpty(encryptedText))
            return false;

         try
         {
            bool isV2 = encryptedText.StartsWith(V2Prefix, StringComparison.Ordinal);
            string base64 = isV2 ? encryptedText.Substring(V2Prefix.Length) : encryptedText;

            byte[] data = Convert.FromBase64String(base64);

            byte[] iv;
            byte[] cipher;
            byte[] key;

            if (isV2)
            {
               if (data.Length < 16) return false;
               iv = new byte[16];
               Buffer.BlockCopy(data, 0, iv, 0, 16);
               cipher = new byte[data.Length - 16];
               Buffer.BlockCopy(data, 16, cipher, 0, cipher.Length);
               key = GetKeyBytes();
            }
            else
            {
               iv = Encoding.ASCII.GetBytes(LegacyIv);
               cipher = data;
               key = GetLegacyKeyBytes();
            }

            using (var aes = Aes.Create())
            {
               aes.Mode = CipherMode.CBC;
               aes.Padding = PaddingMode.PKCS7;
               aes.Key = key;
               aes.IV = iv;

               using (var memoryStream = new MemoryStream(cipher))
               using (var decryptor = aes.CreateDecryptor())
               using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
               using (var reader = new StreamReader(cryptoStream, Encoding.UTF8))
               {
                  result = reader.ReadToEnd();
                  return true;
               }
            }
         }
         catch (Exception ex)
         {
            Debug.LogWarning($"[PmPrefs] Decryption failed: {ex.Message}");
            result = string.Empty;
            return false;
         }
      }

      /// <summary>
      /// Deletes all PlayerPrefs data (both PmPrefs and regular PlayerPrefs).
      /// </summary>
      public static void DeleteAll()
      {
         PlayerPrefs.DeleteAll();
         _listWrapper = new StringListWrapper();
         _keySet = new HashSet<string>();
         _isKeyListDirty = false;
      }

      /// <summary>
      /// Deletes only PmPrefs entries, leaving regular PlayerPrefs intact.
      /// </summary>
      /// <remarks>
      /// This method immediately clears the key list and deletes all PmPrefs entries.
      /// No call to FlushKeyList() is needed as the key list is reset directly.
      /// </remarks>
      public static void DeleteAllPmPrefs()
      {
         foreach (var key in new List<string>(List))
         {
            PlayerPrefs.DeleteKey(Prefix + key);
         }
         PlayerPrefs.DeleteKey(KeyListKey);
         PlayerPrefs.DeleteKey(LegacyKeyListKey);
         _listWrapper = new StringListWrapper();
         _keySet = new HashSet<string>();
         _isKeyListDirty = false;
      }

      /// <summary>
      /// Checks if a key exists in PmPrefs.
      /// </summary>
      /// <param name="key">The key to check.</param>
      /// <returns>True if the key exists.</returns>
      public static bool HasKey(string key)
      {
         if (string.IsNullOrEmpty(key)) return false;
         return PlayerPrefs.HasKey(Prefix + key);
      }

      /// <summary>
      /// Deletes a specific key from PmPrefs.
      /// </summary>
      /// <param name="key">The key to delete.</param>
      /// <remarks>
      /// This method deletes the key immediately from PlayerPrefs but batches the key list update.
      /// Call FlushKeyList() (or SaveAll()) after bulk delete operations to persist the updated key list.
      /// </remarks>
      public static void DeleteKey(string key)
      {
         if (string.IsNullOrEmpty(key)) return;
         PlayerPrefs.DeleteKey(Prefix + key);
         RemoveKeyFromList(key);
      }

      /// <summary>
      /// Flushes the key list and saves all pending changes to disk.
      /// </summary>
      public static void SaveAll()
      {
         FlushKeyList();
         PlayerPrefs.Save();
      }

      /// <summary>
      /// Gets all keys stored in PmPrefs.
      /// </summary>
      /// <returns>List of all stored keys (without prefix).</returns>
      public static List<string> GetAllKeys() => new List<string>(List);

      /// <summary>
      /// Saves a value with a key that can be any type (uses ToString()).
      /// </summary>
      /// <typeparam name="T">The type of the key.</typeparam>
      /// <param name="key">The key (will be converted to string).</param>
      /// <param name="value">The value to save.</param>
      public static void Save<T>(T key, object value)
      {
         if (key == null) return;
         Save(key.ToString(), value);
      }

      /// <summary>
      /// Saves a value with the specified key. The value is encrypted and stored in PlayerPrefs.
      /// Supports primitives (string, int, float, bool, etc.), enums, decimals and complex
      /// [Serializable] objects.
      /// </summary>
      /// <param name="key">The key to save under.</param>
      /// <param name="value">The value to save.</param>
      public static void Save(string key, object value)
      {
         if (string.IsNullOrEmpty(key)) return;

         string str;
         if (value == null)
            str = "";
         else if (value is string s)
            str = s;
         else if (value is Enum)
            str = value.ToString();
         else if (value.GetType().IsPrimitive || value is decimal)
            str = Convert.ToString(value, CultureInfo.InvariantCulture);
         else
            str = JsonUtility.ToJson(value);

         AddKeyToList(key);
         PlayerPrefs.SetString(Prefix + key, Encrypt(str));
      }

      /// <summary>
      /// Saves a raw string value with encryption, bypassing JSON serialization.
      /// The value is encrypted and stored directly without JsonUtility.ToJson wrapping.
      /// </summary>
      /// <param name="key">The key to save under.</param>
      /// <param name="rawValue">The raw string value to encrypt and store.</param>
      public static void SaveRaw(string key, string rawValue)
      {
         if (string.IsNullOrEmpty(key)) return;
         AddKeyToList(key);
         PlayerPrefs.SetString(Prefix + key, Encrypt(rawValue ?? ""));
      }

      /// <summary>
      /// Loads a value with a key that can be any type.
      /// </summary>
      /// <typeparam name="TK">The type of the key.</typeparam>
      /// <typeparam name="T">The type of the value to load.</typeparam>
      /// <param name="key">The key (will be converted to string).</param>
      /// <param name="defaultValue">Default value if key doesn't exist or loading fails.</param>
      /// <returns>The loaded value or default.</returns>
      public static T Load<TK, T>(TK key, T defaultValue = default)
      {
         if (key == null) return defaultValue;
         return Load(key.ToString(), defaultValue);
      }

      /// <summary>
      /// Loads a value from PmPrefs.
      /// Supports primitives (string, int, float, bool, etc.), enums, decimals and complex
      /// [Serializable] objects.
      /// </summary>
      /// <typeparam name="T">The type of the value to load.</typeparam>
      /// <param name="key">The key to load.</param>
      /// <param name="defaultValue">Default value if key doesn't exist or loading fails.</param>
      /// <returns>The loaded value or default.</returns>
      public static T Load<T>(string key, T defaultValue = default)
      {
         if (string.IsNullOrEmpty(key)) return defaultValue;

         if (!HasKey(key)) return defaultValue;

         try
         {
            var encryptedValue = PlayerPrefs.GetString(Prefix + key);
            if (!TryDecrypt(encryptedValue, out string decrypted)) return defaultValue;

            Type targetType = typeof(T);

            // A successfully decrypted (possibly empty) string round-trips verbatim.
            if (targetType == typeof(string))
               return (T)(object)decrypted;

            // Non-string targets cannot be parsed from an empty string.
            if (string.IsNullOrEmpty(decrypted))
               return defaultValue;

            if (targetType == typeof(int))
            {
               if (int.TryParse(decrypted, NumberStyles.Any, CultureInfo.InvariantCulture, out int result))
                  return (T)(object)result;
               return defaultValue;
            }

            if (targetType == typeof(float))
            {
               if (float.TryParse(decrypted, NumberStyles.Any, CultureInfo.InvariantCulture, out float result))
                  return (T)(object)result;
               return defaultValue;
            }

            if (targetType == typeof(bool))
            {
               if (bool.TryParse(decrypted, out bool result))
                  return (T)(object)result;
               return defaultValue;
            }

            if (targetType == typeof(double))
            {
               if (double.TryParse(decrypted, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                  return (T)(object)result;
               return defaultValue;
            }

            if (targetType == typeof(long))
            {
               if (long.TryParse(decrypted, NumberStyles.Any, CultureInfo.InvariantCulture, out long result))
                  return (T)(object)result;
               return defaultValue;
            }

            if (targetType.IsEnum)
            {
               try { return (T)Enum.Parse(targetType, decrypted); }
               catch { return defaultValue; }
            }

            // Remaining primitives (byte, sbyte, short, ushort, uint, ulong, char) and decimal.
            if (targetType.IsPrimitive || targetType == typeof(decimal))
            {
               try { return (T)Convert.ChangeType(decrypted, targetType, CultureInfo.InvariantCulture); }
               catch { return defaultValue; }
            }

            return JsonUtility.FromJson<T>(decrypted);
         }
         catch (Exception ex)
         {
            Debug.LogError($"[PmPrefs] Failed to load key '{key}': {ex.Message}");
            return defaultValue;
         }
      }

      /// <summary>
      /// Clears the internal key list cache. Call this if you've modified PlayerPrefs directly.
      /// </summary>
      public static void RefreshKeyCache()
      {
         _listWrapper = null;
         _keySet = null;
         _isKeyListDirty = false;
      }
   }
}
