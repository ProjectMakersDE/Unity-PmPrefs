# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [2.4.1] - 2026-02-08

### Added
- "Insert Default JSON" button in Create panel to populate the value field with a template JSON structure
- Confirmation dialog when replacing existing value with default JSON template

### Changed
- Create panel value field is now multiline with increased height (80px) for better JSON editing experience

## [2.4.0] - 2026-02-07

### Added
- `PmPrefs.SaveRaw()` method for saving raw string values with encryption, bypassing JsonUtility serialization
- JSON validation on Create and Save — values must be syntactically valid JSON before they can be stored
- Instant save on Create — new preferences are persisted to disk immediately

### Fixed
- Fixed Create panel producing empty/corrupted values because `JsonUtility.ToJson(string)` does not work with plain strings
- Fixed PlayerPrefs tab showing raw/encoded data instead of actual values — now reads through the Unity PlayerPrefs API instead of parsing platform storage directly

### Changed
- Improved list layout: Key column now uses a fixed width for consistent row alignment
- Redesigned list header with bold labels and proper column alignment (Key, Value, Delete)
- Fixed ListView container alignment (`stretch` instead of `flex-end`) so list items fill the full width

## [2.3.0] - 2026-02-07

### Removed
- Removed test helper tools from the Editor menu (`PmPrefsTestHelper`, `PmPrefsBackwardCompatibilityTest`, `PmPrefsPerformanceTest`) — this is a library package, not a test project
- Removed old German documentation files (`Dokumentation.docx`, `Dokumentation.pdf`) — superseded by the English README

### Fixed
- Fixed **Tools > ProjectMakers > PmPrefs** menu item not opening the editor window (test sub-menu items were overriding it)

### Changed
- Updated CONTRIBUTING.md project structure to reflect actual file layout
- Simplified testing documentation in CONTRIBUTING.md

## [2.2.2] - 2026-02-07

### Fixed
- Fixed `hasUnsavedChanges` override causing CS0506 compile error — property is not virtual in `EditorWindow`, now uses the protected setter instead
- Removed duplicate `OnSearchFieldValueChanged` method causing CS0111 compile error
- Fixed duplicate search field callback registration

## [2.2.1] - 2026-02-07

### Fixed
- Resolved merge conflict markers in `PmPrefs.cs`, `PmPrefsEditorWindow.cs`, `PrefsKeyReader.cs`, and `.gitignore`
- Removed orphaned `GetWindowsKeys.cs.meta` (source file was already deleted)
- Added missing `.meta` files for `PrefsKeyReader.cs` and editor scripts
- Removed leftover `verification_report.txt` from project root

## [2.2.0] - 2026-02-05

### Added
- Cross-platform Editor support (Windows, macOS, Linux)
- `DeleteAllPmPrefs()` method to delete only PmPrefs entries without affecting regular PlayerPrefs
- `RefreshKeyCache()` method to manually refresh the internal key cache
- XML documentation comments on all public API methods
- Tooltips on all Editor window buttons
- Error feedback dialogs for import/export operations
- Key caching with timeout for better Editor performance

### Changed
- Replaced deprecated `RijndaelManaged` with modern `Aes.Create()` API
- Import now only deletes PmPrefs entries, preserving regular PlayerPrefs
- Export now always exports readable (decrypted) values
- Improved error handling throughout the codebase
- Updated README to English with comprehensive documentation
- Optimized UI refresh - SaveAll no longer rebuilds entire UI
- Optimized key tracking by replacing `List<string>` with `HashSet<string>` for O(1) lookups

### Fixed
- Fixed JsonUtility not supporting `List<string>` - now uses wrapper class
- Fixed KeyList not using the correct prefix
- Fixed StreamReader not being disposed properly in Import
- Fixed empty string causing crash in Decrypt method
- Fixed FindFile searching wrong directory for PmPrefs.cs
- Fixed event handler registration causing duplicate callbacks
- Fixed potential memory leaks from undisposed crypto transforms

### Removed
- Removed Windows-only `GetWindowsKeys.cs` (replaced by cross-platform `PrefsKeyReader.cs`)
- Removed confusing "single element bug" warning dialog

## [2.1.0] - 2023-09-20

### Added
- Initial public release
- AES encryption for saved data
- Editor window for managing preferences
- Import/Export to CSV
- Support for changing encryption key

### Known Issues
- Editor window only works on Windows (fixed in 2.2.0)
