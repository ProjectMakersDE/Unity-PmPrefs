# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Guidelines

**IMPORTANT**: Follow these rules when working in this repository:

1. **No Automatic Commits**: Do NOT create git commits by yourself. Always ask the user before committing changes.

2. **No AI References**: NEVER mention "claude", "auto-claude", or any AI assistant references in:
   - Commit messages
   - Pull request titles or descriptions
   - Code comments
   - Documentation

3. **Helper Files Management**:
   - Place any helper scripts or temporary files in a separate folder
   - Clean up helper files when they are no longer needed
   - Do not leave unused helper files in the repository

## Project Overview

PmPrefs is a Unity Editor Extension that provides encrypted PlayerPrefs storage with AES-256 encryption. It's distributed as a Unity Package Manager (UPM) package.

**Package Name**: `com.projectmakers.pmprefs`
**Namespace**: `PM.Plugins`
**Minimum Unity Version**: 2018.1+

## Architecture

### Core Components

1. **PmPrefs.cs** (`Scripts/PmPrefs.cs`)
   - Main API for saving/loading encrypted preferences
   - Uses AES encryption with configurable secure key (default: `LoKo1Nibu75XXzu`)
   - All keys are prefixed with `PmPrefs__` to distinguish from regular PlayerPrefs
   - Maintains an internal key list stored in PlayerPrefs as `PmPrefs__KeyList`
   - Serializes all data to JSON before encryption using Unity's `JsonUtility`

2. **PmPrefsEditorWindow.cs** (`Editor/Code/PmPrefsEditorWindow.cs`)
   - Editor window accessible via **Tools > ProjectMakers > PmPrefs**
   - Provides visual management of both PmPrefs and regular PlayerPrefs
   - Features: create/edit/delete preferences, toggle encrypted/decrypted view, import/export CSV
   - Can dynamically change the encryption key (invalidates existing data)
   - Uses UIToolkit (UXML/USS) for UI

3. **PrefsKeyReader.cs** (`Editor/Code/PrefsKeyReader.cs`)
   - Cross-platform reader for PlayerPrefs keys
   - **Windows**: Reads from Registry at `HKCU\Software\Unity\UnityEditor\{CompanyName}\{ProductName}`
   - **macOS**: Reads plist file at `~/Library/Preferences/unity.{companyname}.{productname}.plist`
   - **Linux**: Reads XML file at `~/.config/unity3d/{CompanyName}/{ProductName}/prefs`
   - Implements 2-second caching to reduce file system/registry access
   - Falls back to PmPrefs tracked key list if platform-specific reading fails

4. **PmPrefsListItem.cs** (`Scripts/PmPrefsListItem.cs`)
   - Data model for preference entries in the editor
   - Tracks modification state and delete markers

### Assembly Definitions

- `projectmakers.pmprefs.core` (Scripts): Runtime assembly for the core PmPrefs API
- `projectmakers.PmPrefs.editor` (Editor): Editor-only assembly that references core

### Encryption Details

- **Algorithm**: AES-256 with CBC mode and PKCS7 padding
- **Key Derivation**: Uses `Rfc2898DeriveBytes` with a hardcoded salt (`SaltKey`) and initialization vector (`ViKey`)
- **Secure Key**: Stored as a public constant in `PmPrefs.cs` - users should change this for production
- Encrypted values are stored as Base64-encoded strings in PlayerPrefs

## Common Development Tasks

### Testing the Package

This is a Unity package, so testing requires a Unity project:
```bash
# Open in Unity Editor (no CLI test runner available)
# Use Tools > ProjectMakers > PmPrefs to test editor functionality
```

### Building/Validation

Unity packages don't have a traditional build step. The package is validated by:
1. Ensuring `package.json` has correct metadata
2. Testing in Unity Editor
3. Verifying assembly definitions compile correctly

### Modifying the Encryption Key Location

If changing where `SecureKey` is defined in `PmPrefs.cs`, update the regex pattern in `PmPrefsEditorWindow.cs:583` that searches for:
```csharp
lines[i].Contains("public const string SecureKey =")
```

### Working with Platform-Specific Code

The codebase uses conditional compilation for platform-specific PlayerPrefs reading:
- `#if UNITY_EDITOR_WIN` - Windows Registry access
- `#if UNITY_EDITOR_OSX` - macOS plist parsing
- `#if UNITY_EDITOR_LINUX` - Linux XML prefs parsing

When modifying `PrefsKeyReader.cs`, ensure changes work across all three platforms or add appropriate fallback behavior.

### UI Modifications

Editor UI is defined in UXML files:
- `Editor/Style/PmPrefs.uxml` - Main window layout
- `Editor/Style/PmPrefsListItem.uxml` - List item template
- `Editor/Style/PmPrefs.uss` - Styling

The code loads these from either:
1. `Packages/com.projectmakers.pmprefs/Editor/Style/` (installed package)
2. `Assets/PmPrefs/Editor/Style/` (development fallback)

## Important Constraints

1. **JsonUtility Limitations**: Unity's JsonUtility doesn't support generic collections directly - List<string> requires a wrapper class (see `StringListWrapper` in PmPrefs.cs:28)
2. **PlayerPrefs Threading**: PlayerPrefs is not thread-safe - all operations must be on main thread
3. **Encryption Key Changes**: Changing the secure key invalidates ALL existing encrypted data
4. **Package Structure**: Must maintain UPM package structure with package.json at root
5. **Registry Key Suffixes (Windows)**: Registry keys have hash suffixes like `_h12345` that must be stripped when reading (PrefsKeyReader.cs:138)

## Key Files Reference

- `Scripts/PmPrefs.cs:44` - SecureKey constant definition
- `Scripts/PmPrefs.cs:51` - Key prefix constant (`PmPrefs__`)
- `Editor/Code/PmPrefsEditorWindow.cs:79` - Menu item registration
- `Editor/Code/PrefsKeyReader.cs:93` - Platform-specific key reading logic
- `package.json` - Package metadata and Unity version requirements
