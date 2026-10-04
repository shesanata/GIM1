using System.Collections;
using UnityEngine;

namespace MazeSpell
{
    /// <summary>
    /// Mengontrol visual dan sistem animasi Player pixel-art (top-down):
    /// 1. IDLE: Gerakan napas halus, naik-turun mikro, dan kepala/badan sedikit bergerak.
    /// 2. WALK: Langkah berirama (step bobbing), tilt waddle, dan squash-stretch kaki saat berjalan.
    ///    Langsung kembali ke IDLE saat berhenti tanpa pose berjalan yang tertinggal.
    /// 3. ATTACK: Lunge cepat ke arah Enemy/serangan, squash & stretch pukulan, responsif (~0.2 detik).
    ///    SPACE TIDAK PERNAH mengurangi HP Player.
    /// 4. HIT: Berkedip merah-transparan cepat dan recoil getar saat menerima damage.
    /// 5. COLLECT: Little hop dan scale bounce saat mengambil huruf dari WordCollectionManager.
    /// 6. CELEBRATION: Animasi kemenangan bertingkat saat mengalahkan Enemy:
    ///    - Jongkok ancang-ancang
    ///    - Lompatan tinggi ke udara dengan pose kemenangan & putaran sukacita
    ///    - Mendarat dengan squash dan mini bounce sekunder
    ///    - Kembali ke IDLE secara mulus (~1.1 detik)
    ///    - Mengunci pergerakan selama selebrasi agar tidak overlap atau spam attack.
    /// Prioritas State: CELEBRATION > HIT > ATTACK > COLLECT > WALK > IDLE.
    /// </summary>
    public class PlayerVisualAnimator : MonoBehaviour
    {
        public enum AnimState
        {
            Idle,
            Walk,
            Collect,
            Attack,
            Hit,
            Celebration
        }

        [Header("Referensi Komponen")]
        [Tooltip("Transform child visual yang akan dianimasikan tanpa merusak Rigidbody/Collider")]
        [SerializeField] private Transform visualTransform;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private PlayerController playerController;

        [Header("Pengaturan Idle")]
        [SerializeField] private float idleFrequency = 2.8f;
        [SerializeField] private float idleBobHeight = 0.025f;
        [SerializeField] private float idleScalePulse = 0.025f;

        [Header("Pengaturan Walk")]
        [SerializeField] private float walkStepFrequency = 11f;
        [SerializeField] private float walkBobHeight = 0.055f;
        [SerializeField] private float walkTiltAngle = 4.0f;
        [SerializeField] private float walkSquash = 0.06f;

        [Header("Pengaturan Attack")]
        [SerializeField] private float attackDuration = 0.22f;
        [SerializeField] private float attackLungeDistance = 0.22f;
        [SerializeField] private float attackCooldown = 0.25f;

        [Header("Pengaturan Hit / Damage")]
        [SerializeField] private float hitDuration = 0.35f;

        [Header("Pengaturan Collect")]
        [SerializeField] private float collectHopDuration = 0.32f;
        [SerializeField] private float collectHopHeight = 0.25f;

        [Header("Pengaturan Celebration")]
        [SerializeField] private float celebrationDuration = 1.15f;
        [SerializeField] private float celebrationJumpHeight = 0.6f;

        // Status Internal
        private AnimState currentState = AnimState.Idle;
        private Vector3 initialVisualLocalPos = Vector3.zero;
        private Vector3 baseScale = Vector3.one;
        private float facingDirection = 1f; // 1 = kanan, -1 = kiri
        private float walkTimer = 0f;
        private float lastAttackTime = -999f;
        private Coroutine activeActionRoutine = null;
        private Color originalColor = Color.white;

        // Visual slash / punch indicator
        private GameObject attackFxObj;
        private SpriteRenderer attackFxSr;

        public AnimState CurrentState => currentState;
        public bool IsBusyWithAction => currentState == AnimState.Celebration || currentState == AnimState.Hit;
        public bool IsCelebrating => currentState == AnimState.Celebration;

        private void Awake()
        {
            EnsureVisualHierarchy();
        }

        private void Start()
        {
            EnsureVisualHierarchy();
            SubscribeToGameplayEvents();
        }

        public void ResetToIdleForTesting()
        {
            if (activeActionRoutine != null)
            {
                StopCoroutine(activeActionRoutine);
                activeActionRoutine = null;
            }
            currentState = AnimState.Idle;
            if (visualTransform != null)
            {
                visualTransform.localPosition = initialVisualLocalPos;
                visualTransform.localRotation = Quaternion.identity;
                visualTransform.localScale = new Vector3(baseScale.x * facingDirection, baseScale.y, 1f);
            }
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
            if (attackFxObj != null)
            {
                attackFxObj.SetActive(false);
            }
            if (playerController != null)
            {
                playerController.IsControlLocked = false;
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromGameplayEvents();
        }

        /// <summary>
        /// Memisahkan Visual dari Root Player agar transformasi animasi tidak mengubah
        /// Collider2D, Rigidbody2D, dan koordinat gameplay.
        /// </summary>
        public void EnsureVisualHierarchy()
        {
            if (playerController == null)
            {
                playerController = GetComponent<PlayerController>();
                if (playerController == null)
                {
                    playerController = GetComponentInParent<PlayerController>();
                }
            }

            // Cari atau buat child 'PlayerVisual'
            Transform foundVisual = transform.Find("PlayerVisual");
            if (foundVisual == null)
            {
                // Jika skrip ini menempel pada root Player
                GameObject visualGo = new GameObject("PlayerVisual");
                visualGo.transform.SetParent(transform, false);
                visualGo.transform.localPosition = Vector3.zero;
                visualGo.transform.localRotation = Quaternion.identity;
                visualGo.transform.localScale = Vector3.one;
                visualTransform = visualGo.transform;
            }
            else
            {
                visualTransform = foundVisual;
            }

            // Pastikan SpriteRenderer berada di PlayerVisual child
            SpriteRenderer rootSr = GetComponent<SpriteRenderer>();
            SpriteRenderer childSr = visualTransform.GetComponent<SpriteRenderer>();

            if (childSr == null)
            {
                childSr = visualTransform.gameObject.AddComponent<SpriteRenderer>();
            }

            if (rootSr != null && rootSr != childSr)
            {
                // Salin properti visual dari root ke child
                childSr.sprite = rootSr.sprite;
                childSr.sharedMaterial = rootSr.sharedMaterial;
                childSr.color = rootSr.color;
                childSr.sortingLayerID = rootSr.sortingLayerID;
                childSr.sortingOrder = rootSr.sortingOrder;
                childSr.flipX = rootSr.flipX;
                childSr.flipY = rootSr.flipY;

                // Matikan / hilangkan SpriteRenderer di root agar tidak terjadi duplikasi sprite
                rootSr.enabled = false;
                Destroy(rootSr);
            }

            spriteRenderer = childSr;

            // Pastikan sprite Character dimuat jika belum ada
            if (spriteRenderer.sprite == null)
            {
                Sprite charSp = Resources.Load<Sprite>("Character");
#if UNITY_EDITOR
                if (charSp == null)
                {
                    charSp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Character.png");
                }
#endif
                if (charSp != null)
                {
                    spriteRenderer.sprite = charSp;
                }
            }

            spriteRenderer.sortingOrder = 5;
            originalColor = spriteRenderer.color;

            initialVisualLocalPos = Vector3.zero;
            baseScale = Vector3.one;

            CreateAttackFx();
        }

        private void CreateAttackFx()
        {
            if (attackFxObj != null) return;

            attackFxObj = new GameObject("AttackSlashFX");
            attackFxObj.transform.SetParent(visualTransform, false);
            attackFxObj.transform.localPosition = new Vector3(0.4f, 0f, 0f);
            attackFxObj.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

            attackFxSr = attackFxObj.AddComponent<SpriteRenderer>();
            attackFxSr.sortingOrder = 6;
            attackFxSr.color = new Color(1f, 0.9f, 0.4f, 0f);

            // Buat tekstur slash pixel kecil
            Texture2D tex = new Texture2D(8, 8);
            Color[] colors = new Color[64];
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    bool isSlash = (x == y || x == y + 1 || x == y - 1);
                    colors[y * 8 + x] = isSlash ? new Color(1f, 1f, 1f, 0.95f) : Color.clear;
                }
            }
            tex.filterMode = FilterMode.Point;
            tex.SetPixels(colors);
            tex.Apply();

            Sprite slashSprite = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 16f);
            attackFxSr.sprite = slashSprite;
            attackFxObj.SetActive(false);
        }

        private void SubscribeToGameplayEvents()
        {
            // 1. Listen ke PlayerLivesManager saat terkena damage
            if (PlayerLivesManager.HasInstance && PlayerLivesManager.Instance.onDamageTaken != null)
            {
                PlayerLivesManager.Instance.onDamageTaken.RemoveListener(OnPlayerDamageTaken);
                PlayerLivesManager.Instance.onDamageTaken.AddListener(OnPlayerDamageTaken);
            }

            // 2. Listen ke WordCollectionManager saat huruf berhasil diambil
            if (WordCollectionManager.HasInstance && WordCollectionManager.Instance.onLetterCollected != null)
            {
                WordCollectionManager.Instance.onLetterCollected.RemoveListener(OnLetterCollectedEvent);
                WordCollectionManager.Instance.onLetterCollected.AddListener(OnLetterCollectedEvent);
            }

            // 3. Listen ke EnemyCharacter defeat
            EnemyCharacter.OnEnemyDefeated -= OnEnemyDefeatedHandler;
            EnemyCharacter.OnEnemyDefeated += OnEnemyDefeatedHandler;
        }

        private void UnsubscribeFromGameplayEvents()
        {
            if (PlayerLivesManager.HasInstance && PlayerLivesManager.Instance.onDamageTaken != null)
            {
                PlayerLivesManager.Instance.onDamageTaken.RemoveListener(OnPlayerDamageTaken);
            }

            if (WordCollectionManager.HasInstance && WordCollectionManager.Instance.onLetterCollected != null)
            {
                WordCollectionManager.Instance.onLetterCollected.RemoveListener(OnLetterCollectedEvent);
            }

            EnemyCharacter.OnEnemyDefeated -= OnEnemyDefeatedHandler;
        }

        private void Update()
        {
            if (visualTransform == null) return;

            // Periksa arah hadap Player
            Vector2 input = playerController != null ? playerController.CurrentMoveInput : Vector2.zero;

            if (input.x > 0.05f)
            {
                facingDirection = 1f;
            }
            else if (input.x < -0.05f)
            {
                facingDirection = -1f;
            }

            // Terapkan flip horizontal pada visual
            Vector3 curScale = visualTransform.localScale;
            curScale.x = Mathf.Abs(curScale.x) * facingDirection;
            visualTransform.localScale = curScale;

            // Jika sedang dalam aksi berprioritas tinggi (Celebration, Hit, Attack, Collect), biarkan coroutine mengontrol
            if (currentState == AnimState.Celebration ||
                currentState == AnimState.Hit ||
                currentState == AnimState.Attack ||
                currentState == AnimState.Collect)
            {
                return;
            }

            // Update State: WALK vs IDLE
            bool isMoving = input.sqrMagnitude > 0.01f;

            if (isMoving)
            {
                currentState = AnimState.Walk;
                AnimateWalk();
            }
            else
            {
                currentState = AnimState.Idle;
                AnimateIdle();
            }
        }

        /// <summary>
        /// Animasi IDLE: Badan bernapas lembut, naik-turun halus, dan rotasi mikro.
        /// </summary>
        private void AnimateIdle()
        {
            walkTimer = 0f; // Reset walk cadence

            float breath = Mathf.Sin(Time.time * idleFrequency);
            float yOff = Mathf.Max(0f, breath) * idleBobHeight;
            float scaleY = 1f + (breath * idleScalePulse);
            float scaleX = 1f - (breath * (idleScalePulse * 0.6f));

            float microTilt = Mathf.Sin(Time.time * 1.2f) * 0.75f;

            visualTransform.localPosition = Vector3.Lerp(visualTransform.localPosition, initialVisualLocalPos + new Vector3(0f, yOff, 0f), Time.deltaTime * 12f);
            visualTransform.localRotation = Quaternion.Lerp(visualTransform.localRotation, Quaternion.Euler(0f, 0f, microTilt), Time.deltaTime * 10f);
            visualTransform.localScale = new Vector3(scaleX * facingDirection, scaleY, 1f);
        }

        /// <summary>
        /// Animasi WALK: Rhythmic step bobbing, tilt waddle kiri-kanan sesuai langkah, dan squash-stretch kaki.
        /// Saat berhenti, segera kembali ke IDLE secara mulus.
        /// </summary>
        private void AnimateWalk()
        {
            walkTimer += Time.deltaTime * walkStepFrequency;

            // Step hop: melompat kecil pada tiap langkah
            float stepHop = Mathf.Abs(Mathf.Sin(walkTimer)) * walkBobHeight;

            // Step tilt: bergoyang kiri-kanan mengikuti ayunan kaki
            float stepTilt = Mathf.Sin(walkTimer) * walkTiltAngle;

            // Squash saat menyentuh lantai, stretch saat di puncak langkah
            float stretch = (stepHop / walkBobHeight) * walkSquash;
            float scaleY = 1f + stretch;
            float scaleX = 1f - (stretch * 0.7f);

            visualTransform.localPosition = initialVisualLocalPos + new Vector3(0f, stepHop, 0f);
            visualTransform.localRotation = Quaternion.Euler(0f, 0f, stepTilt * facingDirection);
            visualTransform.localScale = new Vector3(scaleX * facingDirection, scaleY, 1f);
        }

        #region Action Triggers

        /// <summary>
        /// Memicu aksi serangan Player (tombol SPACE).
        /// SPACE TIDAK PERNAH mengurangi HP Player.
        /// </summary>
        public bool RequestAttack(Vector2 targetDirection = default)
        {
            if (currentState == AnimState.Celebration || currentState == AnimState.Hit)
            {
                return false;
            }

            if (Time.time < lastAttackTime + attackCooldown)
            {
                return false;
            }

            lastAttackTime = Time.time;

            if (activeActionRoutine != null)
            {
                StopCoroutine(activeActionRoutine);
            }

            activeActionRoutine = StartCoroutine(AttackRoutine(targetDirection));
            return true;
        }

        private IEnumerator AttackRoutine(Vector2 targetDir)
        {
            currentState = AnimState.Attack;

            // Tentukan arah lunge serangan
            if (targetDir == default || targetDir.sqrMagnitude < 0.01f)
            {
                // Cari apakah ada Enemy aktif terdekat
                EnemyCharacter enemy = EnemyCharacter.ActiveEnemy;
                if (enemy == null)
                {
#if UNITY_2023_1_OR_NEWER
                    enemy = FindAnyObjectByType<EnemyCharacter>();
#else
                    enemy = FindObjectOfType<EnemyCharacter>();
#endif
                }

                if (enemy != null && !enemy.IsDefeated)
                {
                    targetDir = (enemy.transform.position - transform.position).normalized;
                }
                else
                {
                    targetDir = new Vector2(facingDirection, 0f);
                }
            }
            else
            {
                targetDir.Normalize();
            }

            // Tampilkan efek slash
            if (attackFxObj != null && attackFxSr != null)
            {
                attackFxObj.SetActive(true);
                attackFxSr.color = new Color(1f, 0.95f, 0.4f, 1f);
                attackFxObj.transform.localPosition = new Vector3(0.35f, 0.05f, 0f);
            }

            // Phase 1: Antisipasi / windup cepat (0.05s)
            float elapsed = 0f;
            float windupDuration = 0.05f;
            while (elapsed < windupDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / windupDuration;
                visualTransform.localPosition = initialVisualLocalPos - (Vector3)(targetDir * (attackLungeDistance * 0.3f * t));
                visualTransform.localScale = new Vector3(0.9f * facingDirection, 1.15f, 1f);
                yield return null;
            }

            // Phase 2: Lunge pukulan maju ke depan (0.09s)
            elapsed = 0f;
            float thrustDuration = 0.09f;
            Vector3 peakLunge = initialVisualLocalPos + (Vector3)(targetDir * attackLungeDistance);
            while (elapsed < thrustDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / thrustDuration;
                visualTransform.localPosition = Vector3.Lerp(visualTransform.localPosition, peakLunge, t);
                visualTransform.localScale = new Vector3(1.25f * facingDirection, 0.85f, 1f);
                yield return null;
            }

            // Sembunyikan efek slash
            if (attackFxObj != null)
            {
                attackFxObj.SetActive(false);
            }

            // Phase 3: Recovery mulus kembali ke posisi awal (0.08s)
            elapsed = 0f;
            float recoveryDuration = 0.08f;
            while (elapsed < recoveryDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / recoveryDuration;
                visualTransform.localPosition = Vector3.Lerp(visualTransform.localPosition, initialVisualLocalPos, t);
                visualTransform.localScale = Vector3.Lerp(visualTransform.localScale, new Vector3(baseScale.x * facingDirection, baseScale.y, 1f), t);
                yield return null;
            }

            visualTransform.localPosition = initialVisualLocalPos;
            visualTransform.localScale = new Vector3(baseScale.x * facingDirection, baseScale.y, 1f);
            visualTransform.localRotation = Quaternion.identity;

            currentState = AnimState.Idle;
            activeActionRoutine = null;
        }

        /// <summary>
        /// Memicu feedback visual saat Player terkena serangan Enemy/Hazard.
        /// </summary>
        public void TriggerHit()
        {
            if (currentState == AnimState.Celebration) return;

            if (activeActionRoutine != null)
            {
                StopCoroutine(activeActionRoutine);
            }

            activeActionRoutine = StartCoroutine(HitRoutine());
        }

        private IEnumerator HitRoutine()
        {
            currentState = AnimState.Hit;

            float elapsed = 0f;
            int flashCount = 0;

            while (elapsed < hitDuration)
            {
                elapsed += Time.deltaTime;
                flashCount++;

                // Berkedip merah dan transparan
                if (spriteRenderer != null)
                {
                    if (flashCount % 2 == 0)
                    {
                        spriteRenderer.color = new Color(1f, 0.25f, 0.25f, 0.9f);
                    }
                    else
                    {
                        spriteRenderer.color = new Color(1f, 1f, 1f, 0.35f);
                    }
                }

                // Shudder / getaran horizontal kecil
                float shakeX = Random.Range(-0.06f, 0.06f);
                visualTransform.localPosition = initialVisualLocalPos + new Vector3(shakeX, -0.04f, 0f);
                visualTransform.localScale = new Vector3(1.15f * facingDirection, 0.85f, 1f);

                yield return new WaitForSeconds(0.04f);
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }

            visualTransform.localPosition = initialVisualLocalPos;
            visualTransform.localScale = new Vector3(baseScale.x * facingDirection, baseScale.y, 1f);
            visualTransform.localRotation = Quaternion.identity;

            currentState = AnimState.Idle;
            activeActionRoutine = null;
        }

        /// <summary>
        /// Memicu feedback visual little hop dan scale bounce saat mengambil huruf.
        /// </summary>
        public void TriggerCollect()
        {
            if (currentState == AnimState.Celebration || currentState == AnimState.Hit) return;

            if (activeActionRoutine != null)
            {
                StopCoroutine(activeActionRoutine);
            }

            activeActionRoutine = StartCoroutine(CollectRoutine());
        }

        private IEnumerator CollectRoutine()
        {
            currentState = AnimState.Collect;

            float elapsed = 0f;
            while (elapsed < collectHopDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / collectHopDuration;

                // Parabola hop
                float hopY = Mathf.Sin(t * Mathf.PI) * collectHopHeight;

                // Scale swelling di udara
                float scaleBoost = Mathf.Sin(t * Mathf.PI) * 0.2f;

                visualTransform.localPosition = initialVisualLocalPos + new Vector3(0f, hopY, 0f);
                visualTransform.localScale = new Vector3((1f + scaleBoost) * facingDirection, 1f + scaleBoost, 1f);

                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.Lerp(originalColor, new Color(1f, 0.95f, 0.6f, 1f), Mathf.Sin(t * Mathf.PI));
                }

                yield return null;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }

            // Pendaratan empuk (soft squash bounce)
            visualTransform.localPosition = initialVisualLocalPos;
            visualTransform.localScale = new Vector3(1.08f * facingDirection, 0.92f, 1f);
            yield return new WaitForSeconds(0.05f);

            visualTransform.localScale = new Vector3(baseScale.x * facingDirection, baseScale.y, 1f);
            visualTransform.localRotation = Quaternion.identity;

            currentState = AnimState.Idle;
            activeActionRoutine = null;
        }

        /// <summary>
        /// Memicu CELEBRATION ANIMATION ketika Enemy berhasil dikalahkan.
        /// Ini adalah animasi paling penting:
        /// 1. Player melakukan lompatan sukacita ke udara
        /// 2. Squash-stretch kemenangan dan putaran 360 derajat di udara
        /// 3. Mendarat dengan bounce
        /// 4. Durasi ~1.15 detik, mengunci gerak & attack agar tidak tumpang-tindih.
        /// </summary>
        public void TriggerCelebration()
        {
            if (activeActionRoutine != null)
            {
                StopCoroutine(activeActionRoutine);
            }

            activeActionRoutine = StartCoroutine(CelebrationRoutine());
        }

        private IEnumerator CelebrationRoutine()
        {
            currentState = AnimState.Celebration;
            Debug.Log("[PlayerVisualAnimator] ★ CELEBRATION DIMULAI! Enemy telah dikalahkan!");

            // Kunci kontrol gerakan Player selama perayaan
            if (playerController != null)
            {
                playerController.IsControlLocked = true;
            }

            // ==========================================
            // TAHAP 1: Jongkok Ancang-ancang (0.0 - 0.12 detik)
            // ==========================================
            float elapsed = 0f;
            float prepDuration = 0.12f;
            while (elapsed < prepDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / prepDuration;
                visualTransform.localPosition = initialVisualLocalPos + new Vector3(0f, -0.06f * t, 0f);
                visualTransform.localScale = new Vector3(1.25f * facingDirection, 0.75f, 1f);
                yield return null;
            }

            // ==========================================
            // TAHAP 2: Lompatan Tinggi ke Udara (0.12 - 0.50 detik)
            // ==========================================
            elapsed = 0f;
            float jumpUpDuration = 0.38f;
            while (elapsed < jumpUpDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / jumpUpDuration;

                // Kurva melompat ke puncak
                float currentY = Mathf.Sin(t * (Mathf.PI * 0.5f)) * celebrationJumpHeight;

                // Stretch badan saat meluncur ke atas
                visualTransform.localPosition = initialVisualLocalPos + new Vector3(0f, currentY, 0f);
                visualTransform.localScale = new Vector3(0.85f * facingDirection, 1.25f, 1f);

                // Sedikit berkilau emas sukacita
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.Lerp(originalColor, new Color(1f, 1f, 0.65f, 1f), t);
                }

                yield return null;
            }

            // ==========================================
            // TAHAP 3: Pose Kemenangan di Udara & Putaran Sukacita (0.50 - 0.80 detik)
            // ==========================================
            elapsed = 0f;
            float airSpinDuration = 0.30f;
            while (elapsed < airSpinDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / airSpinDuration;

                // Sedikit melayang di puncak (hover)
                float hoverY = celebrationJumpHeight + Mathf.Sin(t * Mathf.PI) * 0.06f;
                visualTransform.localPosition = initialVisualLocalPos + new Vector3(0f, hoverY, 0f);

                // Putaran sukacita 360 derajat di udara
                float spinAngle = 360f * t * facingDirection;
                visualTransform.localRotation = Quaternion.Euler(0f, 0f, spinAngle);
                visualTransform.localScale = new Vector3(1.15f * facingDirection, 1.15f, 1f);

                yield return null;
            }

            visualTransform.localRotation = Quaternion.identity;

            // ==========================================
            // TAHAP 4: Meluncur Turun & Mendarat Squash (0.80 - 1.00 detik)
            // ==========================================
            elapsed = 0f;
            float landDuration = 0.20f;
            while (elapsed < landDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / landDuration;

                float currentY = Mathf.Cos(t * (Mathf.PI * 0.5f)) * celebrationJumpHeight;
                visualTransform.localPosition = initialVisualLocalPos + new Vector3(0f, currentY, 0f);
                visualTransform.localScale = Vector3.Lerp(new Vector3(1.15f * facingDirection, 1.15f, 1f), new Vector3(1.3f * facingDirection, 0.7f, 1f), t);

                yield return null;
            }

            // ==========================================
            // TAHAP 5: Mini Bounce Sekunder (1.00 - 1.15 detik)
            // ==========================================
            elapsed = 0f;
            float bounceDuration = 0.15f;
            while (elapsed < bounceDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / bounceDuration;

                float miniY = Mathf.Sin(t * Mathf.PI) * 0.12f;
                visualTransform.localPosition = initialVisualLocalPos + new Vector3(0f, miniY, 0f);
                visualTransform.localScale = Vector3.Lerp(new Vector3(1.3f * facingDirection, 0.7f, 1f), new Vector3(baseScale.x * facingDirection, baseScale.y, 1f), t);

                yield return null;
            }

            // Kembalikan ke normal
            visualTransform.localPosition = initialVisualLocalPos;
            visualTransform.localRotation = Quaternion.identity;
            visualTransform.localScale = new Vector3(baseScale.x * facingDirection, baseScale.y, 1f);

            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }

            // Buka kembali kontrol gerakan Player
            if (playerController != null)
            {
                playerController.IsControlLocked = false;
            }

            currentState = AnimState.Idle;
            activeActionRoutine = null;
            Debug.Log("[PlayerVisualAnimator] ✓ CELEBRATION SELESAI! Player kembali ke IDLE normal.");
        }

        #endregion

        #region Event Callbacks

        private void OnPlayerDamageTaken()
        {
            TriggerHit();
        }

        private void OnLetterCollectedEvent(char letter, int collected, int total)
        {
            TriggerCollect();
        }

        private void OnEnemyDefeatedHandler(EnemyCharacter enemy)
        {
            TriggerCelebration();
        }

        #endregion
    }
}
