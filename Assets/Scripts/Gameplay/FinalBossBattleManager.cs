using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using TMPro;
using MazeSpell.Audio;

namespace MazeSpell.Gameplay
{
    /// <summary>
    /// Koordinator pertarungan puncak Level 5 (Final Boss Battle):
    /// - Mengatur alur: EXPLORE -> COLLECT FLAME -> FLAME SPELL UNLOCKED -> ARENA TRANSITION
    ///   -> FINAL GUARDIAN -> BOSS PHASES (1, 2, 3) -> BOSS DEFEATED -> FINAL VICTORY.
    /// - Menjamin transformasi labirin menjadi FINAL ARENA terbuka yang aman tanpa dinding jebakan.
    /// - Sinkronisasi Boss HP (3 -> 2 -> 1 -> 0) dengan HUD, feedback visual, dan audio.
    /// - Menampilkan Panel Kemenangan Akhir MAZESPELL COMPLETE!
    /// </summary>
    public class FinalBossBattleManager : MonoBehaviour
    {
        private static FinalBossBattleManager _instance;
        public static FinalBossBattleManager Instance
        {
            get
            {
                if (_instance == null)
                {
#if UNITY_2023_1_OR_NEWER
                    _instance = FindAnyObjectByType<FinalBossBattleManager>();
#else
                    _instance = FindObjectOfType<FinalBossBattleManager>();
#endif
                }
                return _instance;
            }
            private set => _instance = value;
        }

        public enum BossBattleState
        {
            Inactive,
            SpellUnlocked,
            ArenaTransitioning,
            Phase1,
            Phase2,
            Phase3,
            DefeatSequence,
            VictoryModal
        }

        [Header("State Status")]
        [SerializeField] private BossBattleState currentState = BossBattleState.Inactive;
        [SerializeField] private bool isBossDefeated = false;

        public BossBattleState CurrentState => currentState;
        public bool IsBossBattleActive => currentState >= BossBattleState.Phase1 && currentState <= BossBattleState.Phase3;
        public bool IsBossDefeated => isBossDefeated;

        public void SetBossDefeated(bool defeated)
        {
            isBossDefeated = defeated;
        }

        private EnemyCharacter bossEnemy;
        private GameObject playerObj;
        private Coroutine bossRoutine;
        private GameObject victoryModalObj;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            Debug.Log("[BOSS]\nManager instance created");
        }

        private void Start()
        {
            HookEvents();
        }

        private void OnDestroy()
        {
            UnhookEvents();
            if (Instance == this) Instance = null;
        }

        public void HookEvents()
        {
            UnhookEvents();

            if (LevelProgressionManager.HasInstance && LevelProgressionManager.Instance.onLevelStarted != null)
            {
                LevelProgressionManager.Instance.onLevelStarted.AddListener(OnLevelStarted);
            }

            EnemyCharacter.OnEnemyHealthChanged += HandleEnemyHealthChanged;
            EnemyCharacter.OnEnemyDefeated += HandleEnemyDefeated;
        }

        public void UnhookEvents()
        {
            if (LevelProgressionManager.HasInstance && LevelProgressionManager.Instance.onLevelStarted != null)
            {
                LevelProgressionManager.Instance.onLevelStarted.RemoveListener(OnLevelStarted);
            }

            EnemyCharacter.OnEnemyHealthChanged -= HandleEnemyHealthChanged;
            EnemyCharacter.OnEnemyDefeated -= HandleEnemyDefeated;
        }

        private void OnLevelStarted(int level)
        {
            if (victoryModalObj != null) Destroy(victoryModalObj);
            currentState = BossBattleState.Inactive;
            isBossDefeated = false;
            bossEnemy = null;

            if (FinalGuardianHUD.Instance != null)
            {
                FinalGuardianHUD.Instance.HideHUD();
            }
        }

        /// <summary>
        /// Dipanggil saat seluruh huruf FLAME selesai dikumpulkan di Level 5.
        /// Memulai transisi dari maze normal menjadi FINAL ARENA.
        /// </summary>
        public void StartBossArenaTransition()
        {
            Debug.Log("[BOSS]\nLevel = 5");
            Debug.Log("[BOSS]\nTransition requested");

            if (currentState != BossBattleState.Inactive) return;
            currentState = BossBattleState.ArenaTransitioning;

            StartCoroutine(BossArenaTransitionRoutine());
        }

        private IEnumerator BossArenaTransitionRoutine()
        {
            Debug.Log("[FinalBossBattleManager] ★ MEMULAI TRANSISI FINAL ARENA & GUARDIAN BATTLE! ★");

            // 1. Dapatkan referensi Player & hentikan gerakan sesaat
            playerObj = GameObject.FindWithTag("Player");
            if (playerObj == null)
            {
#if UNITY_2023_1_OR_NEWER
                var pc = FindAnyObjectByType<PlayerController>();
#else
                var pc = FindObjectOfType<PlayerController>();
#endif
                if (pc != null) playerObj = pc.gameObject;
            }

            PlayerController playerCtrl = playerObj != null ? playerObj.GetComponent<PlayerController>() : null;
            if (playerCtrl != null)
            {
                playerCtrl.enabled = false;
                var rb = playerCtrl.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero;
            }

            yield return new WaitForSeconds(0.4f);

            // 2. Efek audio & kamera zoom
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.SFXType.WordComplete);
            }

            Camera mainCam = Camera.main;
            float initialCamSize = mainCam != null ? mainCam.orthographicSize : 7.0f;

            if (mainCam != null)
            {
                StartCoroutine(CameraZoomRoutine(mainCam, initialCamSize, 5.8f, 0.6f));
            }

            // 3. Screen flash hangat (api oranye)
            StartCoroutine(ScreenFlashRoutine(new Color(1f, 0.45f, 0.1f, 0.55f), 0.5f));

            yield return new WaitForSeconds(0.6f);

            // 4. Transformasi Labirin menjadi FINAL ARENA terbuka
            Debug.Log("[BOSS]\nTransformMazeToArena START");
            TransformMazeToArena();
            Debug.Log("[BOSS]\nTransformMazeToArena COMPLETE");

            // 5. Tempatkan Player di sisi kiri arena (4.5, 5.5) dan Boss di sisi kanan (16.5, 5.5)
            Vector3 playerArenaPos = new Vector3(4.5f, 5.5f, 0f);
            Vector3 bossArenaPos = new Vector3(16.5f, 5.5f, -0.1f);

            if (playerObj != null)
            {
                playerObj.transform.position = playerArenaPos;
                var pLives = PlayerLivesManager.Instance;
                if (pLives != null)
                {
                    pLives.SetRespawnPosition(playerArenaPos);
                }
                Debug.Log("[BOSS]\nPlayer moved to arena");
            }

            // Dapatkan atau aktifkan Enemy sebagai FINAL GUARDIAN (Tepat 1 Entity)
#if UNITY_2023_1_OR_NEWER
            var allEnemies = FindObjectsByType<EnemyCharacter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
            var allEnemies = FindObjectsOfType<EnemyCharacter>();
#endif
            if (allEnemies != null && allEnemies.Length > 0)
            {
                bossEnemy = allEnemies[0];
                for (int i = 1; i < allEnemies.Length; i++)
                {
                    if (allEnemies[i] != null && allEnemies[i].gameObject != null)
                    {
                        Destroy(allEnemies[i].gameObject);
                    }
                }
            }

            if (bossEnemy == null)
            {
                GameObject bossGo = new GameObject("FinalGuardian");
                bossGo.transform.position = bossArenaPos;
                var sr = bossGo.AddComponent<SpriteRenderer>();
                Sprite enemySprite = null;
#if UNITY_EDITOR
                enemySprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Enemy.png");
#endif
                if (enemySprite == null) enemySprite = Resources.Load<Sprite>("Sprites/Enemy");
                sr.sprite = enemySprite;
                sr.sortingOrder = 4;
                var circleCol = bossGo.AddComponent<CircleCollider2D>();
                circleCol.isTrigger = true;
                circleCol.radius = 0.38f;
                bossEnemy = bossGo.AddComponent<EnemyCharacter>();
            }

            // Matikan goal trigger labirin biasa jika ada agar tidak bentrok
            GameObject goalObj = GameObject.Find("Goal") ?? GameObject.FindWithTag("Finish");
            if (goalObj != null)
            {
                goalObj.SetActive(false);
            }

            if (bossEnemy != null)
            {
                bossEnemy.transform.position = bossArenaPos;
                bossEnemy.SetBasePosition(bossArenaPos);
                bossEnemy.ClearTilemapCache();
                bossEnemy.transform.localScale = Vector3.one * 1.35f; // Visual lebih megah
                bossEnemy.IsBoss = true;
                bossEnemy.SetLevel(5);
                bossEnemy.SetBossStats(2.7f, 0.90f, 0.9f);
                bossEnemy.SetCurrentHealth(3);
                bossEnemy.gameObject.SetActive(true);
                bossEnemy.enabled = true;

                // Pasang Aura Api Boss Phase 1
                ElementalParticleSpawner.SpawnBossAura(bossEnemy.transform, 1);
                Debug.Log("[BOSS]\nBoss moved to arena");

                // STEP 5: Log enemy instance
                Debug.Log($"[BOSS]\nEnemy instance:\nname = {bossEnemy.name}\nposition = {bossEnemy.transform.position}\nactive = {bossEnemy.gameObject.activeInHierarchy}\nhealth = {bossEnemy.CurrentHealth}\nisBoss = {bossEnemy.IsBoss}");
            }

            // 6. Reset kamera kembali ke mode follow pemain di arena
            if (mainCam != null)
            {
                StartCoroutine(CameraZoomRoutine(mainCam, 5.8f, initialCamSize, 0.5f));
            }

            // 7. Tampilkan HUD Boss di bagian atas layar
            EnsureHUD();
            if (FinalGuardianHUD.Instance != null)
            {
                FinalGuardianHUD.Instance.ShowHUD(3);
            }

            // 8. Tampilkan pesan pengantar pertarungan
            FloatingTextHelper.ShowFloatingText(playerArenaPos + Vector3.up * 1.5f, "⚔ DEFEAT THE FINAL GUARDIAN ⚔", new Color(1f, 0.85f, 0.3f, 1f), 38);

            yield return new WaitForSeconds(0.6f);

            // 9. Aktifkan kembali kontrol Player
            if (playerCtrl != null)
            {
                playerCtrl.enabled = true;
            }

            currentState = BossBattleState.Phase1;
            Debug.Log("[BOSS]\nBoss Battle ACTIVE");
            Debug.Log("[FinalBossBattleManager] ✓ ARENA SIAP! Memasuki PHASE 1 (Boss HP: 3/3, SPD: 2.7)");

            StartCoroutine(BossMovementMonitorRoutine());
        }

        private IEnumerator BossMovementMonitorRoutine()
        {
            float t = 0f;
            while (IsBossBattleActive && bossEnemy != null && !bossEnemy.IsDefeated)
            {
                Vector3 bPos = bossEnemy.transform.position;
                Vector3 pPos = playerObj != null ? playerObj.transform.position : Vector3.zero;
                float dist = Vector2.Distance(bPos, pPos);
                Debug.Log($"[BOSS MOVEMENT]\nt={t:F1} position={bPos} distanceToPlayer={dist:F2}");
                yield return new WaitForSeconds(0.5f);
                t += 0.5f;
            }
        }

        /// <summary>
        /// Mengubah area tengah labirin Level 5 menjadi Arena Terbuka berukuran 15x7 petak walkable,
        /// dikelilingi dinding lava pelindung agar Player dan Boss tidak bisa keluar.
        /// </summary>
        public void TransformMazeToArena()
        {
            GameObject gridObj = GameObject.Find("Level5_MazeGrid");
            if (gridObj == null)
            {
                Debug.LogWarning("[FinalBossBattleManager] Level5_MazeGrid tidak ditemukan!");
                return;
            }

            Transform wallsTr = gridObj.transform.Find("Walls_Obstacles");
            Transform pathTr = gridObj.transform.Find("Background_Path");
            if (wallsTr == null || pathTr == null)
            {
                Debug.LogWarning("[FinalBossBattleManager] Tilemap Walls_Obstacles atau Background_Path tidak ditemukan!");
                return;
            }

            Tilemap wallsMap = wallsTr.GetComponent<Tilemap>();
            Tilemap pathMap = pathTr.GetComponent<Tilemap>();
            if (wallsMap == null || pathMap == null) return;

            // Ambil sampel TileBase asli dari scene
            TileBase wallTile = wallsMap.GetTile(new Vector3Int(0, 0, 0));
            TileBase pathTile = pathMap.GetTile(new Vector3Int(2, 20, 0));

            // Jika wallTile null, cari tile dinding terdekat
            if (wallTile == null)
            {
                for (int x = 0; x < 45; x++)
                {
                    wallTile = wallsMap.GetTile(new Vector3Int(x, 22, 0));
                    if (wallTile != null) break;
                }
            }

            // Batas Arena Terbuka:
            // Area Dalam (Walkable Path): Kolom 2 s/d 18, Baris 1 s/d 9 (17 x 9 petak terbuka!)
            int minX = 2;
            int maxX = 18;
            int minY = 1;
            int maxY = 9;

            // Bersihkan dinding dalam arena & lapisi dengan path stone
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector3Int pos = new Vector3Int(x, y, 0);
                    wallsMap.SetTile(pos, null); // Bersihkan dinding rintangan
                    if (pathTile != null)
                    {
                        pathMap.SetTile(pos, pathTile);
                    }
                }
            }

            // Bangun Dinding Pembatas Arena (Keliling rapat tanpa celah)
            // Dinding Bawah (Y = 2) dan Dinding Atas (Y = 10)
            for (int x = minX - 1; x <= maxX + 1; x++)
            {
                if (wallTile != null)
                {
                    wallsMap.SetTile(new Vector3Int(x, minY - 1, 0), wallTile);
                    wallsMap.SetTile(new Vector3Int(x, maxY + 1, 0), wallTile);
                }
            }

            // Dinding Kiri (X = 3) dan Dinding Kanan (X = 19)
            for (int y = minY - 1; y <= maxY + 1; y++)
            {
                if (wallTile != null)
                {
                    wallsMap.SetTile(new Vector3Int(minX - 1, y, 0), wallTile);
                    wallsMap.SetTile(new Vector3Int(maxX + 1, y, 0), wallTile);
                }
            }

            // Segarkan render tilemap & collider
            wallsMap.RefreshAllTiles();
            pathMap.RefreshAllTiles();

            var col = wallsMap.GetComponent<TilemapCollider2D>();
            if (col != null)
            {
                col.ProcessTilemapChanges();
            }

            // Bersihkan hazard flame atau letter yang mungkin masih tersisa di arena
            Transform hazardsTr = gridObj.transform.Find("GameplayElements/Hazards");
            if (hazardsTr != null)
            {
                hazardsTr.gameObject.SetActive(false);
            }

            Debug.Log($"[FinalBossBattleManager] ✓ FINAL ARENA BERHASIL DIKONSTRUKSI! (Area: {minX}..{maxX} x {minY}..{maxY})");
        }

        public void EnsureHUD()
        {
            if (FinalGuardianHUD.Instance == null)
            {
                GameObject hudObj = new GameObject("FinalGuardianHUD");
                hudObj.AddComponent<FinalGuardianHUD>();
            }
        }

        private void HandleEnemyHealthChanged(EnemyCharacter enemy, int currentHp, int maxHp)
        {
            if (enemy == null || !enemy.IsBoss) return;

            Debug.Log($"[FinalBossBattleManager] ⚔ Boss HP Berubah: {currentHp}/{maxHp}");

            if (FinalGuardianHUD.Instance != null)
            {
                FinalGuardianHUD.Instance.UpdateHealth(currentHp, maxHp);
            }

            // PHASE 2 (HP: 2)
            if (currentHp == 2 && currentState == BossBattleState.Phase1)
            {
                currentState = BossBattleState.Phase2;
                enemy.SetBossStats(3.2f, 0.75f, 0.9f); // Lebih cepat & cooldown lebih gesit
                ElementalParticleSpawner.SpawnBossAura(enemy.transform, 2);

                if (FinalGuardianHUD.Instance != null)
                {
                    FinalGuardianHUD.Instance.SetPhaseText("🔥 PHASE 2: ENRAGED 🔥", new Color(1f, 0.45f, 0.1f));
                }

                FloatingTextHelper.ShowFloatingText(enemy.transform.position + Vector3.up * 1.2f, "🔥 PHASE 2: ENRAGED 🔥", new Color(1f, 0.5f, 0.15f), 32);

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(AudioManager.SFXType.EnemyHurt, 1f);
                }
            }
            // PHASE 3 - FINAL PHASE (HP: 1)
            else if (currentHp == 1 && (currentState == BossBattleState.Phase1 || currentState == BossBattleState.Phase2))
            {
                currentState = BossBattleState.Phase3;
                enemy.SetBossStats(3.7f, 0.60f, 0.9f); // Kecepatan maksimal, sangat agresif tapi tetap adil
                ElementalParticleSpawner.SpawnBossAura(enemy.transform, 3);

                if (FinalGuardianHUD.Instance != null)
                {
                    FinalGuardianHUD.Instance.SetPhaseText("🔥 FINAL PHASE: FRENZY 🔥", new Color(1f, 0.2f, 0.05f));
                }

                FloatingTextHelper.ShowFloatingText(enemy.transform.position + Vector3.up * 1.2f, "🔥 FINAL PHASE: FRENZY 🔥", new Color(1f, 0.25f, 0.1f), 34);

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(AudioManager.SFXType.EnemyHurt, 1.2f);
                }
            }
        }

        private void HandleEnemyDefeated(EnemyCharacter enemy)
        {
            if (enemy == null || !enemy.IsBoss) return;
            if (currentState == BossBattleState.DefeatSequence || currentState == BossBattleState.VictoryModal) return;

            currentState = BossBattleState.DefeatSequence;
            isBossDefeated = true;

            StartCoroutine(BossDefeatSequenceRoutine(enemy));
        }

        private IEnumerator BossDefeatSequenceRoutine(EnemyCharacter enemy)
        {
            Debug.Log("[FinalBossBattleManager] ★★★ FINAL GUARDIAN TELAH DIKALAHKAN! MEMULAI DEFEAT SEQUENCE ★★★");

            // 1. Freeze boss sebentar
            enemy.SetStagger(1.5f);

            // 2. Screen Flash & Camera Shake
            StartCoroutine(ScreenFlashRoutine(new Color(1f, 0.9f, 0.6f, 0.65f), 0.4f));
            StartCoroutine(CameraShakeRoutine(0.4f, 0.22f));

            // 3. Ledakan partikel api dahsyat
            ElementalParticleSpawner.SpawnFlameDefeatExplosion(enemy.transform.position);

            // 4. Audio kemenangan
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.SFXType.GameComplete);
            }

            // 5. Selebrasi Player
            if (playerObj != null)
            {
                var anim = playerObj.GetComponentInChildren<PlayerVisualAnimator>();
                if (anim != null) anim.TriggerCelebration();
            }

            // Sembunyikan HUD Boss
            if (FinalGuardianHUD.Instance != null)
            {
                FinalGuardianHUD.Instance.HideHUD();
            }

            // 6. Tampilkan teks pengumuman
            Vector3 textPos = enemy.transform.position + Vector3.up * 1.0f;
            FloatingTextHelper.ShowFloatingText(textPos, "★ FINAL GUARDIAN DEFEATED! ★", new Color(1f, 0.85f, 0.2f), 38);

            yield return new WaitForSeconds(1.8f);

            FloatingTextHelper.ShowFloatingText(textPos, "★ FLAME SPELL COMPLETE! ★", new Color(1f, 0.5f, 0.15f), 38);

            yield return new WaitForSeconds(1.4f);

            // Munculkan Gerbang Kemenangan Spesial Level 5 (Victory Gate)
            ExitGateVisual.TriggerLevel5VictoryGate(enemy.transform.position + Vector3.up * 1.5f);

            // 7. Tampilkan PANEL KEMENANGAN AKHIR
            ShowFinalVictoryModal();
        }

        /// <summary>
        /// Membangun dan menampilkan Panel Kemenangan Akhir (Fitur 11/12/Final Victory):
        /// "MAZESPELL COMPLETE! THE WORD LABYRINTH HAS BEEN RESTORED"
        /// </summary>
        public void ShowFinalVictoryModal()
        {
            if (victoryModalObj != null) Destroy(victoryModalObj);
            currentState = BossBattleState.VictoryModal;

            Canvas targetCanvas = null;
#if UNITY_2023_1_OR_NEWER
            targetCanvas = FindAnyObjectByType<Canvas>();
#else
            targetCanvas = FindObjectOfType<Canvas>();
#endif
            if (targetCanvas == null)
            {
                GameObject cGo = new GameObject("VictoryCanvas");
                targetCanvas = cGo.AddComponent<Canvas>();
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                targetCanvas.sortingOrder = 50;
                CanvasScaler cs = cGo.AddComponent<CanvasScaler>();
                cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                cs.referenceResolution = new Vector2(1920, 1080);
                cGo.AddComponent<GraphicRaycaster>();
            }

            victoryModalObj = new GameObject("FinalVictoryModal");
            victoryModalObj.transform.SetParent(targetCanvas.transform, false);

            RectTransform rootRect = victoryModalObj.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            // Overlay gelap lembut
            Image darkOverlay = victoryModalObj.AddComponent<Image>();
            darkOverlay.color = new Color(0.04f, 0.06f, 0.10f, 0.88f);

            // Modal Box Card
            GameObject cardObj = new GameObject("ModalCard");
            cardObj.transform.SetParent(victoryModalObj.transform, false);
            RectTransform cardRect = cardObj.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(760f, 520f);
            cardRect.anchoredPosition = Vector2.zero;

            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.color = new Color(0.09f, 0.12f, 0.20f, 0.98f);

            // Golden Glow Border
            GameObject borderObj = new GameObject("GoldenBorder");
            borderObj.transform.SetParent(cardObj.transform, false);
            RectTransform borderRect = borderObj.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = new Vector2(-4f, -4f);
            borderRect.offsetMax = new Vector2(4f, 4f);
            Image borderImg = borderObj.AddComponent<Image>();
            borderImg.color = new Color(1.0f, 0.55f, 0.15f, 0.9f);
            borderObj.transform.SetAsFirstSibling();

            // Main Title
            GameObject titleObj = new GameObject("MainTitle");
            titleObj.transform.SetParent(cardObj.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.76f);
            titleRect.anchorMax = new Vector2(1f, 0.96f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "★ MAZESPELL COMPLETE! ★";
            titleTmp.fontSize = 46;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(1.0f, 0.88f, 0.35f, 1f);

            // Subtitle
            GameObject subObj = new GameObject("Subtitle");
            subObj.transform.SetParent(cardObj.transform, false);
            RectTransform subRect = subObj.AddComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.05f, 0.64f);
            subRect.anchorMax = new Vector2(0.95f, 0.76f);
            subRect.offsetMin = Vector2.zero;
            subRect.offsetMax = Vector2.zero;

            TextMeshProUGUI subTmp = subObj.AddComponent<TextMeshProUGUI>();
            subTmp.text = "THE WORD LABYRINTH HAS BEEN RESTORED";
            subTmp.fontSize = 24;
            subTmp.fontStyle = FontStyles.Bold;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.color = new Color(0.9f, 0.95f, 1.0f, 0.9f);

            // Stars Display
            int starsEarned = StarRatingManager.CalculateLevelStars();
            StarRatingManager.SaveLevelStars(5, starsEarned);

            GameObject starsObj = new GameObject("StarsDisplay");
            starsObj.transform.SetParent(cardObj.transform, false);
            RectTransform starsRect = starsObj.AddComponent<RectTransform>();
            starsRect.anchorMin = new Vector2(0f, 0.44f);
            starsRect.anchorMax = new Vector2(1f, 0.62f);
            starsRect.offsetMin = Vector2.zero;
            starsRect.offsetMax = Vector2.zero;

            TextMeshProUGUI starsTmp = starsObj.AddComponent<TextMeshProUGUI>();
            starsTmp.text = "★ ★ ★ ★ ★";
            starsTmp.fontSize = 52;
            starsTmp.alignment = TextAlignmentOptions.Center;
            starsTmp.color = new Color(1.0f, 0.82f, 0.2f, 1f);

            // Rating Detail
            GameObject ratingDetailObj = new GameObject("RatingDetail");
            ratingDetailObj.transform.SetParent(cardObj.transform, false);
            RectTransform detailRect = ratingDetailObj.AddComponent<RectTransform>();
            detailRect.anchorMin = new Vector2(0f, 0.30f);
            detailRect.anchorMax = new Vector2(1f, 0.42f);
            detailRect.offsetMin = Vector2.zero;
            detailRect.offsetMax = Vector2.zero;

            TextMeshProUGUI detailTmp = ratingDetailObj.AddComponent<TextMeshProUGUI>();
            int damageTaken = PlayerLivesManager.Instance != null ? PlayerLivesManager.Instance.DamageTakenThisLevel : 0;
            detailTmp.text = $"Level 5 Rating: {StarRatingManager.FormatStars(starsEarned)}  •  Damage Taken: {damageTaken}";
            detailTmp.fontSize = 20;
            detailTmp.alignment = TextAlignmentOptions.Center;
            detailTmp.color = new Color(0.75f, 0.85f, 0.95f, 0.85f);

            // Buttons Container
            GameObject btnRow = new GameObject("ButtonRow");
            btnRow.transform.SetParent(cardObj.transform, false);
            RectTransform rowRect = btnRow.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.5f, 0.12f);
            rowRect.anchorMax = new Vector2(0.5f, 0.12f);
            rowRect.sizeDelta = new Vector2(500f, 65f);
            rowRect.anchoredPosition = Vector2.zero;

            // 1. Button Main Menu
            CreateButton(btnRow.transform, "BtnMainMenu", new Vector2(-130f, 0f), "MAIN MENU", new Color(0.2f, 0.55f, 0.9f), () =>
            {
                SceneManager.LoadScene("MainMenu");
            });

            // 2. Button Level Select / Continue
            CreateButton(btnRow.transform, "BtnLevelSelect", new Vector2(130f, 0f), "LEVEL SELECT", new Color(0.12f, 0.75f, 0.45f), () =>
            {
                SceneManager.LoadScene("LevelSelect");
            });

            // Confetti partikel perayaan
            ElementalParticleSpawner.SpawnVictoryConfetti(Vector3.zero);

            // Animasi pop-in kartu
            StartCoroutine(AnimatePopIn(cardRect));
        }

        private void CreateButton(Transform parent, string name, Vector2 pos, string label, Color bgCol, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            RectTransform r = btnObj.AddComponent<RectTransform>();
            r.anchoredPosition = pos;
            r.sizeDelta = new Vector2(210f, 56f);

            Image img = btnObj.AddComponent<Image>();
            img.color = bgCol;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform tr = txtObj.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 22;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }

        private IEnumerator AnimatePopIn(RectTransform target)
        {
            if (target == null) yield break;
            target.localScale = Vector3.one * 0.4f;
            float elapsed = 0f;
            float duration = 0.45f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float s = Mathf.Lerp(0.4f, 1.0f, Mathf.Sin(t * Mathf.PI * 0.5f));
                target.localScale = Vector3.one * s;
                yield return null;
            }
            target.localScale = Vector3.one;
        }

        private IEnumerator ScreenFlashRoutine(Color flashColor, float duration)
        {
            Canvas canvas = null;
#if UNITY_2023_1_OR_NEWER
            canvas = FindAnyObjectByType<Canvas>();
#else
            canvas = FindObjectOfType<Canvas>();
#endif
            if (canvas == null) yield break;

            GameObject flashGo = new GameObject("ScreenFlash");
            flashGo.transform.SetParent(canvas.transform, false);
            RectTransform r = flashGo.AddComponent<RectTransform>();
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;

            Image img = flashGo.AddComponent<Image>();
            img.color = flashColor;
            img.raycastTarget = false;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(flashColor.a, 0f, elapsed / duration);
                img.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);
                yield return null;
            }

            Destroy(flashGo);
        }

        private IEnumerator CameraZoomRoutine(Camera cam, float fromSize, float toSize, float duration)
        {
            if (cam == null) yield break;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                cam.orthographicSize = Mathf.Lerp(fromSize, toSize, elapsed / duration);
                yield return null;
            }
            cam.orthographicSize = toSize;
        }

        private IEnumerator CameraShakeRoutine(float duration, float magnitude)
        {
            Camera cam = Camera.main;
            if (cam == null) yield break;
            Vector3 originalPos = cam.transform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float damper = 1f - (elapsed / duration);
                float x = (UnityEngine.Random.value * 2f - 1f) * magnitude * damper;
                float y = (UnityEngine.Random.value * 2f - 1f) * magnitude * damper;
                cam.transform.position = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
                yield return null;
            }
            cam.transform.position = originalPos;
        }
    }
}
