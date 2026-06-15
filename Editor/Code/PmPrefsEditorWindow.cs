using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PM.Plugins
{
   /// <summary>
   /// Export format options for preferences.
   /// </summary>
   public enum ExportFormat
   {
      CSV,
      JSON
   }

   /// <summary>
   /// Editor window for managing PmPrefs and PlayerPrefs.
   /// Provides UI for viewing, editing, creating, and deleting preferences.
   /// </summary>
   public class PmPrefsEditorWindow : EditorWindow
   {
      // Wrapper classes for JSON serialization
      [Serializable]
      private class ExportData
      {
         public List<PreferenceItem> pmPrefs = new List<PreferenceItem>();
         public List<PreferenceItem> playerPrefs = new List<PreferenceItem>();
      }

      [Serializable]
      private class PreferenceItem
      {
         public string key;
         public string value;
      }

      // Cached compiled Regex patterns for performance
      private static readonly Regex AlphanumericValidationPattern = new Regex(@"^[a-zA-Z0-9]+$", RegexOptions.Compiled);
      private static readonly Regex IntegerPattern = new Regex(@"^-?(0|[1-9]\d*)$", RegexOptions.Compiled);
      private static readonly Regex FloatPattern = new Regex(@"^-?(0|[1-9]\d*)(\.\d+)?([eE][+-]?\d+)?$", RegexOptions.Compiled);

      // Shared toolbar selection colors
      private static readonly Color SelectedBg = new Color(.15f, .15f, .15f);
      private static readonly Color UnselectedBg = new Color(.235f, .235f, .235f);

      private VisualElement _root;

      private VisualTreeAsset _visualTreePmPrefsListItem;

      private ListView _listViewPmPrefsList;
      private ListView _listViewPlayerPrefsList;

      private VisualElement _createNewContainer;
      private VisualElement _configurationContainer;

      private Button _saveButton;
      private Button _deleteAllButton;
      private Button _createNewButton;
      private Button _createButton;
      private Button _configurationButton;
      private Button _showEncryptedButton;
      private Button _refreshButton;
      private Button _changeSecureKeyButton;
      private Button _projectMakersButton;

      private Button _showPmPrefsButton;
      private Button _showPlayerPrefsButton;

      private Button _exportButton;
      private Button _importButton;
      private Button _defaultJsonButton;

      private TextField _createNewKeyField;
      private TextField _createNewValueField;

      private TextField _changeSecureKeyField;

      private ToolbarSearchField _searchField;
      private string _currentSearchText = "";

      private EnumField _exportFormatField;

      private Label _unsavedChangesLabel;
      private Label _fallbackWarningLabel;

      private bool _showCreateNew;
      private bool _showConfig;

      private ExportFormat _selectedExportFormat = ExportFormat.CSV;

      /// <summary>
      /// When true, values are shown decrypted in the UI. When false, shows raw encrypted values.
      /// </summary>
      public bool ShowDecrypted;

      /// <summary>
      /// List of PlayerPrefs entries (non-PmPrefs).
      /// </summary>
      public List<PmPrefsListItem> PlayerPrefsList;

      /// <summary>
      /// List of PmPrefs entries.
      /// </summary>
      public List<PmPrefsListItem> PmPrefsList;

      private readonly PrefsKeyReader _prefsKeyReader;

      public PmPrefsEditorWindow()
      {
         _prefsKeyReader = new PrefsKeyReader(this);
      }

      /// <summary>
      /// Opens the PmPrefs editor window.
      /// </summary>
      [MenuItem("Tools/ProjectMakers/PmPrefs")]
      public static void ShowWindow()
      {
         PmPrefsEditorWindow wnd = GetWindow<PmPrefsEditorWindow>();
         wnd.titleContent = new GUIContent("PmPrefs");
         wnd.minSize = new Vector2(420, 520);
      }

      private void Initialize()
      {
         rootVisualElement.Clear();
         _root = rootVisualElement;

         var visualTree = LoadUxml("PmPrefs.uxml");

         if (visualTree == null)
         {
            Debug.LogError("[PmPrefs] Could not load PmPrefs.uxml. Please ensure the package is installed correctly.");
            return;
         }

         var labelFromUxml = visualTree.Instantiate();
         _root.Add(labelFromUxml);

         InitializeVisualElements();
         RefreshLists();

         _root.MarkDirtyRepaint();
         Repaint();
      }

      public void CreateGUI()
      {
         Initialize();

         saveChangesMessage = "PmPrefs has unsaved changes. Do you want to save them?";
      }

      /// <summary>
      /// Loads a UXML asset by file name, independent of where the package is installed
      /// (package folder, Assets/PmPrefs, Assets/Plugins/PmPrefs, ...).
      /// </summary>
      private static VisualTreeAsset LoadUxml(string fileName)
      {
         // Fast paths for the common install locations.
         var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
            $"Packages/com.projectmakers.pmprefs/Editor/Style/{fileName}");
         if (asset != null) return asset;

         asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"Assets/PmPrefs/Editor/Style/{fileName}");
         if (asset != null) return asset;

         // Robust fallback: locate the asset by name anywhere in the project or packages.
         string nameNoExt = Path.GetFileNameWithoutExtension(fileName);
         foreach (var guid in AssetDatabase.FindAssets($"{nameNoExt} t:VisualTreeAsset"))
         {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith("/" + fileName, StringComparison.Ordinal))
            {
               var found = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
               if (found != null) return found;
            }
         }

         return null;
      }

      private void InitializeVisualElements()
      {
         _visualTreePmPrefsListItem = LoadUxml("PmPrefsListItem.uxml");

         _listViewPmPrefsList = _root.Q<ListView>("PmPrefsList");
         _listViewPlayerPrefsList = _root.Q<ListView>("PlayerPrefsList");

         _saveButton = _root.Q<Button>("Save_btn");
         _deleteAllButton = _root.Q<Button>("DeleteAll_btn");
         _createNewButton = _root.Q<Button>("CreateNew_btn");
         _createButton = _root.Q<Button>("Create_btn");
         _changeSecureKeyButton = _root.Q<Button>("ChangeSecureKey_btn");
         _projectMakersButton = _root.Q<Button>("ProjectMakers_btn");

         _createNewKeyField = _root.Q<TextField>("CreateName_tf");
         _createNewValueField = _root.Q<TextField>("CreateValue_tf");
         _changeSecureKeyField = _root.Q<TextField>("ChangeSecureKey_tf");

         _refreshButton = _root.Q<Button>("Refresh_btn");
         _showEncryptedButton = _root.Q<Button>("ShowEncryp_btn");

         _configurationButton = _root.Q<Button>("Configuration_btn");
         _showPmPrefsButton = _root.Q<Button>("PmPrefs_btn");
         _showPlayerPrefsButton = _root.Q<Button>("PlayerPrefs_btn");

         _exportButton = _root.Q<Button>("Export_btn");
         _importButton = _root.Q<Button>("Import_btn");
         _defaultJsonButton = _root.Q<Button>("DefaultJson_btn");

         _createNewContainer = _root.Q<VisualElement>("Create");
         _configurationContainer = _root.Q<VisualElement>("Configuration");

         _searchField = _root.Q<ToolbarSearchField>("search_field");

         _exportFormatField = _root.Q<EnumField>("ExportFormat_enum");
         if (_exportFormatField != null)
         {
            _exportFormatField.Init(_selectedExportFormat);
            _exportFormatField.RegisterValueChangedCallback(evt =>
            {
               _selectedExportFormat = (ExportFormat)evt.newValue;
            });
         }

         _unsavedChangesLabel = _root.Q<Label>("UnsavedChanges_label");
         _fallbackWarningLabel = _root.Q<Label>("FallbackWarning_label");

         // Wire up event handlers
         _saveButton.clicked += SaveAll;
         _deleteAllButton.clicked += OnDeleteAllButtonClicked;
         _exportButton.clicked += OnExportButtonClicked;
         _importButton.clicked += OnImportButtonClicked;
         _createNewButton.clicked += OnCreateNewButtonClicked;
         _createButton.clicked += CreateNewPref;
         _changeSecureKeyButton.clicked += ChangeSecureKey;
         _defaultJsonButton.clicked += OnDefaultJsonButtonClicked;
         _configurationButton.clicked += OnConfigurationButtonClicked;
         _refreshButton.clicked += OnRefreshButtonClicked;
         _showEncryptedButton.clicked += OnShowDecryptedButtonClicked;
         _showPmPrefsButton.clicked += OnShowPmPrefsButtonClicked;
         _showPlayerPrefsButton.clicked += OnShowPlayerPrefsButtonClicked;
         if (_projectMakersButton != null)
         {
            _projectMakersButton.clicked += () => Application.OpenURL("https://projectmakers.de");
         }
         if (_searchField != null)
         {
            _searchField.RegisterValueChangedCallback(OnSearchFieldValueChanged);
         }

         // Add tooltips for better usability
         if (_saveButton != null) _saveButton.tooltip = "Save all changes to preferences";
         if (_deleteAllButton != null) _deleteAllButton.tooltip = "Delete all preferences (PmPrefs and PlayerPrefs)";
         if (_createNewButton != null) _createNewButton.tooltip = "Show/hide the create new preference panel";
         if (_configurationButton != null) _configurationButton.tooltip = "Show/hide configuration options";
         if (_refreshButton != null) _refreshButton.tooltip = "Refresh the preference lists";
         if (_showEncryptedButton != null) _showEncryptedButton.tooltip = "Toggle between encrypted and decrypted view";
         if (_exportButton != null) _exportButton.tooltip = "Export preferences to CSV or JSON file";
         if (_importButton != null) _importButton.tooltip = "Import preferences from CSV or JSON file";
         if (_exportFormatField != null) _exportFormatField.tooltip = "Select export file format (CSV or JSON)";
         if (_projectMakersButton != null) _projectMakersButton.tooltip = "Open projectmakers.de";

         // Reflect the initial state of the toolbar toggles.
         if (_showPmPrefsButton != null) _showPmPrefsButton.style.backgroundColor = new StyleColor(SelectedBg);
         if (_showPlayerPrefsButton != null) _showPlayerPrefsButton.style.backgroundColor = new StyleColor(UnselectedBg);
         if (_showEncryptedButton != null)
            _showEncryptedButton.style.backgroundColor = new StyleColor(ShowDecrypted ? SelectedBg : UnselectedBg);
      }

      private void OnDeleteAllButtonClicked()
      {
         if (EditorUtility.DisplayDialog("Delete All Keys",
            "Are you sure you want to delete all PmPrefs and PlayerPrefs?\n\nThis action cannot be undone!",
            "Yes, Delete All", "Cancel"))
         {
            PmPrefs.DeleteAll();
            PlayerPrefs.Save();

            _prefsKeyReader.InvalidateCache();
            RefreshLists();

            EditorUtility.DisplayDialog("Deleted", "All preferences have been deleted.", "OK");
         }
      }

      private void OnExportButtonClicked()
      {
         string extension = _selectedExportFormat == ExportFormat.JSON ? "json" : "csv";
         string defaultName = $"{DateTime.Now:yyyy-MM-dd}_PmPrefs_Export";
         var path = EditorUtility.SaveFilePanel("Export Preferences", "", defaultName, extension);

         if (string.IsNullOrEmpty(path)) return;

         try
         {
            Export(path);
            EditorUtility.DisplayDialog("Export Complete", $"Preferences exported to:\n{path}", "OK");
         }
         catch (Exception ex)
         {
            EditorUtility.DisplayDialog("Export Failed", $"Failed to export preferences:\n{ex.Message}", "OK");
         }
      }

      private void OnImportButtonClicked()
      {
         var path = EditorUtility.OpenFilePanel("Import Preferences", "", "csv,json");

         if (string.IsNullOrEmpty(path)) return;

         if (!EditorUtility.DisplayDialog("Import Preferences",
            "This will replace all existing PmPrefs with the imported data.\n\nRegular PlayerPrefs will NOT be affected.\n\nContinue?",
            "Import", "Cancel"))
         {
            return;
         }

         try
         {
            Import(path);
            EditorUtility.DisplayDialog("Import Complete", "Preferences imported successfully.", "OK");
         }
         catch (Exception ex)
         {
            EditorUtility.DisplayDialog("Import Failed",
               $"Failed to import preferences:\n{ex.Message}\n\nExisting data was left unchanged.", "OK");
         }
      }

      private void OnRefreshButtonClicked()
      {
         if (!ConfirmDiscardUnsavedChanges()) return;
         _prefsKeyReader.InvalidateCache();
         RefreshLists();
      }

      private void OnSearchFieldValueChanged(ChangeEvent<string> evt)
      {
         _currentSearchText = evt.newValue ?? "";
         FilterList(_listViewPmPrefsList, PmPrefsList, _currentSearchText);
         FilterList(_listViewPlayerPrefsList, PlayerPrefsList, _currentSearchText);
      }

      /// <summary>
      /// Filters a list by creating a filtered itemsSource based on search text.
      /// Uses case-insensitive substring matching without allocating lowercased copies.
      /// </summary>
      private void FilterList(ListView listView, List<PmPrefsListItem> sourceList, string searchText)
      {
         if (listView == null || sourceList == null) return;

         List<PmPrefsListItem> filtered;
         if (string.IsNullOrEmpty(searchText))
         {
            filtered = sourceList;
         }
         else
         {
            filtered = new List<PmPrefsListItem>();
            foreach (var item in sourceList)
            {
               bool keyMatch = item.Key != null &&
                  item.Key.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
               bool valueMatch = !keyMatch && item.Value != null &&
                  item.Value.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
               if (keyMatch || valueMatch)
               {
                  filtered.Add(item);
               }
            }
         }

         listView.itemsSource = filtered;
         listView.RefreshItems();
      }

      private void OnShowDecryptedButtonClicked()
      {
         if (!ConfirmDiscardUnsavedChanges()) return;

         ShowDecrypted = !ShowDecrypted;
         _showEncryptedButton.style.backgroundColor = new StyleColor(ShowDecrypted ? SelectedBg : UnselectedBg);

         RefreshLists();
      }

      private void Export(string path)
      {
         if (_selectedExportFormat == ExportFormat.JSON)
         {
            ExportJson(path);
         }
         else
         {
            ExportCsv(path);
         }
      }

      /// <summary>
      /// Returns the readable (decrypted) value for a PmPrefs list item, regardless of view mode.
      /// </summary>
      private string GetReadableValue(PmPrefsListItem item)
      {
         // When ShowDecrypted is true, item.Value is already the plaintext.
         // When false, item.Value is the raw stored ciphertext (the field is read-only in that
         // mode), so decrypting it in memory is correct and avoids a redundant PlayerPrefs read.
         return ShowDecrypted ? item.Value : PmPrefs.Decrypt(item.Value);
      }

      private void ExportCsv(string path)
      {
         var csv = new StringBuilder();

         foreach (var item in PmPrefsList)
         {
            string value = GetReadableValue(item);
            csv.AppendLine($"{CsvEscape("PmPrefs")};{CsvEscape(item.Key)};{CsvEscape(value)}");
         }

         foreach (var item in PlayerPrefsList)
         {
            csv.AppendLine($"{CsvEscape("PlayerPrefs")};{CsvEscape(item.Key)};{CsvEscape(item.Value)}");
         }

         File.WriteAllText(path, csv.ToString(), Encoding.UTF8);
      }

      private void ExportJson(string path)
      {
         var exportData = new ExportData();

         foreach (var item in PmPrefsList)
         {
            string value = GetReadableValue(item);
            exportData.pmPrefs.Add(new PreferenceItem { key = item.Key, value = value });
         }

         foreach (var item in PlayerPrefsList)
         {
            exportData.playerPrefs.Add(new PreferenceItem { key = item.Key, value = item.Value });
         }

         string json = JsonUtility.ToJson(exportData, true);
         File.WriteAllText(path, json, Encoding.UTF8);
      }

      private void Import(string importPath)
      {
         string extension = Path.GetExtension(importPath).ToLower();

         if (extension == ".json")
         {
            ImportJson(importPath);
         }
         else
         {
            ImportCsv(importPath);
         }
      }

      private void ImportCsv(string importPath)
      {
         // Parse fully into memory BEFORE deleting anything, so a failed/partial read never
         // destroys existing data.
         string text = File.ReadAllText(importPath, Encoding.UTF8);
         var records = ParseCsv(text);

         var pmEntries = new List<KeyValuePair<string, string>>();
         var playerEntries = new List<KeyValuePair<string, string>>();

         for (int i = 0; i < records.Count; i++)
         {
            var fields = records[i];
            if (fields.Count < 3) continue;

            string type = fields[0];
            string key = fields[1];
            string value = fields[2];

            if (string.IsNullOrEmpty(key)) continue;

            if (type == "PmPrefs")
               pmEntries.Add(new KeyValuePair<string, string>(key, value));
            else if (type == "PlayerPrefs")
               playerEntries.Add(new KeyValuePair<string, string>(key, value));
            else
               Debug.LogWarning($"[PmPrefs] Skipping CSV record {i + 1}: unknown type '{type}'.");
         }

         // Apply only after a successful parse.
         PmPrefs.DeleteAllPmPrefs();

         foreach (var entry in pmEntries)
            PmPrefs.SaveRaw(entry.Key, entry.Value);

         foreach (var entry in playerEntries)
            SetPlayerPrefAuto(entry.Key, entry.Value);

         PmPrefs.FlushKeyList();
         PlayerPrefs.Save();
         _prefsKeyReader.InvalidateCache();
         RefreshLists();
      }

      private void ImportJson(string importPath)
      {
         // Parse + validate BEFORE deleting anything.
         string json = File.ReadAllText(importPath, Encoding.UTF8);
         ExportData importData = JsonUtility.FromJson<ExportData>(json);

         if (importData == null)
         {
            throw new Exception("Failed to parse JSON file. Invalid format.");
         }

         // Apply only after a successful parse.
         PmPrefs.DeleteAllPmPrefs();

         if (importData.pmPrefs != null)
         {
            foreach (var item in importData.pmPrefs)
            {
               if (item == null || string.IsNullOrEmpty(item.key)) continue;
               PmPrefs.SaveRaw(item.key, item.value ?? "");
            }
         }

         if (importData.playerPrefs != null)
         {
            foreach (var item in importData.playerPrefs)
            {
               if (item == null || string.IsNullOrEmpty(item.key)) continue;
               SetPlayerPrefAuto(item.key, item.value ?? "");
            }
         }

         PmPrefs.FlushKeyList();
         PlayerPrefs.Save();
         _prefsKeyReader.InvalidateCache();
         RefreshLists();
      }

      /// <summary>
      /// Stores a regular PlayerPref, detecting int/float only for unambiguous numeric strings.
      /// Strings with leading zeros, thousands separators, or tokens like NaN/Infinity are kept
      /// as strings to avoid silently changing their type/precision.
      /// </summary>
      private static void SetPlayerPrefAuto(string key, string value)
      {
         value = value ?? "";

         if (IntegerPattern.IsMatch(value)
            && int.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int intVal))
         {
            PlayerPrefs.SetInt(key, intVal);
            return;
         }

         bool looksFractional = value.IndexOf('.') >= 0 || value.IndexOf('e') >= 0 || value.IndexOf('E') >= 0;
         if (looksFractional
            && FloatPattern.IsMatch(value)
            && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatVal)
            && !float.IsNaN(floatVal) && !float.IsInfinity(floatVal))
         {
            PlayerPrefs.SetFloat(key, floatVal);
            return;
         }

         PlayerPrefs.SetString(key, value);
      }

      // ----- CSV helpers (RFC-4180 style quoting; ';' delimiter) -----

      private static string CsvEscape(string field)
      {
         field = field ?? "";
         bool needsQuote = field.IndexOf(';') >= 0 || field.IndexOf('"') >= 0
            || field.IndexOf('\n') >= 0 || field.IndexOf('\r') >= 0;
         if (!needsQuote) return field;
         return "\"" + field.Replace("\"", "\"\"") + "\"";
      }

      /// <summary>
      /// Parses CSV text into records of fields, honoring double-quoted fields that may contain
      /// the ';' delimiter, embedded newlines, and escaped ("") quotes.
      /// </summary>
      private static List<List<string>> ParseCsv(string text)
      {
         var records = new List<List<string>>();
         if (text == null) return records;

         var record = new List<string>();
         var field = new StringBuilder();
         bool inQuotes = false;
         bool hasContent = false; // a field separator or any char was seen on the current record

         int i = 0;
         while (i < text.Length)
         {
            char c = text[i];

            if (inQuotes)
            {
               if (c == '"')
               {
                  if (i + 1 < text.Length && text[i + 1] == '"')
                  {
                     field.Append('"');
                     i += 2;
                     continue;
                  }
                  inQuotes = false;
                  i++;
                  continue;
               }
               field.Append(c);
               i++;
               continue;
            }

            if (c == '"')
            {
               inQuotes = true;
               hasContent = true;
               i++;
               continue;
            }
            if (c == ';')
            {
               record.Add(field.ToString());
               field.Clear();
               hasContent = true;
               i++;
               continue;
            }
            if (c == '\r')
            {
               i++;
               continue;
            }
            if (c == '\n')
            {
               record.Add(field.ToString());
               field.Clear();
               if (hasContent || record.Count > 1)
               {
                  records.Add(record);
               }
               record = new List<string>();
               hasContent = false;
               i++;
               continue;
            }

            field.Append(c);
            hasContent = true;
            i++;
         }

         // Trailing field/record without a final newline.
         if (hasContent || field.Length > 0 || record.Count > 0)
         {
            record.Add(field.ToString());
            if (hasContent || record.Count > 1 || field.Length > 0)
            {
               records.Add(record);
            }
         }

         return records;
      }

      private void OnConfigurationButtonClicked()
      {
         _showConfig = !_showConfig;

         if (_showConfig)
         {
            _configurationContainer.style.display = DisplayStyle.Flex;
            _configurationButton.style.backgroundColor = new StyleColor(SelectedBg);

            // Hide create panel
            _createNewContainer.style.display = DisplayStyle.None;
            _createNewButton.style.backgroundColor = new StyleColor(UnselectedBg);
            _showCreateNew = false;
         }
         else
         {
            _configurationContainer.style.display = DisplayStyle.None;
            _configurationButton.style.backgroundColor = new StyleColor(UnselectedBg);
         }
      }

      private void OnCreateNewButtonClicked()
      {
         _showCreateNew = !_showCreateNew;

         if (_showCreateNew)
         {
            _createNewContainer.style.display = DisplayStyle.Flex;
            _createNewButton.style.backgroundColor = new StyleColor(SelectedBg);

            // Hide config panel
            _configurationContainer.style.display = DisplayStyle.None;
            _configurationButton.style.backgroundColor = new StyleColor(UnselectedBg);
            _showConfig = false;
         }
         else
         {
            _createNewContainer.style.display = DisplayStyle.None;
            _createNewButton.style.backgroundColor = new StyleColor(UnselectedBg);
         }
      }

      private void OnShowPlayerPrefsButtonClicked()
      {
         _listViewPmPrefsList.style.display = DisplayStyle.None;
         _showPlayerPrefsButton.style.backgroundColor = new StyleColor(SelectedBg);
         _listViewPlayerPrefsList.style.display = DisplayStyle.Flex;
         _showPmPrefsButton.style.backgroundColor = new StyleColor(UnselectedBg);
      }

      private void OnDefaultJsonButtonClicked()
      {
         const string defaultJson = "{\n  \"key\": \"value\"\n}";

         if (!string.IsNullOrEmpty(_createNewValueField.value))
         {
            if (!EditorUtility.DisplayDialog("Replace Value",
               "This will replace the current value with a default JSON template.\n\nContinue?",
               "Replace", "Cancel"))
            {
               return;
            }
         }

         _createNewValueField.value = defaultJson;
      }

      private void OnShowPmPrefsButtonClicked()
      {
         _listViewPmPrefsList.style.display = DisplayStyle.Flex;
         _showPmPrefsButton.style.backgroundColor = new StyleColor(SelectedBg);
         _listViewPlayerPrefsList.style.display = DisplayStyle.None;
         _showPlayerPrefsButton.style.backgroundColor = new StyleColor(UnselectedBg);
      }

      private void CreateNewPref()
      {
         string key = _createNewKeyField.value;
         string value = _createNewValueField.value;

         if (string.IsNullOrWhiteSpace(key))
         {
            EditorUtility.DisplayDialog("Invalid Key", "Please enter a key name.", "OK");
            return;
         }

         if (PmPrefsList.Exists(t => t.Key == key))
         {
            EditorUtility.DisplayDialog("Key Exists", $"A preference with key '{key}' already exists.", "OK");
            return;
         }

         if (string.IsNullOrEmpty(value))
         {
            EditorUtility.DisplayDialog("Invalid Value", "Please enter a value.", "OK");
            return;
         }

         // Values are stored verbatim (matching the runtime API, which stores raw strings).
         PmPrefs.SaveRaw(key, value);
         PmPrefs.FlushKeyList();
         PlayerPrefs.Save();

         _createNewKeyField.value = "";
         _createNewValueField.value = "";

         _prefsKeyReader.InvalidateCache();
         RefreshLists();
      }

      private void FillList(ListView listView, List<PmPrefsListItem> items)
      {
         if (listView == null || _visualTreePmPrefsListItem == null) return;

         // Assign the item factories once; only the data source changes on subsequent refreshes.
         if (listView.makeItem == null)
         {
            listView.makeItem = () =>
            {
               var newListEntry = _visualTreePmPrefsListItem.Instantiate();
               var newListEntryLogic = new PmPrefsListItemEntryController();

               newListEntry.userData = newListEntryLogic;
               newListEntryLogic.SetVisualElement(newListEntry);
               newListEntryLogic.SetOnValueChangedCallback(UpdateUnsavedChangesIndicator);

               return newListEntry;
            };

            listView.bindItem = (item, index) =>
            {
               var source = (List<PmPrefsListItem>)listView.itemsSource;
               if (index >= 0 && index < source.Count)
               {
                  var controller = (PmPrefsListItemEntryController)item.userData;
                  controller.SetData(source[index]);
                  // Encrypted view shows raw ciphertext; keep it read-only so it cannot be
                  // hand-edited into corrupted data.
                  controller.SetValueEditable(ShowDecrypted);
               }
            };
         }

         listView.itemsSource = items;
      }

      private bool HasUnsavedChanges()
      {
         // Check PmPrefs list for changes
         for (var i = 0; i < PmPrefsList.Count; i++)
         {
            var pref = PmPrefsList[i];
            if (pref.Changed || pref.DeleteMarker)
            {
               return true;
            }
         }

         // Check PlayerPrefs list for changes
         for (var i = 0; i < PlayerPrefsList.Count; i++)
         {
            var pref = PlayerPrefsList[i];
            if (pref.Changed || pref.DeleteMarker)
            {
               return true;
            }
         }

         return false;
      }

      /// <summary>
      /// If there are unsaved changes, prompts the user to Save, Discard, or Cancel.
      /// Returns true if the caller should proceed (changes saved or discarded), false to abort.
      /// </summary>
      private bool ConfirmDiscardUnsavedChanges()
      {
         if (PmPrefsList == null || PlayerPrefsList == null) return true;
         if (!HasUnsavedChanges()) return true;

         int choice = EditorUtility.DisplayDialogComplex("Unsaved Changes",
            "You have unsaved changes that will be lost.\n\nSave them before continuing?",
            "Save", "Cancel", "Discard");

         if (choice == 0) // Save
         {
            SaveAll();
            return true;
         }
         if (choice == 2) // Discard
         {
            return true;
         }
         return false; // Cancel
      }

      private void UpdateUnsavedChangesIndicator()
      {
         bool unsaved = HasUnsavedChanges();
         hasUnsavedChanges = unsaved;

         if (_unsavedChangesLabel == null)
         {
            return;
         }

         if (unsaved)
         {
            _unsavedChangesLabel.text = "Unsaved Changes";
            _unsavedChangesLabel.style.display = DisplayStyle.Flex;
            _unsavedChangesLabel.style.color = new StyleColor(new Color(1f, 0.647f, 0f)); // Orange warning color
         }
         else
         {
            _unsavedChangesLabel.style.display = DisplayStyle.None;
         }
      }

      private void UpdateFallbackWarning()
      {
         if (_fallbackWarningLabel == null) return;

         if (_prefsKeyReader != null && _prefsKeyReader.UsedFallback)
         {
            _fallbackWarningLabel.text =
               "Could not read platform storage - showing tracked PmPrefs keys only. Regular PlayerPrefs may be missing.";
            _fallbackWarningLabel.style.display = DisplayStyle.Flex;
         }
         else
         {
            _fallbackWarningLabel.style.display = DisplayStyle.None;
         }
      }

      private void SaveAll()
      {
         int savedCount = 0;
         int deletedCount = 0;

         // Save PmPrefs changes
         for (var i = PmPrefsList.Count - 1; i >= 0; i--)
         {
            var pref = PmPrefsList[i];

            if (pref.DeleteMarker)
            {
               PmPrefs.DeleteKey(pref.Key);
               PmPrefsList.RemoveAt(i);
               deletedCount++;
               continue;
            }

            if (pref.Changed)
            {
               // If showing decrypted, the value needs to be encrypted on save.
               if (ShowDecrypted)
               {
                  PlayerPrefs.SetString(PmPrefs.Prefix + pref.Key, PmPrefs.Encrypt(pref.Value));
               }
               else
               {
                  // Encrypted view is read-only, so this path is not normally reached; persist
                  // the (already-encrypted) value as-is for safety.
                  PlayerPrefs.SetString(PmPrefs.Prefix + pref.Key, pref.Value);
               }
               pref.Save();
               savedCount++;
            }
         }

         // Save PlayerPrefs changes
         for (var i = PlayerPrefsList.Count - 1; i >= 0; i--)
         {
            var pref = PlayerPrefsList[i];

            if (pref.DeleteMarker)
            {
               PlayerPrefs.DeleteKey(pref.Key);
               PlayerPrefsList.RemoveAt(i);
               deletedCount++;
               continue;
            }

            if (pref.Changed)
            {
               SetPlayerPrefAuto(pref.Key, pref.Value);
               pref.Save();
               savedCount++;
            }
         }

         // Flush key list changes, then save all to disk once
         PmPrefs.FlushKeyList();
         PlayerPrefs.Save();
         _prefsKeyReader.InvalidateCache();

         // Rebuild the (possibly filtered) item sources so deleted rows disappear immediately.
         ApplyCurrentFilter();

         if (savedCount > 0 || deletedCount > 0)
         {
            Debug.Log($"[PmPrefs] Saved {savedCount} items, deleted {deletedCount} items.");
         }

         UpdateUnsavedChangesIndicator();
      }

      private void RefreshLists()
      {
         // Initialize lists on first call, otherwise clear and reuse
         if (PmPrefsList == null)
            PmPrefsList = new List<PmPrefsListItem>();
         else
            PmPrefsList.Clear();

         if (PlayerPrefsList == null)
            PlayerPrefsList = new List<PmPrefsListItem>();
         else
            PlayerPrefsList.Clear();

         _prefsKeyReader.GetKeys();

         if (_listViewPmPrefsList != null)
            FillList(_listViewPmPrefsList, PmPrefsList);

         if (_listViewPlayerPrefsList != null)
            FillList(_listViewPlayerPrefsList, PlayerPrefsList);

         // Re-apply the current filter after refreshing lists
         ApplyCurrentFilter();
         UpdateFallbackWarning();
         UpdateUnsavedChangesIndicator();
      }

      /// <summary>
      /// Re-applies the current search filter to both list views and refreshes them.
      /// </summary>
      private void ApplyCurrentFilter()
      {
         FilterList(_listViewPmPrefsList, PmPrefsList, _currentSearchText);
         FilterList(_listViewPlayerPrefsList, PlayerPrefsList, _currentSearchText);
      }

      private void ChangeSecureKey()
      {
         var key = _changeSecureKeyField.value;

         if (string.IsNullOrEmpty(key) || key.Length < 8)
         {
            EditorUtility.DisplayDialog("Invalid Key",
               "The secure key must be at least 8 characters long.", "OK");
            return;
         }

         if (!AlphanumericValidationPattern.IsMatch(key))
         {
            EditorUtility.DisplayDialog("Invalid Key",
               "The secure key must contain only alphanumeric characters (a-z, A-Z, 0-9).", "OK");
            return;
         }

         if (!EditorUtility.DisplayDialog("Change Secure Key",
            "WARNING: Changing the secure key will invalidate all existing PmPrefs data!\n\n" +
            "All encrypted data will be deleted because it cannot be decrypted with the new key.\n\n" +
            "Are you sure you want to continue?",
            "Yes, Change Key", "Cancel"))
         {
            return;
         }

         // 1) Persist the new key FIRST. If this fails, abort WITHOUT deleting any data.
         if (!TryWriteSecureKey(key, out string error))
         {
            EditorUtility.DisplayDialog("Error",
               $"Could not save the new secure key:\n{error}\n\nNo data was deleted.", "OK");
            return;
         }

         // 2) Activate the new key and clear data that can no longer be decrypted.
         PmPrefs.RefreshSecureKey();
         PmPrefs.DeleteAllPmPrefs();
         PlayerPrefs.Save();
         PmPrefs.RefreshKeyCache();

         _changeSecureKeyField.value = "";
         _prefsKeyReader.InvalidateCache();
         RefreshLists();

         EditorUtility.DisplayDialog("Success",
            "The secure key has been changed.\n\n" +
            "Existing PmPrefs data was cleared because it cannot be decrypted with the new key.", "OK");
      }

      /// <summary>
      /// Writes the secure key into a writable <see cref="PmPrefsKeyAsset"/> config asset
      /// (created under Assets/ if necessary). Returns false with an error message on failure.
      /// </summary>
      private bool TryWriteSecureKey(string key, out string error)
      {
         error = null;
         try
         {
            var asset = FindOrCreateConfigAsset();
            if (asset == null)
            {
               error = "Could not create the PmPrefs config asset.";
               return false;
            }

            asset.secureKey = key;
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return true;
         }
         catch (Exception ex)
         {
            error = ex.Message;
            return false;
         }
      }

      /// <summary>
      /// Finds an existing writable PmPrefsKeyAsset under Assets/, or creates one in
      /// Assets/PmPrefs/Resources so it is included in builds and is writable for all install types.
      /// </summary>
      private static PmPrefsKeyAsset FindOrCreateConfigAsset()
      {
         // Only reuse an asset that the runtime can actually load: under Assets/ AND inside a
         // Resources folder (Resources.LoadAll cannot see assets outside Resources).
         var guids = AssetDatabase.FindAssets("t:PmPrefsKeyAsset");
         foreach (var guid in guids)
         {
            string path = AssetDatabase.GUIDToAssetPath(guid).Replace('\\', '/');
            if (path.StartsWith("Assets/") && path.Contains("/Resources/"))
            {
               var existing = AssetDatabase.LoadAssetAtPath<PmPrefsKeyAsset>(path);
               if (existing != null) return existing;
            }
         }

         const string dir = "Assets/PmPrefs/Resources";
         if (!Directory.Exists(dir))
         {
            Directory.CreateDirectory(dir);
         }

         var asset = ScriptableObject.CreateInstance<PmPrefsKeyAsset>();
         string assetPath = dir + "/PmPrefsKeyAsset.asset";
         AssetDatabase.CreateAsset(asset, assetPath);
         AssetDatabase.SaveAssets();
         AssetDatabase.ImportAsset(assetPath);
         return asset;
      }

      /// <summary>
      /// Override SaveChanges to handle the "Save" option when closing the window with unsaved changes.
      /// Called by Unity when the user chooses to save before closing.
      /// </summary>
      public override void SaveChanges()
      {
         SaveAll();
         base.SaveChanges();
      }

      /// <summary>
      /// Override DiscardChanges to handle the "Don't Save" option when closing the window with unsaved changes.
      /// Called by Unity when the user chooses to discard changes before closing.
      /// </summary>
      public override void DiscardChanges()
      {
         // Refresh lists to discard all pending changes
         _prefsKeyReader.InvalidateCache();
         RefreshLists();
         base.DiscardChanges();
      }
   }
}
