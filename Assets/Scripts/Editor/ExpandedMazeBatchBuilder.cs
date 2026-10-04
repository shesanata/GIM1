using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace MazeSpell.Editor
{
    [InitializeOnLoad]
    public static class ExpandedMazeBatchBuilder
    {
        private const string RunKey = "MazeSpell_RunBatchExpansion_Done_V4";

        static ExpandedMazeBatchBuilder()
        {
            EditorApplication.delayCall += RunBatchOnce;
        }

        private static void RunBatchOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (SessionState.GetBool(RunKey, false)) return;

            SessionState.SetBool(RunKey, true);
            ExecuteBuildAndTests();
        }

        [MenuItem("MazeSpell/Execute Expanded Mazes Build & Verification")]
        public static void ExecuteBuildAndTests()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== MAZESPELL EXPANDED MAZE BUILD & VERIFICATION ===");
            sb.AppendLine($"Timestamp: {DateTime.Now}");

            Application.LogCallback logCallback = (condition, stackTrace, type) =>
            {
                sb.AppendLine($"[{type}] {condition}");
            };

            Application.logMessageReceived += logCallback;

            try
            {
                Debug.Log("[Batch] Step 1: Building Level 1 (29x15)...");
                Level1MazeBuilder.BuildLevel1();

                Debug.Log("[Batch] Step 2: Building Level 2 (33x17)...");
                Level2MazeBuilder.BuildLevel2();

                Debug.Log("[Batch] Step 3: Building Level 3 (37x19)...");
                Level3MazeBuilder.BuildLevel3();

                Debug.Log("[Batch] Step 4: Building Level 4 (41x21)...");
                Level4MazeBuilder.BuildLevel4();

                Debug.Log("[Batch] Step 5: Building Level 5 (45x23)...");
                Level5MazeBuilder.BuildLevel5();

                Debug.Log("[Batch] All 5 Expanded Mazes Built and Saved to Prefabs successfully!");

                Debug.Log("[Batch] Step 6: Running MazeRuntimeTestRunner.RunAllTests()...");
                MazeRuntimeTestRunner.RunAllTests();

                Debug.Log("[Batch] Step 7: Batch execution and test runner finished!");
            }
            catch (Exception ex)
            {
                sb.AppendLine($"[FATAL EXCEPTION]: {ex}");
                Debug.LogError($"[ExpandedMazeBatchBuilder Error]: {ex}");
            }
            finally
            {
                Application.logMessageReceived -= logCallback;
                string outputPath = Path.Combine(Application.dataPath, "Scripts/Editor/batch_build_output.txt");
                File.WriteAllText(outputPath, sb.ToString());
                Debug.Log($"[Batch] Output log written to {outputPath}");
            }
        }
    }
}
