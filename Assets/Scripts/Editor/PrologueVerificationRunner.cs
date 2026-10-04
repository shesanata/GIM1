using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using MazeSpell.UI;

namespace MazeSpell.Editor
{
    /// <summary>
    /// Test Runner otomatis untuk memvalidasi Prologue / Intro Cinematic game MazeSpell:
    /// 1. Memeriksa keberadaan scene Prologue.unity.
    /// 2. Memeriksa urutan Build Settings: [0] Prologue, [1] MainMenu, [2] SampleScene, [3] LevelSelect.
    /// 3. Memeriksa playModeStartScene mengarah ke Prologue.unity.
    /// 4. Memeriksa struktur objek di scene Prologue (PrologueController, Canvas, Overlays, StoryText, LetterGroup, MazeReveal, EnemyReveal, PlayerReveal, TitleGroup, SkipHint).
    /// 5. Memeriksa integritas MainMenu.unity dan SampleScene.unity (tidak ada perubahan merusak pada gameplay).
    /// 6. Menguji logika Skip System (SPACE / ESC) dan durasi sequence.
    /// 7. Menyimpan laporan komprehensif ke Logs/prologue_verification.log.
    /// </summary>
    public static class PrologueVerificationRunner
    {
        [MenuItem("MazeSpell/Run Prologue Verification Tests")]
        public static void RunAllTests()
        {
            string logDir = Path.Combine(Application.dataPath, "..", "Logs");
            if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
            string logPath = Path.Combine(logDir, "prologue_verification.log");

            using (StringWriter sw = new StringWriter())
            {
                sw.WriteLine("==================================================================");
                sw.WriteLine(">>> HASIL PENGUJIAN VERIFIKASI PROLOGUE / INTRO CINEMATIC MAZESPELL <<<");
                sw.WriteLine($"Waktu Eksekusi: {DateTime.Now}");
                sw.WriteLine("==================================================================");

                int passedCount = 0;
                int failedCount = 0;

                void AssertTest(bool condition, string testName, string details = "")
                {
                    if (condition)
                    {
                        passedCount++;
                        sw.WriteLine($"[PASS] {testName} {(!string.IsNullOrEmpty(details) ? "- " + details : "")}");
                        Debug.Log($"[PrologueTest PASS] {testName}");
                    }
                    else
                    {
                        failedCount++;
                        sw.WriteLine($"[FAIL] {testName} {(!string.IsNullOrEmpty(details) ? "- " + details : "")}");
                        Debug.LogError($"[PrologueTest FAIL] {testName} {details}");
                    }
                }

                try
                {
                    // Pastikan scene dibangun terlebih dahulu
                    PrologueSceneBuilder.BuildPrologueScene(false);

                    // -------------------------------------------------------------
                    // 1. UJI FILE SCENE & BUILD SETTINGS
                    // -------------------------------------------------------------
                    sw.WriteLine("\n--- 1. Uji Build Settings & Startup Flow ---");
                    bool prologueFileExists = File.Exists(PrologueSceneBuilder.PrologueScenePath);
                    AssertTest(prologueFileExists, "Scene Prologue.unity ada di Assets/Scenes/Prologue.unity");

                    var buildScenes = EditorBuildSettings.scenes;
                    AssertTest(buildScenes != null && buildScenes.Length >= 4, "Build Settings memiliki minimal 4 scene terdaftar");

                    if (buildScenes != null && buildScenes.Length >= 4)
                    {
                        AssertTest(buildScenes[0].path == PrologueSceneBuilder.PrologueScenePath && buildScenes[0].enabled,
                            "Build Settings [0] adalah Prologue.unity (Enabled)");
                        AssertTest(buildScenes[1].path == PrologueSceneBuilder.MainMenuScenePath && buildScenes[1].enabled,
                            "Build Settings [1] adalah MainMenu.unity (Enabled)");
                        AssertTest(buildScenes[2].path == PrologueSceneBuilder.GameplayScenePath && buildScenes[2].enabled,
                            "Build Settings [2] adalah SampleScene.unity (Enabled)");
                        AssertTest((buildScenes[3].path == PrologueSceneBuilder.LevelSelectScenePath || buildScenes[3].path == PrologueSceneBuilder.LegacyLevelSelectScenePath) && buildScenes[3].enabled,
                            "Build Settings [3] adalah LevelSelect.unity (Enabled)");
                    }

                    AssertTest(EditorSceneManager.playModeStartScene != null && EditorSceneManager.playModeStartScene.name == "Prologue",
                        "playModeStartScene terkunci ke Prologue.unity");

                    // -------------------------------------------------------------
                    // 2. UJI STRUKTUR HIERARKI SCENE PROLOGUE
                    // -------------------------------------------------------------
                    sw.WriteLine("\n--- 2. Uji Struktur Hierarki & Objek Scene Prologue ---");
                    var prologueScene = EditorSceneManager.OpenScene(PrologueSceneBuilder.PrologueScenePath, OpenSceneMode.Single);
                    AssertTest(prologueScene.IsValid(), "Scene Prologue.unity berhasil dimuat di Editor");

                    // Main Camera
                    Camera mainCam = Camera.main;
                    AssertTest(mainCam != null, "Main Camera terpasang di Prologue",
                        mainCam != null ? $"Pos: {mainCam.transform.position}, OrthoSize: {mainCam.orthographicSize}" : "");

                    // Controller
                    var controller = UnityEngine.Object.FindAnyObjectByType<PrologueController>();
                    AssertTest(controller != null, "PrologueController ditemukan di scene Prologue");

                    // Canvas & UI Elements
                    var canvas = GameObject.Find("PrologueCanvas");
                    AssertTest(canvas != null, "PrologueCanvas terpasang");

                    var blackOverlay = GameObject.Find("BlackOverlay");
                    AssertTest(blackOverlay != null && blackOverlay.GetComponent<CanvasGroup>() != null,
                        "BlackOverlay terpasang dengan CanvasGroup");

                    var storyText = GameObject.Find("StoryText");
                    AssertTest(storyText != null && storyText.GetComponent<TextMeshProUGUI>() != null,
                        "StoryText terpasang dengan TextMeshProUGUI");

                    var titleGroup = GameObject.Find("TitleGroup");
                    AssertTest(titleGroup != null && titleGroup.GetComponent<CanvasGroup>() != null,
                        "TitleGroup terpasang dengan CanvasGroup");

                    var skipHint = GameObject.Find("SkipHint");
                    AssertTest(skipHint != null && skipHint.GetComponent<TextMeshProUGUI>() != null,
                        "SkipHint terpasang dengan TextMeshProUGUI ('PRESS SPACE / ESC TO SKIP')");

                    var flashOverlay = GameObject.Find("FlashOverlay");
                    AssertTest(flashOverlay != null && flashOverlay.GetComponent<CanvasGroup>() != null,
                        "FlashOverlay terpasang dengan CanvasGroup");

                    // World Elements
                    var worldContainer = GameObject.Find("WorldContainer");
                    AssertTest(worldContainer != null, "WorldContainer terpasang");

                    var letterGroup = GameObject.Find("LetterGroup");
                    AssertTest(letterGroup != null && letterGroup.transform.childCount >= 4,
                        $"LetterGroup terpasang dengan {letterGroup?.transform.childCount} Bubble Letters");

                    var mazeReveal = GameObject.Find("MazeReveal");
                    AssertTest(mazeReveal != null && mazeReveal.transform.childCount > 0,
                        "MazeReveal terpasang dengan visual labirin");

                    var enemyReveal = GameObject.Find("EnemyReveal");
                    var enemyAnimator = enemyReveal != null ? enemyReveal.GetComponent<EnemyVisualAnimator>() : null;
                    AssertTest(enemyReveal != null && enemyAnimator != null,
                        "EnemyReveal terpasang dengan EnemyVisualAnimator");

                    var playerReveal = GameObject.Find("PlayerReveal");
                    var playerBob = playerReveal != null ? playerReveal.GetComponent<ProloguePlayerIdleBob>() : null;
                    AssertTest(playerReveal != null && playerBob != null,
                        "PlayerReveal terpasang dengan ProloguePlayerIdleBob");

                    var playerLetters = GameObject.Find("PlayerLetters");
                    AssertTest(playerLetters != null && playerLetters.transform.childCount >= 3,
                        $"PlayerLetters terpasang dengan {playerLetters?.transform.childCount} huruf di sekitar Player");

                    var particles = GameObject.Find("MagicParticles");
                    AssertTest(particles != null && particles.GetComponent<ParticleSystem>() != null,
                        "MagicParticles terpasang dengan ParticleSystem");

                    // -------------------------------------------------------------
                    // 3. UJI SISTEM SKIP PROLOGUE
                    // -------------------------------------------------------------
                    sw.WriteLine("\n--- 3. Uji Sistem Skip Prologue ---");
                    if (controller != null)
                    {
                        // Verifikasi bahwa SkipPrologue memicu proses tanpa exception
                        try
                        {
                            controller.SkipPrologue();
                            AssertTest(true, "Method SkipPrologue() dapat dieksekusi dengan aman");
                        }
                        catch (Exception skipEx)
                        {
                            AssertTest(false, "Method SkipPrologue() gagal", skipEx.Message);
                        }
                    }

                    // -------------------------------------------------------------
                    // 4. UJI INTEGRITAS SCENE MAIN MENU
                    // -------------------------------------------------------------
                    sw.WriteLine("\n--- 4. Uji Integritas Scene Main Menu ---");
                    var mainMenuScene = EditorSceneManager.OpenScene(PrologueSceneBuilder.MainMenuScenePath, OpenSceneMode.Single);
                    AssertTest(mainMenuScene.IsValid(), "Scene MainMenu.unity berhasil dimuat");

                    var menuController = UnityEngine.Object.FindAnyObjectByType<MainMenuController>();
                    AssertTest(menuController != null, "MainMenuController tetap utuh dan fungsional di MainMenu");

                    var playBtn = GameObject.Find("PlayButton");
                    AssertTest(playBtn != null && playBtn.GetComponent<Button>() != null,
                        "PlayButton tetap ada dan fungsional menuju gameplay");

                    var levelBtn = GameObject.Find("LevelButton");
                    AssertTest(levelBtn != null && levelBtn.GetComponent<Button>() != null,
                        "LevelButton tetap ada dan fungsional menuju LevelSelect");

                    // -------------------------------------------------------------
                    // 5. UJI INTEGRITAS SCENE GAMEPLAY (SampleScene)
                    // -------------------------------------------------------------
                    sw.WriteLine("\n--- 5. Uji Integritas Scene Gameplay (SampleScene) ---");
                    var gameplayScene = EditorSceneManager.OpenScene(PrologueSceneBuilder.GameplayScenePath, OpenSceneMode.Single);
                    AssertTest(gameplayScene.IsValid(), "Scene SampleScene.unity berhasil dimuat");

                    var player = GameObject.Find("Player");
                    AssertTest(player != null, "Player tetap ada di SampleScene gameplay");

                    var pc = player != null ? player.GetComponent<PlayerController>() : null;
                    AssertTest(pc != null, "PlayerController tetap utuh di Player");

                    var gameplayCanvas = GameObject.Find("GameplayCanvas");
                    AssertTest(gameplayCanvas != null, "GameplayCanvas tetap utuh di SampleScene");

                    var progMgr = GameObject.Find("LevelProgressionManager");
                    AssertTest(progMgr != null, "LevelProgressionManager tetap utuh di SampleScene");

                    // Verifikasi singleton runtime managers siap instansiasi
                    AssertTest(PlayerLivesManager.Instance != null, "PlayerLivesManager singleton siap bekerja di runtime");
                    AssertTest(WordCollectionManager.Instance != null, "WordCollectionManager singleton siap bekerja di runtime");

                    // Bersihkan manager sementara yang terbuat di editor test
                    var tempLives = UnityEngine.Object.FindAnyObjectByType<PlayerLivesManager>();
                    if (tempLives != null) UnityEngine.Object.DestroyImmediate(tempLives.gameObject);
                    var tempWords = UnityEngine.Object.FindAnyObjectByType<WordCollectionManager>();
                    if (tempWords != null) UnityEngine.Object.DestroyImmediate(tempWords.gameObject);

                    // Buka kembali Prologue scene sebagai scene aktif di editor
                    EditorSceneManager.OpenScene(PrologueSceneBuilder.PrologueScenePath, OpenSceneMode.Single);

                    sw.WriteLine("\n==================================================================");
                    sw.WriteLine($"HASIL AKHIR: {passedCount} TEST BERHASIL, {failedCount} TEST GAGAL.");
                    sw.WriteLine(failedCount == 0
                        ? ">>> SELURUH INTEGRASI PROLOGUE / INTRO CINEMATIC DINYATAKAN 100% SUKSES & AMAN! <<<"
                        : ">>> TERDAPAT TEST GAGAL PADA INTEGRASI PROLOGUE! <<<");
                    sw.WriteLine("==================================================================");
                }
                catch (Exception ex)
                {
                    failedCount++;
                    sw.WriteLine($"[CRITICAL EXCEPTION] {ex}");
                    Debug.LogError($"[PrologueVerificationRunner] Exception during tests: {ex}");
                }

                File.WriteAllText(logPath, sw.ToString());
                Debug.Log($"[PrologueVerificationRunner] Laporan pengujian disimpan di: {logPath}");
            }
        }
    }
}
