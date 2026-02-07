# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [2.2.1] - 2026-02-07

### Fixed
- Resolved merge conflict markers committed in `PmPrefs.cs` (`AddKeyToList` and `RemoveKeyFromList`)
- Removed orphaned `GetWindowsKeys.cs.meta` (source file was already deleted)
- Added missing `.meta` files for `PrefsKeyReader.cs`, `PmPrefsPerformanceTest.cs`, and `PmPrefsTestHelper.cs`
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
