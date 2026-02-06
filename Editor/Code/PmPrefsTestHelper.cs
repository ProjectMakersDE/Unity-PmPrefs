using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PM.Plugins
{
   /// <summary>
   /// Helper class for testing PmPrefs export/import functionality.
   /// Provides automated test data creation and verification utilities.
   /// </summary>
   public static class PmPrefsTestHelper
   {
      /// <summary>
      /// Creates a comprehensive set of test preferences for export/import testing.
      /// </summary>
      [MenuItem("Tools/ProjectMakers/PmPrefs/Create Test Data")]
      public static void CreateTestData()
      {
         if (!EditorUtility.DisplayDialog("Create Test Data",
            "This will create test preferences for PmPrefs export/import testing.\n\n" +
            "Test data includes:\n" +
            "- 5 PmPrefs entries (string, number, decimal, special chars, unicode)\n" +
            "- 3 PlayerPrefs entries (int, float, string)\n\n" +
            "Continue?",
            "Create", "Cancel"))
         {
            return;
         }

         try
         {
            // Create PmPrefs test entries
            PmPrefs.Save("test_string", "Hello World!");
            PmPrefs.Save("test_number", "12345");
            PmPrefs.Save("test_decimal", "3.14159");
            PmPrefs.Save("test_special_chars", "Test@#$%^&*();:,.<>?");
            PmPrefs.Save("test_unicode", "Tëst Ùñîçødé 🎉");

            // Create PlayerPrefs test entries
            PlayerPrefs.SetInt("player_score", 9999);
            PlayerPrefs.SetFloat("player_health", 85.5f);
            PlayerPrefs.SetString("player_name", "TestPlayer");

            PlayerPrefs.Save();

            Debug.Log("[PmPrefs Test] Test data created successfully:\n" +
                     "- 5 PmPrefs entries\n" +
                     "- 3 PlayerPrefs entries");

            EditorUtility.DisplayDialog("Test Data Created",
               "Test preferences created successfully!\n\n" +
               "PmPrefs: 5 entries\n" +
               "PlayerPrefs: 3 entries\n\n" +
               "Open the PmPrefs window to view them:\n" +
               "Tools > ProjectMakers > PmPrefs",
               "OK");
         }
         catch (Exception ex)
         {
            Debug.LogError($"[PmPrefs Test] Failed to create test data: {ex.Message}");
            EditorUtility.DisplayDialog("Error", $"Failed to create test data:\n{ex.Message}", "OK");
         }
      }

      /// <summary>
      /// Verifies the contents of a JSON export file.
      /// </summary>
      [MenuItem("Tools/ProjectMakers/PmPrefs/Verify JSON Export")]
      public static void VerifyJsonExport()
      {
         string path = EditorUtility.OpenFilePanel("Select JSON Export to Verify", "", "json");

         if (string.IsNullOrEmpty(path))
         {
            return;
         }

         try
         {
            string json = File.ReadAllText(path, Encoding.UTF8);

            var report = new StringBuilder();
            report.AppendLine("=== JSON Export Verification ===");
            report.AppendLine($"File: {Path.GetFileName(path)}");
            report.AppendLine($"Size: {new FileInfo(path).Length} bytes");
            report.AppendLine();

            // Parse JSON
            var data = JsonUtility.FromJson<ExportDataWrapper>(json);

            if (data == null)
            {
               report.AppendLine("❌ FAILED: Invalid JSON structure");
               Debug.LogError(report.ToString());
               EditorUtility.DisplayDialog("Verification Failed", report.ToString(), "OK");
               return;
            }

            // Verify structure
            bool hasErrors = false;

            if (data.pmPrefs == null)
            {
               report.AppendLine("❌ FAILED: Missing 'pmPrefs' array");
               hasErrors = true;
            }
            else
            {
               report.AppendLine($"✓ pmPrefs array found: {data.pmPrefs.Count} entries");

               // Check each PmPrefs entry
               int validEntries = 0;
               foreach (var item in data.pmPrefs)
               {
                  if (!string.IsNullOrEmpty(item.key) && item.value != null)
                  {
                     validEntries++;
                  }
               }
               report.AppendLine($"  - Valid entries: {validEntries}/{data.pmPrefs.Count}");
            }

            if (data.playerPrefs == null)
            {
               report.AppendLine("❌ FAILED: Missing 'playerPrefs' array");
               hasErrors = true;
            }
            else
            {
               report.AppendLine($"✓ playerPrefs array found: {data.playerPrefs.Count} entries");

               // Check each PlayerPrefs entry
               int validEntries = 0;
               foreach (var item in data.playerPrefs)
               {
                  if (!string.IsNullOrEmpty(item.key) && item.value != null)
                  {
                     validEntries++;
                  }
               }
               report.AppendLine($"  - Valid entries: {validEntries}/{data.playerPrefs.Count}");
            }

            report.AppendLine();

            if (hasErrors)
            {
               report.AppendLine("❌ VERIFICATION FAILED");
               Debug.LogError(report.ToString());
            }
            else
            {
               report.AppendLine("✓ VERIFICATION PASSED");
               report.AppendLine("\nAll checks completed successfully!");
               Debug.Log(report.ToString());
            }

            EditorUtility.DisplayDialog(
               hasErrors ? "Verification Failed" : "Verification Passed",
               report.ToString(),
               "OK");
         }
         catch (Exception ex)
         {
            string errorMsg = $"[PmPrefs Test] Failed to verify JSON export: {ex.Message}";
            Debug.LogError(errorMsg);
            EditorUtility.DisplayDialog("Verification Error", errorMsg, "OK");
         }
      }

      /// <summary>
      /// Verifies the contents of a CSV export file.
      /// </summary>
      [MenuItem("Tools/ProjectMakers/PmPrefs/Verify CSV Export")]
      public static void VerifyCsvExport()
      {
         string path = EditorUtility.OpenFilePanel("Select CSV Export to Verify", "", "csv");

         if (string.IsNullOrEmpty(path))
         {
            return;
         }

         try
         {
            var report = new StringBuilder();
            report.AppendLine("=== CSV Export Verification ===");
            report.AppendLine($"File: {Path.GetFileName(path)}");
            report.AppendLine($"Size: {new FileInfo(path).Length} bytes");
            report.AppendLine();

            int pmPrefsCount = 0;
            int playerPrefsCount = 0;
            int invalidLines = 0;
            int lineNumber = 0;

            using (var reader = new StreamReader(File.OpenRead(path), Encoding.UTF8))
            {
               while (!reader.EndOfStream)
               {
                  lineNumber++;
                  var line = reader.ReadLine();

                  if (string.IsNullOrWhiteSpace(line))
                  {
                     continue;
                  }

                  var parts = line.Split(new[] { ';' }, 3);

                  if (parts.Length < 3)
                  {
                     report.AppendLine($"⚠️ Invalid line {lineNumber}: {line}");
                     invalidLines++;
                     continue;
                  }

                  if (parts[0] == "PmPrefs")
                  {
                     pmPrefsCount++;
                  }
                  else if (parts[0] == "PlayerPrefs")
                  {
                     playerPrefsCount++;
                  }
                  else
                  {
                     report.AppendLine($"⚠️ Unknown type at line {lineNumber}: {parts[0]}");
                     invalidLines++;
                  }
               }
            }

            report.AppendLine($"✓ Total lines processed: {lineNumber}");
            report.AppendLine($"✓ PmPrefs entries: {pmPrefsCount}");
            report.AppendLine($"✓ PlayerPrefs entries: {playerPrefsCount}");

            if (invalidLines > 0)
            {
               report.AppendLine($"❌ Invalid lines: {invalidLines}");
               report.AppendLine("\n❌ VERIFICATION FAILED");
               Debug.LogError(report.ToString());
            }
            else
            {
               report.AppendLine($"✓ Invalid lines: {invalidLines}");
               report.AppendLine("\n✓ VERIFICATION PASSED");
               report.AppendLine("\nAll checks completed successfully!");
               Debug.Log(report.ToString());
            }

            EditorUtility.DisplayDialog(
               invalidLines > 0 ? "Verification Failed" : "Verification Passed",
               report.ToString(),
               "OK");
         }
         catch (Exception ex)
         {
            string errorMsg = $"[PmPrefs Test] Failed to verify CSV export: {ex.Message}";
            Debug.LogError(errorMsg);
            EditorUtility.DisplayDialog("Verification Error", errorMsg, "OK");
         }
      }

      /// <summary>
      /// Wrapper class for JSON verification (matches ExportData in PmPrefsEditorWindow).
      /// </summary>
      [Serializable]
      private class ExportDataWrapper
      {
         public System.Collections.Generic.List<PreferenceItemWrapper> pmPrefs =
            new System.Collections.Generic.List<PreferenceItemWrapper>();
         public System.Collections.Generic.List<PreferenceItemWrapper> playerPrefs =
            new System.Collections.Generic.List<PreferenceItemWrapper>();
      }

      /// <summary>
      /// Wrapper class for preference items (matches PreferenceItem in PmPrefsEditorWindow).
      /// </summary>
      [Serializable]
      private class PreferenceItemWrapper
      {
         public string key;
         public string value;
      }
   }
}
