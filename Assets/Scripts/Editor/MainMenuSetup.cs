using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace MazeSpell.Editor
{
    /// <summary>
    /// Editor script untuk mengotomatisasi pembuatan dan perapihan scene MainMenu dan LevelSelect di MazeSpell.
    /// Memastikan hirarki tepat, komponen terhubung, dan pengaturan Build Settings benar.
    /// </summary>
    [InitializeOnLoad]
    public static class MainMenuSetup
    {
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string GameplayScenePath = "Assets/Scenes/SampleScene.unity";
        private const string LevelSelectScenePath = "Assets/Scenes/LevelSelect.unity";
        private const string LegacyLevelSelectScenePath = "Assets/Ui/Screens/LevelSelect.unity";
        private const string SplashScreenScenePath = "Assets/Scenes/SplashScreen.unity";
        private const string MenuBackgroundPath = "Assets/Ui/Background/menuBG.jpeg";
        private const string ButtonHomePath = "Assets/Ui/Button/button_home.png";

        static MainMenuSetup()
        {
            EditorApplication.delayCall += AutoSetupIfNeeded;
        }

        private static void AutoSetupIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            // Selalu pastikan Build Settings terdaftar dengan tepat
            SetupBuildSettings();

            // Jalankan setup otomatis jika belum pernah sukses dilakukan pada sesi ini
            if (!SessionState.GetBool("MazeSpell_MainMenuSetupCompleted_V3", false))
            {
                SessionState.SetBool("MazeSpell_MainMenuSetupCompleted_V3", true);
                SetupMainMenuAndScreens(false);
            }
        }

        [MenuItem("MazeSpell/Setup Main Menu & Level Select")]
        public static void SetupMainMenuAndScreensManual()
        {
            SetupMainMenuAndScreens(true);
        }

        public static void SetupMainMenuAndScreens(bool interactive)
        {
            try
            {
                Debug.Log("[MazeSpell Setup] Memulai setup scene MainMenu dan LevelSelect...");

                SetupBuildSettings();
                SetupMainMenuScene();
                SetupLevelSelectScene();

                // Buka kembali scene MainMenu sebagai scene aktif di editor
                EditorSceneManager.OpenScene(MainMenuScenePath);

                Debug.Log("[MazeSpell Setup] Setup MainMenu dan LevelSelect berhasil diselesaikan!");
                if (interactive)
                {
                    EditorUtility.DisplayDialog("MazeSpell Main Menu", "Sistem Main Menu dan Level Select berhasil dirapikan dan disimpan!", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MazeSpell Setup] Gagal melakukan setup: {ex}");
            }
        }

        public static void SetupBuildSettings()
        {
            EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/Prologue.unity", true),
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(GameplayScenePath, true),
                new EditorBuildSettingsScene(LevelSelectScenePath, true)
            };

            EditorBuildSettings.scenes = scenes;
            Debug.Log("[MazeSpell Setup] Build Settings diperbarui: [0] Prologue, [1] MainMenu, [2] SampleScene, [3] LevelSelect");
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

        private static Sprite LoadHomeButtonSprite()
        {
            UnityEngine.Object[] objects = AssetDatabase.LoadAllAssetsAtPath(ButtonHomePath);
            if (objects != null)
            {
                foreach (var obj in objects)
                {
                    if (obj is Sprite sprite) return sprite;
                }
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(ButtonHomePath);
        }

        private static void SetupMainMenuScene()
        {
            var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);

            // 1. Bersihkan objek lama selain Main Camera
            var rootObjects = scene.GetRootGameObjects();
            GameObject mainCamObj = null;

            foreach (var go in rootObjects)
            {
                if (go.name == "Main Camera")
                {
                    mainCamObj = go;
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }

            // 2. Setup Main Camera
            if (mainCamObj == null)
            {
                mainCamObj = new GameObject("Main Camera");
                mainCamObj.tag = "MainCamera";
                mainCamObj.AddComponent<Camera>();
                mainCamObj.AddComponent<AudioListener>();
            }

            Camera cam = mainCamObj.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.11f, 0.18f, 1f); // Dark slate blue
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            mainCamObj.transform.position = new Vector3(0, 0, -10);

            if (mainCamObj.GetComponent<AudioListener>() == null)
            {
                mainCamObj.AddComponent<AudioListener>();
            }

            // 3. Setup Canvas
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
            var menuController = canvasObj.AddComponent<MazeSpell.UI.MainMenuController>();

            // 4. Background (menuBG)
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
            bgImage.color = Color.white;
            bgImage.raycastTarget = false;

            // Aturan ketat: TIDAK ADA Logo, TIDAK ADA VignetteOverlay, TIDAK ADA AudioSettings/Exit

            Sprite playSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Ui/Button/btn_cartoon_play.png");
            Sprite levelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Ui/Button/btn_cartoon_level.png");

            // 5. PlayButton (Cartoon Green Button)
            GameObject playBtnObj = new GameObject("PlayButton");
            playBtnObj.transform.SetParent(canvasObj.transform, false);
            RectTransform playRect = playBtnObj.AddComponent<RectTransform>();
            playRect.anchorMin = new Vector2(0.5f, 0.5f);
            playRect.anchorMax = new Vector2(0.5f, 0.5f);
            playRect.pivot = new Vector2(0.5f, 0.5f);
            playRect.sizeDelta = new Vector2(450f, 142f);
            playRect.anchoredPosition = new Vector2(0f, -50f);

            Image playImg = playBtnObj.AddComponent<Image>();
            playImg.color = Color.white;
            playImg.preserveAspect = true;
            if (playSprite != null) playImg.sprite = playSprite;

            Button playBtn = playBtnObj.AddComponent<Button>();
            ColorBlock playColors = playBtn.colors;
            playColors.normalColor = Color.white;
            playColors.highlightedColor = new Color(0.92f, 1f, 0.85f, 1f);
            playColors.pressedColor = new Color(0.78f, 0.85f, 0.70f, 1f);
            playColors.selectedColor = new Color(0.92f, 1f, 0.85f, 1f);
            playColors.fadeDuration = 0.08f;
            playBtn.colors = playColors;
            UnityEventTools.AddPersistentListener(playBtn.onClick, menuController.PlayGame);

            // 6. LevelButton (Cartoon Green Button)
            GameObject levelBtnObj = new GameObject("LevelButton");
            levelBtnObj.transform.SetParent(canvasObj.transform, false);
            RectTransform levelRect = levelBtnObj.AddComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(0.5f, 0.5f);
            levelRect.anchorMax = new Vector2(0.5f, 0.5f);
            levelRect.pivot = new Vector2(0.5f, 0.5f);
            levelRect.sizeDelta = new Vector2(490f, 137f);
            levelRect.anchoredPosition = new Vector2(0f, -220f);

            Image levelImg = levelBtnObj.AddComponent<Image>();
            levelImg.color = Color.white;
            levelImg.preserveAspect = true;
            if (levelSprite != null) levelImg.sprite = levelSprite;

            Button levelBtn = levelBtnObj.AddComponent<Button>();
            ColorBlock levelColors = levelBtn.colors;
            levelColors.normalColor = Color.white;
            levelColors.highlightedColor = new Color(0.92f, 1f, 0.85f, 1f);
            levelColors.pressedColor = new Color(0.78f, 0.85f, 0.70f, 1f);
            levelColors.selectedColor = new Color(0.92f, 1f, 0.85f, 1f);
            levelColors.fadeDuration = 0.08f;
            levelBtn.colors = levelColors;
            UnityEventTools.AddPersistentListener(levelBtn.onClick, menuController.OpenLevelSelect);

            // Setup Navigation (Hanya 2 Tombol: PLAY <-> LEVEL)
            Navigation nPlay = new Navigation { mode = Navigation.Mode.Explicit, selectOnDown = levelBtn, selectOnUp = levelBtn };
            Navigation nLevel = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = playBtn, selectOnDown = playBtn };
            playBtn.navigation = nPlay;
            levelBtn.navigation = nLevel;

            // 10. EventSystem
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            eventSystemObj.AddComponent<InputSystemUIInputModule>();
#else
            eventSystemObj.AddComponent<StandaloneInputModule>();
#endif

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[MazeSpell Setup] Scene MainMenu berhasil dibuat dan disimpan!");
        }

        private static void SetupLevelSelectScene()
        {
            var scene = EditorSceneManager.OpenScene(LevelSelectScenePath, OpenSceneMode.Single);

            // Bersihkan semua objek lama
            var rootObjects = scene.GetRootGameObjects();
            GameObject mainCamObj = null;

            foreach (var go in rootObjects)
            {
                if (go.name == "Main Camera")
                {
                    mainCamObj = go;
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }

            if (mainCamObj == null)
            {
                mainCamObj = new GameObject("Main Camera");
                mainCamObj.tag = "MainCamera";
                mainCamObj.AddComponent<Camera>();
                mainCamObj.AddComponent<AudioListener>();
            }

            Camera cam = mainCamObj.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.11f, 0.18f, 1f);
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            mainCamObj.transform.position = new Vector3(0, 0, -10);

            // Canvas
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
            var selectController = canvasObj.AddComponent<MazeSpell.UI.LevelSelectController>();

            // Background
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
            }
            bgImage.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            bgImage.raycastTarget = false;

            // Vignette overlay
            GameObject vignetteObj = new GameObject("VignetteOverlay");
            vignetteObj.transform.SetParent(bgObj.transform, false);
            RectTransform vigRect = vignetteObj.AddComponent<RectTransform>();
            vigRect.anchorMin = Vector2.zero;
            vigRect.anchorMax = Vector2.one;
            vigRect.offsetMin = Vector2.zero;
            vigRect.offsetMax = Vector2.zero;
            Image vigImage = vignetteObj.AddComponent<Image>();
            vigImage.color = new Color(0.04f, 0.07f, 0.13f, 0.45f);
            vigImage.raycastTarget = false;

            // Header Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(canvasObj.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.sizeDelta = new Vector2(800, 120);
            titleRect.anchoredPosition = new Vector2(0, 240);

            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont != null) titleText.font = defaultFont;
            titleText.text = "<b>PILIH LEVEL</b>\n<size=24><color=#E2E8F0>Pilih labirin untuk memulai petualangan kata</color></size>";
            titleText.fontSize = 58;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(0.99f, 0.85f, 0.35f, 1f);
            titleText.raycastTarget = false;
            titleObj.SetActive(false); // Sembunyikan Title agar bersih sesuai gambar referensi

            // Level Buttons Container
            GameObject buttonsContainer = new GameObject("LevelButtonsContainer");
            buttonsContainer.transform.SetParent(canvasObj.transform, false);
            RectTransform contRect = buttonsContainer.AddComponent<RectTransform>();
            contRect.anchorMin = new Vector2(0.5f, 0.5f);
            contRect.anchorMax = new Vector2(0.5f, 0.5f);
            contRect.sizeDelta = new Vector2(1920, 1080);
            contRect.anchoredPosition = Vector2.zero;

            Vector2[] buttonPositions = new Vector2[]
            {
                new Vector2(-470f, 150f), // Level 1
                new Vector2(0f, 150f),     // Level 2
                new Vector2(470f, 150f),  // Level 3
                new Vector2(-235f, -140f), // Level 4
                new Vector2(235f, -140f)   // Level 5
            };
            Vector2 buttonSize = new Vector2(400f, 175f);

            for (int i = 1; i <= 5; i++)
            {
                int levelIndex = i;
                GameObject lvlBtnObj = new GameObject($"Level_{i}_Button");
                lvlBtnObj.transform.SetParent(buttonsContainer.transform, false);
                RectTransform rect = lvlBtnObj.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = buttonSize;
                rect.anchoredPosition = buttonPositions[i - 1];

                Image img = lvlBtnObj.AddComponent<Image>();
                img.color = Color.white;
                img.preserveAspect = true;
                Sprite sp = selectController.GetUnlockedSprite(i);
                if (sp != null) img.sprite = sp;

                Button btn = lvlBtnObj.AddComponent<Button>();
                UnityEventTools.AddIntPersistentListener(btn.onClick, selectController.SelectLevel, levelIndex);
            }

            // Back / Home Button
            GameObject homeBtnObj = new GameObject("HomeButton");
            homeBtnObj.transform.SetParent(canvasObj.transform, false);
            RectTransform homeRect = homeBtnObj.AddComponent<RectTransform>();
            homeRect.anchorMin = new Vector2(0.5f, 0.5f);
            homeRect.anchorMax = new Vector2(0.5f, 0.5f);
            homeRect.sizeDelta = new Vector2(260, 64);
            homeRect.anchoredPosition = new Vector2(0, -390);

            Image homeBg = homeBtnObj.AddComponent<Image>();
            homeBg.color = new Color(0.25f, 0.3f, 0.4f, 1f);

            Button homeBtn = homeBtnObj.AddComponent<Button>();
            ColorBlock cb = homeBtn.colors;
            cb.normalColor = new Color(0.25f, 0.3f, 0.4f, 1f);
            cb.highlightedColor = new Color(0.35f, 0.42f, 0.55f, 1f);
            cb.pressedColor = new Color(0.18f, 0.22f, 0.3f, 1f);
            homeBtn.colors = cb;

            UnityEventTools.AddPersistentListener(homeBtn.onClick, selectController.BackToMainMenu);

            // Icon Home jika ada
            Sprite homeSprite = LoadHomeButtonSprite();
            if (homeSprite != null)
            {
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(homeBtnObj.transform, false);
                RectTransform iconRect = iconObj.AddComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0, 0.5f);
                iconRect.anchorMax = new Vector2(0, 0.5f);
                iconRect.sizeDelta = new Vector2(40, 40);
                iconRect.anchoredPosition = new Vector2(32, 0);
                Image iconImg = iconObj.AddComponent<Image>();
                iconImg.sprite = homeSprite;
                iconImg.raycastTarget = false;
            }

            // Text Kembali
            GameObject homeTextObj = new GameObject("Text");
            homeTextObj.transform.SetParent(homeBtnObj.transform, false);
            RectTransform homeTextRect = homeTextObj.AddComponent<RectTransform>();
            homeTextRect.anchorMin = Vector2.zero;
            homeTextRect.anchorMax = Vector2.one;
            homeTextRect.offsetMin = new Vector2(40, 0);
            homeTextRect.offsetMax = Vector2.zero;

            TextMeshProUGUI homeText = homeTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) homeText.font = defaultFont;
            homeText.text = "MENU UTAMA";
            homeText.fontSize = 22;
            homeText.fontStyle = FontStyles.Bold;
            homeText.alignment = TextAlignmentOptions.Center;
            homeText.color = Color.white;
            homeText.raycastTarget = false;

            // EventSystem
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            eventSystemObj.AddComponent<InputSystemUIInputModule>();
#else
            eventSystemObj.AddComponent<StandaloneInputModule>();
#endif

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (!string.IsNullOrEmpty(LegacyLevelSelectScenePath) && File.Exists(LevelSelectScenePath))
            {
                File.Copy(LevelSelectScenePath, LegacyLevelSelectScenePath, true);
            }
            Debug.Log("[MazeSpell Setup] Scene LevelSelect berhasil dibuat dan disimpan!");
        }

        private static GameObject CreateStyledButton(
            string name,
            Transform parent,
            Vector2 position,
            Vector2 size,
            string label,
            Color normalColor,
            Color hoverColor,
            Color pressColor,
            float fontSize)
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
            colors.selectedColor = normalColor;
            colors.fadeDuration = 0.1f;
            btn.colors = colors;

            // Text Label
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont != null) tmp.font = defaultFont;
            tmp.text = label;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            return btnObj;
        }
    }
}
