using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif
using MazeSpell.Audio;

namespace MazeSpell
{
    /// <summary>
    /// Komponen AI Enemy untuk game MazeSpell (Level 1 - 5):
    /// 1. Tepat 1 Enemy per level dengan kecepatan meningkat otomatis:
    ///    Level 1: 1.5, Level 2: 1.8, Level 3: 2.1, Level 4: 2.4, Level 5: 2.7.
    /// 2. Pathfinding BFS pada lorong batu/walkable agar tidak pernah menembus dinding.
    /// 3. Logika Jarak & Serangan:
    ///    - PLAYER JAUH: Enemy mengejar, belum menyerang, tidak ada damage.
    ///    - PLAYER DEKAT / ATTACK RANGE (0.8 - 1.0 unit): Enemy berhenti mengejar sementara,
    ///      menghadap Player, dan melakukan serangan dengan cooldown wajar (~1 detik).
    /// 4. Player menyerang balik dengan tombol SPACE ketika berada dalam jangkauan (1.2 - 1.5 unit).
    /// 5. Enemy yang dikalahkan memberikan reward +1 nyawa (maksimum 3, tepat 1 kali, tidak respawn).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class EnemyCharacter : MonoBehaviour
    {
        [Header("Stat Enemy (Kompatibilitas)")]
        [Tooltip("Stat Speed dasar (kompatibilitas spesifikasi 5)")]
        [SerializeField] private float speed = 5f;

        [Header("Kecepatan Berdasarkan Level (Otomatis)")]
        [Tooltip("Kecepatan gerak aktual saat mengejar Player (otomatis mengikuti level)")]
        [SerializeField] private float chaseSpeed = 1.5f;

        [Tooltip("Override level manual jika > 0, default 0 (otomatis mendeteksi level aktif)")]
        [SerializeField] private int overrideLevel = 0;

        [Header("Kesehatan Enemy")]
        [Tooltip("Kesehatan Enemy saat ini")]
        [SerializeField] private int currentHealth = 1;

        [Tooltip("Kesehatan maksimal Enemy")]
        [SerializeField] private int maxHealth = 1;

        [Header("Pengaturan Jarak & Serangan Enemy")]
        [Tooltip("Jarak serangan Enemy (sekitar 0.8 - 1.0 unit). Di luar jarak ini Enemy tidak memberi damage")]
        [SerializeField] private float attackRange = 0.9f;

        [Tooltip("Jumlah nyawa Player yang dikurangi saat serangan Enemy berhasil")]
        [SerializeField] private int damageAmount = 1;

        [Tooltip("Jeda cooldown antar-serangan (sekitar 1 detik)")]
        [SerializeField] private float damageCooldown = 1.0f;

        [Tooltip("Jarak deteksi Player untuk mulai mengejar")]
        [SerializeField] private float detectionRange = 8.5f;

        [Header("Pengaturan Serangan Balik Player")]
        [Tooltip("Jarak maksimal serangan Player menggunakan SPACE (1.2 - 1.5 unit)")]
        [SerializeField] private float playerAttackRange = 1.4f;

        public enum PatrolType
        {
            StandbyHorizontal,
            CircuitRoam,
            DirectChaseAlways
        }

        [Header("Patrol & AI Behavior")]
        [Tooltip("Mode patroli musuh (Standby horizontal vs Muter/Roaming circuit)")]
        [SerializeField] private PatrolType patrolMode = PatrolType.CircuitRoam;
        [SerializeField] private Vector3 anchorPosition;
        [SerializeField] private float horizontalPatrolRange = 1.6f;
        [SerializeField] private List<Vector3> patrolWaypoints = new List<Vector3>();
        private int currentWaypointIndex = 0;
        private int horizontalDir = 1;
        private MazeSpell.Gameplay.EnemyVisionController visionController;

        [Header("Stealth & Vision Chase Settings")]
        [Tooltip("Waktu grace period mengejar posisi terakhir Player saat keluar dari pandangan (0.8 - 1.5 detik)")]
        [SerializeField] private float visionGracePeriod = 1.2f;
        private float gracePeriodTimer = 0f;
        private Vector3 lastKnownTargetPosition;
        private bool hasLastKnownPosition = false;
        private Vector2 currentFacingDirection = Vector2.right;

        public Vector2 CurrentFacingDirection => currentFacingDirection;
        public void SetFacingDirection(Vector2 dir) { if (dir.sqrMagnitude > 0.001f) currentFacingDirection = dir.normalized; }
        public float VisionGracePeriod { get => visionGracePeriod; set => visionGracePeriod = value; }
        public float GracePeriodTimer => gracePeriodTimer;
        public bool HasLastKnownPosition => hasLastKnownPosition;
        public Vector3 LastKnownTargetPosition => lastKnownTargetPosition;

        public void SetTarget(Transform target)
        {
            playerTransform = target;
            if (target != null)
            {
                lastKnownTargetPosition = target.position;
                hasLastKnownPosition = true;
            }
        }

        public void SetTarget(GameObject target)
        {
            SetTarget(target != null ? target.transform : null);
        }

        public void ClearTargetImmediate()
        {
            gracePeriodTimer = 0f;
            hasLastKnownPosition = false;
            isChasing = false;
            isPreparingAttack = false;
        }

        [Header("Visual & Animasi")]
        [Tooltip("Animasi melayang / breathing saat idle")]
        [SerializeField] private bool enableIdleAnimation = true;
        [SerializeField] private float idleBobSpeed = 2.5f;
        [SerializeField] private float idleBobAmplitude = 0.06f;

        // Komponen & Referensi
        private Collider2D enemyCollider;
        private SpriteRenderer spriteRenderer;
        private Transform playerTransform;
        private Tilemap wallsTilemap;
        private BoundsInt mazeBounds;

        // Split System
        [Header("Enemy Split System")]
        [Tooltip("Apakah Enemy ini diizinkan untuk membelah menjadi Mini Enemy saat HP habis")]
        [SerializeField] private bool canSplit = true;
        [SerializeField] private bool hasSplit = false;
        [SerializeField] private int splitGeneration = 0; // 0 = main enemy, 1 = mini enemy
        [SerializeField] private bool isMiniEnemy = false;
        private EnemySplitController splitController;

        // Status Runtime
        private Vector3 initialBasePos;
        private float lastDamageTime = -999f;
        private float lastAttackAttemptTime = -999f;
        private float lastRepathTime = 0f;
        private float staggerTimer = 0f;
        private EnemyHealthIndicator healthIndicator;
        private float RepathInterval => GetRepathIntervalForLevel(currentActiveLevel);

        private List<Vector3Int> currentPath = new List<Vector3Int>();
        private int currentPathIndex = 0;
        private bool isChasing = false;
        private bool isPreparingAttack = false;
        private bool isDefeated = false;
        private bool rewardGiven = false;
        private bool isFlashing = false;
        private EnemyVisualAnimator visualAnimator;
        private int currentActiveLevel = 1;

        // Flag kontrol thread/instance saat instansiasi mini enemy
        public static bool IsSpawningMiniEnemy = false;

        // Properti Publik
        public static System.Action<EnemyCharacter> OnEnemyDefeated;
        public static System.Action<EnemyCharacter, int, int> OnEnemyHealthChanged;
        private static EnemyCharacter _activeEnemy;
        public static EnemyCharacter ActiveEnemy
        {
            get
            {
                // Prioritaskan Enemy terdekat yang masih aktif & belum kalah
                EnemyCharacter closest = GetClosestActiveEnemyToPlayer();
                if (closest != null)
                {
                    _activeEnemy = closest;
                    return _activeEnemy;
                }

                if (_activeEnemy != null && _activeEnemy.gameObject.activeInHierarchy && !_activeEnemy.isDefeated)
                {
                    return _activeEnemy;
                }

                return null;
            }
            set => _activeEnemy = value;
        }

        /// <summary>
        /// Menemukan Enemy yang aktif dan paling dekat dengan posisi Player (berguna saat terdapat Mini Enemy).
        /// </summary>
        public static EnemyCharacter GetClosestActiveEnemyToPlayer()
        {
#if UNITY_2023_1_OR_NEWER
            var all = FindObjectsByType<EnemyCharacter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
            var all = FindObjectsOfType<EnemyCharacter>();
#endif
            if (all == null || all.Length == 0) return null;

            GameObject player = GameObject.FindWithTag("Player");
            Vector3 pPos = player != null ? player.transform.position : Vector3.zero;

            EnemyCharacter closest = null;
            float minDist = float.MaxValue;

            for (int i = 0; i < all.Length; i++)
            {
                var e = all[i];
                if (e != null && !e.isDefeated && e.gameObject.activeInHierarchy)
                {
                    if (player == null) return e;

                    float dist = Vector2.Distance(e.transform.position, pPos);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closest = e;
                    }
                }
            }

            return closest;
        }

        public bool CanSplit
        {
            get => canSplit && !IsBoss && splitGeneration == 0 && !hasSplit;
            set => canSplit = value;
        }
        public bool HasSplit { get => hasSplit; set => hasSplit = value; }
        public int SplitGeneration { get => splitGeneration; set => splitGeneration = value; }
        public bool IsMiniEnemy { get => isMiniEnemy; set => isMiniEnemy = value; }
        public EnemySplitController SplitController => splitController;

        public bool IsBoss { get; set; } = false;
        public EnemyVisualAnimator VisualAnimator => visualAnimator;
        public float Speed => speed;
        public float ChaseSpeed => chaseSpeed;
        public float MoveSpeed => chaseSpeed;
        public int Health => currentHealth;
        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public int DamageAmount => damageAmount;
        public float DamageCooldown => damageCooldown;
        public float DetectionRange => detectionRange;
        public float AttackRange => attackRange;
        public float PlayerAttackRange => playerAttackRange;
        public bool IsDefeated => isDefeated;
        public bool IsChasing => isChasing;
        public bool IsPreparingAttack => isPreparingAttack;
        public int CurrentLevelNumber => currentActiveLevel;
        public Transform PlayerTransform => playerTransform;

        /// <summary>
        /// Menghitung kecepatan Enemy sesuai nomor level:
        /// Level 1 = 1.5, Level 2 = 1.8, Level 3 = 2.1, Level 4 = 2.4, Level 5 = 2.7.
        /// Menjamin Level 1 < Level 2 < Level 3 < Level 4 < Level 5.
        /// </summary>
        public static float GetSpeedForLevel(int level)
        {
            switch (level)
            {
                case 1: return 1.5f;
                case 2: return 1.8f;
                case 3: return 2.1f;
                case 4: return 2.4f;
                case 5: return 2.7f;
                default:
                    return Mathf.Clamp(1.5f + (Mathf.Max(1, level) - 1) * 0.3f, 1.5f, 3.5f);
            }
        }

        /// <summary>
        /// Menghitung HP maksimal Enemy sesuai nomor level:
        /// Level 1 = 1 HP (1 hit)
        /// Level 2 = 1 HP (1 hit)
        /// Level 3 = 1 HP (1 hit)
        /// Level 4 = 2 HP (2 hits)
        /// Level 5 = 3 HP (3 hits - FINAL CHALLENGE)
        /// </summary>
        public static int GetMaxHealthForLevel(int level)
        {
            switch (level)
            {
                case 1: return 1;
                case 2: return 1;
                case 3: return 1;
                case 4: return 2;
                case 5: return 3;
                default: return 1;
            }
        }

        /// <summary>
        /// Menghitung cooldown serangan Enemy sesuai nomor level:
        /// Level 1-3 = 1.0s (wajar)
        /// Level 4 = 0.85s (lebih cepat)
        /// Level 5 = 0.75s (sangat agresif, ~0.7 - 0.8s)
        /// </summary>
        public static float GetAttackCooldownForLevel(int level)
        {
            switch (level)
            {
                case 1: return 1.0f;
                case 2: return 1.0f;
                case 3: return 1.0f;
                case 4: return 0.85f;
                case 5: return 0.75f;
                default: return 1.0f;
            }
        }

        /// <summary>
        /// Menghitung interval kalkulasi ulang jalur BFS ke Player:
        /// Level 1-2 = 0.25s, Level 3 = 0.20s, Level 4 = 0.16s, Level 5 = 0.12s.
        /// Semakin tinggi level, Enemy semakin responsif dan konsisten mengejar Player.
        /// </summary>
        public static float GetRepathIntervalForLevel(int level)
        {
            switch (level)
            {
                case 1: return 0.25f;
                case 2: return 0.22f;
                case 3: return 0.20f;
                case 4: return 0.16f;
                case 5: return 0.12f;
                default: return 0.20f;
            }
        }

        private void Awake()
        {
            if (IsSpawningMiniEnemy)
            {
                isMiniEnemy = true;
                splitGeneration = 1;
                canSplit = false;
                hasSplit = true;
            }

            initialBasePos = transform.localPosition;
            enemyCollider = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();

            if (enemyCollider != null)
            {
                enemyCollider.isTrigger = true;
            }

            EnsureVisualHierarchy();
            EnsureHealthIndicator();

            if (splitController == null)
            {
                splitController = GetComponent<EnemySplitController>();
                if (splitController == null)
                {
                    splitController = gameObject.AddComponent<EnemySplitController>();
                    splitController.Initialize(this, CanSplit, isMiniEnemy, splitGeneration);
                }
            }

            // ATURAN: Tepat 1 Enemy aktif per level (cegah duplikasi)
            EnforceSingleEnemyInLevel();

            // Inisialisasi stat (speed, HP, cooldown) sesuai level jika bukan mini enemy
            if (!isMiniEnemy)
            {
                UpdateStatsForCurrentLevel();
            }
        }

        private void OnEnable()
        {
            _activeEnemy = this;
            if (LevelProgressionManager.HasInstance && LevelProgressionManager.Instance.onLevelStarted != null)
            {
                LevelProgressionManager.Instance.onLevelStarted.AddListener(OnLevelProgressionStarted);
            }
        }

        private void OnDisable()
        {
            if (_activeEnemy == this) _activeEnemy = null;
            if (LevelProgressionManager.HasInstance && LevelProgressionManager.Instance.onLevelStarted != null)
            {
                LevelProgressionManager.Instance.onLevelStarted.RemoveListener(OnLevelProgressionStarted);
            }
        }

        private void OnLevelProgressionStarted(int level)
        {
            ClearTilemapCache();
            UpdateStatsForCurrentLevel();
            FindPlayerReference();
        }

        private void Start()
        {
            if (anchorPosition == Vector3.zero)
            {
                anchorPosition = transform.position;
            }
            visionController = GetComponent<MazeSpell.Gameplay.EnemyVisionController>();

            EnsureVisualHierarchy();
            EnsureHealthIndicator();
            FindPlayerReference();
            CacheMazeTilemaps();
            UpdateStatsForCurrentLevel();
        }

        /// <summary>
        /// Mengonfigurasi mode patroli musuh (Standby horizontal vs Muter/Roaming circuit).
        /// </summary>
        public void SetPatrolConfig(PatrolType mode, Vector3 anchor, List<Vector3> waypoints = null, float range = 1.6f)
        {
            patrolMode = mode;
            anchorPosition = anchor;
            horizontalPatrolRange = range;
            if (waypoints != null)
            {
                patrolWaypoints = new List<Vector3>(waypoints);
            }
            currentWaypointIndex = 0;
        }

        private string lastLoggedBossState = "";
        public void LogBossStateChange(string newState)
        {
            if (IsBoss && newState != lastLoggedBossState)
            {
                lastLoggedBossState = newState;
                Debug.Log($"[BOSS STATE]\n{newState}");
            }
        }

        public string GetCurrentStateName()
        {
            if (isDefeated) return "DEFEATED";
            if (staggerTimer > 0f) return "HURT";
            if (visualAnimator != null)
            {
                switch (visualAnimator.CurrentState)
                {
                    case EnemyVisualAnimator.AnimState.AttackPrepare: return "ATTACK PREPARE";
                    case EnemyVisualAnimator.AnimState.AttackSwing: return "ATTACK";
                    case EnemyVisualAnimator.AnimState.AttackRecovery: return "RECOVERY";
                    case EnemyVisualAnimator.AnimState.Hurt: return "HURT";
                    case EnemyVisualAnimator.AnimState.Defeated: return "DEFEATED";
                }
            }
            if (isPreparingAttack) return "ATTACK PREPARE";
            if (isChasing) return "CHASE";
            return "IDLE";
        }

        public void ClearTilemapCache()
        {
            wallsTilemap = null;
            currentPath = null;
            currentPathIndex = 0;
            lastRepathTime = 0f;
            CacheMazeTilemaps();
        }

        private float lastEnemyDebugLogTime = -999f;
        private void LogEnemyDebug()
        {
            if (Time.time - lastEnemyDebugLogTime < 1.0f) return;
            lastEnemyDebugLogTime = Time.time;
            float dist = playerTransform != null ? Vector2.Distance(transform.position, playerTransform.position) : -1f;
            Debug.Log($"[ENEMY DEBUG]\nEnemy: {gameObject.name}\nPlayer: {(playerTransform != null ? playerTransform.name : "null")}\nPlayer position: {(playerTransform != null ? playerTransform.position.ToString("F2") : "null")}\nEnemy position: {transform.position.ToString("F2")}\nDistance: {dist:F2}\nTarget assigned: {playerTransform != null}\nIsBoss: {IsBoss}\nCurrentState: {GetCurrentStateName()}\nCurrentHealth: {currentHealth}");
        }

        private float lastEnemyMoveLogTime = -999f;
        private void LogEnemyMove(Vector3 targetWorldPos, float dist)
        {
            if (Time.time - lastEnemyMoveLogTime < 0.5f) return;
            lastEnemyMoveLogTime = Time.time;
            Debug.Log($"[ENEMY MOVE]\nPosition: {transform.position.ToString("F2")}\nTarget: {targetWorldPos.ToString("F2")}\nDistance: {dist:F2}\nState: {GetCurrentStateName()}\nSpeed: {chaseSpeed:F1}\nPathIndex: {currentPathIndex}");
        }

        public void LogEnemyAttack(string state, float dist, float cooldownRemaining, bool attackStarted, bool damageWindow, bool playerHit)
        {
            Debug.Log($"[ENEMY ATTACK]\nState: {state}\nDistance: {dist:F2}\nCooldown: {cooldownRemaining:F2}\nAttack Started: {attackStarted}\nDamage Window: {damageWindow}\nPlayer Hit: {playerHit}");
        }

        private void Update()
        {
            if (isDefeated) return;

            // 0. Cek masa stagger (reaksi terkena serangan SPACE): Enemy berhenti sebentar
            if (staggerTimer > 0f)
            {
                staggerTimer -= Time.deltaTime;
                if (visualAnimator != null) visualAnimator.SetMoving(false, Vector2.zero);
                isChasing = false;
                isPreparingAttack = false;
                LogBossStateChange("STAGGER");
                return;
            }

            FindPlayerReference();
            LogEnemyDebug();

            // Sinkronisasi otomatis stat jika level berubah saat runtime
            int detectedLevel = GetCurrentLevelNumber();
            if (detectedLevel != currentActiveLevel)
            {
                UpdateStatsForCurrentLevel();
            }

            // 1. Cek input serangan Player dengan tombol SPACE
            CheckPlayerSpaceCombatInput();

            // 2. Jika Player tidak ada atau Game Over, hentikan aksi
            if (playerTransform == null || (PlayerLivesManager.Instance != null && PlayerLivesManager.Instance.IsGameOver))
            {
                isChasing = false;
                isPreparingAttack = false;
                if (visualAnimator != null) visualAnimator.SetMoving(false, Vector2.zero);
                LogBossStateChange("IDLE");
                return;
            }

            // Jika sedang dalam rangkaian attack sequence (Prepare / Swing / Recovery),
            // Enemy fokus menyelesaikan ayunan serangan dan TIDAK boleh memotong serangan dengan gerak
            if (visualAnimator != null && visualAnimator.IsBusyWithAttack)
            {
                isChasing = false;
                isPreparingAttack = visualAnimator.CurrentState == EnemyVisualAnimator.AnimState.AttackPrepare;
                return;
            }

            // Cek apakah ada Vision Controller dan apakah Player terlihat
            if (visionController == null)
            {
                visionController = GetComponent<MazeSpell.Gameplay.EnemyVisionController>();
                if (visionController == null)
                {
                    visionController = gameObject.AddComponent<MazeSpell.Gameplay.EnemyVisionController>();
                }
            }

            bool canDirectChase = (patrolMode == PatrolType.DirectChaseAlways) || IsBoss;
            bool playerCurrentlyVisible = (visionController != null) && visionController.IsPlayerSpotted;

            if (playerCurrentlyVisible)
            {
                // Player terlihat langsung di vision cone!
                gracePeriodTimer = visionGracePeriod;
                if (playerTransform != null)
                {
                    lastKnownTargetPosition = playerTransform.position;
                    hasLastKnownPosition = true;
                }
            }
            else
            {
                // Player di luar cone atau di balik tembok
                if (gracePeriodTimer > 0f)
                {
                    gracePeriodTimer -= Time.deltaTime;
                    if (gracePeriodTimer <= 0f)
                    {
                        // Grace period habis -> Enemy benar-benar kehilangan target!
                        hasLastKnownPosition = false;
                        if (visionController != null)
                        {
                            visionController.NotifyTargetLost();
                        }
                    }
                }
            }

            bool shouldChase = canDirectChase || playerCurrentlyVisible || (gracePeriodTimer > 0f && hasLastKnownPosition);

            // =================================================================
            // MODE PATROLI / STANDBY (Jika Player belum terdeteksi penglihatan)
            // =================================================================
            if (!shouldChase)
            {
                isChasing = false;
                isPreparingAttack = false;
                ExecutePatrolBehavior();
                return;
            }

            float distToPlayer = playerTransform != null 
                ? Vector2.Distance(transform.position, playerTransform.position)
                : (hasLastKnownPosition ? Vector2.Distance(transform.position, lastKnownTargetPosition) : 999f);

            // =================================================================
            // STATE MACHINE BEHAVIOR:
            // ATTACK: dist <= attackRange (0.9 unit) DAN terlihat -> Enemy berhenti & menyerang
            // CHASE: dist > attackRange (0.9 unit) -> Enemy bergerak mengejar Player / Posisi Terakhir
            // =================================================================

            // STATE ATTACK: PLAYER SANGAT DEKAT (<= attackRange, ~0.9 unit) DAN TERLIHAT
            if (playerCurrentlyVisible && distToPlayer <= attackRange)
            {
                // Berhenti mengejar saat masuk jangkauan serang
                isChasing = false;
                isPreparingAttack = true;
                LogBossStateChange("ATTACK PREPARE");

                if (visualAnimator != null)
                {
                    visualAnimator.SetMoving(false, Vector2.zero);
                    visualAnimator.FaceDirection(playerTransform.position.x - transform.position.x);
                }
                FaceTowardsPlayer();

                // Lakukan serangan jika cooldown selesai
                AttemptAttackPlayer();
            }
            // STATE CHASE: PLAYER BERADA DI LUAR ATTACK RANGE (> attackRange) ATAU DALAM GRACE PERIOD
            else
            {
                isChasing = true;
                isPreparingAttack = false;
                LogBossStateChange("CHASE");

                // Bergerak mengejar Player atau posisi terakhir mengikuti jalur BFS lorong aman
                UpdateAndFollowPathToTarget(dt: -1f, usePlayerTransform: playerCurrentlyVisible);
            }
        }

        /// <summary>
        /// Menjalankan pergerakan patroli saat Player belum terdeteksi penglihatan musuh.
        /// </summary>
        private void ExecutePatrolBehavior()
        {
            float dt = Time.deltaTime > 0f ? Time.deltaTime : 0.02f;

            if (patrolMode == PatrolType.StandbyHorizontal)
            {
                // Bergerak horizontal bolak-balik pendek di sekitar anchor
                float targetX = anchorPosition.x + (horizontalDir * horizontalPatrolRange);
                Vector3 targetPos = new Vector3(targetX, anchorPosition.y, transform.position.z);
                
                // Cek apakah petak di depan masih walkable agar tidak menabrak dinding
                Vector3Int checkCell = wallsTilemap != null
                    ? wallsTilemap.WorldToCell(transform.position + new Vector3(horizontalDir * 0.55f, 0f, 0f))
                    : new Vector3Int(Mathf.FloorToInt(transform.position.x + horizontalDir * 0.55f), Mathf.FloorToInt(transform.position.y), 0);

                if (!IsCellWalkable(checkCell))
                {
                    horizontalDir = -horizontalDir;
                    targetX = anchorPosition.x + (horizontalDir * horizontalPatrolRange);
                    targetPos = new Vector3(targetX, anchorPosition.y, transform.position.z);
                }

                transform.position = Vector3.MoveTowards(transform.position, targetPos, chaseSpeed * 0.45f * dt);
                FaceTowards(targetPos);

                if (Mathf.Abs(transform.position.x - targetX) < 0.1f)
                {
                    horizontalDir = -horizontalDir;
                }

                if (visualAnimator != null)
                {
                    visualAnimator.SetMoving(true, new Vector2(horizontalDir, 0f));
                }
            }
            else if (patrolMode == PatrolType.CircuitRoam)
            {
                // Bergerak menyusuri serangkaian waypoint keliling ruangan / lorong
                if (patrolWaypoints != null && patrolWaypoints.Count > 0)
                {
                    Vector3 targetWaypoint = patrolWaypoints[currentWaypointIndex];
                    Vector2 moveDelta = (Vector2)targetWaypoint - (Vector2)transform.position;
                    float dist = moveDelta.magnitude;

                    transform.position = Vector3.MoveTowards(transform.position, targetWaypoint, chaseSpeed * 0.65f * dt);
                    FaceTowards(targetWaypoint);

                    if (visualAnimator != null)
                    {
                        visualAnimator.SetMoving(true, moveDelta.normalized);
                    }

                    if (dist < 0.25f)
                    {
                        currentWaypointIndex = (currentWaypointIndex + 1) % patrolWaypoints.Count;
                    }
                }
                else
                {
                    // Fallback jika waypoints belum ditentukan: lakukan standby horizontal aman
                    float targetX = anchorPosition.x + (horizontalDir * horizontalPatrolRange);
                    Vector3 targetPos = new Vector3(targetX, anchorPosition.y, transform.position.z);
                    Vector3Int checkCell = wallsTilemap != null
                        ? wallsTilemap.WorldToCell(transform.position + new Vector3(horizontalDir * 0.55f, 0f, 0f))
                        : new Vector3Int(Mathf.FloorToInt(transform.position.x + horizontalDir * 0.55f), Mathf.FloorToInt(transform.position.y), 0);

                    if (!IsCellWalkable(checkCell))
                    {
                        horizontalDir = -horizontalDir;
                        targetX = anchorPosition.x + (horizontalDir * horizontalPatrolRange);
                        targetPos = new Vector3(targetX, anchorPosition.y, transform.position.z);
                    }

                    transform.position = Vector3.MoveTowards(transform.position, targetPos, chaseSpeed * 0.40f * dt);
                    FaceTowards(targetPos);

                    if (Mathf.Abs(transform.position.x - targetX) < 0.1f)
                    {
                        horizontalDir = -horizontalDir;
                    }

                    if (visualAnimator != null)
                    {
                        visualAnimator.SetMoving(true, new Vector2(horizontalDir, 0f));
                    }
                }
            }
        }

        /// <summary>
        /// Mengatur level Enemy secara eksplisit dan memperbarui stat (speed, HP, cooldown).
        /// </summary>
        public void SetLevel(int level)
        {
            overrideLevel = Mathf.Clamp(level, 1, 5);
            UpdateStatsForCurrentLevel();
        }

        /// <summary>
        /// Mendeteksi nomor level aktif secara otomatis melalui LevelProgressionManager atau hierarki prefab.
        /// </summary>
        public int GetCurrentLevelNumber()
        {
            if (overrideLevel > 0)
                return overrideLevel;

            if (LevelProgressionManager.HasInstance && LevelProgressionManager.Instance.CurrentLevel > 0)
            {
                return LevelProgressionManager.Instance.CurrentLevel;
            }

            // Fallback: deteksi dari nama GameObject induk (misal: Level2_MazeGrid)
            Transform curr = transform;
            while (curr != null)
            {
                string n = curr.name;
                if (n.StartsWith("Level") && n.Length >= 6 && char.IsDigit(n[5]))
                {
                    return (int)char.GetNumericValue(n[5]);
                }
                curr = curr.parent;
            }

            return 1;
        }

        /// <summary>
        /// Memperbarui seluruh stat Enemy (Speed, HP, Attack Cooldown) otomatis sesuai nomor level aktif.
        /// </summary>
        public void UpdateStatsForCurrentLevel()
        {
            currentActiveLevel = GetCurrentLevelNumber();
            chaseSpeed = GetSpeedForLevel(currentActiveLevel);
            maxHealth = GetMaxHealthForLevel(currentActiveLevel);
            currentHealth = maxHealth;
            damageCooldown = GetAttackCooldownForLevel(currentActiveLevel);
            attackRange = 0.9f;
            playerAttackRange = 1.4f;

            Debug.Log($"[EnemyCharacter] Stats Enemy Level {currentActiveLevel}: Speed={chaseSpeed}, HP={maxHealth}, Cooldown={damageCooldown}s, AttackRange={attackRange}");
            UpdateHealthIndicator();
            OnEnemyHealthChanged?.Invoke(this, currentHealth, maxHealth);
        }

        /// <summary>
        /// Mengatur statistik pertempuran khusus mode Boss / Guardian.
        /// </summary>
        public void SetBossStats(float newChaseSpeed, float newAttackCooldown, float newAttackRange = 0.9f)
        {
            chaseSpeed = newChaseSpeed;
            damageCooldown = newAttackCooldown;
            attackRange = newAttackRange;
        }

        /// <summary>
        /// Mengatur HP saat ini secara presisi dan memperbarui visual indikator & event.
        /// </summary>
        public void SetCurrentHealth(int hp)
        {
            currentHealth = Mathf.Clamp(hp, 0, maxHealth);
            UpdateHealthIndicator();
            OnEnemyHealthChanged?.Invoke(this, currentHealth, maxHealth);
        }

        /// <summary>
        /// Memicu masa stagger (berhenti sesaat akibat benturan/tebasan).
        /// </summary>
        public void SetStagger(float duration)
        {
            staggerTimer = duration;
            lastAttackAttemptTime = Time.time + duration + 0.2f;
            lastDamageTime = Time.time + duration + 0.2f;
        }

        /// <summary>
        /// Kompatibilitas mundur: memanggil UpdateStatsForCurrentLevel.
        /// </summary>
        public void UpdateSpeedForCurrentLevel()
        {
            UpdateStatsForCurrentLevel();
        }

        /// <summary>
        /// Menyetel level Enemy secara eksplisit untuk keperluan testing.
        /// </summary>
        public void SetLevelForTesting(int level)
        {
            currentActiveLevel = level;
            chaseSpeed = GetSpeedForLevel(currentActiveLevel);
            maxHealth = GetMaxHealthForLevel(currentActiveLevel);
            currentHealth = maxHealth;
            damageCooldown = GetAttackCooldownForLevel(currentActiveLevel);
            isDefeated = false;
            OnEnemyHealthChanged?.Invoke(this, currentHealth, maxHealth);
        }

        /// <summary>
        /// Mengatur properti split controller untuk enemy ini.
        /// </summary>
        public void SetSplitProperties(bool canSplitVal, bool isMiniVal, int generation)
        {
            canSplit = canSplitVal;
            isMiniEnemy = isMiniVal;
            splitGeneration = generation;
            if (isMiniVal || generation > 0)
            {
                hasSplit = true;
                canSplit = false;
            }
        }

        /// <summary>
        /// Mengatur statistik pertempuran khusus untuk Mini Enemy hasil pembelahan:
        /// HP 1, ukuran ~50%, movement +20% lebih cepat, cooldown 1.35s.
        /// </summary>
        public void SetMiniEnemyStats(float speedMultiplier = 1.20f, float newCooldown = 1.35f)
        {
            isMiniEnemy = true;
            splitGeneration = 1;
            canSplit = false;
            hasSplit = true;
            chaseSpeed = GetSpeedForLevel(currentActiveLevel) * speedMultiplier;
            damageCooldown = newCooldown;
            maxHealth = 1;
            currentHealth = 1;
            attackRange = 0.85f;
            playerAttackRange = 1.35f;
            UpdateHealthIndicator();
            OnEnemyHealthChanged?.Invoke(this, currentHealth, maxHealth);
        }

        /// <summary>
        /// Memicu efek peringatan dramatis saat Boss HP mencapai 1:
        /// Aura Flame membesar, teks peringatan, boss sedikit mengecil/mengencang, tetap mengejar Player.
        /// </summary>
        public void TriggerBossEnrageWarning()
        {
            if (!IsBoss) return;
            StartCoroutine(BossEnrageWarningRoutine());
        }

        private IEnumerator BossEnrageWarningRoutine()
        {
            Debug.Log("[BOSS ENRAGE WARNING] HP reached 1! Flame aura intensifying!");
            MazeSpell.Gameplay.FloatingTextHelper.ShowFloatingText(transform.position + Vector3.up * 1.5f, "⚠ DANGER: GUARDIAN ENRAGED! ⚠", new Color(1f, 0.2f, 0.1f, 1f), 34);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.SFXType.EnemyHurt, 1.25f);
            }

            MazeSpell.Gameplay.ElementalParticleSpawner.SpawnBossAura(transform, 3);
            MazeSpell.Gameplay.ElementalParticleSpawner.SpawnElementEffect(5, transform.position);

            Vector3 startSc = transform.localScale;
            Vector3 targetSc = Vector3.one * 1.18f;
            float dur = 0.25f;
            float el = 0f;
            while (el < dur)
            {
                el += Time.deltaTime;
                float t = Mathf.Clamp01(el / dur);
                transform.localScale = Vector3.Lerp(startSc, targetSc, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            transform.localScale = targetSc;
        }

        /// <summary>
        /// Mengizinkan musuh berjumlah sesuai konfigurasi level tanpa penghapusan paksa.
        /// </summary>
        private void EnforceSingleEnemyInLevel()
        {
            // Diperbarui: Mendukung multi-enemy per level sesuai spesifikasi level 1-5
        }

        private void FindPlayerReference()
        {
            if (playerTransform != null && playerTransform.gameObject != null && playerTransform.gameObject.activeInHierarchy)
                return;

            playerTransform = null;
            GameObject p = GameObject.FindWithTag("Player");
            if (p == null)
            {
#if UNITY_2023_1_OR_NEWER
                PlayerController pc = FindAnyObjectByType<PlayerController>();
#else
                PlayerController pc = FindObjectOfType<PlayerController>();
#endif
                if (pc != null && pc.gameObject.activeInHierarchy) p = pc.gameObject;
            }

            if (p != null)
            {
                playerTransform = p.transform;
            }
        }

        private void CacheMazeTilemaps()
        {
            if (wallsTilemap != null && wallsTilemap.gameObject != null && wallsTilemap.gameObject.activeInHierarchy)
                return;

            wallsTilemap = null;

            Grid grid = GetComponentInParent<Grid>();
            if (grid != null)
            {
                Transform wallsTr = grid.transform.Find("Walls_Obstacles");
                if (wallsTr != null)
                {
                    wallsTilemap = wallsTr.GetComponent<Tilemap>();
                }
            }

            if (wallsTilemap == null)
            {
                GameObject wallsObj = GameObject.Find("Walls_Obstacles");
                if (wallsObj != null)
                {
                    wallsTilemap = wallsObj.GetComponent<Tilemap>();
                }
            }

            if (wallsTilemap == null)
            {
#if UNITY_2023_1_OR_NEWER
                Tilemap[] allMaps = FindObjectsByType<Tilemap>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
                Tilemap[] allMaps = FindObjectsOfType<Tilemap>();
#endif
                foreach (var tm in allMaps)
                {
                    if (tm.name.Contains("Walls") || tm.name.Contains("Obstacles"))
                    {
                        wallsTilemap = tm;
                        break;
                    }
                }
            }

            if (wallsTilemap != null)
            {
                mazeBounds = wallsTilemap.cellBounds;
            }
        }

        /// <summary>
        /// Mengecek apakah suatu koordinat sel tile adalah area walkable di dalam labirin.
        /// </summary>
        public bool IsCellWalkable(Vector3Int cell)
        {
            int maxCols = 45;
            int maxRows = 23;

            if (LevelProgressionManager.HasInstance && LevelProgressionManager.Instance.CurrentData != null)
            {
                maxCols = LevelProgressionManager.Instance.CurrentData.columns;
                maxRows = LevelProgressionManager.Instance.CurrentData.rows;
            }
            else
            {
                int lvl = GetCurrentLevelNumber();
                switch (lvl)
                {
                    case 1: maxCols = 29; maxRows = 15; break;
                    case 2: maxCols = 33; maxRows = 17; break;
                    case 3: maxCols = 37; maxRows = 19; break;
                    case 4: maxCols = 41; maxRows = 21; break;
                    case 5: maxCols = 45; maxRows = 23; break;
                    default: maxCols = 45; maxRows = 23; break;
                }
            }

            CacheMazeTilemaps();
            if (wallsTilemap != null)
            {
                maxCols = Mathf.Max(maxCols, wallsTilemap.cellBounds.xMax);
                maxRows = Mathf.Max(maxRows, wallsTilemap.cellBounds.yMax);
            }

            int minX = wallsTilemap != null ? Mathf.Min(0, wallsTilemap.cellBounds.xMin) : 0;
            int minY = wallsTilemap != null ? Mathf.Min(0, wallsTilemap.cellBounds.yMin) : 0;

            if (cell.x < minX || cell.x >= maxCols || cell.y < minY || cell.y >= maxRows)
            {
                return false;
            }

            if (wallsTilemap != null)
            {
                return !wallsTilemap.HasTile(cell);
            }

            Vector2 worldPos = new Vector2(cell.x + 0.5f, cell.y + 0.5f);
            Collider2D hit = Physics2D.OverlapCircle(worldPos, 0.35f);
            if (hit != null && (hit.name.Contains("Wall") || hit.GetComponent<TilemapCollider2D>() != null))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Memperbarui jalur BFS ke Player dan menggerakkan Enemy mengikuti lorong aman dengan kecepatan level.
        /// </summary>
        public void UpdateAndFollowPathToPlayer(float dt = -1f)
        {
            UpdateAndFollowPathToTarget(dt, true);
        }

        /// <summary>
        /// Menggerakkan Enemy mengikuti jalur BFS menuju target tertentu (Player atau Posisi Terakhir saat grace period).
        /// </summary>
        public void UpdateAndFollowPathToTarget(float dt = -1f, bool usePlayerTransform = true)
        {
            if (dt < 0f) dt = Time.deltaTime > 0f ? Time.deltaTime : 0.02f;

            Vector3 targetWorldPos = (usePlayerTransform && playerTransform != null)
                ? playerTransform.position
                : (hasLastKnownPosition ? lastKnownTargetPosition : (playerTransform != null ? playerTransform.position : transform.position));

            CacheMazeTilemaps();

            Vector3Int startCell = wallsTilemap != null 
                ? wallsTilemap.WorldToCell(transform.position)
                : new Vector3Int(Mathf.FloorToInt(transform.position.x), Mathf.FloorToInt(transform.position.y), 0);

            Vector3Int targetCell = wallsTilemap != null
                ? wallsTilemap.WorldToCell(targetWorldPos)
                : new Vector3Int(Mathf.FloorToInt(targetWorldPos.x), Mathf.FloorToInt(targetWorldPos.y), 0);

            if (!IsCellWalkable(targetCell))
            {
                targetCell = GetClosestWalkableNeighbor(targetCell);
            }

            // Repath berkala sesuai agresivitas level
            bool needsRepath = (Time.time - lastRepathTime > RepathInterval) || currentPath == null || currentPath.Count == 0 || (currentPathIndex >= currentPath.Count);
            if (needsRepath)
            {
                lastRepathTime = Time.time;
                currentPath = CalculateBFSPath(startCell, targetCell);
                currentPathIndex = (currentPath != null && currentPath.Count > 1) ? 1 : 0;

                Debug.Log($"[ENEMY PATH]\nStart Cell: {startCell}\nTarget Cell: {targetCell}\nPath Found: {(currentPath != null && currentPath.Count > 0)}\nPath Length: {(currentPath != null ? currentPath.Count : 0)}");
            }

            // Ikuti waypoint jalur BFS
            if (currentPath != null && currentPath.Count > 0 && currentPathIndex < currentPath.Count)
            {
                Vector3Int nextCell = currentPath[currentPathIndex];
                Vector3 targetWaypointPos = new Vector3(nextCell.x + 0.5f, nextCell.y + 0.5f, transform.position.z);
                Vector2 moveDelta = (Vector2)targetWaypointPos - (Vector2)transform.position;
                float distToWaypoint = moveDelta.magnitude;

                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetWaypointPos,
                    chaseSpeed * dt
                );

                FaceTowards(targetWaypointPos);

                if (visualAnimator != null)
                {
                    visualAnimator.SetMoving(true, moveDelta.normalized);
                }

                float distToTarget = Vector2.Distance(transform.position, targetWorldPos);
                LogEnemyMove(targetWaypointPos, distToTarget);

                if (distToWaypoint < 0.12f)
                {
                    currentPathIndex++;
                }
            }
            else
            {
                Vector2 moveDelta = (Vector2)targetWorldPos - (Vector2)transform.position;

                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetWorldPos,
                    chaseSpeed * dt
                );
                FaceTowards(targetWorldPos);

                if (visualAnimator != null)
                {
                    visualAnimator.SetMoving(true, moveDelta.normalized);
                }

                float distToTarget = Vector2.Distance(transform.position, targetWorldPos);
                LogEnemyMove(targetWorldPos, distToTarget);
            }
        }

        /// <summary>
        /// Melakukan satu langkah update pergerakan/logika Enemy (berguna untuk Play Mode dan verifikasi runtime).
        /// </summary>
        public void StepMovement(float dt = 0.02f)
        {
            if (isDefeated) return;
            FindPlayerReference();
            if (playerTransform == null) return;

            if (visualAnimator != null && visualAnimator.IsBusyWithAttack)
            {
                return;
            }

            float dist = Vector2.Distance(transform.position, playerTransform.position);
            if (dist > attackRange)
            {
                isChasing = true;
                isPreparingAttack = false;
                UpdateAndFollowPathToPlayer(dt);
            }
            else
            {
                isChasing = false;
                isPreparingAttack = true;
                FaceTowardsPlayer();
                AttemptAttackPlayer();
            }
        }

        private void FaceTowardsPlayer()
        {
            if (playerTransform != null)
            {
                FaceTowards(playerTransform.position);
            }
        }

        private void FaceTowards(Vector3 targetWorldPos)
        {
            float dx = targetWorldPos.x - transform.position.x;
            float dy = targetWorldPos.y - transform.position.y;
            Vector2 dir = new Vector2(dx, dy);
            if (dir.sqrMagnitude > 0.001f)
            {
                currentFacingDirection = dir.normalized;
                if (visionController != null)
                {
                    visionController.SetFacingDirection(currentFacingDirection);
                }
            }

            if (visualAnimator != null)
            {
                visualAnimator.FaceDirection(dx);
            }

            if (spriteRenderer != null)
            {
                if (dx > 0.04f)
                {
                    spriteRenderer.flipX = false;
                }
                else if (dx < -0.04f)
                {
                    spriteRenderer.flipX = true;
                }
            }
        }

        private Vector3Int GetClosestWalkableNeighbor(Vector3Int origin)
        {
            if (IsCellWalkable(origin)) return origin;

            for (int r = 1; r <= 3; r++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        if (Mathf.Abs(dx) + Mathf.Abs(dy) == r)
                        {
                            Vector3Int cand = new Vector3Int(origin.x + dx, origin.y + dy, 0);
                            if (IsCellWalkable(cand)) return cand;
                        }
                    }
                }
            }
            return origin;
        }

        /// <summary>
        /// Algoritma BFS untuk menemukan rute lorong labirin yang bebas dinding.
        /// </summary>
        public List<Vector3Int> CalculateBFSPath(Vector3Int start, Vector3Int target)
        {
            if (start == target)
            {
                return new List<Vector3Int> { start };
            }

            if (!IsCellWalkable(start))
            {
                start = GetClosestWalkableNeighbor(start);
            }
            if (!IsCellWalkable(target))
            {
                target = GetClosestWalkableNeighbor(target);
            }

            if (start == target)
            {
                return new List<Vector3Int> { start };
            }

            Queue<Vector3Int> queue = new Queue<Vector3Int>();
            Dictionary<Vector3Int, Vector3Int> cameFrom = new Dictionary<Vector3Int, Vector3Int>();

            queue.Enqueue(start);
            cameFrom[start] = start;

            Vector3Int[] directions = new Vector3Int[]
            {
                Vector3Int.up,
                Vector3Int.down,
                Vector3Int.left,
                Vector3Int.right
            };

            bool found = false;
            int maxIterations = 2500;
            int iter = 0;
            Vector3Int closestVisited = start;
            float minDistanceToTarget = float.MaxValue;

            while (queue.Count > 0 && iter++ < maxIterations)
            {
                Vector3Int current = queue.Dequeue();
                float distToTgt = (current - target).sqrMagnitude;
                if (distToTgt < minDistanceToTarget)
                {
                    minDistanceToTarget = distToTgt;
                    closestVisited = current;
                }

                if (current == target)
                {
                    found = true;
                    break;
                }

                for (int i = 0; i < 4; i++)
                {
                    Vector3Int neighbor = current + directions[i];
                    if (!cameFrom.ContainsKey(neighbor) && IsCellWalkable(neighbor))
                    {
                        cameFrom[neighbor] = current;
                        queue.Enqueue(neighbor);
                    }
                }
            }

            Vector3Int destination = found ? target : closestVisited;
            if (destination == start)
            {
                return new List<Vector3Int> { start };
            }

            List<Vector3Int> path = new List<Vector3Int>();
            Vector3Int curr = destination;
            while (curr != start)
            {
                path.Add(curr);
                curr = cameFrom[curr];
            }
            path.Add(start);
            path.Reverse();
            return path;
        }

        /// <summary>
        /// Mengeksekusi rangkaian serangan Enemy ke Player:
        /// APPROACH -> ATTACK PREPARATION -> SWORD SWING -> HIT / MISS -> RECOVERY -> CHASE/IDLE
        /// </summary>
        public void AttemptAttackPlayer()
        {
            if (isDefeated) return;

            float cooldownRemaining = Mathf.Max(0f, damageCooldown - (Time.time - lastAttackAttemptTime));
            float distToPlayer = playerTransform != null ? Vector2.Distance(transform.position, playerTransform.position) : 999f;

            if (Time.time - lastAttackAttemptTime < damageCooldown)
            {
                LogEnemyAttack(GetCurrentStateName(), distToPlayer, cooldownRemaining, false, false, false);
                return;
            }

            if (visualAnimator != null && visualAnimator.IsBusyWithAttack)
                return;

            lastAttackAttemptTime = Time.time;
            LogEnemyAttack("ATTACK PREPARE", distToPlayer, 0f, true, false, false);

            if (visualAnimator != null)
            {
                visualAnimator.StartAttackSequence(OnAttackHitFrame);
            }
            else
            {
                StartCoroutine(AttackLungeRoutine());
                CheckAndDamagePlayer(playerTransform != null ? playerTransform.gameObject : null);
            }
        }

        /// <summary>
        /// Dipanggil tepat SATU KALI pada hit frame puncak ayunan pedang.
        /// Mengecek apakah Player masih dalam jangkauan (HIT) atau berhasil menghindar (MISS).
        /// </summary>
        private void OnAttackHitFrame()
        {
            if (isDefeated) return;
            FindPlayerReference();
            if (playerTransform == null) return;

            if (IsBoss)
            {
                Debug.Log("[BOSS ATTACK]\nDamage Window");
            }

            float dist = Vector2.Distance(transform.position, playerTransform.position);
            bool hitSuccess = false;

            if (dist <= attackRange)
            {
                if (IsBoss)
                {
                    LogBossStateChange("ATTACK");
                    Debug.Log("[BOSS ATTACK]\nHit Player");
                }

                // =========================================================
                // HIT: Player masih berada dalam jangkauan pedang
                // =========================================================
                if (PlayerLivesManager.Instance != null)
                {
                    if (PlayerLivesManager.Instance.IsInvulnerable || PlayerLivesManager.Instance.IsGameOver)
                    {
                        Debug.Log("[Combat] Enemy hit blocked: Player invulnerable atau game over.");
                    }
                    else
                    {
                        hitSuccess = true;
                        lastDamageTime = Time.time;
                        Debug.Log($"[Combat] ⚔ ENEMY HIT! Jarak={dist:F2} <= {attackRange}. Player kehilangan {damageAmount} HP.");
                        PlayerLivesManager.Instance.TakeDamage(damageAmount);

                        if (AudioManager.Instance != null)
                        {
                            AudioManager.Instance.PlaySFX(AudioManager.SFXType.EnemySwordHit, 0.9f);
                        }

                        if (visualAnimator != null)
                        {
                            visualAnimator.TriggerHitImpactRecoil();
                        }
                    }
                }
            }
            else
            {
                if (IsBoss)
                {
                    Debug.Log("[BOSS ATTACK]\nMiss");
                }
                Debug.Log($"[Combat] 💨 ENEMY MISS! Jarak={dist:F2} > {attackRange}. Player berhasil menghindar!");
            }

            LogEnemyAttack("ATTACK", dist, 0f, false, true, hitSuccess);
        }

        private IEnumerator AttackLungeRoutine()
        {
            if (playerTransform == null) yield break;

            Vector3 originalPos = transform.position;
            Vector3 targetDir = (playerTransform.position - transform.position).normalized * 0.15f;

            transform.position += targetDir;
            yield return new WaitForSeconds(0.08f);
            transform.position = originalPos;
        }

        /// <summary>
        /// Me-reset cooldown damage lokal (berguna untuk testing atau inisialisasi).
        /// </summary>
        public void ResetDamageCooldown()
        {
            lastDamageTime = -999f;
            lastAttackAttemptTime = -999f;
        }

        /// <summary>
        /// Mengecek apakah objek target adalah Player dan memberikan damage HANYA JIKA dalam attackRange.
        /// Mengurangi 1 nyawa Player secara terkontrol dengan cooldown wajar.
        /// </summary>
        public void CheckAndDamagePlayer(GameObject target, bool ignoreInvulnerable = false, bool ignoreDistance = false)
        {
            if (target == null || isDefeated) return;

            bool isPlayer = target.CompareTag("Player") || target.GetComponent<PlayerController>() != null;
            if (!isPlayer) return;

            // ATURAN 1: Enemy HANYA menyerang jika sudah dekat (masuk attackRange)
            if (!ignoreDistance)
            {
                float dist = Vector2.Distance(transform.position, target.transform.position);
                if (dist > attackRange)
                {
                    // Player masih jauh -> Enemy TIDAK memberikan damage
                    return;
                }
            }

            // ATURAN 3 & 4: Periksa jeda cooldown serangan
            if (Time.time - lastDamageTime < damageCooldown)
            {
                return;
            }

            if (PlayerLivesManager.Instance != null)
            {
                if ((!ignoreInvulnerable && PlayerLivesManager.Instance.IsInvulnerable) || PlayerLivesManager.Instance.IsGameOver)
                {
                    return;
                }

                lastDamageTime = Time.time;
                Debug.Log($"[EnemyCharacter] ⚔ Enemy '{gameObject.name}' menyerang Player! Mengurangi {damageAmount} nyawa.");
                PlayerLivesManager.Instance.TakeDamage(damageAmount, ignoreInvulnerable);
            }
            else
            {
                Debug.LogWarning("[EnemyCharacter] PlayerLivesManager.Instance tidak ditemukan!");
            }
        }

        /// <summary>
        /// Membaca input serangan Player dengan tombol utama: SPACE.
        /// Player HANYA dapat merusak Enemy jika berada dalam playerAttackRange (1.2 - 1.5 unit).
        /// SPACE TIDAK BOLEH MENGURANGI NYAWA PLAYER!
        /// </summary>
        private void CheckPlayerSpaceCombatInput()
        {
            if (isDefeated) return;

            bool spaceTriggered = false;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.spaceKey.wasPressedThisFrame)
            {
                spaceTriggered = true;
            }

            var gp = Gamepad.current;
            if (gp != null && (gp.buttonSouth.wasPressedThisFrame || gp.buttonWest.wasPressedThisFrame))
            {
                spaceTriggered = true;
            }
#endif

            // Fallback Legacy Input untuk SPACE (Aman dari Exception New Input System)
            if (!spaceTriggered)
            {
                try
                {
                    if (Input.GetKeyDown(KeyCode.Space))
                    {
                        spaceTriggered = true;
                    }
                }
                catch (System.InvalidOperationException)
                {
                    // New Input System package aktif di Player Settings
                }
            }

            // Cadangan opsional tombol sekunder jika SPACE tidak ditekan
            if (!spaceTriggered)
            {
                try
                {
                    if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
                    {
                        spaceTriggered = true;
                    }
                }
                catch (System.InvalidOperationException)
                {
                    // New Input System package aktif di Player Settings
                }
            }

            if (spaceTriggered)
            {
                Debug.Log("[PLAYER ATTACK]\nSPACE pressed");
                FindPlayerReference();
                if (playerTransform != null)
                {
                    float dist = Vector2.Distance(transform.position, playerTransform.position);
                    int inRange = dist <= playerAttackRange ? 1 : 0;
                    Debug.Log($"[PLAYER ATTACK]\nEnemy count in range = {inRange}");

                    var visual = playerTransform.GetComponentInChildren<PlayerVisualAnimator>();
                    if (visual != null)
                    {
                        visual.RequestAttack((Vector2)(transform.position - playerTransform.position));
                    }
                    TryPlayerAttack(playerTransform.gameObject, 1);
                }
            }
        }

        /// <summary>
        /// Menjalankan serangan Player terhadap Enemy dengan validasi jangkauan (playerAttackRange).
        /// Mengembalikan true jika Enemy berhasil diserang, false jika Player terlalu jauh.
        /// SPACE HANYA MERUSAK ENEMY DAN TIDAK PERNAH MENGURANGI NYAWA PLAYER!
        /// </summary>
        public bool TryPlayerAttack(GameObject player, int damage = 1)
        {
            if (isDefeated || player == null) return false;

            float dist = Vector2.Distance(transform.position, player.transform.position);

            // ATURAN 3: Jika Enemy terlalu jauh (> playerAttackRange), SPACE TIDAK melakukan damage
            if (dist > playerAttackRange)
            {
                Debug.Log($"[Combat] PLAYER ATTACK SPACE: Enemy distance = {dist:F2} > {playerAttackRange}. Tidak ada damage. HP Player aman.");
                return false;
            }

            // Player berada dalam jangkauan serangan (<= 1.4 unit)
            Debug.Log($"[Combat] ⚡ PLAYER ATTACK SPACE: Enemy distance = {dist:F2} <= {playerAttackRange}. Enemy hit!");

            // 1. Enemy berhenti sebentar (stagger) & jeda serangan
            staggerTimer = 0.28f;
            lastAttackAttemptTime = Time.time + 0.45f;
            lastDamageTime = Time.time + 0.45f;

            // 2. Knockback pendek menjauhi Player dengan aman (tanpa menembus dinding labirin)
            Vector2 knockDir = ((Vector2)transform.position - (Vector2)player.transform.position).normalized;
            if (knockDir.sqrMagnitude > 0.001f)
            {
                Vector3 targetPos = transform.position + (Vector3)(knockDir * 0.22f);
                Vector3Int cellPos = new Vector3Int(Mathf.FloorToInt(targetPos.x), Mathf.FloorToInt(targetPos.y), 0);
                if (IsCellWalkable(cellPos))
                {
                    transform.position = targetPos;
                }
            }

            // 3. Efek Hit Partikel & Audio
            MazeSpell.Gameplay.ElementalParticleSpawner.SpawnHitImpactEffect(transform.position);
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.SFXType.EnemyHit, 0.95f);
            }

            // 4. Kurangi HP Enemy dan jalankan reaksi visual/suara
            TakeDamage(damage);
            return true;
        }

        private void OnMouseDown()
        {
            if (isDefeated) return;

            if (playerTransform != null)
            {
                TryPlayerAttack(playerTransform.gameObject, 1);
            }
        }

        private float lastPlayerHitTime = -999f;

        /// <summary>
        /// Mengurangi HP Enemy saat terkena serangan Player (tombol SPACE).
        /// </summary>
        public void TakeDamage(int damage = 1, bool bypassDebounce = false)
        {
            if (isDefeated) return;
            if (!bypassDebounce && Time.time > 0.001f && (Time.time - lastPlayerHitTime) < 0.15f) return;
            lastPlayerHitTime = Time.time;

            int prevHealth = currentHealth;
            currentHealth = Mathf.Max(0, currentHealth - damage);

            if (IsBoss)
            {
                Debug.Log($"[BOSS]\nDamage received = {damage}\nBoss HP:\n{prevHealth} → {currentHealth}");
            }
            Debug.Log($"[EnemyCharacter] 💥 Enemy menerima {damage} damage. Sisa HP: {currentHealth}/{maxHealth}");

            if (visualAnimator != null)
            {
                visualAnimator.TriggerHurt();
            }
            else if (!isFlashing)
            {
                StartCoroutine(DamageFlashRoutine());
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.SFXType.EnemyHurt, 0.9f);
            }

            UpdateHealthIndicator();
            OnEnemyHealthChanged?.Invoke(this, currentHealth, maxHealth);

            // Reaksi Boss saat HP mencapai 1 (Warning Phase)
            if (IsBoss && currentHealth == 1)
            {
                TriggerBossEnrageWarning();
            }

            // Penentuan Kalah vs Split
            if (currentHealth <= 0)
            {
                if (CanSplit)
                {
                    if (splitController == null)
                    {
                        splitController = GetComponent<EnemySplitController>() ?? gameObject.AddComponent<EnemySplitController>();
                        splitController.Initialize(this, true, false, 0);
                    }
                    Debug.Log($"[EnemyCharacter] HP 0 reached! Splitting {gameObject.name} into 2 mini enemies.");
                    splitController.TriggerSplit(this);
                }
                else
                {
                    Defeat();
                }
            }
        }

        private IEnumerator DamageFlashRoutine()
        {
            isFlashing = true;
            if (spriteRenderer != null)
            {
                Color orig = spriteRenderer.color;
                spriteRenderer.color = new Color(1f, 0.3f, 0.3f, 1f);
                yield return new WaitForSeconds(0.12f);
                spriteRenderer.color = orig;
            }
            isFlashing = false;
        }

        /// <summary>
        /// Mengalahkan Enemy:
        /// 1. Ditandai defeated sebelum reward diberikan.
        /// 2. Memberikan bonus +1 nyawa ke Player (maksimum 3, tepat 1 kali).
        /// 3. Menjalankan animasi kekalahan dan memicu selebrasi Player.
        /// 4. Menghilangkan Enemy dari level secara permanen (tidak respawn).
        /// </summary>
        public void Defeat()
        {
            if (isDefeated) return;

            isDefeated = true;
            currentHealth = 0;
            isChasing = false;
            isPreparingAttack = false;

            if (enemyCollider == null)
            {
                enemyCollider = GetComponent<Collider2D>();
            }

            if (enemyCollider != null)
            {
                enemyCollider.enabled = false;
            }

            UpdateHealthIndicator();
            OnEnemyHealthChanged?.Invoke(this, currentHealth, maxHealth);

            if (!rewardGiven)
            {
                rewardGiven = true;
                if (PlayerLivesManager.Instance != null)
                {
                    Debug.Log("[EnemyCharacter] ★ Enemy dikalahkan! Player mendapatkan reward +1 nyawa.");
                    PlayerLivesManager.Instance.AddLife(1);
                }
            }

            OnEnemyDefeated?.Invoke(this);

            if (visualAnimator != null)
            {
                visualAnimator.TriggerDefeat(OnDefeatComplete);
            }
            else
            {
                StartCoroutine(DefeatRoutine());
            }
        }

        private void OnDefeatComplete()
        {
            FindPlayerReference();
            if (playerTransform != null)
            {
                PlayerVisualAnimator playerAnim = playerTransform.GetComponentInChildren<PlayerVisualAnimator>();
                if (playerAnim != null)
                {
                    playerAnim.TriggerCelebration();
                }
            }

            Destroy(gameObject);
        }

        private IEnumerator DefeatRoutine()
        {
            float duration = 0.35f;
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                transform.Rotate(0f, 0f, 720f * Time.deltaTime);

                if (spriteRenderer != null)
                {
                    Color c = spriteRenderer.color;
                    c.a = Mathf.Lerp(1f, 0f, t);
                    spriteRenderer.color = c;
                }

                yield return null;
            }

            OnDefeatComplete();
        }

        private void DoIdleBobbing()
        {
            if (enableIdleAnimation && visualAnimator == null)
            {
                float newY = initialBasePos.y + Mathf.Sin(Time.time * idleBobSpeed) * idleBobAmplitude;
                transform.localPosition = new Vector3(initialBasePos.x, newY, initialBasePos.z);
            }
        }

        public void SetBasePosition(Vector3 pos)
        {
            initialBasePos = pos;
            anchorPosition = pos;
            transform.localPosition = pos;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
            {
                AttemptAttackPlayer();
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
            {
                AttemptAttackPlayer();
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Player") || collision.gameObject.GetComponent<PlayerController>() != null)
            {
                AttemptAttackPlayer();
            }
        }

        /// <summary>
        /// Memastikan struktur visual Enemy (EnemyVisual + Sword + SlashFx) terpasang dengan rapi.
        /// </summary>
        public void EnsureVisualHierarchy()
        {
            if (visualAnimator != null) return;

            Transform visualTr = transform.Find("EnemyVisual");
            if (visualTr == null)
            {
                GameObject visualGo = new GameObject("EnemyVisual");
                visualGo.transform.SetParent(transform, false);
                visualGo.transform.localPosition = Vector3.zero;
                visualGo.transform.localRotation = Quaternion.identity;
                visualGo.transform.localScale = Vector3.one;
                visualTr = visualGo.transform;
            }

            // SpriteRenderer untuk badan
            SpriteRenderer rootSr = GetComponent<SpriteRenderer>();
            SpriteRenderer bodySr = visualTr.GetComponent<SpriteRenderer>();
            if (bodySr == null)
            {
                bodySr = visualTr.gameObject.AddComponent<SpriteRenderer>();
            }

            Sprite bodySprite = null;
#if UNITY_EDITOR
            bodySprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Enemy_Body.png");
#endif
            if (bodySprite == null && rootSr != null && rootSr.sprite != null)
            {
                bodySprite = rootSr.sprite;
            }
            if (bodySprite != null) bodySr.sprite = bodySprite;
            bodySr.sortingOrder = 4;
            if (rootSr != null)
            {
                bodySr.color = rootSr.color;
            }

            // Nonaktifkan SpriteRenderer pada root agar tidak dobel render
            if (rootSr != null && rootSr != bodySr)
            {
                rootSr.enabled = false;
            }

            // Sword child
            Transform swordTr = visualTr.Find("Sword");
            if (swordTr == null)
            {
                GameObject swordGo = new GameObject("Sword");
                swordGo.transform.SetParent(visualTr, false);
                swordGo.transform.localPosition = new Vector3(-0.14f, -0.28f, -0.01f);
                swordGo.transform.localRotation = Quaternion.Euler(0f, 0f, -40f);
                swordTr = swordGo.transform;
            }

            SpriteRenderer swordSr = swordTr.GetComponent<SpriteRenderer>();
            if (swordSr == null)
            {
                swordSr = swordTr.gameObject.AddComponent<SpriteRenderer>();
            }
            Sprite swordSprite = null;
#if UNITY_EDITOR
            swordSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Enemy_Sword.png");
#endif
            if (swordSprite != null) swordSr.sprite = swordSprite;
            swordSr.sortingOrder = 5;

            // SlashFx child
            Transform slashTr = visualTr.Find("SlashFx");
            if (slashTr == null)
            {
                GameObject slashGo = new GameObject("SlashFx");
                slashGo.transform.SetParent(visualTr, false);
                slashGo.transform.localPosition = new Vector3(0.26f, -0.05f, -0.02f);
                slashTr = slashGo.transform;
            }

            SpriteRenderer slashSr = slashTr.GetComponent<SpriteRenderer>();
            if (slashSr == null)
            {
                slashSr = slashTr.gameObject.AddComponent<SpriteRenderer>();
            }
            Sprite slashSprite = null;
#if UNITY_EDITOR
            slashSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Slash_Arc.png");
#endif
            if (slashSprite != null) slashSr.sprite = slashSprite;
            slashSr.sortingOrder = 6;
            slashTr.gameObject.SetActive(false);

            // Audio Source
            AudioSource aSrc = GetComponent<AudioSource>();
            if (aSrc == null)
            {
                aSrc = gameObject.AddComponent<AudioSource>();
                aSrc.playOnAwake = false;
                aSrc.spatialBlend = 0f;
            }

            visualAnimator = visualTr.GetComponent<EnemyVisualAnimator>();
            if (visualAnimator == null)
            {
                visualAnimator = visualTr.gameObject.AddComponent<EnemyVisualAnimator>();
            }

            visualAnimator.Initialize(visualTr, bodySr, swordTr, swordSr, slashTr, slashSr, this);
            EnsureHealthIndicator();
            if (visionController == null)
            {
                visionController = GetComponent<MazeSpell.Gameplay.EnemyVisionController>();
                if (visionController == null)
                {
                    visionController = gameObject.AddComponent<MazeSpell.Gameplay.EnemyVisionController>();
                }
            }
        }

        /// <summary>
        /// Memastikan indikator HP Enemy ada dan terhubung.
        /// </summary>
        public void EnsureHealthIndicator()
        {
            if (healthIndicator != null) return;

            Transform indTr = transform.Find("HealthIndicator");
            if (indTr == null)
            {
                GameObject indGo = new GameObject("HealthIndicator");
                indGo.transform.SetParent(transform, false);
                indGo.transform.localPosition = new Vector3(0f, 0.72f, -0.05f);
                indTr = indGo.transform;
            }

            healthIndicator = indTr.GetComponent<EnemyHealthIndicator>();
            if (healthIndicator == null)
            {
                healthIndicator = indTr.gameObject.AddComponent<EnemyHealthIndicator>();
            }
        }

        /// <summary>
        /// Memperbarui tampilan indikator HP Enemy (♥ ♥ ♥).
        /// </summary>
        public void UpdateHealthIndicator()
        {
            EnsureHealthIndicator();
            if (healthIndicator != null)
            {
                healthIndicator.UpdateHealth(currentHealth, maxHealth);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.9f, 0.2f, 0.2f, 0.75f);
            if (enemyCollider != null)
            {
                Gizmos.DrawWireCube(enemyCollider.bounds.center, enemyCollider.bounds.size);
            }
            else
            {
                Gizmos.DrawWireSphere(transform.position, 0.45f);
            }

            // Detection range
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            // Attack range (merah)
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, attackRange);

            // Player attack range (hijau)
            Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, playerAttackRange);

            GUIStyle style = new GUIStyle();
            style.fontSize = 9;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = new Color(1f, 0.3f, 0.3f, 0.9f);
            style.alignment = TextAnchor.MiddleCenter;
            Handles.Label(transform.position + Vector3.up * 0.55f, $"⚔ ENEMY Lvl:{currentActiveLevel} (HP:{currentHealth} SPD:{chaseSpeed})", style);
        }
#endif
    }
}
