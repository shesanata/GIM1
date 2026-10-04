using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace MazeSpell.UI
{
    /// <summary>
    /// Mengontrol tampilan, animasi fade/scale, input skip (Keyboard & Gamepad),
    /// serta transisi otomatis dari SplashScreen menuju MainMenu.
    /// </summary>
    public class SplashScreenController : MonoBehaviour
    {
        [Header("Konfigurasi Transisi Scene")]
        [Tooltip("Nama scene yang akan dimuat setelah splash screen selesai")]
        [SerializeField] private string nextSceneName = "MainMenu";

        [Tooltip("Durasi tampilan splash screen dalam detik sebelum berpindah otomatis")]
        [SerializeField] private float splashDuration = 2.5f;

        [Header("Referensi Elemen UI")]
        [SerializeField] private CanvasGroup contentCanvasGroup;
        [SerializeField] private RectTransform logoTransform;
        [SerializeField] private TextMeshProUGUI promptText;

        private bool isTransitioning = false;
        private float timer = 0f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreateSplashScreenIfNeeded()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != "SplashScreen") return;

#if UNITY_2023_1_OR_NEWER
            if (Object.FindAnyObjectByType<SplashScreenController>() != null) return;
#else
            if (Object.FindObjectOfType<SplashScreenController>() != null) return;
#endif

            CreateRuntimeSplashScreen();
        }

        private static void CreateRuntimeSplashScreen()
        {
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
            var controller = canvasObj.AddComponent<SplashScreenController>();

            // Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.12f, 0.18f, 1f);
            bgImg.raycastTarget = false;

            // Cari sprite menuBG jika ada di memory
            var allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
            if (allSprites != null)
            {
                foreach (var s in allSprites)
                {
                    if (s.name == "menuBG_0")
                    {
                        bgImg.sprite = s;
                        bgImg.color = new Color(0.75f, 0.75f, 0.8f, 1f);
                        break;
                    }
                }
            }

            // Vignette
            GameObject vigObj = new GameObject("VignetteOverlay");
            vigObj.transform.SetParent(bgObj.transform, false);
            RectTransform vigRect = vigObj.AddComponent<RectTransform>();
            vigRect.anchorMin = Vector2.zero;
            vigRect.anchorMax = Vector2.one;
            vigRect.offsetMin = Vector2.zero;
            vigRect.offsetMax = Vector2.zero;
            Image vigImg = vigObj.AddComponent<Image>();
            vigImg.color = new Color(0.03f, 0.06f, 0.12f, 0.55f);
            vigImg.raycastTarget = false;

            // ContentGroup
            GameObject contentObj = new GameObject("ContentGroup");
            contentObj.transform.SetParent(canvasObj.transform, false);
            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            CanvasGroup group = contentObj.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            TMP_FontAsset font = TMP_Settings.defaultFontAsset;

            // LogoContainer
            GameObject logoContObj = new GameObject("LogoContainer");
            logoContObj.transform.SetParent(contentObj.transform, false);
            RectTransform logoContRect = logoContObj.AddComponent<RectTransform>();
            logoContRect.anchorMin = new Vector2(0.5f, 0.5f);
            logoContRect.anchorMax = new Vector2(0.5f, 0.5f);
            logoContRect.sizeDelta = new Vector2(900f, 220f);
            logoContRect.anchoredPosition = new Vector2(0f, 30f);

            // Logo
            GameObject logoObj = new GameObject("Logo");
            logoObj.transform.SetParent(logoContObj.transform, false);
            RectTransform logoRect = logoObj.AddComponent<RectTransform>();
            logoRect.anchorMin = new Vector2(0f, 0.35f);
            logoRect.anchorMax = new Vector2(1f, 1f);
            logoRect.offsetMin = Vector2.zero;
            logoRect.offsetMax = Vector2.zero;
            TextMeshProUGUI logoTmp = logoObj.AddComponent<TextMeshProUGUI>();
            if (font != null) logoTmp.font = font;
            logoTmp.text = "<b>MAZESPELL</b>";
            logoTmp.fontSize = 88;
            logoTmp.fontStyle = FontStyles.Bold;
            logoTmp.alignment = TextAlignmentOptions.Center;
            logoTmp.color = new Color(0.99f, 0.85f, 0.35f, 1f);
            logoTmp.characterSpacing = 6f;
            logoTmp.raycastTarget = false;

            // Subtitle
            GameObject subObj = new GameObject("Subtitle");
            subObj.transform.SetParent(logoContObj.transform, false);
            RectTransform subRect = subObj.AddComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0f, 0f);
            subRect.anchorMax = new Vector2(1f, 0.4f);
            subRect.offsetMin = Vector2.zero;
            subRect.offsetMax = Vector2.zero;
            TextMeshProUGUI subTmp = subObj.AddComponent<TextMeshProUGUI>();
            if (font != null) subTmp.font = font;
            subTmp.text = "THE WORD LABYRINTH ADVENTURE";
            subTmp.fontSize = 24;
            subTmp.fontStyle = FontStyles.Bold;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.color = new Color(0.58f, 0.77f, 0.99f, 1f);
            subTmp.characterSpacing = 12f;
            subTmp.raycastTarget = false;

            // Loading / Prompt Text
            GameObject promptObj = new GameObject("LoadingText");
            promptObj.transform.SetParent(contentObj.transform, false);
            RectTransform promptRect = promptObj.AddComponent<RectTransform>();
            promptRect.anchorMin = new Vector2(0.5f, 0.5f);
            promptRect.anchorMax = new Vector2(0.5f, 0.5f);
            promptRect.sizeDelta = new Vector2(600f, 50f);
            promptRect.anchoredPosition = new Vector2(0f, -180f);
            TextMeshProUGUI promptTmp = promptObj.AddComponent<TextMeshProUGUI>();
            if (font != null) promptTmp.font = font;
            promptTmp.text = "PRESS ANY BUTTON TO CONTINUE";
            promptTmp.fontSize = 20;
            promptTmp.fontStyle = FontStyles.Bold;
            promptTmp.alignment = TextAlignmentOptions.Center;
            promptTmp.color = new Color(0.88f, 0.92f, 0.98f, 0.85f);
            promptTmp.characterSpacing = 4f;
            promptTmp.raycastTarget = false;

            controller.ConfigureReferences(group, logoContRect, promptTmp);

            // EventSystem
#if UNITY_2023_1_OR_NEWER
            if (Object.FindAnyObjectByType<EventSystem>() == null)
#else
            if (Object.FindObjectOfType<EventSystem>() == null)
#endif
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                es.AddComponent<StandaloneInputModule>();
#endif
            }
        }

        private void Awake()
        {
            // Bersihkan instance manager gameplay sisa jika ada saat kembali ke Splash
            CleanupGameplayManagers();
        }

        private void Start()
        {
            if (contentCanvasGroup != null)
            {
                contentCanvasGroup.alpha = 0f;
            }

            if (logoTransform != null)
            {
                logoTransform.localScale = Vector3.one * 0.92f;
            }

            StartCoroutine(SplashRoutine());
        }

        private void Update()
        {
            if (isTransitioning) return;

            // Periksa input skip dari keyboard atau controller/stik PS
            if (CheckSkipInput())
            {
                Debug.Log("[SplashScreen] Input skip terdeteksi! Langsung berpindah ke MainMenu...");
                SkipToMainMenu();
            }

            // Animasi berkedip halus pada prompt text jika tersedia
            if (promptText != null && timer > 0.8f)
            {
                float pulse = 0.45f + 0.55f * Mathf.PingPong(Time.time * 1.5f, 1f);
                Color c = promptText.color;
                c.a = pulse;
                promptText.color = c;
            }
        }

        /// <summary>
        /// Rutin animasi fade in dan scale halus logo sebelum berpindah scene.
        /// </summary>
        private IEnumerator SplashRoutine()
        {
            float elapsed = 0f;
            Vector3 startScale = Vector3.one * 0.92f;
            Vector3 endScale = Vector3.one * 1.04f;

            while (elapsed < splashDuration)
            {
                if (isTransitioning) yield break;

                elapsed += Time.deltaTime;
                timer = elapsed;
                float progress = Mathf.Clamp01(elapsed / splashDuration);

                // Fade in di 35% durasi pertama
                if (contentCanvasGroup != null)
                {
                    contentCanvasGroup.alpha = Mathf.Clamp01(elapsed / (splashDuration * 0.35f));
                }

                // Scale halus logo
                if (logoTransform != null)
                {
                    logoTransform.localScale = Vector3.Lerp(startScale, endScale, Mathf.SmoothStep(0f, 1f, progress));
                }

                yield return null;
            }

            // Transisi otomatis setelah durasi selesai
            TransitionToMainMenu();
        }

        /// <summary>
        /// Memeriksa apakah ada tombol keyboard, gamepad/stik PS, atau klik mouse yang ditekan.
        /// </summary>
        private bool CheckSkipInput()
        {
#if ENABLE_INPUT_SYSTEM
            // 1. Keyboard
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            {
                return true;
            }

            // 2. Gamepad / Stik PS (Tombol Aksi, D-Pad, Start, Options)
            var pad = Gamepad.current;
            if (pad != null)
            {
                if (pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame ||
                    pad.buttonWest.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame ||
                    pad.startButton.wasPressedThisFrame || pad.selectButton.wasPressedThisFrame ||
                    pad.leftShoulder.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame ||
                    pad.dpad.up.wasPressedThisFrame || pad.dpad.down.wasPressedThisFrame ||
                    pad.dpad.left.wasPressedThisFrame || pad.dpad.right.wasPressedThisFrame)
                {
                    return true;
                }
            }

            // 3. Mouse (Klik Kiri / Kanan)
            if (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame))
            {
                return true;
            }
#endif

            // Fallback Legacy Input
            try
            {
                if (Input.anyKeyDown)
                {
                    return true;
                }
            }
            catch
            {
                // Antisipasi jika legacy input dinonaktifkan di PlayerSettings
            }

            return false;
        }

        public void SkipToMainMenu()
        {
            if (isTransitioning) return;
            TransitionToMainMenu();
        }

        private void TransitionToMainMenu()
        {
            if (isTransitioning) return;
            isTransitioning = true;

            Debug.Log($"[SplashScreen] Memuat scene MainMenu: {nextSceneName}");

            if (Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                // Fallback memuat berdasarkan index 1 di Build Settings
                SceneManager.LoadScene(1);
            }
        }

        private void CleanupGameplayManagers()
        {
            var progression = Object.FindAnyObjectByType<MazeSpell.LevelProgressionManager>();
            if (progression != null) Destroy(progression.gameObject);

            var word = Object.FindAnyObjectByType<MazeSpell.WordCollectionManager>();
            if (word != null) Destroy(word.gameObject);

            var lives = Object.FindAnyObjectByType<MazeSpell.PlayerLivesManager>();
            if (lives != null) Destroy(lives.gameObject);

            var player = GameObject.Find("Player");
            if (player != null) Destroy(player);

            var ground = GameObject.Find("Ground");
            if (ground != null) Destroy(ground);
        }

        public void ConfigureReferences(CanvasGroup group, RectTransform logo, TextMeshProUGUI prompt)
        {
            contentCanvasGroup = group;
            logoTransform = logo;
            promptText = prompt;
        }
    }
}
