#pragma warning disable 0649

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MazeSpell.Audio
{
    /// <summary>
    /// Mengontrol UI Audio Settings untuk game MazeSpell:
    /// - Master Volume Slider (0% - 100%)
    /// - Music Volume Slider (0% - 100%)
    /// - SFX Volume Slider (0% - 100%)
    /// - Music Mute Toggle (ON / OFF)
    /// - SFX Mute Toggle (ON / OFF)
    /// - Tombol Tutup / Kembali
    /// Mendukung penuh navigasi Gamepad (D-Pad, Left Stick, Cross/A, Circle/B),
    /// Keyboard (Panah, WASD, Enter, Space, ESC), dan Mouse.
    /// </summary>
    public class AudioSettingsController : MonoBehaviour
    {
        public static AudioSettingsController Instance { get; private set; }

        [Header("Referensi UI")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private TextMeshProUGUI masterValueText;
        [SerializeField] private TextMeshProUGUI musicValueText;
        [SerializeField] private TextMeshProUGUI sfxValueText;
        [SerializeField] private Button musicMuteButton;
        [SerializeField] private TextMeshProUGUI musicMuteText;
        [SerializeField] private Button sfxMuteButton;
        [SerializeField] private TextMeshProUGUI sfxMuteText;
        [SerializeField] private Button closeButton;

        private GameObject previousSelectedObject;
        private bool isOpen = false;
        public bool IsOpen => isOpen;

        #region Inisialisasi Otomatis di Scene

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitializeSettingsUI()
        {
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (activeScene.name == "MainMenu") return; // Audio Settings tidak boleh ada di Main Menu

            // Hanya buat jika ada Canvas di scene
            GameObject runner = new GameObject("[AudioSettingsInitializer]");
            runner.AddComponent<AudioSettingsInitializer>();
        }

        private class AudioSettingsInitializer : MonoBehaviour
        {
            private IEnumerator Start()
            {
                yield return null; // Tunggu 1 frame agar Canvas selesai dibangun

#if UNITY_2023_1_OR_NEWER
                var existing = Object.FindAnyObjectByType<AudioSettingsController>();
#else
                var existing = Object.FindObjectOfType<AudioSettingsController>();
#endif
                if (existing != null)
                {
                    Destroy(gameObject);
                    yield break;
                }

                // Cari Canvas di scene
                Transform canvasTransform = null;
#if UNITY_6000_0_OR_NEWER
                var canvases = Object.FindObjectsByType<Canvas>();
#elif UNITY_2023_1_OR_NEWER
                var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
#else
                var canvases = Object.FindObjectsOfType<Canvas>();
#endif
                if (canvases != null && canvases.Length > 0)
                {
                    // Prioritaskan GameplayCanvas atau Canvas utama
                    for (int i = 0; i < canvases.Length; i++)
                    {
                        if (canvases[i].renderMode == RenderMode.ScreenSpaceOverlay)
                        {
                            canvasTransform = canvases[i].transform;
                            break;
                        }
                    }
                    if (canvasTransform == null) canvasTransform = canvases[0].transform;
                }

                if (canvasTransform != null)
                {
                    CreateAudioSettingsPanel(canvasTransform);
                }

                Destroy(gameObject);
            }
        }

        public static AudioSettingsController CreateAudioSettingsPanel(Transform parentCanvas)
        {
            GameObject settingsRoot = new GameObject("AudioSettingsDialog");
            settingsRoot.transform.SetParent(parentCanvas, false);

            AudioSettingsController controller = settingsRoot.AddComponent<AudioSettingsController>();
            controller.BuildSettingsUI(settingsRoot.transform);
            settingsRoot.SetActive(false);

            return controller;
        }

        #endregion

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
            RefreshUIValues();
        }

        private void Update()
        {
            if (!isOpen)
            {
                // Pintasan tombol 'O' (Options / Audio Settings) pada gameplay
                if (CheckOpenShortcut())
                {
                    OpenSettings();
                }
                return;
            }

            // Input pembatalan / penutupan dialog (ESC atau Circle/B pada Gamepad)
            if (CheckCloseInput())
            {
                CloseSettings();
                return;
            }

            // Pulihkan seleksi jika klik mouse terjadi di area kosong
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null && masterSlider != null)
            {
                EventSystem.current.SetSelectedGameObject(masterSlider.gameObject);
            }
        }

        private bool CheckOpenShortcut()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.oKey.wasPressedThisFrame)
            {
                return true;
            }
#endif
            try
            {
                if (Input.GetKeyDown(KeyCode.O))
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

        private bool CheckCloseInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                return true;
            }

            var pad = Gamepad.current;
            if (pad != null && pad.buttonEast.wasPressedThisFrame)
            {
                return true;
            }
#endif
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

        public void OpenSettings(GameObject returnTarget = null)
        {
            isOpen = true;
            previousSelectedObject = returnTarget;

            if (previousSelectedObject == null && EventSystem.current != null)
            {
                previousSelectedObject = EventSystem.current.currentSelectedGameObject;
            }

            RefreshUIValues();

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
            }
            else
            {
                gameObject.SetActive(true);
            }

            // Fokuskan Master Slider untuk navigasi Gamepad/Keyboard seketika
            StartCoroutine(FocusMasterSliderDeferred());
        }

        private IEnumerator FocusMasterSliderDeferred()
        {
            yield return null;
            if (EventSystem.current != null && masterSlider != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(masterSlider.gameObject);
            }
        }

        public void CloseSettings()
        {
            isOpen = false;

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }

            // Pulihkan fokus ke UI sebelumnya
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                if (previousSelectedObject != null && previousSelectedObject.activeInHierarchy)
                {
                    EventSystem.current.SetSelectedGameObject(previousSelectedObject);
                }
            }

            AudioManager.PlayUIStatic(AudioManager.SFXType.ButtonClick, 0.8f);
        }

        public void RefreshUIValues()
        {
            if (AudioManager.Instance == null) return;

            if (masterSlider != null) masterSlider.value = AudioManager.Instance.MasterVolume;
            if (musicSlider != null) musicSlider.value = AudioManager.Instance.MusicVolume;
            if (sfxSlider != null) sfxSlider.value = AudioManager.Instance.SFXVolume;

            UpdateMasterLabel(AudioManager.Instance.MasterVolume);
            UpdateMusicLabel(AudioManager.Instance.MusicVolume);
            UpdateSFXLabel(AudioManager.Instance.SFXVolume);
            UpdateMusicMuteButtonVisual();
            UpdateSFXMuteButtonVisual();
        }

        #region Event Handlers UI

        private void OnMasterSliderChanged(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMasterVolume(value);
            }
            UpdateMasterLabel(value);
        }

        private void OnMusicSliderChanged(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMusicVolume(value);
            }
            UpdateMusicLabel(value);
        }

        private void OnSFXSliderChanged(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetSFXVolume(value);
            }
            UpdateSFXLabel(value);
        }

        private void OnToggleMusicMuteClicked()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.ToggleMusicMute();
                UpdateMusicMuteButtonVisual();
                AudioManager.PlayUIStatic(AudioManager.SFXType.ButtonClick, 0.8f);
            }
        }

        private void OnToggleSFXMuteClicked()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.ToggleSFXMute();
                UpdateSFXMuteButtonVisual();
                AudioManager.PlayUIStatic(AudioManager.SFXType.ButtonClick, 0.8f);
            }
        }

        private void UpdateMasterLabel(float val)
        {
            if (masterValueText != null) masterValueText.text = $"{Mathf.RoundToInt(val * 100f)}%";
        }

        private void UpdateMusicLabel(float val)
        {
            if (musicValueText != null) musicValueText.text = $"{Mathf.RoundToInt(val * 100f)}%";
        }

        private void UpdateSFXLabel(float val)
        {
            if (sfxValueText != null) sfxValueText.text = $"{Mathf.RoundToInt(val * 100f)}%";
        }

        private void UpdateMusicMuteButtonVisual()
        {
            if (AudioManager.Instance == null) return;
            bool isMuted = AudioManager.Instance.IsMusicMuted;
            if (musicMuteText != null)
            {
                musicMuteText.text = isMuted ? "MUSIC: <b>OFF</b>" : "MUSIC: <b>ON</b>";
                musicMuteText.color = isMuted ? new Color(0.95f, 0.40f, 0.40f, 1f) : new Color(0.35f, 0.90f, 0.55f, 1f);
            }
        }

        private void UpdateSFXMuteButtonVisual()
        {
            if (AudioManager.Instance == null) return;
            bool isMuted = AudioManager.Instance.IsSFXMuted;
            if (sfxMuteText != null)
            {
                sfxMuteText.text = isMuted ? "SFX: <b>OFF</b>" : "SFX: <b>ON</b>";
                sfxMuteText.color = isMuted ? new Color(0.95f, 0.40f, 0.40f, 1f) : new Color(0.35f, 0.90f, 0.55f, 1f);
            }
        }

        #endregion

        #region Konstruksi UI Panel Modern

        public void BuildSettingsUI(Transform root)
        {
            TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;

            // 1. Root Overlay
            RectTransform rootRect = root.gameObject.GetComponent<RectTransform>();
            if (rootRect == null) rootRect = root.gameObject.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image dimBg = root.gameObject.AddComponent<Image>();
            dimBg.color = new Color(0.03f, 0.05f, 0.09f, 0.92f); // Dark translucent backdrop
            dimBg.raycastTarget = true;
            settingsPanel = root.gameObject;

            // 2. Card Panel Tengah
            GameObject card = new GameObject("SettingsCard");
            card.transform.SetParent(root, false);
            RectTransform cardRect = card.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(620f, 480f);
            cardRect.anchoredPosition = Vector2.zero;

            Image cardImg = card.AddComponent<Image>();
            cardImg.color = new Color(0.07f, 0.12f, 0.20f, 0.96f);

            // 3. Header Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(card.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.sizeDelta = new Vector2(580f, 50f);
            titleRect.anchoredPosition = new Vector2(0f, 195f);

            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) titleTmp.font = defaultFont;
            titleTmp.text = "<b>AUDIO SETTINGS</b>";
            titleTmp.fontSize = 32;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(0.99f, 0.85f, 0.35f, 1f); // Warm Gold
            titleTmp.characterSpacing = 2f;

            // 4. Slider 1: Master Volume
            float startY = 130f;
            float stepY = 62f;

            masterSlider = CreateSliderRow("MasterSliderRow", card.transform, new Vector2(0f, startY),
                "MASTER VOLUME", out masterValueText, defaultFont);
            masterSlider.onValueChanged.AddListener(OnMasterSliderChanged);

            // Slider 2: Music Volume
            musicSlider = CreateSliderRow("MusicSliderRow", card.transform, new Vector2(0f, startY - stepY),
                "MUSIC VOLUME", out musicValueText, defaultFont);
            musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);

            // Slider 3: SFX Volume
            sfxSlider = CreateSliderRow("SFXSliderRow", card.transform, new Vector2(0f, startY - stepY * 2),
                "SFX VOLUME", out sfxValueText, defaultFont);
            sfxSlider.onValueChanged.AddListener(OnSFXSliderChanged);

            // 5. Toggles Row (Music ON/OFF & SFX ON/OFF)
            float toggleY = startY - stepY * 3 - 5f;

            // Toggle Music
            GameObject musicBtnObj = CreateButton("MusicMuteBtn", card.transform,
                new Vector2(-130f, toggleY), new Vector2(230f, 48f),
                "MUSIC: <b>ON</b>",
                new Color(0.12f, 0.32f, 0.48f, 1f),
                new Color(0.20f, 0.45f, 0.68f, 1f),
                new Color(0.08f, 0.22f, 0.35f, 1f),
                out musicMuteText, defaultFont);
            musicMuteButton = musicBtnObj.GetComponent<Button>();
            musicMuteButton.onClick.AddListener(OnToggleMusicMuteClicked);

            // Toggle SFX
            GameObject sfxBtnObj = CreateButton("SFXMuteBtn", card.transform,
                new Vector2(130f, toggleY), new Vector2(230f, 48f),
                "SFX: <b>ON</b>",
                new Color(0.12f, 0.32f, 0.48f, 1f),
                new Color(0.20f, 0.45f, 0.68f, 1f),
                new Color(0.08f, 0.22f, 0.35f, 1f),
                out sfxMuteText, defaultFont);
            sfxMuteButton = sfxBtnObj.GetComponent<Button>();
            sfxMuteButton.onClick.AddListener(OnToggleSFXMuteClicked);

            // 6. Tombol Close / Back
            float closeY = toggleY - 60f;
            GameObject closeBtnObj = CreateButton("CloseBtn", card.transform,
                new Vector2(0f, closeY), new Vector2(300f, 50f),
                "◀  KEMBALI",
                new Color(0.10f, 0.62f, 0.40f, 1f),
                new Color(0.18f, 0.78f, 0.50f, 1f),
                new Color(0.06f, 0.45f, 0.28f, 1f),
                out _, defaultFont);
            closeButton = closeBtnObj.GetComponent<Button>();
            closeButton.onClick.AddListener(CloseSettings);

            // 7. Setup Navigasi Gamepad & Keyboard
            SetupDialogNavigation();
        }

        private Slider CreateSliderRow(string name, Transform parent, Vector2 pos, string label,
            out TextMeshProUGUI valueTmp, TMP_FontAsset font)
        {
            GameObject rowObj = new GameObject(name);
            rowObj.transform.SetParent(parent, false);
            RectTransform rowRect = rowObj.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.5f, 0.5f);
            rowRect.anchorMax = new Vector2(0.5f, 0.5f);
            rowRect.sizeDelta = new Vector2(520f, 45f);
            rowRect.anchoredPosition = pos;

            // Label Kiri
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(rowObj.transform, false);
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(0.38f, 1f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
            if (font != null) labelTmp.font = font;
            labelTmp.text = label;
            labelTmp.fontSize = 16;
            labelTmp.fontStyle = FontStyles.Bold;
            labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
            labelTmp.color = new Color(0.85f, 0.90f, 0.98f, 1f);

            // Nilai Persentase Kanan
            GameObject valObj = new GameObject("ValueText");
            valObj.transform.SetParent(rowObj.transform, false);
            RectTransform valRect = valObj.AddComponent<RectTransform>();
            valRect.anchorMin = new Vector2(0.86f, 0f);
            valRect.anchorMax = new Vector2(1f, 1f);
            valRect.offsetMin = Vector2.zero;
            valRect.offsetMax = Vector2.zero;

            valueTmp = valObj.AddComponent<TextMeshProUGUI>();
            if (font != null) valueTmp.font = font;
            valueTmp.text = "100%";
            valueTmp.fontSize = 16;
            valueTmp.fontStyle = FontStyles.Bold;
            valueTmp.alignment = TextAlignmentOptions.MidlineRight;
            valueTmp.color = new Color(0.99f, 0.85f, 0.35f, 1f);

            // Slider Tengah
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(rowObj.transform, false);
            RectTransform sRect = sliderObj.AddComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0.40f, 0.25f);
            sRect.anchorMax = new Vector2(0.84f, 0.75f);
            sRect.offsetMin = Vector2.zero;
            sRect.offsetMax = Vector2.zero;

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.value = 1f;

            // Background Slider
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0.3f);
            bgRect.anchorMax = new Vector2(1f, 0.7f);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.20f, 0.30f, 0.9f);

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.3f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.7f);
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.20f, 0.65f, 0.95f, 1f);

            slider.fillRect = fillRect;

            // Handle Area
            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderObj.transform, false);
            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = Vector2.zero;
            handleAreaRect.offsetMax = Vector2.zero;

            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            RectTransform handleRect = handle.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(22f, 22f);
            Image handleImg = handle.AddComponent<Image>();
            handleImg.color = new Color(0.99f, 0.85f, 0.35f, 1f);

            slider.handleRect = handleRect;
            slider.targetGraphic = handleImg;

            ColorBlock cb = slider.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1f, 1f, 0.7f, 1f);
            cb.selectedColor = new Color(1f, 1f, 0.7f, 1f);
            slider.colors = cb;

            return slider;
        }

        private GameObject CreateButton(string name, Transform parent, Vector2 pos, Vector2 size,
            string label, Color normal, Color hover, Color press, out TextMeshProUGUI btnTmp, TMP_FontAsset font)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = pos;

            Image img = btnObj.AddComponent<Image>();
            img.color = normal;

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = normal;
            cb.highlightedColor = hover;
            cb.selectedColor = hover;
            cb.pressedColor = press;
            cb.fadeDuration = 0.08f;
            btn.colors = cb;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            btnTmp = textObj.AddComponent<TextMeshProUGUI>();
            if (font != null) btnTmp.font = font;
            btnTmp.text = label;
            btnTmp.fontSize = 18;
            btnTmp.fontStyle = FontStyles.Bold;
            btnTmp.alignment = TextAlignmentOptions.Center;
            btnTmp.color = Color.white;
            btnTmp.raycastTarget = false;

            return btnObj;
        }

        private void SetupDialogNavigation()
        {
            if (masterSlider == null || musicSlider == null || sfxSlider == null ||
                musicMuteButton == null || sfxMuteButton == null || closeButton == null) return;

            // 1. Master Slider
            Navigation navMaster = new Navigation { mode = Navigation.Mode.Explicit, selectOnDown = musicSlider, selectOnUp = closeButton };
            masterSlider.navigation = navMaster;

            // 2. Music Slider
            Navigation navMusic = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = masterSlider, selectOnDown = sfxSlider };
            musicSlider.navigation = navMusic;

            // 3. SFX Slider
            Navigation navSFX = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = musicSlider, selectOnDown = musicMuteButton };
            sfxSlider.navigation = navSFX;

            // 4. Music Mute Button
            Navigation navMusicMute = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = sfxSlider, selectOnDown = closeButton, selectOnRight = sfxMuteButton, selectOnLeft = sfxMuteButton };
            musicMuteButton.navigation = navMusicMute;

            // 5. SFX Mute Button
            Navigation navSFXMute = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = sfxSlider, selectOnDown = closeButton, selectOnLeft = musicMuteButton, selectOnRight = musicMuteButton };
            sfxMuteButton.navigation = navSFXMute;

            // 6. Close Button
            Navigation navClose = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = musicMuteButton, selectOnDown = masterSlider };
            closeButton.navigation = navClose;
        }

        #endregion
    }

    /// <summary>
    /// Helper component opsional yang dapat dipasang pada Button mana pun
    /// untuk memicu sound effect Hover dan Click secara konsisten.
    /// </summary>
    public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, ISubmitHandler
    {
        public void OnPointerEnter(PointerEventData eventData)
        {
            AudioManager.PlayUIStatic(AudioManager.SFXType.ButtonHover, 0.7f);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            AudioManager.PlayUIStatic(AudioManager.SFXType.ButtonClick, 0.85f);
        }

        public void OnSubmit(BaseEventData eventData)
        {
            AudioManager.PlayUIStatic(AudioManager.SFXType.ButtonClick, 0.85f);
        }
    }
}
