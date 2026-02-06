# PmPrefs Architecture

This document describes the internal architecture, encryption implementation, and data flow of the PmPrefs package. Understanding these details helps contributors maintain the codebase and helps security-conscious users make informed decisions about using PmPrefs in production.

## Table of Contents

- [Overview](#overview)
- [Data Flow Pipeline](#data-flow-pipeline)
- [Encryption Implementation](#encryption-implementation)
  - [Key Derivation (PBKDF2)](#key-derivation-pbkdf2)
  - [AES-256-CBC Encryption](#aes-256-cbc-encryption)
  - [Security Implications](#security-implications)
- [Key Management System](#key-management-system)
- [Cross-Platform Storage](#cross-platform-storage)
- [Editor Implementation](#editor-implementation)
- [API Design Patterns](#api-design-patterns)

---

## Overview

PmPrefs is a wrapper around Unity's `PlayerPrefs` that adds transparent encryption. The core design principle is **simplicity**: developers call `Save()` and `Load()` just like `PlayerPrefs`, but data is automatically encrypted before storage.

**Architecture layers:**

1. **Public API** (`PmPrefs.cs`) - Simple Save/Load methods
2. **Serialization** - JSON conversion via Unity's `JsonUtility`
3. **Encryption** - AES-256-CBC with PBKDF2 key derivation
4. **Storage** - Unity's `PlayerPrefs` (platform-specific backends)
5. **Key Tracking** - Internal list to enumerate all PmPrefs keys

---

## Data Flow Pipeline

### Save Operation

```
User Object → JSON → Encrypt → Base64 → PlayerPrefs
```

**Detailed steps:**

1. **Serialize**: Object is converted to JSON string using `JsonUtility.ToJson()`
2. **Encrypt**: JSON string is encrypted using AES-256-CBC (see [Encryption](#encryption-implementation))
3. **Encode**: Encrypted bytes are encoded as Base64 string
4. **Store**: Base64 string is stored in `PlayerPrefs` with prefixed key (`PmPrefs__` + key)
5. **Track**: Key is added to internal key list (`PmPrefs__KeyList`)

**Code flow:**
```csharp
// PmPrefs.cs:267-274
public static void Save(string key, object value)
{
    string str = JsonUtility.ToJson(value);           // Step 1: Serialize
    AddKeyToList(key);                                 // Step 5: Track
    PlayerPrefs.SetString(Prefix + key, Encrypt(str)); // Steps 2-4: Encrypt + Store
}
```

### Load Operation

```
PlayerPrefs → Base64 → Decrypt → JSON → User Object
```

**Detailed steps:**

1. **Retrieve**: Get Base64 string from `PlayerPrefs` using prefixed key
2. **Decode**: Decode Base64 to encrypted bytes
3. **Decrypt**: Decrypt bytes using AES-256-CBC (see [Encryption](#encryption-implementation))
4. **Deserialize**: Convert JSON string back to object using `JsonUtility.FromJson<T>()`

**Code flow:**
```csharp
// PmPrefs.cs:297-318
public static T Load<T>(string key, T defaultValue = default)
{
    var encryptedValue = PlayerPrefs.GetString(Prefix + key); // Step 1: Retrieve
    var decrypted = Decrypt(encryptedValue);                  // Steps 2-3: Decrypt
    return JsonUtility.FromJson<T>(decrypted);                // Step 4: Deserialize
}
```

---

## Encryption Implementation

PmPrefs uses industry-standard **AES-256-CBC** encryption with **PBKDF2** key derivation. This section explains the cryptographic details and security implications.

### Key Derivation (PBKDF2)

The user-provided `SecureKey` (default: `"LoKo1Nibu75XXzu"`) is too weak to use directly as an encryption key. PBKDF2 (Password-Based Key Derivation Function 2) strengthens it through multiple iterations.

**Implementation:**
```csharp
// PmPrefs.cs:91-102
private static byte[] GetKeyBytes()
{
    using (var derive = new Rfc2898DeriveBytes(SecureKey, Encoding.ASCII.GetBytes(SaltKey)))
    {
        _keyBytes = derive.GetBytes(32); // Derive 256-bit (32-byte) key
    }
    return _keyBytes;
}
```

**Constants:**
```csharp
private const string SaltKey = "F1m5eJVO9ASPxGW7B3KP9t8iNd5Edpb48LAGNlWcLHeNkeH6PNYf3BCztZB7D3ch";
public const string SecureKey = "LoKo1Nibu75XXzu"; // Default - should be changed per project
```

**Key derivation process:**
1. Input: `SecureKey` (password) + `SaltKey` (salt)
2. Algorithm: PBKDF2-HMAC-SHA1 (default for `Rfc2898DeriveBytes`)
3. Iterations: 1000 (default for `Rfc2898DeriveBytes`)
4. Output: 256-bit (32-byte) AES key

**Caching:** The derived key is cached in `_keyBytes` and reused until `SecureKey` changes. This avoids re-deriving the key on every encryption operation.

### AES-256-CBC Encryption

PmPrefs uses **AES (Advanced Encryption Standard)** in **CBC (Cipher Block Chaining)** mode with **PKCS7 padding**.

**Encryption implementation:**
```csharp
// PmPrefs.cs:133-156
public static string Encrypt(string plainText)
{
    var plainTextBytes = Encoding.UTF8.GetBytes(plainText.Trim());

    using (var aes = Aes.Create())
    {
        aes.Mode = CipherMode.CBC;           // Cipher Block Chaining
        aes.Padding = PaddingMode.PKCS7;     // PKCS#7 padding
        aes.Key = GetKeyBytes();             // 256-bit key from PBKDF2
        aes.IV = Encoding.ASCII.GetBytes(ViKey); // Static 16-byte IV (!)

        using (var encryptor = aes.CreateEncryptor())
        using (var cryptoStream = new CryptoStream(...))
        {
            cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
            cryptoStream.FlushFinalBlock();
            return Convert.ToBase64String(memoryStream.ToArray());
        }
    }
}
```

**Decryption implementation:**
```csharp
// PmPrefs.cs:163-193
public static string Decrypt(string encryptedText)
{
    var cipherTextBytes = Convert.FromBase64String(encryptedText);

    using (var aes = Aes.Create())
    {
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = GetKeyBytes();
        aes.IV = Encoding.ASCII.GetBytes(ViKey); // Same static IV

        using (var decryptor = aes.CreateDecryptor())
        using (var cryptoStream = new CryptoStream(...))
        using (var reader = new StreamReader(cryptoStream, Encoding.UTF8))
        {
            return reader.ReadToEnd();
        }
    }
}
```

**Encryption parameters:**
- **Algorithm**: AES (Rijndael with 128-bit block size)
- **Key size**: 256 bits (32 bytes)
- **Mode**: CBC (Cipher Block Chaining)
- **Padding**: PKCS7
- **IV**: Static 16-byte value: `"NiB3KP9VksfNf3Bi"`

### Security Implications

⚠️ **Important security considerations:**

#### 1. Static Initialization Vector (IV)

**The IV is static and hardcoded:**
```csharp
private const string ViKey = "NiB3KP9VksfNf3Bi";
```

**Security impact:**
- ✅ **Good**: Protects against casual inspection of save files
- ⚠️ **Limitation**: Using the same IV for all encryptions is **not cryptographically ideal**
- ⚠️ **Risk**: If an attacker knows the plaintext of one encrypted value, they can derive information about other encrypted values with identical prefixes
- ⚠️ **Risk**: Identical plaintext always produces identical ciphertext (pattern leakage)

**Best practice (not implemented):** Each encryption operation should generate a random IV and prepend it to the ciphertext. This would require format changes and break compatibility with existing save data.

#### 2. Hardcoded Salt

The salt for PBKDF2 is hardcoded in the source:
```csharp
private const string SaltKey = "F1m5eJVO9ASPxGW7B3KP9t8iNd5Edpb48LAGNlWcLHeNkeH6PNYf3BCztZB7D3ch";
```

**Security impact:**
- ✅ **Good**: Salt prevents rainbow table attacks on the password
- ⚠️ **Limitation**: Same salt is used across all projects using PmPrefs
- ✅ **Acceptable**: For local save data protection, this is a reasonable trade-off

#### 3. Key Storage in Source Code

Both `SecureKey` and `SaltKey` are stored as constants in `PmPrefs.cs`:

**Security impact:**
- ⚠️ **Critical limitation**: Anyone with access to the compiled game can extract these keys
- ✅ **Use case**: PmPrefs is designed for **save game protection**, not cryptographic security
- ⚠️ **Warning**: Do NOT use PmPrefs to protect sensitive data like passwords, tokens, or payment information

#### 4. Threat Model

**PmPrefs protects against:**
- ✅ Casual users editing save files in text editors
- ✅ Basic cheat tools scanning for readable values
- ✅ Accidental exposure of save data

**PmPrefs does NOT protect against:**
- ❌ Determined attackers with reverse engineering skills
- ❌ Memory editing tools (CheatEngine, etc.)
- ❌ Decompilation and key extraction
- ❌ Pattern analysis attacks (due to static IV)

**Recommendation:**
> PmPrefs is suitable for protecting single-player save games, user preferences, and offline data. For sensitive data or anti-cheat in competitive games, implement server-side validation and consider additional security measures.

#### 5. Changing the Encryption Key

Users can change `SecureKey` via the Editor window or by modifying the constant:

**Important notes:**
- ⚠️ Changing the key after data is saved will make existing data **unreadable**
- ⚠️ Export data before changing keys, then re-import after
- ✅ Each project should use a unique key for better security

---

## Key Management System

PmPrefs maintains an internal list of all saved keys to enable enumeration and bulk operations. This is necessary because `PlayerPrefs` has no native API to list all keys.

### Key List Storage

**Implementation:**
```csharp
// PmPrefs.cs:28-32
[Serializable]
private class StringListWrapper
{
    public List<string> items = new List<string>();
}
```

**Storage location:**
- Key: `"PmPrefs__KeyList"`
- Format: JSON-serialized `StringListWrapper`
- Stored in: `PlayerPrefs` (unencrypted)

**Why a wrapper class?**
Unity's `JsonUtility` cannot serialize `List<string>` directly. The wrapper class provides a serializable container.

### Key Tracking Operations

**Adding a key:**
```csharp
// PmPrefs.cs:104-111
private static void AddKeyToList(string key)
{
    if (List.Contains(key)) return; // Avoid duplicates
    List.Add(key);
    SaveKeyList(); // Persist immediately
}
```

**Removing a key:**
```csharp
// PmPrefs.cs:113-120
private static void RemoveKeyFromList(string key)
{
    if (!List.Contains(key)) return;
    List.Remove(key);
    SaveKeyList(); // Persist immediately
}
```

**Lazy loading:**
```csharp
// PmPrefs.cs:56-86
private static List<string> List
{
    get
    {
        if (_listWrapper == null)
        {
            // Load from PlayerPrefs on first access
            string json = PlayerPrefs.GetString(KeyListKey);
            _listWrapper = JsonUtility.FromJson<StringListWrapper>(json);
        }
        return _listWrapper.items;
    }
}
```

### Key Prefix System

All PmPrefs keys are stored with a prefix to distinguish them from regular `PlayerPrefs`:

```csharp
public const string Prefix = "PmPrefs__";
```

**Example:**
- User key: `"playerName"`
- Stored as: `"PmPrefs__playerName"`

**Benefits:**
- Easy identification of PmPrefs vs PlayerPrefs
- Enables `DeleteAllPmPrefs()` without affecting regular PlayerPrefs
- Editor window can show separate lists

### Cache Invalidation

The key list is cached in memory. To refresh after external modifications:

```csharp
// PmPrefs.cs:323-326
public static void RefreshKeyCache()
{
    _listWrapper = null; // Force reload on next access
}
```

---

## Cross-Platform Storage

Unity's `PlayerPrefs` uses different storage backends per platform. PmPrefs inherits this behavior and provides cross-platform key enumeration in the Editor.

### Storage Locations

| Platform | Storage Backend | Location |
|----------|----------------|----------|
| **Windows** | Windows Registry | `HKEY_CURRENT_USER\Software\Unity\UnityEditor\[CompanyName]\[ProductName]` |
| **macOS** | Plist file | `~/Library/Preferences/unity.[companyname].[productname].plist` |
| **Linux** | XML prefs file | `~/.config/unity3d/[CompanyName]/[ProductName]/prefs` |
| **iOS** | NSUserDefaults | Managed by iOS |
| **Android** | SharedPreferences | `/data/data/[package]/shared_prefs/[package].xml` |
| **WebGL** | Browser storage | IndexedDB or LocalStorage |

### Editor Key Reading (PrefsKeyReader.cs)

The Editor window needs to read all keys from storage. `PrefsKeyReader.cs` implements platform-specific readers:

#### Windows (Registry)
```csharp
// PrefsKeyReader.cs:124-159
private Dictionary<string, object> GetKeysFromWindowsRegistry()
{
    string registryPath = $@"Software\Unity\UnityEditor\{PlayerSettings.companyName}\{PlayerSettings.productName}";
    using (var key = Registry.CurrentUser.OpenSubKey(registryPath))
    {
        foreach (var valueName in key.GetValueNames())
        {
            // Unity adds hash suffix: "keyname_h12345"
            int lastUnderscore = valueName.LastIndexOf('_');
            string cleanName = valueName.Substring(0, lastUnderscore);
            result[cleanName] = key.GetValue(valueName);
        }
    }
}
```

**Note:** Unity appends a hash suffix (`_h12345`) to prevent key collisions. The reader strips this suffix.

#### macOS (Plist)
```csharp
// PrefsKeyReader.cs:163-219
private Dictionary<string, object> GetKeysFromMacOSPlist()
{
    string plistPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Personal),
        $"Library/Preferences/unity.{companyName}.{productName}.plist"
    );

    // Use macOS plutil command to convert binary plist to readable format
    var process = Process.Start("/usr/bin/plutil", $"-p \"{plistPath}\"");
    string output = process.StandardOutput.ReadToEnd();

    // Parse key-value pairs from output
    var match = Regex.Match(line, @"""([^""]+)""\s*=>\s*(.+)");
}
```

**Note:** Plist files are binary. The code uses macOS's `plutil` command to convert to readable format.

#### Linux (XML)
```csharp
// PrefsKeyReader.cs:230-281
private Dictionary<string, object> GetKeysFromLinuxPrefs()
{
    string prefsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Personal),
        ".config/unity3d",
        PlayerSettings.companyName,
        PlayerSettings.productName,
        "prefs"
    );

    string content = File.ReadAllText(prefsPath);
    var keyMatches = Regex.Matches(content, @"<pref\s+name=""([^""]+)""[^>]*>([^<]*)</pref>");
}
```

**Note:** Linux prefs is an XML file with `<pref>` tags.

### Fallback Method

If platform-specific reading fails, fall back to the tracked key list:

```csharp
// PrefsKeyReader.cs:288-304
private Dictionary<string, object> GetKeysFromTrackedList()
{
    var pmPrefsKeys = PmPrefs.GetAllKeys();
    foreach (var key in pmPrefsKeys)
    {
        result[PmPrefs.Prefix + key] = PlayerPrefs.GetString(PmPrefs.Prefix + key);
    }
}
```

**Limitation:** This only returns PmPrefs keys, not regular PlayerPrefs.

### Caching

Key reading is cached for 2 seconds to avoid repeated file/registry access:

```csharp
// PrefsKeyReader.cs:23-25
private DateTime _lastCacheTime;
private static readonly TimeSpan CacheTimeout = TimeSpan.FromSeconds(2);
```

---

## Editor Implementation

The Editor window (`PmPrefsEditorWindow.cs`) provides a GUI for managing preferences.

### Window Structure

**Main components:**
1. **Toolbar**: Create, Configuration, Delete All, Save, Refresh buttons
2. **Shield icon**: Toggle encrypted/decrypted view
3. **Tab bar**: Switch between PmPrefs and PlayerPrefs
4. **List view**: Scrollable list of key-value pairs

### Encrypted vs Decrypted View

Users can toggle between viewing encrypted (Base64) or decrypted (plaintext) values:

```csharp
// PrefsKeyReader.cs:55-65
if (_editorWindow.ShowEncrypted)
{
    // Show decrypted value
    string encrypted = PlayerPrefs.GetString(keyName);
    strValue = PmPrefs.Decrypt(encrypted);
}
else
{
    // Show encrypted value (Base64)
    strValue = PlayerPrefs.GetString(keyName);
}
```

### Import/Export

**Export format (CSV):**
```
Key,Value
playerName,"John"
highScore,"1000"
```

**Export implementation:**
- Always exports **decrypted** values for readability
- Uses `StreamWriter` with UTF-8 encoding
- Properly escapes commas and quotes in values

**Import implementation:**
- Parses CSV line-by-line
- Automatically encrypts values during import
- Deletes only PmPrefs entries before import (preserves PlayerPrefs)

### Configuration Panel

Allows changing the encryption key:

**Warning system:**
- Shows dialog explaining that changing the key makes existing data unreadable
- Recommends exporting before key change
- Requires confirmation

**Implementation note:**
Changing `SecureKey` requires modifying source code. The Editor window locates `PmPrefs.cs` and performs text replacement.

---

## API Design Patterns

### Generic Save/Load

PmPrefs uses C# generics to provide type-safe loading:

```csharp
public static void Save(string key, object value)
public static T Load<T>(string key, T defaultValue = default)
```

**Design rationale:**
- `Save()` accepts `object` for maximum flexibility
- `Load<T>()` requires explicit type for type safety
- Default value pattern matches `PlayerPrefs` API conventions

### Enum Keys

PmPrefs supports using enums as keys for better maintainability:

```csharp
public enum SaveKey { PlayerName, HighScore }

PmPrefs.Save(SaveKey.PlayerName, "John");
string name = PmPrefs.Load<SaveKey, string>(SaveKey.PlayerName, "Guest");
```

**Implementation:**
```csharp
// PmPrefs.cs:256-260
public static void Save<T>(T key, object value)
{
    Save(key.ToString(), value); // Convert to string
}
```

### Error Handling Strategy

**Encryption failures:**
- Return empty string
- Log warning with `Debug.LogWarning()`
- Continue execution (fail gracefully)

**Decryption failures:**
- Return empty string
- Log warning
- Return default value in `Load<T>()`

**Rationale:**
Non-breaking behavior prevents crashes from corrupted save data. Users can implement their own validation if needed.

### Resource Management

All crypto objects are properly disposed using `using` statements:

```csharp
using (var aes = Aes.Create())
using (var memoryStream = new MemoryStream())
using (var encryptor = aes.CreateEncryptor())
using (var cryptoStream = new CryptoStream(...))
{
    // Encryption operations
} // All resources automatically disposed
```

**Benefits:**
- Prevents memory leaks
- Releases unmanaged crypto resources
- Thread-safe (each operation creates fresh instances)

---

## Summary

**Key architectural decisions:**

1. **Simplicity over configurability**: Static IV and hardcoded constants trade security for ease of use
2. **Transparency**: Encryption is automatic; users don't need to think about it
3. **Compatibility**: Wraps `PlayerPrefs` to work on all Unity platforms
4. **Cross-platform Editor**: Platform-specific code enables full functionality on Windows, macOS, and Linux
5. **Key tracking**: Internal list enables enumeration despite `PlayerPrefs` limitations

**Security posture:**
- ✅ Protects against casual save file editing
- ⚠️ Not secure against determined attackers
- ✅ Appropriate for single-player games and offline data
- ❌ Not suitable for sensitive credentials or competitive anti-cheat

**Performance characteristics:**
- ✅ Derived key is cached (fast repeated operations)
- ✅ Key list is lazy-loaded and cached
- ✅ Editor key reading is cached with 2-second timeout
- ⚠️ PBKDF2 derivation on first use (one-time cost)

---

*For usage examples and public API documentation, see [README.md](README.md)*
*For version history and changes, see [CHANGELOG.md](CHANGELOG.md)*
