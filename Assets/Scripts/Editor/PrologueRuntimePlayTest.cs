using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using MazeSpell.UI;

namespace MazeSpell.Editor
{
    public static class PrologueRuntimePlayTest
    {
        private const string LogPath = "Logs/prologue_runtime_play_test.log";

        [MenuItem("MazeSpell/Run Full Prologue Runtime & Play Test")]
        public static void RunFullTest()
        {
            var sb = new StringBuilder();
            int passed = 0;
            int failed = 0;

            void Log(string msg)
            {
                Debug.Log(msg);
                sb.AppendLine(msg);
            }

            void Assert(bool cond, string name)
            {
                if (cond)
                {
                    passed++;
                    Log($"[PASS] {name}");
                }
                else
                {
                    failed++;
                    Log($"[FAIL] {name}");
                }
            }

            Log("=================================================");
            Log(">>> UJI RUNTIME & PLAY MODE LENGKAP PROLOGUE <<<");
            Log($"Timestamp: {DateTime.Now}");
            Log("=================================================");

            try
            {
                // =========================================================================
                // 1. REBUILD SCENE PROLOGUE SECARA BERSIH
                // =========================================================================
                Log("\n--- 1. MEMBUAT ULANG SCENE PROLOGUE SECARA BERSIH ---");
                PrologueSceneBuilder.BuildPrologueScene(false);
                Assert(File.Exists(PrologueSceneBuilder.PrologueScenePath), "File Assets/Scenes/Prologue.unity tersedia");

                // =========================================================================
                // 2. CEK PLAY MODE START SCENE
                // =========================================================================
                Log("\n--- 2. VERIFIKASI PLAY MODE START SCENE ---");
                MainMenuPlayModeEnforcer.EnforceStartupFlow();
                Assert(EditorSceneManager.playModeStartScene != null, "EditorSceneManager.playModeStartScene TIDAK null");
                Assert(EditorSceneManager.playModeStartScene != null && EditorSceneManager.playModeStartScene.name == "Prologue",
                    $"playModeStartScene mengarah ke Prologue.unity (saat ini: {(EditorSceneManager.playModeStartScene != null ? EditorSceneManager.playModeStartScene.name : "null")})");

                // =========================================================================
                // 3. CEK BUILD SETTINGS
                // =========================================================================
                Log("\n--- 3. VERIFIKASI BUILD SETTINGS ---");
                var buildScenes = EditorBuildSettings.scenes;
                Assert(buildScenes.Length == 4, $"Build Settings memiliki tepat 4 scene (ditemukan: {buildScenes.Length})");
                Assert(buildScenes.Length > 0 && buildScenes[0].path == "Assets/Scenes/Prologue.unity" && buildScenes[0].enabled,
                    "[0] Assets/Scenes/Prologue.unity (Enabled)");
                Assert(buildScenes.Length > 1 && buildScenes[1].path == "Assets/Scenes/MainMenu.unity" && buildScenes[1].enabled,
                    "[1] Assets/Scenes/MainMenu.unity (Enabled)");
                Assert(buildScenes.Length > 2 && buildScenes[2].path == "Assets/Scenes/SampleScene.unity" && buildScenes[2].enabled,
                    "[2] Assets/Scenes/SampleScene.unity (Enabled)");
                Assert(buildScenes.Length > 3 && buildScenes[3].path == "Assets/Scenes/LevelSelect.unity" && buildScenes[3].enabled,
                    "[3] Assets/Scenes/LevelSelect.unity (Enabled)");

                // =========================================================================
                // 4. BUKA PROLOGUE SECARA LANGSUNG & PERIKSA STRUKTUR HIERARCHY
                // =========================================================================
                Log("\n--- 4. BUKA PROLOGUE SECARA LANGSUNG DI EDITOR ---");
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/Prologue.unity", OpenSceneMode.Single);
                Assert(scene.IsValid(), "Prologue.unity berhasil dibuka sebagai active scene");

                // Pastikan tidak ada Ground atau Player gameplay liar yang diinject
                var strayPlayer = GameObject.Find("Player");
                var strayGround = GameObject.Find("Ground");
                Assert(strayPlayer == null, "TIDAK ADA objek 'Player' gameplay liar di Prologue.unity");
                Assert(strayGround == null, "TIDAK ADA objek 'Ground' gameplay liar di Prologue.unity");

                // Main Camera
                var cam = Camera.main;
                Assert(cam != null && cam.enabled, "Main Camera ada dan aktif");
                Assert(cam != null && cam.orthographic, "Main Camera orthographic = true");
                Assert(cam != null && Mathf.Approximately(cam.transform.position.x, 8.5f) && Mathf.Approximately(cam.transform.position.y, 5.5f),
                    $"Main Camera terfokus di pusat visual maze (8.5, 5.5) (posisi saat ini: {cam?.transform.position})");
                Assert(cam != null && Mathf.Approximately(cam.transform.position.z, -10f), "Main Camera Z position = -10");

                // PrologueCanvas
                var canvasObj = GameObject.Find("PrologueCanvas");
                Assert(canvasObj != null && canvasObj.activeSelf, "PrologueCanvas GameObject aktif");
                var canvas = canvasObj != null ? canvasObj.GetComponent<Canvas>() : null;
                Assert(canvas != null && canvas.enabled, "Canvas component aktif");
                Assert(canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay, "Canvas renderMode = ScreenSpaceOverlay");

                // Overlays & UI Text
                var blackOvr = GameObject.Find("BlackOverlay");
                Assert(blackOvr != null && blackOvr.activeSelf, "BlackOverlay aktif");
                var blackCg = blackOvr != null ? blackOvr.GetComponent<CanvasGroup>() : null;
                Assert(blackCg != null, "BlackOverlay memiliki CanvasGroup");

                var storyObj = GameObject.Find("StoryText");
                Assert(storyObj != null && storyObj.activeSelf, "StoryText aktif");
                var storyTmp = storyObj != null ? storyObj.GetComponent<TextMeshProUGUI>() : null;
                Assert(storyTmp != null && storyTmp.font != null, $"StoryText memiliki Font valid ({storyTmp?.font?.name})");

                var titleObj = GameObject.Find("TitleGroup");
                Assert(titleObj != null && titleObj.activeSelf, "TitleGroup aktif");

                var skipObj = GameObject.Find("SkipHint");
                Assert(skipObj != null && skipObj.activeSelf, "SkipHint aktif");

                // World elements
                var letterGroup = GameObject.Find("LetterGroup");
                Assert(letterGroup != null, "LetterGroup ada di dalam hierarchy");

                var mazeReveal = GameObject.Find("MazeReveal");
                Assert(mazeReveal != null, "MazeReveal ada di dalam hierarchy");

                var enemyReveal = GameObject.Find("EnemyReveal");
                Assert(enemyReveal != null, "EnemyReveal ada di dalam hierarchy");

                var playerReveal = GameObject.Find("PlayerReveal");
                Assert(playerReveal != null, "PlayerReveal ada di dalam hierarchy");

                // PrologueController
                var controller = UnityEngine.Object.FindAnyObjectByType<PrologueController>();
                Assert(controller != null && controller.enabled, "PrologueController ada dan aktif");

                // =========================================================================
                // 5. SIMULASI RUNTIME METHOD & COROUTINE CONTROLLER
                // =========================================================================
                Log("\n--- 5. SIMULASI RUNTIME PROLOGUE CONTROLLER ---");

                // Panggil Awake() via reflection
                MethodInfo awakeMethod = typeof(PrologueController).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert(awakeMethod != null, "Method Awake() ditemukan di PrologueController");
                awakeMethod.Invoke(controller, null);
                Assert(blackCg.alpha == 1f, "Awake() berhasil: BlackOverlay dimulai dari alpha = 1.0 (Black screen)");

                // Panggil Start() via reflection
                MethodInfo startMethod = typeof(PrologueController).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert(startMethod != null, "Method Start() ditemukan di PrologueController");
                startMethod.Invoke(controller, null);

                // Jalankan PlayPrologueSequence Coroutine secara terprogram (step-by-step)
                MethodInfo seqMethod = typeof(PrologueController).GetMethod("PlayPrologueSequence", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert(seqMethod != null, "Method PlayPrologueSequence() ditemukan");

                IEnumerator seqEnumerator = (IEnumerator)seqMethod.Invoke(controller, null);
                Assert(seqEnumerator != null, "PlayPrologueSequence() coroutine berhasil diinisialisasi");

                // Step coroutine untuk Scene 1
                bool hasMore = seqEnumerator.MoveNext();
                Assert(hasMore, "Scene 1: Coroutine berjalan step pertama");
                object currentYield = seqEnumerator.Current;

                // Step sampai sub-coroutine DisplayStoryText dijalankan
                int steps = 0;
                while (steps < 5 && seqEnumerator.MoveNext())
                {
                    steps++;
                }
                Log($"[INFO] Coroutine maju {steps} step.");

                // Periksa StoryText
                Assert(storyTmp != null, "StoryText TMP komponen valid saat runtime");

                // =========================================================================
                // 6. UJI MEKANISME SKIP (SPACE / ESC)
                // =========================================================================
                Log("\n--- 6. UJI MEKANISME SKIP (SPACE / ESC) ---");
                controller.SkipPrologue();
                FieldInfo isSkippingField = typeof(PrologueController).GetField("isSkipping", BindingFlags.Instance | BindingFlags.NonPublic);
                bool isSkipping = (bool)isSkippingField.GetValue(controller);
                Assert(isSkipping, "SkipPrologue() berhasil: isSkipping bernilai TRUE");

                // =========================================================================
                // 7. PERSISTENSI PLAY MODE DARI SCENE LAIN
                // =========================================================================
                Log("\n--- 7. PERSISTENSI PLAY MODE START SCENE DARI SCENE LAIN ---");
                // Buka MainMenu.unity
                var mainMenuScene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
                Assert(mainMenuScene.IsValid(), "MainMenu.unity dibuka");

                // Pastikan playModeStartScene TETAP Prologue.unity walau MainMenu yang sedang aktif di hierarchy
                MainMenuPlayModeEnforcer.EnforceStartupFlow();
                Assert(EditorSceneManager.playModeStartScene != null && EditorSceneManager.playModeStartScene.name == "Prologue",
                    "playModeStartScene TETAP terkunci ke Prologue.unity saat MainMenu aktif");

                // Buka kembali Prologue.unity sebagai scene kerja
                EditorSceneManager.OpenScene("Assets/Scenes/Prologue.unity", OpenSceneMode.Single);
                MainMenuPlayModeEnforcer.EnforceStartupFlow();
                Assert(EditorSceneManager.playModeStartScene != null && EditorSceneManager.playModeStartScene.name == "Prologue",
                    "playModeStartScene terkunci ke Prologue.unity saat Prologue.unity aktif");

                Log("=================================================");
                Log($"HASIL RUNTIME TEST: {passed} LULUS, {failed} GAGAL");
                Log("=================================================");
            }
            catch (Exception ex)
            {
                failed++;
                Log($"[EXCEPTION] Error pengujian runtime: {ex.Message}\n{ex.StackTrace}");
            }

            Directory.CreateDirectory("Logs");
            File.WriteAllText(LogPath, sb.ToString());
            Log($"[TEST COMPLETE] Laporan lengkap disimpan di: {LogPath}");
        }
    }
}
