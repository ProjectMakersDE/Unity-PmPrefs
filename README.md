# PmPrefs - Unity Editor Extension

[![Unity](https://img.shields.io/badge/Unity-2018.1%2B-blue.svg)](https://unity.com/)
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

## Security Note

The encryption key is stored in the source code (`PmPrefs.cs`). For production use:

1. **Change the default key**: Use the Configuration panel in the editor window, or manually edit `SecureKey` in `PmPrefs.cs`
2. **Use a unique key per project**: Don't use the default key in released games
3. **Understand the limitations**: This protects against casual inspection, not determined attackers with access to your compiled code

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

## Requirements

- Unity 2018.1 or later (tested up to Unity 6000)
- .NET Standard 2.0 or later

## Support

Having issues or suggestions? [Open an issue](https://github.com/ProjectMakersDE/Unity-PmPrefs/issues) on GitHub.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

---

*Made by [ProjectMakers](https://projectmakers.de)*
