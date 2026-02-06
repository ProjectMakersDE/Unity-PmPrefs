using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using PM.Plugins;

namespace PM.Plugins.Editor
{
    /// <summary>
    /// Editor utility for testing PmPrefs backward compatibility with legacy List format.
    /// </summary>
    public static class PmPrefsBackwardCompatibilityTest
    {
        private const string TestMenuPath = "Tools/PmPrefs/Backward Compatibility Tests/";

        [MenuItem(TestMenuPath + "1. Create Legacy List Format Data")]
        public static void CreateLegacyData()
        {
            Debug.Log("[PmPrefs Test] Creating legacy List format data...");

            // Create test data in old List<string> format
            var testKeys = new List<string>
            {
                "test_key_1",
                "test_key_2",
                "test_key_3",
                "player_name",
                "high_score"
            };

            // Simulate old format: {"items":["key1","key2","key3"]}
            string legacyJson = "{\"items\":[";
            for (int i = 0; i < testKeys.Count; i++)
            {
                legacyJson += "\"" + testKeys[i] + "\"";
                if (i < testKeys.Count - 1)
                {
                    legacyJson += ",";
                }
            }
            legacyJson += "]}";

            // Save in old format directly to PlayerPrefs
            PlayerPrefs.SetString("PmPrefs__KeyList", legacyJson);
            PlayerPrefs.Save();

            // Also create some actual PmPrefs entries for these keys
            PmPrefs.Save("test_key_1", "Test Value 1");
            PmPrefs.Save("test_key_2", 42);
            PmPrefs.Save("test_key_3", new TestData { name = "Test", value = 100 });
            PmPrefs.Save("player_name", "Player One");
            PmPrefs.Save("high_score", 9999);

            // Force refresh to reset internal cache
            PmPrefs.RefreshKeyCache();

            Debug.Log($"[PmPrefs Test] Created legacy data with {testKeys.Count} keys");
            Debug.Log($"[PmPrefs Test] Legacy JSON: {legacyJson}");
            Debug.Log("[PmPrefs Test] ✓ Step 1 Complete - Now run Step 2 to test loading");
        }

        [MenuItem(TestMenuPath + "2. Test Load Legacy Data")]
        public static void TestLoadLegacyData()
        {
            Debug.Log("[PmPrefs Test] Testing load of legacy List format...");

            // Force refresh to ensure we're loading from PlayerPrefs
            PmPrefs.RefreshKeyCache();

            // Get all keys - this should trigger the backward compatibility logic
            List<string> loadedKeys = PmPrefs.GetAllKeys();

            Debug.Log($"[PmPrefs Test] Loaded {loadedKeys.Count} keys from PmPrefs");

            foreach (var key in loadedKeys)
            {
                Debug.Log($"[PmPrefs Test]   - {key}");
            }

            // Verify we can load the data
            string testValue1 = PmPrefs.Load<string>("test_key_1", "");
            int testValue2 = PmPrefs.Load<int>("test_key_2", 0);
            TestData testValue3 = PmPrefs.Load<TestData>("test_key_3");
            string playerName = PmPrefs.Load<string>("player_name", "");
            int highScore = PmPrefs.Load<int>("high_score", 0);

            Debug.Log($"[PmPrefs Test] test_key_1 = \"{testValue1}\"");
            Debug.Log($"[PmPrefs Test] test_key_2 = {testValue2}");
            Debug.Log($"[PmPrefs Test] test_key_3 = {testValue3?.name ?? "null"} ({testValue3?.value ?? 0})");
            Debug.Log($"[PmPrefs Test] player_name = \"{playerName}\"");
            Debug.Log($"[PmPrefs Test] high_score = {highScore}");

            // Check that migration happened
            string currentJson = PlayerPrefs.GetString("PmPrefs__KeyList", "");
            Debug.Log($"[PmPrefs Test] Current JSON format: {currentJson}");

            bool migrationSuccess = loadedKeys.Count >= 5;
            if (migrationSuccess)
            {
                Debug.Log("[PmPrefs Test] ✓ Step 2 Complete - Legacy data loaded successfully");
                Debug.Log("[PmPrefs Test] ✓ Data should now be migrated to HashSet format");
            }
            else
            {
                Debug.LogError("[PmPrefs Test] ✗ Failed to load legacy data - expected at least 5 keys");
            }
        }

        [MenuItem(TestMenuPath + "3. Test Add/Remove Operations")]
        public static void TestAddRemoveOperations()
        {
            Debug.Log("[PmPrefs Test] Testing Add/Remove operations...");

            // Get current keys
            List<string> beforeKeys = PmPrefs.GetAllKeys();
            int beforeCount = beforeKeys.Count;
            Debug.Log($"[PmPrefs Test] Starting with {beforeCount} keys");

            // Test adding a new key
            PmPrefs.Save("new_test_key", "New Value");
            List<string> afterAdd = PmPrefs.GetAllKeys();
            int afterAddCount = afterAdd.Count;

            bool addSuccess = afterAddCount == beforeCount + 1 && afterAdd.Contains("new_test_key");
            Debug.Log($"[PmPrefs Test] After Add: {afterAddCount} keys {(addSuccess ? "✓" : "✗")}");

            // Test removing a key
            PmPrefs.DeleteKey("new_test_key");
            List<string> afterRemove = PmPrefs.GetAllKeys();
            int afterRemoveCount = afterRemove.Count;

            bool removeSuccess = afterRemoveCount == beforeCount && !afterRemove.Contains("new_test_key");
            Debug.Log($"[PmPrefs Test] After Remove: {afterRemoveCount} keys {(removeSuccess ? "✓" : "✗")}");

            // Test adding duplicate (should not increase count)
            if (beforeKeys.Count > 0)
            {
                string existingKey = beforeKeys[0];
                PmPrefs.Save(existingKey, "Updated Value");
                List<string> afterDuplicate = PmPrefs.GetAllKeys();
                int afterDuplicateCount = afterDuplicate.Count;

                bool duplicateSuccess = afterDuplicateCount == beforeCount;
                Debug.Log($"[PmPrefs Test] After Duplicate Save: {afterDuplicateCount} keys {(duplicateSuccess ? "✓" : "✗")}");
            }

            if (addSuccess && removeSuccess)
            {
                Debug.Log("[PmPrefs Test] ✓ Step 3 Complete - Add/Remove operations work correctly");
            }
            else
            {
                Debug.LogError("[PmPrefs Test] ✗ Add/Remove operations failed");
            }
        }

        [MenuItem(TestMenuPath + "4. Verify HashSet Performance")]
        public static void TestHashSetPerformance()
        {
            Debug.Log("[PmPrefs Test] Testing HashSet performance...");

            // Create a larger dataset
            int testSize = 100;
            Debug.Log($"[PmPrefs Test] Creating {testSize} test keys...");

            for (int i = 0; i < testSize; i++)
            {
                PmPrefs.Save($"perf_test_key_{i}", $"Value {i}");
            }

            List<string> allKeys = PmPrefs.GetAllKeys();
            Debug.Log($"[PmPrefs Test] Total keys: {allKeys.Count}");

            // Test add performance
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            for (int i = 0; i < 100; i++)
            {
                PmPrefs.Save($"perf_add_test_{i}", i);
            }

            sw.Stop();
            Debug.Log($"[PmPrefs Test] Added 100 keys in {sw.ElapsedMilliseconds}ms");

            // Test remove performance
            sw.Restart();

            for (int i = 0; i < 100; i++)
            {
                PmPrefs.DeleteKey($"perf_add_test_{i}");
            }

            sw.Stop();
            Debug.Log($"[PmPrefs Test] Removed 100 keys in {sw.ElapsedMilliseconds}ms");

            Debug.Log("[PmPrefs Test] ✓ Step 4 Complete - Performance test finished");
            Debug.Log("[PmPrefs Test] Note: HashSet operations should be O(1), significantly faster with large datasets");
        }

        [MenuItem(TestMenuPath + "5. Clean Up Test Data")]
        public static void CleanUpTestData()
        {
            Debug.Log("[PmPrefs Test] Cleaning up test data...");

            List<string> allKeys = PmPrefs.GetAllKeys();
            int removedCount = 0;

            foreach (var key in new List<string>(allKeys))
            {
                if (key.StartsWith("test_") || key.StartsWith("perf_") || key.StartsWith("new_"))
                {
                    PmPrefs.DeleteKey(key);
                    removedCount++;
                }
            }

            Debug.Log($"[PmPrefs Test] Removed {removedCount} test keys");
            Debug.Log($"[PmPrefs Test] Remaining keys: {PmPrefs.GetAllKeys().Count}");
            Debug.Log("[PmPrefs Test] ✓ Cleanup Complete");
        }

        [MenuItem(TestMenuPath + "Run All Tests")]
        public static void RunAllTests()
        {
            Debug.Log("=== PmPrefs Backward Compatibility Test Suite ===\n");

            CreateLegacyData();
            Debug.Log("");

            TestLoadLegacyData();
            Debug.Log("");

            TestAddRemoveOperations();
            Debug.Log("");

            TestHashSetPerformance();
            Debug.Log("");

            CleanUpTestData();
            Debug.Log("");

            Debug.Log("=== Test Suite Complete ===");
        }

        [System.Serializable]
        private class TestData
        {
            public string name;
            public int value;
        }
    }
}
