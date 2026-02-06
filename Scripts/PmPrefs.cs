using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace PM.Plugins
{
   /// <summary>
   /// PmPrefs provides encrypted PlayerPrefs storage for Unity.
   /// Save and load any serializable object with automatic AES encryption.
   /// </summary>
   /// <example>
   /// <code>
   /// // Save data
   /// PmPrefs.Save("playerName", "John");
   /// PmPrefs.Save("settings", mySettingsObject);
   ///
   /// // Load data
   /// string name = PmPrefs.Load&lt;string&gt;("playerName", "DefaultName");
   /// MySettings settings = PmPrefs.Load&lt;MySettings&gt;("settings");
   /// </code>
   /// </example>
   public static class PmPrefs
   {
      /// <summary>
      /// Wrapper class for JSON serialization of HashSet&lt;string&gt;.
      /// Uses HashSet for O(1) key lookups, additions, and removals, providing better performance than List&lt;string&gt;
      /// which would require O(n) operations for duplicate checking and removals.
      /// Unity's JsonUtility doesn't support generic HashSet directly, so this wrapper enables serialization.
      /// </summary>
      [Serializable]
      private class StringListWrapper
      {
         public HashSet<string> items = new HashSet<string>();
      }

      /// <summary>
      /// Legacy wrapper class for backward compatibility with old List&lt;string&gt; format.
      /// Used only for migrating existing data to the new HashSet-based format.
      /// The List format is deprecated due to O(n) performance for duplicate checking and removals.
      /// </summary>
      [Serializable]
      private class LegacyStringListWrapper
      {
         public List<string> items = new List<string>();
      }

      private static StringListWrapper _listWrapper;

      private const string SaltKey = "F1m5eJVO9ASPxGW7B3KP9t8iNd5Edpb48LAGNlWcLHeNkeH6PNYf3BCztZB7D3ch";
      private const string ViKey = "NiB3KP9VksfNf3Bi";

      /// <summary>
      /// The encryption key used for all PmPrefs data.
      /// Change this to a unique value for your project for better security.
      /// Must be at least 8 characters, alphanumeric only.
      /// </summary>
      public const string SecureKey = "LoKo1Nibu75XXzu";

      private const string KeyListKey = "PmPrefs__KeyList";

      /// <summary>
      /// Prefix added to all PmPrefs keys to distinguish from regular PlayerPrefs.
      /// </summary>
      public const string Prefix = "PmPrefs__";

      private static byte[] _keyBytes;
      private static string _currentSecureKey;

      /// <summary>
      /// Gets the internal HashSet used for tracking all PmPrefs keys.
      /// Uses HashSet for O(1) performance on add, remove, and contains operations.
      /// Automatically handles migration from legacy List&lt;string&gt; format to HashSet on first access.
      /// </summary>
      /// <remarks>
      /// Performance benefits of HashSet over List:
      /// - Add with duplicate check: O(1) vs O(n)
      /// - Remove: O(1) vs O(n)
      /// - Contains: O(1) vs O(n)
      /// This significantly improves performance when managing large numbers of keys.
      /// </remarks>
      private static HashSet<string> List
      {
         get
         {
            if (_listWrapper == null)
            {
               if (PlayerPrefs.HasKey(KeyListKey))
               {
                  string json = PlayerPrefs.GetString(KeyListKey);
                  if (!string.IsNullOrEmpty(json))
                  {
                     // Try loading as new format (HashSet)
                     try
                     {
                        _listWrapper = JsonUtility.FromJson<StringListWrapper>(json);
                     }
                     catch (Exception ex)
                     {
                        Debug.LogWarning($"[PmPrefs] Failed to load key list as new format: {ex.Message}");
                     }

                     // If new format failed or resulted in empty items, try loading as legacy format (List)
                     if (_listWrapper == null || _listWrapper.items == null || _listWrapper.items.Count == 0)
                     {
                        try
                        {
                           LegacyStringListWrapper legacyWrapper = JsonUtility.FromJson<LegacyStringListWrapper>(json);
                           if (legacyWrapper != null && legacyWrapper.items != null && legacyWrapper.items.Count > 0)
                           {
                              // Convert legacy List to new HashSet format
                              _listWrapper = new StringListWrapper();
                              foreach (var item in legacyWrapper.items)
                              {
                                 _listWrapper.items.Add(item);
                              }

                              // Save in new format to complete migration
                              SaveKeyList();
                              Debug.Log("[PmPrefs] Migrated key list from legacy List format to HashSet format");
                           }
                        }
                        catch (Exception ex)
                        {
                           Debug.LogWarning($"[PmPrefs] Failed to load key list as legacy format: {ex.Message}");
                        }
                     }
                  }
               }

               if (_listWrapper == null)
               {
                  _listWrapper = new StringListWrapper();
               }
            }

            return _listWrapper.items;
         }
      }

      /// <summary>
      /// Ensures the key derivation bytes are initialized with the current SecureKey.
      /// </summary>
      private static byte[] GetKeyBytes()
      {
         if (_keyBytes == null || _currentSecureKey != SecureKey)
         {
            using (var derive = new Rfc2898DeriveBytes(SecureKey, Encoding.ASCII.GetBytes(SaltKey)))
            {
               _keyBytes = derive.GetBytes(32);
            }
            _currentSecureKey = SecureKey;
         }
         return _keyBytes;
      }

      /// <summary>
      /// Adds a key to the internal HashSet tracking system.
      /// HashSet.Add automatically prevents duplicates and returns false if the key already exists,
      /// eliminating the need for manual Contains checks (O(1) vs O(n) with List).
      /// Only saves to PlayerPrefs if the key was actually added.
      /// </summary>
      /// <param name="key">The key to add to the tracking system.</param>
      private static void AddKeyToList(string key)
      {
         if (string.IsNullOrEmpty(key)) return;

         if (List.Add(key))
         {
            SaveKeyList();
         }
      }

      /// <summary>
      /// Removes a key from the internal HashSet tracking system.
      /// HashSet.Remove provides O(1) performance compared to List.Remove which requires O(n) searching.
      /// Only saves to PlayerPrefs if the key was actually removed.
      /// </summary>
      /// <param name="key">The key to remove from the tracking system.</param>
      private static void RemoveKeyFromList(string key)
      {
         if (string.IsNullOrEmpty(key)) return;

         if (List.Remove(key))
         {
            SaveKeyList();
         }
      }

      /// <summary>
      /// Serializes and saves the HashSet-based key list to PlayerPrefs.
      /// The HashSet is wrapped in StringListWrapper for JSON serialization compatibility.
      /// </summary>
      private static void SaveKeyList()
      {
         string json = JsonUtility.ToJson(_listWrapper);
         PlayerPrefs.SetString(KeyListKey, json);
      }

      /// <summary>
      /// Encrypts the given plain text using AES encryption.
      /// </summary>
      /// <param name="plainText">The text to encrypt.</param>
      /// <returns>Base64 encoded encrypted string, or empty string if input is null/empty.</returns>
      public static string Encrypt(string plainText)
      {
         if (string.IsNullOrEmpty(plainText))
            return string.Empty;

         var plainTextBytes = Encoding.UTF8.GetBytes(plainText.Trim());

         using (var aes = Aes.Create())
         {
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = GetKeyBytes();
            aes.IV = Encoding.ASCII.GetBytes(ViKey);

            using (var memoryStream = new MemoryStream())
            using (var encryptor = aes.CreateEncryptor())
            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
            {
               cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
               cryptoStream.FlushFinalBlock();
               return Convert.ToBase64String(memoryStream.ToArray());
            }
         }
      }

      /// <summary>
      /// Decrypts the given encrypted text.
      /// </summary>
      /// <param name="encryptedText">Base64 encoded encrypted string.</param>
      /// <returns>Decrypted plain text, or empty string if input is invalid.</returns>
      public static string Decrypt(string encryptedText)
      {
         if (string.IsNullOrEmpty(encryptedText))
            return string.Empty;

         try
         {
            var cipherTextBytes = Convert.FromBase64String(encryptedText);

            using (var aes = Aes.Create())
            {
               aes.Mode = CipherMode.CBC;
               aes.Padding = PaddingMode.PKCS7;
               aes.Key = GetKeyBytes();
               aes.IV = Encoding.ASCII.GetBytes(ViKey);

               using (var memoryStream = new MemoryStream(cipherTextBytes))
               using (var decryptor = aes.CreateDecryptor())
               using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
               using (var reader = new StreamReader(cryptoStream, Encoding.UTF8))
               {
                  return reader.ReadToEnd();
               }
            }
         }
         catch (Exception ex)
         {
            Debug.LogWarning($"[PmPrefs] Decryption failed: {ex.Message}");
            return string.Empty;
         }
      }

      /// <summary>
      /// Deletes all PlayerPrefs data (both PmPrefs and regular PlayerPrefs).
      /// </summary>
      public static void DeleteAll()
      {
         PlayerPrefs.DeleteAll();
         _listWrapper = new StringListWrapper();
      }

      /// <summary>
      /// Deletes only PmPrefs entries, leaving regular PlayerPrefs intact.
      /// </summary>
      public static void DeleteAllPmPrefs()
      {
         foreach (var key in new List<string>(List))
         {
            PlayerPrefs.DeleteKey(Prefix + key);
         }
         PlayerPrefs.DeleteKey(KeyListKey);
         _listWrapper = new StringListWrapper();
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
      public static void DeleteKey(string key)
      {
         if (string.IsNullOrEmpty(key)) return;
         PlayerPrefs.DeleteKey(Prefix + key);
         RemoveKeyFromList(key);
      }

      /// <summary>
      /// Saves all pending changes to disk.
      /// </summary>
      public static void SaveAll() => PlayerPrefs.Save();

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
      /// Saves a value with the specified key. The value is serialized to JSON and encrypted.
      /// </summary>
      /// <param name="key">The key to save under.</param>
      /// <param name="value">The value to save (must be serializable by JsonUtility).</param>
      public static void Save(string key, object value)
      {
         if (string.IsNullOrEmpty(key)) return;

         string str = JsonUtility.ToJson(value);
         AddKeyToList(key);
         PlayerPrefs.SetString(Prefix + key, Encrypt(str));
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
            if (string.IsNullOrEmpty(encryptedValue)) return defaultValue;

            var decrypted = Decrypt(encryptedValue);
            if (string.IsNullOrEmpty(decrypted)) return defaultValue;

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
      }
   }
}
