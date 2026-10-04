using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace MazeSpell.UI
{
    /// <summary>
    /// Mengontrol logika Pause Menu di gameplay MazeSpell.
    /// Mengatur Time.timeScale, input pause (ESC & Gamepad Start/Options),
    /// serta tombol Resume, Restart Level, Main Menu, dan Quit.
    /// Mendukung penuh navigasi stik PS / Gamepad dan Keyboard.
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        public static PauseMenuController Instance { get; private set; }

        [Header("Referensi UI")]
        [Tooltip("Panel utama pause yang akan diaktifkan/dinonaktifkan")]
        [SerializeField] private GameObject pausePanel;

        [Tooltip("Tombol Resume pertama untuk fokus navigasi Gamepad/Keyboard")]
        [SerializeField] private Button resumeButton;

        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button quitButton;

        [Header("Konfigurasi Scene")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private bool isPaused = false;
        public bool IsPaused => isPaused;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreatePauseMenuIfNeeded()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.name != "SampleScene") return;

#if UNITY_2023_1_OR_NEWER
            if (Object.FindAnyObjectByType<PauseMenuController>() != null) return;
#else
            if (Object.FindObjectOfType<PauseMenuController>() != null) return;
#endif

            // Gunakan helper runner agar menunggu 1 frame, memastikan GameplayHUD & Canvas sudah ada
            GameObject runner = new GameObject("[PauseMenuRuntimeRunner]");
            runner.AddComponent<PauseMenuRuntimeRunner>();
        }

        private class PauseMenuRuntimeRunner : MonoBehaviour
        {
            private IEnumerator Start()
            {
                yield return null; // Tunggu satu frame unscaled agar Canvas GameplayHUD sudah selesai terbuat
                CreateRuntimePauseMenu();
                Destroy(gameObject);
            }
        }

        public static void CreateRuntimePauseMenu()
        {
#if UNITY_2023_1_OR_NEWER
            if (Object.FindAnyObjectByType<PauseMenuController>() != null) return;
#else
            if (Object.FindObjectOfType<PauseMenuController>() != null) return;
#endif

            // 1. Cari Canvas gameplay yang sudah ada agar tidak membuat Canvas ganda
            Transform parentCanvas = null;
#if UNITY_2023_1_OR_NEWER
            var hud = Object.FindAnyObjectByType<GameplayHUD>();
            var anyCanvas = Object.FindAnyObjectByType<Canvas>();
#else
            var hud = Object.FindObjectOfType<GameplayHUD>();
            var anyCanvas = Object.FindObjectOfType<Canvas>();
#endif
            if (hud != null)
            {
                parentCanvas = hud.transform;
            }
            else if (anyCanvas != null)
            {
                parentCanvas = anyCanvas.transform;
            }
            else
            {
                GameObject canvasObj = new GameObject("GameplayCanvas");
                Canvas c = canvasObj.AddComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                c.sortingOrder = 20;
                CanvasScaler cs = canvasObj.AddComponent<CanvasScaler>();
                cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                cs.referenceResolution = new Vector2(1920, 1080);
                cs.matchWidthOrHeight = 0.5f;
                canvasObj.AddComponent<GraphicRaycaster>();
                parentCanvas = canvasObj.transform;
            }

            // 2. Pastikan EventSystem tersedia untuk navigasi Gamepad & Keyboard
#if UNITY_2023_1_OR_NEWER
            var existingEventSystem = Object.FindAnyObjectByType<EventSystem>();
#else
            var existingEventSystem = Object.FindObjectOfType<EventSystem>();
#endif
            if (existingEventSystem == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                esObj.AddComponent<InputSystemUIInputModule>();
#else
                esObj.AddComponent<StandaloneInputModule>();
#endif
            }

            // 3. Bangun hierarki PausePanel di dalam Canvas yang sama
            GameObject pausePanelObj = BuildPausePanelHierarchy(parentCanvas);
            pausePanelObj.SetActive(false);
        }

        public static GameObject BuildPausePanelHierarchy(Transform canvasParent)
        {
            TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;

            // 1. PausePanel (Full Overlay)
            GameObject panelObj = new GameObject("PausePanel");
            panelObj.transform.SetParent(canvasParent, false);
            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var controller = panelObj.AddComponent<PauseMenuController>();

            // 2. DimBackground (Latar Gelap Semi-Transparan)
            GameObject dimObj = new GameObject("DimBackground");
            dimObj.transform.SetParent(panelObj.transform, false);
            RectTransform dimRect = dimObj.AddComponent<RectTransform>();
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;
            Image dimImg = dimObj.AddComponent<Image>();
            dimImg.color = new Color(0.04f, 0.07f, 0.12f, 0.90f); // Dark slate blur tint
            dimImg.raycastTarget = true; // Mencegah klik tembus ke gameplay

            // 3. Title ("GAME PAUSED")
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panelObj.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.sizeDelta = new Vector2(650f, 100f);
            titleRect.anchoredPosition = new Vector2(0f, 185f);

            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) titleTmp.font = defaultFont;
            titleTmp.text = "<b>GAME PAUSED</b>\n<size=18><color=#93C5FD>MAZESPELL</color></size>";
            titleTmp.fontSize = 50;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(0.99f, 0.85f, 0.35f, 1f); // Warm Gold
            titleTmp.characterSpacing = 3f;
            titleTmp.raycastTarget = false;

            // 4. Buttons Container
            float btnW = 360f;
            float btnH = 64f;

            // 1. Resume Button
            GameObject resumeBtnObj = CreateButton("ResumeButton", panelObj.transform,
                new Vector2(0f, 55f), new Vector2(btnW, btnH),
                "▶  RESUME",
                new Color(0.10f, 0.68f, 0.42f, 1f),
                new Color(0.18f, 0.82f, 0.52f, 1f),
                new Color(0.06f, 0.50f, 0.30f, 1f),
                defaultFont);
            Button resumeBtn = resumeBtnObj.GetComponent<Button>();
            resumeBtn.onClick.AddListener(controller.ResumeGame);

            // 2. Restart Level Button
            GameObject restartBtnObj = CreateButton("RestartButton", panelObj.transform,
                new Vector2(0f, -20f), new Vector2(btnW, btnH),
                "↺  RESTART LEVEL",
                new Color(0.85f, 0.55f, 0.12f, 1f),
                new Color(0.98f, 0.68f, 0.20f, 1f),
                new Color(0.68f, 0.42f, 0.08f, 1f),
                defaultFont);
            Button restartBtn = restartBtnObj.GetComponent<Button>();
            restartBtn.onClick.AddListener(controller.RestartLevel);

            // 3. Main Menu Button
            GameObject menuBtnObj = CreateButton("MainMenuButton", panelObj.transform,
                new Vector2(0f, -95f), new Vector2(btnW, btnH),
                "⌂  MAIN MENU",
                new Color(0.20f, 0.52f, 0.90f, 1f),
                new Color(0.32f, 0.65f, 0.98f, 1f),
                new Color(0.12f, 0.38f, 0.70f, 1f),
                defaultFont);
            Button menuBtn = menuBtnObj.GetComponent<Button>();
            menuBtn.onClick.AddListener(controller.ReturnToMainMenu);

            // 4. Quit Button
            GameObject quitBtnObj = CreateButton("QuitButton", panelObj.transform,
                new Vector2(0f, -170f), new Vector2(btnW, btnH),
                "✖  QUIT",
                new Color(0.85f, 0.25f, 0.28f, 1f),
                new Color(0.95f, 0.38f, 0.42f, 1f),
                new Color(0.65f, 0.16f, 0.18f, 1f),
                defaultFont);
            Button quitBtn = quitBtnObj.GetComponent<Button>();
            quitBtn.onClick.AddListener(controller.QuitGame);

            // Setup Navigation antar tombol untuk Gamepad D-Pad & Left Stick
            SetupButtonNavigation(resumeBtn, restartBtn, menuBtn, quitBtn);

            controller.ConfigureReferences(panelObj, resumeBtn, restartBtn, menuBtn, quitBtn);

            return panelObj;
        }

        private static GameObject CreateButton(string name, Transform parent,
            Vector2 position, Vector2 size, string label,
            Color normalColor, Color hoverColor, Color pressColor, TMP_FontAsset font)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            Image img = btnObj.AddComponent<Image>();
            img.color = normalColor;

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = hoverColor;
            colors.pressedColor = pressColor;
            colors.selectedColor = hoverColor;
            colors.fadeDuration = 0.08f;
            btn.colors = colors;

            // Label Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = label;
            tmp.fontSize = 22;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            return btnObj;
        }

        private static void SetupButtonNavigation(Button b1, Button b2, Button b3, Button b4)
        {
            Navigation n1 = new Navigation { mode = Navigation.Mode.Explicit, selectOnDown = b2, selectOnUp = b4 };
            Navigation n2 = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = b1, selectOnDown = b3 };
            Navigation n3 = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = b2, selectOnDown = b4 };
            Navigation n4 = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = b3, selectOnDown = b1 };

            b1.navigation = n1;
            b2.navigation = n2;
            b3.navigation = n3;
            b4.navigation = n4;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Pastikan timeScale selalu 1 saat inisialisasi awal scene
            Time.timeScale = 1f;
            isPaused = false;
        }

        private void Start()
        {
            if (pausePanel != null && pausePanel.activeSelf)
            {
                pausePanel.SetActive(false);
            }
        }

        private void Update()
        {
            // Periksa input pembuka/penutup pause (ESC atau Gamepad Options/Start)
            if (CheckPauseToggleInput())
            {
                TogglePause();
                return;
            }

            // Jika sedang pause
            if (isPaused)
            {
                // Jika pemain menekan tombol Cancel (B/Circle di stik PS), lakukan resume
                if (CheckCancelInput())
                {
                    ResumeGame();
                    return;
                }

                // Pulihkan fokus tombol jika pengguna mengklik layar di luar tombol
                if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null && resumeButton != null)
                {
                    EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
                }
            }
        }

        private void OnDisable()
        {
            // Pastikan pergerakan player dan timeScale pulih jika script dinonaktifkan
            RestoreGameplayState();
        }

        private void OnDestroy()
        {
            // Pastikan waktu kembali normal saat objek/scene berganti
            RestoreGameplayState();
        }

        private void RestoreGameplayState()
        {
            Time.timeScale = 1f;
            isPaused = false;

            // Pulihkan PlayerController jika sempat dinonaktifkan saat pause
#if UNITY_2023_1_OR_NEWER
            var player = Object.FindAnyObjectByType<PlayerController>();
#else
            var player = Object.FindObjectOfType<PlayerController>();
#endif
            if (player != null && !player.enabled)
            {
                player.enabled = true;
            }
        }

        /// <summary>
        /// Memeriksa input toggle pause dari Keyboard (ESC) dan Gamepad (Start / Options).
        /// </summary>
        private bool CheckPauseToggleInput()
        {
#if ENABLE_INPUT_SYSTEM
            // 1. Keyboard ESC
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                return true;
            }

            // 2. Gamepad / Stik PS (Start / Options)
            var pad = Gamepad.current;
            if (pad != null)
            {
                if (pad.startButton.wasPressedThisFrame || pad.selectButton.wasPressedThisFrame)
                {
                    return true;
                }
            }
#endif

            // Fallback Legacy Input
            try
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    return true;
                }
            }
            catch
            {
                // Fallback aman
            }

            return false;
        }

        /// <summary>
        /// Memeriksa tombol Cancel (B / Circle) pada Gamepad saat pause untuk langsung resume.
        /// </summary>
        private bool CheckCancelInput()
        {
#if ENABLE_INPUT_SYSTEM
            var pad = Gamepad.current;
            if (pad != null && pad.buttonEast.wasPressedThisFrame)
            {
                return true;
            }
#endif
            return false;
        }

        public void TogglePause()
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }

        public void PauseGame()
        {
            isPaused = true;
            Time.timeScale = 0f;

            // Hentikan pergerakan PlayerController saat pause
#if UNITY_2023_1_OR_NEWER
            var player = Object.FindAnyObjectByType<PlayerController>();
#else
            var player = Object.FindObjectOfType<PlayerController>();
#endif
            if (player != null)
            {
                player.enabled = false;
                var rb = player.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
#if UNITY_6000_0_OR_NEWER
                    rb.linearVelocity = Vector2.zero;
#else
                    rb.velocity = Vector2.zero;
#endif
                }
            }

            if (pausePanel != null)
            {
                pausePanel.SetActive(true);
            }

            // Fokuskan tombol Resume untuk navigasi Gamepad/Keyboard seketika
            StartCoroutine(FocusResumeButtonNextFrame());
            Debug.Log("[PauseMenu] Game PAUSED. Time.timeScale = 0");
        }

        private IEnumerator FocusResumeButtonNextFrame()
        {
            yield return null; // Tunggu satu frame unscaled
            if (EventSystem.current != null && resumeButton != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
            }
        }

        public void ResumeGame()
        {
            isPaused = false;
            Time.timeScale = 1f;

            // Aktifkan kembali pergerakan PlayerController
#if UNITY_2023_1_OR_NEWER
            var player = Object.FindAnyObjectByType<PlayerController>();
#else
            var player = Object.FindObjectOfType<PlayerController>();
#endif
            if (player != null)
            {
                player.enabled = true;
            }

            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            Debug.Log("[PauseMenu] Game RESUMED. Time.timeScale = 1");
        }

        public void RestartLevel()
        {
            Debug.Log("[PauseMenu] Restart Level diminta...");
            ResumeGame();

            // Gunakan sistem RestartCurrentLevel() yang sudah tersedia di LevelProgressionManager
            if (LevelProgressionManager.Instance != null)
            {
                LevelProgressionManager.Instance.RestartCurrentLevel();
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        public void ReturnToMainMenu()
        {
            Debug.Log($"[PauseMenu] Kembali ke Main Menu: {mainMenuSceneName}");
            // Sangat penting: Kembalikan waktu normal sebelum berpindah scene!
            RestoreGameplayState();

            if (Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                SceneManager.LoadScene(mainMenuSceneName);
            }
            else
            {
                SceneManager.LoadScene(0); // Index 0 adalah MainMenu
            }
        }

        public void QuitGame()
        {
            Debug.Log("[PauseMenu] Keluar dari game...");
            RestoreGameplayState();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void ConfigureReferences(GameObject panel, Button resume, Button restart, Button menu, Button quit)
        {
            pausePanel = panel;
            resumeButton = resume;
            restartButton = restart;
            mainMenuButton = menu;
            quitButton = quit;
        }
    }
}
