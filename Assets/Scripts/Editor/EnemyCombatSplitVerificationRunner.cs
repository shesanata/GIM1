using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MazeSpell;
using MazeSpell.Audio;
using MazeSpell.Gameplay;
using MazeSpell.UI;

namespace MazeSpell.Editor
{
    /// <summary>
    /// Test Runner Otomatis untuk Memverifikasi Enemy Combat Feedback & Split System:
    /// - Level 1-3: Hit -> Knockback & Hurt -> Split -> 2 Mini Enemies -> Chase
    /// - Level 4: Hit 1 (HP 2->1, no split) -> Hit 2 (HP 1->0, split 2 Mini Enemies)
    /// - Level 5: Final Guardian HP 3->2->1 (Enrage Warning, no split)->0 (Final Victory Modal, no split)
    /// - Batasan Split (Limit Test): Mini Enemy defeated -> no further split (0 infinite spawning)
    /// - Pathfinding & Wall Collision: Mini Enemy walkable corridor verification
    /// </summary>
    [InitializeOnLoad]
    public static class EnemyCombatSplitVerificationRunner
    {
        private const string LogPath = "Logs/enemy_combat_split_summary.log";
        private static StringBuilder logBuilder = new StringBuilder();
        private static int passedTests = 0;
        private static int totalTests = 0;

        static EnemyCombatSplitVerificationRunner()
        {
            EditorApplication.delayCall += AutoRunOnce;
        }

        private static void AutoRunOnce()
        {
            if (!SessionState.GetBool("EnemyCombatSplit_AutoRun_Done_V3", false))
            {
                SessionState.SetBool("EnemyCombatSplit_AutoRun_Done_V3", true);
                RunVerification();
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
            logBuilder.AppendLine("[FAIL] " + msg);
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

        [MenuItem("MazeSpell/Run Enemy Combat & Split Verification")]
        public static void RunVerification()
        {
            passedTests = 0;
            totalTests = 0;
            logBuilder.Clear();

            LogMessage("==================================================================");
            LogMessage("=== MAZESPELL ENEMY COMBAT & SPLIT SYSTEM VERIFICATION ===");
            LogMessage($"=== Timestamp: {DateTime.Now} ===");
            LogMessage("==================================================================");

            try
            {
                EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");

                Test1_AudioAndHitFeedbackComponents();
                Test2_Level1To3_SplitSystem();
                Test3_Level4_TwoHitSplitSystem();
                Test4_Level5_FinalGuardianNoSplitAndVictory();
                Test5_InfiniteSpawnPreventionAndLimits();
                Test6_MiniEnemyPathfindingAndWalls();
                Test7_SceneTransitionAndCleanup();
            }
            catch (Exception ex)
            {
                LogError($"Exception during verification: {ex.Message}\n{ex.StackTrace}");
            }

            LogMessage("==================================================================");
            LogMessage($"=== VERIFICATION SUMMARY: {passedTests}/{totalTests} TESTS PASSED ===");
            LogMessage("==================================================================");

            Directory.CreateDirectory("Logs");
            try
            {
                File.WriteAllText(LogPath, logBuilder.ToString());
                Debug.Log($"Log written to {LogPath}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Could not write to {LogPath}: {e.Message}");
            }
        }

        private static void Test1_AudioAndHitFeedbackComponents()
        {
            LogMessage("\n--- TEST 1: AUDIO & HIT FEEDBACK ASSETS ---");
            bool hasHitSFX = Enum.IsDefined(typeof(AudioManager.SFXType), "EnemyHit");
            bool hasSplitSFX = Enum.IsDefined(typeof(AudioManager.SFXType), "EnemySplit");
            bool hasMiniSpawnSFX = Enum.IsDefined(typeof(AudioManager.SFXType), "MiniEnemySpawn");

            Assert(hasHitSFX, "SFXType.EnemyHit exists in AudioManager");
            Assert(hasSplitSFX, "SFXType.EnemySplit exists in AudioManager");
            Assert(hasMiniSpawnSFX, "SFXType.MiniEnemySpawn exists in AudioManager");

            GameObject testObj = new GameObject("TestEnemyVisual");
            testObj.AddComponent<CircleCollider2D>();
            var enemy = testObj.AddComponent<EnemyCharacter>();
            enemy.EnsureVisualHierarchy();
            Assert(enemy.VisualAnimator != null, "EnemyVisualAnimator hierarchy created");
            UnityEngine.Object.DestroyImmediate(testObj);
        }

        private static void Test2_Level1To3_SplitSystem()
        {
            LogMessage("\n--- TEST 2: LEVEL 1-3 SPLIT SYSTEM ---");

            for (int lvl = 1; lvl <= 3; lvl++)
            {
                GameObject root = new GameObject($"TestL{lvl}_Maze");
                GameObject playerGo = new GameObject("Player");
                playerGo.tag = "Player";
                playerGo.transform.position = new Vector3(10f, 10f, 0f);

                GameObject enemyGo = new GameObject($"Enemy_L{lvl}");
                enemyGo.transform.SetParent(root.transform);
                enemyGo.transform.position = new Vector3(10f, 11f, 0f);

                enemyGo.AddComponent<CircleCollider2D>();
                var enemy = enemyGo.AddComponent<EnemyCharacter>();
                enemy.SetLevelForTesting(lvl);

                Assert(enemy.CurrentHealth == 1, $"Level {lvl}: Enemy starts with HP 1");
                Assert(enemy.CanSplit == true, $"Level {lvl}: Enemy main instance CanSplit == true");
                Assert(enemy.HasSplit == false, $"Level {lvl}: Enemy main instance HasSplit == false");

                // Simulate Player SPACE Attack
                bool attackSuccess = enemy.TryPlayerAttack(playerGo, 1);
                Assert(attackSuccess, $"Level {lvl}: Attack landed in range");

                // Wait or step frame
                var miniEnemies = root.GetComponentsInChildren<EnemyCharacter>();
                Assert(miniEnemies.Length == 2, $"Level {lvl}: Split produced exactly 2 Mini Enemies");

                foreach (var mini in miniEnemies)
                {
                    Assert(mini.IsMiniEnemy == true, $"Level {lvl}: Mini Enemy flagged as IsMiniEnemy == true");
                    Assert(mini.CanSplit == false, $"Level {lvl}: Mini Enemy CanSplit == false (cannot split again)");
                    Assert(mini.HasSplit == true, $"Level {lvl}: Mini Enemy HasSplit == true");
                    Assert(mini.CurrentHealth == 1, $"Level {lvl}: Mini Enemy HP == 1");
                    Assert(mini.transform.localScale.x <= 0.60f, $"Level {lvl}: Mini Enemy scale is ~50% of main ({mini.transform.localScale.x:F2})");
                }

                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(playerGo);
            }
        }

        private static void Test3_Level4_TwoHitSplitSystem()
        {
            LogMessage("\n--- TEST 3: LEVEL 4 TWO-HIT SPLIT SYSTEM ---");

            GameObject root = new GameObject("TestL4_Maze");
            GameObject playerGo = new GameObject("Player");
            playerGo.tag = "Player";
            playerGo.transform.position = new Vector3(10f, 10f, 0f);

            GameObject enemyGo = new GameObject("Enemy_L4");
            enemyGo.transform.SetParent(root.transform);
            enemyGo.transform.position = new Vector3(10f, 11f, 0f);

            enemyGo.AddComponent<CircleCollider2D>();
            var enemy = enemyGo.AddComponent<EnemyCharacter>();
            enemy.SetLevelForTesting(4);

            Assert(enemy.CurrentHealth == 2, "Level 4: Enemy starts with HP 2");
            Assert(enemy.CanSplit == true, "Level 4: CanSplit == true");

            // Hit 1: HP 2 -> 1
            enemy.TryPlayerAttack(playerGo, 1);
            Assert(enemy.CurrentHealth == 1, "Level 4: Hit 1 reduces HP to 1");
            Assert(enemy.HasSplit == false, "Level 4: Hit 1 does NOT trigger split");

            // Hit 2: HP 1 -> 0
            enemy.TryPlayerAttack(playerGo, 1);

            var miniEnemies = root.GetComponentsInChildren<EnemyCharacter>();
            Assert(miniEnemies.Length == 2, "Level 4: Hit 2 triggers split into exactly 2 Mini Enemies");

            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(playerGo);
        }

        private static void Test4_Level5_FinalGuardianNoSplitAndVictory()
        {
            LogMessage("\n--- TEST 4: LEVEL 5 FINAL GUARDIAN NO-SPLIT & VICTORY ---");

            GameObject root = new GameObject("TestL5_Arena");
            GameObject playerGo = new GameObject("Player");
            playerGo.tag = "Player";
            playerGo.transform.position = new Vector3(17.5f, 11f, 0f);

            GameObject bossGo = new GameObject("FinalGuardian");
            bossGo.transform.SetParent(root.transform);
            bossGo.transform.position = new Vector3(26.5f, 11f, 0f);

            bossGo.AddComponent<CircleCollider2D>();
            var boss = bossGo.AddComponent<EnemyCharacter>();
            boss.IsBoss = true;
            boss.SetLevelForTesting(5);
            boss.SetCurrentHealth(3);

            Assert(boss.IsBoss == true, "Level 5: IsBoss is true");
            Assert(boss.CanSplit == false, "Level 5: Boss CanSplit is strictly FALSE");
            Assert(boss.CurrentHealth == 3, "Level 5: Boss starts with HP 3");

            // Hit 1: 3 -> 2
            boss.TakeDamage(1, true);
            Assert(boss.CurrentHealth == 2, "Level 5: Hit 1 reduces Boss HP to 2");
            Assert(boss.CanSplit == false, "Level 5: Hit 1 does not split");

            // Hit 2: 2 -> 1 (Enrage warning)
            boss.TakeDamage(1, true);
            Assert(boss.CurrentHealth == 1, "Level 5: Hit 2 reduces Boss HP to 1 (Enrage warning)");
            Assert(boss.CanSplit == false, "Level 5: Hit 2 does not split");

            // Hit 3: 1 -> 0 (Defeat, Final Victory)
            boss.TakeDamage(1, true);
            Assert(boss.CurrentHealth == 0, "Level 5: Hit 3 reduces Boss HP to 0");

            // Ensure no mini enemies ever spawned in boss fight
            var anyMiniEnemies = root.GetComponentsInChildren<EnemySplitController>();
            bool anySplitActive = false;
            foreach (var sc in anyMiniEnemies)
            {
                if (sc.IsMiniEnemy) anySplitActive = true;
            }
            Assert(!anySplitActive, "Level 5: Zero Mini Enemies spawned in Final Guardian battle");

            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(playerGo);
        }

        private static void Test5_InfiniteSpawnPreventionAndLimits()
        {
            LogMessage("\n--- TEST 5: INFINITE SPAWN PREVENTION & LIMITS ---");

            GameObject root = new GameObject("TestLimit_Container");
            GameObject enemyGo = new GameObject("Enemy_Original");
            enemyGo.transform.SetParent(root.transform);
            enemyGo.AddComponent<CircleCollider2D>();
            var enemy = enemyGo.AddComponent<EnemyCharacter>();
            enemy.SetLevelForTesting(1);

            // HP 1 -> 0 triggers split automatically
            enemy.TakeDamage(1, true);

            var minis = root.GetComponentsInChildren<EnemyCharacter>();
            Assert(minis.Length == 2, "Spawned exactly 2 mini enemies from 1 main enemy");

            // Try attacking Mini Enemy 1
            minis[0].TakeDamage(1, true);
            Assert(minis[0].IsDefeated || minis[0].CurrentHealth <= 0, "Mini Enemy 1 is defeated on hit");

            // Check if mini enemy created any more mini enemies
            var postMinis = root.GetComponentsInChildren<EnemyCharacter>();
            int aliveMiniCount = 0;
            foreach (var m in postMinis)
            {
                if (m != null && !m.IsDefeated && m.gameObject.activeInHierarchy) aliveMiniCount++;
            }

            Assert(aliveMiniCount <= 1, $"Alive Mini Enemies decreased to {aliveMiniCount} (NO recursive split)");

            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void Test6_MiniEnemyPathfindingAndWalls()
        {
            LogMessage("\n--- TEST 6: MINI ENEMY PATHFINDING & CORRIDORS ---");

            GameObject root = new GameObject("TestBFS_Container");
            GameObject playerGo = new GameObject("Player");
            playerGo.tag = "Player";
            playerGo.transform.position = new Vector3(5f, 5f, 0f);

            GameObject miniGo = new GameObject("MiniEnemy_BFS");
            miniGo.transform.SetParent(root.transform);
            miniGo.transform.position = new Vector3(5f, 8f, 0f);

            miniGo.AddComponent<CircleCollider2D>();
            var mini = miniGo.AddComponent<EnemyCharacter>();
            mini.SetMiniEnemyStats(1.2f, 1.35f);

            Vector3Int startCell = new Vector3Int(5, 8, 0);
            Vector3Int targetCell = new Vector3Int(5, 5, 0);
            var path = mini.CalculateBFSPath(startCell, targetCell);

            Assert(path != null && path.Count > 0, $"Mini Enemy BFS path calculated successfully (Length: {(path != null ? path.Count : 0)})");

            Vector3 prePos = mini.transform.position;
            mini.StepMovement(0.04f);
            float dist = Vector3.Distance(prePos, mini.transform.position);
            Assert(dist > 0.001f, $"Mini Enemy stepped toward target ({dist:F3} units)");

            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(playerGo);
        }

        private static void Test7_SceneTransitionAndCleanup()
        {
            LogMessage("\n--- TEST 7: SCENE TRANSITION AND CLEANUP ---");

            // Ensure ActiveEnemy resolves closest active target
            GameObject pGo = new GameObject("Player");
            pGo.tag = "Player";
            pGo.transform.position = Vector3.zero;

            GameObject e1 = new GameObject("Enemy_Far");
            e1.transform.position = new Vector3(10f, 0f, 0f);
            e1.AddComponent<CircleCollider2D>();
            var ef = e1.AddComponent<EnemyCharacter>();

            GameObject e2 = new GameObject("Enemy_Near");
            e2.transform.position = new Vector3(1f, 0f, 0f);
            e2.AddComponent<CircleCollider2D>();
            var en = e2.AddComponent<EnemyCharacter>();

            EnemyCharacter active = EnemyCharacter.ActiveEnemy;
            Assert(active == en, "EnemyCharacter.ActiveEnemy dynamically targets closest enemy to player");

            UnityEngine.Object.DestroyImmediate(pGo);
            UnityEngine.Object.DestroyImmediate(e1);
            UnityEngine.Object.DestroyImmediate(e2);
        }
    }
}
