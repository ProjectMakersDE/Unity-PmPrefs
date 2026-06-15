# PmPrefs - Unity Editor Extension

[![Unity](https://img.shields.io/badge/Unity-2021.3%2B-blue.svg)](https://unity.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE.md)

A simple and secure way to save and load data in Unity games. PmPrefs wraps Unity's PlayerPrefs with automatic AES encryption and provides a visual editor for managing preferences.

## Features

### Simple Save & Load
Save any serializable .NET object with a single line of code:
```csharp
PmPrefs.Save("playerName", "John");
PmPrefs.Save("settings", mySettingsObject);
```

Load data just as easily (specify the type):
```csharp
string name = PmPrefs.Load<string>("playerName", "DefaultName");
MySettings settings = PmPrefs.Load<MySettings>("settings");
```

### Secure Storage
All data is automatically encrypted using AES-256 encryption. Your saved data cannot be easily read or manipulated by users.

### Visual Editor
Access the editor window via **Tools > ProjectMakers > PmPrefs**:
- View and edit all PmPrefs and PlayerPrefs
- Toggle between encrypted and decrypted view
- Create, modify, and delete preferences
- Copy keys and values to clipboard with one click
- Import/Export to CSV for backup or migration

### Cross-Platform
- Works on Windows, macOS, and Linux in the Editor
- Runtime API works on all Unity-supported platforms
- No special permissions required on target devices

## Installation

### Via Package Manager (Recommended)
1. Open **Window > Package Manager**
2. Click the **+** button and select **Add package from git URL**
3. Enter: `https://github.com/ProjectMakersDE/Unity-PmPrefs.git`
4. Click **Add**

### Manual Installation
1. Download or clone this repository
2. Copy the folder into your project's `Packages` directory

## Usage

### Basic Operations

```csharp
using PM.Plugins;

// Save data
PmPrefs.Save("score", 100);
PmPrefs.Save("player", new PlayerData { Name = "John", Level = 5 });

// Load data
int score = PmPrefs.Load<int>("score", 0);
PlayerData player = PmPrefs.Load<PlayerData>("player");

// Check if key exists
if (PmPrefs.HasKey("score"))
{
    // Key exists
}

// Delete specific key
PmPrefs.DeleteKey("score");

// Delete all PmPrefs (keeps regular PlayerPrefs)
PmPrefs.DeleteAllPmPrefs();

// Force save to disk
PmPrefs.SaveAll();
```

### Manual Encryption
You can also use the encryption methods directly:
```csharp
string encrypted = PmPrefs.Encrypt("sensitive data");
string decrypted = PmPrefs.Decrypt(encrypted);
```

### Using Enums as Keys
```csharp
public enum SaveKey { PlayerName, HighScore, Settings }

PmPrefs.Save(SaveKey.PlayerName, "John");
string name = PmPrefs.Load<SaveKey, string>(SaveKey.PlayerName, "Guest");
```

## Serialization Requirements

PmPrefs uses Unity's `JsonUtility` for serialization, which has specific requirements for the types you can save:

### Supported Types
- **Primitives**: `int`, `float`, `string`, `bool`, etc.
- **Unity types**: `Vector3`, `Color`, `Quaternion`, etc.
- **Custom classes**: Must be marked with `[Serializable]` attribute
- **Arrays**: Single-dimensional arrays of supported types

```csharp
// ✓ These work fine
PmPrefs.Save("score", 100);
PmPrefs.Save("position", new Vector3(1, 2, 3));

[Serializable]
public class PlayerData
{
    public string name;
    public int level;
}
PmPrefs.Save("player", new PlayerData { name = "John", level = 5 });
```

### Unsupported Types (Without Workarounds)
- **Generic collections**: `List<T>`, `Dictionary<K,V>`, `HashSet<T>`
- **Nested generics**: `List<List<T>>`
- **Interfaces and abstract classes**

```csharp
// ✗ These will NOT work directly
List<string> items = new List<string> { "item1", "item2" };
PmPrefs.Save("items", items); // JsonUtility cannot serialize List<T> directly

Dictionary<string, int> scores = new Dictionary<string, int>();
PmPrefs.Save("scores", scores); // JsonUtility cannot serialize Dictionary<K,V>
```

### Workaround: Wrapper Classes
To save generic collections, wrap them in a serializable class:

#### Example 1: List of Strings
```csharp
[Serializable]
public class StringListWrapper
{
    public List<string> items = new List<string>();
}

// Save the list
StringListWrapper wrapper = new StringListWrapper();
wrapper.items = new List<string> { "item1", "item2", "item3" };
PmPrefs.Save("itemList", wrapper);

// Load it back
StringListWrapper loaded = PmPrefs.Load<StringListWrapper>("itemList");
List<string> myItems = loaded.items;
```

#### Example 2: List of Custom Objects
```csharp
[Serializable]
public class PlayerData
{
    public string name;
    public int level;
    public float health;
}

[Serializable]
public class PlayerListWrapper
{
    public List<PlayerData> players = new List<PlayerData>();
}

// Save a list of players
PlayerListWrapper wrapper = new PlayerListWrapper();
wrapper.players.Add(new PlayerData { name = "Alice", level = 10, health = 100f });
wrapper.players.Add(new PlayerData { name = "Bob", level = 15, health = 85f });
PmPrefs.Save("playerList", wrapper);

// Load it back
PlayerListWrapper loaded = PmPrefs.Load<PlayerListWrapper>("playerList");
foreach (var player in loaded.players)
{
    Debug.Log($"{player.name}: Level {player.level}");
}
```

#### Example 3: Dictionary Workaround
Since `JsonUtility` doesn't support dictionaries, convert them to lists of key-value pairs:

```csharp
[Serializable]
public class StringIntPair
{
    public string key;
    public int value;
}

[Serializable]
public class StringIntDictionaryWrapper
{
    public List<StringIntPair> items = new List<StringIntPair>();

    // Helper methods for easy conversion
    public void FromDictionary(Dictionary<string, int> dict)
    {
        items.Clear();
        foreach (var kvp in dict)
        {
            items.Add(new StringIntPair { key = kvp.Key, value = kvp.Value });
        }
    }

    public Dictionary<string, int> ToDictionary()
    {
        Dictionary<string, int> dict = new Dictionary<string, int>();
        foreach (var item in items)
        {
            dict[item.key] = item.value;
        }
        return dict;
    }
}

// Save a dictionary
Dictionary<string, int> highScores = new Dictionary<string, int>
{
    { "Alice", 1000 },
    { "Bob", 850 },
    { "Charlie", 920 }
};

StringIntDictionaryWrapper wrapper = new StringIntDictionaryWrapper();
wrapper.FromDictionary(highScores);
PmPrefs.Save("highScores", wrapper);

// Load it back
StringIntDictionaryWrapper loaded = PmPrefs.Load<StringIntDictionaryWrapper>("highScores");
Dictionary<string, int> scores = loaded.ToDictionary();
```

#### Example 4: Generic Dictionary Wrapper
For reusability, create a generic wrapper pattern:

```csharp
[Serializable]
public class SerializableKeyValuePair<TKey, TValue>
{
    public TKey key;
    public TValue value;

    public SerializableKeyValuePair() { }
    public SerializableKeyValuePair(TKey k, TValue v) { key = k; value = v; }
}

[Serializable]
public class SerializableDictionary<TKey, TValue>
{
    public List<SerializableKeyValuePair<TKey, TValue>> items = new List<SerializableKeyValuePair<TKey, TValue>>();

    public void FromDictionary(Dictionary<TKey, TValue> dict)
    {
        items.Clear();
        foreach (var kvp in dict)
        {
            items.Add(new SerializableKeyValuePair<TKey, TValue>(kvp.Key, kvp.Value));
        }
    }

    public Dictionary<TKey, TValue> ToDictionary()
    {
        Dictionary<TKey, TValue> dict = new Dictionary<TKey, TValue>();
        foreach (var item in items)
        {
            dict[item.key] = item.value;
        }
        return dict;
    }
}

// Usage with any key/value types
Dictionary<string, PlayerData> playerRegistry = new Dictionary<string, PlayerData>();
playerRegistry["player1"] = new PlayerData { name = "Alice", level = 10, health = 100f };

SerializableDictionary<string, PlayerData> wrapper = new SerializableDictionary<string, PlayerData>();
wrapper.FromDictionary(playerRegistry);
PmPrefs.Save("playerRegistry", wrapper);

// Load it back
SerializableDictionary<string, PlayerData> loaded = PmPrefs.Load<SerializableDictionary<string, PlayerData>>("playerRegistry");
Dictionary<string, PlayerData> registry = loaded.ToDictionary();
```

**Note**: PmPrefs internally uses this wrapper pattern for managing its own data (see `StringListWrapper` in `PmPrefs.cs`).

### Learn More

For complete details about Unity's JsonUtility serialization system, see the [official Unity documentation](https://docs.unity3d.com/ScriptReference/JsonUtility.html).

## Security Note

By default PmPrefs uses a built-in fallback key. The active key is read at runtime from a
`PmPrefsKeyAsset` config asset placed in any `Resources` folder; if none is configured, the
built-in `DefaultSecureKey` is used. Each value is encrypted with AES-256-CBC using a **random IV
per value**, so identical values do not produce identical ciphertext.

For production use:

1. **Change the default key**: Open **Tools > ProjectMakers > PmPrefs > Configuration > Secure Key**
   and set a project-specific key. This writes the key into a config asset under
   `Assets/PmPrefs/Resources/` (no source edit and no recompile required, and it works for
   read-only Package Manager / git-URL installs).
2. **Use a unique key per project**: Don't ship released games with the default key.
3. **Understand the limitations**: The key is still derivable from the build. This protects against
   casual inspection and tampering, **not** determined attackers with access to your compiled code.
   Do not use PmPrefs for passwords, tokens, or other sensitive data (see the FAQ).

## Editor Window

Open via **Tools > ProjectMakers > PmPrefs**

| Button | Function |
|--------|----------|
| Create | Add a new preference |
| Configuration | Change encryption key, import/export |
| Delete All | Remove all preferences |
| Save | Save pending changes |
| Shield icon | Toggle encrypted/decrypted view |
| Refresh | Reload preferences from storage |
| PmPrefs/PlayerPrefs | Switch between preference lists |
| Copy buttons | Copy preference key or value to clipboard (per item) |

## Troubleshooting

### Encryption Key Mismatch

**Problem:** Data saved with one encryption key cannot be decrypted with a different key. If you change the encryption key after saving data, you'll lose access to previously saved preferences.

**Symptoms:**
- `Load()` returns default values instead of saved data
- Error messages about decryption failures in the console
- Editor window shows garbled or empty data

**Solutions:**

1. **Export before changing the key:**
   - Open **Tools > ProjectMakers > PmPrefs**
   - Click **Configuration > Export to CSV**
   - Save your data to a CSV file
   - Change the encryption key
   - Click **Configuration > Import from CSV** to restore your data

2. **Temporarily revert the key:**
   - If you changed the key and forgot to export, set the old key again in the `PmPrefsKeyAsset`
     config asset (or via Configuration > Secure Key)
   - Export your data to CSV or JSON
   - Change back to the new key
   - Import the file

3. **Clear and start fresh:**
   - If the old data isn't critical, use `PmPrefs.DeleteAllPmPrefs()` to clear all encrypted preferences
   - Or use the **Delete All** button in the editor window

**Prevention:**
- Always export your data before changing the encryption key
- Document your encryption key changes in version control
- Consider using different keys for development and production builds

### Data Not Persisting

**Problem:** Changes are not saved between sessions.

**Solution:** Call `PmPrefs.SaveAll()` to force an immediate write to disk. By default, Unity saves PlayerPrefs when the application quits, but explicit saves ensure data persistence.

### Editor Window Not Showing Keys

**Problem:** The PmPrefs editor window appears empty or doesn't display saved preferences, even though data exists in PlayerPrefs.

**Symptoms:**
- Editor window shows no entries or appears blank
- Keys saved via code don't appear in the window
- Window content doesn't update after saving new preferences
- Switching between PmPrefs and PlayerPrefs tabs shows nothing

**Solutions:**

1. **Click the Refresh button:**
   - Open **Tools > ProjectMakers > PmPrefs**
   - Click the **Refresh** button (circular arrow icon) in the toolbar
   - This reloads all preferences from storage

2. **Close and reopen the window:**
   - Close the PmPrefs editor window
   - Reopen it via **Tools > ProjectMakers > PmPrefs**
   - The window reloads all data on initialization

3. **Check if you're on the correct tab:**
   - Make sure you're viewing the **PmPrefs** tab, not the **PlayerPrefs** tab
   - PmPrefs keys are prefixed with `_PM_` in PlayerPrefs storage but appear without the prefix in the PmPrefs tab
   - Regular PlayerPrefs keys won't show in the PmPrefs tab and vice versa

4. **Verify data exists:**
   ```csharp
   // Check if key exists in code
   if (PmPrefs.HasKey("myKey"))
   {
       Debug.Log("Key exists in PmPrefs");
   }

   // Check PlayerPrefs directly
   if (PlayerPrefs.HasKey("_PM_myKey"))
   {
       Debug.Log("Key exists in PlayerPrefs storage");
   }
   ```

5. **Force a save and refresh:**
   ```csharp
   // In your code, force save
   PmPrefs.SaveAll();

   // Then refresh the editor window
   ```

6. **Check for Unity editor focus:**
   - Sometimes Unity needs to regain focus for the window to update
   - Click on the Unity editor window if it's in the background
   - Try clicking the Refresh button after regaining focus

**Prevention:**
- Always click **Refresh** after making changes via code while the editor window is open
- Use the **Save** button in the editor after making changes in the window
- Close and reopen the window if it becomes unresponsive

### Serialization Errors

**Problem:** PmPrefs uses Unity's `JsonUtility` for serialization, which has limitations on what types can be serialized. Attempting to save unsupported types will result in errors or data loss.

**Symptoms:**
- Console errors like "ArgumentException: JSON serialization error"
- Data loads as default values instead of saved values
- Empty or partial data when loading complex objects
- No error but data is missing fields

**Common Limitations:**

1. **Dictionaries are not supported:**
   ```csharp
   // ❌ This will NOT work
   Dictionary<string, int> scores = new Dictionary<string, int>();
   PmPrefs.Save("scores", scores);

   // ✅ Use a serializable wrapper class instead
   [System.Serializable]
   public class ScoreData
   {
       public string[] keys;
       public int[] values;
   }
   ```

2. **Lists of primitive types need wrapping:**
   ```csharp
   // ❌ This may not work correctly
   List<string> items = new List<string> { "apple", "banana" };
   PmPrefs.Save("items", items);

   // ✅ Use an array or a wrapper class
   string[] items = new string[] { "apple", "banana" };
   PmPrefs.Save("items", items);

   // Or wrap in a serializable class
   [System.Serializable]
   public class ItemList
   {
       public List<string> items;
   }
   ```

3. **Circular references are not allowed:**
   ```csharp
   // ❌ This will cause infinite recursion
   [System.Serializable]
   public class Node
   {
       public Node parent;  // Circular reference
       public Node child;
   }

   // ✅ Break the circular reference or use IDs
   [System.Serializable]
   public class Node
   {
       public int parentId;  // Reference by ID instead
       public int childId;
   }
   ```

**Solutions:**

1. **Use JsonUtility-compatible types:**
   - Primitive types: `int`, `float`, `string`, `bool`
   - Unity types: `Vector3`, `Color`, `Quaternion`
   - Arrays of serializable types
   - Classes marked with `[System.Serializable]`

2. **Create wrapper classes for complex data:**
   ```csharp
   [System.Serializable]
   public class SerializableData
   {
       public string[] keys;
       public int[] values;

       public Dictionary<string, int> ToDictionary()
       {
           var dict = new Dictionary<string, int>();
           for (int i = 0; i < keys.Length; i++)
               dict[keys[i]] = values[i];
           return dict;
       }

       public static SerializableData FromDictionary(Dictionary<string, int> dict)
       {
           return new SerializableData
           {
               keys = dict.Keys.ToArray(),
               values = dict.Values.ToArray()
           };
       }
   }
   ```

3. **Use alternative serialization:**
   - For complex data structures, consider using a different serialization library (JSON.NET, MessagePack, etc.)
   - Manually serialize to a supported format before saving with PmPrefs

**Prevention:**
- Always mark classes with `[System.Serializable]`
- Test serialization with small data sets first
- Check Unity console for serialization warnings
- Use simple, flat data structures when possible

## FAQ

### Where is PmPrefs data stored on different platforms?

PmPrefs uses Unity's PlayerPrefs as its underlying storage mechanism, which stores data in different locations depending on the platform:

**Windows (Editor and Standalone):**
- Location: Windows Registry
- Path: `HKCU\Software\[CompanyName]\[ProductName]`
- Keys are stored as registry values under this path
- Access via Registry Editor (`regedit.exe`)

**macOS (Editor and Standalone):**
- Location: plist file
- Path: `~/Library/Preferences/unity.[CompanyName].[ProductName].plist`
- Keys are stored in XML format
- Access via Xcode or `defaults` command in Terminal

**Linux (Editor and Standalone):**
- Location: Configuration file
- Path: `~/.config/unity3d/[CompanyName]/[ProductName]/prefs`
- Keys are stored in a plain text file
- Access via any text editor

**iOS:**
- Location: `NSUserDefaults` system
- Path: `/Library/Preferences/[BundleIdentifier].plist`
- Stored within the app's sandbox
- Automatically backed up to iCloud (if enabled)

**Android:**
- Location: SharedPreferences XML file
- Path: `/data/data/[PackageName]/shared_prefs/[PackageName].xml`
- Requires root access to view directly on device
- Can be extracted using Android Debug Bridge (adb)

**WebGL:**
- Location: Browser's IndexedDB
- Stored in the browser's local storage
- Persists until browser cache is cleared
- Size limitations vary by browser

**Note:** `[CompanyName]` and `[ProductName]` are set in Unity's **Edit > Project Settings > Player** settings.

### Can I migrate PmPrefs data between platforms?

**Short Answer:** Yes, but it requires manual export and import.

**Cross-Platform Migration Steps:**

1. **Export from the source platform:**
   - Open the Unity Editor on the source platform
   - Go to **Tools > ProjectMakers > PmPrefs**
   - Click **Configuration > Export to CSV**
   - Save the CSV file to a shared location (cloud storage, USB drive, etc.)

2. **Import on the target platform:**
   - Open the Unity Editor on the target platform
   - Go to **Tools > ProjectMakers > PmPrefs**
   - Click **Configuration > Import from CSV**
   - Select the exported CSV file
   - All preferences will be restored on the new platform

**Important Notes:**

- **Encryption key must match:** The same encryption key must be used on both platforms. If the keys differ, data will not decrypt correctly.
- **Editor-only feature:** CSV export/import is only available in the Unity Editor, not at runtime.
- **Runtime migration:** For runtime cross-platform migration (e.g., cloud saves), you'll need to implement your own sync system using the CSV export format or by accessing PlayerPrefs data directly.

### Can players migrate their save data between devices?

**By Default:** No. PlayerPrefs (and therefore PmPrefs) data is stored locally on each device and is not automatically synchronized.

**Implementation Options:**

1. **Manual Export/Import (Editor only):**
   - Use the built-in CSV export/import feature
   - Only works in the Unity Editor, not useful for end users

2. **Cloud Save Implementation:**
   - Read all PmPrefs data in your game code
   - Upload to a cloud service (Firebase, PlayFab, custom backend)
   - Download and restore on other devices
   - Example:
   ```csharp
   // Pseudo-code for cloud save
   public void UploadSaveData()
   {
       // Collect all game data
       var saveData = new SaveData {
           playerName = PmPrefs.Load<string>("playerName"),
           score = PmPrefs.Load<int>("score"),
           settings = PmPrefs.Load<Settings>("settings")
       };

       // Upload to cloud (implement your own cloud service)
       CloudService.Upload(saveData);
   }

   public void DownloadSaveData()
   {
       // Download from cloud
       var saveData = CloudService.Download<SaveData>();

       // Restore to PmPrefs
       PmPrefs.Save("playerName", saveData.playerName);
       PmPrefs.Save("score", saveData.score);
       PmPrefs.Save("settings", saveData.settings);
       PmPrefs.SaveAll();
   }
   ```

3. **Platform-Specific Solutions:**
   - **iOS/macOS:** Use iCloud with native plugins
   - **Android:** Use Google Play Games Services
   - **Steam:** Use Steam Cloud
   - **Console:** Use platform-specific save systems

4. **Manual File Transfer:**
   - Access the PlayerPrefs storage location (see "Where is PmPrefs data stored?" above)
   - Copy the file/registry data to the target device
   - Requires technical knowledge and may not be user-friendly

**Recommendation:** For production games that need cross-device saves, implement a cloud save system that stores your game data on a server, rather than relying on PlayerPrefs migration.

### Does PmPrefs work the same way on all platforms?

**Core Functionality:** Yes. The PmPrefs API works identically on all Unity-supported platforms.

**Platform Differences:**

1. **Storage Location:**
   - Each platform uses a different underlying storage mechanism (registry, plist, file, etc.)
   - Storage paths differ (see "Where is PmPrefs data stored?" above)

2. **Editor Window:**
   - Only available in the Unity Editor (Windows, macOS, Linux)
   - Not accessible in builds or at runtime

3. **Performance:**
   - Registry access (Windows) may be slightly slower than file-based storage
   - Browser-based storage (WebGL) has size limitations
   - Mobile platforms may have additional security restrictions

4. **Data Persistence:**
   - Desktop: Data persists indefinitely unless manually deleted
   - Mobile: Data persists unless the app is uninstalled or cache is cleared
   - WebGL: Data persists until browser cache is cleared
   - Console: May be tied to user profiles

5. **Data Access:**
   - Desktop: Users can easily access and modify PlayerPrefs data
   - Mobile: Requires root/jailbreak to access directly
   - WebGL: Can be inspected via browser developer tools

**Best Practice:** Always use the same encryption key across all platforms to ensure data compatibility if you ever need to migrate preferences.

### Is PmPrefs secure enough for sensitive data like passwords or payment information?

**Short Answer:** No. PmPrefs is NOT designed for highly sensitive data like passwords, credit card numbers, or authentication tokens.

**Security Limitations:**

1. **Encryption key is in source code:**
   - The AES encryption key is stored in `PmPrefs.cs`
   - Anyone with access to your compiled game can potentially extract this key through decompilation or reverse engineering
   - This is true even if you obfuscate your code

2. **Protection Level:**
   - ✅ **Good for:** Protecting game progress, settings, and scores from casual inspection and modification
   - ✅ **Good for:** Preventing average players from easily editing save files
   - ❌ **NOT good for:** Protecting sensitive personal information
   - ❌ **NOT good for:** Security against determined attackers
   - ❌ **NOT good for:** Compliance with data protection regulations (GDPR, CCPA, etc.)

3. **Platform-specific concerns:**
   - On some platforms (desktop, rooted/jailbroken devices), players can access PlayerPrefs storage directly
   - WebGL data can be inspected using browser developer tools
   - Mobile apps can be decompiled to extract encryption keys

**What to Do Instead:**

1. **For passwords and authentication:**
   - Never store passwords locally
   - Use token-based authentication with expiration
   - Store tokens in platform-specific secure storage:
     - iOS: Keychain
     - Android: Android Keystore
     - Use plugins like [Unity Keychain Plugin](https://github.com/example/keychain)

2. **For payment information:**
   - Never store payment details locally
   - Always use secure payment gateways (Stripe, PayPal, etc.)
   - Let the payment provider handle sensitive data

3. **For user data subject to privacy laws:**
   - Use proper encryption with keys stored securely
   - Consider server-side storage with proper authentication
   - Implement proper data retention and deletion policies

**When to Use PmPrefs:**
- Game settings (audio, graphics, controls)
- Player progress and achievements
- High scores and statistics
- Non-sensitive user preferences
- Cached data that's not critical if compromised

**Bottom Line:** PmPrefs provides "security through obscurity" which is suitable for game data, but not for protecting truly sensitive information.

### What's the performance impact of using PmPrefs?

**Short Answer:** PmPrefs has minimal performance impact for typical game data, but encryption/decryption does add overhead compared to plain PlayerPrefs.

**Performance Characteristics:**

1. **Encryption/Decryption Overhead:**
   - AES encryption is fast but not free
   - Small data (strings, ints): ~0.1-0.5ms per operation
   - Large objects (complex classes): ~1-10ms depending on size
   - Negligible for most use cases (saving once per game session, loading at startup)

2. **Serialization Cost:**
   - Uses `JsonUtility.ToJson()` and `JsonUtility.FromJson()`
   - Cost scales with object complexity
   - Simple objects: <1ms
   - Complex nested objects: 1-10ms
   - Very large arrays: 10-50ms+

3. **PlayerPrefs I/O:**
   - Underlying PlayerPrefs access speed varies by platform:
     - Windows (Registry): 1-5ms per access
     - macOS/Linux (File): <1ms per access
     - Mobile: 1-10ms depending on storage speed
     - WebGL: Varies by browser, typically 1-5ms

**Benchmarks (Approximate):**

```csharp
// Simple int save/load
PmPrefs.Save("score", 100);              // ~0.2ms
int score = PmPrefs.Load<int>("score"); // ~0.2ms

// Complex object save/load
PmPrefs.Save("player", playerData);      // ~2-5ms
var data = PmPrefs.Load<PlayerData>();   // ~2-5ms

// Large array (1000 items)
PmPrefs.Save("items", largeArray);       // ~10-20ms
var items = PmPrefs.Load<Item[]>();      // ~10-20ms
```

**Performance Best Practices:**

1. **Batch your saves:**
   ```csharp
   // ❌ BAD: Save after every change
   void OnScoreChange(int newScore)
   {
       PmPrefs.Save("score", newScore);
       PmPrefs.SaveAll(); // Triggers disk write!
   }

   // ✅ GOOD: Save periodically or at checkpoints
   void OnLevelComplete()
   {
       PmPrefs.Save("score", currentScore);
       PmPrefs.Save("level", currentLevel);
       PmPrefs.Save("stats", playerStats);
       PmPrefs.SaveAll(); // One disk write
   }
   ```

2. **Load once, cache in memory:**
   ```csharp
   // ✅ Load at startup, use cached values during gameplay
   public class GameManager
   {
       private PlayerData cachedData;

       void Start()
       {
           cachedData = PmPrefs.Load<PlayerData>("player");
       }

       void UpdateScore(int points)
       {
           cachedData.score += points; // Use cached data
       }

       void OnQuit()
       {
           PmPrefs.Save("player", cachedData); // Save once
           PmPrefs.SaveAll();
       }
   }
   ```

3. **Avoid saving large data frequently:**
   - Don't save multi-megabyte objects every frame
   - Consider breaking large data into smaller chunks
   - Save only changed data when possible

4. **Use appropriate save timing:**
   - ✅ On game pause/quit
   - ✅ At checkpoints or level completion
   - ✅ After important player actions
   - ❌ Every frame
   - ❌ On every small UI change

**When Performance Matters:**

- **Not a concern:** Menu screens, settings changes, one-time saves at game start/end
- **Minor concern:** Saving during gameplay (2-10ms is acceptable in most games)
- **Major concern:** Real-time multiplayer, VR games with strict frame budgets, saving very large data sets (>1MB)

**Alternative for Large/Frequent Saves:**
- For very large data or high-frequency saves, consider using file-based storage directly
- Use `System.IO.File` with your own encryption for better control
- PmPrefs is optimized for small-to-medium data that's saved infrequently

### When should I use PmPrefs vs regular PlayerPrefs or other storage solutions?

**Use PmPrefs When:**

✅ **You need basic encryption for game data**
- Protecting high scores from casual cheating
- Preventing players from easily editing save files
- Storing game progress that shouldn't be trivially modified
- Saving settings that you don't want users to tamper with

✅ **You want a simple API**
- You like the ease of saving/loading any serializable object
- You don't want to write your own serialization code
- You prefer type-safe loading with generics

✅ **You want a visual editor**
- The PmPrefs editor window is useful for debugging
- You need to inspect or modify preferences during development
- You want export/import functionality for testing

✅ **Your data is small to medium sized**
- Settings, preferences, flags: <1KB
- Player profiles, inventories: 1-100KB
- Total storage needs: <10MB

**Use Regular PlayerPrefs When:**

✅ **Performance is critical**
- You're saving very frequently (every frame)
- You need the absolute fastest save/load times
- Your game has tight performance budgets

✅ **You only store simple types**
- Just ints, floats, and strings
- No complex objects or serialization needed

✅ **Security doesn't matter**
- Your game is entirely offline with no competitive elements
- You don't care if players edit their save files
- Local data has no value to protect

✅ **You need maximum compatibility**
- Working with legacy code that uses PlayerPrefs
- Integrating with third-party plugins that expect PlayerPrefs
- Maximum Unity version compatibility is required

**Use File-Based Storage When:**

✅ **You have large amounts of data**
- Saving worlds, procedural content: 10MB+
- Large inventories with thousands of items
- Extensive player-generated content

✅ **You need fine-grained control**
- Custom serialization formats (binary, Protocol Buffers, MessagePack)
- Incremental saves (saving only changed data)
- Streaming data in/out as needed
- Custom encryption with externally managed keys

✅ **You need multiple save slots**
- Player can have multiple profiles
- Save/load system with named slots
- Backup/autosave functionality

```csharp
// Example: File-based save system
string path = Application.persistentDataPath + "/savegame.dat";
File.WriteAllText(path, JsonUtility.ToJson(gameData));
```

**Use Cloud/Database Storage When:**

✅ **You need cross-device synchronization**
- Players can continue on different devices
- Cloud saves are a core feature
- Backup and restore functionality

✅ **You have server-side logic**
- Validating game state server-side
- Preventing cheating in multiplayer
- Analytics and player behavior tracking

✅ **You need collaboration**
- Multiple players affecting shared data
- Social features requiring online connectivity
- Leaderboards and global stats

**Use Platform-Specific Secure Storage When:**

✅ **You're storing sensitive data**
- Authentication tokens
- User credentials (though passwords should never be stored locally)
- Personally identifiable information
- Payment-related data

```csharp
// Example: Use platform-specific secure storage for tokens
// iOS Keychain, Android Keystore, etc.
SecureStorage.SetString("authToken", token); // Via plugin
```

**Decision Matrix:**

| Requirement | Solution | Reason |
|-------------|----------|--------|
| Encrypted game progress | PmPrefs | Built-in encryption, easy API |
| High scores, achievements | PmPrefs | Protection from casual cheating |
| Audio/graphics settings | PlayerPrefs or PmPrefs | Either works, PmPrefs if you want consistency |
| Large save files (>10MB) | File-based | Better performance for large data |
| Multiple save slots | File-based | Easier to manage multiple files |
| Cross-device saves | Cloud storage | Required for sync functionality |
| Authentication tokens | Platform secure storage | Proper security for sensitive data |
| Offline-only casual game | PlayerPrefs | Simplest option if security isn't needed |
| Competitive multiplayer | Server-side storage | Prevent cheating, validate on server |

**Can I Use Multiple Solutions?**

Yes! Many games use a hybrid approach:
```csharp
// PmPrefs for game data
PmPrefs.Save("playerProgress", progress);

// Secure platform storage for tokens
SecureStorage.SetString("authToken", token);

// File storage for large data
File.WriteAllBytes(worldPath, worldData);

// Cloud for cross-device sync
CloudSave.Upload("playerProgress", progress);
```

**Bottom Line:** Use PmPrefs when you want simple encrypted storage for game data that's not highly sensitive. For other use cases, choose the appropriate storage mechanism based on your specific requirements.

## Requirements

- Unity 2021.3 (LTS) or later (tested up to Unity 6000) — the editor window uses UI Toolkit APIs (e.g. `ListView` dynamic-height virtualization) introduced in Unity 2021.2
- .NET Standard 2.0 or later

## Support

Having issues or suggestions? [Open an issue](https://github.com/ProjectMakersDE/Unity-PmPrefs/issues) on GitHub.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

---

*Made by [ProjectMakers](https://projectmakers.de)*
