using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MazeSpell.Editor
{
    /// <summary>
    /// Test runner otomatis untuk memverifikasi sistem peningkatan difficulty Enemy bertahap di Level 1-5:
    /// - Level 1-3: HP 1, 1 hit = kalah
    /// - Level 4: HP 2, hit 1 -> HP 1 (masih hidup, 0 reward), hit 2 -> HP 0 (defeated, +1 reward)
    /// - Level 5: HP 3 (FINAL CHALLENGE), hit 1 -> HP 2, hit 2 -> HP 1, hit 3 -> HP 0 (+1 reward)
    /// - Pengujian transisi HP 3 -> 2 -> 1 -> 0
    /// - Pengujian reward: hit 1 = 0, hit 2 = 0, hit 3 = +1 (hanya setelah HP 0)
    /// - Pengujian speed: 1.5, 1.8, 2.1, 2.4, 2.7
    /// - Pengujian cooldown: L5 (0.75s) < L4 (0.85s) < L1-3 (1.0s)
    /// - Pengujian wall collision, range check, SPACE tidak merusak Player, no respawn, attack stop
    /// - Pengujian health indicator (♥ ♥ ♥ -> ♥ ♥ -> ♥ -> hidden)
    /// </summary>
    public static class EnemyDifficultyVerificationRunner
    {
        private static int passedTests = 0;
        private static int failedTests = 0;
        private static readonly StringBuilder report = new StringBuilder();

        [MenuItem("MazeSpell/Run Enemy Difficulty Verification")]
        public static void RunTests()
        {
            passedTests = 0;
            failedTests = 0;
            report.Clear();

            LogHeader("VERIFIKASI SISTEM PENINGKATAN DIFFICULTY ENEMY (LEVEL 1–5)");

            try
            {
                Test1_Level1Enemy();
                Test2_Level2Enemy();
                Test3_Level3Enemy();
                Test4_Level4EnemyMultiHit();
                Test5_Level5FinalChallengeCombatFlow();
                Test6_Level5HpIndicatorTransitions();
                Test7_Level5AdditionalConstraints();
                Test8_PrefabLevelVerification();
            }
            catch (Exception ex)
            {
                LogFail($"Exception tidak tertangani saat pengujian: {ex}");
            }

            LogSummary();

            string reportPath = "Logs/enemy_difficulty_report.txt";
            File.WriteAllText(reportPath, report.ToString());
            Debug.Log($"[EnemyDifficultyVerificationRunner] Laporan pengujian tersimpan di {reportPath}");
        }

        private static void Test1_Level1Enemy()
        {
            LogSection("PENGUJIAN LEVEL 1: ENEMY HP 1 (1 HIT = KALAH)");

            GameObject enemyObj = CreateTestEnemy(1);
            EnemyCharacter enemy = enemyObj.GetComponent<EnemyCharacter>();
            GameObject player = CreateTestPlayer();
            var livesManager = PlayerLivesManager.Instance;
            livesManager.SetLivesForTesting(2);

            Assert(enemy.Health == 1, $"Level 1 Enemy HP == 1 (Aktual: {enemy.Health})");
            Assert(Mathf.Approximately(enemy.ChaseSpeed, 1.5f), $"Level 1 Enemy Speed == 1.5 (Aktual: {enemy.ChaseSpeed})");
            Assert(Mathf.Approximately(enemy.AttackRange, 0.9f), $"Level 1 Enemy Attack Range == 0.9 (Aktual: {enemy.AttackRange})");

            // 1 serangan Player dalam range
            player.transform.position = enemyObj.transform.position + new Vector3(1.0f, 0f, 0f);
            bool hit = enemy.TryPlayerAttack(player, 1);

            Assert(hit, "Serangan Player berhasil mengenai Enemy");
            Assert(enemy.Health == 0, $"Enemy HP berkurang dari 1 menjadi 0 (Aktual: {enemy.Health})");
            Assert(enemy.IsDefeated, "Enemy langsung kalah/defeated setelah 1 hit");
            Assert(livesManager.CurrentLives == 3, $"Player mendapatkan +1 reward nyawa (Aktual: {livesManager.CurrentLives})");

            Cleanup(enemyObj, player);
        }

        private static void Test2_Level2Enemy()
        {
            LogSection("PENGUJIAN LEVEL 2: ENEMY HP 1, SPEED 1.8 (1 HIT = KALAH)");

            GameObject enemyObj = CreateTestEnemy(2);
            EnemyCharacter enemy = enemyObj.GetComponent<EnemyCharacter>();
            GameObject player = CreateTestPlayer();
            var livesManager = PlayerLivesManager.Instance;
            livesManager.SetLivesForTesting(2);

            Assert(enemy.Health == 1, $"Level 2 Enemy HP == 1 (Aktual: {enemy.Health})");
            Assert(Mathf.Approximately(enemy.ChaseSpeed, 1.8f), $"Level 2 Enemy Speed == 1.8 (Aktual: {enemy.ChaseSpeed})");

            player.transform.position = enemyObj.transform.position + new Vector3(1.0f, 0f, 0f);
            bool hit = enemy.TryPlayerAttack(player, 1);

            Assert(hit && enemy.Health == 0 && enemy.IsDefeated, "Level 2: 1 serangan Player langsung mengalahkan Enemy");
            Assert(livesManager.CurrentLives == 3, "Level 2: Player menerima +1 reward nyawa setelah kalah");

            Cleanup(enemyObj, player);
        }

        private static void Test3_Level3Enemy()
        {
            LogSection("PENGUJIAN LEVEL 3: ENEMY HP 1, SPEED 2.1 (LEBIH AGRESIF, 1 HIT = KALAH)");

            GameObject enemyObj = CreateTestEnemy(3);
            EnemyCharacter enemy = enemyObj.GetComponent<EnemyCharacter>();
            GameObject player = CreateTestPlayer();
            var livesManager = PlayerLivesManager.Instance;
            livesManager.SetLivesForTesting(2);

            Assert(enemy.Health == 1, $"Level 3 Enemy HP == 1 (Aktual: {enemy.Health})");
            Assert(Mathf.Approximately(enemy.ChaseSpeed, 2.1f), $"Level 3 Enemy Speed == 2.1 (Aktual: {enemy.ChaseSpeed})");
            Assert(EnemyCharacter.GetRepathIntervalForLevel(3) < EnemyCharacter.GetRepathIntervalForLevel(1),
                "Level 3 Enemy memiliki repath interval lebih cepat daripada Level 1");

            player.transform.position = enemyObj.transform.position + new Vector3(1.0f, 0f, 0f);
            bool hit = enemy.TryPlayerAttack(player, 1);

            Assert(hit && enemy.Health == 0 && enemy.IsDefeated, "Level 3: 1 serangan Player langsung mengalahkan Enemy");
            Assert(livesManager.CurrentLives == 3, "Level 3: Player menerima +1 reward nyawa setelah kalah");

            Cleanup(enemyObj, player);
        }

        private static void Test4_Level4EnemyMultiHit()
        {
            LogSection("PENGUJIAN LEVEL 4: ENEMY HP 2, SPEED 2.4 (2 HITS UNTUK MENGALAHKAN)");

            GameObject enemyObj = CreateTestEnemy(4);
            EnemyCharacter enemy = enemyObj.GetComponent<EnemyCharacter>();
            GameObject player = CreateTestPlayer();
            var livesManager = PlayerLivesManager.Instance;
            livesManager.SetLivesForTesting(1);

            Assert(enemy.Health == 2, $"Level 4 Enemy HP awal == 2 (Aktual: {enemy.Health})");
            Assert(Mathf.Approximately(enemy.ChaseSpeed, 2.4f), $"Level 4 Enemy Speed == 2.4 (Aktual: {enemy.ChaseSpeed})");
            Assert(enemy.DamageCooldown < 1.0f, $"Level 4 Attack Cooldown lebih cepat ({enemy.DamageCooldown}s < 1.0s)");

            player.transform.position = enemyObj.transform.position + new Vector3(1.0f, 0f, 0f);

            // HIT #1
            bool hit1 = enemy.TryPlayerAttack(player, 1);
            Assert(hit1, "Level 4: Hit 1 berhasil");
            Assert(enemy.Health == 1, $"Level 4: Hit 1 -> HP berkurang dari 2 ke 1 (Aktual: {enemy.Health})");
            Assert(!enemy.IsDefeated, "Level 4: Enemy masih hidup setelah Hit 1");
            Assert(livesManager.CurrentLives == 1, "Level 4: Player TIDAK mendapatkan reward pada Hit 1 (Reward hanya saat HP=0)");

            // HIT #2
            bool hit2 = enemy.TryPlayerAttack(player, 1);
            Assert(hit2, "Level 4: Hit 2 berhasil");
            Assert(enemy.Health == 0, $"Level 4: Hit 2 -> HP berkurang dari 1 ke 0 (Aktual: {enemy.Health})");
            Assert(enemy.IsDefeated, "Level 4: Enemy defeated setelah Hit 2");
            Assert(livesManager.CurrentLives == 2, "Level 4: Player mendapatkan +1 reward nyawa setelah Hit 2 (HP=0)");

            Cleanup(enemyObj, player);
        }

        private static void Test5_Level5FinalChallengeCombatFlow()
        {
            LogSection("PENGUJIAN LEVEL 5: FINAL CHALLENGE COMBAT (HP 3 -> 2 -> 1 -> 0, REWARD HIT 3)");

            GameObject enemyObj = CreateTestEnemy(5);
            EnemyCharacter enemy = enemyObj.GetComponent<EnemyCharacter>();
            GameObject player = CreateTestPlayer();
            var livesManager = PlayerLivesManager.Instance;
            livesManager.SetLivesForTesting(1);

            Assert(enemy.Health == 3, $"Level 5 Enemy HP awal == 3 (Aktual: {enemy.Health})");
            Assert(Mathf.Approximately(enemy.ChaseSpeed, 2.7f), $"Level 5 Enemy Speed == 2.7 (Aktual: {enemy.ChaseSpeed})");
            Assert(Mathf.Approximately(enemy.DamageCooldown, 0.75f), $"Level 5 Attack Cooldown == 0.75s (Aktual: {enemy.DamageCooldown}s)");

            player.transform.position = enemyObj.transform.position + new Vector3(1.0f, 0f, 0f);

            // SPACE #1: HP 3 -> 2
            bool hit1 = enemy.TryPlayerAttack(player, 1);
            Assert(hit1, "SPACE #1 berhasil mengenai Enemy");
            Assert(enemy.Health == 2, $"SPACE #1: HP 3 -> 2 (Aktual: {enemy.Health})");
            Assert(!enemy.IsDefeated, "SPACE #1: Enemy tetap hidup");
            Assert(livesManager.CurrentLives == 1, "SPACE #1: Tidak memberikan reward (Reward: 0)");

            // SPACE #2: HP 2 -> 1
            bool hit2 = enemy.TryPlayerAttack(player, 1);
            Assert(hit2, "SPACE #2 berhasil mengenai Enemy");
            Assert(enemy.Health == 1, $"SPACE #2: HP 2 -> 1 (Aktual: {enemy.Health})");
            Assert(!enemy.IsDefeated, "SPACE #2: Enemy tetap hidup dan semakin agresif");
            Assert(livesManager.CurrentLives == 1, "SPACE #2: Tidak memberikan reward (Reward: 0)");

            // SPACE #3: HP 1 -> 0
            bool hit3 = enemy.TryPlayerAttack(player, 1);
            Assert(hit3, "SPACE #3 berhasil mengenai Enemy");
            Assert(enemy.Health == 0, $"SPACE #3: HP 1 -> 0 (Aktual: {enemy.Health})");
            Assert(enemy.IsDefeated, "SPACE #3: Enemy defeated");
            Assert(livesManager.CurrentLives == 2, "SPACE #3: Reward +1 nyawa diberikan setelah HP benar-benar 0");

            // Serangan tambahan setelah defeated tidak memberi reward ganda
            bool hitExtra = enemy.TryPlayerAttack(player, 1);
            Assert(!hitExtra, "Serangan pada Enemy yang sudah kalah diabaikan");
            Assert(livesManager.CurrentLives == 2, "Reward tidak berulang (tetap 2)");

            Cleanup(enemyObj, player);
        }

        private static void Test6_Level5HpIndicatorTransitions()
        {
            LogSection("PENGUJIAN INDIKATOR HP ENEMY (♥ ♥ ♥ -> ♥ ♥ -> ♥ -> HIDDEN)");

            GameObject enemyObj = CreateTestEnemy(5);
            EnemyCharacter enemy = enemyObj.GetComponent<EnemyCharacter>();
            EnemyHealthIndicator indicator = enemyObj.GetComponentInChildren<EnemyHealthIndicator>(true);

            Assert(indicator != null, "Indikator HP (EnemyHealthIndicator) terpasang pada Enemy");

            // Kondisi awal (HP 3/3) -> 3 hati aktif
            Assert(indicator.CurrentHealth == 3, "Indikator HP awal menampilkan 3 hati");

            // Hit 1 -> HP 2
            enemy.TakeDamage(1);
            Assert(indicator.CurrentHealth == 2, "Indikator HP menampilkan 2 hati setelah Hit 1");

            // Hit 2 -> HP 1
            enemy.TakeDamage(1);
            Assert(indicator.CurrentHealth == 1, "Indikator HP menampilkan 1 hati setelah Hit 2");

            // Hit 3 -> Defeated (HP 0)
            enemy.TakeDamage(1);
            Assert(indicator.CurrentHealth == 0, "Indikator HP kosong setelah Hit 3 (Defeated)");

            Cleanup(enemyObj, null);
        }

        private static void Test7_Level5AdditionalConstraints()
        {
            LogSection("PENGUJIAN ATURAN TAMBAHAN & FAIRNESS LEVEL 5");

            GameObject enemyObj = CreateTestEnemy(5);
            EnemyCharacter enemy = enemyObj.GetComponent<EnemyCharacter>();
            GameObject player = CreateTestPlayer();
            var livesManager = PlayerLivesManager.Instance;
            livesManager.ResetLives();

            // 1. Attack Cooldown Level 5 lebih cepat dari Level 4
            float cd4 = EnemyCharacter.GetAttackCooldownForLevel(4);
            float cd5 = EnemyCharacter.GetAttackCooldownForLevel(5);
            Assert(cd5 < cd4, $"Attack Cooldown Level 5 ({cd5}s) lebih cepat daripada Level 4 ({cd4}s)");

            // 2. Enemy tidak bisa menyerang dari jauh
            player.transform.position = enemyObj.transform.position + new Vector3(3.0f, 0f, 0f); // 3.0 > 0.9
            enemy.ResetDamageCooldown();
            enemy.CheckAndDamagePlayer(player);
            Assert(livesManager.CurrentLives == 3, "Enemy tidak bisa menyerang dari jauh (Player nyawa aman = 3)");

            // 3. SPACE tidak mengurangi HP Player
            enemy.TryPlayerAttack(player); // Jarak 3.0 > 1.4 -> miss
            Assert(livesManager.CurrentLives == 3, "SPACE di luar jangkauan TIDAK mengurangi HP Player");

            // 4. Enemy tidak bisa menembus wall
            Vector3Int wallCell = new Vector3Int(-1, 0, 0); // Sel dinding/luar batas labirin
            Assert(!enemy.IsCellWalkable(wallCell), "Enemy mendeteksi sel dinding sebagai non-walkable");

            // 5. Enemy berhenti menyerang setelah defeated
            enemy.Defeat();
            player.transform.position = enemyObj.transform.position + new Vector3(0.5f, 0f, 0f); // Sangat dekat
            enemy.ResetDamageCooldown();
            enemy.CheckAndDamagePlayer(player);
            Assert(livesManager.CurrentLives == 3, "Enemy yang kalah berhenti menyerang dan tidak merusak Player");

            // 6. Collider dinonaktifkan saat kalah
            Collider2D col = enemyObj.GetComponent<Collider2D>();
            Assert(!col.enabled, "Collider Enemy dinonaktifkan saat defeated (tidak respawn/double trigger)");

            Cleanup(enemyObj, player);
        }

        private static void Test8_PrefabLevelVerification()
        {
            LogSection("PENGUJIAN KONSISTENSI PREFAB LEVEL 1–5");

            string[] paths = new string[]
            {
                "Assets/Prefabs/MazeGrids/Level1_MazeGrid.prefab",
                "Assets/Prefabs/MazeGrids/Level2_MazeGrid.prefab",
                "Assets/Prefabs/MazeGrids/Level3_MazeGrid.prefab",
                "Assets/Prefabs/MazeGrids/Level4_MazeGrid.prefab",
                "Assets/Prefabs/MazeGrids/Level5_MazeGrid.prefab"
            };

            int[] expectedHps = new int[] { 1, 1, 1, 2, 3 };
            float[] expectedSpeeds = new float[] { 1.5f, 1.8f, 2.1f, 2.4f, 2.7f };
            float[] expectedCooldowns = new float[] { 1.0f, 1.0f, 1.0f, 0.85f, 0.75f };

            for (int i = 0; i < paths.Length; i++)
            {
                int lvl = i + 1;
                GameObject root = PrefabUtility.LoadPrefabContents(paths[i]);
                Assert(root != null, $"Prefab Level {lvl} berhasil dimuat");

                if (root != null)
                {
                    EnemyCharacter enemy = root.GetComponentInChildren<EnemyCharacter>(true);
                    Assert(enemy != null, $"Level {lvl} memiliki komponen EnemyCharacter");

                    if (enemy != null)
                    {
                        Assert(enemy.Health == expectedHps[i], $"Level {lvl} Prefab Enemy HP == {expectedHps[i]} (Aktual: {enemy.Health})");
                        Assert(Mathf.Approximately(enemy.ChaseSpeed, expectedSpeeds[i]), $"Level {lvl} Prefab Enemy Speed == {expectedSpeeds[i]} (Aktual: {enemy.ChaseSpeed})");
                        Assert(Mathf.Approximately(enemy.DamageCooldown, expectedCooldowns[i]), $"Level {lvl} Prefab Enemy Cooldown == {expectedCooldowns[i]}s (Aktual: {enemy.DamageCooldown}s)");

                        EnemyHealthIndicator ind = enemy.GetComponentInChildren<EnemyHealthIndicator>(true);
                        Assert(ind != null, $"Level {lvl} Enemy memiliki EnemyHealthIndicator");
                    }

                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        private static GameObject CreateTestEnemy(int level)
        {
            GameObject go = new GameObject($"TestEnemy_L{level}");
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.38f;
            var enemy = go.AddComponent<EnemyCharacter>();
            enemy.EnsureVisualHierarchy();
            enemy.EnsureHealthIndicator();
            enemy.SetLevel(level);
            return go;
        }

        private static GameObject CreateTestPlayer()
        {
            GameObject go = new GameObject("TestPlayer");
            go.tag = "Player";
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            go.AddComponent<PlayerController>();
            return go;
        }

        private static void Cleanup(GameObject enemy, GameObject player)
        {
            if (enemy != null) UnityEngine.Object.DestroyImmediate(enemy);
            if (player != null) UnityEngine.Object.DestroyImmediate(player);
        }

        private static void Assert(bool condition, string message)
        {
            if (condition)
            {
                passedTests++;
                report.AppendLine($"[PASS] {message}");
            }
            else
            {
                failedTests++;
                report.AppendLine($"[FAIL] {message}");
                Debug.LogError($"[EnemyDifficultyVerificationRunner FAIL] {message}");
            }
        }

        private static void LogHeader(string title)
        {
            report.AppendLine("==================================================================");
            report.AppendLine($">>> {title} <<<");
            report.AppendLine($"Waktu: {DateTime.Now}");
            report.AppendLine("==================================================================");
            report.AppendLine();
        }

        private static void LogSection(string sectionName)
        {
            report.AppendLine();
            report.AppendLine($"--- {sectionName} ---");
        }

        private static void LogFail(string message)
        {
            failedTests++;
            report.AppendLine($"[CRITICAL FAIL] {message}");
            Debug.LogError($"[EnemyDifficultyVerificationRunner CRITICAL] {message}");
        }

        private static void LogSummary()
        {
            report.AppendLine();
            report.AppendLine("==================================================================");
            report.AppendLine($"TOTAL TEST BERHASIL: {passedTests}");
            report.AppendLine($"TOTAL TEST GAGAL   : {failedTests}");
            report.AppendLine($"STATUS AKHIR       : {(failedTests == 0 ? "SEMUA PENGUJIAN LULUS 100%!" : "ADA TEST YANG GAGAL!")}");
            report.AppendLine("==================================================================");
        }
    }
}
