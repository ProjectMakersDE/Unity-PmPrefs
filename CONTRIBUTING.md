# Contributing to PmPrefs

[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](https://github.com/ProjectMakersDE/Unity-PmPrefs/pulls)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE.md)

Thank you for your interest in contributing to PmPrefs! This document provides guidelines and instructions for contributing to the project.

PmPrefs is a Unity Editor extension that provides secure, encrypted data storage for Unity games. For project overview and features, see the [README.md](README.md).

We welcome contributions of all kinds:
- 🐛 Bug reports and fixes
- ✨ New features and enhancements
- 📝 Documentation improvements
- 🧪 Tests and quality improvements
- 💡 Ideas and suggestions

## Table of Contents

- [Development Environment Setup](#development-environment-setup)
  - [Prerequisites](#prerequisites)
  - [Installation Methods](#installation-methods)
  - [Verifying Your Setup](#verifying-your-setup)
- [Project Structure](#project-structure)
  - [Directory Organization](#directory-organization)
  - [Assembly Definitions](#assembly-definitions)
  - [UIElements Architecture](#uielements-architecture)
- [Code Style and Conventions](#code-style-and-conventions)
  - [Naming Conventions](#naming-conventions)
  - [Documentation Requirements](#documentation-requirements)
  - [Namespace Usage](#namespace-usage)
  - [Editor UI Guidelines](#editor-ui-guidelines)
- [Testing and Quality Assurance](#testing-and-quality-assurance)
  - [Manual Testing Procedures](#manual-testing-procedures)
  - [Quality Checklist](#quality-checklist)
  - [Testing Across Unity Versions](#testing-across-unity-versions)
- [Pull Request Process](#pull-request-process)
  - [Before Submitting](#before-submitting)
  - [Commit Message Guidelines](#commit-message-guidelines)
  - [Updating the Changelog](#updating-the-changelog)
  - [Review Process](#review-process)
- [Community Guidelines](#community-guidelines)
  - [Code of Conduct](#code-of-conduct)
  - [Getting Help](#getting-help)

---

## Development Environment Setup

### Prerequisites

Before you begin, ensure you have:

- **Unity Editor**: Version 2018.1 or later
  - Recommended: Unity 2020.3 LTS or newer for best compatibility
  - Tested up to Unity 6000
- **Git**: For cloning the repository and version control
- **Code Editor**: Visual Studio, VS Code, or Rider (with Unity support)
- **.NET Standard 2.0**: Automatically included with Unity 2018.1+

### Installation Methods

#### Method 1: Fork and Clone (Recommended for Contributors)

1. **Fork the repository** on GitHub
   - Visit [https://github.com/ProjectMakersDE/Unity-PmPrefs](https://github.com/ProjectMakersDE/Unity-PmPrefs)
   - Click the **Fork** button in the top-right corner

2. **Clone your fork** to your local machine:
   ```bash
   git clone https://github.com/YOUR-USERNAME/Unity-PmPrefs.git
   cd Unity-PmPrefs
   ```

3. **Add the upstream remote** (to sync with the main repository):
   ```bash
   git remote add upstream https://github.com/ProjectMakersDE/Unity-PmPrefs.git
   ```

#### Method 2: Direct Clone (For Testing)

If you just want to test or explore the code:

```bash
git clone https://github.com/ProjectMakersDE/Unity-PmPrefs.git
cd Unity-PmPrefs
```

### Setting Up in Unity

1. **Open Unity Hub** and create a new project or use an existing one
   - Template: Any (3D, 2D, or URP/HDRP)
   - Unity Version: 2018.1 or later

2. **Add the package to your project**:

   **Option A: Via Package Manager (Recommended)**
   - Open Unity Editor
   - Go to **Window > Package Manager**
   - Click **+** button → **Add package from disk**
   - Navigate to the cloned repository
   - Select `package.json` from the root directory
   - Click **Open**

   **Option B: Manual Copy**
   - Copy the entire `Unity-PmPrefs` folder
   - Paste it into your project's `Packages` directory
   - Unity will automatically detect and import the package

3. **Wait for Unity to compile** the package
   - Check the Console for any compilation errors
   - The package should compile without errors

### Verifying Your Setup

After installation, verify everything is working:

1. **Check Package Manager**:
   - Open **Window > Package Manager**
   - Look for **PmPrefs** in the package list
   - Verify the version matches the one in `package.json`

2. **Open the PmPrefs Editor**:
   - Go to **Tools > ProjectMakers > PmPrefs**
   - The editor window should open without errors
   - You should see an empty preferences list (or any existing PlayerPrefs)

3. **Test the API** in a simple script:
   ```csharp
   using UnityEngine;
   using PM.Plugins;

   public class PmPrefsTest : MonoBehaviour
   {
       void Start()
       {
           // Save a test value
           PmPrefs.Save("test_key", "Hello PmPrefs!");

           // Load it back
           string value = PmPrefs.Load<string>("test_key", "");
           Debug.Log($"Loaded value: {value}");

           // Clean up
           PmPrefs.DeleteKey("test_key");
       }
   }
   ```
   - Attach this script to a GameObject
   - Enter Play Mode
   - Check the Console for "Loaded value: Hello PmPrefs!"

4. **Verify UIElements are loading** (Unity 2019.1+):
   - Open the PmPrefs Editor window
   - The interface should display properly
   - Try creating a new preference using the **Create** button
   - The UI should be responsive and styled correctly

### Common Setup Issues

| Issue | Solution |
|-------|----------|
| "Package not found" | Ensure `package.json` is in the repository root |
| Compilation errors | Check Unity version is 2018.1 or later |
| Editor window blank | Verify UIElements are supported (Unity 2019.1+) |
| API not found | Ensure namespace `using PM.Plugins;` is included |
| Changes not reflected | Click **Assets > Refresh** or restart Unity |

### Staying Updated

To keep your fork synchronized with the main repository:

```bash
# Fetch latest changes from upstream
git fetch upstream

# Merge them into your local branch
git checkout main
git merge upstream/main

# Push updates to your fork
git push origin main
```

## Project Structure

Understanding the project organization will help you navigate the codebase and contribute effectively.

### Directory Organization

PmPrefs follows Unity's package structure conventions with a clear separation between runtime and editor code:

```
Unity-PmPrefs/
├── Scripts/                          # Runtime code (available in builds)
│   ├── PmPrefs.cs                   # Core API for encrypted preferences
│   └── projectmakers.pmprefs.core.asmdef
├── Editor/                           # Editor-only code (not included in builds)
│   ├── Code/                        # C# editor scripts
│   │   ├── PmPrefsEditorWindow.cs  # Main editor window
│   │   ├── PrefsKeyReader.cs       # Platform-specific prefs reader
│   │   ├── PmPrefsListItem.cs      # List item data model
│   │   └── projectmakers.pmprefs.editor.asmdef
│   └── Style/                       # UIElements visual assets
│       ├── PmPrefs.uxml             # Main window layout
│       ├── PmPrefs.uss              # Stylesheet for main window
│       └── PmPrefsListItem.uxml     # List item template
├── Tests/                            # Unit and integration tests
├── Documentation~/                   # Package documentation
└── package.json                      # Package manifest
```

#### Scripts/ Directory

Contains the **runtime code** that users will interact with in their game code:

- **Purpose**: Core functionality available in both Editor and builds
- **Key Files**:
  - `PmPrefs.cs`: Main static API for saving, loading, and managing encrypted preferences
- **Usage**: This code is compiled into builds and should have minimal dependencies
- **Best Practice**: Keep runtime code lean and avoid editor-only Unity APIs

**Example from Scripts/PmPrefs.cs:**
```csharp
namespace PM.Plugins
{
    /// <summary>
    /// PmPrefs provides encrypted PlayerPrefs storage for Unity.
    /// </summary>
    public static class PmPrefs
    {
        public static void Save<T>(string key, T value) { /* ... */ }
        public static T Load<T>(string key, T defaultValue = default) { /* ... */ }
    }
}
```

#### Editor/ Directory

Contains **editor-only code** that enhances the development experience:

##### Code/ Subdirectory

C# scripts that extend the Unity Editor:

- **Purpose**: Editor windows, inspectors, and tools
- **Key Files**:
  - `PmPrefsEditorWindow.cs`: Main editor window using UIElements
  - `PrefsKeyReader.cs`: Platform-specific logic for reading system preferences
  - `PmPrefsListItem.cs`: Data model for preference list items
- **Usage**: Only compiled and available in the Unity Editor
- **Best Practice**: Use `UnityEditor` namespace APIs freely, implement editor-specific features

**Example from Editor/Code/PmPrefsEditorWindow.cs:**
```csharp
namespace PM.Plugins
{
    /// <summary>
    /// Editor window for managing PmPrefs and PlayerPrefs.
    /// </summary>
    public class PmPrefsEditorWindow : EditorWindow
    {
        [MenuItem("Tools/ProjectMakers/PmPrefs")]
        public static void ShowWindow() { /* ... */ }
    }
}
```

##### Style/ Subdirectory

UIElements visual assets for the editor interface:

- **Purpose**: Separation of UI structure and styling from logic
- **File Types**:
  - `.uxml` (UXML): UI structure and layout (like HTML)
  - `.uss` (USS): Styling and appearance (like CSS)
- **Key Files**:
  - `PmPrefs.uxml`: Main editor window layout
  - `PmPrefs.uss`: Global styles and theme
  - `PmPrefsListItem.uxml`: Template for list item display
- **Best Practice**: Keep UI markup separate from C# logic for maintainability

**Loading assets in code:**
```csharp
var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
    "Packages/com.projectmakers.pmprefs/Editor/Style/PmPrefs.uxml");
```

### Assembly Definitions

The project uses Unity Assembly Definitions (`.asmdef`) to organize code into modules and improve compilation times:

#### Core Assembly: `projectmakers.pmprefs.core.asmdef`

Located in `Scripts/` directory:

```json
{
    "name": "projectmakers.pmprefs.core"
}
```

- **Purpose**: Runtime API available to game code
- **Platform**: All platforms (included in builds)
- **Dependencies**: None (only Unity engine)
- **Namespace**: `PM.Plugins`
- **When to modify**: When adding new runtime functionality

#### Editor Assembly: `projectmakers.pmprefs.editor.asmdef`

Located in `Editor/Code/` directory:

```json
{
    "name": "projectmakers.PmPrefs.editor",
    "references": [
        "GUID:539df69f0ec247c4aa0656232fbd86e9"  // References core assembly
    ],
    "includePlatforms": [
        "Editor"  // Only compile for Unity Editor
    ]
}
```

- **Purpose**: Editor tools and UI for managing preferences
- **Platform**: Editor-only (excluded from builds)
- **Dependencies**: References core assembly to access `PmPrefs` API
- **Namespace**: `PM.Plugins`
- **When to modify**: When adding new editor features or tools

**Why use assembly definitions?**

1. **Faster Compilation**: Changes only recompile affected assemblies
2. **Clear Dependencies**: Explicit references prevent circular dependencies
3. **Platform Separation**: Editor code automatically excluded from builds
4. **Professional Structure**: Standard practice for Unity packages

**Guidelines for contributors:**

- ✅ Add new runtime code to `Scripts/` (no editor dependencies)
- ✅ Add new editor code to `Editor/Code/` (can reference Scripts/)
- ✅ Add new UI assets to `Editor/Style/`
- ❌ Don't reference editor assemblies from runtime code
- ❌ Don't mix runtime and editor code in the same assembly

### UIElements Architecture

PmPrefs uses Unity's UIElements framework (Unity 2019.1+) for its editor interface:

#### Separation of Concerns

The architecture follows best practices by separating structure, style, and logic:

```
┌─────────────────────────────────────────┐
│  PmPrefsEditorWindow.cs                 │  ← Logic & Behavior
│  (C# code)                              │
└────────────┬────────────────────────────┘
             │ loads and manipulates
             ↓
┌─────────────────────────────────────────┐
│  PmPrefs.uxml                           │  ← Structure & Layout
│  (UI markup)                            │
└────────────┬────────────────────────────┘
             │ references
             ↓
┌─────────────────────────────────────────┐
│  PmPrefs.uss                            │  ← Styling & Appearance
│  (stylesheets)                          │
└─────────────────────────────────────────┘
```

#### File Responsibilities

| File | Responsibility | Example |
|------|----------------|---------|
| `.uxml` | UI structure, element hierarchy | `<Button name="Save_btn" text="Save" />` |
| `.uss` | Visual styling, colors, spacing | `.button { background-color: #4a9eff; }` |
| `.cs` | Logic, event handling, data binding | `_saveButton.clicked += SaveAll;` |

#### Why This Matters

When contributing to the editor UI:

1. **Structural changes**: Modify `.uxml` files
2. **Visual changes**: Modify `.uss` files
3. **Functional changes**: Modify `.cs` files
4. **Keep them separated**: Don't inline styles in C# or add logic to UXML

**Example workflow for adding a new button:**

1. Add button element to `PmPrefs.uxml`:
   ```xml
   <Button name="MyNewButton_btn" text="My Action" class="button" />
   ```

2. Query and wire it up in `PmPrefsEditorWindow.cs`:
   ```csharp
   private Button _myNewButton;

   private void InitializeVisualElements()
   {
       _myNewButton = _root.Q<Button>("MyNewButton_btn");
       _myNewButton.clicked += OnMyNewButtonClicked;
       _myNewButton.tooltip = "Performs my new action";
   }

   private void OnMyNewButtonClicked()
   {
       // Implementation here
   }
   ```

3. Style it in `PmPrefs.uss` (if needed):
   ```css
   .button {
       padding: 8px 16px;
       border-radius: 4px;
   }
   ```

This architecture makes the codebase easier to maintain and allows designers to modify the UI without touching C# code.

## Code Style and Conventions

Consistent code style makes the project easier to read, maintain, and contribute to. PmPrefs follows standard C# and Unity conventions with some specific guidelines.

### Naming Conventions

PmPrefs follows Microsoft's C# naming conventions with Unity-specific additions:

#### General Rules

| Element | Convention | Example |
|---------|-----------|---------|
| **Classes** | PascalCase | `PmPrefs`, `PmPrefsEditorWindow` |
| **Interfaces** | PascalCase with `I` prefix | `IPrefsReader` |
| **Methods** | PascalCase | `Save()`, `Load()`, `DeleteAll()` |
| **Public Properties** | PascalCase | `ShowEncrypted`, `PmPrefsList` |
| **Public Fields** | PascalCase | `SecureKey`, `Prefix` |
| **Constants** | PascalCase | `SaltKey`, `ViKey`, `KeyListKey` |
| **Private Fields** | camelCase with `_` prefix | `_root`, `_listWrapper`, `_keyBytes` |
| **Parameters** | camelCase | `key`, `value`, `plainText` |
| **Local Variables** | camelCase | `json`, `plainTextBytes` |
| **Event Handlers** | PascalCase with `On` prefix | `OnSaveButtonClicked()` |

#### Examples from Codebase

**✅ Correct:**
```csharp
public class PmPrefsEditorWindow : EditorWindow
{
    // Private fields with underscore prefix
    private VisualElement _root;
    private ListView _listViewPmPrefsList;
    private Button _saveButton;

    // Public properties - PascalCase
    public bool ShowEncrypted;
    public List<PmPrefsListItem> PmPrefsList;

    // Constants - PascalCase
    public const string Prefix = "PmPrefs__";

    // Methods - PascalCase
    public static void ShowWindow()
    {
        // Local variables - camelCase
        PmPrefsEditorWindow wnd = GetWindow<PmPrefsEditorWindow>();
        wnd.titleContent = new GUIContent("PmPrefs");
    }

    // Event handlers - On prefix
    private void OnSaveButtonClicked()
    {
        SaveAll();
    }
}
```

**❌ Incorrect:**
```csharp
// Don't use Hungarian notation
private Button m_saveButton;  // Wrong
private Button btnSave;       // Wrong

// Don't use incorrect casing
private VisualElement Root;   // Wrong (should be _root)
public void saveAll()         // Wrong (should be SaveAll)
private void saveButtonClick() // Wrong (should be OnSaveButtonClicked)
```

#### Special Cases

**UIElements Query Names**

When querying UIElements by name, use the following convention:

```csharp
// UXML element names use suffix to indicate type
<Button name="Save_btn" />
<TextField name="CreateName_tf" />
<ListView name="PmPrefsList" />

// C# field names match but use proper casing
private Button _saveButton;          // Query from "Save_btn"
private TextField _createNameField;  // Query from "CreateName_tf"
private ListView _listViewPmPrefsList; // Query from "PmPrefsList"
```

**Platform-Specific Code**

Use conditional compilation with clear naming:

```csharp
#if UNITY_EDITOR_WIN
    private const string PrefsPath = @"HKEY_CURRENT_USER\Software\Unity\UnityEditor";
#elif UNITY_EDITOR_OSX
    private const string PrefsPath = "~/Library/Preferences/com.unity3d.UnityEditor.plist";
#endif
```

### Documentation Requirements

All public APIs must have XML documentation. This is not optional—it's required for maintainability and IntelliSense support.

#### Required XML Tags

Every public member must include at minimum:

```csharp
/// <summary>
/// Brief description of what this does (one or two sentences).
/// </summary>
public void MyMethod() { }
```

For methods with parameters and return values:

```csharp
/// <summary>
/// Brief description of what this method does.
/// </summary>
/// <param name="key">Description of the key parameter.</param>
/// <param name="value">Description of the value parameter.</param>
/// <returns>Description of what is returned.</returns>
public T Load<T>(string key, T value) { }
```

#### Complete Documentation Template

For complex public APIs, use this full template:

```csharp
/// <summary>
/// Saves a value with the given key using AES encryption.
/// The value is serialized to JSON before encryption.
/// </summary>
/// <typeparam name="T">The type of value to save. Must be serializable by JsonUtility.</typeparam>
/// <param name="key">The unique identifier for this preference. Cannot be null or empty.</param>
/// <param name="value">The value to save. If null, the key will be deleted.</param>
/// <exception cref="ArgumentException">Thrown when key is null or empty.</exception>
/// <example>
/// <code>
/// // Save a string
/// PmPrefs.Save("playerName", "John");
///
/// // Save a custom object
/// var settings = new GameSettings { volume = 0.8f };
/// PmPrefs.Save("settings", settings);
/// </code>
/// </example>
/// <remarks>
/// The saved data is encrypted using AES-256 encryption.
/// Change the SecureKey constant for production use.
/// </remarks>
public static void Save<T>(string key, T value)
{
    // Implementation...
}
```

#### Documentation Standards

**Summary Guidelines:**
- Start with a verb ("Saves...", "Loads...", "Deletes...", "Checks...")
- Keep it concise (1-2 sentences)
- Explain WHAT the method does, not HOW it works
- Add implementation details in `<remarks>` if needed

**Examples are Required For:**
- Public API methods users will call frequently
- Methods with non-obvious usage patterns
- Complex methods with multiple overloads

**✅ Good Documentation:**
```csharp
/// <summary>
/// Encrypts the given plain text using AES encryption.
/// </summary>
/// <param name="plainText">The text to encrypt.</param>
/// <returns>Base64 encoded encrypted string, or empty string if input is null/empty.</returns>
public static string Encrypt(string plainText)
```

**❌ Poor Documentation:**
```csharp
/// <summary>
/// Encryption method
/// </summary>
public static string Encrypt(string plainText) // Too vague

// OR

/// <summary>
/// This method takes the plainText parameter and converts it to bytes
/// then uses the Aes class to create an encryptor and writes to a memory stream...
/// </summary>
public static string Encrypt(string plainText) // Too detailed (belongs in remarks)
```

#### Internal/Private Member Documentation

Private members should have documentation when:
- The purpose is not immediately obvious
- The member is used across multiple methods
- There are important implementation details to remember

```csharp
/// <summary>
/// Ensures the key derivation bytes are initialized with the current SecureKey.
/// </summary>
private static byte[] GetKeyBytes()
{
    // Implementation...
}
```

Simple, self-explanatory private members don't need XML docs:

```csharp
// This is clear enough without XML docs
private void SaveKeyList()
{
    string json = JsonUtility.ToJson(_listWrapper);
    PlayerPrefs.SetString(KeyListKey, json);
}
```

### Namespace Usage

PmPrefs uses a consistent namespace structure to avoid conflicts and follow Unity package conventions.

#### Primary Namespace

**All PmPrefs code uses:**
```csharp
namespace PM.Plugins
{
    // Your code here
}
```

**Why this namespace?**
- `PM` = ProjectMakers (the publisher)
- `Plugins` = Indicates this is a plugin/package, not core game code
- Short and memorable for users
- Unlikely to conflict with other packages

#### Using Directives

**Runtime Scripts (Scripts/):**

Minimal dependencies—only include what's necessary:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace PM.Plugins
{
    public static class PmPrefs
    {
        // Implementation...
    }
}
```

**Editor Scripts (Editor/Code/):**

Can include editor namespaces:

```csharp
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PM.Plugins
{
    public class PmPrefsEditorWindow : EditorWindow
    {
        // Implementation...
    }
}
```

#### Namespace Guidelines

**✅ Do:**
- Use the `PM.Plugins` namespace for all project code
- Order using directives: System namespaces → Unity namespaces → Third-party → Project namespaces
- Remove unused using directives
- Use fully qualified names for rarely-used types to keep using directives clean

**❌ Don't:**
- Create nested namespaces (e.g., `PM.Plugins.Editor`) unless absolutely necessary
- Use `global::` unless resolving a genuine ambiguity
- Add `using static` directives (reduces code clarity)

#### Example: Resolving Conflicts

If you encounter namespace conflicts, use aliases:

```csharp
using UnityEditor;
using SystemFile = System.IO.File;  // Alias to avoid conflict with UnityEditor.File

namespace PM.Plugins
{
    public class PrefsImporter
    {
        public void Import(string path)
        {
            // Use the alias
            string content = SystemFile.ReadAllText(path);
        }
    }
}
```

### Editor UI Guidelines

PmPrefs uses UIElements for its editor interface. Follow these guidelines to maintain consistency.

#### UIElements Naming and Structure

**UXML Element Naming:**

Use descriptive names with type suffixes:

```xml
<!-- Buttons: _btn suffix -->
<Button name="Save_btn" text="Save" class="button" />
<Button name="DeleteAll_btn" text="Delete All" class="button-danger" />

<!-- Text fields: _tf suffix -->
<TextField name="CreateName_tf" label="Key" />
<TextField name="CreateValue_tf" label="Value" />

<!-- Lists: descriptive names without suffix -->
<ListView name="PmPrefsList" class="prefs-list" />

<!-- Containers: descriptive names -->
<VisualElement name="CreateNewContainer" class="section" />
```

**C# Field Mapping:**

Query elements using descriptive C# field names:

```csharp
// Query elements in InitializeVisualElements()
private Button _saveButton;
private Button _deleteAllButton;
private TextField _createNameField;
private TextField _createValueField;
private ListView _listViewPmPrefsList;
private VisualElement _createNewContainer;

private void InitializeVisualElements()
{
    // Map UXML names to C# fields
    _saveButton = _root.Q<Button>("Save_btn");
    _deleteAllButton = _root.Q<Button>("DeleteAll_btn");
    _createNameField = _root.Q<TextField>("CreateName_tf");
    _createValueField = _root.Q<TextField>("CreateValue_tf");
    _listViewPmPrefsList = _root.Q<ListView>("PmPrefsList");
    _createNewContainer = _root.Q<VisualElement>("CreateNewContainer");
}
```

#### Event Handling Patterns

**Use consistent event handler patterns:**

```csharp
private void InitializeVisualElements()
{
    // Query all elements first
    _saveButton = _root.Q<Button>("Save_btn");
    _deleteAllButton = _root.Q<Button>("DeleteAll_btn");

    // Wire up event handlers
    _saveButton.clicked += SaveAll;  // Direct method reference
    _deleteAllButton.clicked += OnDeleteAllButtonClicked;  // With confirmation

    // Add tooltips for better UX
    _saveButton.tooltip = "Save all changes to preferences";
    _deleteAllButton.tooltip = "Delete all preferences (PmPrefs and PlayerPrefs)";
}

// Simple action - direct method
private void SaveAll()
{
    // Implementation...
}

// Complex action - On prefix with descriptive name
private void OnDeleteAllButtonClicked()
{
    if (EditorUtility.DisplayDialog("Delete All Keys",
        "Are you sure you want to delete all preferences?",
        "Yes", "Cancel"))
    {
        PmPrefs.DeleteAll();
        RefreshLists();
    }
}
```

#### UI Patterns to Follow

**Loading Assets:**

Always provide fallback paths for development scenarios:

```csharp
private void Initialize()
{
    // Try package path first
    var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
        "Packages/com.projectmakers.pmprefs/Editor/Style/PmPrefs.uxml");

    // Fallback to Assets folder (for development)
    if (visualTree == null)
    {
        visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
            "Assets/PmPrefs/Editor/Style/PmPrefs.uxml");
    }

    // Handle failure gracefully
    if (visualTree == null)
    {
        Debug.LogError("[PmPrefs] Could not load PmPrefs.uxml");
        return;
    }

    var root = visualTree.Instantiate();
    rootVisualElement.Add(root);
}
```

**Error Handling in UI Code:**

Always validate and provide user feedback:

```csharp
private void CreateNewPref()
{
    string key = _createNameField.value;
    string value = _createValueField.value;

    // Validate input
    if (string.IsNullOrWhiteSpace(key))
    {
        EditorUtility.DisplayDialog("Invalid Key",
            "Key cannot be empty.",
            "OK");
        return;
    }

    // Check for duplicates
    if (PmPrefs.HasKey(key))
    {
        if (!EditorUtility.DisplayDialog("Key Exists",
            $"Key '{key}' already exists. Overwrite?",
            "Yes", "Cancel"))
        {
            return;
        }
    }

    // Perform action
    PmPrefs.Save(key, value);

    // Update UI
    RefreshLists();

    // Clear form
    _createNameField.value = "";
    _createValueField.value = "";
}
```

**Refresh Patterns:**

Provide manual refresh options for long-running operations:

```csharp
private void OnRefreshButtonClicked()
{
    _prefsKeyReader.InvalidateCache();
    RefreshLists();
    _root.MarkDirtyRepaint();
    Repaint();
}
```

#### USS Styling Guidelines

Keep styles organized and use CSS-like conventions:

```css
/* Group related styles together */
.button {
    padding: 8px 16px;
    border-radius: 4px;
    background-color: #4a9eff;
    color: white;
}

.button:hover {
    background-color: #3a8eef;
}

/* Use BEM-like naming for variants */
.button-danger {
    background-color: #ff4a4a;
}

.button-danger:hover {
    background-color: #ef3a3a;
}

/* Descriptive class names for sections */
.prefs-list {
    flex-grow: 1;
    min-height: 200px;
}

.section {
    padding: 12px;
    margin: 8px;
    border-width: 1px;
    border-color: rgba(0, 0, 0, 0.2);
}
```

#### Accessibility Considerations

Make the UI accessible to all developers:

```csharp
// Always add tooltips
_saveButton.tooltip = "Save all changes to preferences";

// Use clear, descriptive labels
_createNameField.label = "Preference Key";
_createValueField.label = "Value";

// Provide keyboard support
_createNameField.RegisterCallback<KeyDownEvent>(evt =>
{
    if (evt.keyCode == KeyCode.Return)
    {
        CreateNewPref();
    }
});

// Use semantic naming
var container = new VisualElement { name = "PreferencesContainer" };
```

---

**Quick Reference:**

- 📝 **Naming**: PascalCase for public, _camelCase for private
- 📖 **Docs**: XML comments required for all public APIs
- 📦 **Namespace**: Always use `PM.Plugins`
- 🎨 **UI**: Query elements → Wire events → Add tooltips

## Testing and Quality Assurance

PmPrefs does not currently have automated tests, so thorough manual testing is essential for maintaining quality. Before submitting any contribution, you must verify your changes across multiple scenarios.

### Manual Testing Procedures

#### Core API Testing

Every change that affects the core `PmPrefs` API must be tested with the following scenarios:

**1. Basic Save and Load Operations**

Create a test script to verify basic functionality:

```csharp
using UnityEngine;
using PM.Plugins;

public class PmPrefsApiTest : MonoBehaviour
{
    void Start()
    {
        RunTests();
    }

    private void RunTests()
    {
        Debug.Log("=== PmPrefs API Tests ===");

        // Test 1: Save and load string
        PmPrefs.Save("test_string", "Hello PmPrefs!");
        string loadedString = PmPrefs.Load<string>("test_string", "");
        Debug.Assert(loadedString == "Hello PmPrefs!", "String test failed");
        Debug.Log("✓ String save/load test passed");

        // Test 2: Save and load int
        PmPrefs.Save("test_int", 42);
        int loadedInt = PmPrefs.Load<int>("test_int", 0);
        Debug.Assert(loadedInt == 42, "Int test failed");
        Debug.Log("✓ Int save/load test passed");

        // Test 3: Save and load float
        PmPrefs.Save("test_float", 3.14f);
        float loadedFloat = PmPrefs.Load<float>("test_float", 0f);
        Debug.Assert(Mathf.Approximately(loadedFloat, 3.14f), "Float test failed");
        Debug.Log("✓ Float save/load test passed");

        // Test 4: Save and load bool
        PmPrefs.Save("test_bool", true);
        bool loadedBool = PmPrefs.Load<bool>("test_bool", false);
        Debug.Assert(loadedBool == true, "Bool test failed");
        Debug.Log("✓ Bool save/load test passed");

        // Test 5: Default value when key doesn't exist
        string defaultValue = PmPrefs.Load<string>("nonexistent_key", "default");
        Debug.Assert(defaultValue == "default", "Default value test failed");
        Debug.Log("✓ Default value test passed");

        // Test 6: HasKey check
        Debug.Assert(PmPrefs.HasKey("test_string"), "HasKey test failed for existing key");
        Debug.Assert(!PmPrefs.HasKey("nonexistent_key"), "HasKey test failed for nonexistent key");
        Debug.Log("✓ HasKey test passed");

        // Test 7: Delete key
        PmPrefs.DeleteKey("test_string");
        Debug.Assert(!PmPrefs.HasKey("test_string"), "DeleteKey test failed");
        Debug.Log("✓ DeleteKey test passed");

        // Cleanup
        PmPrefs.DeleteKey("test_int");
        PmPrefs.DeleteKey("test_float");
        PmPrefs.DeleteKey("test_bool");

        Debug.Log("=== All API tests passed! ===");
    }
}
```

**2. Complex Data Types Testing**

Test serialization of custom objects:

```csharp
[System.Serializable]
public class TestData
{
    public string playerName;
    public int level;
    public float health;
    public bool isActive;
}

private void TestComplexTypes()
{
    // Save complex object
    var testData = new TestData
    {
        playerName = "TestPlayer",
        level = 10,
        health = 95.5f,
        isActive = true
    };

    PmPrefs.Save("test_object", testData);

    // Load and verify
    var loadedData = PmPrefs.Load<TestData>("test_object", null);
    Debug.Assert(loadedData != null, "Complex object is null");
    Debug.Assert(loadedData.playerName == "TestPlayer", "Complex object name mismatch");
    Debug.Assert(loadedData.level == 10, "Complex object level mismatch");
    Debug.Assert(Mathf.Approximately(loadedData.health, 95.5f), "Complex object health mismatch");
    Debug.Assert(loadedData.isActive == true, "Complex object isActive mismatch");

    Debug.Log("✓ Complex object test passed");

    // Cleanup
    PmPrefs.DeleteKey("test_object");
}
```

**3. Edge Cases and Error Handling**

Test boundary conditions:

```csharp
private void TestEdgeCases()
{
    // Empty string key (should handle gracefully)
    try
    {
        PmPrefs.Save("", "value");
        Debug.LogWarning("Empty key was accepted - verify this is intended behavior");
    }
    catch (System.Exception e)
    {
        Debug.Log("✓ Empty key rejected correctly: " + e.Message);
    }

    // Null value
    PmPrefs.Save("null_test", (string)null);
    string nullResult = PmPrefs.Load<string>("null_test", "default");
    Debug.Log($"Null value result: {nullResult}");

    // Very long strings
    string longString = new string('A', 10000);
    PmPrefs.Save("long_string", longString);
    string loadedLong = PmPrefs.Load<string>("long_string", "");
    Debug.Assert(loadedLong.Length == 10000, "Long string test failed");
    Debug.Log("✓ Long string test passed");

    // Special characters
    string specialChars = "Test!@#$%^&*()[]{}|\\:;\"'<>,.?/";
    PmPrefs.Save("special_chars", specialChars);
    string loadedSpecial = PmPrefs.Load<string>("special_chars", "");
    Debug.Assert(loadedSpecial == specialChars, "Special characters test failed");
    Debug.Log("✓ Special characters test passed");

    // Cleanup
    PmPrefs.DeleteKey("null_test");
    PmPrefs.DeleteKey("long_string");
    PmPrefs.DeleteKey("special_chars");
}
```

#### Editor Window Testing

The PmPrefs Editor window must be tested manually for each UI feature:

**1. Opening and Initialization**

- [ ] Open editor window via **Tools > ProjectMakers > PmPrefs**
- [ ] Verify window opens without console errors
- [ ] Verify UI elements are visible and properly styled
- [ ] Verify window can be docked, undocked, and resized
- [ ] Verify minimum window size is enforced (380x356)

**2. Viewing Preferences**

- [ ] **PmPrefs List**: Click **PmPrefs** tab and verify encrypted preferences are shown
- [ ] **PlayerPrefs List**: Click **PlayerPrefs** tab and verify non-PmPrefs entries are shown
- [ ] **Search**: Type in search field and verify filtering works
- [ ] **Show Encrypted**: Toggle button and verify values switch between encrypted/decrypted
- [ ] **Refresh**: Click refresh button and verify lists update

**3. Creating New Preferences**

- [ ] Click **Create New** button to show/hide the creation panel
- [ ] Enter a key and value, click **Create**
- [ ] Verify the new preference appears in the list
- [ ] Try creating a duplicate key—verify warning dialog appears
- [ ] Try creating with empty key—verify validation prevents it
- [ ] Verify created preferences persist after closing/reopening Unity

**4. Editing Preferences**

- [ ] Click on a preference in the list
- [ ] Modify the value in the detail view
- [ ] Click **Save** button
- [ ] Verify changes are saved (refresh and check)
- [ ] Verify edited values are properly encrypted in PlayerPrefs

**5. Deleting Preferences**

- [ ] Select a preference and click its delete button
- [ ] Verify confirmation dialog appears
- [ ] Confirm deletion and verify preference is removed
- [ ] Click **Delete All** button
- [ ] Verify confirmation dialog warns about irreversible action
- [ ] Confirm and verify all preferences are deleted

**6. Import/Export Functionality**

- [ ] Click **Export** button
- [ ] Verify file save dialog appears
- [ ] Save to CSV file
- [ ] Open the CSV file and verify format is correct:
  ```
  Key,Value,Type
  test_key,test_value,PmPrefs
  ```
- [ ] Delete some preferences from the editor
- [ ] Click **Import** button
- [ ] Select the previously exported CSV file
- [ ] Verify preferences are restored correctly
- [ ] Test importing invalid CSV format—verify error handling

**7. Configuration Panel**

- [ ] Click **Configuration** button to show/hide panel
- [ ] Enter a new secure key in the field
- [ ] Click **Change Secure Key**
- [ ] Verify warning dialog explains the consequences
- [ ] Confirm and verify preferences are re-encrypted
- [ ] Verify old key can no longer decrypt preferences
- [ ] Verify new key successfully decrypts preferences

#### Encryption Security Testing

**1. Encryption Verification**

Manually verify that values are actually encrypted:

```csharp
// Save a value via PmPrefs
PmPrefs.Save("test_encrypted", "MySensitiveData");

// Read the raw PlayerPrefs value
string rawValue = PlayerPrefs.GetString("PmPrefs__test_encrypted");

Debug.Log("Raw encrypted value: " + rawValue);
// Should output Base64-encoded encrypted string, NOT "MySensitiveData"

Debug.Assert(!rawValue.Contains("MySensitiveData"),
    "SECURITY ISSUE: Value is not encrypted!");
```

**2. Key Isolation Testing**

Verify PmPrefs doesn't interfere with regular PlayerPrefs:

```csharp
// Create a regular PlayerPrefs entry
PlayerPrefs.SetString("regular_key", "regular_value");

// Create a PmPrefs entry
PmPrefs.Save("pmprefs_key", "pmprefs_value");

// Verify both exist independently
string regular = PlayerPrefs.GetString("regular_key");
string pmprefs = PmPrefs.Load<string>("pmprefs_key", "");

Debug.Assert(regular == "regular_value", "PlayerPrefs interference detected");
Debug.Assert(pmprefs == "pmprefs_value", "PmPrefs value corrupted");
Debug.Log("✓ Key isolation test passed");
```

**3. Secure Key Change Testing**

Verify that changing the secure key properly re-encrypts data:

```csharp
// Save data with original key
PmPrefs.Save("test_reencrypt", "TestData");

// Change the secure key (via editor or code)
// Then verify data can still be loaded
string reloaded = PmPrefs.Load<string>("test_reencrypt", "FAILED");

Debug.Assert(reloaded == "TestData", "Re-encryption failed");
Debug.Log("✓ Secure key change test passed");
```

#### Platform-Specific Testing

PmPrefs uses platform-specific PlayerPrefs storage. Test on each platform:

**Windows Testing**

- [ ] Verify preferences save/load correctly
- [ ] Check Registry Editor at: `HKEY_CURRENT_USER\Software\[Company]\[Product]`
- [ ] Verify encrypted values are stored in registry
- [ ] Test with Windows-specific paths and special characters

**macOS Testing**

- [ ] Verify preferences save/load correctly
- [ ] Check plist file at: `~/Library/Preferences/com.[Company].[Product].plist`
- [ ] Use `defaults read` command to inspect raw values
- [ ] Verify encrypted values are stored correctly

**Linux Testing**

- [ ] Verify preferences save/load correctly
- [ ] Check preference file at: `~/.config/unity3d/[Company]/[Product]/prefs`
- [ ] Verify file permissions are correct
- [ ] Test with Linux-specific path handling

**WebGL Considerations**

- [ ] Note that PmPrefs uses PlayerPrefs, which uses browser localStorage on WebGL
- [ ] Test in multiple browsers (Chrome, Firefox, Safari)
- [ ] Verify encrypted data works within localStorage size limits (usually 5-10MB)

### Quality Checklist

Before submitting a pull request, verify ALL items in this checklist:

#### Code Quality

- [ ] **No compilation errors**: Project compiles without errors or warnings
- [ ] **No console errors**: No errors in Unity Console when testing
- [ ] **No debug code**: Remove all `Debug.Log`, `Console.WriteLine`, or test code
- [ ] **XML documentation**: All public APIs have complete XML doc comments
- [ ] **Consistent naming**: Follows naming conventions (PascalCase, _camelCase, etc.)
- [ ] **Namespace correct**: All code uses `PM.Plugins` namespace
- [ ] **No hardcoded paths**: Use proper asset loading with fallbacks
- [ ] **Error handling**: Exceptions are caught and handled gracefully

#### Functionality

- [ ] **Feature works as intended**: Core functionality is fully implemented
- [ ] **Edge cases handled**: Tested with null, empty, and invalid inputs
- [ ] **No regressions**: Existing features still work after changes
- [ ] **UI responsive**: Editor window updates immediately after actions
- [ ] **Data persistence**: Preferences survive Unity restarts
- [ ] **Encryption intact**: Values are properly encrypted in PlayerPrefs

#### User Experience

- [ ] **Clear error messages**: Users understand what went wrong and how to fix it
- [ ] **Tooltips added**: All buttons and interactive elements have helpful tooltips
- [ ] **Confirmation dialogs**: Destructive actions (delete, delete all) require confirmation
- [ ] **Visual feedback**: UI updates reflect the current state accurately
- [ ] **Responsive UI**: No freezing or lag during operations
- [ ] **Keyboard support**: Enter key works in text fields where appropriate

#### Documentation

- [ ] **Public APIs documented**: XML comments explain what, why, and how
- [ ] **Code comments added**: Complex logic has explanatory comments
- [ ] **README updated**: If adding new features, update README.md
- [ ] **CHANGELOG updated**: Add entry describing your changes
- [ ] **Examples provided**: Complex features have usage examples

#### Testing

- [ ] **Manual testing completed**: All testing procedures above were followed
- [ ] **Multiple scenarios tested**: Tested with various data types and edge cases
- [ ] **Cross-platform tested**: Verified on Windows, macOS, or Linux (if possible)
- [ ] **Unity version tested**: Tested on target Unity version (ideally 2020.3 LTS or newer)
- [ ] **Clean environment tested**: Tested in fresh Unity project to catch dependencies

#### Performance

- [ ] **No performance regressions**: Operations complete in reasonable time
- [ ] **Large datasets handled**: Tested with 100+ preferences
- [ ] **Memory leaks checked**: No increasing memory usage over time
- [ ] **UI remains responsive**: Editor doesn't freeze during operations

#### Security

- [ ] **Encryption verified**: Confirmed values are encrypted in storage
- [ ] **No plaintext leaks**: Sensitive data never logged or displayed unintentionally
- [ ] **Secure key handling**: Default secure key is documented as needing change
- [ ] **No security warnings**: No obvious security vulnerabilities introduced

### Testing Across Unity Versions

PmPrefs supports Unity 2018.1 and later. When making changes, consider compatibility:

#### Minimum Version Testing (Unity 2018.1)

If you have access to Unity 2018.1:

- [ ] Verify package compiles without errors
- [ ] Test core API functionality
- [ ] Note any deprecated API warnings

**Common compatibility issues:**
- UIElements API differs significantly between 2018-2019
- Some `System` APIs may not be available in older .NET Standard 2.0
- PlayerPrefs behavior is consistent, but storage locations may differ

#### LTS Version Testing (Unity 2020.3 LTS)

Recommended baseline for testing:

- [ ] Full testing of all features
- [ ] Editor window UI displays correctly
- [ ] All UIElements features work properly
- [ ] Performance is acceptable

#### Latest Version Testing (Unity 2022.3+ / Unity 6000)

Test with the latest LTS or stable version:

- [ ] Verify no deprecation warnings
- [ ] Test with latest UIElements improvements
- [ ] Verify package manifest compatibility
- [ ] Check for API changes or obsolete warnings

#### Version-Specific Notes

**Unity 2018.x - 2019.x:**
- UIElements is less mature—test thoroughly
- Limited UIElements debugging tools
- May need IMGUI fallbacks for some features

**Unity 2020.3 LTS:**
- Stable UIElements implementation
- Good baseline for testing
- Widely used by production projects

**Unity 2021.3 LTS and later:**
- Mature UIElements with better debugging
- Package Manager improvements
- Better performance overall

**Unity 6000:**
- Latest Unity version with all modern features
- Test to ensure forward compatibility
- Note any new features that could improve PmPrefs

#### Testing Strategy

If you don't have access to multiple Unity versions:

1. **Test on your current version thoroughly**
2. **Note your Unity version in the PR description**
3. **Document any version-specific code you added**
4. **Use conditional compilation for version-specific features:**

```csharp
#if UNITY_2020_1_OR_NEWER
    // Use newer API
    var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
#else
    // Use older API for compatibility
    var visualTree = (VisualTreeAsset)AssetDatabase.LoadAssetAtPath(path, typeof(VisualTreeAsset));
#endif
```

#### Continuous Integration Note

**Current Status**: PmPrefs does not have CI/CD pipelines set up.

**Future Goal**: Automated testing across multiple Unity versions using Unity Test Framework.

**How you can help**: If you have experience setting up Unity CI/CD pipelines (GitHub Actions, Unity Cloud Build, etc.), contributions to add automated testing would be greatly appreciated!

---

**Testing Summary:**

✅ **Before every commit:**
- Run manual API tests
- Test in editor window
- Check for console errors
- Verify encryption is working

✅ **Before every PR:**
- Complete full quality checklist
- Test on your Unity version
- Update documentation
- Clean up debug code

✅ **For major changes:**
- Test across multiple Unity versions if possible
- Test on multiple platforms if possible
- Add examples to README
- Update CHANGELOG

## Pull Request Process

Following the proper pull request process ensures your contribution can be reviewed and merged efficiently. This section covers everything from preparing your changes to getting them merged.

### Before Submitting

Before you create a pull request, ensure you have completed all the necessary steps:

#### 1. Complete the Quality Checklist

Go through the entire [Quality Checklist](#quality-checklist) and verify every item. This includes:

- ✅ Code compiles without errors or warnings
- ✅ Manual testing completed successfully
- ✅ XML documentation added to all public APIs
- ✅ No debug code (`Debug.Log`, `Console.WriteLine`, etc.)
- ✅ Error handling in place
- ✅ Tooltips added to UI elements
- ✅ Code follows naming conventions
- ✅ Performance is acceptable

#### 2. Update Documentation

If your changes affect user-facing functionality:

- **README.md**: Update if you've added new features, changed behavior, or modified the API
- **CHANGELOG.md**: Add an entry describing your changes (see [Updating the Changelog](#updating-the-changelog))
- **XML Comments**: Ensure all public APIs have complete documentation
- **Code Comments**: Add explanatory comments for complex logic

#### 3. Test Thoroughly

At a minimum:

- [ ] Test your changes in a clean Unity project
- [ ] Test with Unity 2020.3 LTS or newer (if possible)
- [ ] Verify no regression—existing features still work
- [ ] Test edge cases (null, empty, invalid inputs)
- [ ] Check for console errors and warnings
- [ ] Verify encryption is working (for core changes)

#### 4. Clean Up Your Commits

Before submitting:

```bash
# Ensure you're on your feature branch
git checkout your-feature-branch

# Review your changes
git status
git diff main

# Rebase on latest main (if needed)
git fetch upstream
git rebase upstream/main

# Clean up commit history (if needed)
git rebase -i upstream/main
```

Make sure your commit history is clean and follows the [Commit Message Guidelines](#commit-message-guidelines).

### Commit Message Guidelines

PmPrefs follows conventional commit conventions to maintain a clear and searchable history.

#### Format

```
<type>: <subject>

<body>

<footer>
```

#### Type

Use one of the following commit types:

| Type | Description | Example |
|------|-------------|---------|
| `feat` | New feature or enhancement | `feat: add DeleteAllPmPrefs method` |
| `fix` | Bug fix | `fix: prevent crash when decrypting empty string` |
| `docs` | Documentation changes only | `docs: update README with new examples` |
| `style` | Code style/formatting (no logic change) | `style: fix indentation in PmPrefs.cs` |
| `refactor` | Code refactoring (no behavior change) | `refactor: extract encryption logic to separate method` |
| `perf` | Performance improvement | `perf: add key caching to reduce lookup time` |
| `test` | Adding or updating tests | `test: add edge case tests for Save method` |
| `chore` | Maintenance tasks, build changes | `chore: update package.json version to 2.2.0` |

#### Subject Line

The subject line should:

- **Start with lowercase** (except proper nouns)
- **Use imperative mood** ("add" not "added" or "adds")
- **Be concise** (50 characters or less)
- **Not end with a period**
- **Describe what the commit does**, not what you did

**✅ Good examples:**
```
feat: add cross-platform prefs reader
fix: dispose StreamReader properly in Import
docs: add contribution guidelines
refactor: replace RijndaelManaged with Aes.Create
```

**❌ Bad examples:**
```
Added new feature    # Wrong tense, no type prefix
fix: Fixed the bug.  # Wrong tense, has period
Update code          # Too vague, no type
feat: I added a new method for deleting all preferences  # Too long, wrong perspective
```

#### Body (Optional but Recommended)

The commit body should:

- **Explain WHY** you made the change, not WHAT changed (the diff shows that)
- **Provide context** for reviewers
- **Reference issues** if applicable
- **Wrap at 72 characters** per line

**Example:**
```
feat: add DeleteAllPmPrefs method

PmPrefs.DeleteAll() was deleting both PmPrefs AND regular PlayerPrefs,
which could cause data loss for users who mix both systems.

This adds a new DeleteAllPmPrefs() method that only deletes PmPrefs
entries, preserving regular PlayerPrefs. The old DeleteAll() is kept
for backward compatibility but now logs a warning.

Fixes #42
```

#### Footer (When Applicable)

Use the footer for:

- **Breaking changes**: Start with `BREAKING CHANGE:`
- **Issue references**: `Fixes #123`, `Closes #456`, `Relates to #789`

**Example:**
```
feat: change encryption to require explicit key

BREAKING CHANGE: SecureKey is now a required parameter instead of
a hardcoded constant. Users must provide their own encryption key.

Migration guide:
- Before: PmPrefs.Save("key", "value")
- After: PmPrefs.Save("key", "value", mySecureKey)

Fixes #15
```

#### Real Examples from PmPrefs

Here are actual good commit examples:

```
fix: dispose StreamReader properly in Import

The Import method was not disposing the StreamReader,
causing file locks on Windows. Wrapped it in a using
statement to ensure proper disposal.
```

```
feat: add XML documentation to public API

All public methods now have complete XML doc comments
for better IntelliSense support and maintainability.
Includes summary, param, returns, and example tags.
```

```
refactor: replace RijndaelManaged with Aes.Create

RijndaelManaged is deprecated in modern .NET. Switched
to the recommended Aes.Create() API while maintaining
backward compatibility with existing encrypted data.
```

### Updating the Changelog

**IMPORTANT**: Every pull request that changes functionality must update `CHANGELOG.md`.

#### When to Update the Changelog

Update the changelog if your PR includes:

- ✅ New features or enhancements
- ✅ Bug fixes
- ✅ Breaking changes
- ✅ Deprecations or removals
- ✅ Performance improvements
- ✅ Security fixes

**Skip the changelog only for:**
- ❌ Documentation-only changes
- ❌ Code style/formatting changes
- ❌ Internal refactoring with no user-facing impact

#### Changelog Format

PmPrefs follows [Keep a Changelog](http://keepachangelog.com/) format.

**Structure:**

```markdown
# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- New features go here

### Changed
- Changes to existing functionality go here

### Fixed
- Bug fixes go here

### Removed
- Removed features go here

## [2.2.0] - 2026-02-05

### Added
- Previous release entries...
```

#### How to Add Your Changes

1. **Find or create the `[Unreleased]` section** at the top of the changelog (after the header)
2. **Add your changes under the appropriate category:**

   | Category | Use For |
   |----------|---------|
   | `Added` | New features, new methods, new capabilities |
   | `Changed` | Changes to existing functionality, updated behavior |
   | `Fixed` | Bug fixes, corrections |
   | `Deprecated` | Features marked for removal (rarely used) |
   | `Removed` | Removed features or deprecated code |
   | `Security` | Security-related fixes or improvements |

3. **Write a clear, concise entry:**
   - Start with a verb (Added, Fixed, Changed, etc.)
   - Be specific about what changed
   - Include method names in backticks: `` `DeleteKey()` ``
   - Keep it user-focused—what does this mean for users?

**✅ Good changelog entries:**

```markdown
### Added
- `DeleteAllPmPrefs()` method to delete only PmPrefs entries without affecting regular PlayerPrefs
- XML documentation comments on all public API methods
- Tooltips on all Editor window buttons for better UX

### Changed
- Export now always exports readable (decrypted) values instead of encrypted data
- Optimized UI refresh - SaveAll no longer rebuilds entire UI

### Fixed
- Fixed JsonUtility not supporting `List<string>` - now uses wrapper class
- Fixed StreamReader not being disposed properly in Import
- Fixed empty string causing crash in Decrypt method

### Removed
- Removed Windows-only `GetWindowsKeys.cs` (replaced by cross-platform `PrefsKeyReader.cs`)
```

**❌ Bad changelog entries:**

```markdown
### Added
- Added stuff  # Too vague
- New method   # What method? What does it do?
- I added a new feature that lets you delete preferences  # Too verbose, wrong perspective

### Fixed
- Fixed bug    # What bug? Where?
- Various fixes  # Not helpful to users

### Changed
- Updated code  # What changed? Why does it matter?
```

#### Example Workflow

1. **Before making changes**, add an `[Unreleased]` section if it doesn't exist:

   ```markdown
   ## [Unreleased]

   ### Added

   ### Changed

   ### Fixed

   ## [2.2.0] - 2026-02-05
   ...
   ```

2. **As you work**, add entries to the appropriate category:

   ```markdown
   ## [Unreleased]

   ### Added
   - `HasKey()` method to check if a key exists without loading its value

   ### Fixed
   - Fixed race condition when multiple threads access PmPrefs simultaneously
   ```

3. **Before submitting your PR**, review your changelog entries for clarity

4. **In your PR description**, mention the changelog update

#### Changelog Best Practices

- **Be user-focused**: Write for people who will read the changelog to understand what changed, not how it was implemented
- **Group related changes**: If you fixed multiple related issues, consider combining them into one entry
- **Link to issues**: Reference issue numbers when applicable: `Fixes #42`
- **Keep it readable**: Don't make it too technical unless necessary
- **Follow existing style**: Look at previous changelog entries for guidance

**Example of grouping related changes:**

```markdown
### Fixed
- Fixed multiple encryption issues:
  - Empty string no longer causes crash in Decrypt method
  - StreamReader now properly disposed in Import
  - Crypto transforms no longer leak memory
```

### Review Process

Once you've submitted your pull request, here's what to expect:

#### 1. Automated Checks

Currently, PmPrefs does not have CI/CD automation, but your PR will be manually reviewed for:

- ✅ Code compiles without errors
- ✅ Follows code style conventions
- ✅ No obvious bugs or security issues
- ✅ Documentation is complete

**Future**: We plan to add automated testing with Unity Test Framework and CI/CD pipelines.

#### 2. Code Review

A maintainer will review your code and may:

- **Request changes**: Ask you to fix issues, improve code, or add documentation
- **Ask questions**: Seek clarification on your approach or implementation
- **Suggest improvements**: Recommend alternative approaches or optimizations
- **Approve**: Give their approval if everything looks good

**How to respond to review feedback:**

```bash
# Make the requested changes
# Edit your files locally

# Commit the changes
git add .
git commit -m "refactor: address PR review feedback"

# Push to your fork (this updates the PR automatically)
git push origin your-feature-branch
```

**Best practices during review:**

- ✅ Respond to feedback promptly
- ✅ Ask questions if you don't understand a comment
- ✅ Be open to suggestions—reviewers want to help improve the code
- ✅ Mark conversations as resolved once addressed
- ❌ Don't take feedback personally—it's about the code, not you
- ❌ Don't force-push after review has started (makes it hard to see changes)

#### 3. Testing by Maintainers

Maintainers may:

- Test your changes in multiple Unity versions
- Test on different platforms (Windows, macOS, Linux)
- Verify no regressions with existing features
- Check performance impact

#### 4. Merge

Once approved and all checks pass:

1. **Maintainer merges** your PR into the `main` branch
2. **Your changes appear** in the next release
3. **You're credited** as a contributor in release notes
4. **Thank you!** 🎉 You've successfully contributed to PmPrefs!

#### Common Review Feedback

Be prepared for these common review comments:

| Feedback | Why It Matters | How to Fix |
|----------|----------------|------------|
| "Add XML docs to public methods" | Required for maintainability | Add `/// <summary>` comments |
| "Remove debug logs" | Production code shouldn't log debug info | Remove `Debug.Log()` calls |
| "Add error handling" | Code should handle edge cases gracefully | Add try-catch, null checks |
| "This could break existing code" | Backward compatibility is important | Provide migration path or deprecate gracefully |
| "Add tooltips to UI elements" | Improves user experience | Add `.tooltip = "..."` to buttons |
| "Update the changelog" | Users need to know what changed | Add entry to `CHANGELOG.md` |
| "Tests needed" | Need to verify functionality | Add manual testing steps or automated tests |

#### Timeline Expectations

- **Initial review**: Within 1-3 days (maintainers are volunteers)
- **Follow-up responses**: 1-2 days
- **Merge**: Once approved, usually within 24 hours

**If you don't hear back after a week**, feel free to:

- Add a polite comment to your PR: "Friendly ping—any update on this PR?"
- Reach out on GitHub Discussions or Issues

#### After Your PR is Merged

1. **Sync your fork** with the main repository:

   ```bash
   git checkout main
   git fetch upstream
   git merge upstream/main
   git push origin main
   ```

2. **Delete your feature branch** (optional but recommended):

   ```bash
   git branch -d your-feature-branch
   git push origin --delete your-feature-branch
   ```

3. **Celebrate!** 🎉 You're now a PmPrefs contributor!

4. **Watch for the release** that includes your changes

---

**Quick Reference:**

1. ✅ Complete quality checklist
2. 📝 Update CHANGELOG.md
3. ✍️ Write good commit messages (`type: subject`)
4. 🔄 Submit pull request
5. 💬 Respond to review feedback
6. 🎉 Merge and celebrate!

## Community Guidelines

PmPrefs is an open-source project built and maintained by contributors like you. We strive to foster a welcoming, inclusive, and collaborative environment where everyone feels comfortable contributing.

### Code of Conduct

By participating in this project, you agree to abide by our Code of Conduct:

#### Our Pledge

We pledge to make participation in PmPrefs a harassment-free experience for everyone, regardless of:

- Age, body size, disability, ethnicity, gender identity and expression
- Level of experience, education, or technical skill
- Nationality, personal appearance, race, religion, or sexual identity and orientation

#### Our Standards

**Positive behaviors that contribute to a welcoming environment:**

✅ **Be Respectful**: Treat all contributors with respect and kindness. Disagreements are normal, but always remain professional and constructive.

✅ **Be Collaborative**: Work together toward the best solutions. Share knowledge, help others learn, and accept feedback gracefully.

✅ **Be Inclusive**: Use welcoming and inclusive language. Make newcomers feel welcome and help them get started.

✅ **Be Patient**: Remember that everyone was a beginner once. Take time to explain concepts and answer questions thoroughly.

✅ **Be Constructive**: Provide helpful, actionable feedback. Focus on the code, not the person.

✅ **Give Credit**: Acknowledge the contributions of others. Attribute ideas and give credit where it's due.

**Unacceptable behaviors:**

❌ Harassment, trolling, or insulting/derogatory comments
❌ Personal or political attacks
❌ Publishing others' private information without permission
❌ Spam, advertisements, or off-topic content
❌ Any conduct that would be inappropriate in a professional setting

#### Enforcement

Instances of unacceptable behavior may be reported to the project maintainers. All complaints will be reviewed and investigated promptly and fairly.

Maintainers have the right and responsibility to remove, edit, or reject comments, commits, code, issues, and other contributions that don't align with this Code of Conduct.

#### Scope

This Code of Conduct applies to all project spaces, including:

- GitHub repository (issues, pull requests, discussions)
- Code reviews and comments
- Project documentation
- Any other communication channels associated with PmPrefs

### Getting Help

We're here to help you contribute successfully! Here are the best ways to get assistance:

#### For Questions About Contributing

**📖 Read the Documentation First**

Before asking questions, check if your answer is already available:

- **This CONTRIBUTING.md**: Comprehensive guide to contributing
- **README.md**: Project overview, usage examples, and features
- **Existing Issues**: Someone may have already asked your question
- **Pull Requests**: See how others have contributed

#### For Technical Questions

**🐛 Open a GitHub Issue**

If you've found a bug or have a technical question:

1. **Search existing issues** first to avoid duplicates
2. **Use descriptive titles**: "Editor window crashes when importing CSV with 1000+ entries"
3. **Provide context**:
   - Unity version
   - Operating system
   - Steps to reproduce
   - Expected vs. actual behavior
   - Console errors (if any)
4. **Be specific**: The more details you provide, the faster we can help

**Example of a good issue:**

```
Title: PmPrefs.Load<T> throws exception with nested generic types

Description:
I'm trying to load a List<List<int>> using PmPrefs.Load, but it throws
a JsonUtility exception.

Unity Version: 2021.3.15f1
OS: Windows 11
Steps to reproduce:
1. Save: PmPrefs.Save("nested", new List<List<int>>());
2. Load: var result = PmPrefs.Load<List<List<int>>>("nested");
3. Exception is thrown

Expected: Should serialize/deserialize nested lists
Actual: JsonUtility exception about nested generics

Error message:
[Full error message from console]
```

#### For Feature Requests

**💡 Open a GitHub Discussion or Issue**

We love hearing your ideas! When suggesting features:

1. **Explain the use case**: Why do you need this feature?
2. **Describe the desired behavior**: What should it do?
3. **Consider alternatives**: Are there workarounds available?
4. **Be open to feedback**: Maintainers may suggest different approaches

**Example:**

```
Title: Add async/await support for Save and Load operations

Use case:
When saving large objects, the main thread freezes. It would be great
to have async versions of Save/Load for better performance.

Proposed API:
await PmPrefs.SaveAsync("key", largeObject);
var data = await PmPrefs.LoadAsync<T>("key");

Alternative considered:
Manual threading, but async/await would be cleaner and more modern.
```

#### For Pull Request Help

**🔄 Ask in Your PR**

If you're stuck while working on a pull request:

- **Comment on your own PR**: Describe what you're stuck on
- **Tag a maintainer**: Use `@username` to get attention (use sparingly)
- **Be specific**: "I'm not sure how to test this encryption change" is better than "need help"

#### Response Time Expectations

This is a volunteer-maintained project, so please be patient:

- **Issues**: Typically reviewed within 2-3 days
- **Pull Requests**: Initial review within 1-3 days
- **Questions**: Usually answered within 24-48 hours

If you haven't heard back after a week, feel free to add a polite follow-up comment.

#### Community Etiquette

When asking for help:

✅ **Do:**
- Search before asking
- Provide sufficient context
- Be patient and respectful
- Thank people who help you
- Share your solution if you figure it out

❌ **Don't:**
- Demand immediate responses
- Post the same question multiple times
- Ask to DM maintainers directly (keep discussions public so others can learn)
- Get frustrated if your feature request isn't immediately accepted

### Attribution

We value all contributions, no matter how small:

- 🐛 **Bug reports** are contributions
- 📝 **Documentation improvements** are contributions
- 💡 **Suggestions and feedback** are contributions
- 🧪 **Testing and verification** are contributions
- 💻 **Code changes** are contributions

All contributors will be acknowledged in release notes and the project's contributor list.

### Thank You!

Thank you for being part of the PmPrefs community! Your contributions—whether code, documentation, bug reports, or encouragement—make this project better for everyone.

Together, we're building a tool that helps Unity developers create better games. 🎮✨

---

**Questions or concerns about these guidelines?**

Open a GitHub Discussion or contact the maintainers. We're here to help make contributing a positive experience.

---

*Made by [ProjectMakers](https://projectmakers.de)*
