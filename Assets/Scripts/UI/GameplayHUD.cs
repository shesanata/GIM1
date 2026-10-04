using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MazeSpell.Gameplay;

namespace MazeSpell.UI
{
    /// <summary>
    /// Mengontrol tampilan UI Gameplay (HUD) untuk game MazeSpell dengan gaya Top-Down Adventure:
    /// - TOP LEFT: Pause button, Fast Forward, Action/Bag icon
    /// - TOP RIGHT: Hearts / Nyawa (3 Icon Hati Merah) + Tombol Menu
    /// - BOTTOM LEFT: Virtual Joystick (Touch / Mouse drag analog) untuk gerak karakter
    /// - Sesuai instruksi: Label "Target Word" dan "Level" dihapus dari tampilan agar bersih & imersif.
    /// Format: Screen Space - Overlay sehingga selalu fixed di layar.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasScaler))]
    public class GameplayHUD : MonoBehaviour
    {
        public static GameplayHUD Instance { get; private set; }

        [Header("Top Left Controls")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button fastForwardButton;
        [SerializeField] private Button bagButton;

        [Header("Top Right Nyawa")]
        [SerializeField] private RectTransform heartsContainer;
        [SerializeField] private Image[] heartImages = new Image[3];
        [SerializeField] private Button topMenuButton;

        [Header("Bottom Left Controls")]
        [SerializeField] private VirtualJoystick virtualJoystick;
        [SerializeField] private RectTransform joystickContainer;

        [Header("Legacy Compatible References")]
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI themeText;
        [SerializeField] private TextMeshProUGUI livesText;
        [SerializeField] private TextMeshProUGUI targetLabel;
        [SerializeField] private TextMeshProUGUI letterProgressText;
        [SerializeField] private RectTransform levelPanel;
        [SerializeField] private RectTransform livesPanel;
        [SerializeField] private RectTransform wordPanel;

        public TextMeshProUGUI LevelText => levelText;
        public TextMeshProUGUI ThemeText => themeText;
        public TextMeshProUGUI LivesText => livesText;
        public TextMeshProUGUI TargetText => targetLabel;
        public TextMeshProUGUI LetterProgressText => letterProgressText;
        public RectTransform LevelPanel => levelPanel;
        public RectTransform LivesContainer => livesPanel;
        public RectTransform TargetPanel => wordPanel;
        public VirtualJoystick VirtualJoystick => virtualJoystick;

        private int lastDisplayedLives = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreateHUDIfNeeded()
        {
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (activeScene.name != "SampleScene") return;

#if UNITY_2023_1_OR_NEWER
            if (UnityEngine.Object.FindAnyObjectByType<GameplayHUD>() != null) return;
#else
            if (UnityEngine.Object.FindObjectOfType<GameplayHUD>() != null) return;
#endif
            CreateRuntimeHUD();
        }

        public static GameplayHUD CreateRuntimeHUD()
        {
            GameObject canvasObj = new GameObject("GameplayCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 25;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
            var hud = canvasObj.AddComponent<GameplayHUD>();

            TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;

            // =========================================================
            // 1. TOP-LEFT: ACTION / PAUSE BUTTONS (Yellow/Gold circles)
            // =========================================================
            GameObject topLeftGroup = new GameObject("TopLeftControls");
            topLeftGroup.transform.SetParent(canvasObj.transform, false);
            RectTransform tlRect = topLeftGroup.AddComponent<RectTransform>();
            tlRect.anchorMin = new Vector2(0f, 1f);
            tlRect.anchorMax = new Vector2(0f, 1f);
            tlRect.pivot = new Vector2(0f, 1f);
            tlRect.anchoredPosition = new Vector2(25f, -25f);
            tlRect.sizeDelta = new Vector2(240f, 70f);

            // Pause Button (Yellow circle with ||)
            Button btnPause = CreateCircleButton(topLeftGroup.transform, "Btn_Pause", new Vector2(0f, 0f), 64f,
                new Color(0.95f, 0.75f, 0.2f, 1f), new Color(0.6f, 0.35f, 0.05f, 1f), "<b>||</b>", defaultFont, 26);
            btnPause.onClick.AddListener(() =>
            {
                if (PauseMenuController.Instance != null)
                {
                    PauseMenuController.Instance.PauseGame();
                }
            });

            // Fast Forward Button (>> decoration)
            Button btnFF = CreateCircleButton(topLeftGroup.transform, "Btn_FastForward", new Vector2(75f, 0f), 64f,
                new Color(0.95f, 0.75f, 0.2f, 1f), new Color(0.6f, 0.35f, 0.05f, 1f), "<b>>></b>", defaultFont, 24);

            // Bag / Backpack Button (🎒 decoration)
            Button btnBag = CreateCircleButton(topLeftGroup.transform, "Btn_Bag", new Vector2(150f, 0f), 64f,
                new Color(0.95f, 0.75f, 0.2f, 1f), new Color(0.6f, 0.35f, 0.05f, 1f), "<b>⚿</b>", defaultFont, 28);

            // =========================================================
            // 2. TOP-RIGHT: 3 HEARTS & MENU BUTTON
            // =========================================================
            GameObject topRightGroup = new GameObject("TopRightControls");
            topRightGroup.transform.SetParent(canvasObj.transform, false);
            RectTransform trRect = topRightGroup.AddComponent<RectTransform>();
            trRect.anchorMin = new Vector2(1f, 1f);
            trRect.anchorMax = new Vector2(1f, 1f);
            trRect.pivot = new Vector2(1f, 1f);
            trRect.anchoredPosition = new Vector2(-25f, -25f);
            trRect.sizeDelta = new Vector2(240f, 70f);

            // Hearts Container
            GameObject heartsGo = new GameObject("HeartsContainer");
            heartsGo.transform.SetParent(topRightGroup.transform, false);
            RectTransform hRect = heartsGo.AddComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0f, 0.5f);
            hRect.anchorMax = new Vector2(0f, 0.5f);
            hRect.pivot = new Vector2(0f, 0.5f);
            hRect.anchoredPosition = new Vector2(0f, 0f);
            hRect.sizeDelta = new Vector2(150f, 50f);

            Image[] hearts = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                GameObject singleHeart = new GameObject($"Heart_{i + 1}");
                singleHeart.transform.SetParent(heartsGo.transform, false);
                RectTransform shRect = singleHeart.AddComponent<RectTransform>();
                shRect.anchorMin = new Vector2(0f, 0.5f);
                shRect.anchorMax = new Vector2(0f, 0.5f);
                shRect.pivot = new Vector2(0f, 0.5f);
                shRect.anchoredPosition = new Vector2(i * 48f, 0f);
                shRect.sizeDelta = new Vector2(42f, 42f);

                Image hImg = singleHeart.AddComponent<Image>();
                hImg.color = new Color(0.95f, 0.2f, 0.2f, 1f); // Vibrant red heart
                hImg.raycastTarget = false;

                // Teks simbol hati
                GameObject hTxtGo = new GameObject("HeartIcon");
                hTxtGo.transform.SetParent(singleHeart.transform, false);
                RectTransform htRect = hTxtGo.AddComponent<RectTransform>();
                htRect.anchorMin = Vector2.zero;
                htRect.anchorMax = Vector2.one;
                htRect.offsetMin = Vector2.zero;
                htRect.offsetMax = Vector2.zero;
                TextMeshProUGUI hTmp = hTxtGo.AddComponent<TextMeshProUGUI>();
                if (defaultFont != null) hTmp.font = defaultFont;
                hTmp.text = "♥";
                hTmp.fontSize = 34;
                hTmp.alignment = TextAlignmentOptions.Center;
                hTmp.color = Color.white;
                hTmp.raycastTarget = false;

                hearts[i] = hImg;
            }

            // Top-right Blue Pause/Menu Button
            Button btnMenu = CreateCircleButton(topRightGroup.transform, "Btn_Menu", new Vector2(170f, 0f), 64f,
                new Color(0.2f, 0.45f, 0.85f, 1f), new Color(0.08f, 0.2f, 0.5f, 1f), "<b>||</b>", defaultFont, 26);
            btnMenu.onClick.AddListener(() =>
            {
                if (PauseMenuController.Instance != null)
                {
                    PauseMenuController.Instance.PauseGame();
                }
            });

            // =========================================================
            // 3. BOTTOM-LEFT: VIRTUAL JOYSTICK
            // =========================================================
            GameObject joystickGo = new GameObject("VirtualJoystick");
            joystickGo.transform.SetParent(canvasObj.transform, false);
            RectTransform joyRect = joystickGo.AddComponent<RectTransform>();
            joyRect.anchorMin = new Vector2(0f, 0f);
            joyRect.anchorMax = new Vector2(0f, 0f);
            joyRect.pivot = new Vector2(0f, 0f);
            joyRect.anchoredPosition = new Vector2(60f, 60f);
            joyRect.sizeDelta = new Vector2(180f, 180f);

            // Joystick Background (Translucent outer ring with directional hints)
            Image joyBg = joystickGo.AddComponent<Image>();
            joyBg.color = new Color(1f, 1f, 1f, 0.25f);

            // 4 Directional Arrows Text
            GameObject arrowsGo = new GameObject("Arrows");
            arrowsGo.transform.SetParent(joystickGo.transform, false);
            RectTransform arrRect = arrowsGo.AddComponent<RectTransform>();
            arrRect.anchorMin = Vector2.zero;
            arrRect.anchorMax = Vector2.one;
            arrRect.offsetMin = Vector2.zero;
            arrRect.offsetMax = Vector2.zero;
            TextMeshProUGUI arrTmp = arrowsGo.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) arrTmp.font = defaultFont;
            arrTmp.text = "▲\n◄   ►\n▼";
            arrTmp.fontSize = 20;
            arrTmp.alignment = TextAlignmentOptions.Center;
            arrTmp.color = new Color(1f, 1f, 1f, 0.55f);
            arrTmp.raycastTarget = false;

            // Joystick Handle (Center movable knob)
            GameObject handleGo = new GameObject("Handle");
            handleGo.transform.SetParent(joystickGo.transform, false);
            RectTransform handleRect = handleGo.AddComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.anchoredPosition = Vector2.zero;
            handleRect.sizeDelta = new Vector2(80f, 80f);

            Image handleImg = handleGo.AddComponent<Image>();
            handleImg.color = new Color(1f, 1f, 1f, 0.65f);

            var vJoy = joystickGo.AddComponent<VirtualJoystick>();

            // =========================================================
            // 4. BOTTOM-RIGHT: ACTION BUTTONS ([E] HIDE & [SPACE] ATTACK)
            // =========================================================
            GameObject bottomRightGroup = new GameObject("BottomRightActions");
            bottomRightGroup.transform.SetParent(canvasObj.transform, false);
            RectTransform brRect = bottomRightGroup.AddComponent<RectTransform>();
            brRect.anchorMin = new Vector2(1f, 0f);
            brRect.anchorMax = new Vector2(1f, 0f);
            brRect.pivot = new Vector2(1f, 0f);
            brRect.anchoredPosition = new Vector2(-60f, 60f);
            brRect.sizeDelta = new Vector2(220f, 100f);

            // Button [E] Sembunyi / Hide
            Button btnHide = CreateCircleButton(bottomRightGroup.transform, "Btn_Hide_E", new Vector2(-110f, 0f), 76f,
                new Color(0.95f, 0.75f, 0.2f, 0.95f), new Color(0.6f, 0.35f, 0.05f, 1f), "<b>E</b>\n<size=11>Kardus</size>", defaultFont, 22);
            btnHide.onClick.AddListener(() =>
            {
                var box = UnityEngine.Object.FindAnyObjectByType<HidingBox>();
                if (box != null)
                {
                    box.ToggleHiding();
                }
            });

            // Button [SPACE] Serang / Attack
            Button btnAttack = CreateCircleButton(bottomRightGroup.transform, "Btn_Attack_Space", new Vector2(0f, 0f), 76f,
                new Color(0.85f, 0.25f, 0.25f, 0.95f), new Color(0.5f, 0.1f, 0.1f, 1f), "<b>⚔</b>\n<size=11>Serang</size>", defaultFont, 22);
            btnAttack.onClick.AddListener(() =>
            {
                var player = PlayerController.Instance;
                if (player != null && !player.IsHiding)
                {
                    var enemy = EnemyCharacter.ActiveEnemy;
                    if (enemy != null && !enemy.IsDefeated)
                    {
                        enemy.TryPlayerAttack(player.gameObject, 1);
                    }
                }
            });

            // Setup References
            hud.pauseButton = btnPause;
            hud.fastForwardButton = btnFF;
            hud.bagButton = btnBag;
            hud.heartsContainer = hRect;
            hud.heartImages = hearts;
            hud.topMenuButton = btnMenu;
            hud.virtualJoystick = vJoy;
            hud.joystickContainer = joyRect;

            // Pastikan ada EventSystem
#if UNITY_2023_1_OR_NEWER
            if (UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
#else
            if (UnityEngine.Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
#endif
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
            }

            return hud;
        }

        private static Button CreateCircleButton(Transform parent, string name, Vector2 localPos, float size,
            Color bgColor, Color borderColor, string labelText, TMP_FontAsset font, float fontSize)
        {
            GameObject btnGo = new GameObject(name);
            btnGo.transform.SetParent(parent, false);

            RectTransform rect = btnGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = localPos;
            rect.sizeDelta = new Vector2(size, size);

            Image img = btnGo.AddComponent<Image>();
            img.color = bgColor;

            Button btn = btnGo.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = Color.Lerp(bgColor, Color.white, 0.25f);
            cb.pressedColor = Color.Lerp(bgColor, Color.black, 0.25f);
            btn.colors = cb;

            GameObject textGo = new GameObject("Label");
            textGo.transform.SetParent(btnGo.transform, false);
            RectTransform tRect = textGo.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = labelText;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = borderColor;
            tmp.raycastTarget = false;

            return btn;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            UpdateHeartsDisplay();
        }

        private void Update()
        {
            UpdateHeartsDisplay();
        }

        public void UpdateHeartsDisplay()
        {
            var livesMgr = PlayerLivesManager.Instance;
            int currentLives = (livesMgr != null) ? livesMgr.CurrentLives : 3;

            if (currentLives == lastDisplayedLives) return;
            lastDisplayedLives = currentLives;

            if (heartImages != null)
            {
                for (int i = 0; i < heartImages.Length; i++)
                {
                    if (heartImages[i] != null)
                    {
                        bool active = i < currentLives;
                        heartImages[i].color = active
                            ? new Color(0.95f, 0.2f, 0.2f, 1f) // Heart full red
                            : new Color(0.3f, 0.3f, 0.35f, 0.45f); // Heart empty gray
                    }
                }
            }
        }

        // Backward compatibility stubs for legacy setup and test runners
        public void EnsureReferences() { }
        public void EnforceScreenSpaceCanvas() { }
        public void UpdateLevelUI(int level, string theme) { }
        public void UpdateWordUI(string word, string progress) { }
        public void UpdateLivesUI(int current, int max) { UpdateHeartsDisplay(); }
        public void ConfigureReferences(TextMeshProUGUI lvlTxt, TextMeshProUGUI thmTxt, TextMeshProUGUI livTxt,
            TextMeshProUGUI tgtTxt, TextMeshProUGUI prgTxt, RectTransform lvlPnl, RectTransform livPnl, RectTransform wrdPnl)
        {
            levelText = lvlTxt;
            themeText = thmTxt;
            livesText = livTxt;
            targetLabel = tgtTxt;
            letterProgressText = prgTxt;
            levelPanel = lvlPnl;
            livesPanel = livPnl;
            wordPanel = wrdPnl;
        }
    }
}
