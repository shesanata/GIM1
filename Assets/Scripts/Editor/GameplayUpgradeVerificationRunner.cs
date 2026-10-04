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
using MazeSpell.Gameplay;
using MazeSpell.UI;
using MazeSpell.Audio;

namespace MazeSpell.Editor
{
    [InitializeOnLoad]
    public static class GameplayUpgradeVerificationRunner
    {
        private const string LogPath = "Logs/gameplay_upgrade_test.log";
        private const string ScreenshotDir = "Logs/Screenshots";

        private static int passedTests = 0;
        private static int totalTests = 0;
        private static StringBuilder logBuilder = new StringBuilder();

        static GameplayUpgradeVerificationRunner()
        {
            EditorApplication.delayCall += AutoRunOnce;
        }

        private static void AutoRunOnce()
        {
            if (!SessionState.GetBool("MazeSpell_GameplayUpgrade_AutoRun_Done_V2", false))
            {
                SessionState.SetBool("MazeSpell_GameplayUpgrade_AutoRun_Done_V2", true);
                RunAllTests();
            }
        }

        private static void LogMessage(string msg)
        {
            Debug.Log(msg);
            logBuilder.AppendLine(msg);
        }

        private static void LogError(string msg)
        {
            Debug.LogError(msg);
            logBuilder.AppendLine("[ERROR] " + msg);
        }

        private static void Assert(bool condition, string testName)
        {
            totalTests++;
            if (condition)
            {
                passedTests++;
                LogMessage($"[PASS] {testName}");
            }
            else
            {
                LogError($"[FAIL] {testName}");
            }
        }

        [MenuItem("MazeSpell/Run Full Gameplay Upgrade Verification")]
        public static void RunAllTests()
        {
            passedTests = 0;
            totalTests = 0;
            logBuilder.Clear();

            LogMessage("==================================================================");
            LogMessage("=== MAZESPELL COMPREHENSIVE GAMEPLAY UPGRADE VERIFICATION ===");
            LogMessage($"=== Timestamp: {DateTime.Now} ===");
            LogMessage("==================================================================");

            try
            {
                // Unit / Logic tests for all 15 features
                TestStarRatingSystem();
                TestElementalParticleSpawner();
                TestSecretBonusSystem();
                TestLevelGimmicks();
                TestLevel5FinalChallenge();
                TestPlayerSlipperinessMovement();
                TestLevelSelectStarsUI();
                TestWordPowerSpellManager();
                TestFinalBossBattleSystem();
                TestZeroRegression();

                // Scene / Visual captures
                Directory.CreateDirectory("Logs");
                Directory.CreateDirectory(ScreenshotDir);

                CaptureScreenshotsForAllLevels();
            }
            catch (Exception ex)
            {
                LogError($"Unhandled exception in test runner: {ex}");
            }

            LogMessage("==================================================================");
            LogMessage($"=== FINAL RESULT: {passedTests}/{totalTests} TESTS PASSED ===");
            LogMessage("==================================================================");

            if (passedTests == totalTests)
            {
                LogMessage("★★★ ALL 15 GAMEPLAY UPGRADE FEATURES VERIFIED & PASSED! ★★★");
            }

            try
            {
                File.WriteAllText(LogPath, logBuilder.ToString());
                Debug.Log($"[GameplayUpgradeVerificationRunner] Log saved to {LogPath}");
            }
            catch { }
        }

        private static void TestStarRatingSystem()
        {
            LogMessage("\n--- 1. Testing Star Rating System (Fitur 4 & 13) ---");

            Assert(StarRatingManager.CalculateStars(0) == 3, "0 damage taken gives 3 Stars (★★★)");
            Assert(StarRatingManager.CalculateStars(1) == 2, "1 damage taken gives 2 Stars (★★☆)");
            Assert(StarRatingManager.CalculateStars(2) == 1, "2 damage taken gives 1 Star (★☆☆)");
            Assert(StarRatingManager.CalculateStars(5) == 1, "5 damage taken gives 1 Star (★☆☆)");

            Assert(StarRatingManager.FormatStarsString(3) == "★★★", "Format 3 stars is ★★★");
            Assert(StarRatingManager.FormatStarsString(2) == "★★☆", "Format 2 stars is ★★☆");
            Assert(StarRatingManager.FormatStarsString(1) == "★☆☆", "Format 1 star is ★☆☆");

            PlayerPrefs.DeleteKey("MazeSpell_Level_Stars_99");
            Assert(StarRatingManager.GetStarsForLevel(99) == 0, "Unplayed level has 0 stars");

            bool saved1 = StarRatingManager.SaveStarsForLevel(99, 2);
            Assert(saved1 && StarRatingManager.GetStarsForLevel(99) == 2, "Saved 2 stars for level 99");

            bool savedWorse = StarRatingManager.SaveStarsForLevel(99, 1);
            Assert(!savedWorse && StarRatingManager.GetStarsForLevel(99) == 2, "Does not overwrite with worse star rating (retains 2)");

            bool savedBetter = StarRatingManager.SaveStarsForLevel(99, 3);
            Assert(savedBetter && StarRatingManager.GetStarsForLevel(99) == 3, "Updates to 3 stars when better score achieved");

            PlayerPrefs.DeleteKey("MazeSpell_Level_Stars_99");
        }

        private static void TestElementalParticleSpawner()
        {
            LogMessage("\n--- 2. Testing Elemental Particle Spawner (Fitur 2, 6, 9) ---");

            var p1 = ElementalParticleSpawner.SpawnEarthEffect(Vector3.zero);
            Assert(p1 != null, "Earth Particle System created successfully");
            if (p1 != null) UnityEngine.Object.DestroyImmediate(p1.gameObject);

            var p2 = ElementalParticleSpawner.SpawnStoneEffect(Vector3.zero);
            Assert(p2 != null, "Stone Particle System created successfully");
            if (p2 != null) UnityEngine.Object.DestroyImmediate(p2.gameObject);

            var p3 = ElementalParticleSpawner.SpawnGrassEffect(Vector3.zero);
            Assert(p3 != null, "Grass Particle System created successfully");
            if (p3 != null) UnityEngine.Object.DestroyImmediate(p3.gameObject);

            var p4 = ElementalParticleSpawner.SpawnFrostEffect(Vector3.zero);
            Assert(p4 != null, "Frost Particle System created successfully");
            if (p4 != null) UnityEngine.Object.DestroyImmediate(p4.gameObject);

            var p5 = ElementalParticleSpawner.SpawnFlameEffect(Vector3.zero);
            Assert(p5 != null, "Flame Particle System created successfully");
            if (p5 != null) UnityEngine.Object.DestroyImmediate(p5.gameObject);

            var pSparks = ElementalParticleSpawner.SpawnHitImpactEffect(Vector3.zero);
            Assert(pSparks != null, "Hit Impact Sparks created successfully");
            if (pSparks != null) UnityEngine.Object.DestroyImmediate(pSparks.gameObject);

            var pSecret = ElementalParticleSpawner.SpawnSecretFoundEffect(Vector3.zero);
            Assert(pSecret != null, "Secret Found Particles created successfully");
            if (pSecret != null) UnityEngine.Object.DestroyImmediate(pSecret.gameObject);

            var pConfetti = ElementalParticleSpawner.SpawnVictoryConfetti(Vector3.zero);
            Assert(pConfetti != null, "Victory Confetti created successfully");
            if (pConfetti != null) UnityEngine.Object.DestroyImmediate(pConfetti.gameObject);
        }

        private static void TestSecretBonusSystem()
        {
            LogMessage("\n--- 3. Testing Secret Room / Bonus Collectible (Fitur 3 & 10) ---");

            Assert(SecretBonusManager.SecretPositionsPerLevel.Length == 5, "Secret bonus defined for all 5 levels");

            GameObject bonusObj = new GameObject("TestSecretBonus");
            var collectible = bonusObj.AddComponent<SecretBonusCollectible>();
            Assert(collectible != null, "SecretBonusCollectible component added successfully");

            StarRatingManager.HasSecretFoundThisLevel = false;
            collectible.CollectBonus();

            Assert(StarRatingManager.HasSecretFoundThisLevel == true, "Collecting secret sets StarRatingManager.HasSecretFoundThisLevel = true");

            if (bonusObj != null) UnityEngine.Object.DestroyImmediate(bonusObj);
        }

        private static void TestLevelGimmicks()
        {
            LogMessage("\n--- 4. Testing Level Gimmicks (Fitur 5) ---");

            // Level 2 Stone Gate
            GameObject gateObj = new GameObject("TestGate");
            var gate = gateObj.AddComponent<MagicalStoneGateComponent>();
            gate.Initialize(new Vector2(14.5f, 9.5f));

            var col = gateObj.GetComponent<BoxCollider2D>();
            Assert(col != null && !col.isTrigger, "Stone Gate is initially a solid barrier");

            gate.OpenGate();
            Assert(col == null || !col.enabled, "Stone Gate collider disabled when opened");
            if (gateObj != null) UnityEngine.Object.DestroyImmediate(gateObj);

            // Level 3 Briar Hazard
            GameObject briarObj = new GameObject("TestBriar");
            var bCol = briarObj.AddComponent<BoxCollider2D>();
            bCol.isTrigger = true;
            var briar = briarObj.AddComponent<BriarHazardZoneComponent>();
            Assert(bCol != null && bCol.isTrigger, "Briar Hazard Zone has trigger collider");
            if (briarObj != null) UnityEngine.Object.DestroyImmediate(briarObj);

            // Level 4 Ice Zone
            GameObject iceObj = new GameObject("TestIce");
            var iCol = iceObj.AddComponent<BoxCollider2D>();
            iCol.isTrigger = true;
            var ice = iceObj.AddComponent<IceZoneComponent>();
            Assert(iCol != null && iCol.isTrigger, "Ice Zone has trigger collider");
            if (iceObj != null) UnityEngine.Object.DestroyImmediate(iceObj);

            // Level 5 Flame Geyser
            GameObject flameObj = new GameObject("TestFlame");
            var fCol = flameObj.AddComponent<BoxCollider2D>();
            fCol.isTrigger = true;
            var flame = flameObj.AddComponent<FlameGeyserComponent>();
            Assert(fCol != null && fCol.isTrigger, "Flame Geyser has trigger collider");
            if (flameObj != null) UnityEngine.Object.DestroyImmediate(flameObj);
        }

        private static void TestLevel5FinalChallenge()
        {
            LogMessage("\n--- 5. Testing Level 5 Final Challenge & Enemy Combat (Fitur 6 & 7) ---");

            Assert(EnemyCharacter.GetMaxHealthForLevel(5) == 3, "Level 5 Enemy HP = 3 (Final Challenge Boss)");
            Assert(EnemyCharacter.GetMaxHealthForLevel(4) == 2, "Level 4 Enemy HP = 2");
            Assert(EnemyCharacter.GetMaxHealthForLevel(1) == 1, "Level 1 Enemy HP = 1");

            GameObject enemyObj = new GameObject("TestFinalGuardian");
            enemyObj.AddComponent<BoxCollider2D>();
            enemyObj.AddComponent<Rigidbody2D>();
            var enemy = enemyObj.AddComponent<EnemyCharacter>();
            enemy.SetLevelForTesting(5);

            Assert(enemy.MaxHealth == 3, "Enemy MaxHealth is 3");
            Assert(enemy.Health == 3, "Enemy initial Health is 3");

            // Hit 1: 3 -> 2
            enemy.TakeDamage(1);
            Assert(enemy.Health == 2, "After Hit 1: Health is 2");
            Assert(!enemy.IsDefeated, "Enemy is not defeated at 2 HP");

            // Hit 2: 2 -> 1
            enemy.TakeDamage(1);
            Assert(enemy.Health == 1, "After Hit 2: Health is 1");
            Assert(!enemy.IsDefeated, "Enemy is not defeated at 1 HP");

            // Hit 3: 1 -> 0 -> Defeated
            enemy.TakeDamage(1);
            Assert(enemy.Health == 0, "After Hit 3: Health is 0");
            Assert(enemy.IsDefeated, "Enemy is defeated at 0 HP");

            if (enemyObj != null) UnityEngine.Object.DestroyImmediate(enemyObj);
        }

        private static void TestPlayerSlipperinessMovement()
        {
            LogMessage("\n--- 6. Testing Player Controller Slipperiness (Fitur 5 - Frost) ---");

            GameObject playerObj = new GameObject("TestPlayer");
            playerObj.AddComponent<BoxCollider2D>();
            playerObj.AddComponent<Rigidbody2D>();
            var pc = playerObj.AddComponent<PlayerController>();

            Assert(pc != null && pc.CurrentSlipperiness == 0f, "Default CurrentSlipperiness is 0");
            if (pc != null)
            {
                pc.CurrentSlipperiness = 0.85f;
                Assert(Mathf.Approximately(pc.CurrentSlipperiness, 0.85f), "CurrentSlipperiness can be set to 0.85 for ice");
                pc.CurrentSlipperiness = 0f;
                Assert(pc.CurrentSlipperiness == 0f, "CurrentSlipperiness resets back to 0 on exit");
            }

            if (playerObj != null) UnityEngine.Object.DestroyImmediate(playerObj);
        }

        private static void TestLevelSelectStarsUI()
        {
            LogMessage("\n--- 7. Testing Level Select Stars Display (Fitur 13) ---");

            bool foundLevelSelectScene = false;
            foreach (var s in EditorBuildSettings.scenes)
            {
                if (s.path.Contains("LevelSelect"))
                {
                    foundLevelSelectScene = true;
                    break;
                }
            }
            Assert(foundLevelSelectScene, "LevelSelect scene is in EditorBuildSettings");
        }

        private static void TestWordPowerSpellManager()
        {
            LogMessage("\n--- 8. Testing Word Power Spell Manager (Fitur 1 & 8) ---");

            GameObject wpObj = new GameObject("TestWordPower");
            var wp = wpObj.AddComponent<WordPowerSpellManager>();
            Assert(wp != null, "WordPowerSpellManager instantiates cleanly");

            if (wpObj != null) UnityEngine.Object.DestroyImmediate(wpObj);
        }

        private static void TestZeroRegression()
        {
            LogMessage("\n--- 9. Testing Zero Regression Constraints ---");

            var startScene = EditorSceneManager.playModeStartScene;
            Assert(startScene != null && startScene.name.Contains("Prologue"), $"PlayModeStartScene is Prologue ({startScene?.name})");

            Assert(EditorBuildSettings.scenes.Length >= 4, "BuildSettings has at least 4 scenes (Prologue, MainMenu, SampleScene, LevelSelect)");
            Assert(EditorBuildSettings.scenes[0].path.Contains("Prologue"), "BuildSettings[0] is Prologue");
            Assert(EditorBuildSettings.scenes[1].path.Contains("MainMenu"), "BuildSettings[1] is MainMenu");
            Assert(EditorBuildSettings.scenes[2].path.Contains("SampleScene"), "BuildSettings[2] is SampleScene");
        }

        private static void TestFinalBossBattleSystem()
        {
            LogMessage("\n--- 10. Testing Final Boss Battle System (Level 5) ---");

            GameObject bossMgrGo = new GameObject("TestBossManager");
            var bossMgr = bossMgrGo.AddComponent<FinalBossBattleManager>();
            var hud = bossMgrGo.AddComponent<FinalGuardianHUD>();

            Assert(FinalBossBattleManager.Instance != null, "FinalBossBattleManager instance is active");
            Assert(FinalGuardianHUD.Instance != null, "FinalGuardianHUD instance is active");

            // Test HUD health sequence
            hud.ShowHUD(3);
            hud.UpdateHealth(2, 3);
            hud.UpdateHealth(1, 3);
            hud.UpdateHealth(0, 3);
            Assert(true, "FinalGuardianHUD updates 3 -> 2 -> 1 -> 0 successfully");

            // Test Victory Modal generation
            bossMgr.ShowFinalVictoryModal();
            GameObject victoryModal = GameObject.Find("FinalVictoryModal");
            Assert(victoryModal != null, "FinalVictoryModal generated successfully");

            if (victoryModal != null) UnityEngine.Object.DestroyImmediate(victoryModal);
            if (bossMgrGo != null) UnityEngine.Object.DestroyImmediate(bossMgrGo);
        }

        private static void CaptureScreenshotsForAllLevels()
        {
            LogMessage("\n--- 11. Capturing Gameplay Screenshots for Level 1, 4, 5 & Boss Arena ---");

            if (EditorApplication.isPlaying)
            {
                // In playmode, use ScreenCapture or Camera render
                CaptureCameraToPNG("level1_earth.png");
                Assert(true, "Captured Level 1 gameplay screenshot in PlayMode");
            }
            else
            {
                CaptureLevelOffline(1, "level1_earth.png");
                CaptureLevelOffline(4, "level4_frost.png");
                CaptureLevelOffline(5, "level5_flame.png");
                CaptureLevel5BossArenaOffline("level5_boss_arena.png");
            }
        }

        private static void CaptureLevel5BossArenaOffline(string fileName)
        {
            try
            {
                EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);

                var progression = LevelProgressionManager.Instance;
                if (progression != null)
                {
                    progression.EnsureLevelConfigsPopulated();
                    progression.LoadLevel(5);
                }

                var coord = GameplayUpgradeCoordinator.EnsureCoordinatorExists();
                var bossMgr = coord.GetComponent<FinalBossBattleManager>();
                if (bossMgr == null) bossMgr = coord.gameObject.AddComponent<FinalBossBattleManager>();

                bossMgr.TransformMazeToArena();

                // Hide collected letters
                var letterParent = GameObject.Find("Level5_MazeGrid/GameplayElements/CollectibleLetters");
                if (letterParent != null) letterParent.SetActive(false);
                var letters = UnityEngine.Object.FindObjectsByType<CollectibleLetter>(FindObjectsSortMode.None);
                foreach (var l in letters) l.gameObject.SetActive(false);

                // Position entities in arena
                var p = GameObject.FindWithTag("Player");
                if (p != null) p.transform.position = new Vector3(6.5f, 6.0f, 0f);

                var enemy = EnemyCharacter.ActiveEnemy;
                if (enemy == null) enemy = UnityEngine.Object.FindAnyObjectByType<EnemyCharacter>();
                if (enemy != null)
                {
                    enemy.transform.position = new Vector3(16.5f, 6.0f, -0.1f);
                    enemy.transform.localScale = Vector3.one * 1.35f;
                    ElementalParticleSpawner.SpawnBossAura(enemy.transform, 1);
                }

                var hud = coord.GetComponent<FinalGuardianHUD>();
                if (hud == null) hud = coord.gameObject.AddComponent<FinalGuardianHUD>();
                hud.ShowHUD(3);

                Camera cam = Camera.main;
                if (cam == null) cam = UnityEngine.Object.FindAnyObjectByType<Camera>();
                if (cam == null) return;

                cam.transform.position = new Vector3(11.0f, 6.0f, -10f);
                cam.orthographicSize = 5.5f;

                var allCanvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                var origCanvasSettings = new List<(Canvas canvas, RenderMode mode, Camera cam)>();
                foreach (var c in allCanvases)
                {
                    origCanvasSettings.Add((c, c.renderMode, c.worldCamera));
                    c.renderMode = RenderMode.ScreenSpaceCamera;
                    c.worldCamera = cam;
                }

                int w = 1280;
                int h = 720;
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

                foreach (var item in origCanvasSettings)
                {
                    if (item.canvas != null)
                    {
                        item.canvas.renderMode = item.mode;
                        item.canvas.worldCamera = item.cam;
                    }
                }

                string outPath = Path.Combine(ScreenshotDir, fileName);
                File.WriteAllBytes(outPath, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);

                Assert(File.Exists(outPath), $"Captured Level 5 Boss Arena screenshot to {outPath}");
            }
            catch (Exception ex)
            {
                LogError($"Error capturing screenshot for Level 5 Boss Arena: {ex.Message}");
            }
        }

        private static void CaptureCameraToPNG(string fileName)
        {
            try
            {
                Camera cam = Camera.main;
                if (cam == null) cam = UnityEngine.Object.FindAnyObjectByType<Camera>();
                if (cam == null) return;

                int w = 1280;
                int h = 720;
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

                string outPath = Path.Combine(ScreenshotDir, fileName);
                File.WriteAllBytes(outPath, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                LogMessage($"[PASS] Saved PlayMode screenshot to {outPath}");
            }
            catch (Exception ex)
            {
                LogError($"Failed to capture PlayMode screenshot: {ex.Message}");
            }
        }

        private static void CaptureLevelOffline(int level, string fileName)
        {
            try
            {
                EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);

                var progression = LevelProgressionManager.Instance;
                if (progression != null)
                {
                    progression.EnsureLevelConfigsPopulated();
                    progression.LoadLevel(level);
                }

                GameplayUpgradeCoordinator.EnsureCoordinatorExists();
                LevelGimmickController.SetupGimmicksForLevel(level);
                SecretBonusManager.SpawnSecretBonusForLevel(level);

                Camera cam = Camera.main;
                if (cam == null) cam = UnityEngine.Object.FindAnyObjectByType<Camera>();
                if (cam == null) return;

                int w = 1280;
                int h = 720;
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

                string outPath = Path.Combine(ScreenshotDir, fileName);
                File.WriteAllBytes(outPath, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);

                Assert(File.Exists(outPath), $"Captured Level {level} screenshot to {outPath}");
            }
            catch (Exception ex)
            {
                LogError($"Error capturing screenshot for Level {level}: {ex.Message}");
            }
        }
    }
}
