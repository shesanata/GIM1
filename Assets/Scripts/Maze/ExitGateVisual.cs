using System;
using System.Collections;
using UnityEngine;
using TMPro;
using MazeSpell.Audio;

namespace MazeSpell
{
    /// <summary>
    /// Visual dan Animasi Gerbang Pintu Keluar (Exit Gate) dalam World Space.
    /// Menggantikan label teks sederhana dengan gerbang cartoon adventure bertema:
    /// - Level 1 (EARTH): Stone/earth gate, batu loam, sedikit moss, warm magical glow (amber/emas).
    /// - Level 2 (STONE): Ancient stone gate, ukiran batu, rune sederhana, blue magical glow.
    /// - Level 3 (GRASS): Ancient gate tertutup tanaman, daun/vines, green emerald glow.
    /// - Level 4 (FROST): Ice gate, kristal es, cyan/white magical glow & frost motes.
    /// - Level 5 (FLAME): Volcanic dark basalt gate, retakan magma, bara api & flame glow megah.
    /// Memiliki status Locked, Unlocking, Unlocked, Opening, dan Victory Gate (Level 5 post-boss).
    /// Sepenuhnya berada di World Space, terintegrasi dengan LevelGoalTrigger yang sudah ada.
    /// </summary>
    [SelectionBase]
    public class ExitGateVisual : MonoBehaviour
    {
        public enum GateState
        {
            Locked,
            Unlocking,
            Unlocked,
            Opening,
            Opened,
            VictoryGate
        }

        [Header("State & Level")]
        [SerializeField] private int levelNumber = 1;
        [SerializeField] private GateState currentState = GateState.Locked;

        [Header("Proximity Settings")]
        [SerializeField] private float proximityDistance = 3.2f;
        [SerializeField] private float promptFadeSpeed = 6f;

        // Visual Child References
        private Transform visualRoot;
        private Transform frameObj;
        private Transform leftDoorObj;
        private Transform rightDoorObj;
        private Transform portalObj;
        private Transform lockObj;
        private Transform promptObj;
        private Transform shadowObj;
        private Transform keystoneObj;

        private SpriteRenderer portalSr;
        private SpriteRenderer lockSr;
        private SpriteRenderer promptBgSr;
        private TextMeshPro promptTmp;
        private ParticleSystem ambientParticles;
        private ParticleSystem burstParticles;

        // Dynamic State Variables
        private Transform playerTransform;
        private float currentPromptAlpha = 0f;
        private float pulseTimer = 0f;
        private bool isShaking = false;
        private bool isInitialized = false;

        // Door Sliding Positions (Local X)
        private const float DoorClosedLeftX = -0.38f;
        private const float DoorClosedRightX = 0.38f;
        private const float DoorOpenLeftX = -1.15f;
        private const float DoorOpenRightX = 1.15f;

        // Theme Palette Definition
        public struct GateTheme
        {
            public string name;
            public Color stonePrimary;
            public Color stoneShadow;
            public Color stoneHighlight;
            public Color portalCore;
            public Color portalGlow;
            public Color runeColor;
            public Color accentDetail;
            public string runeSymbol;
        }

        private GateTheme theme;

        public GateState CurrentState => currentState;
        public int LevelNumber => levelNumber;

        private void Awake()
        {
            if (!isInitialized)
            {
                Initialize(levelNumber);
            }
        }

        private void Start()
        {
            FindPlayerReference();
            UpdateInitialLockState();
        }

        /// <summary>
        /// Menginisialisasi visual gerbang sesuai nomor level (1 - 5).
        /// </summary>
        public void Initialize(int level)
        {
            levelNumber = Mathf.Clamp(level, 1, 5);
            SetupThemeData();
            BuildGateHierarchy();
            isInitialized = true;

            // Pastikan trigger collider pada objek induk berukuran pas dengan gerbang
            BoxCollider2D boxCol = GetComponent<BoxCollider2D>();
            if (boxCol != null)
            {
                boxCol.isTrigger = true;
                boxCol.size = new Vector2(1.5f, 1.6f);
                boxCol.offset = new Vector2(0f, 0.05f);
            }

            UpdateVisualsForCurrentState(true);
        }

        private void SetupThemeData()
        {
            switch (levelNumber)
            {
                case 1: // EARTH
                    theme = new GateTheme
                    {
                        name = "EARTH",
                        stonePrimary = new Color(0.55f, 0.42f, 0.30f),     // Warm Earth Loam
                        stoneShadow = new Color(0.36f, 0.26f, 0.18f),      // Dark Loam
                        stoneHighlight = new Color(0.72f, 0.58f, 0.44f),   // Sunlit Stone
                        portalCore = new Color(1.00f, 0.92f, 0.65f),       // Warm Golden Core
                        portalGlow = new Color(0.96f, 0.62f, 0.10f),       // Radiant Amber
                        runeColor = new Color(0.98f, 0.75f, 0.20f),        // Amber Rune
                        accentDetail = new Color(0.42f, 0.68f, 0.22f),     // Mossy green foliage
                        runeSymbol = "ᛖ"
                    };
                    break;

                case 2: // STONE
                    theme = new GateTheme
                    {
                        name = "STONE",
                        stonePrimary = new Color(0.45f, 0.52f, 0.60f),     // Ancient Granite
                        stoneShadow = new Color(0.28f, 0.33f, 0.40f),      // Deep Slate
                        stoneHighlight = new Color(0.65f, 0.72f, 0.80f),   // Chiseled Ridge
                        portalCore = new Color(0.85f, 0.95f, 1.00f),       // Mystic White-Blue
                        portalGlow = new Color(0.22f, 0.68f, 0.98f),       // Mystic Azure
                        runeColor = new Color(0.38f, 0.75f, 1.00f),        // Azure Rune
                        accentDetail = new Color(0.18f, 0.48f, 0.82f),     // Carved Rune Lines
                        runeSymbol = "ᚱ"
                    };
                    break;

                case 3: // GRASS
                    theme = new GateTheme
                    {
                        name = "GRASS",
                        stonePrimary = new Color(0.35f, 0.48f, 0.32f),     // Mossy Forest Stone
                        stoneShadow = new Color(0.20f, 0.30f, 0.18f),      // Deep Forest Green
                        stoneHighlight = new Color(0.52f, 0.68f, 0.48f),   // Soft Leaf Highlight
                        portalCore = new Color(0.80f, 1.00f, 0.85f),       // Spring Light Core
                        portalGlow = new Color(0.10f, 0.78f, 0.45f),       // Emerald Glow
                        runeColor = new Color(0.25f, 0.95f, 0.55f),        // Emerald Rune
                        accentDetail = new Color(0.30f, 0.85f, 0.35f),     // Climbing Ivy Vines
                        runeSymbol = "🌿"
                    };
                    break;

                case 4: // FROST
                    theme = new GateTheme
                    {
                        name = "FROST",
                        stonePrimary = new Color(0.60f, 0.78f, 0.92f),     // Glacial Ice Stone
                        stoneShadow = new Color(0.35f, 0.52f, 0.70f),      // Deep Blue Ice
                        stoneHighlight = new Color(0.88f, 0.96f, 1.00f),   // Crisp Snow Rim
                        portalCore = new Color(0.92f, 0.98f, 1.00f),       // Diamond White Core
                        portalGlow = new Color(0.15f, 0.82f, 0.95f),       // Radiant Cyan
                        runeColor = new Color(0.40f, 0.92f, 1.00f),        // Cyan Frost Rune
                        accentDetail = new Color(0.75f, 0.92f, 1.00f),     // Hanging Icicles
                        runeSymbol = "❄"
                    };
                    break;

                case 5: // FLAME
                    theme = new GateTheme
                    {
                        name = "FLAME",
                        stonePrimary = new Color(0.22f, 0.20f, 0.25f),     // Volcanic Obsidian
                        stoneShadow = new Color(0.12f, 0.10f, 0.15f),      // Charcoal Shadow
                        stoneHighlight = new Color(0.45f, 0.35f, 0.40f),   // Dark Basalt Rim
                        portalCore = new Color(1.00f, 0.95f, 0.65f),       // Intense White-Hot Core
                        portalGlow = new Color(0.95f, 0.28f, 0.10f),       // Blazing Flame Crimson
                        runeColor = new Color(1.00f, 0.48f, 0.12f),        // Magma Orange Rune
                        accentDetail = new Color(0.92f, 0.20f, 0.10f),     // Glowing Lava Veins
                        runeSymbol = "🔥"
                    };
                    break;
            }
        }

        #region Hierarchy Construction

        private void BuildGateHierarchy()
        {
            // Hapus visual lama jika ada
            Transform existing = transform.Find("ExitGateVisual");
            if (existing != null)
            {
                if (Application.isPlaying) Destroy(existing.gameObject);
                else DestroyImmediate(existing.gameObject);
            }

            // Hapus teks TMP lama "Visual" jika ada di parent untuk mencegah tumpang tindih
            Transform oldTmpVisual = transform.Find("Visual");
            if (oldTmpVisual != null)
            {
                // Sembunyikan atau nonaktifkan teks legacy tanpa merusak referensi apapun
                var tmp = oldTmpVisual.GetComponent<TextMeshPro>();
                if (tmp != null) tmp.enabled = false;
            }

            GameObject rootGo = new GameObject("ExitGateVisual");
            rootGo.transform.SetParent(transform, false);
            rootGo.transform.localPosition = Vector3.zero;
            rootGo.transform.localRotation = Quaternion.identity;
            rootGo.transform.localScale = Vector3.one;
            visualRoot = rootGo.transform;

            // 1. Shadow di lantai
            shadowObj = CreateChild("Shadow", visualRoot, new Vector3(0f, -0.92f, 0f));
            SpriteRenderer shadowSr = shadowObj.gameObject.AddComponent<SpriteRenderer>();
            shadowSr.sprite = GenerateShadowSprite();
            shadowSr.sortingOrder = 1;
            shadowSr.color = new Color(0f, 0f, 0f, 0.45f);

            // 2. Portal Backing (Aperture / Glowing Center)
            portalObj = CreateChild("PortalBacking", visualRoot, new Vector3(0f, 0.05f, 0.02f));
            portalSr = portalObj.gameObject.AddComponent<SpriteRenderer>();
            portalSr.sprite = GeneratePortalSprite();
            portalSr.sortingOrder = 2;

            // 3. Sliding Door Panels (Left & Right)
            leftDoorObj = CreateChild("DoorPanel_Left", visualRoot, new Vector3(DoorClosedLeftX, 0.02f, -0.01f));
            SpriteRenderer leftDoorSr = leftDoorObj.gameObject.AddComponent<SpriteRenderer>();
            leftDoorSr.sprite = GenerateDoorPanelSprite(true);
            leftDoorSr.sortingOrder = 3;

            rightDoorObj = CreateChild("DoorPanel_Right", visualRoot, new Vector3(DoorClosedRightX, 0.02f, -0.01f));
            SpriteRenderer rightDoorSr = rightDoorObj.gameObject.AddComponent<SpriteRenderer>();
            rightDoorSr.sprite = GenerateDoorPanelSprite(false);
            rightDoorSr.sortingOrder = 3;

            // 4. Lock Seal / Center Medallion
            lockObj = CreateChild("LockSeal", visualRoot, new Vector3(0f, 0.08f, -0.02f));
            lockSr = lockObj.gameObject.AddComponent<SpriteRenderer>();
            lockSr.sprite = GenerateLockSprite();
            lockSr.sortingOrder = 4;

            // 5. Archway Frame (Stone Pillars, Lintel, Arch)
            frameObj = CreateChild("ArchwayFrame", visualRoot, new Vector3(0f, 0.05f, -0.03f));
            SpriteRenderer frameSr = frameObj.gameObject.AddComponent<SpriteRenderer>();
            frameSr.sprite = GenerateArchwaySprite();
            frameSr.sortingOrder = 5;

            // 6. Keystone Emblem / Elemental Rune on Top Arch
            GameObject keystoneGo = new GameObject("KeystoneEmblem");
            keystoneGo.transform.SetParent(visualRoot, false);
            keystoneGo.transform.localPosition = new Vector3(0f, 1.08f, -0.04f);
            TextMeshPro keystoneTmp = keystoneGo.AddComponent<TextMeshPro>();
            keystoneTmp.text = $"<b>{theme.runeSymbol}</b>";
            keystoneTmp.fontSize = 4.2f;
            keystoneTmp.alignment = TextAlignmentOptions.Center;
            keystoneTmp.color = theme.runeColor;
            keystoneTmp.sortingOrder = 6;
            keystoneTmp.rectTransform.sizeDelta = new Vector2(1.2f, 1.2f);
            keystoneObj = keystoneGo.transform;

            // 7. Ambient Particle System
            BuildParticleSystems();

            // 8. World Space Floating Proximity Indicator
            BuildProximityPrompt();
        }

        private Transform CreateChild(string name, Transform parent, Vector3 localPos)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return go.transform;
        }

        private void BuildProximityPrompt()
        {
            promptObj = CreateChild("ProximityPrompt", visualRoot, new Vector3(0f, 1.68f, -0.08f));

            // Background Ribbon Pill
            GameObject bgGo = new GameObject("PromptBg");
            bgGo.transform.SetParent(promptObj, false);
            bgGo.transform.localPosition = Vector3.zero;
            promptBgSr = bgGo.AddComponent<SpriteRenderer>();
            promptBgSr.sprite = GeneratePromptBadgeSprite();
            promptBgSr.sortingOrder = 10;
            promptBgSr.color = new Color(1f, 1f, 1f, 0f); // Awalnya transparan

            // Teks Indikator
            GameObject textGo = new GameObject("PromptText");
            textGo.transform.SetParent(promptObj, false);
            textGo.transform.localPosition = new Vector3(0f, 0.02f, -0.01f);
            promptTmp = textGo.AddComponent<TextMeshPro>();
            promptTmp.fontSize = 2.6f;
            promptTmp.fontStyle = FontStyles.Bold;
            promptTmp.alignment = TextAlignmentOptions.Center;
            promptTmp.sortingOrder = 11;
            promptTmp.color = new Color(1f, 1f, 1f, 0f);
            promptTmp.rectTransform.sizeDelta = new Vector2(2.8f, 0.8f);

            UpdatePromptText();
        }

        private void BuildParticleSystems()
        {
            // Ambient Particle System (Looping)
            GameObject ambGo = new GameObject("AmbientMotes");
            ambGo.transform.SetParent(visualRoot, false);
            ambGo.transform.localPosition = new Vector3(0f, -0.7f, 0.01f);
            ambientParticles = ambGo.AddComponent<ParticleSystem>();

            var main = ambientParticles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.duration = 2.0f;
            main.startLifetime = 1.6f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.24f);
            main.startColor = theme.portalGlow;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ambientParticles.emission;
            emission.rateOverTime = 8f;

            var shape = ambientParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(1.1f, 0.2f, 0.1f);

            var colorOverLife = ambientParticles.colorOverLifetime;
            colorOverLife.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(theme.portalCore, 0f),
                    new GradientColorKey(theme.portalGlow, 0.6f),
                    new GradientColorKey(theme.runeColor, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.85f, 0.25f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLife.color = grad;

            var velOverLife = ambientParticles.velocityOverLifetime;
            velOverLife.enabled = true;
            velOverLife.y = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);

            var psRenderer = ambientParticles.GetComponent<ParticleSystemRenderer>();
            psRenderer.sortingOrder = 4;

            // Burst Particle System (One-shot)
            GameObject burstGo = new GameObject("BurstParticles");
            burstGo.transform.SetParent(visualRoot, false);
            burstGo.transform.localPosition = new Vector3(0f, 0.1f, -0.05f);
            burstParticles = burstGo.AddComponent<ParticleSystem>();

            var bMain = burstParticles.main;
            bMain.loop = false;
            bMain.playOnAwake = false;
            bMain.duration = 1.0f;
            bMain.startLifetime = 0.9f;
            bMain.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.2f);
            bMain.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.35f);
            bMain.startColor = theme.portalCore;
            bMain.simulationSpace = ParticleSystemSimulationSpace.World;

            var bEmission = burstParticles.emission;
            bEmission.rateOverTime = 0;

            var bShape = burstParticles.shape;
            bShape.shapeType = ParticleSystemShapeType.Sphere;
            bShape.radius = 0.35f;

            var bColorOverLife = burstParticles.colorOverLifetime;
            bColorOverLife.enabled = true;
            bColorOverLife.color = grad;

            var bPsRenderer = burstParticles.GetComponent<ParticleSystemRenderer>();
            bPsRenderer.sortingOrder = 7;
        }

        #endregion

        #region Runtime Updates & Animations

        private void Update()
        {
            pulseTimer += Time.deltaTime;

            UpdateProximityDetection();
            UpdateVisualPulse();
        }

        private void FindPlayerReference()
        {
            if (playerTransform != null) return;

            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
            else
            {
                var pc = FindAnyObjectByType<PlayerController>();
                if (pc != null) playerTransform = pc.transform;
            }
        }

        private void UpdateInitialLockState()
        {
            bool isWordComplete = WordCollectionManager.Instance != null && WordCollectionManager.Instance.IsWordComplete;

            if (levelNumber == 5)
            {
                if (MazeSpell.Gameplay.FinalBossBattleManager.Instance != null &&
                    MazeSpell.Gameplay.FinalBossBattleManager.Instance.IsBossDefeated)
                {
                    SetState(GateState.VictoryGate);
                    return;
                }
            }

            if (isWordComplete)
            {
                SetState(GateState.Unlocked);
            }
            else
            {
                SetState(GateState.Locked);
            }
        }

        private void UpdateProximityDetection()
        {
            if (playerTransform == null)
            {
                FindPlayerReference();
                return;
            }

            float dist = Vector2.Distance(transform.position, playerTransform.position);
            bool isNear = dist <= proximityDistance;

            float targetAlpha = isNear ? Mathf.Clamp01(1f - ((dist - 1.2f) / (proximityDistance - 1.2f))) : 0f;
            currentPromptAlpha = Mathf.MoveTowards(currentPromptAlpha, targetAlpha, Time.deltaTime * promptFadeSpeed);

            // Terapkan alpha pada badge & teks
            if (promptBgSr != null)
            {
                Color c = promptBgSr.color;
                c.a = currentPromptAlpha * 0.92f;
                promptBgSr.color = c;
            }

            if (promptTmp != null)
            {
                Color tc = promptTmp.color;
                tc.a = currentPromptAlpha;
                promptTmp.color = tc;
            }

            // Bobbing pelan pada prompt jika terlihat
            if (promptObj != null && currentPromptAlpha > 0.02f)
            {
                float bobOffset = Mathf.Sin(pulseTimer * 3.5f) * 0.05f;
                promptObj.localPosition = new Vector3(0f, 1.68f + bobOffset, -0.08f);
            }
        }

        private void UpdateVisualPulse()
        {
            if (isShaking) return;

            float proximityBoost = (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= proximityDistance) ? 0.25f : 0f;

            switch (currentState)
            {
                case GateState.Locked:
                    // Subtle breathing pulse pada gembok
                    if (lockObj != null)
                    {
                        float s = 1.0f + Mathf.Sin(pulseTimer * 2f) * 0.03f;
                        lockObj.localScale = new Vector3(s, s, 1f);
                    }
                    if (portalSr != null)
                    {
                        portalSr.color = new Color(0.2f, 0.2f, 0.25f, 0.45f + Mathf.Sin(pulseTimer * 2f) * 0.08f + proximityBoost * 0.15f);
                    }
                    break;

                case GateState.Unlocked:
                    // Portal bersinar lembut & berdenyut
                    if (portalSr != null)
                    {
                        float alpha = 0.85f + Mathf.Sin(pulseTimer * 3.2f) * 0.15f + proximityBoost * 0.15f;
                        portalSr.color = new Color(theme.portalCore.r, theme.portalCore.g, theme.portalCore.b, Mathf.Clamp01(alpha));
                        float pScale = 1.0f + Mathf.Sin(pulseTimer * 2.8f) * 0.035f;
                        portalObj.localScale = new Vector3(pScale, pScale, 1f);
                    }
                    if (frameObj != null && proximityBoost > 0f)
                    {
                        float fScale = 1.0f + Mathf.Sin(pulseTimer * 4.5f) * 0.015f;
                        frameObj.localScale = new Vector3(fScale, fScale, 1f);
                    }
                    break;

                case GateState.VictoryGate:
                    // Denyut kemenangan Level 5 dahsyat
                    if (portalSr != null)
                    {
                        float alpha = 0.95f + Mathf.Sin(pulseTimer * 5.0f) * 0.05f;
                        portalSr.color = new Color(1f, 0.85f, 0.3f, alpha);
                        float pScale = 1.15f + Mathf.Sin(pulseTimer * 4.0f) * 0.06f;
                        portalObj.localScale = new Vector3(pScale, pScale, 1f);
                    }
                    break;
            }
        }

        private void UpdatePromptText()
        {
            if (promptTmp == null) return;

            switch (currentState)
            {
                case GateState.Locked:
                    promptTmp.text = "<color=#FCD34D>🔒 COLLECT ALL LETTERS</color>";
                    break;

                case GateState.Unlocked:
                    promptTmp.text = "<color=#6EE7B7>✨ ENTER EXIT ✨</color>";
                    break;

                case GateState.Opening:
                case GateState.Opened:
                    promptTmp.text = "<color=#A7F3D0>★ OPENING ★</color>";
                    break;

                case GateState.VictoryGate:
                    promptTmp.text = "<color=#FDE047>★ VICTORY PORTAL ★</color>";
                    break;
            }
        }

        private void UpdateVisualsForCurrentState(bool immediate)
        {
            UpdatePromptText();

            switch (currentState)
            {
                case GateState.Locked:
                    if (leftDoorObj != null) leftDoorObj.localPosition = new Vector3(DoorClosedLeftX, 0.02f, -0.01f);
                    if (rightDoorObj != null) rightDoorObj.localPosition = new Vector3(DoorClosedRightX, 0.02f, -0.01f);
                    if (lockObj != null)
                    {
                        lockObj.gameObject.SetActive(true);
                        lockObj.localScale = Vector3.one;
                    }
                    if (portalSr != null) portalSr.color = new Color(0.2f, 0.2f, 0.25f, 0.5f);
                    if (ambientParticles != null && ambientParticles.isPlaying) ambientParticles.Stop();
                    break;

                case GateState.Unlocked:
                    if (leftDoorObj != null) leftDoorObj.localPosition = new Vector3(DoorClosedLeftX, 0.02f, -0.01f);
                    if (rightDoorObj != null) rightDoorObj.localPosition = new Vector3(DoorClosedRightX, 0.02f, -0.01f);
                    if (lockObj != null) lockObj.gameObject.SetActive(false);
                    if (portalSr != null) portalSr.color = new Color(theme.portalCore.r, theme.portalCore.g, theme.portalCore.b, 0.9f);
                    if (ambientParticles != null && !ambientParticles.isPlaying) ambientParticles.Play();
                    break;

                case GateState.Opened:
                    if (leftDoorObj != null) leftDoorObj.localPosition = new Vector3(DoorOpenLeftX, 0.02f, -0.01f);
                    if (rightDoorObj != null) rightDoorObj.localPosition = new Vector3(DoorOpenRightX, 0.02f, -0.01f);
                    if (lockObj != null) lockObj.gameObject.SetActive(false);
                    if (portalSr != null) portalSr.color = new Color(theme.portalCore.r, theme.portalCore.g, theme.portalCore.b, 1.0f);
                    if (ambientParticles != null && !ambientParticles.isPlaying) ambientParticles.Play();
                    break;

                case GateState.VictoryGate:
                    visualRoot.localScale = Vector3.one * 1.25f;
                    if (leftDoorObj != null) leftDoorObj.localPosition = new Vector3(DoorOpenLeftX, 0.02f, -0.01f);
                    if (rightDoorObj != null) rightDoorObj.localPosition = new Vector3(DoorOpenRightX, 0.02f, -0.01f);
                    if (lockObj != null) lockObj.gameObject.SetActive(false);
                    if (portalSr != null) portalSr.color = new Color(1f, 0.85f, 0.3f, 1f);
                    if (ambientParticles != null && !ambientParticles.isPlaying) ambientParticles.Play();
                    break;
            }
        }

        #endregion

        #region State Transitions & Event Callbacks

        /// <summary>
        /// Dipanggil saat kata level berhasil diselesaikan untuk memicu animasi unlock gerbang.
        /// </summary>
        public void OnWordCompleted()
        {
            if (currentState == GateState.Locked)
            {
                StartCoroutine(PlayUnlockAnimationRoutine());
            }
        }

        /// <summary>
        /// Dipanggil saat pemain mencoba masuk ke pintu yang masih terkunci.
        /// Memberikan efek getar pintu (rattle shake), audio gembok terkunci, dan prompt peringatan.
        /// </summary>
        public void OnPlayerTouchLocked()
        {
            if (!isShaking && currentState == GateState.Locked)
            {
                StartCoroutine(PlayLockedShakeRoutine());
            }

            // Audio locked
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.SFXType.GoalLocked, 0.9f);
            }
        }

        private IEnumerator PlayLockedShakeRoutine()
        {
            isShaking = true;
            Vector3 origL = leftDoorObj != null ? leftDoorObj.localPosition : Vector3.zero;
            Vector3 origR = rightDoorObj != null ? rightDoorObj.localPosition : Vector3.zero;

            float dur = 0.28f;
            float el = 0f;
            while (el < dur)
            {
                el += Time.deltaTime;
                float shake = Mathf.Sin(el * 50f) * 0.05f * (1f - (el / dur));
                if (leftDoorObj != null) leftDoorObj.localPosition = origL + new Vector3(shake, 0f, 0f);
                if (rightDoorObj != null) rightDoorObj.localPosition = origR + new Vector3(-shake, 0f, 0f);
                if (lockObj != null) lockObj.localPosition = new Vector3(shake * 1.5f, 0.08f, -0.02f);
                yield return null;
            }

            if (leftDoorObj != null) leftDoorObj.localPosition = origL;
            if (rightDoorObj != null) rightDoorObj.localPosition = origR;
            if (lockObj != null) lockObj.localPosition = new Vector3(0f, 0.08f, -0.02f);
            isShaking = false;
        }

        /// <summary>
        /// Animasi unlock berdurasi 0.7 - 0.9 detik saat huruf lengkap.
        /// Gembok menyala, membesar, dissolve dengan percikan partikel, dan portal terbuka.
        /// </summary>
        public IEnumerator PlayUnlockAnimationRoutine()
        {
            currentState = GateState.Unlocking;
            UpdatePromptText();

            // 1. Audio Unlock
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.SFXType.GoalUnlocked, 1.0f);
            }

            // 2. Partikel burst
            if (burstParticles != null)
            {
                burstParticles.Emit(25);
            }

            // 3. Efek animasi gembok membesar lalu dissolve
            float duration = 0.75f;
            float elapsed = 0f;
            Vector3 origLockScale = lockObj != null ? lockObj.localScale : Vector3.one;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Gembok membesar lalu memudar
                if (lockObj != null)
                {
                    float s = Mathf.Lerp(1.0f, 1.45f, t);
                    lockObj.localScale = origLockScale * s;
                    if (lockSr != null)
                    {
                        Color lc = lockSr.color;
                        lc.a = Mathf.Lerp(1f, 0f, t);
                        lockSr.color = lc;
                    }
                }

                // Portal glow meningkat
                if (portalSr != null)
                {
                    float alpha = Mathf.Lerp(0.4f, 0.95f, t);
                    portalSr.color = new Color(theme.portalCore.r, theme.portalCore.g, theme.portalCore.b, alpha);
                }

                yield return null;
            }

            if (lockObj != null)
            {
                lockObj.gameObject.SetActive(false);
            }

            if (ambientParticles != null && !ambientParticles.isPlaying)
            {
                ambientParticles.Play();
            }

            currentState = GateState.Unlocked;
            UpdateVisualsForCurrentState(false);
        }

        /// <summary>
        /// Animasi pintu terbuka (0.9 detik) saat pemain memasuki trigger dalam kondisi huruf lengkap.
        /// Panel pintu meluncur ke kiri dan kanan, semburan cahaya memancar, audio berputar,
        /// lalu callback diteruskan ke LevelGoalTrigger / LevelCompletePopup.
        /// </summary>
        public IEnumerator PlayOpenAnimationRoutine(Action onAnimationFinished)
        {
            if (currentState == GateState.Opening || currentState == GateState.Opened)
            {
                onAnimationFinished?.Invoke();
                yield break;
            }

            currentState = GateState.Opening;
            UpdatePromptText();

            // 1. Audio Gerbang Terbuka
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.SFXType.GoalUnlocked, 1.1f);
                AudioManager.Instance.PlaySFX(AudioManager.SFXType.LevelTransition, 0.85f);
            }

            // 2. Light burst partikel
            if (burstParticles != null)
            {
                burstParticles.Emit(45);
            }

            // 3. Animasi sliding door panels
            float duration = 0.95f;
            float elapsed = 0f;

            Vector3 startL = leftDoorObj != null ? leftDoorObj.localPosition : new Vector3(DoorClosedLeftX, 0f, 0f);
            Vector3 startR = rightDoorObj != null ? rightDoorObj.localPosition : new Vector3(DoorClosedRightX, 0f, 0f);
            Vector3 targetL = new Vector3(DoorOpenLeftX, startL.y, startL.z);
            Vector3 targetR = new Vector3(DoorOpenRightX, startR.y, startR.z);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Smooth ease-out curve
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                if (leftDoorObj != null)
                {
                    leftDoorObj.localPosition = Vector3.Lerp(startL, targetL, smoothT);
                }

                if (rightDoorObj != null)
                {
                    rightDoorObj.localPosition = Vector3.Lerp(startR, targetR, smoothT);
                }

                // Portal radiance flare
                if (portalSr != null)
                {
                    float pScale = 1.0f + Mathf.Sin(smoothT * Mathf.PI) * 0.12f;
                    portalObj.localScale = new Vector3(pScale, pScale, 1f);
                }

                yield return null;
            }

            if (leftDoorObj != null) leftDoorObj.localPosition = targetL;
            if (rightDoorObj != null) rightDoorObj.localPosition = targetR;

            currentState = GateState.Opened;
            UpdatePromptText();

            // Tunggu sesaat untuk kepuasan visual sebelum melanjutkan flow level
            yield return new WaitForSeconds(0.2f);

            onAnimationFinished?.Invoke();
        }

        public void SetState(GateState newState)
        {
            currentState = newState;
            UpdateVisualsForCurrentState(true);
        }

        /// <summary>
        /// Mengubah gerbang menjadi Victory Gate megah di Level 5 (Post Final Guardian defeat).
        /// </summary>
        public void SetVictoryGateState()
        {
            SetState(GateState.VictoryGate);
            if (burstParticles != null) burstParticles.Emit(50);
            if (ambientParticles != null && !ambientParticles.isPlaying) ambientParticles.Play();
        }

        /// <summary>
        /// Helper statis untuk memicu Victory Gate Level 5 jika dibutuhkan dari luar (misal FinalBossBattleManager).
        /// </summary>
        public static void TriggerLevel5VictoryGate(Vector3 arenaPosition)
        {
            ExitGateVisual[] gates = FindObjectsByType<ExitGateVisual>(FindObjectsSortMode.None);
            if (gates != null && gates.Length > 0)
            {
                foreach (var g in gates)
                {
                    if (g.LevelNumber == 5)
                    {
                        g.transform.position = arenaPosition;
                        g.gameObject.SetActive(true);
                        g.SetVictoryGateState();
                        return;
                    }
                }
            }

            // Jika belum ada, buat instance Victory Gate baru di posisi arena
            GameObject newGate = new GameObject("VictoryExitGate");
            newGate.transform.position = arenaPosition;
            var box = newGate.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1.8f, 1.8f);
            var trigger = newGate.AddComponent<LevelGoalTrigger>();
            trigger.ResetTrigger();

            var visual = newGate.AddComponent<ExitGateVisual>();
            visual.Initialize(5);
            visual.SetVictoryGateState();
        }

        #endregion

        #region Procedural Cartoon House Exit Door Texture & Sprite Generation

        private Sprite GenerateShadowSprite()
        {
            int w = 64;
            int h = 32;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Vector2 center = new Vector2(w * 0.5f, h * 0.5f);
            float radiusX = w * 0.46f;
            float radiusY = h * 0.44f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - center.x) / radiusX;
                    float dy = (y - center.y) / radiusY;
                    float d = (dx * dx) + (dy * dy);
                    float a = Mathf.Clamp01(1f - Mathf.Sqrt(d));
                    a = a * a;
                    tex.SetPixel(x, y, new Color(0f, 0f, 0f, a * 0.35f));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 50f);
        }

        private Sprite GeneratePortalSprite()
        {
            // Keset Pintu Keluar Hijau dengan Panah ke Bawah (Green Exit Welcome Mat)
            int w = 64;
            int h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Vector2 center = new Vector2(w * 0.5f, h * 0.5f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool isBorder = (x <= 4 || x >= w - 5 || y <= 4 || y >= h - 5);
                    if (isBorder)
                    {
                        tex.SetPixel(x, y, new Color(0.12f, 0.48f, 0.24f, 1f));
                        continue;
                    }

                    float dist = Vector2.Distance(new Vector2(x, y), center);

                    // Lingkaran hijau cerah di tengah
                    if (dist <= 22f)
                    {
                        int cx = x - (w / 2);
                        int cy = y - (h / 2);

                        // Panah ke bawah putih
                        bool inStem = (Mathf.Abs(cx) <= 4 && cy >= -4 && cy <= 12);
                        bool inHead = (cy < -4 && cy >= -15 && Mathf.Abs(cx) <= (cy + 15) * 0.85f);

                        if (inStem || inHead)
                        {
                            tex.SetPixel(x, y, new Color(0.98f, 0.98f, 0.98f, 1f));
                        }
                        else
                        {
                            tex.SetPixel(x, y, new Color(0.20f, 0.85f, 0.38f, 1f));
                        }
                        continue;
                    }

                    tex.SetPixel(x, y, new Color(0.16f, 0.42f, 0.25f, 0.95f));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 50f);
        }

        private Sprite GenerateDoorPanelSprite(bool isLeft)
        {
            // Panel pintu kayu cartoon rumah
            int w = 32;
            int h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color woodBase = new Color(0.72f, 0.52f, 0.32f, 1f);
            Color woodDark = new Color(0.52f, 0.35f, 0.20f, 1f);
            Color brassHandle = new Color(0.95f, 0.82f, 0.25f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool isBorder = (x == 0 || x == w - 1 || y == 0 || y == h - 1);
                    if (isBorder)
                    {
                        tex.SetPixel(x, y, woodDark);
                        continue;
                    }

                    // Gagang pintu
                    bool isHandle = isLeft ? (x >= w - 6 && x <= w - 3 && y >= 28 && y <= 36)
                                           : (x >= 3 && x <= 6 && y >= 28 && y <= 36);
                    if (isHandle)
                    {
                        tex.SetPixel(x, y, brassHandle);
                        continue;
                    }

                    tex.SetPixel(x, y, woodBase);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 50f);
        }

        private Sprite GenerateLockSprite()
        {
            // Gembok gantung kartun
            int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Lengkungan shackle atas
                    float dShackle = Vector2.Distance(new Vector2(x, y), new Vector2(c.x, c.y + 4f));
                    if (dShackle <= 9f && dShackle >= 6f && y >= c.y + 4f)
                    {
                        tex.SetPixel(x, y, new Color(0.65f, 0.70f, 0.75f, 1f));
                        continue;
                    }

                    // Bodi gembok emas persegi di bawah (x: 7..24, y: 5..19)
                    if (x >= 7 && x <= 24 && y >= 5 && y <= 19)
                    {
                        bool isLockEdge = (x == 7 || x == 24 || y == 5 || y == 19);
                        if (isLockEdge)
                        {
                            tex.SetPixel(x, y, new Color(0.65f, 0.45f, 0.15f, 1f));
                            continue;
                        }

                        // Lubang kunci hitam
                        if (x >= 14 && x <= 17 && y >= 9 && y <= 15)
                        {
                            tex.SetPixel(x, y, new Color(0.15f, 0.15f, 0.18f, 1f));
                            continue;
                        }

                        tex.SetPixel(x, y, new Color(0.95f, 0.78f, 0.22f, 1f));
                        continue;
                    }

                    tex.SetPixel(x, y, Color.clear);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 40f);
        }

        private Sprite GenerateArchwaySprite()
        {
            // Kusen Pintu Rumah dengan Indikator EXIT Hijau di Atas
            int w = 72;
            int h = 72;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color frameCol = new Color(0.90f, 0.86f, 0.80f, 1f);
            Color frameDark = new Color(0.62f, 0.58f, 0.52f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Tiang kusen kiri (x: 0..6) & kanan (x: w-7..w-1)
                    bool isPillar = (x <= 6 || x >= w - 7) && y <= h - 1;

                    // Balok kusen atas (y: h-8..h-1)
                    bool isHeader = (y >= h - 8);

                    if (isPillar || isHeader)
                    {
                        bool isEdge = (x == 0 || x == w - 1 || y == 0 || y == h - 1);
                        tex.SetPixel(x, y, isEdge ? frameDark : frameCol);
                        continue;
                    }

                    // Badge EXIT kecil di tengah balok atas
                    if (y >= h - 16 && y <= h - 9 && x >= 20 && x <= w - 21)
                    {
                        tex.SetPixel(x, y, new Color(0.18f, 0.82f, 0.35f, 1f)); // Green EXIT sign
                        continue;
                    }

                    tex.SetPixel(x, y, Color.clear);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 50f);
        }

        private Sprite GeneratePromptBadgeSprite()
        {
            int w = 160;
            int h = 42;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color bg = new Color(0.08f, 0.10f, 0.14f, 0.88f);
            Color border = theme.runeColor;

            float r = h * 0.46f;
            Vector2 leftC = new Vector2(r, h * 0.5f);
            Vector2 rightC = new Vector2(w - r, h * 0.5f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool inside = false;
                    bool isBorder = false;

                    if (x >= r && x <= w - r)
                    {
                        if (y >= 0 && y < h)
                        {
                            inside = true;
                            if (y <= 2 || y >= h - 3) isBorder = true;
                        }
                    }
                    else if (x < r)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), leftC);
                        if (d <= r)
                        {
                            inside = true;
                            if (d >= r - 2.5f) isBorder = true;
                        }
                    }
                    else
                    {
                        float d = Vector2.Distance(new Vector2(x, y), rightC);
                        if (d <= r)
                        {
                            inside = true;
                            if (d >= r - 2.5f) isBorder = true;
                        }
                    }

                    if (!inside) tex.SetPixel(x, y, Color.clear);
                    else if (isBorder) tex.SetPixel(x, y, border);
                    else tex.SetPixel(x, y, bg);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 55f);
        }

        #endregion
    }
}
