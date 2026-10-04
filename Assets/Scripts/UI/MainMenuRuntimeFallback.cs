using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using MazeSpell.Audio;

namespace MazeSpell.UI
{
    /// <summary>
    /// Runtime Fallback & Layout Controller untuk Main Menu MazeSpell:
    /// - Menjamin HANYA ADA 2 BUTTON: PLAY dan LEVEL.
    /// - Menghapus seluruh tombol/elemen UI lama (Exit, AudioSettings, Logo teks duplikat, Vignette).
    /// - Menggunakan visual tombol kartun hijau sesuai gambar referensi pengguna.
    /// </summary>
    public class MainMenuRuntimeFallback : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitializeMainMenuIfNeeded()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.name != "MainMenu") return;

            // Bersihkan manager gameplay sisa jika ada saat kembali ke menu
            CleanupStrayGameplayObjects();

#if UNITY_2023_1_OR_NEWER
            var controller = Object.FindAnyObjectByType<MainMenuController>();
#else
            var controller = Object.FindObjectOfType<MainMenuController>();
#endif

            if (controller == null)
            {
                CreateRuntimeMainMenuUI();
            }
            else
            {
                EnsureTwoButtonsMenuUI(controller);
            }
        }

        private static void CleanupStrayGameplayObjects()
        {
            var ground = GameObject.Find("Ground");
            if (ground != null) Destroy(ground);

            var player = GameObject.Find("Player");
            if (player != null) Destroy(player);
        }

        public static void CreateRuntimeMainMenuUI()
        {
            // 1. Camera
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                cam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.11f, 0.18f, 1f);
            cam.orthographic = true;
            cam.orthographicSize = 5f;

            // 2. EventSystem
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

            // 3. Canvas
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
            var menuController = canvasObj.AddComponent<MainMenuController>();

            // 4. Background (Dinding batu & logo MazeSpell referensi)
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = Color.white;
            bgImage.raycastTarget = false;

            Sprite bgSprite = FindLoadedSprite("menuBG");
            if (bgSprite != null) bgImage.sprite = bgSprite;

            // 5. Tombol PLAY dan LEVEL saja
            Sprite playSprite = FindLoadedSprite("btn_cartoon_play");
            Sprite levelSprite = FindLoadedSprite("btn_cartoon_level");

            GameObject playBtnObj = CreateButtonGameObject("PlayButton", canvasObj.transform,
                new Vector2(0f, -50f), new Vector2(450f, 142f), playSprite, "PLAY");
            Button playBtn = playBtnObj.GetComponent<Button>();
            playBtn.onClick.AddListener(menuController.PlayGame);
            playBtnObj.AddComponent<UIButtonSound>();

            GameObject levelBtnObj = CreateButtonGameObject("LevelButton", canvasObj.transform,
                new Vector2(0f, -220f), new Vector2(490f, 137f), levelSprite, "LEVEL");
            Button levelBtn = levelBtnObj.GetComponent<Button>();
            levelBtn.onClick.AddListener(menuController.OpenLevelSelect);
            levelBtnObj.AddComponent<UIButtonSound>();

            // 6. Setup Navigasi Eksplisit 2 Tombol
            SetupTwoButtonNavigation(playBtn, levelBtn);

            // 7. Auto-fokus tombol PLAY
            var runner = canvasObj.AddComponent<MainMenuFocusRunner>();
            runner.targetButton = playBtn;
        }

        public static void EnsureTwoButtonsMenuUI(MainMenuController controller)
        {
            Transform canvasTransform = controller.transform;

            // HAPUS SEMUA UI, LOGO, DAN BUTTON LAMA SELAIN PLAY DAN LEVEL
            string[] toPurge = new string[]
            {
                "AudioSettingsButton",
                "ExitButton",
                "SettingsButton",
                "BackButton",
                "OptionsButton",
                "Logo",
                "Title",
                "VignetteOverlay"
            };

            foreach (var name in toPurge)
            {
                Transform t = canvasTransform.Find(name);
                if (t != null)
                {
                    Destroy(t.gameObject);
                }
            }

            // Bersihkan VignetteOverlay di dalam Background jika ada
            Transform bgT = canvasTransform.Find("Background");
            if (bgT != null)
            {
                Transform vig = bgT.Find("VignetteOverlay");
                if (vig != null) Destroy(vig.gameObject);
                Image bgImg = bgT.GetComponent<Image>();
                if (bgImg != null) bgImg.color = Color.white;
            }

            Sprite playSprite = FindLoadedSprite("btn_cartoon_play");
            Sprite levelSprite = FindLoadedSprite("btn_cartoon_level");

            // Konfigurasi PLAY Button (pertahankan desain kartun hijau murni tanpa teks)
            Transform playT = canvasTransform.Find("PlayButton");
            Button playBtn = null;
            if (playT != null)
            {
                playT.gameObject.SetActive(true);
                // Hapus semua child (seperti Text) agar tidak menimpa grafis kartun asli
                for (int i = playT.childCount - 1; i >= 0; i--)
                {
                    Destroy(playT.GetChild(i).gameObject);
                }

                RectTransform playRect = playT.GetComponent<RectTransform>();
                if (playRect != null)
                {
                    playRect.anchorMin = new Vector2(0.5f, 0.5f);
                    playRect.anchorMax = new Vector2(0.5f, 0.5f);
                    playRect.pivot = new Vector2(0.5f, 0.5f);
                    playRect.anchoredPosition = new Vector2(0f, -50f);
                    playRect.sizeDelta = new Vector2(450f, 142f);
                }

                Image playImg = playT.GetComponent<Image>();
                if (playImg != null)
                {
                    if (playSprite != null) playImg.sprite = playSprite;
                    playImg.color = Color.white;
                    playImg.preserveAspect = true;
                }

                playBtn = playT.GetComponent<Button>();
                if (playBtn != null)
                {
                    playBtn.onClick.RemoveAllListeners();
                    playBtn.onClick.AddListener(controller.PlayGame);
                }
            }

            // Konfigurasi LEVEL Button (pertahankan desain kartun hijau murni tanpa teks)
            Transform levelT = canvasTransform.Find("LevelButton");
            Button levelBtn = null;
            if (levelT != null)
            {
                levelT.gameObject.SetActive(true);
                // Hapus semua child (seperti Text)
                for (int i = levelT.childCount - 1; i >= 0; i--)
                {
                    Destroy(levelT.GetChild(i).gameObject);
                }

                RectTransform levelRect = levelT.GetComponent<RectTransform>();
                if (levelRect != null)
                {
                    levelRect.anchorMin = new Vector2(0.5f, 0.5f);
                    levelRect.anchorMax = new Vector2(0.5f, 0.5f);
                    levelRect.pivot = new Vector2(0.5f, 0.5f);
                    levelRect.anchoredPosition = new Vector2(0f, -220f);
                    levelRect.sizeDelta = new Vector2(490f, 137f);
                }

                Image levelImg = levelT.GetComponent<Image>();
                if (levelImg != null)
                {
                    if (levelSprite != null) levelImg.sprite = levelSprite;
                    levelImg.color = Color.white;
                    levelImg.preserveAspect = true;
                }

                levelBtn = levelT.GetComponent<Button>();
                if (levelBtn != null)
                {
                    levelBtn.onClick.RemoveAllListeners();
                    levelBtn.onClick.AddListener(controller.OpenLevelSelect);
                }
            }

            // Setup Navigasi Eksplisit 2 Tombol: PLAY <-> LEVEL
            if (playBtn != null && levelBtn != null)
            {
                SetupTwoButtonNavigation(playBtn, levelBtn);
            }

            var runner = controller.gameObject.GetComponent<MainMenuFocusRunner>();
            if (runner == null) runner = controller.gameObject.AddComponent<MainMenuFocusRunner>();
            runner.targetButton = playBtn;
        }

        private static void SetupTwoButtonNavigation(Button playBtn, Button levelBtn)
        {
            Navigation nPlay = new Navigation { mode = Navigation.Mode.Explicit, selectOnDown = levelBtn, selectOnUp = levelBtn };
            Navigation nLevel = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = playBtn, selectOnDown = playBtn };
            playBtn.navigation = nPlay;
            levelBtn.navigation = nLevel;
        }

        private static GameObject CreateButtonGameObject(string name, Transform parent,
            Vector2 position, Vector2 size, Sprite sprite, string fallbackLabel)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            Image img = btnObj.AddComponent<Image>();
            img.color = Color.white;
            img.raycastTarget = true;
            img.preserveAspect = true;

            if (sprite != null)
            {
                img.sprite = sprite;
            }
            else
            {
                img.color = new Color(0.15f, 0.75f, 0.45f, 1f);
                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(btnObj.transform, false);
                RectTransform textRect = textObj.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;

                TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
                tmp.text = fallbackLabel;
                tmp.fontSize = 28;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
            }

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.92f, 1f, 0.85f, 1f);
            colors.pressedColor = new Color(0.78f, 0.85f, 0.70f, 1f);
            colors.selectedColor = new Color(0.92f, 1f, 0.85f, 1f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;

            return btnObj;
        }

        private static Sprite FindLoadedSprite(string nameContains)
        {
            var allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
            if (allSprites != null)
            {
                foreach (var s in allSprites)
                {
                    if (s.name.Contains(nameContains))
                    {
                        return s;
                    }
                }
            }
            return null;
        }

        private class MainMenuFocusRunner : MonoBehaviour
        {
            public Button targetButton;

            private IEnumerator Start()
            {
                yield return null;
                if (EventSystem.current != null && targetButton != null)
                {
                    EventSystem.current.SetSelectedGameObject(null);
                    EventSystem.current.SetSelectedGameObject(targetButton.gameObject);
                }
            }

            private void Update()
            {
                if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null && targetButton != null)
                {
                    EventSystem.current.SetSelectedGameObject(targetButton.gameObject);
                }
            }
        }
    }
}
