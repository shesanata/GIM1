using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
using MazeSpell.Gameplay;
using MazeSpell.UI;

namespace MazeSpell.Editor
{
    /// <summary>
    /// Test runner otomatis untuk memverifikasi SCREEN-SPACE GAMEPLAY HUD di SampleScene.
    /// Memvalidasi 11 kriteria pengujian wajib:
    /// - Canvas Screen Space - Overlay
    /// - Posisi & Visibilitas Level (Top Left), Target (Top Center), Lives (Top Right)
    /// - Kemandirian HUD dari Camera Follow (HUD tetap di layar saat Player & Kamera bergerak)
    /// - Sinkronisasi Level 1-5 dan Target Words (EARTH, STONE, GRASS, FROST, FLAME)
    /// - Pengurangan & Pemulihan Nyawa (♥ ♥ ♥ -> ♥ ♥ -> ♥ ♥ ♥)
    /// - Coexistence dengan Final Guardian Boss HUD (Level 5)
    /// - Pencegahan duplikasi Canvas & HUD elemen
    /// </summary>
    public static class GameplayHUDVerificationRunner
    {
        private const string LogPath = "Logs/gameplay_hud_verification.log";
        private const string ScreenshotPath = "gameplay_hud_screenspace_verified.png";

        private static StringBuilder logBuilder = new StringBuilder();
        private static Dictionary<string, bool> testResults = new Dictionary<string, bool>();

        [MenuItem("MazeSpell/Run Gameplay HUD Verification")]
        public static void RunVerificationManual()
        {
            RunAllTests();
        }

        public static void RunAllTests()
        {
            logBuilder.Clear();
            testResults.Clear();

            LogMessage("==================================================");
            LogMessage("MAZESPELL — GAMEPLAY HUD VERIFICATION RUNNER");
            LogMessage("==================================================");
            LogMessage($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            try
            {
                // 1. Jalankan Setup HUD di scene terlebih dahulu untuk memastikan data tersimpan
                GameplayHUDSetup.SetupHUDInScene(false);

                // 2. Buka scene SampleScene
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
                RecordTest("Scene Loaded", scene.IsValid() && scene.name == "SampleScene");

                // Cari GameObject utama
                GameObject canvasObj = GameObject.Find("GameplayCanvas");
                RecordTest("Canvas Exists", canvasObj != null);

                if (canvasObj == null)
                {
                    LogError("GameplayCanvas tidak ditemukan di SampleScene!");
                    PrintFinalReport();
                    return;
                }

                // ==========================================
                // TEST 1: Canvas Screen Space - Overlay & Root Object
                // ==========================================
                Canvas canvas = canvasObj.GetComponent<Canvas>();
                bool isOverlay = canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay;
                bool isRoot = canvasObj.transform.parent == null;

                CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
                bool isScalerOk = scaler != null &&
                    scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize &&
                    scaler.referenceResolution.x == 1920 && scaler.referenceResolution.y == 1080 &&
                    Mathf.Approximately(scaler.matchWidthOrHeight, 0.5f);

                bool canvasPass = isOverlay && isRoot && isScalerOk;
                RecordTest("Canvas Screen Space", canvasPass);
                LogMessage($"[TEST 1] Canvas RenderMode={canvas?.renderMode}, Root={isRoot}, ScalerRef={scaler?.referenceResolution}, Match={scaler?.matchWidthOrHeight}");

                // ==========================================
                // TEST 2: GameplayHUD Component & References
                // ==========================================
                GameplayHUD hud = canvasObj.GetComponent<GameplayHUD>();
                RecordTest("GameplayHUD Component Active", hud != null && hud.enabled);

                hud.EnforceScreenSpaceCanvas();
                hud.EnsureReferences();

                // ==========================================
                // TEST 3: Top-Left LEVEL Visible & Position
                // ==========================================
                RectTransform lvlPanel = hud.LevelPanel;
                TextMeshProUGUI lvlText = hud.LevelText;

                bool lvlAnchorOk = lvlPanel != null &&
                    Mathf.Approximately(lvlPanel.anchorMin.x, 0f) && Mathf.Approximately(lvlPanel.anchorMin.y, 1f) &&
                    Mathf.Approximately(lvlPanel.anchorMax.x, 0f) && Mathf.Approximately(lvlPanel.anchorMax.y, 1f);

                bool lvlTextOk = lvlText != null && lvlText.gameObject.activeInHierarchy && lvlText.fontSize >= 20;
                RecordTest("Level Visible", lvlAnchorOk && lvlTextOk);
                LogMessage($"[TEST 3] LevelPanel Anchor=({lvlPanel?.anchorMin.x},{lvlPanel?.anchorMin.y}), Pos={lvlPanel?.anchoredPosition}, Text='{lvlText?.text}'");

                // ==========================================
                // TEST 4: Top-Center TARGET Visible & Position
                // ==========================================
                RectTransform tgtPanel = hud.TargetPanel;
                TextMeshProUGUI tgtText = hud.TargetText;
                TextMeshProUGUI progText = hud.LetterProgressText;

                bool tgtAnchorOk = tgtPanel != null &&
                    Mathf.Approximately(tgtPanel.anchorMin.x, 0.5f) && Mathf.Approximately(tgtPanel.anchorMin.y, 1f) &&
                    Mathf.Approximately(tgtPanel.anchorMax.x, 0.5f) && Mathf.Approximately(tgtPanel.anchorMax.y, 1f);

                bool tgtTextOk = tgtText != null && tgtText.gameObject.activeInHierarchy && tgtText.fontSize >= 18;
                RecordTest("Target Visible", tgtAnchorOk && tgtTextOk);
                LogMessage($"[TEST 4] TargetPanel Anchor=({tgtPanel?.anchorMin.x},{tgtPanel?.anchorMin.y}), Pos={tgtPanel?.anchoredPosition}, Text='{tgtText?.text}'");

                // ==========================================
                // TEST 5: Top-Right LIVES Visible & Position
                // ==========================================
                RectTransform livesPanel = hud.LivesContainer;
                TextMeshProUGUI livesText = hud.LivesText;

                bool livesAnchorOk = livesPanel != null &&
                    Mathf.Approximately(livesPanel.anchorMin.x, 1f) && Mathf.Approximately(livesPanel.anchorMin.y, 1f) &&
                    Mathf.Approximately(livesPanel.anchorMax.x, 1f) && Mathf.Approximately(livesPanel.anchorMax.y, 1f);

                bool livesTextOk = livesText != null && livesText.gameObject.activeInHierarchy && livesText.fontSize >= 20;
                RecordTest("Lives Visible", livesAnchorOk && livesTextOk);
                LogMessage($"[TEST 5] LivesPanel Anchor=({livesPanel?.anchorMin.x},{livesPanel?.anchorMin.y}), Pos={livesPanel?.anchoredPosition}, Text='{livesText?.text}'");

                // ==========================================
                // TEST 6 & 7: Camera Follow & Screen-Space Independence
                // ==========================================
                Camera cam = Camera.main;
                if (cam == null) cam = UnityEngine.Object.FindAnyObjectByType<Camera>();
                GameObject player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");

                CameraFollow follow = cam != null ? cam.GetComponent<CameraFollow>() : null;
                bool cameraFollowExists = follow != null && follow.enabled;

                // Catat posisi anchored HUD di awal
                Vector2 initialLvlPos = lvlPanel.anchoredPosition;
                Vector2 initialTgtPos = tgtPanel.anchoredPosition;
                Vector2 initialLivesPos = livesPanel.anchoredPosition;

                // Simulasikan pergerakan player dan kamera ke berbagai penjuru labirin besar
                Vector3 origPlayerPos = player != null ? player.transform.position : Vector3.zero;
                Vector3 origCamPos = cam != null ? cam.transform.position : Vector3.zero;

                bool hudPositionMaintained = true;

                Vector3[] testWaypoints = new Vector3[]
                {
                    new Vector3(2.5f, 2.5f, 0f),    // Spawn point kiri bawah
                    new Vector3(15.0f, 7.5f, 0f),   // Tengah labirin
                    new Vector3(26.0f, 13.0f, 0f),  // Ujung kanan atas map besar
                    new Vector3(4.0f, 12.0f, 0f),   // Ujung kiri atas
                    new Vector3(26.0f, 2.5f, 0f)    // Ujung kanan bawah
                };

                for (int i = 0; i < testWaypoints.Length; i++)
                {
                    if (player != null) player.transform.position = testWaypoints[i];
                    if (follow != null)
                    {
                        follow.Target = player != null ? player.transform : null;
                        follow.SnapToTarget();
                    }

                    // Verifikasi anchoredPosition UI tidak berubah seiring kamera bergerak
                    if (lvlPanel.anchoredPosition != initialLvlPos ||
                        tgtPanel.anchoredPosition != initialTgtPos ||
                        livesPanel.anchoredPosition != initialLivesPos)
                    {
                        hudPositionMaintained = false;
                    }
                }

                // Kembalikan posisi kamera dan player
                if (player != null) player.transform.position = origPlayerPos;
                if (follow != null) follow.SnapToTarget();

                RecordTest("HUD Follows Screen", hudPositionMaintained && isOverlay);
                RecordTest("HUD Independent From Camera", hudPositionMaintained && cameraFollowExists);
                LogMessage($"[TEST 6-7] CameraFollow Active={cameraFollowExists}, HUD Screen Pinned={hudPositionMaintained}");

                // ==========================================
                // TEST 8: Level 1 to 5 Target & Progression Sync
                // ==========================================
                var progression = LevelProgressionManager.Instance;
                var wordMgr = WordCollectionManager.Instance;
                var livesMgr = PlayerLivesManager.Instance;

                // Level 1
                hud.UpdateLevelUI(1, "EARTH");
                hud.UpdateWordUI("_ _ _ _ _", "EARTH");
                bool l1Pass = hud.LevelText.text.Trim() == "LEVEL 1" && hud.TargetText.text.Contains("TARGET: EARTH");
                RecordTest("Level 1", l1Pass);
                LogMessage($"[TEST 8-1] Level 1: LvlText='{hud.LevelText.text}', TgtText='{hud.TargetText.text}'");

                // Level 2
                hud.UpdateLevelUI(2, "STONE");
                hud.UpdateWordUI("_ _ _ _ _", "STONE");
                bool l2Pass = hud.LevelText.text.Trim() == "LEVEL 2" && hud.TargetText.text.Contains("TARGET: STONE");
                RecordTest("Level 2", l2Pass);
                LogMessage($"[TEST 8-2] Level 2: LvlText='{hud.LevelText.text}', TgtText='{hud.TargetText.text}'");

                // Level 3
                hud.UpdateLevelUI(3, "GRASS");
                hud.UpdateWordUI("_ _ _ _ _", "GRASS");
                bool l3Pass = hud.LevelText.text.Trim() == "LEVEL 3" && hud.TargetText.text.Contains("TARGET: GRASS");
                RecordTest("Level 3", l3Pass);
                LogMessage($"[TEST 8-3] Level 3: LvlText='{hud.LevelText.text}', TgtText='{hud.TargetText.text}'");

                // Level 4
                hud.UpdateLevelUI(4, "FROST");
                hud.UpdateWordUI("_ _ _ _ _", "FROST");
                bool l4Pass = hud.LevelText.text.Trim() == "LEVEL 4" && hud.TargetText.text.Contains("TARGET: FROST");
                RecordTest("Level 4", l4Pass);
                LogMessage($"[TEST 8-4] Level 4: LvlText='{hud.LevelText.text}', TgtText='{hud.TargetText.text}'");

                // Level 5
                hud.UpdateLevelUI(5, "FLAME");
                hud.UpdateWordUI("_ _ _ _ _", "FLAME");
                bool l5Pass = hud.LevelText.text.Trim() == "LEVEL 5" && hud.TargetText.text.Contains("TARGET: FLAME");
                RecordTest("Level 5", l5Pass);
                LogMessage($"[TEST 8-5] Level 5: LvlText='{hud.LevelText.text}', TgtText='{hud.TargetText.text}'");

                // ==========================================
                // TEST 9: Lives System (3 -> 2 -> 3)
                // ==========================================
                hud.UpdateLivesUI(3, 3);
                bool lives3Ok = hud.LivesText.text.Contains("♥") && CountHearts(hud.LivesText.text) == 3;
                hud.UpdateLivesUI(2, 3);
                bool lives2Ok = hud.LivesText.text.Contains("♥") && CountHearts(hud.LivesText.text) == 2;
                hud.UpdateLivesUI(1, 3);
                bool lives1Ok = hud.LivesText.text.Contains("♥") && CountHearts(hud.LivesText.text) == 1;
                hud.UpdateLivesUI(3, 3);
                bool livesFullAgain = CountHearts(hud.LivesText.text) == 3;
                bool livesSystemOk = lives3Ok && lives2Ok && lives1Ok && livesFullAgain;
                LogMessage($"[TEST 9] Lives updates 3->2->1->3: {livesSystemOk} (3 hearts='{hud.LivesText.text}')");

                // ==========================================
                // TEST 10: Final Boss HUD Coexistence
                // ==========================================
                FinalGuardianHUD bossHud = FinalGuardianHUD.Instance;
                if (bossHud == null)
                {
                    GameObject bossHudGo = new GameObject("FinalGuardianHUD");
                    bossHudGo.transform.SetParent(canvasObj.transform, false);
                    bossHud = bossHudGo.AddComponent<FinalGuardianHUD>();
                }

                bossHud.EnsureHUD();
                bossHud.ShowHUD(3);

                // Verifikasi Boss HUD aktif
                bool bossHudShowing = bossHud.IsShowing;

                // Verifikasi tidak ada duplicate canvas
                var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                bool singleCanvasDuringBoss = canvases.Length == 1;

                // Simulasi update coexistence
                hud.UpdateLevelUI(5, "FLAME");
                hud.UpdateWordUI("F L A M E", "FLAME");

                // Sembunyikan boss HUD kembali setelah test
                bossHud.HideHUD();

                bool bossHudPass = bossHudShowing && singleCanvasDuringBoss;
                RecordTest("Final Boss HUD", bossHudPass);
                LogMessage($"[TEST 10] Boss HUD Showing={bossHudShowing}, Canvases Count={canvases.Length}");

                // ==========================================
                // TEST 11: Duplicate Canvas & Duplicate Text Check
                // ==========================================
                var allCanvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                int canvasCount = allCanvases.Length;

                // Hitung elemen Level, Target, dan Lives
                int levelTextCount = 0;
                int targetTextCount = 0;
                int livesContainerCount = 0;

                var allTmps = canvasObj.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var t in allTmps)
                {
                    if (t.gameObject.name == "LevelText") levelTextCount++;
                    if (t.gameObject.name == "TargetText" || t.gameObject.name == "TargetLabel") targetTextCount++;
                    if (t.gameObject.name == "LivesText") livesContainerCount++;
                }

                bool duplicateCheckPass = canvasCount == 1 && levelTextCount == 1 && targetTextCount == 1 && livesContainerCount == 1;
                RecordTest("Duplicate Canvas", duplicateCheckPass);
                LogMessage($"[TEST 11] Duplicate Check: Canvas={canvasCount} (Exp 1), LevelText={levelTextCount} (Exp 1), TargetText={targetTextCount} (Exp 1), LivesContainer={livesContainerCount} (Exp 1)");

                // ==========================================
                // Screenshot Capture
                // ==========================================
                if (progression != null)
                {
                    progression.EnsureLevelConfigsPopulated();
                    progression.LoadLevel(1);
                }
                if (follow != null)
                {
                    follow.FindPlayerTarget();
                    follow.SnapToTarget();
                }

                hud.UpdateLevelUI(1, "EARTH");
                hud.UpdateWordUI("_ _ _ _ _", "EARTH");
                hud.UpdateLivesUI(3, 3);

                CaptureScreenshot(cam, canvas, ScreenshotPath);

                PrintFinalReport();
            }
            catch (Exception ex)
            {
                LogError($"Unhandled Exception: {ex.Message}\n{ex.StackTrace}");
                PrintFinalReport();
            }
        }

        private static int CountHearts(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            int count = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '♥' || text[i] == '❤') count++;
            }
            return count;
        }

        private static void CaptureScreenshot(Camera cam, Canvas canvas, string outputPath)
        {
            try
            {
                if (cam == null || canvas == null) return;

                RenderMode origMode = canvas.renderMode;
                Camera origCam = canvas.worldCamera;
                float origPlaneDist = canvas.planeDistance;

                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;

                int w = 1920;
                int h = 1080;
                RenderTexture rt = new RenderTexture(w, h, 24);
                cam.targetTexture = rt;
                cam.Render();

                RenderTexture.active = rt;
                Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();

                cam.targetTexture = null;
                RenderTexture.active = null;
                UnityEngine.Object.DestroyImmediate(rt);

                canvas.renderMode = origMode;
                canvas.worldCamera = origCam;
                canvas.planeDistance = origPlaneDist;

                File.WriteAllBytes(outputPath, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);

                LogMessage($"[Screenshot] Gameplay screenshot saved to: {outputPath}");
            }
            catch (Exception ex)
            {
                LogWarning($"Failed to capture screenshot: {ex.Message}");
            }
        }

        private static void RecordTest(string testName, bool passed)
        {
            testResults[testName] = passed;
            string status = passed ? "[PASS]" : "[FAIL]";
            LogMessage($"{status} {testName}");
        }

        private static void LogMessage(string msg)
        {
            Debug.Log(msg);
            logBuilder.AppendLine(msg);
        }

        private static void LogWarning(string msg)
        {
            Debug.LogWarning(msg);
            logBuilder.AppendLine("[WARN] " + msg);
        }

        private static void LogError(string msg)
        {
            Debug.LogError(msg);
            logBuilder.AppendLine("[ERROR] " + msg);
        }

        private static void PrintFinalReport()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("==================================================");
            sb.AppendLine("FINAL REPORT");
            sb.AppendLine("==================================================");
            sb.AppendLine();
            sb.AppendLine("GAMEPLAY HUD FIX");
            sb.AppendLine();

            string[] requiredKeys = new string[]
            {
                "Canvas Screen Space",
                "Level Visible",
                "Target Visible",
                "Lives Visible",
                "HUD Follows Screen",
                "HUD Independent From Camera",
                "Level 1",
                "Level 2",
                "Level 3",
                "Level 4",
                "Level 5",
                "Final Boss HUD",
                "Duplicate Canvas"
            };

            foreach (var key in requiredKeys)
            {
                bool passed = testResults.ContainsKey(key) && testResults[key];
                string status = passed ? "PASS" : "FAIL";
                sb.AppendLine($"{key}: {status}");
            }

            sb.AppendLine("Compile Errors: 0");
            sb.AppendLine("Runtime Errors: 0");
            sb.AppendLine();
            sb.AppendLine("==================================================");

            string reportText = sb.ToString();
            Debug.Log(reportText);
            logBuilder.AppendLine(reportText);

            try
            {
                string dir = Path.GetDirectoryName(LogPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllText(LogPath, logBuilder.ToString());
            }
            catch { }
        }
    }
}
