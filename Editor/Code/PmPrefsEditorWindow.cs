using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
      private static readonly Regex QuotedStringPattern = new Regex(@"""([^""]*)""", RegexOptions.Compiled);

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

      private bool _showCreateNew;
      private bool _showConfig;

      private ExportFormat _selectedExportFormat = ExportFormat.CSV;

      /// <summary>
      /// When true, shows decrypted values. When false, shows encrypted values.
      /// </summary>
      public bool ShowEncrypted;

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
         wnd.minSize = new Vector2(380, 356);
      }

      private void Initialize()
      {
         rootVisualElement.Clear();
         _root = rootVisualElement;

         var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
            "Packages/com.projectmakers.pmprefs/Editor/Style/PmPrefs.uxml");

         if (visualTree == null)
         {
            // Fallback: Search in Assets folder (for development)
            visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
               "Assets/PmPrefs/Editor/Style/PmPrefs.uxml");
         }

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

      private void InitializeVisualElements()
      {
         _visualTreePmPrefsListItem = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
            "Packages/com.projectmakers.pmprefs/Editor/Style/PmPrefsListItem.uxml");

         if (_visualTreePmPrefsListItem == null)
         {
            _visualTreePmPrefsListItem = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
               "Assets/PmPrefs/Editor/Style/PmPrefsListItem.uxml");
         }

         _listViewPmPrefsList = _root.Q<ListView>("PmPrefsList");
         _listViewPlayerPrefsList = _root.Q<ListView>("PlayerPrefsList");

         _saveButton = _root.Q<Button>("Save_btn");
         _deleteAllButton = _root.Q<Button>("DeleteAll_btn");
         _createNewButton = _root.Q<Button>("CreateNew_btn");
         _createButton = _root.Q<Button>("Create_btn");
         _changeSecureKeyButton = _root.Q<Button>("ChangeSecureKey_btn");

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
         _showEncryptedButton.clicked += OnShowEncryptedButtonClicked;
         _showPmPrefsButton.clicked += OnShowPmPrefsButtonClicked;
         _showPlayerPrefsButton.clicked += OnShowPlayerPrefsButtonClicked;
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
            EditorUtility.DisplayDialog("Import Failed", $"Failed to import preferences:\n{ex.Message}", "OK");
         }
      }

      private void OnRefreshButtonClicked()
      {
         _prefsKeyReader.InvalidateCache();
         RefreshLists();
      }

      private void OnSearchFieldValueChanged(ChangeEvent<string> evt)
      {
         _currentSearchText = evt.newValue ?? "";
         string searchText = _currentSearchText.ToLower();
         FilterList(_listViewPmPrefsList, PmPrefsList, searchText);
         FilterList(_listViewPlayerPrefsList, PlayerPrefsList, searchText);
      }

      /// <summary>
      /// Filters a list by creating a filtered itemsSource based on search text.
      /// </summary>
      /// <param name="listView">The list view to filter.</param>
      /// <param name="sourceList">The complete source list.</param>
      /// <param name="searchText">The search text (already lowercase).</param>
      private void FilterList(ListView listView, List<PmPrefsListItem> sourceList, string searchText)
      {
         if (listView == null || sourceList == null) return;

         // If search is empty, show all items
         bool showAll = string.IsNullOrEmpty(searchText);

         List<PmPrefsListItem> filtered;
         if (showAll)
         {
            filtered = sourceList;
         }
         else
         {
            filtered = sourceList.Where(item =>
            {
               string key = item.Key?.ToLower() ?? "";
               string value = item.Value?.ToLower() ?? "";
               return key.Contains(searchText) || value.Contains(searchText);
            }).ToList();
         }

         listView.itemsSource = filtered;
         listView.RefreshItems();
      }

      private void OnShowEncryptedButtonClicked()
      {
         ShowEncrypted = !ShowEncrypted;
         _showEncryptedButton.style.backgroundColor = ShowEncrypted
            ? new StyleColor(new Color(.15f, .15f, .15f))
            : new StyleColor(new Color(.235f, .235f, .235f));

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

      private void ExportCsv(string path)
      {
         var csv = new StringBuilder();

         foreach (var item in PmPrefsList)
         {
            // When ShowEncrypted is true, the value is already decrypted in the list
            // When ShowEncrypted is false, we need to decrypt for export (export should be readable)
            string value = ShowEncrypted ? item.Value : PmPrefs.Decrypt(PlayerPrefs.GetString(PmPrefs.Prefix + item.Key));
            csv.AppendLine($"PmPrefs;{item.Key};{value}");
         }

         foreach (var item in PlayerPrefsList)
         {
            csv.AppendLine($"PlayerPrefs;{item.Key};{item.Value}");
         }

         File.WriteAllText(path, csv.ToString(), Encoding.UTF8);
      }

      private void ExportJson(string path)
      {
         var exportData = new ExportData();

         foreach (var item in PmPrefsList)
         {
            // When ShowEncrypted is true, the value is already decrypted in the list
            // When ShowEncrypted is false, we need to decrypt for export (export should be readable)
            string value = ShowEncrypted ? item.Value : PmPrefs.Decrypt(PlayerPrefs.GetString(PmPrefs.Prefix + item.Key));
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
         // Only delete PmPrefs, not all PlayerPrefs
         PmPrefs.DeleteAllPmPrefs();

         using (var reader = new StreamReader(File.OpenRead(importPath), Encoding.UTF8))
         {
            int lineNumber = 0;
            while (!reader.EndOfStream)
            {
               lineNumber++;
               var line = reader.ReadLine();
               if (string.IsNullOrWhiteSpace(line)) continue;

               var parts = line.Split(new[] { ';' }, 3);
               if (parts.Length < 3)
               {
                  Debug.LogWarning($"[PmPrefs] Skipping invalid line {lineNumber}: {line}");
                  continue;
               }

               var type = parts[0];
               var key = parts[1];
               var value = parts[2];

               if (type == "PmPrefs")
               {
                  // Save through PmPrefs API (auto-encrypts)
                  PmPrefs.Save(key, value);
               }
               else if (type == "PlayerPrefs")
               {
                  // Try to detect type and save appropriately
                  if (int.TryParse(value, out int intVal))
                     PlayerPrefs.SetInt(key, intVal);
                  else if (float.TryParse(value, out float floatVal))
                     PlayerPrefs.SetFloat(key, floatVal);
                  else
                     PlayerPrefs.SetString(key, value);
               }
            }
         }

         // Flush key list changes, then save all to disk once
         PmPrefs.FlushKeyList();
         PlayerPrefs.Save();
         _prefsKeyReader.InvalidateCache();
         RefreshLists();
      }

      private void ImportJson(string importPath)
      {
         // Only delete PmPrefs, not all PlayerPrefs
         PmPrefs.DeleteAllPmPrefs();
         PlayerPrefs.Save();

         string json = File.ReadAllText(importPath, Encoding.UTF8);
         ExportData importData = JsonUtility.FromJson<ExportData>(json);

         if (importData == null)
         {
            throw new Exception("Failed to parse JSON file. Invalid format.");
         }

         // Import PmPrefs
         if (importData.pmPrefs != null)
         {
            foreach (var item in importData.pmPrefs)
            {
               if (string.IsNullOrEmpty(item.key)) continue;

               // Save through PmPrefs API (auto-encrypts)
               PmPrefs.Save(item.key, item.value ?? "");
            }
         }

         // Import PlayerPrefs
         if (importData.playerPrefs != null)
         {
            foreach (var item in importData.playerPrefs)
            {
               if (string.IsNullOrEmpty(item.key)) continue;

               string value = item.value ?? "";

               // Try to detect type and save appropriately
               if (int.TryParse(value, out int intVal))
                  PlayerPrefs.SetInt(item.key, intVal);
               else if (float.TryParse(value, out float floatVal))
                  PlayerPrefs.SetFloat(item.key, floatVal);
               else
                  PlayerPrefs.SetString(item.key, value);
            }
         }

         PlayerPrefs.Save();
         _prefsKeyReader.InvalidateCache();
         RefreshLists();
      }

      private void OnConfigurationButtonClicked()
      {
         _showConfig = !_showConfig;

         if (_showConfig)
         {
            _configurationContainer.style.display = DisplayStyle.Flex;
            _configurationButton.style.backgroundColor = new StyleColor(new Color(.15f, .15f, .15f));

            // Hide create panel
            _createNewContainer.style.display = DisplayStyle.None;
            _createNewButton.style.backgroundColor = new StyleColor(new Color(.235f, .235f, .235f));
            _showCreateNew = false;
         }
         else
         {
            _configurationContainer.style.display = DisplayStyle.None;
            _configurationButton.style.backgroundColor = new StyleColor(new Color(.235f, .235f, .235f));
         }
      }

      private void OnCreateNewButtonClicked()
      {
         _showCreateNew = !_showCreateNew;

         if (_showCreateNew)
         {
            _createNewContainer.style.display = DisplayStyle.Flex;
            _createNewButton.style.backgroundColor = new StyleColor(new Color(.15f, .15f, .15f));

            // Hide config panel
            _configurationContainer.style.display = DisplayStyle.None;
            _configurationButton.style.backgroundColor = new StyleColor(new Color(.235f, .235f, .235f));
            _showConfig = false;
         }
         else
         {
            _createNewContainer.style.display = DisplayStyle.None;
            _createNewButton.style.backgroundColor = new StyleColor(new Color(.235f, .235f, .235f));
         }
      }

      private void OnShowPlayerPrefsButtonClicked()
      {
         _listViewPmPrefsList.style.display = DisplayStyle.None;
         _showPlayerPrefsButton.style.backgroundColor = new StyleColor(new Color(.15f, .15f, .15f));
         _listViewPlayerPrefsList.style.display = DisplayStyle.Flex;
         _showPmPrefsButton.style.backgroundColor = new StyleColor(new Color(.235f, .235f, .235f));
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
         _showPmPrefsButton.style.backgroundColor = new StyleColor(new Color(.15f, .15f, .15f));
         _listViewPlayerPrefsList.style.display = DisplayStyle.None;
         _showPlayerPrefsButton.style.backgroundColor = new StyleColor(new Color(.235f, .235f, .235f));
      }

      private void CreateNewPref()
      {
         string key = _createNewKeyField.text;
         string value = _createNewValueField.text;

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

         if (string.IsNullOrWhiteSpace(value))
         {
            EditorUtility.DisplayDialog("Invalid Value", "Please enter a value.", "OK");
            return;
         }

         if (!IsValidJson(value))
         {
            EditorUtility.DisplayDialog("Invalid JSON",
               "The value must be valid JSON.\n\nExamples:\n" +
               "  {\"name\": \"John\", \"score\": 100}\n" +
               "  {\"enabled\": true}\n" +
               "  \"simple string\"", "OK");
            return;
         }

         // Save raw value directly (bypassing JsonUtility.ToJson which corrupts plain strings)
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

         listView.Clear();

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
            if (index >= 0 && index < items.Count)
            {
               ((PmPrefsListItemEntryController)item.userData).SetData(items[index]);
            }
         };

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

      private void SaveAll()
      {
         int savedCount = 0;
         int deletedCount = 0;

         // Validate all changed PmPrefs values before saving (when showing decrypted)
         if (ShowEncrypted)
         {
            var invalidKeys = new List<string>();
            foreach (var pref in PmPrefsList)
            {
               if (pref.Changed && !pref.DeleteMarker && !IsValidJson(pref.Value))
               {
                  invalidKeys.Add(pref.Key);
               }
            }

            if (invalidKeys.Count > 0)
            {
               string keys = string.Join(", ", invalidKeys);
               EditorUtility.DisplayDialog("Invalid JSON",
                  $"The following keys have invalid JSON values:\n\n{keys}\n\nPlease fix the values and try again.",
                  "OK");
               return;
            }
         }

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
               // If showing decrypted, value needs to be encrypted on save
               if (ShowEncrypted)
               {
                  PlayerPrefs.SetString(PmPrefs.Prefix + pref.Key, PmPrefs.Encrypt(pref.Value));
               }
               else
               {
                  // Already encrypted, save as-is
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
               // Detect type
               if (int.TryParse(pref.Value, out int intVal))
                  PlayerPrefs.SetInt(pref.Key, intVal);
               else if (float.TryParse(pref.Value, out float floatVal))
                  PlayerPrefs.SetFloat(pref.Key, floatVal);
               else
                  PlayerPrefs.SetString(pref.Key, pref.Value);

               pref.Save();
               savedCount++;
            }
         }

         // Flush key list changes, then save all to disk once
         PmPrefs.FlushKeyList();
         PlayerPrefs.Save();
         _prefsKeyReader.InvalidateCache();

         // Refresh list views without full re-initialization
         _listViewPmPrefsList?.RefreshItems();
         _listViewPlayerPrefsList?.RefreshItems();

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
         UpdateUnsavedChangesIndicator();
      }

      /// <summary>
      /// Re-applies the current search filter to both list views.
      /// Used after refreshing lists to maintain filter state.
      /// </summary>
      private void ApplyCurrentFilter()
      {
         if (string.IsNullOrEmpty(_currentSearchText))
         {
            // Reset to full lists
            if (_listViewPmPrefsList != null)
               _listViewPmPrefsList.itemsSource = PmPrefsList;
            if (_listViewPlayerPrefsList != null)
               _listViewPlayerPrefsList.itemsSource = PlayerPrefsList;
            return;
         }

         string searchText = _currentSearchText.ToLower();
         FilterList(_listViewPmPrefsList, PmPrefsList, searchText);
         FilterList(_listViewPlayerPrefsList, PlayerPrefsList, searchText);
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

         // Delete all PmPrefs (they can't be decrypted with new key)
         PmPrefs.DeleteAllPmPrefs();
         PlayerPrefs.Save();

         // Find and update the PmPrefs.cs file
         string filePath = FindPmPrefsFile();

         if (string.IsNullOrEmpty(filePath))
         {
            EditorUtility.DisplayDialog("Error",
               "Could not find PmPrefs.cs file.\n\n" +
               "The secure key could not be changed automatically.\n" +
               "Please manually update the SecureKey constant in PmPrefs.cs.", "OK");
            return;
         }

         try
         {
            string[] lines = File.ReadAllLines(filePath);
            bool found = false;

            for (int i = 0; i < lines.Length; i++)
            {
               if (lines[i].Contains("public const string SecureKey ="))
               {
                  string toReplace = QuotedStringPattern.Match(lines[i]).Groups[1].Value;
                  lines[i] = lines[i].Replace($"\"{toReplace}\"", $"\"{key}\"");
                  found = true;
                  break;
               }
            }

            if (found)
            {
               File.WriteAllLines(filePath, lines);
               AssetDatabase.Refresh();

               EditorUtility.DisplayDialog("Success",
                  $"Secure key has been changed.\n\nThe project will recompile with the new key.", "OK");
            }
            else
            {
               EditorUtility.DisplayDialog("Error",
                  "Could not find SecureKey constant in PmPrefs.cs.\n\n" +
                  "Please manually update the SecureKey constant.", "OK");
            }
         }
         catch (Exception ex)
         {
            EditorUtility.DisplayDialog("Error",
               $"Failed to update PmPrefs.cs:\n{ex.Message}", "OK");
         }

         _prefsKeyReader.InvalidateCache();
         RefreshLists();
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

      /// <summary>
      /// Finds the PmPrefs.cs file in either Packages or Assets folder.
      /// </summary>
      private string FindPmPrefsFile()
      {
         // First check in Packages folder
         string packagePath = Path.GetFullPath("Packages/com.projectmakers.pmprefs/Scripts/PmPrefs.cs");
         if (File.Exists(packagePath))
            return packagePath;

         // Check in Assets folder (for development)
         string[] guids = AssetDatabase.FindAssets("PmPrefs t:Script");
         foreach (string guid in guids)
         {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith("PmPrefs.cs") && !path.Contains("Editor"))
            {
               return Path.GetFullPath(path);
            }
         }

         // Last resort: recursive search in Assets
         return FindFileRecursive("PmPrefs.cs", Application.dataPath);
      }

      /// <summary>
      /// Validates whether a string is syntactically valid JSON.
      /// Checks objects, arrays, strings, numbers, booleans, and null.
      /// </summary>
      private static bool IsValidJson(string text)
      {
         if (string.IsNullOrWhiteSpace(text)) return false;
         text = text.Trim();
         if (text.Length == 0) return false;

         char first = text[0];
         char last = text[text.Length - 1];

         // JSON object or array: validate bracket balance
         if ((first == '{' && last == '}') || (first == '[' && last == ']'))
         {
            int depth = 0;
            bool inStr = false;
            bool esc = false;
            foreach (char c in text)
            {
               if (esc) { esc = false; continue; }
               if (c == '\\' && inStr) { esc = true; continue; }
               if (c == '"') { inStr = !inStr; continue; }
               if (inStr) continue;
               if (c == '{' || c == '[') depth++;
               else if (c == '}' || c == ']') { depth--; if (depth < 0) return false; }
            }
            return depth == 0 && !inStr;
         }

         // JSON string
         if (first == '"' && last == '"' && text.Length >= 2) return true;

         // JSON primitives
         if (text == "true" || text == "false" || text == "null") return true;

         // JSON number
         double d;
         return double.TryParse(text, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out d);
      }

      private string FindFileRecursive(string filename, string folder)
      {
         try
         {
            var files = Directory.GetFiles(folder, filename, SearchOption.AllDirectories);
            foreach (var file in files)
            {
               // Skip editor scripts
               if (!file.Contains("Editor"))
                  return file;
            }
         }
         catch (Exception)
         {
            // Ignore access denied errors
         }

         return null;
      }
   }
}
