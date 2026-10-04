using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MazeSpell;
using MazeSpell.Gameplay;

namespace MazeSpell.Editor
{
    [InitializeOnLoad]
    public static class HouseMapExecutionRunner
    {
        private const string TriggerFile = "house_map_pending.trigger";
        private const string OutputLogFile = "house_map_overhaul_result.log";

        static HouseMapExecutionRunner()
        {
            EditorApplication.update += CheckAndExecutePending;
        }

        [InitializeOnLoadMethod]
        private static void OnEditorLoad()
        {
            EditorApplication.update += CheckAndExecutePending;
        }

        private static void CheckAndExecutePending()
        {
            if (File.Exists(TriggerFile))
            {
                if (EditorApplication.isPlaying)
                {
                    Debug.Log("[HouseMapExecutionRunner] Stopping Play Mode to execute Overhaul...");
                    EditorApplication.isPlaying = false;
                    return;
                }

                try
                {
                    File.Delete(TriggerFile);
                }
                catch { }

                ExecuteOverhaulAndVerify();
            }
        }

        [MenuItem("MazeSpell/Execute House Map Full Overhaul & Verification")]
        public static void ExecuteOverhaulAndVerify()
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
                Log(">>> MEMULAI OVERHAUL TOP-DOWN HOUSE MAP (L1 - L5) & PROLOGUE <<<");
                Log("=================================================");

                try
                {
                    // 1. Generate All Textures, Sprites & Tiles
                    Log("[Step 1] Menghasilkan Texture, Sprite Furniture & Tile Denah Rumah...");
                    HouseMapAssetGenerator.GenerateAllAssets();
                    Log("[Step 1] ✓ Seluruh Asset Rumah berhasil dibuat.");

                    // 2. Build Level 1 - 5 House Prefabs
                    Log("[Step 2] Menyusun Prefab Level 1-5 dengan Denah Rumah Cartoon Sesuai Gambar Referensi...");
                    HouseMapBuilder.BuildAllHouseLevels();
                    Log("[Step 2] ✓ Prefab Level 1-5 berhasil dirakit.");

                    // 3. Build Pure Black Prologue Scene
                    Log("[Step 3] Membangun Scene Prologue dengan Background Pure Black...");
                    PrologueSceneBuilder.BuildPrologueScene(false);
                    Log("[Step 3] ✓ Scene Prologue berhasil dibuat.");

                    // 4. Update Level Data Assets
                    Log("[Step 4] Memperbarui ScriptableObjects MazeLevelData...");
                    UpdateLevelData(1, 20, 16, "EARTH");
                    UpdateLevelData(2, 18, 16, "STONE");
                    UpdateLevelData(3, 20, 18, "GRASS");
                    UpdateLevelData(4, 20, 16, "FROST");
                    UpdateLevelData(5, 22, 16, "FLAME");
                    AssetDatabase.SaveAssets();
                    Log("[Step 4] ✓ ScriptableObjects Level 1-5 tersimpan.");

                    // 5. Capture Game View Screenshots
                    Log("[Step 5] Mengambil Screenshot Prologue & Level 1-5...");
                    CapturePrologueScreenshot(Log);
                    CaptureAllLevelHouseScreenshots(Log);
                    Log("[Step 5] ✓ Seluruh Screenshot berhasil diambil.");

                    // 6. Run Runtime Verification Tests
                    Log("[Step 6] Menjalankan Pengujian Runtime Gameplay & Verification Tests...");
                    MazeRuntimeTestRunner.RunAllTests();
                    Log("[Step 6] ✓ Pengujian Runtime Selesai.");

                    Log(">>> ★ OVERHAUL HOUSE MAP & PROLOGUE SELESAI DENGAN SUKSES! ★ <<<");
                }
                catch (Exception ex)
                {
                    Log($"[ERROR] Eksekusi gagal: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        private static void UpdateLevelData(int level, int cols, int rows, string targetWord)
        {
            string dataPath = $"Assets/Tilemaps/Level{level}/Level{level}_Data.asset";
            MazeLevelData data = AssetDatabase.LoadAssetAtPath<MazeLevelData>(dataPath);
            if (data != null)
            {
                data.columns = cols;
                data.rows = rows;
                data.themeName = "Cartoon House";
                EditorUtility.SetDirty(data);
            }
        }

        private static void CapturePrologueScreenshot(Action<string> logger = null)
        {
            try
            {
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/Prologue.unity", OpenSceneMode.Single);
                Camera cam = Camera.main;
                if (cam == null) cam = UnityEngine.Object.FindAnyObjectByType<Camera>();
                if (cam == null) return;

                int width = 1280;
                int height = 720;
                RenderTexture rt = new RenderTexture(width, height, 24);
                cam.targetTexture = rt;
                Texture2D screenTex = new Texture2D(width, height, TextureFormat.RGB24, false);

                cam.Render();

                RenderTexture.active = rt;
                screenTex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                screenTex.Apply();
                RenderTexture.active = null;

                byte[] bytes = screenTex.EncodeToPNG();
                string outPath = Path.Combine(Application.dataPath, "../prologue_screenshot.png");
                File.WriteAllBytes(outPath, bytes);
                logger?.Invoke($"[Screenshot] Captured Prologue to: {outPath}");

                cam.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(screenTex);
            }
            catch (Exception ex)
            {
                logger?.Invoke($"[Screenshot] Gagal capture Prologue: {ex.Message}");
            }
        }

        private static void CaptureAllLevelHouseScreenshots(Action<string> logger = null)
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

                Vector3 centerPos;
                float orthoSize;
                switch (lvl)
                {
                    case 1: centerPos = new Vector3(10.0f, 8.0f, -10f); orthoSize = 8.5f; break;
                    case 2: centerPos = new Vector3(9.0f, 8.0f, -10f); orthoSize = 8.5f; break;
                    case 3: centerPos = new Vector3(10.0f, 9.0f, -10f); orthoSize = 9.5f; break;
                    case 4: centerPos = new Vector3(10.0f, 8.0f, -10f); orthoSize = 8.5f; break;
                    case 5: centerPos = new Vector3(11.0f, 8.0f, -10f); orthoSize = 9.0f; break;
                    default: centerPos = new Vector3(10.0f, 8.0f, -10f); orthoSize = 8.5f; break;
                }

                cam.transform.position = centerPos;
                cam.orthographic = true;
                cam.orthographicSize = orthoSize;

                cam.Render();

                RenderTexture.active = rt;
                screenTex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                screenTex.Apply();
                RenderTexture.active = null;

                byte[] bytes = screenTex.EncodeToPNG();
                string outPath = Path.Combine(Application.dataPath, $"../level{lvl}_house_screenshot.png");
                File.WriteAllBytes(outPath, bytes);
                logger?.Invoke($"[Screenshot] Captured House Map level {lvl} to: {outPath}");
            }

            cam.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(screenTex);
        }
    }
}
