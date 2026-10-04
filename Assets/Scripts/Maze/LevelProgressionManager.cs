using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace MazeSpell
{
    /// <summary>
    /// Mengatur alur Level 1 sampai Level 5 pada game MazeSpell.
    /// Memuat prefab maze grid, mengatur kamera, memposisikan pemain,
    /// serta mengelola transisi level setelah seluruh huruf kata target terkumpul.
    /// </summary>
    public class LevelProgressionManager : MonoBehaviour
    {
        private static LevelProgressionManager _instance;
        public static LevelProgressionManager Instance
        {
            get
            {
                if (_instance == null)
                {
#if UNITY_2023_1_OR_NEWER
                    _instance = Object.FindAnyObjectByType<LevelProgressionManager>();
#else
                    _instance = Object.FindObjectOfType<LevelProgressionManager>();
#endif
                    if (_instance == null)
                    {
                        var go = new GameObject("LevelProgressionManager");
                        _instance = go.AddComponent<LevelProgressionManager>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        public static bool HasInstance => _instance != null;

        public const int TotalLevels = 5;
        public const string PREFS_HIGHEST_UNLOCKED_LEVEL = "MazeSpell_HighestUnlockedLevel";

        public static int HighestUnlockedLevel
        {
            get => PlayerPrefs.GetInt(PREFS_HIGHEST_UNLOCKED_LEVEL, 1);
            set
            {
                int clamped = Mathf.Clamp(value, 1, TotalLevels);
                if (clamped > HighestUnlockedLevel)
                {
                    PlayerPrefs.SetInt(PREFS_HIGHEST_UNLOCKED_LEVEL, clamped);
                    PlayerPrefs.Save();
                }
            }
        }

        public static bool IsLevelUnlocked(int levelNumber)
        {
            if (levelNumber <= 1) return true;
            return levelNumber <= HighestUnlockedLevel;
        }

        public static void UnlockLevel(int levelNumber)
        {
            if (levelNumber > HighestUnlockedLevel)
            {
                HighestUnlockedLevel = levelNumber;
            }
        }

        public static void ResetProgression()
        {
            PlayerPrefs.SetInt(PREFS_HIGHEST_UNLOCKED_LEVEL, 1);
            PlayerPrefs.Save();
        }

        [System.Serializable]
        public class LevelConfig
        {
            public int levelNumber = 1;
            public MazeLevelData levelData;
            public GameObject mazeGridPrefab;
        }

        [Header("Konfigurasi Level (1 - 5)")]
        [SerializeField]
        private LevelConfig[] levelConfigs = new LevelConfig[TotalLevels];

        [Header("Status Level")]
        [SerializeField] private int currentLevel = 1;
        [SerializeField] private bool isGameCompleted = false;
        [SerializeField] private bool autoLoadOnStart = true;

        [Header("Pemain")]
        [SerializeField] private GameObject playerObject;
        [SerializeField] private Vector3 playerSpawnPosition = Vector3.zero;

        [Header("Kamera")]
        [SerializeField] private bool autoAdjustCamera = true;

        [Header("Debug HUD (IMGUI)")]
        [Tooltip("Aktifkan hanya jika ingin melihat box debug OnGUI")]
        [SerializeField] private bool showHUD = false;

        [Header("Events")]
        public UnityEvent<int> onLevelStarted = new UnityEvent<int>();
        public UnityEvent<int> onLevelCompleted = new UnityEvent<int>();
        public UnityEvent onAllLevelsCompleted = new UnityEvent();

        private GameObject currentGridInstance;
        private bool isTransitioning = false;

        public int CurrentLevel => currentLevel;
        public bool IsGameCompleted => isGameCompleted;

        public LevelConfig CurrentConfig => GetConfigForLevel(currentLevel);

        public MazeLevelData CurrentData => CurrentConfig != null ? CurrentConfig.levelData : null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitializeOnSceneLoad()
        {
            var inst = Instance;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }

            EnsureUnityEvents();

            MazeSpell.Gameplay.GameplayUpgradeCoordinator.EnsureCoordinatorExists();

#if UNITY_EDITOR
            EnsureLevelConfigsPopulated();
#endif
        }

        private void EnsureUnityEvents()
        {
            if (onLevelStarted == null) onLevelStarted = new UnityEvent<int>();
            if (onLevelCompleted == null) onLevelCompleted = new UnityEvent<int>();
            if (onAllLevelsCompleted == null) onAllLevelsCompleted = new UnityEvent();
        }

        private void Start()
        {
            FindPlayer();

            if (autoLoadOnStart)
            {
                LoadLevel(currentLevel);
            }
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            EnsureLevelConfigsPopulated();
#endif
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// Mengisi konfigurasi 5 level secara otomatis jika belum lengkap di Inspector.
        /// </summary>
        public void EnsureLevelConfigsPopulated()
        {
            if (levelConfigs == null || levelConfigs.Length != TotalLevels)
            {
                levelConfigs = new LevelConfig[TotalLevels];
            }

            for (int i = 0; i < TotalLevels; i++)
            {
                int lvl = i + 1;
                if (levelConfigs[i] == null)
                {
                    levelConfigs[i] = new LevelConfig { levelNumber = lvl };
                }
                else
                {
                    levelConfigs[i].levelNumber = lvl;
                }

#if UNITY_EDITOR
                if (levelConfigs[i].mazeGridPrefab == null)
                {
                    levelConfigs[i].mazeGridPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                        $"Assets/Prefabs/MazeGrids/Level{lvl}_MazeGrid.prefab"
                    );
                }

                if (levelConfigs[i].levelData == null)
                {
                    levelConfigs[i].levelData = UnityEditor.AssetDatabase.LoadAssetAtPath<MazeLevelData>(
                        $"Assets/Tilemaps/Level{lvl}/Level{lvl}_Data.asset"
                    );
                }
#endif
            }
        }

        /// <summary>
        /// Memuat labirin untuk level tertentu, menyusun player di SpawnPoint,
        /// menyiapkan trigger Goal, dan menyetel kamera.
        /// </summary>
        public void LoadLevel(int levelNumber)
        {
            if (levelNumber < 1 || levelNumber > TotalLevels)
            {
                Debug.LogWarning($"[LevelProgressionManager] Nomor level {levelNumber} tidak valid (harus 1 - {TotalLevels}).");
                return;
            }

            currentLevel = levelNumber;
            isGameCompleted = false;
            isTransitioning = false;

            Debug.Log($"[LevelProgressionManager] Mempersiapkan Level {currentLevel}/{TotalLevels}...");

            // 1. Bersihkan grid maze sebelumnya jika ada
            if (currentGridInstance != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(currentGridInstance);
                else
                    Destroy(currentGridInstance);
#else
                Destroy(currentGridInstance);
#endif
                currentGridInstance = null;
            }

            for (int i = 1; i <= TotalLevels; i++)
            {
                GameObject oldGrid = GameObject.Find($"Level{i}_MazeGrid");
                if (oldGrid != null)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        DestroyImmediate(oldGrid);
                    else
                        Destroy(oldGrid);
#else
                    Destroy(oldGrid);
#endif
                }
            }

            LevelConfig config = GetConfigForLevel(currentLevel);

            // 2. Instantiate Prefab Maze Grid
            if (config != null && config.mazeGridPrefab != null)
            {
                currentGridInstance = Instantiate(
                    config.mazeGridPrefab,
                    Vector3.zero,
                    Quaternion.identity
                );
                currentGridInstance.name = $"Level{currentLevel}_MazeGrid";

                EnsureMazeGridPopulated(currentGridInstance, currentLevel);
            }
            else
            {
                Debug.LogWarning($"[LevelProgressionManager] Prefab untuk Level {currentLevel} belum terpasang!");
            }

            // 3. Setup Objek Goal di dalam labirin
            SetupGoalTriggerInGrid();

            // 4. Setup Posisi Pemain berdasarkan SpawnPoint labirin
            SetupPlayerPositionInGrid();

            // 5. Sesuaikan Kamera dengan batas labirin
            if (autoAdjustCamera)
            {
                AdjustCamera(config != null ? config.levelData : null);
            }

            Debug.Log($"[LevelProgressionManager] ★ LEVEL {currentLevel}/{TotalLevels} DIMULAI ★");

            // 6. Sinkronisasi langsung manager gameplay
            MazeSpell.Gameplay.GameplayUpgradeCoordinator.EnsureCoordinatorExists();

            if (WordCollectionManager.Instance != null)
            {
                WordCollectionManager.Instance.SetLevelWord(currentLevel);
            }

            if (PlayerLivesManager.Instance != null)
            {
                PlayerLivesManager.Instance.ResetLives();
            }

            // 7. Refresh dan sinkronisasi EnemyCharacter di dalam grid baru
#if UNITY_2023_1_OR_NEWER
            var enemies = FindObjectsByType<EnemyCharacter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
            var enemies = FindObjectsOfType<EnemyCharacter>();
#endif
            foreach (var en in enemies)
            {
                en.ClearTilemapCache();
                en.UpdateStatsForCurrentLevel();
            }

            // 8. Picu Event agar WordCollectionManager, PlayerLivesManager, HUD, dan AudioManager sinkron
            onLevelStarted?.Invoke(currentLevel);
        }

        private void SetupGoalTriggerInGrid()
        {
            if (currentGridInstance == null)
                return;

            Transform goalTr = currentGridInstance.transform.Find("GameplayObjects/Goal");
            if (goalTr == null)
            {
                goalTr = currentGridInstance.transform.Find("Goal");
            }

            if (goalTr != null)
            {
                // Pastikan memiliki komponen LevelGoalTrigger
                var goalTrigger = goalTr.GetComponent<LevelGoalTrigger>();
                if (goalTrigger == null)
                {
                    goalTrigger = goalTr.gameObject.AddComponent<LevelGoalTrigger>();
                }
                goalTrigger.ResetTrigger();

                // Pastikan memiliki komponen ExitGateVisual yang terinisialisasi
                var exitGate = goalTr.GetComponent<ExitGateVisual>();
                if (exitGate == null)
                {
                    exitGate = goalTr.gameObject.AddComponent<ExitGateVisual>();
                }
                exitGate.Initialize(currentLevel);

                // Pastikan collider adalah trigger
                Collider2D col = goalTr.GetComponent<Collider2D>();
                if (col != null)
                {
                    col.isTrigger = true;
                    col.enabled = true;
                }
            }
        }

        private void SetupPlayerPositionInGrid()
        {
            FindPlayer();

            Vector3 targetSpawn = playerSpawnPosition;

            if (currentGridInstance != null)
            {
                Transform spawnTr = currentGridInstance.transform.Find("GameplayObjects/SpawnPoint");
                if (spawnTr == null)
                {
                    spawnTr = currentGridInstance.transform.Find("SpawnPoint");
                }

                if (spawnTr != null)
                {
                    targetSpawn = spawnTr.position;
                }
            }

            playerSpawnPosition = targetSpawn;

            if (playerObject != null)
            {
                playerObject.transform.position = targetSpawn;

                Rigidbody2D rb = playerObject.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
#if UNITY_6000_0_OR_NEWER
                    rb.linearVelocity = Vector2.zero;
#else
                    rb.velocity = Vector2.zero;
#endif
                    rb.angularVelocity = 0f;
                }
            }

            // Daftarkan posisi respawn ke PlayerLivesManager
            if (PlayerLivesManager.Instance != null)
            {
                PlayerLivesManager.Instance.SetRespawnPosition(targetSpawn);
            }
        }

        /// <summary>
        /// Dipanggil HANYA saat pintu Goal disentuh pemain dan SEMUA huruf kata target sudah lengkap.
        /// </summary>
        public void CompleteCurrentLevel()
        {
            if (isGameCompleted || isTransitioning)
                return;

            // Pengaman ganda: level tidak boleh selesai jika kata target belum lengkap
            if (WordCollectionManager.Instance != null && !WordCollectionManager.Instance.IsWordComplete)
            {
                Debug.LogWarning("[LevelProgressionManager] Level belum dapat diselesaikan! Semua huruf belum terkumpul.");
                return;
            }

            isTransitioning = true;

            Debug.Log($"[LevelProgressionManager] ✓ LEVEL {currentLevel} SELESAI!");

            // Buka level berikutnya pada sistem progress dan simpan
            UnlockLevel(Mathf.Min(currentLevel + 1, TotalLevels));

            onLevelCompleted?.Invoke(currentLevel);

            if (currentLevel < TotalLevels)
            {
                int nextLevel = currentLevel + 1;
                if (MazeSpell.UI.GameplayFeedbackController.Instance != null)
                {
                    MazeSpell.UI.GameplayFeedbackController.Instance.ExecuteLevelTransition(nextLevel, () =>
                    {
                        LoadLevel(nextLevel);
                    });
                }
                else
                {
                    LoadLevel(nextLevel);
                }
            }
            else
            {
                isGameCompleted = true;
                isTransitioning = false;

                Debug.Log("[LevelProgressionManager] ★★★ SEMUA 5 LEVEL MAZESPELL SELESAI! ★★★");

                onAllLevelsCompleted?.Invoke();
            }
        }

        public void RestartCurrentLevel()
        {
            Debug.Log($"[LevelProgressionManager] Me-restart Level {currentLevel}...");
            LoadLevel(currentLevel);
        }

        public void RestartGame()
        {
            Debug.Log("[LevelProgressionManager] Me-restart permainan dari Level 1...");
            isGameCompleted = false;
            LoadLevel(1);
        }

        private void FindPlayer()
        {
            if (playerObject != null)
                return;

            playerObject = GameObject.FindWithTag("Player");

            if (playerObject == null)
            {
#if UNITY_2023_1_OR_NEWER
                PlayerController pc = FindAnyObjectByType<PlayerController>();
#else
                PlayerController pc = FindObjectOfType<PlayerController>();
#endif
                if (pc != null)
                {
                    playerObject = pc.gameObject;
                }
            }
        }

        private void AdjustCamera(MazeLevelData data)
        {
            Camera cam = Camera.main;
            if (cam == null)
                return;

            cam.orthographic = true;

            var follow = cam.GetComponent<CameraFollow>();
            if (follow != null && follow.enabled)
            {
                cam.orthographicSize = 6.0f;
                if (data != null)
                {
                    follow.SetMazeBounds(data.TotalWidth, data.TotalHeight, Vector2.zero);
                }
                follow.SnapToTarget();
                return;
            }

            if (data != null)
            {
                Vector3 center = data.GetCenterOffset();
                cam.transform.position = new Vector3(center.x, center.y, -10f);
                cam.orthographicSize = data.CalculateOrthographicSize();
            }
            else
            {
                cam.transform.position = new Vector3(0f, 0f, -10f);
            }
        }

        public LevelConfig GetConfigForLevel(int levelNumber)
        {
            if (levelConfigs == null)
                return null;

            for (int i = 0; i < levelConfigs.Length; i++)
            {
                if (levelConfigs[i] != null && levelConfigs[i].levelNumber == levelNumber)
                {
                    return levelConfigs[i];
                }
            }

            return null;
        }

        private void OnGUI()
        {
            if (isGameCompleted)
            {
                float w = 430f;
                float h = 200f;
                float x = (Screen.width - w) / 2f;
                float y = (Screen.height - h) / 2f;

                GUI.Box(new Rect(x, y, w, h), "★ MAZESPELL SELESAI ★");

                GUI.Label(
                    new Rect(x + 20, y + 45, w - 40, 60),
                    "SELAMAT!\nKamu berhasil menyelesaikan\nsemua 5 level MazeSpell!"
                );

                if (GUI.Button(new Rect(x + 40, y + 125, w - 80, 40), "Main Lagi dari Level 1"))
                {
                    RestartGame();
                }

                return;
            }

            if (!showHUD)
                return;

            GUI.Box(new Rect(10, 10, 320, 45), $"MazeSpell   |   Level {currentLevel}/{TotalLevels}");
        }

        // 21 Kolom x 11 Baris untuk Labirin Level 1 (Sesuai Referensi Visual Lama)
        // 0 = Walkable Path, 1 = Non-walkable Wall dengan collider
        public static readonly int[,] Level1Matrix = MazeMatrixRegistry.Level1;

        private void EnsureMazeGridPopulated(GameObject gridInstance, int level)
        {
            if (gridInstance == null || level != 1) return;

            Transform wallsTr = gridInstance.transform.Find("Walls_Obstacles");
            Transform pathTr = gridInstance.transform.Find("Background_Path");

            if (wallsTr == null || pathTr == null) return;

            Tilemap wallsTilemap = wallsTr.GetComponent<Tilemap>();
            Tilemap pathTilemap = pathTr.GetComponent<Tilemap>();

            if (wallsTilemap == null || pathTilemap == null) return;

            if (wallsTilemap.GetUsedTilesCount() > 0) return;

            Sprite pathStoneSprite = null;
            Sprite groundSprite = null;

#if UNITY_EDITOR
            pathStoneSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Path_Stone_Blue.png");
            groundSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Ground.png");
#endif

            if (groundSprite == null)
            {
                Texture2D groundTex = new Texture2D(32, 32);
                Color groundCol = new Color(0.56f, 0.64f, 0.36f, 1f);
                for (int py = 0; py < 32; py++)
                    for (int px = 0; px < 32; px++)
                        groundTex.SetPixel(px, py, groundCol);
                groundTex.Apply();
                groundSprite = Sprite.Create(groundTex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
            }

            if (pathStoneSprite == null)
            {
                Texture2D stoneTex = new Texture2D(32, 32);
                Color stoneCol = new Color(0.44f, 0.54f, 0.63f, 1f);
                for (int py = 0; py < 32; py++)
                    for (int px = 0; px < 32; px++)
                        stoneTex.SetPixel(px, py, stoneCol);
                stoneTex.Apply();
                pathStoneSprite = Sprite.Create(stoneTex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
            }

            Tile pathStoneTile = ScriptableObject.CreateInstance<Tile>();
            pathStoneTile.sprite = pathStoneSprite;
            pathStoneTile.color = Color.white;
            pathStoneTile.colliderType = Tile.ColliderType.None;

            Tile grassTile = ScriptableObject.CreateInstance<Tile>();
            grassTile.sprite = groundSprite;
            grassTile.color = Color.white;
            grassTile.colliderType = Tile.ColliderType.None;

            Tile wallTile = ScriptableObject.CreateInstance<Tile>();
            wallTile.sprite = pathStoneSprite;
            wallTile.color = Color.white;
            wallTile.colliderType = Tile.ColliderType.Grid;

            int rows = Level1Matrix.GetLength(0);
            int cols = Level1Matrix.GetLength(1);

            for (int row = 0; row < rows; row++)
            {
                int y = (rows - 1) - row;
                for (int x = 0; x < cols; x++)
                {
                    Vector3Int pos = new Vector3Int(x, y, 0);

                    if (Level1Matrix[row, x] == 0)
                    {
                        pathTilemap.SetTile(pos, grassTile);
                        wallsTilemap.SetTile(pos, null);
                    }
                    else
                    {
                        pathTilemap.SetTile(pos, null);
                        wallsTilemap.SetTile(pos, wallTile);
                    }
                }
            }

            Debug.Log($"[LevelProgressionManager] Labirin Level 1 berhasil dipopulasikan ke Tilemap ({cols}x{rows} Walkable & Collider)!");
        }
    }
}