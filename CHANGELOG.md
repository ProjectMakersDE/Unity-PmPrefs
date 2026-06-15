# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [2.5.0] - 2026-06-15

### Added
- Runtime-configurable encryption key: the key is now read from a `PmPrefsKeyAsset` config asset in any `Resources` folder (falls back to the built-in default). Changing the key in the editor no longer rewrites source code and works for read-only (Package Manager / git URL) installs.
- New encrypted format (v2) with a random IV per value, removing the previous deterministic-ciphertext weakness. Legacy data written by older versions is still decrypted transparently; values migrate to v2 as they are re-saved.
- `PmPrefs.RefreshSecureKey()` to re-resolve the active key after it changes.
- Warning banner in the editor window when platform storage cannot be read and only tracked PmPrefs keys are shown.

### Fixed
- **Data loss:** a user key named `KeyList` no longer collides with and corrupts the internal key registry; the registry is stored under a non-colliding key and migrated automatically.
- **Data loss:** `Save()`/`Load()` are now symmetric — enums, `decimal`, `char`, and all integer primitives (`byte`/`sbyte`/`short`/`ushort`/`uint`/`ulong`) round-trip correctly instead of silently returning the default.
- **Data loss:** empty/`null` string values now round-trip instead of being indistinguishable from a missing key.
- **Data loss:** CSV export/import now uses RFC-4180 quoting, so values with newlines (including the default JSON template) or `;`, and keys containing `;`, survive a round-trip.
- **Data loss:** import now fully parses the file before deleting existing data, so a malformed/locked import file no longer wipes your preferences.
- **Data loss:** "Change Secure Key" writes the new key before deleting data and aborts safely if it cannot be saved.
- macOS: the editor reads the correct plist filename (company/product names verbatim) and converts binary plists via `plutil`, so regular PlayerPrefs are visible again.
- Encrypted-view value fields are read-only, preventing accidental corruption of raw ciphertext.
- Toggling the encrypted/decrypted view or clicking Refresh now prompts before discarding unsaved edits.
- Editing a deleted entry while a search filter is active no longer leaves ghost rows.
- PlayerPrefs type auto-detection on import/save no longer silently converts strings like `007`, `1,000`, `NaN`, or `Infinity` into numbers.
- Editor no longer forces values to be JSON; plain strings created/edited in the editor now match the runtime API's storage exactly.
- Linux/Windows key reading hardened (XML-entity decoding, attribute-order independence, validated registry suffix stripping).
- Copy-to-clipboard buttons show a brief, visible confirmation flash and no longer lose their background styling.

### Changed
- List column headers align with the actual row layout; long keys are fully readable via tooltip; multiline values size correctly.
- Search filtering is allocation-light (case-insensitive substring match) for snappier typing on large key sets.
- Encrypt/Decrypt cache derived key material; export reuses already-loaded values instead of re-reading PlayerPrefs.
- The "ProjectMakers" footer button opens projectmakers.de; active tab/view state is highlighted on open.
- Minimum window size increased and panels no longer collapse the preference list at small sizes.
- Declared minimum Unity version corrected to 2021.3 LTS to match the UI Toolkit APIs the editor window already required (it was inaccurately listed as 2018.1).

### Security
- AES key derivation for the v2 format uses 100,000 PBKDF2 iterations (up from the framework default of 1,000) and UTF-8 salt encoding.
- **Note:** the key is still derivable from the build; PmPrefs remains "security through obscurity" suitable for game data, not for secrets. See the Security Note and FAQ in the README.

### Upgrade notes
- If you previously changed the encryption key by editing the `SecureKey` constant in `PmPrefs.cs`, set the same key again via **Configuration > Secure Key** after updating (source edits are overwritten by package updates). Data encrypted with that key will then decrypt normally.
- `PmPrefs.SecureKey` is now a read-only property (resolved at runtime) instead of a `const`. The built-in default key is exposed as `PmPrefs.DefaultSecureKey`.

## [2.4.2] - 2026-02-27

### Fixed
- Fixed 8 bugs causing PmPrefs variables to always be empty (JsonUtility primitive serialization, HashSet serialization, Encrypt short-circuit, import using Save instead of SaveRaw, missing FlushKeyList in JSON import, Encrypt trimming whitespace, SaveAll not flushing key list)
- Fixed search filter not working correctly
- Fixed locale parsing issue
- Fixed NullReferenceException after cache reset

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
