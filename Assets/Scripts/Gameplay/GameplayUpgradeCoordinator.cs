using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MazeSpell.Gameplay
{
    /// <summary>
    /// Koordinator utama seluruh sistem Upgrade Gameplay MazeSpell:
    /// 1. WordPowerSpellManager (Fitur 1, 2, 8)
    /// 2. LevelGimmickController (Fitur 5)
    /// 3. SecretBonusManager (Fitur 3, 10)
    /// 4. CombatFeedbackEnhancer (Fitur 6, 7)
    /// 5. LevelCompletePopup (Fitur 9)
    /// 6. StarRatingManager (Fitur 4, 13)
    /// Menjamin semua fitur aktif secara mulus dan otomatis saat level dimainkan,
    /// tanpa merusak sistem dasar atau alur scene sebelumnya.
    /// </summary>
    public class GameplayUpgradeCoordinator : MonoBehaviour
    {
        public static GameplayUpgradeCoordinator Instance { get; private set; }

        private bool isHooked = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitializeRuntime()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoadedGlobal;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoadedGlobal;
        }

        private static void OnSceneLoadedGlobal(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            if (scene.name == "SampleScene")
            {
                EnsureCoordinatorExists();
            }
        }

        public static GameplayUpgradeCoordinator EnsureCoordinatorExists()
        {
            if (Instance != null)
            {
                Instance.EnsureChildManagers();
                return Instance;
            }

#if UNITY_2023_1_OR_NEWER
            var found = Object.FindAnyObjectByType<GameplayUpgradeCoordinator>();
#else
            var found = Object.FindObjectOfType<GameplayUpgradeCoordinator>();
#endif
            if (found != null)
            {
                Instance = found;
                Instance.EnsureChildManagers();
                return Instance;
            }

            GameObject coordObj = new GameObject("GameplayUpgradeCoordinator");
            Instance = coordObj.AddComponent<GameplayUpgradeCoordinator>();
            Instance.EnsureChildManagers();
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureChildManagers();
        }

        private void Start()
        {
            HookEvents();

            int initialLevel = 1;
            if (LevelProgressionManager.HasInstance)
            {
                initialLevel = LevelProgressionManager.Instance.CurrentLevel;
            }
            SetupLevelGameplay(initialLevel);
        }

        private void OnDestroy()
        {
            UnhookEvents();
            if (Instance == this) Instance = null;
        }

        private void EnsureChildManagers()
        {
            // 0. Maze Gameplay Manager
            MazeSpell.MazeGameplayManager.EnsureManagerExists();

            // 1. Word Power Spell Manager
            var word = GetComponent<WordPowerSpellManager>();
            if (word == null)
            {
                word = gameObject.AddComponent<WordPowerSpellManager>();
            }
            word.HookEvents();

            // 2. Combat Feedback Enhancer
            var combat = GetComponent<CombatFeedbackEnhancer>();
            if (combat == null)
            {
                gameObject.AddComponent<CombatFeedbackEnhancer>();
            }

            // 3. Level Complete Popup
            var popup = GetComponent<LevelCompletePopup>();
            if (popup == null)
            {
                gameObject.AddComponent<LevelCompletePopup>();
            }

            // 4. Final Boss Battle Manager (Level 5)
            var boss = GetComponent<FinalBossBattleManager>();
            if (boss == null)
            {
                boss = gameObject.AddComponent<FinalBossBattleManager>();
            }
            boss.HookEvents();

            // 5. Final Guardian HUD
            var hud = GetComponent<FinalGuardianHUD>();
            if (hud == null)
            {
                gameObject.AddComponent<FinalGuardianHUD>();
            }
        }

        private void HookEvents()
        {
            if (isHooked) return;
            isHooked = true;

            if (LevelProgressionManager.HasInstance && LevelProgressionManager.Instance.onLevelStarted != null)
            {
                LevelProgressionManager.Instance.onLevelStarted.AddListener(OnLevelStarted);
            }
        }

        private void UnhookEvents()
        {
            if (!isHooked) return;
            isHooked = false;

            if (LevelProgressionManager.HasInstance && LevelProgressionManager.Instance.onLevelStarted != null)
            {
                LevelProgressionManager.Instance.onLevelStarted.RemoveListener(OnLevelStarted);
            }
        }

        private void OnLevelStarted(int level)
        {
            SetupLevelGameplay(level);
        }

        /// <summary>
        /// Menginisialisasi seluruh gimmick dan bonus saat level baru dimuat.
        /// </summary>
        public void SetupLevelGameplay(int level)
        {
            StartCoroutine(SetupLevelDeferred(level));
        }

        private IEnumerator SetupLevelDeferred(int level)
        {
            // Tunggu 1 frame agar grid labirin & pemain sudah terposisikan
            yield return null;

            Debug.Log($"[GameplayUpgradeCoordinator] Mempersiapkan Upgrade Gameplay untuk Level {level}...");

            if (level == 5)
            {
                Debug.Log("[MAZESPELL RUNTIME]\nLevel 5 started");
                var player = GameObject.FindWithTag("Player");
                var gameplayMgr = MazeGameplayManager.Instance;
                var coord = Instance;
                var wordMgr = WordPowerSpellManager.Instance;
                var bossMgr = FinalBossBattleManager.Instance;
                var enemy = EnemyCharacter.ActiveEnemy != null ? EnemyCharacter.ActiveEnemy : Object.FindAnyObjectByType<EnemyCharacter>();
                var hud = FinalGuardianHUD.Instance;

                Debug.Log($"[MAZESPELL RUNTIME]\n- Player: {(player != null ? player.name : "null")}\n- MazeGameplayManager: {(gameplayMgr != null ? "active" : "null")}\n- GameplayUpgradeCoordinator: {(coord != null ? "active" : "null")}\n- WordPowerSpellManager: {(wordMgr != null ? "active" : "null")}\n- FinalBossBattleManager: {(bossMgr != null ? "active" : "null")}\n- EnemyCharacter: {(enemy != null ? enemy.name : "null")}\n- FinalGuardianHUD: {(hud != null ? "active" : "null")}");
            }

            // 1. Reset Star Rating tracking untuk level ini
            StarRatingManager.ResetLevelTracking();

            // 2. Dapatkan Transform grid level jika ada
            GameObject gridObj = GameObject.Find($"Level{level}_MazeGrid");
            Transform parentGrid = gridObj != null ? gridObj.transform : null;

            // 3. Pasang Gimmick Khas Level (Fitur 5)
            LevelGimmickController.SetupGimmicksForLevel(level, parentGrid);

            // 4. Pasang Secret Room Bonus Collectible (Fitur 3 & 10)
            SecretBonusManager.SpawnSecretBonusForLevel(level, parentGrid);

            Debug.Log($"[GameplayUpgradeCoordinator] ✓ Upgrade Gameplay Level {level} telah siap sepenuhnya!");
        }
    }
}
