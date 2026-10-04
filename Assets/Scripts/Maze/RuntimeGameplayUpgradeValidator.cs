using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using MazeSpell.Gameplay;

namespace MazeSpell
{
    /// <summary>
    /// Validator runtime yang memverifikasi seluruh komponen Gameplay Upgrade aktif di dalam Play Mode.
    /// Menyimpan laporan ke Logs/runtime_gameplay_upgrade_results.log.
    /// </summary>
    public class RuntimeGameplayUpgradeValidator : MonoBehaviour
    {
        private const string LogPath = "Logs/runtime_gameplay_upgrade_results.log";
        private static bool hasRun = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoRunOnSceneLoad()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != "SampleScene") return;

            if (hasRun) return;
            hasRun = true;

            GameObject runner = new GameObject("RuntimeGameplayUpgradeValidator");
            runner.AddComponent<RuntimeGameplayUpgradeValidator>();
        }

        private void Start()
        {
            StartCoroutine(ValidationRoutine());
        }

        private IEnumerator ValidationRoutine()
        {
            // Tunggu 0.5 detik agar semua manajer dan objek gameplay siap
            yield return new WaitForSeconds(0.5f);

            StringBuilder sb = new StringBuilder();
            int passed = 0;
            int total = 0;

            void Log(string msg)
            {
                Debug.Log(msg);
                sb.AppendLine(msg);
            }

            void Assert(bool cond, string name)
            {
                total++;
                if (cond)
                {
                    passed++;
                    Log($"[PASS] {name}");
                }
                else
                {
                    Log($"[FAIL] {name}");
                }
            }

            Log("==================================================================");
            Log("=== MAZESPELL RUNTIME GAMEPLAY UPGRADE VALIDATION ===");
            Log($"=== Timestamp: {DateTime.Now} ===");
            Log("==================================================================");

            // 1. Coordinators & Managers
            Assert(GameplayUpgradeCoordinator.Instance != null, "GameplayUpgradeCoordinator is active");
            Assert(WordPowerSpellManager.Instance != null, "WordPowerSpellManager is active");
            Assert(CombatFeedbackEnhancer.Instance != null, "CombatFeedbackEnhancer is active");
            Assert(LevelCompletePopup.Instance != null, "LevelCompletePopup is active");

            // 2. Active Level Check
            int lvl = 1;
            if (LevelProgressionManager.HasInstance)
            {
                lvl = LevelProgressionManager.Instance.CurrentLevel;
            }
            Log($"Current Active Level: {lvl}");

            // 3. Secret Room Bonus Check
            var secret = GameObject.Find($"SecretBonus_Level_{lvl}");
            Assert(secret != null, $"Secret Room Bonus is spawned for Level {lvl} at {secret?.transform.position}");

            // 4. Level-specific Gimmick Check
            var gimmick = GameObject.Find("ActiveLevelGimmick");
            Assert(gimmick != null, $"Level-specific Gimmick is active for Level {lvl}");

            // 5. Enemy HP check for active level
            var enemy = EnemyCharacter.ActiveEnemy;
            if (enemy != null)
            {
                int expectedHP = EnemyCharacter.GetMaxHealthForLevel(lvl);
                Assert(enemy.MaxHealth == expectedHP, $"Enemy Max HP ({enemy.MaxHealth}) matches expected Level {lvl} HP ({expectedHP})");
                Assert(enemy.Health == expectedHP, $"Enemy Current HP ({enemy.Health}) is full");
            }
            else
            {
                Log($"[INFO] Enemy not yet spawned or searching in scene");
            }

            // 6. Player Controller Slipperiness support
            var player = GameObject.FindWithTag("Player");
            var pc = player != null ? player.GetComponent<PlayerController>() : null;
            Assert(pc != null, "PlayerController exists on Player");
            if (pc != null)
            {
                Assert(pc.CurrentSlipperiness >= 0f, "PlayerController has CurrentSlipperiness property");
            }

            // 7. Player Lives Damage Tracking
            if (PlayerLivesManager.Instance != null)
            {
                Assert(PlayerLivesManager.Instance.DamageTakenThisLevel == 0, "DamageTakenThisLevel initialized to 0");
            }

            // 8. Word Collection Target Word
            if (WordCollectionManager.Instance != null)
            {
                string word = WordCollectionManager.Instance.CurrentTargetWord;
                Assert(!string.IsNullOrEmpty(word), $"Target word for Level {lvl} is '{word}'");
            }

            Log("==================================================================");
            Log($"=== RUNTIME RESULT: {passed}/{total} CHECKS PASSED ===");
            Log("==================================================================");

            try
            {
                Directory.CreateDirectory("Logs");
                File.WriteAllText(LogPath, sb.ToString());
                Debug.Log($"[RuntimeGameplayUpgradeValidator] Results saved to {LogPath}");
            }
            catch { }
        }
    }
}
