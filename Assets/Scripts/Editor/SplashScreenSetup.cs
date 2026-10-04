using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace MazeSpell.Editor
{
    /// <summary>
    /// Editor tool untuk membuat dan menata scene SplashScreen.unity di MazeSpell.
    /// Mendaftarkan SplashScreen pada index 0 di Build Settings sesuai urutan alur game.
    /// </summary>
    [InitializeOnLoad]
    public static class SplashScreenSetup
    {
        private const string SplashScreenScenePath = "Assets/Scenes/SplashScreen.unity";
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string GameplayScenePath = "Assets/Scenes/SampleScene.unity";
        private const string LevelSelectScenePath = "Assets/Ui/Screens/LevelSelect.unity";
        private const string MenuBackgroundPath = "Assets/Ui/Background/menuBG.jpeg";

        static SplashScreenSetup()
        {
            EditorApplication.delayCall += AutoSetupIfNeeded;
        }

        private static void AutoSetupIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            // Pastikan Build Settings memiliki SplashScreen di index 0
            UpdateBuildSettings();

            if (!SessionState.GetBool("MazeSpell_SplashScreenSetupDone_V1", false))
            {
                SessionState.SetBool("MazeSpell_SplashScreenSetupDone_V1", true);
                SetupSplashScreenScene(false);
            }
        }

        [MenuItem("MazeSpell/Setup Splash Screen Scene")]
        public static void SetupSplashScreenManual()
        {
            SetupSplashScreenScene(true);
        }

        public static void SetupSplashScreenScene(bool interactive)
        {
            try
            {
                Debug.Log("[SplashScreenSetup] Memulai pembuatan scene SplashScreen.unity...");

                // 1. Buat atau buka scene SplashScreen
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                // Setup Main Camera
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                Camera cam = camObj.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.06f, 0.09f, 0.15f, 1f); // Deep midnight blue
                cam.orthographic = true;
                cam.orthographicSize = 5f;
                camObj.transform.position = new Vector3(0f, 0f, -10f);
                camObj.AddComponent<AudioListener>();

                // 2. Setup Canvas
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
                var splashController = canvasObj.AddComponent<MazeSpell.UI.SplashScreenController>();

                // 3. Background (menuBG)
                GameObject bgObj = new GameObject("Background");
                bgObj.transform.SetParent(canvasObj.transform, false);
                RectTransform bgRect = bgObj.AddComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;

                Image bgImage = bgObj.AddComponent<Image>();
                Sprite bgSprite = LoadMenuBackgroundSprite();
                if (bgSprite != null)
                {
                    bgImage.sprite = bgSprite;
                    bgImage.type = Image.Type.Simple;
                    bgImage.preserveAspect = false;
                }
                bgImage.color = new Color(0.75f, 0.75f, 0.8f, 1f);
                bgImage.raycastTarget = false;

                // Vignette overlay
                GameObject vigObj = new GameObject("VignetteOverlay");
                vigObj.transform.SetParent(bgObj.transform, false);
                RectTransform vigRect = vigObj.AddComponent<RectTransform>();
                vigRect.anchorMin = Vector2.zero;
                vigRect.anchorMax = Vector2.one;
                vigRect.offsetMin = Vector2.zero;
                vigRect.offsetMax = Vector2.zero;
                Image vigImage = vigObj.AddComponent<Image>();
                vigImage.color = new Color(0.03f, 0.06f, 0.12f, 0.55f);
                vigImage.raycastTarget = false;

                // 4. Content Group (dengan CanvasGroup untuk fade in)
                GameObject contentObj = new GameObject("ContentGroup");
                contentObj.transform.SetParent(canvasObj.transform, false);
                RectTransform contentRect = contentObj.AddComponent<RectTransform>();
                contentRect.anchorMin = Vector2.zero;
                contentRect.anchorMax = Vector2.one;
                contentRect.offsetMin = Vector2.zero;
                contentRect.offsetMax = Vector2.zero;

                CanvasGroup canvasGroup = contentObj.AddComponent<CanvasGroup>();
                canvasGroup.alpha = 0f;

                // Font bawaan TMP
                TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;

                // Logo Container (untuk scale effect)
                GameObject logoContainer = new GameObject("LogoContainer");
                logoContainer.transform.SetParent(contentObj.transform, false);
                RectTransform logoContRect = logoContainer.AddComponent<RectTransform>();
                logoContRect.anchorMin = new Vector2(0.5f, 0.5f);
                logoContRect.anchorMax = new Vector2(0.5f, 0.5f);
                logoContRect.sizeDelta = new Vector2(900f, 220f);
                logoContRect.anchoredPosition = new Vector2(0f, 30f);

                // Logo Text ("MAZESPELL")
                GameObject logoTextObj = new GameObject("Logo");
                logoTextObj.transform.SetParent(logoContainer.transform, false);
                RectTransform logoTextRect = logoTextObj.AddComponent<RectTransform>();
                logoTextRect.anchorMin = new Vector2(0f, 0.35f);
                logoTextRect.anchorMax = new Vector2(1f, 1f);
                logoTextRect.offsetMin = Vector2.zero;
                logoTextRect.offsetMax = Vector2.zero;

                TextMeshProUGUI logoTmp = logoTextObj.AddComponent<TextMeshProUGUI>();
                if (defaultFont != null) logoTmp.font = defaultFont;
                logoTmp.text = "<b>MAZESPELL</b>";
                logoTmp.fontSize = 88;
                logoTmp.fontStyle = FontStyles.Bold;
                logoTmp.alignment = TextAlignmentOptions.Center;
                logoTmp.color = new Color(0.99f, 0.85f, 0.35f, 1f); // Warm Gold
                logoTmp.characterSpacing = 6f;
                logoTmp.raycastTarget = false;

                // Subtitle Text ("THE WORD LABYRINTH ADVENTURE")
                GameObject subTextObj = new GameObject("Subtitle");
                subTextObj.transform.SetParent(logoContainer.transform, false);
                RectTransform subTextRect = subTextObj.AddComponent<RectTransform>();
                subTextRect.anchorMin = new Vector2(0f, 0f);
                subTextRect.anchorMax = new Vector2(1f, 0.4f);
                subTextRect.offsetMin = Vector2.zero;
                subTextRect.offsetMax = Vector2.zero;

                TextMeshProUGUI subTmp = subTextObj.AddComponent<TextMeshProUGUI>();
                if (defaultFont != null) subTmp.font = defaultFont;
                subTmp.text = "THE WORD LABYRINTH ADVENTURE";
                subTmp.fontSize = 24;
                subTmp.fontStyle = FontStyles.Bold;
                subTmp.alignment = TextAlignmentOptions.Center;
                subTmp.color = new Color(0.58f, 0.77f, 0.99f, 1f); // Soft Adventure Blue
                subTmp.characterSpacing = 12f;
                subTmp.raycastTarget = false;

                // Loading / Prompt Text ("PRESS ANY BUTTON TO CONTINUE")
                GameObject promptObj = new GameObject("LoadingText");
                promptObj.transform.SetParent(contentObj.transform, false);
                RectTransform promptRect = promptObj.AddComponent<RectTransform>();
                promptRect.anchorMin = new Vector2(0.5f, 0.5f);
                promptRect.anchorMax = new Vector2(0.5f, 0.5f);
                promptRect.sizeDelta = new Vector2(600f, 50f);
                promptRect.anchoredPosition = new Vector2(0f, -180f);

                TextMeshProUGUI promptTmp = promptObj.AddComponent<TextMeshProUGUI>();
                if (defaultFont != null) promptTmp.font = defaultFont;
                promptTmp.text = "PRESS ANY BUTTON TO CONTINUE";
                promptTmp.fontSize = 20;
                promptTmp.fontStyle = FontStyles.Bold;
                promptTmp.alignment = TextAlignmentOptions.Center;
                promptTmp.color = new Color(0.88f, 0.92f, 0.98f, 0.85f);
                promptTmp.characterSpacing = 4f;
                promptTmp.raycastTarget = false;

                // 5. Hubungkan referensi controller
                splashController.ConfigureReferences(canvasGroup, logoContRect, promptTmp);

                // 6. EventSystem
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                eventSystemObj.AddComponent<InputSystemUIInputModule>();
#else
                eventSystemObj.AddComponent<StandaloneInputModule>();
#endif

                // 7. Simpan scene
                EditorSceneManager.SaveScene(scene, SplashScreenScenePath);
                Debug.Log($"[SplashScreenSetup] Scene berhasil disimpan di '{SplashScreenScenePath}'!");

                // 8. Update Build Settings agar urutan tepat: [0] SplashScreen, [1] MainMenu, [2] SampleScene, [3] LevelSelect
                UpdateBuildSettings();

                if (interactive)
                {
                    EditorUtility.DisplayDialog("Splash Screen Setup", "Scene SplashScreen.unity berhasil dibuat dan ditambahkan ke Build Settings pada index 0!", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SplashScreenSetup] Gagal membuat scene SplashScreen: {ex}");
            }
        }

        public static void UpdateBuildSettings()
        {
            EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/Prologue.unity", true),
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(GameplayScenePath, true),
                new EditorBuildSettingsScene("Assets/Scenes/LevelSelect.unity", true)
            };

            EditorBuildSettings.scenes = scenes;
            Debug.Log("[SplashScreenSetup] Build Settings diperbarui: [0] Prologue, [1] MainMenu, [2] SampleScene, [3] LevelSelect");
        }

        private static Sprite LoadMenuBackgroundSprite()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(MenuBackgroundPath);
            if (assets != null)
            {
                foreach (var asset in assets)
                {
                    if (asset is Sprite s && s.name == "menuBG_0") return s;
                }
                foreach (var asset in assets)
                {
                    if (asset is Sprite s) return s;
                }
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(MenuBackgroundPath);
        }
    }
}
