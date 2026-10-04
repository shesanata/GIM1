using System;
using System.Collections;
using UnityEngine;
using MazeSpell.Audio;

namespace MazeSpell
{
    /// <summary>
    /// Mengontrol visual dan sistem animasi Enemy pixel-art dengan pedang:
    /// 1. IDLE: Gerakan napas halus, badan naik-turun mikro, pedang sedikit bergerak/sway.
    /// 2. WALK/CHASE: Langkah berirama (step bounce), tilt waddle kaki, pedang mengikuti gerakan tubuh,
    ///    langsung kembali ke IDLE saat berhenti tanpa frame berjalan yang tertahan.
    /// 3. ATTACK SEQUENCE:
    ///    - APPROACH -> ATTACK_PREPARATION (wind-up ~0.20s, badan mundur, pedang siap, ready SFX)
    ///    - -> SWORD_SWING (~0.16s, whoosh SFX, lunge, ayunan pedang, slash visual effect)
    ///    - -> HIT / MISS (single hit check pada hit frame: jika dalam range -> HIT, impact SFX,
    ///         player -1 HP, enemy recoil; jika player menghindar -> MISS, 0 damage)
    ///    - -> RECOVERY (~0.14s, kembali ke posisi netral) -> COOLDOWN -> IDLE / CHASE.
    /// 4. HURT: Flash merah/terang, recoil visual getar singkat, hurt SFX, kembali mengejar tanpa merusak BFS.
    /// 5. DEFEATED: Defeat animation (bounce / spin / squash), defeat SFX, disappear,
    ///    memicu selebrasi Player dan reward +1 HP (sekali, max 3, tidak respawn).
    /// Prioritas State: DEFEATED > HURT > ATTACK_SWING > ATTACK_PREPARE > ATTACK_RECOVERY > CHASE > IDLE.
    /// </summary>
    public class EnemyVisualAnimator : MonoBehaviour
    {
        public enum AnimState
        {
            Idle,
            Walk,
            AttackPrepare,
            AttackSwing,
            AttackRecovery,
            Hurt,
            Defeated
        }

        [Header("Referensi Komponen Visual")]
        [SerializeField] private Transform visualTransform;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private Transform swordTransform;
        [SerializeField] private SpriteRenderer swordRenderer;
        [SerializeField] private Transform slashFxTransform;
        [SerializeField] private SpriteRenderer slashFxRenderer;
        [SerializeField] private EnemyCharacter enemyCharacter;
        [SerializeField] private AudioSource localAudioSource;

        [Header("Pengaturan Idle")]
        [SerializeField] private float idleFrequency = 2.6f;
        [SerializeField] private float idleBobHeight = 0.022f;
        [SerializeField] private float idleScalePulse = 0.020f;
        [SerializeField] private float swordIdleSwayAngle = 3.5f;

        [Header("Pengaturan Walk / Chase")]
        [SerializeField] private float walkStepFrequency = 10.5f;
        [SerializeField] private float walkBobHeight = 0.045f;
        [SerializeField] private float walkTiltAngle = 3.5f;
        [SerializeField] private float walkSquash = 0.05f;
        [SerializeField] private float swordWalkSwayAngle = 7.0f;

        [Header("Pengaturan Attack Sequence")]
        [SerializeField] private float attackPrepareDuration = 0.20f;
        [SerializeField] private float attackSwingDuration = 0.16f;
        [SerializeField] private float attackRecoveryDuration = 0.14f;
        [SerializeField] private float attackLungeDistance = 0.20f;
        [SerializeField] private float swordReadyAngle = 45f;
        [SerializeField] private float swordSwingStartAngle = 45f;
        [SerializeField] private float swordSwingEndAngle = -70f;

        [Header("Pengaturan Hurt")]
        [SerializeField] private float hurtDuration = 0.20f;

        [Header("Pengaturan Defeat")]
        [SerializeField] private float defeatDuration = 0.45f;

        [Header("Audio Clips (Opsional Asset Eksternal)")]
        [SerializeField] private AudioClip swordSwingClip;
        [SerializeField] private AudioClip swordHitClip;
        [SerializeField] private AudioClip enemyHurtClip;
        [SerializeField] private AudioClip enemyDefeatedClip;
        [SerializeField] private AudioClip enemyAttackReadyClip;
        [SerializeField] private AudioClip enemyStepClip;

        // Status Internal
        private AnimState currentState = AnimState.Idle;
        private Vector3 initialVisualPos = Vector3.zero;
        private Vector3 initialSwordPos = new Vector3(-0.14f, -0.28f, -0.01f);
        private float initialSwordAngle = -40f;
        private Vector3 baseScale = Vector3.one;
        private float facingDirection = 1f; // 1 = kanan, -1 = kiri
        private float walkTimer = 0f;
        private float nextAllowedStepAudioTime = 0f;
        private bool isMoving = false;
        private Coroutine activeSequenceRoutine = null;
        private Color originalBodyColor = Color.white;
        private Color originalSwordColor = Color.white;

        // Properti Publik
        public AnimState CurrentState => currentState;
        public bool IsBusyWithAttack => currentState == AnimState.AttackPrepare || currentState == AnimState.AttackSwing || currentState == AnimState.AttackRecovery;
        public bool IsBusyWithHighPriority => currentState == AnimState.Defeated || currentState == AnimState.Hurt || currentState == AnimState.AttackSwing;
        public bool IsDefeated => currentState == AnimState.Defeated;

        private void Awake()
        {
            if (visualTransform == null) visualTransform = transform;
            if (bodyRenderer == null) bodyRenderer = GetComponent<SpriteRenderer>();
            if (localAudioSource == null) localAudioSource = GetComponent<AudioSource>();
            if (localAudioSource == null)
            {
                localAudioSource = gameObject.AddComponent<AudioSource>();
                localAudioSource.playOnAwake = false;
                localAudioSource.spatialBlend = 0f; // 2D clean audio
            }

            initialVisualPos = visualTransform.localPosition;
            if (bodyRenderer != null) originalBodyColor = bodyRenderer.color;

            if (swordTransform != null)
            {
                initialSwordPos = swordTransform.localPosition;
                initialSwordAngle = swordTransform.localEulerAngles.z;
                if (initialSwordAngle > 180f) initialSwordAngle -= 360f;
                if (swordRenderer != null) originalSwordColor = swordRenderer.color;
            }

            if (slashFxTransform != null)
            {
                slashFxTransform.gameObject.SetActive(false);
            }
        }

        public void Initialize(
            Transform visualTr, 
            SpriteRenderer bodySr, 
            Transform swordTr, 
            SpriteRenderer swordSr, 
            Transform slashTr, 
            SpriteRenderer slashSr, 
            EnemyCharacter enemyChar)
        {
            visualTransform = visualTr;
            bodyRenderer = bodySr;
            swordTransform = swordTr;
            swordRenderer = swordSr;
            slashFxTransform = slashTr;
            slashFxRenderer = slashSr;
            enemyCharacter = enemyChar;

            if (bodyRenderer != null) originalBodyColor = bodyRenderer.color;
            if (swordRenderer != null) originalSwordColor = swordRenderer.color;

            initialVisualPos = visualTransform.localPosition;
            if (swordTransform != null)
            {
                initialSwordPos = swordTransform.localPosition;
                initialSwordAngle = swordTransform.localEulerAngles.z;
                if (initialSwordAngle > 180f) initialSwordAngle -= 360f;
            }

            if (slashFxTransform != null)
            {
                slashFxTransform.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (currentState == AnimState.Defeated) return;

            // Update pose berdasarkan state aktif (prioritas rendah)
            if (currentState == AnimState.Idle)
            {
                DoIdleAnimation();
            }
            else if (currentState == AnimState.Walk)
            {
                DoWalkAnimation();
            }
        }

        #region Animasi Idle

        private void DoIdleAnimation()
        {
            float t = Time.time * idleFrequency;
            float bobY = Mathf.Sin(t) * idleBobHeight;
            float scalePulse = Mathf.Sin(t) * idleScalePulse;

            if (visualTransform != null)
            {
                visualTransform.localPosition = initialVisualPos + new Vector3(0f, bobY, 0f);
                visualTransform.localRotation = Quaternion.identity;
                visualTransform.localScale = new Vector3(baseScale.x * facingDirection, baseScale.y * (1f + scalePulse), 1f);
            }

            if (swordTransform != null)
            {
                float swordSway = Mathf.Sin(t + 0.5f) * swordIdleSwayAngle;
                swordTransform.localPosition = initialSwordPos;
                swordTransform.localRotation = Quaternion.Euler(0f, 0f, initialSwordAngle + swordSway);
            }
        }

        #endregion

        #region Animasi Walk / Chase

        private void DoWalkAnimation()
        {
            walkTimer += Time.deltaTime * walkStepFrequency;
            float stepBob = Mathf.Abs(Mathf.Sin(walkTimer)) * walkBobHeight;
            float tilt = Mathf.Sin(walkTimer) * walkTiltAngle;
            float squash = 1f - (Mathf.Sin(walkTimer * 2f) * walkSquash);

            if (visualTransform != null)
            {
                visualTransform.localPosition = initialVisualPos + new Vector3(0f, stepBob, 0f);
                visualTransform.localRotation = Quaternion.Euler(0f, 0f, tilt);
                visualTransform.localScale = new Vector3(baseScale.x * facingDirection, baseScale.y * squash, 1f);
            }

            if (swordTransform != null)
            {
                float swordSway = Mathf.Sin(walkTimer) * swordWalkSwayAngle;
                swordTransform.localPosition = initialSwordPos;
                swordTransform.localRotation = Quaternion.Euler(0f, 0f, initialSwordAngle + swordSway);
            }

            // Suara langkah kaki lembut (volume rendah, tidak bising)
            if (Mathf.Sin(walkTimer) > 0.95f && Time.time >= nextAllowedStepAudioTime)
            {
                nextAllowedStepAudioTime = Time.time + 0.35f;
                PlayEnemySFX(AudioManager.SFXType.EnemyStep, enemyStepClip, 0.25f);
            }
        }

        /// <summary>
        /// Mengatur status gerak Enemy dan arah hadap dari EnemyCharacter.
        /// </summary>
        public void SetMoving(bool moving, Vector2 moveDirection)
        {
            if (currentState == AnimState.Defeated) return;

            isMoving = moving;

            if (Mathf.Abs(moveDirection.x) > 0.05f)
            {
                facingDirection = moveDirection.x > 0f ? 1f : -1f;
            }

            // Transisi State
            if (!IsBusyWithAttack && currentState != AnimState.Hurt && currentState != AnimState.Defeated)
            {
                if (isMoving)
                {
                    currentState = AnimState.Walk;
                }
                else
                {
                    if (currentState != AnimState.Idle)
                    {
                        ResetToIdlePose();
                    }
                }
            }
        }

        /// <summary>
        /// Menghadapkan Enemy ke arah target posisi.
        /// </summary>
        public void FaceDirection(float dirX)
        {
            if (Mathf.Abs(dirX) > 0.02f)
            {
                facingDirection = dirX > 0f ? 1f : -1f;
                if (visualTransform != null)
                {
                    visualTransform.localScale = new Vector3(baseScale.x * facingDirection, visualTransform.localScale.y, 1f);
                }
            }
        }

        #endregion

        #region Attack Sequence

        /// <summary>
        /// Memulai urutan serangan lengkap:
        /// APPROACH -> ATTACK PREPARATION -> SWORD SWING -> HIT / MISS -> RECOVERY -> CHASE/IDLE
        /// </summary>
        public bool StartAttackSequence(Action onHitCheckCallback)
        {
            if (currentState == AnimState.Defeated || currentState == AnimState.Hurt) return false;
            if (IsBusyWithAttack) return false;

            if (activeSequenceRoutine != null)
            {
                StopCoroutine(activeSequenceRoutine);
            }

            activeSequenceRoutine = StartCoroutine(AttackSequenceRoutine(onHitCheckCallback));
            return true;
        }

        private EnemyCharacter parentEnemy;
        private bool IsBoss
        {
            get
            {
                if (parentEnemy == null) parentEnemy = GetComponentInParent<EnemyCharacter>();
                return parentEnemy != null && parentEnemy.IsBoss;
            }
        }

        private IEnumerator AttackSequenceRoutine(Action onHitCheckCallback)
        {
            // =========================================================================
            // FASE 1: ATTACK PREPARATION (Wind-up ~0.20s)
            // =========================================================================
            currentState = AnimState.AttackPrepare;

            if (IsBoss)
            {
                Debug.Log("[BOSS ATTACK]\nPrepare");
            }

            // Mainkan suara siap menyerang (anticipation glint)
            PlayEnemySFX(AudioManager.SFXType.EnemyAttackReady, enemyAttackReadyClip, 0.65f);

            float prepElapsed = 0f;
            Vector3 prepStartPos = visualTransform.localPosition;
            Vector3 prepTargetPos = initialVisualPos - new Vector3(facingDirection * 0.12f, 0f, 0f);

            float prepStartSwordAngle = swordTransform != null ? swordTransform.localEulerAngles.z : initialSwordAngle;
            if (prepStartSwordAngle > 180f) prepStartSwordAngle -= 360f;

            while (prepElapsed < attackPrepareDuration)
            {
                prepElapsed += Time.deltaTime;
                float t = prepElapsed / attackPrepareDuration;
                float curve = Mathf.SmoothStep(0f, 1f, t);

                // Badan sedikit mundur menarik ancang-ancang
                if (visualTransform != null)
                {
                    visualTransform.localPosition = Vector3.Lerp(prepStartPos, prepTargetPos, curve);
                    visualTransform.localRotation = Quaternion.Euler(0f, 0f, -facingDirection * 4f * curve);
                    visualTransform.localScale = new Vector3(baseScale.x * facingDirection, baseScale.y * (1f - 0.05f * curve), 1f);
                }

                // Pedang ditarik ke posisi siap menyerang (terangkat ke atas)
                if (swordTransform != null)
                {
                    float angle = Mathf.Lerp(prepStartSwordAngle, swordReadyAngle, curve);
                    swordTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
                }

                yield return null;
            }

            // =========================================================================
            // FASE 2: SWORD SWING & HIT FRAME (~0.16s)
            // =========================================================================
            currentState = AnimState.AttackSwing;

            if (IsBoss)
            {
                Debug.Log("[BOSS ATTACK]\nSwing");
            }

            // Mainkan suara whoosh pedang tepat saat ayunan dimulai
            PlayEnemySFX(AudioManager.SFXType.EnemySwordSwing, swordSwingClip, 0.85f);

            // Aktifkan visual slash arc
            if (slashFxTransform != null && slashFxRenderer != null)
            {
                slashFxTransform.localPosition = new Vector3(0.26f, -0.05f, -0.02f);
                slashFxTransform.localRotation = Quaternion.Euler(0f, 0f, -15f);
                slashFxTransform.localScale = new Vector3(1f, 1f, 1f);
                slashFxRenderer.color = new Color(1f, 1f, 1f, 0.90f);
                slashFxTransform.gameObject.SetActive(true);
            }

            float swingElapsed = 0f;
            Vector3 swingStartPos = visualTransform.localPosition;
            Vector3 swingLungePos = initialVisualPos + new Vector3(facingDirection * attackLungeDistance, 0f, 0f);

            bool hitChecked = false;
            float hitCheckTimeThreshold = attackSwingDuration * 0.45f; // Frame puncak ayunan (~0.07 detik)

            while (swingElapsed < attackSwingDuration)
            {
                swingElapsed += Time.deltaTime;
                float t = swingElapsed / attackSwingDuration;

                // Lunge cepat ke depan dengan sedikit squash-stretch
                if (visualTransform != null)
                {
                    float posT = Mathf.Sin(t * Mathf.PI * 0.5f); // Fast forward attack
                    visualTransform.localPosition = Vector3.Lerp(swingStartPos, swingLungePos, posT);
                    visualTransform.localRotation = Quaternion.Euler(0f, 0f, facingDirection * 6f * (1f - t));
                    visualTransform.localScale = new Vector3(baseScale.x * facingDirection * 1.08f, baseScale.y * 0.92f, 1f);
                }

                // Pedang berayun cepat ke bawah melintasi busur
                if (swordTransform != null)
                {
                    float swingT = Mathf.SmoothStep(0f, 1f, t);
                    float swordAngle = Mathf.Lerp(swordSwingStartAngle, swordSwingEndAngle, swingT);
                    swordTransform.localRotation = Quaternion.Euler(0f, 0f, swordAngle);
                }

                // Fade slash arc
                if (slashFxTransform != null && slashFxRenderer != null)
                {
                    float slashAlpha = Mathf.Lerp(0.90f, 0f, t);
                    slashFxRenderer.color = new Color(1f, 1f, 1f, slashAlpha);
                }

                // HIT FRAME SATU KALI: Dipanggil tepat di puncak ayunan
                if (!hitChecked && swingElapsed >= hitCheckTimeThreshold)
                {
                    hitChecked = true;
                    onHitCheckCallback?.Invoke();
                }

                yield return null;
            }

            // Pastikan hit check tetap terpanggil jika delta time melompati threshold
            if (!hitChecked)
            {
                hitChecked = true;
                onHitCheckCallback?.Invoke();
            }

            // Matikan slash arc
            if (slashFxTransform != null)
            {
                slashFxTransform.gameObject.SetActive(false);
            }

            // =========================================================================
            // FASE 3: RECOVERY (~0.14s)
            // =========================================================================
            currentState = AnimState.AttackRecovery;
            if (IsBoss && parentEnemy != null)
            {
                parentEnemy.LogBossStateChange("RECOVERY");
            }

            float recElapsed = 0f;
            Vector3 recStartPos = visualTransform.localPosition;
            float recStartSwordAngle = swordTransform != null ? swordTransform.localEulerAngles.z : initialSwordAngle;
            if (recStartSwordAngle > 180f) recStartSwordAngle -= 360f;

            while (recElapsed < attackRecoveryDuration)
            {
                recElapsed += Time.deltaTime;
                float t = recElapsed / attackRecoveryDuration;
                float curve = Mathf.SmoothStep(0f, 1f, t);

                // Kembali ke posisi netral
                if (visualTransform != null)
                {
                    visualTransform.localPosition = Vector3.Lerp(recStartPos, initialVisualPos, curve);
                    visualTransform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                    visualTransform.localScale = new Vector3(baseScale.x * facingDirection, baseScale.y, 1f);
                }

                // Pedang kembali ke sudut idle
                if (swordTransform != null)
                {
                    float angle = Mathf.Lerp(recStartSwordAngle, initialSwordAngle, curve);
                    swordTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
                }

                yield return null;
            }

            // Selesai -> Kembali ke Idle atau Walk
            activeSequenceRoutine = null;
            ResetToIdlePose();
        }

        /// <summary>
        /// Memberikan efek recoil visual instan saat pedang berhasil mengenai target.
        /// </summary>
        public void TriggerHitImpactRecoil()
        {
            if (visualTransform != null)
            {
                visualTransform.localPosition -= new Vector3(facingDirection * 0.08f, 0f, 0f);
            }
        }

        /// <summary>
        /// Menyimulasikan frame hit check secara langsung untuk pengujian test runner.
        /// </summary>
        public void SimulateHitFrameForTesting(Action onHitCheckCallback)
        {
            currentState = AnimState.AttackSwing;
            onHitCheckCallback?.Invoke();
        }

        #endregion

        #region Animasi Hurt / Damage Reaction

        /// <summary>
        /// Memicu reaksi saat Enemy diserang Player (tombol SPACE):
        /// Flash merah/terang, recoil visual getar singkat, hit sound, kembali mengejar tanpa merusak koordinat BFS.
        /// </summary>
        public void TriggerHurt()
        {
            if (currentState == AnimState.Defeated) return;

            if (activeSequenceRoutine != null)
            {
                StopCoroutine(activeSequenceRoutine);
                activeSequenceRoutine = null;
            }

            if (slashFxTransform != null) slashFxTransform.gameObject.SetActive(false);

            activeSequenceRoutine = StartCoroutine(HurtRoutine());
        }

        private IEnumerator HurtRoutine()
        {
            currentState = AnimState.Hurt;

            // Mainkan suara Enemy Hit & Hurt
            PlayEnemySFX(AudioManager.SFXType.EnemyHit, enemyHurtClip, 0.95f);

            // Munculkan percikan partikel hit
            MazeSpell.Gameplay.ElementalParticleSpawner.SpawnHitImpactEffect(transform.position);

            // Flash visual warna merah-putih terang
            if (bodyRenderer != null) bodyRenderer.color = new Color(1f, 0.25f, 0.25f, 1f);
            if (swordRenderer != null) swordRenderer.color = new Color(1f, 0.6f, 0.6f, 1f);

            float elapsed = 0f;
            Vector3 startPos = visualTransform != null ? visualTransform.localPosition : initialVisualPos;
            Vector3 recoilPos = initialVisualPos - new Vector3(facingDirection * 0.15f, 0f, 0f);
            Vector3 origScale = baseScale;

            while (elapsed < hurtDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / hurtDuration);

                // Efek getar mikro (jitter)
                float jitter = (UnityEngine.Random.value * 2f - 1f) * 0.03f * (1f - t);

                // Squash & Stretch:
                // 0 - 0.35 : Squash (melebar & memendek)
                // 0.35 - 0.75 : Stretch (menyempit & meninggi)
                // 0.75 - 1.0 : Return to baseScale
                float sx = origScale.x;
                float sy = origScale.y;
                if (t < 0.35f)
                {
                    float sqT = t / 0.35f;
                    sx = Mathf.Lerp(origScale.x, origScale.x * 1.25f, sqT);
                    sy = Mathf.Lerp(origScale.y, origScale.y * 0.75f, sqT);
                }
                else if (t < 0.75f)
                {
                    float stT = (t - 0.35f) / 0.40f;
                    sx = Mathf.Lerp(origScale.x * 1.25f, origScale.x * 0.85f, stT);
                    sy = Mathf.Lerp(origScale.y * 0.75f, origScale.y * 1.20f, stT);
                }
                else
                {
                    float retT = (t - 0.75f) / 0.25f;
                    sx = Mathf.Lerp(origScale.x * 0.85f, origScale.x, retT);
                    sy = Mathf.Lerp(origScale.y * 1.20f, origScale.y, retT);
                }

                if (visualTransform != null)
                {
                    Vector3 currentBase = Vector3.Lerp(recoilPos, initialVisualPos, t);
                    visualTransform.localPosition = currentBase + new Vector3(0f, jitter, 0f);
                    visualTransform.localRotation = Quaternion.Euler(0f, 0f, -facingDirection * 6f * (1f - t));
                    visualTransform.localScale = new Vector3(sx * facingDirection, sy, 1f);
                }

                if (swordTransform != null)
                {
                    // Pedang sedikit terjatuh/lunglai saat terhentak
                    swordTransform.localRotation = Quaternion.Euler(0f, 0f, initialSwordAngle - 18f * (1f - t));
                }

                yield return null;
            }

            // Pulihkan warna asli & scale
            if (bodyRenderer != null) bodyRenderer.color = originalBodyColor;
            if (swordRenderer != null) swordRenderer.color = originalSwordColor;
            if (visualTransform != null) visualTransform.localScale = new Vector3(origScale.x * facingDirection, origScale.y, 1f);

            activeSequenceRoutine = null;
            ResetToIdlePose();
        }

        #endregion

        #region Animasi Defeated

        /// <summary>
        /// Memicu urutan kekalahan Enemy:
        /// Bounce/spin/squash, defeat SFX, disappear, callback selebrasi Player.
        /// </summary>
        public void TriggerDefeat(Action onDefeatComplete)
        {
            if (currentState == AnimState.Defeated) return;

            if (activeSequenceRoutine != null)
            {
                StopCoroutine(activeSequenceRoutine);
                activeSequenceRoutine = null;
            }

            if (slashFxTransform != null) slashFxTransform.gameObject.SetActive(false);

            activeSequenceRoutine = StartCoroutine(DefeatRoutine(onDefeatComplete));
        }

        private IEnumerator DefeatRoutine(Action onDefeatComplete)
        {
            currentState = AnimState.Defeated;

            // Mainkan suara Enemy Defeated satu kali
            PlayEnemySFX(AudioManager.SFXType.EnemyDefeated, enemyDefeatedClip, 1.0f);

            float elapsed = 0f;
            Vector3 startPos = visualTransform.localPosition;
            Vector3 startScale = visualTransform.localScale;

            while (elapsed < defeatDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / defeatDuration;

                // Pop bounce naik lalu jatuh
                float heightOffset = Mathf.Sin(t * Mathf.PI) * 0.25f;
                if (visualTransform != null)
                {
                    visualTransform.localPosition = startPos + new Vector3(0f, heightOffset, 0f);
                    // Putaran sukacita/kalah
                    visualTransform.localRotation = Quaternion.Euler(0f, 0f, 360f * t * facingDirection);
                    // Shrink scale
                    visualTransform.localScale = Vector3.Lerp(startScale, Vector3.zero, t * t);
                }

                // Pedang berputar terlepas
                if (swordTransform != null)
                {
                    swordTransform.localRotation = Quaternion.Euler(0f, 0f, initialSwordAngle + 720f * t);
                    swordTransform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
                }

                // Fade alpha
                float alpha = Mathf.Lerp(1f, 0f, t);
                if (bodyRenderer != null)
                {
                    Color c = originalBodyColor;
                    c.a = alpha;
                    bodyRenderer.color = c;
                }
                if (swordRenderer != null)
                {
                    Color c = originalSwordColor;
                    c.a = alpha;
                    swordRenderer.color = c;
                }

                yield return null;
            }

            onDefeatComplete?.Invoke();
        }

        #endregion

        #region Helper & Audio

        public void ResetToIdlePose()
        {
            if (currentState == AnimState.Defeated) return;

            currentState = isMoving ? AnimState.Walk : AnimState.Idle;

            if (visualTransform != null)
            {
                visualTransform.localPosition = initialVisualPos;
                visualTransform.localRotation = Quaternion.identity;
                visualTransform.localScale = new Vector3(baseScale.x * facingDirection, baseScale.y, 1f);
            }

            if (swordTransform != null)
            {
                swordTransform.localPosition = initialSwordPos;
                swordTransform.localRotation = Quaternion.Euler(0f, 0f, initialSwordAngle);
            }

            if (bodyRenderer != null) bodyRenderer.color = originalBodyColor;
            if (swordRenderer != null) swordRenderer.color = originalSwordColor;
            if (slashFxTransform != null) slashFxTransform.gameObject.SetActive(false);
        }

        private void PlayEnemySFX(AudioManager.SFXType sfxType, AudioClip fallbackClip, float volume = 0.8f)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(sfxType, volume);
            }
            else if (localAudioSource != null && fallbackClip != null)
            {
                localAudioSource.PlayOneShot(fallbackClip, volume);
            }
        }

        #endregion
    }
}
