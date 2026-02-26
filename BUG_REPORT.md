# PmPrefs Bug Report - Variables Always Empty

**Date:** 2026-02-26
**Scope:** Full codebase analysis of all 6 C# source files
**Symptom:** PmPrefs variables do not save; they are always empty when loaded

---

## Executive Summary

Three critical bugs in the serialization and key-tracking layers cause **all stored values to be empty**. The root causes are: (1) `JsonUtility.ToJson()` destroying primitive values, (2) `HashSet<string>` being invisible to Unity's serializer, and (3) `Encrypt()` short-circuiting on the resulting empty strings. Additionally, 5 secondary bugs affect import/export and data integrity.

---

## Critical Bugs (Root Cause of Empty Variables)

### BUG-1: `JsonUtility.ToJson()` destroys primitive values [CRITICAL]

**File:** `Scripts/PmPrefs.cs`, line 454
**Severity:** Critical - Primary root cause

```csharp
public static void Save(string key, object value)
{
    if (string.IsNullOrEmpty(key)) return;
    string str = JsonUtility.ToJson(value);  // <-- BUG HERE
    AddKeyToList(key);
    PlayerPrefs.SetString(Prefix + key, Encrypt(str));
}
```

**Problem:** Unity's `JsonUtility.ToJson()` does **not** support primitive types (`string`, `int`, `float`, `bool`). For any non-`[Serializable]` class, it returns an empty string `""`.

**Reproduction:**
```csharp
JsonUtility.ToJson("hello")  // returns ""
JsonUtility.ToJson(42)        // returns ""
JsonUtility.ToJson(true)      // returns ""
JsonUtility.ToJson(3.14f)     // returns ""
```

**Impact:** Every call like `PmPrefs.Save("playerName", "John")` serializes `"John"` to `""`, encrypts `""` (which also returns `""` due to BUG-3), and stores an empty string. On load, the default value is returned.

**Fix:** Implement type-aware serialization that handles primitives before falling back to `JsonUtility.ToJson()` for complex objects. For example:
```csharp
private static string SerializeValue(object value)
{
    if (value == null) return "";
    if (value is string s) return s;
    if (value is int || value is float || value is double || value is bool || value is long)
        return value.ToString();
    return JsonUtility.ToJson(value);
}
```

---

### BUG-2: `HashSet<string>` is not serializable by `JsonUtility` [CRITICAL]

**File:** `Scripts/PmPrefs.cs`, lines 53-57
**Severity:** Critical - Breaks all key tracking

```csharp
[Serializable]
private class StringListWrapper
{
    public HashSet<string> items = new HashSet<string>();  // <-- BUG HERE
}
```

**Problem:** Unity's `JsonUtility` does **not** support `HashSet<T>`. It only supports `List<T>` and arrays. When `JsonUtility.ToJson()` is called on `StringListWrapper`, the `items` field is **silently skipped**, producing `{}`.

**Chain of failure:**
1. `SaveKeyList()` (line 215) calls `JsonUtility.ToJson(_listWrapper)` -> produces `{}`
2. `PlayerPrefs.SetString(KeyListKey, "{}")` stores empty JSON
3. On next load, `JsonUtility.FromJson<StringListWrapper>("{}")` creates instance with empty HashSet
4. Falls through to legacy format check, which also finds empty data
5. Creates new empty `StringListWrapper`

**Impact:** `GetAllKeys()` always returns an empty list. The fallback platform reader (`GetKeysFromTrackedList()`) produces nothing. Key tracking is completely non-functional.

**Fix:** Change `HashSet<string>` back to `List<string>` for serialization, or use a custom serialization approach:
```csharp
[Serializable]
private class StringListWrapper
{
    public List<string> items = new List<string>();
}

// Keep a runtime HashSet for O(1) lookups, sync with List for serialization
private static HashSet<string> _runtimeKeySet;
```

---

### BUG-3: `Encrypt("")` returns empty string [CRITICAL]

**File:** `Scripts/PmPrefs.cs`, lines 276-277
**Severity:** Critical - Compounds BUG-1

```csharp
public static string Encrypt(string plainText)
{
    if (string.IsNullOrEmpty(plainText))
        return string.Empty;  // <-- short-circuits on empty
    ...
}
```

**Problem:** When BUG-1 causes `JsonUtility.ToJson()` to return `""`, the `Encrypt()` method short-circuits and returns `string.Empty`. This means `PlayerPrefs.SetString(key, "")` stores an empty value.

**Chain:** `Save("key", "value")` -> `JsonUtility.ToJson("value")` = `""` -> `Encrypt("")` = `""` -> `PlayerPrefs.SetString("PmPrefs__key", "")` -> stored as empty

**On load:** `PlayerPrefs.GetString("PmPrefs__key")` = `""` -> `Decrypt("")` = `""` -> returns `defaultValue`

**Fix:** This guard is actually correct behavior for null/empty input. Fixing BUG-1 resolves this chain. However, consider whether empty strings should be a valid storable value (encrypt a sentinel instead of short-circuiting).

---

## Secondary Bugs

### BUG-4: Import functions use `PmPrefs.Save()` on raw strings

**Files:**
- `Editor/Code/PmPrefsEditorWindow.cs`, line 454 (CSV import)
- `Editor/Code/PmPrefsEditorWindow.cs`, line 499 (JSON import)

**CSV Import:**
```csharp
if (type == "PmPrefs")
{
    PmPrefs.Save(key, value);  // <-- string goes through JsonUtility.ToJson()
}
```

**JSON Import:**
```csharp
PmPrefs.Save(item.key, item.value ?? "");  // <-- same problem
```

**Problem:** Both import methods pass raw string values to `PmPrefs.Save()`, which routes them through `JsonUtility.ToJson()` (BUG-1), destroying the data. All imported data becomes empty.

**Fix:** Use `PmPrefs.SaveRaw()` instead of `PmPrefs.Save()` for import operations:
```csharp
PmPrefs.SaveRaw(key, value);
```
Note: `CreateNewPref()` at line 636 already correctly uses `SaveRaw()`.

---

### BUG-5: `ImportJson` missing `FlushKeyList()` before save

**File:** `Editor/Code/PmPrefsEditorWindow.cs`, line 522

```csharp
private void ImportJson(string importPath)
{
    ...
    // Import PmPrefs
    foreach (var item in importData.pmPrefs)
    {
        PmPrefs.Save(item.key, item.value ?? "");
    }
    ...
    PlayerPrefs.Save();  // <-- Missing FlushKeyList() before this!
    ...
}
```

**Compare with `ImportCsv` (correct):**
```csharp
PmPrefs.FlushKeyList();  // <-- present
PlayerPrefs.Save();
```

**Impact:** Key list changes from JSON import are never persisted. Even if other bugs were fixed, imported keys would be lost on next session.

**Fix:** Add `PmPrefs.FlushKeyList()` before `PlayerPrefs.Save()` in `ImportJson`.

---

### BUG-6: `Encrypt()` silently trims whitespace from data

**File:** `Scripts/PmPrefs.cs`, line 279

```csharp
var plainTextBytes = Encoding.UTF8.GetBytes(plainText.Trim());  // <-- .Trim()
```

**Problem:** The `.Trim()` call silently removes leading and trailing whitespace from data before encryption. If a user stores a value with intentional whitespace (e.g., formatted JSON with leading newlines), the whitespace is permanently lost.

**Impact:** Data integrity issue. Stored value differs from input value after round-trip.

**Fix:** Remove `.Trim()`:
```csharp
var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
```

---

### BUG-7: `ShowEncrypted` boolean naming is inverted

**File:** `Editor/Code/PrefsKeyReader.cs`, lines 63-73

```csharp
if (_editorWindow.ShowEncrypted)
{
    // Show decrypted value  <-- comment says "decrypted" but field is "ShowEncrypted"
    string encrypted = PlayerPrefs.GetString(keyName);
    strValue = PmPrefs.Decrypt(encrypted);
}
else
{
    // Show encrypted value
    strValue = PlayerPrefs.GetString(keyName);
}
```

**Problem:** When `ShowEncrypted = true`, the code shows **decrypted** values. The boolean name is the opposite of its behavior. This is confusing and bug-prone for future maintenance.

**Impact:** Code readability and maintenance risk. Not a functional bug since it's consistently inverted throughout, but makes the codebase harder to reason about.

**Fix:** Rename to `ShowDecrypted` and update all references, or flip the logic to match the name.

---

### BUG-8: `PmPrefs.SaveAll()` public API doesn't flush key list

**File:** `Scripts/PmPrefs.cs`, line 413

```csharp
public static void SaveAll() => PlayerPrefs.Save();
```

**Problem:** The public API method `SaveAll()` only calls `PlayerPrefs.Save()` without first flushing the key list. The XML doc comments even acknowledge this (line 401-403), telling users to manually call `FlushKeyList()`. This is a footgun API design.

**Impact:** Users calling the public API sequence `PmPrefs.Save("key", value); PmPrefs.SaveAll();` will lose their key list changes. The editor window's `SaveAll()` correctly calls `FlushKeyList()` first, but external callers won't know to do this.

**Fix:** Have `SaveAll()` call `FlushKeyList()` internally:
```csharp
public static void SaveAll()
{
    FlushKeyList();
    PlayerPrefs.Save();
}
```

---

## Bug Dependency Chain

```
User calls PmPrefs.Save("name", "John")
         |
         v
BUG-1: JsonUtility.ToJson("John") = ""
         |
         v
BUG-3: Encrypt("") = ""
         |
         v
PlayerPrefs.SetString("PmPrefs__name", "")
         |
         v
BUG-2: Key list (HashSet) serializes to {} -> key "name" not tracked
         |
         v
On load: HasKey works (PlayerPrefs has the key), but value is ""
         -> Decrypt("") = "" -> returns default value
         |
         v
Result: Variable appears empty
```

---

## Priority Fix Order

1. **BUG-1** - Fix serialization (unblocks all save operations)
2. **BUG-2** - Fix key list serialization (unblocks key tracking)
3. **BUG-3** - Consider empty string handling (auto-resolves with BUG-1 fix)
4. **BUG-4** - Fix import to use `SaveRaw()` (unblocks import)
5. **BUG-5** - Add missing `FlushKeyList()` in JSON import
6. **BUG-8** - Make `SaveAll()` flush internally
7. **BUG-6** - Remove `.Trim()` from Encrypt
8. **BUG-7** - Rename `ShowEncrypted` to `ShowDecrypted`

---

## Files Affected

| File | Bugs | Lines |
|------|------|-------|
| `Scripts/PmPrefs.cs` | BUG-1, BUG-2, BUG-3, BUG-6, BUG-8 | 454, 56, 276, 279, 413 |
| `Editor/Code/PmPrefsEditorWindow.cs` | BUG-4, BUG-5 | 454/499, 522 |
| `Editor/Code/PrefsKeyReader.cs` | BUG-7 | 63 |
| `Editor/Code/PmPrefsListItemEntryController.cs` | None | - |
| `Scripts/PmPrefsListItem.cs` | None | - |
| `Scripts/PmPrefsKeyAsset.cs` | None | - |
