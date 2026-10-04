using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace MazeSpell
{
    /// <summary>
    /// Mengatur 3 nyawa Player, sistem damage, respawn, dan Game Over.
    /// Nyawa selalu di-reset menjadi 3 pada awal level atau saat restart level.
    /// </summary>
    public class PlayerLivesManager : MonoBehaviour
    {
        private static PlayerLivesManager _instance;
        public static PlayerLivesManager Instance
        {
            get
            {
                if (_instance == null)
                {
#if UNITY_2023_1_OR_NEWER
                    _instance = Object.FindAnyObjectByType<PlayerLivesManager>();
#else
                    _instance = Object.FindObjectOfType<PlayerLivesManager>();
#endif
                    if (_instance == null)
                    {
                        var go = new GameObject("PlayerLivesManager");
                        _instance = go.AddComponent<PlayerLivesManager>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        public static bool HasInstance => _instance != null;

        public const int MaxLives = 3;

        [Header("Status Nyawa")]
        [SerializeField] private int currentLives = MaxLives;
        [SerializeField] private bool isGameOver = false;

        [Header("Pengaturan Damage & Respawn")]
        [SerializeField] private float invulnerabilityDuration = 1.2f;
        [SerializeField] private bool respawnOnDamage = true;
        [SerializeField] private Vector3 respawnPosition = Vector3.zero;

        [Header("Debug HUD (IMGUI)")]
        [Tooltip("Aktifkan hanya jika ingin melihat box debug OnGUI")]
        [SerializeField] private bool showHUD = false;

        [Header("Events")]
        public UnityEvent<int, int> onLivesChanged = new UnityEvent<int, int>();
        public UnityEvent onDamageTaken = new UnityEvent();
        public UnityEvent onGameOver = new UnityEvent();
        public UnityEvent onLivesReset = new UnityEvent();

        private bool isInvulnerable = false;
        private GameObject playerObject;

        public int CurrentLives => currentLives;
        public int MaxLivesCount => MaxLives;
        public bool IsGameOver => isGameOver;
        public bool IsInvulnerable => isInvulnerable;
        public Vector3 RespawnPosition => respawnPosition;
        public int DamageTakenThisLevel { get; private set; } = 0;

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
        }

        private void EnsureUnityEvents()
        {
            if (onLivesChanged == null) onLivesChanged = new UnityEvent<int, int>();
            if (onDamageTaken == null) onDamageTaken = new UnityEvent();
            if (onGameOver == null) onGameOver = new UnityEvent();
            if (onLivesReset == null) onLivesReset = new UnityEvent();
        }

        private void Start()
        {
            EnsureUnityEvents();
            FindPlayer();

            if (LevelProgressionManager.HasInstance && LevelProgressionManager.Instance.onLevelStarted != null)
            {
                LevelProgressionManager.Instance.onLevelStarted.RemoveListener(OnLevelStarted);
                LevelProgressionManager.Instance.onLevelStarted.AddListener(OnLevelStarted);
            }

            ResetLives();
        }

        private void OnDestroy()
        {
            if (LevelProgressionManager.HasInstance && LevelProgressionManager.Instance.onLevelStarted != null)
            {
                LevelProgressionManager.Instance.onLevelStarted.RemoveListener(OnLevelStarted);
            }

            if (_instance == this)
            {
                _instance = null;
            }
        }

        public void SetRespawnPosition(Vector3 newPosition)
        {
            respawnPosition = newPosition;
            Debug.Log($"[PlayerLivesManager] Posisi respawn diperbarui: {respawnPosition}");
        }

        private void OnLevelStarted(int level)
        {
            Debug.Log($"[PlayerLivesManager] Level {level} dimulai. Nyawa kembali 3/3.");
            ResetLives();
        }

        /// <summary>
        /// Mengurangi nyawa pemain. Jika nyawa habis, memicu Game Over.
        /// Jika masih ada nyawa, respawn pemain ke titik awal level dan memberikan invulnerability sesaat.
        /// </summary>
        public void TakeDamage(int damage = 1, bool ignoreInvulnerable = false)
        {
            if (isGameOver || (!ignoreInvulnerable && isInvulnerable))
                return;

            DamageTakenThisLevel += damage;
            currentLives = Mathf.Clamp(currentLives - damage, 0, MaxLives);

            Debug.Log($"[PlayerLivesManager] ❤️ Nyawa berkurang: {currentLives}/{MaxLives}");

            onLivesChanged?.Invoke(currentLives, MaxLives);
            onDamageTaken?.Invoke();

            if (currentLives <= 0)
            {
                GameOver();
                return;
            }

            if (respawnOnDamage)
            {
                RespawnPlayer();
            }

            StartCoroutine(InvulnerabilityRoutine());
        }

        private void GameOver()
        {
            isGameOver = true;
            Debug.Log("[PlayerLivesManager] 💀 GAME OVER! Nyawa telah habis.");
            onGameOver?.Invoke();
        }

        /// <summary>
        /// Menambahkan nyawa pemain (misal saat Enemy berhasil dikalahkan).
        /// Maksimum nyawa tetap MaxLives (3).
        /// Memperbarui UI secara langsung melalui event onLivesChanged.
        /// </summary>
        public void AddLife(int amount = 1)
        {
            if (isGameOver)
                return;

            int oldLives = currentLives;
            currentLives = Mathf.Clamp(currentLives + amount, 0, MaxLives);

            Debug.Log($"[PlayerLivesManager] ❤️ Nyawa bertambah (+{amount}): {currentLives}/{MaxLives}");

            onLivesChanged?.Invoke(currentLives, MaxLives);
        }

        /// <summary>
        /// Menyetel nilai nyawa secara langsung tanpa invulnerability (untuk keperluan testing).
        /// </summary>
        public void SetLivesForTesting(int lives)
        {
            StopAllCoroutines();
            currentLives = Mathf.Clamp(lives, 0, MaxLives);
            isInvulnerable = false;
            isGameOver = (currentLives <= 0);
            onLivesChanged?.Invoke(currentLives, MaxLives);
        }

        /// <summary>
        /// Mengembalikan nyawa menjadi 3 penuh dan mereset status Game Over.
        /// </summary>
        public void ResetLives()
        {
            StopAllCoroutines();

            currentLives = MaxLives;
            DamageTakenThisLevel = 0;
            isGameOver = false;
            isInvulnerable = false;

            // Pastikan sprite pemain tampak normal
            FindPlayer();
            if (playerObject != null)
            {
                SpriteRenderer sr = playerObject.GetComponent<SpriteRenderer>();
                if (sr != null) sr.enabled = true;
            }

            onLivesChanged?.Invoke(currentLives, MaxLives);
            onLivesReset?.Invoke();

            Debug.Log("[PlayerLivesManager] ❤️ Nyawa berhasil di-reset menjadi 3/3.");
        }

        /// <summary>
        /// Mengulang level yang sedang aktif dengan 3 nyawa penuh dan progres kata yang di-reset.
        /// </summary>
        public void RestartLevel()
        {
            ResetLives();

            if (LevelProgressionManager.Instance != null)
            {
                LevelProgressionManager.Instance.RestartCurrentLevel();
            }
            else
            {
                RespawnPlayer();
            }
        }

        public void RestartGameFromBeginning()
        {
            ResetLives();

            if (LevelProgressionManager.Instance != null)
            {
                LevelProgressionManager.Instance.RestartGame();
            }
            else
            {
                RespawnPlayer();
            }
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

        public void RespawnPlayer()
        {
            FindPlayer();

            if (playerObject == null)
                return;

            playerObject.transform.position = respawnPosition;

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

        private IEnumerator InvulnerabilityRoutine()
        {
            isInvulnerable = true;
            FindPlayer();

            SpriteRenderer sr = playerObject != null ? playerObject.GetComponent<SpriteRenderer>() : null;
            float elapsed = 0f;
            float interval = 0.15f;

            while (elapsed < invulnerabilityDuration)
            {
                if (sr != null)
                {
                    sr.enabled = !sr.enabled;
                }

                yield return new WaitForSeconds(interval);
                elapsed += interval;
            }

            if (sr != null)
            {
                sr.enabled = true;
            }

            isInvulnerable = false;
        }

        private void OnGUI()
        {
            // Tampilkan dialog Game Over jika pemain kehabisan nyawa
            if (isGameOver)
            {
                GUI.color = Color.red;

                float boxW = 380f;
                float boxH = 180f;
                float boxX = (Screen.width - boxW) / 2f;
                float boxY = (Screen.height - boxH) / 2f;

                GUI.Box(new Rect(boxX, boxY, boxW, boxH), "💀 GAME OVER");

                GUI.color = Color.white;
                GUI.Label(
                    new Rect(boxX + 20, boxY + 45, boxW - 40, 40),
                    "Nyawa kamu sudah habis! Ingin mengulang level ini?"
                );

                if (GUI.Button(new Rect(boxX + 40, boxY + 105, boxW - 80, 40), "Ulangi Level"))
                {
                    RestartLevel();
                }

                return;
            }

            if (!showHUD)
                return;

            float x = 10f;
            float y = 290f;
            float width = 300f;
            float height = 60f;

            GUI.Box(new Rect(x, y, width, height), "❤️ NYAWA");

            string hearts = "";
            for (int i = 0; i < MaxLives; i++)
            {
                hearts += i < currentLives ? "♥ " : "♡ ";
            }

            GUI.Label(new Rect(x + 15, y + 25, width - 30, 25), $"{hearts}   {currentLives}/{MaxLives}");
        }
    }
}