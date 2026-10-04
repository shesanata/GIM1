using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MazeSpell;
using MazeSpell.UI;
using MazeSpell.Gameplay;

namespace MazeSpell.Editor
{
    /// <summary>
    /// Runner otomatis untuk merekonstruksi dan memverifikasi layout labirin referensi lama
    /// pada Level 1 - 5, mengambil screenshot Game View di Play Mode / Scene View,
    /// serta menguji gameplay secara lengkap.
    /// </summary>
    [InitializeOnLoad]
    public static class RestoreOldMazeExecutionRunner
    {
        private static bool hasExecuted = false;

        static RestoreOldMazeExecutionRunner()
        {
            EditorApplication.delayCall += OnEditorReady;
        }

        private static void OnEditorReady()
        {
            if (hasExecuted) return;
            hasExecuted = true;
            ExecuteRestoreAndVerify();
        }

        [MenuItem("MazeSpell/Restore Old Maze Reference Layout (All Levels)")]
        public static void ExecuteRestoreAndVerify()
        {
            string logPath = "c:/Users/ASUS/Mazell/restore_output_log.txt";
            using (var writer = new StreamWriter(logPath, false))
            {
                writer.WriteLine("==================================================================");
                writer.WriteLine(">>> MEMULAI RESTORASI TOTAL DESAIN MAZE LAMA (LEVEL 1 - 5) <<<");
                writer.WriteLine("==================================================================");

                // 1. Generate Tile Assets
                writer.WriteLine("[RestoreRunner] Step 0: Generating tile assets...");
                MazeThematicVisualGenerator.GenerateAllVisualAssets();

                // 2. Build All 5 Level Prefabs
                writer.WriteLine("[RestoreRunner] Step 1: Building Level 1 (21x11 Earth)...");
                Level1MazeBuilder.BuildLevel1();

                writer.WriteLine("[RestoreRunner] Step 2: Building Level 2 (21x11 Stone)...");
                Level2MazeBuilder.BuildLevel2();

                writer.WriteLine("[RestoreRunner] Step 3: Building Level 3 (21x11 Grass)...");
                Level3MazeBuilder.BuildLevel3();

                writer.WriteLine("[RestoreRunner] Step 4: Building Level 4 (21x11 Frost)...");
                Level4MazeBuilder.BuildLevel4();

                writer.WriteLine("[RestoreRunner] Step 5: Building Level 5 (21x11 Flame)...");
                Level5MazeBuilder.BuildLevel5();

                // 3. Fix & Verify Letter Positions
                writer.WriteLine("[RestoreRunner] Step 6: Fixing letter positions...");
                LetterPlacementSetup.FixAllLetterPositions(false);

                // 4. Capture Screenshots for all 5 levels
                for (int lvl = 1; lvl <= 5; lvl++)
                {
                    writer.WriteLine($"[RestoreRunner] Step 7.{lvl}: Capturing Screenshot Level {lvl}...");
                    CaptureLevelGameView(lvl);
                }

                // 5. Run Full Gameplay Verification Tests
                writer.WriteLine("[RestoreRunner] Step 8: Running Runtime Verification Tests...");
                MazeRuntimeTestRunner.RunAllTests();

                writer.WriteLine("==================================================================");
                writer.WriteLine(">>> RESTORASI DESAIN MAZE LAMA SELESAI 100% <<<");
                writer.WriteLine("==================================================================");
            }
            Debug.Log("[RestoreRunner] Execution completed. Results written to restore_output_log.txt");
        }

        public static void CaptureLevelGameView(int level)
        {
            string scenePath = "Assets/Scenes/SampleScene.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) return;

            // Bersihkan semua instance maze grid di scene
            var existingGrids = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in existingGrids)
            {
                if (go != null && go.name.Contains("MazeGrid"))
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }

            // Muat prefab level yang sesuai
            string prefabPath = $"Assets/Prefabs/MazeGrids/Level{level}_MazeGrid.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                var gridInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                gridInstance.name = $"Level{level}_MazeGrid";
            }

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(10.5f, 5.5f, -10f); // Center of 21x11 maze
                cam.orthographic = true;
                cam.orthographicSize = 6.2f;

                Color[] bgColors = {
                    new Color(0.32f, 0.22f, 0.15f, 1f), // L1: Earth
                    new Color(0.12f, 0.16f, 0.24f, 1f), // L2: Stone
                    new Color(0.15f, 0.32f, 0.15f, 1f), // L3: Grass
                    new Color(0.12f, 0.25f, 0.38f, 1f), // L4: Frost
                    new Color(0.28f, 0.10f, 0.06f, 1f)  // L5: Flame
                };
                cam.backgroundColor = bgColors[Mathf.Clamp(level - 1, 0, 4)];

                var follow = cam.GetComponent<CameraFollow>();
                if (follow != null)
                {
                    follow.SetMazeBounds(21f, 11f, Vector2.zero);
                }
            }

            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                player.transform.position = new Vector3(1.5f, 2.5f, 0f);
            }

            int width = 960;
            int height = 540;
            RenderTexture rt = new RenderTexture(width, height, 24);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(rt);

            string outPath = $"c:/Users/ASUS/Mazell/level{level}_gameview_screenshot.png";
            File.WriteAllBytes(outPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            Debug.Log($"[RestoreRunner] Screenshot Game View Level {level} berhasil disimpan ke: {outPath}");
        }
    }
}
