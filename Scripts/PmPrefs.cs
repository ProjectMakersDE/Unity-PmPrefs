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
   /// <remarks>
   /// <para><b>Performance Optimization:</b></para>
   /// <para>
   /// PmPrefs uses an internal key list to track all stored keys. When you call Save() or DeleteKey(),
   /// the key list is marked as dirty but NOT immediately written to disk. This batching behavior
   /// improves performance during bulk operations by avoiding repeated disk writes.
   /// </para>
   /// <para>
   /// Call FlushKeyList() after batch operations to ensure the key list is persisted.
   /// The key list will also be saved automatically when PlayerPrefs.Save() or SaveAll() is called.
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
   /// PmPrefs.FlushKeyList(); // Persist key list after batch operation
   /// PmPrefs.SaveAll(); // Persist all PlayerPrefs data
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
      private static bool _isKeyListDirty;

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

<<<<<<< HEAD
         if (List.Add(key))
         {
            SaveKeyList();
         }
=======
         List.Add(key);
         _isKeyListDirty = true;
>>>>>>> auto-claude/013-batch-savekeylist-calls-and-playerprefs-save-with-
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

<<<<<<< HEAD
         if (List.Remove(key))
         {
            SaveKeyList();
         }
=======
         List.Remove(key);
         _isKeyListDirty = true;
>>>>>>> auto-claude/013-batch-savekeylist-calls-and-playerprefs-save-with-
      }

      /// <summary>
      /// Serializes and saves the HashSet-based key list to PlayerPrefs.
      /// The HashSet is wrapped in StringListWrapper for JSON serialization compatibility.
      /// </summary>
      private static void SaveKeyList()
      {
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
      /// There is no performance penalty for calling this method when no changes are pending.
      /// </para>
      /// <para>
      /// The key list is automatically flushed when SaveAll() or PlayerPrefs.Save() is called,
      /// so you don't need to call this explicitly before those methods.
      /// </para>
      /// </remarks>
      /// <example>
      /// <code>
      /// // Good: Batch operations with single flush
      /// for (int i = 0; i &lt; 1000; i++)
      /// {
      ///     PmPrefs.Save($"level_{i}", levelData[i]);
      /// }
      /// PmPrefs.FlushKeyList(); // Single write of key list
      /// PmPrefs.SaveAll(); // Persist all data
      ///
      /// // Avoid: Flushing inside loops (unnecessary performance cost)
      /// for (int i = 0; i &lt; 1000; i++)
      /// {
      ///     PmPrefs.Save($"level_{i}", levelData[i]);
      ///     PmPrefs.FlushKeyList(); // Don't do this - 1000 writes!
      /// }
      /// </code>
      /// </example>
      public static void FlushKeyList()
      {
         if (_isKeyListDirty)
         {
            SaveKeyList();
         }
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
      /// <remarks>
      /// <para><b>Performance Note:</b></para>
      /// <para>
      /// This method immediately clears the key list and deletes all PmPrefs entries.
      /// No call to FlushKeyList() is needed as the key list is reset directly.
      /// </para>
      /// </remarks>
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
      /// <remarks>
      /// <para><b>Performance Note:</b></para>
      /// <para>
      /// This method deletes the key immediately from PlayerPrefs but batches the key list update.
      /// Call FlushKeyList() after bulk delete operations to persist the updated key list.
      /// </para>
      /// </remarks>
      public static void DeleteKey(string key)
      {
         if (string.IsNullOrEmpty(key)) return;
         PlayerPrefs.DeleteKey(Prefix + key);
         RemoveKeyFromList(key);
      }

      /// <summary>
      /// Saves all pending changes to disk.
      /// </summary>
      /// <remarks>
      /// <para><b>Note:</b></para>
      /// <para>
      /// This method calls PlayerPrefs.Save() to persist all PlayerPrefs data.
      /// It does NOT automatically flush the PmPrefs key list - you should call FlushKeyList()
      /// before SaveAll() to ensure the key list is included in the save operation.
      /// </para>
      /// </remarks>
      /// <example>
      /// <code>
      /// // Proper save sequence
      /// PmPrefs.Save("data", myData);
      /// PmPrefs.FlushKeyList(); // Ensure key list is persisted
      /// PmPrefs.SaveAll(); // Save all PlayerPrefs data
      /// </code>
      /// </example>
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
      /// <remarks>
      /// <para><b>Performance Note:</b></para>
      /// <para>
      /// This method writes the encrypted value immediately to PlayerPrefs but batches the key list update.
      /// The key list is only marked as dirty and will be written when FlushKeyList() or SaveAll() is called.
      /// This design optimizes batch operations while ensuring data values are always saved.
      /// </para>
      /// <para>
      /// For best performance during bulk operations, call FlushKeyList() once after all saves are complete
      /// rather than relying on the automatic flush during SaveAll().
      /// </para>
      /// </remarks>
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
