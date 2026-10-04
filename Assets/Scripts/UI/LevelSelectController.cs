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
using MazeSpell.Audio;

namespace MazeSpell.UI
{
    /// <summary>
    /// Mengontrol tampilan dan interaksi halaman Level Select game MazeSpell.
    /// Menggunakan 5 tombol level cartoon (Level 1-5) yang disusun dalam 2 baris
    /// (Baris 1: Level 1-3, Baris 2: Level 4-5) sesuai gambar referensi.
    /// Mendukung buka/tutup level berbasis progres PlayerPrefs dari LevelProgressionManager.
    /// Level yang terkunci menampilkan visual gembok dan tidak dapat ditekan.
    /// </summary>
    public class LevelSelectController : MonoBehaviour
    {
        public static LevelSelectController Instance { get; private set; }

        [Header("Konfigurasi Scene")]
        [Tooltip("Nama scene Main Menu")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        [Tooltip("Nama scene gameplay utama")]
        [SerializeField] private string gameplaySceneName = "SampleScene";

        [Header("Asset Sprite Tombol Level (Opsional/Inspector)")]
        [SerializeField] private Sprite[] levelSprites = new Sprite[5];
        [SerializeField] private Sprite lockedSprite;

        private static int requestedLevel = 1;
        private Button firstSelectedButton;
        private Button[] activeLevelButtons = new Button[5];
        private Button activeHomeButton;

        private static Sprite[] cachedUnlockedSprites;
        private static Sprite cachedLockedSprite;

        #region Inisialisasi Otomatis Runtime

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreateLevelSelectIfNeeded()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != "LevelSelect") return;

#if UNITY_2023_1_OR_NEWER
            if (Object.FindAnyObjectByType<LevelSelectController>() != null) return;
#else
            if (Object.FindObjectOfType<LevelSelectController>() != null) return;
#endif

            CreateRuntimeLevelSelectUI();
        }

        public static void CreateRuntimeLevelSelectUI()
        {
            // 1. Pastikan EventSystem tersedia
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
            var controller = canvasObj.AddComponent<LevelSelectController>();

            // 3. Background (Stone Wall / menuBG)
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            bgImage.raycastTarget = false;

            // Cari sprite menuBG jika ada
            var allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
            if (allSprites != null)
            {
                foreach (var s in allSprites)
                {
                    if (s != null && (s.name == "menuBG_0" || s.name == "menuBG"))
                    {
                        bgImage.sprite = s;
                        break;
                    }
                }
            }

            // Vignette Overlay
            GameObject vigObj = new GameObject("VignetteOverlay");
            vigObj.transform.SetParent(bgObj.transform, false);
            RectTransform vigRect = vigObj.AddComponent<RectTransform>();
            vigRect.anchorMin = Vector2.zero;
            vigRect.anchorMax = Vector2.one;
            vigRect.offsetMin = Vector2.zero;
            vigRect.offsetMax = Vector2.zero;
            Image vigImage = vigObj.AddComponent<Image>();
            vigImage.color = new Color(0.04f, 0.07f, 0.13f, 0.35f);
            vigImage.raycastTarget = false;

            // 4. Level Buttons Container
            GameObject buttonsContainer = new GameObject("LevelButtonsContainer");
            buttonsContainer.transform.SetParent(canvasObj.transform, false);
            RectTransform contRect = buttonsContainer.AddComponent<RectTransform>();
            contRect.anchorMin = new Vector2(0.5f, 0.5f);
            contRect.anchorMax = new Vector2(0.5f, 0.5f);
            contRect.sizeDelta = new Vector2(1920f, 1080f);
            contRect.anchoredPosition = Vector2.zero;

            // Buat 5 Button Slot
            for (int i = 1; i <= 5; i++)
            {
                GameObject btnObj = new GameObject($"Level_{i}_Button");
                btnObj.transform.SetParent(buttonsContainer.transform, false);
                btnObj.AddComponent<RectTransform>();
                btnObj.AddComponent<Image>();
                btnObj.AddComponent<Button>();
                btnObj.AddComponent<UIButtonSound>();
            }

            // 5. Home / Back Button
            GameObject homeBtnObj = new GameObject("HomeButton");
            homeBtnObj.transform.SetParent(canvasObj.transform, false);
            RectTransform homeRect = homeBtnObj.AddComponent<RectTransform>();
            homeRect.anchorMin = new Vector2(0.5f, 0.5f);
            homeRect.anchorMax = new Vector2(0.5f, 0.5f);
            homeRect.sizeDelta = new Vector2(260f, 64f);
            homeRect.anchoredPosition = new Vector2(0f, -390f);

            Image homeImg = homeBtnObj.AddComponent<Image>();
            homeImg.color = new Color(0.20f, 0.26f, 0.36f, 0.95f);

            Button homeBtn = homeBtnObj.AddComponent<Button>();
            ColorBlock cb = homeBtn.colors;
            cb.normalColor = new Color(0.20f, 0.26f, 0.36f, 0.95f);
            cb.highlightedColor = new Color(0.30f, 0.40f, 0.55f, 1f);
            cb.pressedColor = new Color(0.12f, 0.18f, 0.26f, 1f);
            homeBtn.colors = cb;
            homeBtn.onClick.AddListener(controller.BackToMainMenu);
            homeBtnObj.AddComponent<UIButtonSound>();

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(homeBtnObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont != null) tmp.font = defaultFont;
            tmp.text = "◀  MENU UTAMA";
            tmp.fontSize = 22;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            // 6. Refresh dan atur tampilan visual sesuai progres
            controller.RefreshLevelButtonsUI();
        }

        #endregion

        #region Lifecycle & Refresh UI

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            CleanupGameplayManagersInMenu();
        }

        private void Start()
        {
            CleanupGameplayManagersInMenu();
            RefreshLevelButtonsUI();
            StartCoroutine(FocusFirstButtonNextFrame());
        }

        private void OnEnable()
        {
            RefreshLevelButtonsUI();
        }

        /// <summary>
        /// Mengatur tata letak 2 baris (Baris 1: Level 1-3, Baris 2: Level 4-5),
        /// menerapkan gambar level terbuka atau terkunci, serta mengatur interaksi tombol.
        /// </summary>
        public void RefreshLevelButtonsUI()
        {
            // Sembunyikan Title lama jika ada di scene
            var titleObj = GameObject.Find("Title");
            if (titleObj != null)
            {
                titleObj.SetActive(false);
            }

            // Koordinat tombol 2 baris (resolusi referensi 1920x1080)
            // Baris 1: Level 1, 2, 3 di Y = +150
            // Baris 2: Level 4, 5 di Y = -140
            Vector2[] buttonPositions = new Vector2[]
            {
                new Vector2(-470f, 150f), // Level 1
                new Vector2(0f, 150f),     // Level 2
                new Vector2(470f, 150f),  // Level 3
                new Vector2(-235f, -140f), // Level 4
                new Vector2(235f, -140f)   // Level 5
            };
            Vector2 buttonSize = new Vector2(400f, 175f);

            int highestUnlocked = LevelProgressionManager.HighestUnlockedLevel;

            for (int i = 0; i < 5; i++)
            {
                int levelNum = i + 1;
                bool isUnlocked = (levelNum <= highestUnlocked);

                // Temukan GameObject tombol level
                GameObject btnObj = GameObject.Find($"Level_{levelNum}_Button");
                if (btnObj == null)
                {
                    Transform t = transform.Find($"LevelButtonsContainer/Level_{levelNum}_Button");
                    if (t == null) t = transform.Find($"Level_{levelNum}_Button");
                    if (t != null) btnObj = t.gameObject;
                }

                if (btnObj == null) continue;

                // Atur posisi dan ukuran
                RectTransform rect = btnObj.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.sizeDelta = buttonSize;
                    rect.anchoredPosition = buttonPositions[i];
                    rect.localScale = Vector3.one;
                }

                // Hapus / nonaktifkan teks lama (Text atau TextMeshProUGUI)
                var tmps = btnObj.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var tmp in tmps)
                {
                    tmp.gameObject.SetActive(false);
                }
                var legacyTexts = btnObj.GetComponentsInChildren<Text>(true);
                foreach (var txt in legacyTexts)
                {
                    txt.gameObject.SetActive(false);
                }

                // Atur Image Sprite
                Image img = btnObj.GetComponent<Image>();
                if (img == null) img = btnObj.AddComponent<Image>();
                img.preserveAspect = true;
                img.color = Color.white;
                img.raycastTarget = true;

                Sprite targetSprite = isUnlocked ? GetUnlockedSprite(levelNum) : GetLockedSprite();
                if (targetSprite != null)
                {
                    img.sprite = targetSprite;
                }

                // Atur Button
                Button btn = btnObj.GetComponent<Button>();
                if (btn == null) btn = btnObj.AddComponent<Button>();

                btn.targetGraphic = img;
                btn.interactable = isUnlocked;

                ColorBlock cb = btn.colors;
                cb.normalColor = Color.white;
                cb.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
                cb.selectedColor = new Color(1.05f, 1.05f, 1.05f, 1f);
                cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
                cb.disabledColor = Color.white; // Tetap jelas terlihat seperti gambar referensi
                cb.colorMultiplier = 1f;
                cb.fadeDuration = 0.08f;
                btn.colors = cb;

#if UNITY_EDITOR
                // Hapus persistent call lama pada tombol level jika ada
                while (btn.onClick.GetPersistentEventCount() > 0)
                {
                    UnityEditor.Events.UnityEventTools.RemovePersistentListener(btn.onClick, 0);
                }
#endif

                // Atur listener klik
                btn.onClick.RemoveAllListeners();
                if (isUnlocked)
                {
                    int capturedLevel = levelNum;
                    btn.onClick.AddListener(() => SelectLevel(capturedLevel));
                }

                // Pastikan sound feedback ada
                if (btnObj.GetComponent<UIButtonSound>() == null)
                {
                    btnObj.AddComponent<UIButtonSound>();
                }

                // Star Rating Display (Fitur 13 - Replay Value)
                Transform starBadgeTr = btnObj.transform.Find("StarRatingDisplay");
                if (starBadgeTr != null) Destroy(starBadgeTr.gameObject);

                if (isUnlocked)
                {
                    int stars = MazeSpell.Gameplay.StarRatingManager.GetStarsForLevel(levelNum);
                    GameObject starBadge = new GameObject("StarRatingDisplay");
                    starBadge.transform.SetParent(btnObj.transform, false);

                    RectTransform starRect = starBadge.AddComponent<RectTransform>();
                    starRect.anchorMin = new Vector2(0.5f, 0f);
                    starRect.anchorMax = new Vector2(0.5f, 0f);
                    starRect.pivot = new Vector2(0.5f, 1f);
                    starRect.sizeDelta = new Vector2(180f, 36f);
                    starRect.anchoredPosition = new Vector2(0f, -8f);

                    Image starBg = starBadge.AddComponent<Image>();
                    starBg.color = new Color(0.04f, 0.08f, 0.15f, 0.88f);
                    starBg.raycastTarget = false;

                    GameObject txtObj = new GameObject("StarsText");
                    txtObj.transform.SetParent(starBadge.transform, false);
                    RectTransform txtRect = txtObj.AddComponent<RectTransform>();
                    txtRect.anchorMin = Vector2.zero;
                    txtRect.anchorMax = Vector2.one;
                    txtRect.offsetMin = Vector2.zero;
                    txtRect.offsetMax = Vector2.zero;

                    TextMeshProUGUI starTmp = txtObj.AddComponent<TextMeshProUGUI>();
                    starTmp.fontSize = 24;
                    starTmp.fontStyle = FontStyles.Bold;
                    starTmp.alignment = TextAlignmentOptions.Center;
                    starTmp.characterSpacing = 4f;
                    starTmp.raycastTarget = false;

                    if (stars > 0)
                    {
                        string filled = new string('★', stars);
                        string empty = new string('☆', 3 - stars);
                        starTmp.text = $"<color=#FCD34D>{filled}</color><color=#6B7280>{empty}</color>";
                    }
                    else
                    {
                        starTmp.text = "<color=#6B7280>☆☆☆</color>";
                    }
                }

                activeLevelButtons[i] = btn;
            }

            // Atur HomeButton
            var homeObj = GameObject.Find("HomeButton");
            if (homeObj != null)
            {
                RectTransform homeRect = homeObj.GetComponent<RectTransform>();
                if (homeRect != null)
                {
                    homeRect.anchorMin = new Vector2(0.5f, 0.5f);
                    homeRect.anchorMax = new Vector2(0.5f, 0.5f);
                    homeRect.pivot = new Vector2(0.5f, 0.5f);
                    homeRect.sizeDelta = new Vector2(260f, 64f);
                    homeRect.anchoredPosition = new Vector2(0f, -390f);
                }

                activeHomeButton = homeObj.GetComponent<Button>();
                if (activeHomeButton != null)
                {
                    activeHomeButton.onClick.RemoveAllListeners();
                    activeHomeButton.onClick.AddListener(BackToMainMenu);
                }
            }

            // Atur Navigasi Eksplisit Gamepad & Keyboard
            SetupButtonNavigation();

            // Tentukan tombol awal yang difokuskan (Level 1 atau level tertinggi yang terbuka)
            firstSelectedButton = activeLevelButtons[0];
        }

        private void SetupButtonNavigation()
        {
            if (activeLevelButtons[0] == null) return;

            // Baris 1: Level 1 (0), Level 2 (1), Level 3 (2)
            // Baris 2: Level 4 (3), Level 5 (4)
            // Bawah: HomeButton

            // Level 1
            SetButtonNav(activeLevelButtons[0],
                onLeft: activeLevelButtons[2],
                onRight: activeLevelButtons[1],
                onUp: activeHomeButton,
                onDown: activeLevelButtons[3]);

            // Level 2
            SetButtonNav(activeLevelButtons[1],
                onLeft: activeLevelButtons[0],
                onRight: activeLevelButtons[2],
                onUp: activeHomeButton,
                onDown: activeLevelButtons[3]);

            // Level 3
            SetButtonNav(activeLevelButtons[2],
                onLeft: activeLevelButtons[1],
                onRight: activeLevelButtons[0],
                onUp: activeHomeButton,
                onDown: activeLevelButtons[4]);

            // Level 4
            SetButtonNav(activeLevelButtons[3],
                onLeft: activeLevelButtons[4],
                onRight: activeLevelButtons[4],
                onUp: activeLevelButtons[0],
                onDown: activeHomeButton);

            // Level 5
            SetButtonNav(activeLevelButtons[4],
                onLeft: activeLevelButtons[3],
                onRight: activeLevelButtons[3],
                onUp: activeLevelButtons[2],
                onDown: activeHomeButton);

            // HomeButton
            if (activeHomeButton != null)
            {
                SetButtonNav(activeHomeButton,
                    onLeft: activeLevelButtons[3],
                    onRight: activeLevelButtons[4],
                    onUp: activeLevelButtons[3],
                    onDown: activeLevelButtons[1]);
            }
        }

        private void SetButtonNav(Button btn, Selectable onLeft, Selectable onRight, Selectable onUp, Selectable onDown)
        {
            if (btn == null) return;
            Navigation nav = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnLeft = onLeft,
                selectOnRight = onRight,
                selectOnUp = onUp,
                selectOnDown = onDown
            };
            btn.navigation = nav;
        }

        #endregion

        #region Sprite Loading

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureSpritesAssigned();
        }

        public void EnsureSpritesAssigned()
        {
            if (levelSprites == null || levelSprites.Length != 5)
            {
                levelSprites = new Sprite[5];
            }

            for (int i = 0; i < 5; i++)
            {
                int lvl = i + 1;
                if (levelSprites[i] == null)
                {
                    string path = $"Assets/Ui/Button/btn_level{lvl}.png";
                    levelSprites[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (levelSprites[i] == null)
                    {
                        levelSprites[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Ui/Button/btn_level_{lvl}.png");
                    }
                }
            }

            if (lockedSprite == null)
            {
                lockedSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Ui/Button/btn_level_locked.png");
            }
        }
#endif

        public Sprite GetUnlockedSprite(int level)
        {
            int index = level - 1;
            if (index < 0 || index >= 5) return null;

            // 1. Cek serialized field
            if (levelSprites != null && levelSprites.Length > index && levelSprites[index] != null)
            {
                return levelSprites[index];
            }

            if (cachedUnlockedSprites == null)
            {
                cachedUnlockedSprites = new Sprite[5];
            }

            if (cachedUnlockedSprites[index] != null)
            {
                return cachedUnlockedSprites[index];
            }

            // 2. Cari di memori / loaded sprites (utamakan format btn_level{level} baru)
            var allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
            if (allSprites != null)
            {
                foreach (var s in allSprites)
                {
                    if (s == null) continue;
                    if (s.name == $"btn_level{level}" || s.name == $"btn_level{level}_0")
                    {
                        cachedUnlockedSprites[index] = s;
                        return s;
                    }
                }
                foreach (var s in allSprites)
                {
                    if (s == null) continue;
                    if (s.name == $"btn_level_{level}" || s.name == $"btn_level_{level}_0")
                    {
                        cachedUnlockedSprites[index] = s;
                        return s;
                    }
                }
            }

#if UNITY_EDITOR
            // 3. Muat via AssetDatabase (utamakan format btn_level{level}.png baru)
            string[] searchPaths = new string[]
            {
                $"Assets/Ui/Button/btn_level{level}.png",
                $"Assets/Ui/Button/btn_level_{level}.png"
            };
            foreach (var path in searchPaths)
            {
                var sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sp != null)
                {
                    cachedUnlockedSprites[index] = sp;
                    return sp;
                }
            }
#endif

            return null;
        }

        public Sprite GetLockedSprite()
        {
            // 1. Cek serialized field
            if (lockedSprite != null) return lockedSprite;

            if (cachedLockedSprite != null) return cachedLockedSprite;

            // 2. Cari di memori / loaded sprites
            var allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
            if (allSprites != null)
            {
                foreach (var s in allSprites)
                {
                    if (s == null) continue;
                    if (s.name == "btn_level_locked" || s.name == "btn_level_locked_0")
                    {
                        cachedLockedSprite = s;
                        return s;
                    }
                }
            }

#if UNITY_EDITOR
            // 3. Muat via AssetDatabase
            var sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Ui/Button/btn_level_locked.png");
            if (sp != null)
            {
                cachedLockedSprite = sp;
                return sp;
            }
#endif

            return null;
        }

        #endregion

        #region Navigasi & Input

        private IEnumerator FocusFirstButtonNextFrame()
        {
            yield return null;
            if (EventSystem.current != null)
            {
                Button target = firstSelectedButton;
                if (target == null || !target.interactable)
                {
                    // Cari tombol pertama yang interactable
                    foreach (var b in activeLevelButtons)
                    {
                        if (b != null && b.interactable)
                        {
                            target = b;
                            break;
                        }
                    }
                }
                if (target != null)
                {
                    EventSystem.current.SetSelectedGameObject(null);
                    EventSystem.current.SetSelectedGameObject(target.gameObject);
                }
            }
        }

        private void Update()
        {
            // Input pembatalan / kembali (ESC atau Gamepad Circle / B)
            if (CheckCancelInput())
            {
                BackToMainMenu();
                return;
            }

            // Pulihkan fokus jika klik terjadi di luar tombol
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null && firstSelectedButton != null)
            {
                if (firstSelectedButton.interactable)
                {
                    EventSystem.current.SetSelectedGameObject(firstSelectedButton.gameObject);
                }
            }
        }

        private bool CheckCancelInput()
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

        private void CleanupGameplayManagersInMenu()
        {
            var progressionManager = Object.FindAnyObjectByType<MazeSpell.LevelProgressionManager>();
            if (progressionManager != null)
            {
                Destroy(progressionManager.gameObject);
            }

            var wordManager = Object.FindAnyObjectByType<MazeSpell.WordCollectionManager>();
            if (wordManager != null)
            {
                Destroy(wordManager.gameObject);
            }

            var livesManager = Object.FindAnyObjectByType<MazeSpell.PlayerLivesManager>();
            if (livesManager != null)
            {
                Destroy(livesManager.gameObject);
            }

            for (int i = 1; i <= 5; i++)
            {
                GameObject gridObj = GameObject.Find($"Level{i}_MazeGrid");
                if (gridObj != null)
                {
                    Destroy(gridObj);
                }
            }
        }

        /// <summary>
        /// Memuat gameplay pada level tertentu (1 s/d 5).
        /// Hanya berjalan jika level sudah terbuka.
        /// </summary>
        public void SelectLevel(int levelNumber)
        {
            if (levelNumber < 1 || levelNumber > 5)
            {
                Debug.LogWarning($"[LevelSelect] Level {levelNumber} tidak valid (harus 1-5).");
                return;
            }

            // Verifikasi status unlock level
            if (!LevelProgressionManager.IsLevelUnlocked(levelNumber))
            {
                Debug.LogWarning($"[LevelSelect] Level {levelNumber} masih terkunci! Selesaikan level sebelumnya terlebih dahulu.");
                return;
            }

            Debug.Log($"[LevelSelect] Memilih Level {levelNumber}. Memuat gameplay: {gameplaySceneName}");
            requestedLevel = levelNumber;

            AudioManager.PlayUIStatic(AudioManager.SFXType.ButtonClick, 0.9f);

            // Daftarkan listener satu kali untuk mengatur level setelah scene gameplay selesai dimuat
            SceneManager.sceneLoaded += OnGameplaySceneLoaded;

            if (Application.isPlaying)
            {
                if (Application.CanStreamedLevelBeLoaded(gameplaySceneName))
                {
                    SceneManager.LoadScene(gameplaySceneName);
                }
                else
                {
                    SceneManager.LoadScene(1); // Index 1 adalah SampleScene dalam Build Settings
                }
            }
        }

        private static void OnGameplaySceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnGameplaySceneLoaded;

            if (scene.name == "SampleScene")
            {
                var progression = Object.FindAnyObjectByType<MazeSpell.LevelProgressionManager>();
                if (progression != null)
                {
                    progression.LoadLevel(requestedLevel);
                }
            }
        }

        /// <summary>
        /// Kembali ke scene Main Menu.
        /// </summary>
        public void BackToMainMenu()
        {
            Debug.Log($"[LevelSelect] Kembali ke Main Menu... Memuat: {mainMenuSceneName}");
            AudioManager.PlayUIStatic(AudioManager.SFXType.ButtonClick, 0.8f);

            if (Application.isPlaying)
            {
                if (Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
                {
                    SceneManager.LoadScene(mainMenuSceneName);
                }
                else
                {
                    SceneManager.LoadScene(0); // Index 0 adalah MainMenu dalam Build Settings
                }
            }
        }

        #endregion
    }
}
