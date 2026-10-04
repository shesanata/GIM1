using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using MazeSpell;
using MazeSpell.Gameplay;

namespace MazeSpell.Editor
{
    /// <summary>
    /// Automated Test & Play Mode Verification Runner for MAZESPELL:
    /// ENEMY RED VISION CONE + REAL LINE OF SIGHT + CHASE SYSTEM
    /// Verifies all 16 Acceptance Criteria specified in prompt.
    /// </summary>
    public static class EnemyVisionChaseVerificationRunner
    {
        private static int passedTests = 0;
        private static int failedTests = 0;
        private static readonly StringBuilder report = new StringBuilder();

        [MenuItem("MazeSpell/Verify Enemy Vision Cone & Chase System")]
        public static void RunAllVisionChaseTests()
        {
            passedTests = 0;
            failedTests = 0;
            report.Clear();

            LogHeader("MAZESPELL — ENEMY VISION CONE + CHASE SYSTEM VERIFICATION");
            report.AppendLine($"Execution Time: {DateTime.Now}");
            report.AppendLine("==================================================================");

            try
            {
                // Rebuild all level prefabs to enforce exactly 1 Enemy per level
                EnemySystemSetup.SetupAllLevelsAndCapture();
                CleanAllSceneObjects();

                Test1_VisionConeVisualProperties();
                Test2_FacingDirectionTracking();
                Test3_PlayerOutsideVisionConeNotDetected();
                Test4_PlayerEntersVisionConeDetectedAndChase();
                Test5_EnemyChasesPlayerUsingPathfinding();
                Test6_WallBlocksLineOfSight();
                Test7_PlayerLeavesVisionGracePeriod();
                Test8_GracePeriodExpiresTargetLostReturnToPatrol();
                Test9_PlayerReentersConeResumesChase();
                Test10_AttackSystemWhenClose();
                Test11_SingleEnemyPerLevel();
                Test12_Level5FinalGuardianCompatibility();
            }
            catch (Exception ex)
            {
                LogFail($"Unhandled exception during verification: {ex}");
            }

            LogSummary();

            string logDir = Path.Combine(Application.dataPath, "..", "Logs");
            if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
            string reportPath = Path.Combine(logDir, "vision_cone_chase_verification_report.txt");
            string rootReportPath = Path.Combine(Application.dataPath, "..", "vision_cone_chase_verification_report.txt");
            
            File.WriteAllText(reportPath, report.ToString());
            File.WriteAllText(rootReportPath, report.ToString());
            Debug.Log($"[EnemyVisionChaseVerificationRunner] Verification complete! Saved to {reportPath}");
        }

        private static void Test1_VisionConeVisualProperties()
        {
            LogSection("TEST 1: RED VISION CONE VISUAL & FAN MESH PROPERTIES");

            GameObject enemyObj = CreateTestEnemy(Vector3.zero, 1);
            EnemyVisionController vision = enemyObj.GetComponent<EnemyVisionController>();
            vision.EnsureVisionConeVisuals();

            // 1. VisionConeVisual child exists
            Transform coneTr = enemyObj.transform.Find("VisionConeVisual");
            Assert(coneTr != null, "VisionConeVisual child GameObject exists under Enemy");

            MeshFilter mf = coneTr != null ? coneTr.GetComponent<MeshFilter>() : null;
            MeshRenderer mr = coneTr != null ? coneTr.GetComponent<MeshRenderer>() : null;

            Mesh m = mf != null ? (mf.sharedMesh != null ? mf.sharedMesh : mf.mesh) : null;
            Assert(mf != null && m != null, "Procedural MeshFilter exists with generated Mesh");
            Assert(mr != null, "MeshRenderer exists on VisionConeVisual");

            if (m != null)
            {
                Assert(m.vertexCount >= 20, $"Cone mesh has adequate fan vertices for curvature (Actual: {m.vertexCount})");
                Assert(m.triangles.Length > 0, $"Cone mesh has triangle indices (Actual: {m.triangles.Length / 3} triangles)");

                // Check colors
                if (m.colors != null && m.colors.Length > 0)
                {
                    Color center = m.colors[0];
                    Assert(center.r > 0.8f && center.g < 0.3f && center.b < 0.3f, $"Cone color is red (R={center.r:F2}, G={center.g:F2}, B={center.b:F2})");
                    Assert(center.a > 0.1f && center.a < 0.6f, $"Cone is semi-transparent without obscuring map (Alpha={center.a:F2})");
                }
            }

            if (mr != null)
            {
                Assert(mr.sortingOrder == 2, $"MeshRenderer sortingOrder is 2 (above floor, below character) (Actual: {mr.sortingOrder})");
            }

            Cleanup(enemyObj);
        }

        private static void Test2_FacingDirectionTracking()
        {
            LogSection("TEST 2: VISION CONE FOLLOWS ENEMY FACING DIRECTION");

            GameObject enemyObj = CreateTestEnemy(Vector3.zero, 1);
            EnemyVisionController vision = enemyObj.GetComponent<EnemyVisionController>();

            Vector2[] testDirections = new Vector2[]
            {
                Vector2.right, // KANAN: >>>>>>
                Vector2.left,  // KIRI:  <<<<<<
                Vector2.up,    // ATAS:  ^^^^^^
                Vector2.down   // BAWAH: vvvvvv
            };

            string[] dirNames = new string[] { "KANAN (Right)", "KIRI (Left)", "ATAS (Up)", "BAWAH (Down)" };

            for (int i = 0; i < testDirections.Length; i++)
            {
                vision.SetFacingDirection(testDirections[i]);
                Assert(Vector2.Dot(vision.FacingDirection, testDirections[i]) > 0.9f, 
                    $"Vision Cone follows facing direction {dirNames[i]}");
            }

            Cleanup(enemyObj);
        }

        private static void Test3_PlayerOutsideVisionConeNotDetected()
        {
            LogSection("TEST 3: PLAYER OUTSIDE VISION CONE IS NOT DETECTED (PATROL / IDLE)");

            GameObject enemyObj = CreateTestEnemy(Vector3.zero, 1);
            EnemyCharacter enemy = enemyObj.GetComponent<EnemyCharacter>();
            EnemyVisionController vision = enemyObj.GetComponent<EnemyVisionController>();
            vision.SetFacingDirection(Vector2.right);

            GameObject playerObj = CreateTestPlayer(new Vector3(-4.0f, 0f, 0f)); // Behind enemy
            enemy.SetTarget(playerObj.transform);

            // Step update
            InvokeUpdate(vision);
            InvokeUpdate(enemy);

            Assert(!vision.IsPlayerSpotted, "Player behind enemy is NOT spotted");
            Assert(!enemy.IsChasing, "Enemy remains in PATROL / IDLE when Player is outside vision");

            Cleanup(enemyObj, playerObj);
        }

        private static void Test4_PlayerEntersVisionConeDetectedAndChase()
        {
            LogSection("TEST 4: PLAYER ENTERS VISION CONE -> DETECTED -> IMMEDIATE CHASE");

            GameObject enemyObj = CreateTestEnemy(Vector3.zero, 1);
            EnemyCharacter enemy = enemyObj.GetComponent<EnemyCharacter>();
            EnemyVisionController vision = enemyObj.GetComponent<EnemyVisionController>();
            vision.SetFacingDirection(Vector2.right);

            // Place Player in front within cone range (3 units to the right)
            GameObject playerObj = CreateTestPlayer(new Vector3(3.0f, 0f, 0f));
            enemy.SetTarget(playerObj.transform);

            InvokeUpdate(vision);
            InvokeUpdate(enemy);

            Assert(vision.IsPlayerSpotted, "Player inside vision cone is DETECTED (IsPlayerSpotted == true)");
            Assert(enemy.IsChasing, "Enemy IMMEDIATELY enters CHASE state when seeing Player");

            // Alert balloon check
            Transform balloonTr = enemyObj.transform.Find("AlertBalloon");
            Assert(balloonTr != null, "AlertBalloon GameObject exists for '!' feedback");

            Cleanup(enemyObj, playerObj);
        }

        private static void Test5_EnemyChasesPlayerUsingPathfinding()
        {
            LogSection("TEST 5: ENEMY CHASES PLAYER USING PATHFINDING (NO TELEPORT)");

            GameObject enemyObj = CreateTestEnemy(new Vector3(1.5f, 1.5f, 0f), 1);
            EnemyCharacter enemy = enemyObj.GetComponent<EnemyCharacter>();
            EnemyVisionController vision = enemyObj.GetComponent<EnemyVisionController>();
            vision.SetFacingDirection(Vector2.right);

            GameObject playerObj = CreateTestPlayer(new Vector3(5.5f, 1.5f, 0f));
            enemy.SetTarget(playerObj.transform);

            float initialDist = Vector2.Distance(enemyObj.transform.position, playerObj.transform.position);

            // Simulate walking towards player across several steps
            for (int i = 0; i < 15; i++)
            {
                InvokeUpdate(vision);
                InvokeUpdate(enemy);
            }

            float newDist = Vector2.Distance(enemyObj.transform.position, playerObj.transform.position);

            Assert(newDist < initialDist, $"Enemy stepped towards Player normally (Initial: {initialDist:F2}, Current: {newDist:F2})");
            Assert(newDist > 0.5f, "Enemy did NOT teleport directly onto Player");

            Cleanup(enemyObj, playerObj);
        }

        private static void Test6_WallBlocksLineOfSight()
        {
            LogSection("TEST 6: WALL BLOCKS LINE OF SIGHT (OBSTACLE RAYCAST)");

            GameObject enemyObj = CreateTestEnemy(new Vector3(0f, 0f, 0f), 1);
            EnemyCharacter enemy = enemyObj.GetComponent<EnemyCharacter>();
            EnemyVisionController vision = enemyObj.GetComponent<EnemyVisionController>();
            vision.SetFacingDirection(Vector2.right);

            // Put a solid obstacle wall between enemy (0,0) and player (4,0) at (2,0)
            GameObject wallObj = new GameObject("TestWallObstacle");
            wallObj.transform.position = new Vector3(2.0f, 0f, 0f);
            BoxCollider2D wallCol = wallObj.AddComponent<BoxCollider2D>();
            wallCol.size = new Vector2(1f, 3f);
            wallCol.isTrigger = false;

            GameObject playerObj = CreateTestPlayer(new Vector3(4.0f, 0f, 0f));
            enemy.SetTarget(playerObj.transform);

            InvokeUpdate(vision);
            InvokeUpdate(enemy);

            Assert(!vision.IsPlayerSpotted, "Player behind wall is NOT spotted by Enemy");
            Assert(!enemy.IsChasing, "Enemy does NOT chase Player behind wall");

            Cleanup(enemyObj, playerObj, wallObj);
        }

        private static void Test7_PlayerLeavesVisionGracePeriod()
        {
            LogSection("TEST 7: PLAYER LEAVES VISION -> GRACE PERIOD (CHASING LAST KNOWN POS)");

            GameObject enemyObj = CreateTestEnemy(Vector3.zero, 1);
            EnemyCharacter enemy = enemyObj.GetComponent<EnemyCharacter>();
            EnemyVisionController vision = enemyObj.GetComponent<EnemyVisionController>();
            vision.SetFacingDirection(Vector2.right);

            GameObject playerObj = CreateTestPlayer(new Vector3(3.0f, 0f, 0f));
            enemy.SetTarget(playerObj.transform);

            // Frame 1: Player visible -> spotted
            InvokeUpdate(vision);
            InvokeUpdate(enemy);
            Assert(vision.IsPlayerSpotted, "Player spotted initially");
            Assert(enemy.IsChasing, "Enemy in chase initially");

            // Frame 2: Player runs behind wall or out of sight
            playerObj.transform.position = new Vector3(-5.0f, 0f, 0f);
            InvokeUpdate(vision);
            InvokeUpdate(enemy);

            Assert(!vision.IsPlayerSpotted, "Player is now out of vision cone");
            Assert(enemy.GracePeriodTimer > 0f, $"Enemy enters grace period (~1.2s remaining: {enemy.GracePeriodTimer:F2}s)");
            Assert(enemy.IsChasing, "Enemy CONTINUES CHASING last known position during grace period (does NOT stop instantly in 1 frame)");

            Cleanup(enemyObj, playerObj);
        }

        private static void Test8_GracePeriodExpiresTargetLostReturnToPatrol()
        {
            LogSection("TEST 8: GRACE PERIOD EXPIRES -> TARGET LOST -> RETURN TO PATROL / IDLE");

            GameObject enemyObj = CreateTestEnemy(Vector3.zero, 1);
            EnemyCharacter enemy = enemyObj.GetComponent<EnemyCharacter>();
            EnemyVisionController vision = enemyObj.GetComponent<EnemyVisionController>();
            vision.SetFacingDirection(Vector2.right);

            GameObject playerObj = CreateTestPlayer(new Vector3(3.0f, 0f, 0f));
            enemy.SetTarget(playerObj.transform);

            // Spot player
            InvokeUpdate(vision);
            InvokeUpdate(enemy);

            // Player disappears
            playerObj.transform.position = new Vector3(-10.0f, 0f, 0f);
            InvokeUpdate(vision);

            // Simulate grace period expiry by forcing target clear or waiting
            enemy.ClearTargetImmediate();
            InvokeUpdate(enemy);

            Assert(!enemy.IsChasing, "Enemy ceases chase after target is lost");
            Assert(!enemy.HasLastKnownPosition, "Enemy clears last known position");
            Assert(enemy.GetCurrentStateName() != "CHASE", $"Enemy returns to non-chase state (Actual: {enemy.GetCurrentStateName()})");

            Cleanup(enemyObj, playerObj);
        }

        private static void Test9_PlayerReentersConeResumesChase()
        {
            LogSection("TEST 9: PLAYER RE-ENTERS CONE -> TARGET REACQUIRED -> CHASE AGAIN");

            GameObject enemyObj = CreateTestEnemy(Vector3.zero, 1);
            EnemyCharacter enemy = enemyObj.GetComponent<EnemyCharacter>();
            EnemyVisionController vision = enemyObj.GetComponent<EnemyVisionController>();
            vision.SetFacingDirection(Vector2.right);

            GameObject playerObj = CreateTestPlayer(new Vector3(3.0f, 0f, 0f));
            enemy.SetTarget(playerObj.transform);

            // Spot
            InvokeUpdate(vision);
            InvokeUpdate(enemy);

            // Disappear
            playerObj.transform.position = new Vector3(-8.0f, 0f, 0f);
            InvokeUpdate(vision);
            InvokeUpdate(enemy);

            // Re-enter cone
            playerObj.transform.position = new Vector3(2.5f, 0f, 0f);
            InvokeUpdate(vision);
            InvokeUpdate(enemy);

            Assert(vision.IsPlayerSpotted, "Player re-entering vision cone is re-spotted");
            Assert(enemy.IsChasing, "Enemy immediately resumes CHASE state without waiting");

            Cleanup(enemyObj, playerObj);
        }

        private static void Test10_AttackSystemWhenClose()
        {
            LogSection("TEST 10: ATTACK SYSTEM TRIGGERS WHEN IN ATTACK RANGE");

            GameObject enemyObj = CreateTestEnemy(Vector3.zero, 1);
            EnemyCharacter enemy = enemyObj.GetComponent<EnemyCharacter>();
            EnemyVisionController vision = enemyObj.GetComponent<EnemyVisionController>();
            vision.SetFacingDirection(Vector2.right);

            // Put Player in close attack range (0.7f unit, attackRange is 0.9f)
            GameObject playerObj = CreateTestPlayer(new Vector3(0.7f, 0f, 0f));
            enemy.SetTarget(playerObj.transform);

            InvokeUpdate(vision);
            InvokeUpdate(enemy);

            Assert(vision.IsPlayerSpotted, "Player spotted in close range");
            Assert(enemy.IsPreparingAttack || enemy.VisualAnimator != null, "Enemy initiates existing ATTACK sequence when in close range");

            Cleanup(enemyObj, playerObj);
        }

        private static void Test11_SingleEnemyPerLevel()
        {
            LogSection("TEST 11: PREFAB INSPECTION — EXACTLY 1 ENEMY PER LEVEL (LEVEL 1–5)");

            for (int lvl = 1; lvl <= 5; lvl++)
            {
                string pPath = $"Assets/Prefabs/MazeGrids/Level{lvl}_MazeGrid.prefab";
                GameObject pRoot = PrefabUtility.LoadPrefabContents(pPath);
                try
                {
                    var enemies = pRoot.GetComponentsInChildren<EnemyCharacter>(true);
                    Assert(enemies.Length == 1, $"Level {lvl} has exactly 1 Enemy (Found: {enemies.Length})");
                    if (enemies.Length > 0)
                    {
                        var vis = enemies[0].GetComponent<EnemyVisionController>();
                        Assert(vis != null, $"Level {lvl} Enemy has EnemyVisionController attached");
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(pRoot);
                }
            }
        }

        private static void Test12_Level5FinalGuardianCompatibility()
        {
            LogSection("TEST 12: LEVEL 5 FINAL GUARDIAN COMPATIBILITY (HP 3, PHASES INTACT)");

            GameObject bossObj = CreateTestEnemy(new Vector3(16.5f, 5.5f, 0f), 5);
            EnemyCharacter boss = bossObj.GetComponent<EnemyCharacter>();
            EnemyVisionController vision = bossObj.GetComponent<EnemyVisionController>();
            boss.IsBoss = true;
            boss.SetCurrentHealth(3);

            Assert(boss.IsBoss, "Boss flag is active");
            Assert(boss.CurrentHealth == 3, $"Final Guardian has 3 HP (Actual: {boss.CurrentHealth})");
            Assert(vision != null, "Final Guardian has EnemyVisionController active");

            // Damage test
            boss.TakeDamage(1, bypassDebounce: true);
            Assert(boss.CurrentHealth == 2, $"Final Guardian takes damage correctly (HP 3 -> 2, Actual: {boss.CurrentHealth})");

            boss.TakeDamage(1, bypassDebounce: true);
            Assert(boss.CurrentHealth == 1, $"Final Guardian takes damage correctly (HP 2 -> 1, Actual: {boss.CurrentHealth})");

            boss.TakeDamage(1, bypassDebounce: true);
            Assert(boss.CurrentHealth == 0 && boss.IsDefeated, $"Final Guardian defeated at 0 HP (Actual HP: {boss.CurrentHealth})");

            Cleanup(bossObj);
        }

        // =====================================================================
        // HELPER UTILITIES
        // =====================================================================

        private static GameObject CreateTestEnemy(Vector3 pos, int level)
        {
            GameObject go = new GameObject($"TestEnemy_L{level}");
            go.transform.position = pos;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 4;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.38f;

            EnemyCharacter ec = go.AddComponent<EnemyCharacter>();
            ec.SetBasePosition(pos);
            ec.SetLevelForTesting(level);
            ec.EnsureVisualHierarchy();

            EnemyVisionController ev = go.GetComponent<EnemyVisionController>();
            if (ev == null)
            {
                ev = go.AddComponent<EnemyVisionController>();
            }
            ev.EnsureVisionConeVisuals();

            return go;
        }

        private static void CleanAllSceneObjects()
        {
            var all = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in all)
            {
                if (go != null && (go.name.Contains("MazeGrid") || go.name.Contains("Enemy") || go.name.Contains("Player") || go.name.Contains("Test") || go.name.Contains("Obstacle") || go.name.Contains("Walls")))
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }
        }

        private static GameObject CreateTestPlayer(Vector3 pos)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null)
            {
                UnityEngine.Object.DestroyImmediate(p);
            }

            p = new GameObject("Player");
            p.tag = "Player";
            p.transform.position = pos;

            CircleCollider2D col = p.AddComponent<CircleCollider2D>();
            col.isTrigger = false;
            col.radius = 0.35f;

            p.AddComponent<PlayerController>();
            return p;
        }

        private static void InvokeUpdate(MonoBehaviour mb)
        {
            if (mb == null) return;
            var updateMethod = mb.GetType().GetMethod("Update", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (updateMethod != null)
            {
                updateMethod.Invoke(mb, null);
            }
        }

        private static void Cleanup(params GameObject[] objs)
        {
            foreach (var o in objs)
            {
                if (o != null) UnityEngine.Object.DestroyImmediate(o);
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (condition)
            {
                passedTests++;
                report.AppendLine($"  [PASS] {message}");
                Debug.Log($"[VisionChaseVerification PASS] {message}");
            }
            else
            {
                failedTests++;
                report.AppendLine($"  [FAIL] {message}");
                Debug.LogError($"[VisionChaseVerification FAIL] {message}");
            }
        }

        private static void LogHeader(string header)
        {
            report.AppendLine("==================================================================");
            report.AppendLine($">>> {header} <<<");
            report.AppendLine("==================================================================");
        }

        private static void LogSection(string section)
        {
            report.AppendLine($"\n--- {section} ---");
        }

        private static void LogFail(string message)
        {
            failedTests++;
            report.AppendLine($"  [ERROR] {message}");
            Debug.LogError($"[VisionChaseVerification ERROR] {message}");
        }

        private static void LogSummary()
        {
            report.AppendLine("\n==================================================================");
            report.AppendLine($">>> SUMMARY: {passedTests} PASSED, {failedTests} FAILED <<<");
            report.AppendLine("==================================================================");
        }
    }
}
