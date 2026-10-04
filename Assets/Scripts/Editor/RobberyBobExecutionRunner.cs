using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MazeSpell;
using MazeSpell.Gameplay;

namespace MazeSpell.Editor
{
    public static class RobberyBobExecutionRunner
    {
        private const string TriggerFile = "robbery_bob_pending.trigger";
        private const string OutputLogFile = "robbery_bob_restore_result.log";

        [InitializeOnLoadMethod]
        private static void OnEditorLoad()
        {
            EditorApplication.delayCall += CheckAndExecutePending;
        }

        private static void CheckAndExecutePending()
        {
            if (File.Exists(TriggerFile))
            {
                try
                {
                    File.Delete(TriggerFile);
                }
                catch { }

                ExecuteRestoreAndVerify();
            }
        }

        [MenuItem("MazeSpell/Execute Robbery Bob Full Restore & Verification")]
        public static void ExecuteRestoreAndVerify()
        {
            using (var sw = new StreamWriter(OutputLogFile, false))
            {
                void Log(string msg)
                {
                    Debug.Log(msg);
                    sw.WriteLine(msg);
                    sw.Flush();
                }

                Log("=================================================");
                Log(">>> MEMULAI RESTORASI MAP ROBBERY BOB & VERIFIKASI <<<");
                Log("=================================================");

                try
                {
                    // 1. Generate All Textures & Tiles
                    Log("[Step 1] Menghasilkan Texture, Sprite Dekorasi & Tile Robbery Bob...");
                    RobberyBobVisualGenerator.GenerateAllRobberyBobAssets();
                    Log("[Step 1] ✓ Asset Robbery Bob berhasil dibuat.");

                    // 2. Build All Prefabs (L1 - L5)
                    Log("[Step 2] Menyusun Prefab Level 1-5 dengan Layout Robbery Bob Mansion...");
                    RobberyBobMazeBuilder.BuildAllRobberyBobLevels();
                    Log("[Step 2] ✓ Prefab Level 1-5 berhasil dirakit.");

                    // 3. Update Level Data Assets
                    Log("[Step 3] Memperbarui MazeLevelData ScriptableObjects (22x13)...");
                    for (int i = 1; i <= 5; i++)
                    {
                        string dataPath = $"Assets/Tilemaps/Level{i}/Level{i}_Data.asset";
                        MazeLevelData data = AssetDatabase.LoadAssetAtPath<MazeLevelData>(dataPath);
                        if (data != null)
                        {
                            data.columns = 22;
                            data.rows = 13;
                            data.themeName = "Robbery Bob Mansion";
                            EditorUtility.SetDirty(data);
                        }
                    }
                    AssetDatabase.SaveAssets();
                    Log("[Step 3] ✓ Data Level 1-5 tersimpan.");

                    // 4. Capture Screenshots for all 5 levels in SampleScene
                    Log("[Step 4] Mengambil screenshot Game View untuk 5 level...");
                    CaptureAllLevelScreenshots(Log);
                    Log("[Step 4] ✓ Screenshot 5 level berhasil disimpan.");

                    // 5. Run Full Gameplay Verification
                    Log("[Step 5] Menjalankan Pengujian Runtime Gameplay & Combat...");
                    MazeRuntimeTestRunner.RunAllTests();
                    Log("[Step 5] ✓ Pengujian Runtime Selesai.");

                    Log(">>> ★ PROSES RESTORASI ROBBERY BOB SELESAI DENGAN SUKSES! ★ <<<");
                }
                catch (Exception ex)
                {
                    Log($"[ERROR] Eksekusi gagal: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        private static void CaptureAllLevelScreenshots(Action<string> logger = null)
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            var progression = LevelProgressionManager.Instance;
            if (progression == null) return;

            progression.EnsureLevelConfigsPopulated();

            Camera cam = Camera.main;
            if (cam == null) cam = UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (cam == null) return;

            int width = 1280;
            int height = 720;
            RenderTexture rt = new RenderTexture(width, height, 24);
            cam.targetTexture = rt;
            Texture2D screenTex = new Texture2D(width, height, TextureFormat.RGB24, false);

            for (int lvl = 1; lvl <= 5; lvl++)
            {
                progression.LoadLevel(lvl);

                // 1. Full Mansion Overview screenshot
                cam.transform.position = new Vector3(11.0f, 6.5f, -10f);
                cam.orthographic = true;
                cam.orthographicSize = 7.0f;

                cam.Render();

                RenderTexture.active = rt;
                screenTex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                screenTex.Apply();
                RenderTexture.active = null;

                byte[] bytes = screenTex.EncodeToPNG();
                string outPath = Path.Combine(Application.dataPath, $"../level{lvl}_gameview_screenshot.png");
                File.WriteAllBytes(outPath, bytes);
                logger?.Invoke($"[Screenshot] Captured Robbery Bob level {lvl} to: {outPath}");

                // 2. Player Close-up (Robbery Bob camera view) for Level 1
                if (lvl == 1)
                {
                    cam.transform.position = new Vector3(2.5f, 2.5f, -10f);
                    cam.orthographicSize = 4.8f;

                    cam.Render();

                    RenderTexture.active = rt;
                    screenTex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    screenTex.Apply();
                    RenderTexture.active = null;

                    byte[] closeupBytes = screenTex.EncodeToPNG();
                    string closeupPath = Path.Combine(Application.dataPath, "../robbery_bob_player_closeup.png");
                    File.WriteAllBytes(closeupPath, closeupBytes);
                    logger?.Invoke($"[Screenshot] Captured Robbery Bob Player Close-up to: {closeupPath}");
                }
            }

            cam.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(screenTex);
        }
    }
}
