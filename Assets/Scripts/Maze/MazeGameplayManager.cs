using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;
using MazeSpell.UI;

namespace MazeSpell
{
    [System.Serializable]
    public class LevelPlacementData
    {
        public int levelNumber;
        public string targetWord;
        public Vector2 spawnPosition;
        public Vector2 goalPosition;
        public Vector2[] letterPositions;
        public Vector2[] hazardPositions;
    }

    public class MazeGameplayManager : MonoBehaviour
    {
        private static MazeGameplayManager _instance;
        public static MazeGameplayManager Instance
        {
            get
            {
                if (_instance == null)
                {
#if UNITY_2023_1_OR_NEWER
                    _instance = FindAnyObjectByType<MazeGameplayManager>();
#else
                    _instance = FindObjectOfType<MazeGameplayManager>();
#endif
                }
                return _instance;
            }
            private set => _instance = value;
        }

        public static readonly LevelPlacementData[] AllLevelsData =
        {
            new LevelPlacementData
            {
                levelNumber = 1,
                targetWord = "EARTH",
                spawnPosition = new Vector2(2.5f, 7.5f),
                goalPosition = new Vector2(18.5f, 4.5f),
                letterPositions = new Vector2[]
                {
                    new Vector2(4.5f, 12.5f),  // E: Kamar Tidur Kiri Atas
                    new Vector2(10.5f, 12.5f), // A: Dapur / Ruang Tengah Atas
                    new Vector2(10.5f, 6.5f),  // R: Karpet Merah Living Room
                    new Vector2(16.5f, 8.5f),  // T: Ruang Makan Karpet Hijau
                    new Vector2(16.5f, 5.5f)   // H: Ruang Belajar Karpet Cokelat
                },
                hazardPositions = new Vector2[]
                {
                    new Vector2(11.5f, 10.5f)  // Enemy Guard: Patrol Ruang Atas
                }
            },

            new LevelPlacementData
            {
                levelNumber = 2,
                targetWord = "STONE",
                spawnPosition = new Vector2(9.5f, 1.5f),
                goalPosition = new Vector2(16.5f, 3.5f),
                letterPositions = new Vector2[]
                {
                    new Vector2(4.5f, 12.5f),  // S: Kamar Tidur Kiri Atas
                    new Vector2(14.5f, 12.5f), // T: Ruang Makan Kanan Atas
                    new Vector2(4.5f, 8.5f),   // O: Living Room Kiri Tengah
                    new Vector2(14.5f, 8.5f),  // N: Ruangan Lantai Kayu Kanan Tengah
                    new Vector2(4.5f, 4.5f)    // E: Living Room Kiri Bawah
                },
                hazardPositions = new Vector2[]
                {
                    new Vector2(8.5f, 10.5f)   // Enemy Guard: Koridor Tengah
                }
            },

            new LevelPlacementData
            {
                levelNumber = 3,
                targetWord = "GRASS",
                spawnPosition = new Vector2(11.5f, 3.5f),
                goalPosition = new Vector2(17.5f, 6.5f),
                letterPositions = new Vector2[]
                {
                    new Vector2(4.5f, 14.5f),  // G: Kamar Tidur Kiri Atas
                    new Vector2(15.5f, 14.5f), // R: Kamar Tidur Kanan Atas
                    new Vector2(4.5f, 10.5f),  // A: Dapur Kiri Tengah
                    new Vector2(15.5f, 10.5f), // S_1: Ruang Makan Kanan Tengah
                    new Vector2(15.5f, 5.5f)   // S_2: Kamar Tidur Kanan Bawah
                },
                hazardPositions = new Vector2[]
                {
                    new Vector2(9.5f, 8.5f)    // Enemy Guard: Koridor Tengah
                }
            },

            new LevelPlacementData
            {
                levelNumber = 4,
                targetWord = "FROST",
                spawnPosition = new Vector2(7.5f, 2.5f),
                goalPosition = new Vector2(17.5f, 4.5f),
                letterPositions = new Vector2[]
                {
                    new Vector2(4.5f, 12.5f),  // F: Master Bedroom Kiri Atas
                    new Vector2(10.5f, 12.5f), // R: Kamar Mandi Tengah Atas
                    new Vector2(15.5f, 12.5f), // O: Living Room Kanan Atas
                    new Vector2(4.5f, 7.5f),   // S: Living Room Kiri Bawah
                    new Vector2(15.5f, 6.5f)   // T: Ruang Makan Kanan Bawah
                },
                hazardPositions = new Vector2[]
                {
                    new Vector2(10.5f, 8.5f)   // Enemy Guard: Lorong Tengah
                }
            },

            new LevelPlacementData
            {
                levelNumber = 5,
                targetWord = "FLAME",
                spawnPosition = new Vector2(9.5f, 2.5f),
                goalPosition = new Vector2(20.5f, 4.5f),
                letterPositions = new Vector2[]
                {
                    new Vector2(4.5f, 12.5f),  // F: Kamar Tidur Kiri Atas
                    new Vector2(13.5f, 12.5f), // L: Ruang Belajar Tengah Atas
                    new Vector2(18.5f, 12.5f), // A: Dapur Kanan Atas
                    new Vector2(4.5f, 7.5f),   // M: Living Room Kiri Bawah
                    new Vector2(15.5f, 6.5f)   // E: Living Room Kanan Bawah
                },
                hazardPositions = new Vector2[]
                {
                    new Vector2(8.5f, 8.5f),   // Enemy Guard: Koridor Atas
                    new Vector2(10.5f, 4.5f)   // Final Guardian: Hall Bawah
                }
            }
        };

        private GameObject currentPlayer;
        private GameObject currentGoalObject;
        private TextMeshPro currentGoalText;

        private GameObject wordProgressObject;
        private TextMeshProUGUI wordProgressText;

        private GameObject gameCompletionPanel;
        private Button playAgainButton;
        private Button mainMenuButton;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneLoadedHook()
        {
            SceneManager.sceneLoaded += (scene, mode) =>
            {
                if (scene.name == "SampleScene")
                {
                    EnsureManagerExists();
                }
            };
        }

        public static MazeGameplayManager EnsureManagerExists()
        {
            if (Instance != null) return Instance;
#if UNITY_2023_1_OR_NEWER
            var found = FindAnyObjectByType<MazeGameplayManager>();
#else
            var found = FindObjectOfType<MazeGameplayManager>();
#endif
            if (found != null)
            {
                _instance = found;
                return _instance;
            }

            GameObject managerGo = new GameObject("MazeGameplayManager");
            _instance = managerGo.AddComponent<MazeGameplayManager>();
            return _instance;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        private void Start()
        {
            FindPlayer();
            HookEvents();
            SuppressLegacyStatusUI();

            if (LevelProgressionManager.Instance != null)
            {
                StartCoroutine(
                    SetupLevelDeferred(
                        LevelProgressionManager.Instance.CurrentLevel
                    )
                );
            }
        }

        private void OnDestroy()
        {
            UnhookEvents();
            if (_instance == this) _instance = null;
        }

        private void SuppressLegacyStatusUI()
        {
            if (LevelProgressionManager.Instance == null)
                return;

            var field = typeof(LevelProgressionManager).GetField(
                "showSimpleStatusUI",
                BindingFlags.NonPublic | BindingFlags.Instance
            );

            if (field != null)
            {
                field.SetValue(
                    LevelProgressionManager.Instance,
                    false
                );
            }
        }

        private void HookEvents()
        {
            if (LevelProgressionManager.Instance != null)
            {
                LevelProgressionManager.Instance.onLevelStarted?.AddListener(OnLevelStarted);
                LevelProgressionManager.Instance.onAllLevelsCompleted?.AddListener(OnAllLevelsCompleted);
            }

            if (WordCollectionManager.Instance != null)
            {
                WordCollectionManager.Instance.onWordCompleted?.AddListener(OnWordCompleted);

                // Coba hubungkan event perubahan huruf jika tersedia
                TryHookWordProgressEvent();
            }
        }

        private void UnhookEvents()
        {
            if (LevelProgressionManager.Instance != null)
            {
                LevelProgressionManager.Instance.onLevelStarted?.RemoveListener(OnLevelStarted);
                LevelProgressionManager.Instance.onAllLevelsCompleted?.RemoveListener(OnAllLevelsCompleted);
            }

            if (WordCollectionManager.Instance != null)
            {
                WordCollectionManager.Instance.onWordCompleted?.RemoveListener(OnWordCompleted);
            }
        }

        private void TryHookWordProgressEvent()
        {
            // Tidak menggunakan event yang belum tentu ada.
            // Progres kata akan diperbarui melalui coroutine.
        }

        private void FindPlayer()
        {
            if (currentPlayer != null)
                return;

            currentPlayer = GameObject.FindWithTag("Player");

            if (currentPlayer == null)
            {
#if UNITY_2023_1_OR_NEWER
                var pc = FindAnyObjectByType<PlayerController>();
#else
                var pc = FindObjectOfType<PlayerController>();
#endif

                if (pc != null)
                    currentPlayer = pc.gameObject;
            }
        }

        private void OnLevelStarted(int level)
        {
            StartCoroutine(SetupLevelDeferred(level));
        }

        private IEnumerator SetupLevelDeferred(int level)
        {
            yield return null;

            FindPlayer();

            GameObject grid =
                GameObject.Find($"Level{level}_MazeGrid");

            if (grid == null)
            {
                Debug.LogWarning(
                    $"[MazeGameplayManager] Level {level}_MazeGrid tidak ditemukan."
                );

                yield break;
            }

            Transform gameplayContainer =
                grid.transform.Find("GameplayObjects");

            if (gameplayContainer == null)
            {
                BuildGameplayObjectsForLevel(
                    grid.transform,
                    level
                );

                gameplayContainer =
                    grid.transform.Find("GameplayObjects");
            }

            // =========================
            // VALIDASI & ANTI-WALL LETTERS
            // =========================
            MazeLetterValidator.ValidateAllLettersInGrid(grid, level);

            // =========================
            // RESET / SETUP WORD
            // =========================

            CreateWordProgressUI();

            StartCoroutine(UpdateWordProgressRoutine());

            // =========================
            // SPAWN PLAYER
            // =========================

            Transform spawn =
                gameplayContainer != null
                    ? gameplayContainer.Find("SpawnPoint")
                    : null;

            if (spawn != null && currentPlayer != null)
            {
                currentPlayer.transform.position =
                    spawn.position;

                Rigidbody2D rb =
                    currentPlayer.GetComponent<Rigidbody2D>();

                if (rb != null)
                {
#if UNITY_6000_0_OR_NEWER
                    rb.linearVelocity = Vector2.zero;
#else
                    rb.velocity = Vector2.zero;
#endif

                    rb.angularVelocity = 0f;
                }

                // Simpan posisi respawn ke PlayerLivesManager
                if (PlayerLivesManager.Instance != null)
                {
                    var respawnField =
                        typeof(PlayerLivesManager).GetField(
                            "respawnPosition",
                            BindingFlags.NonPublic |
                            BindingFlags.Instance
                        );

                    if (respawnField != null)
                    {
                        respawnField.SetValue(
                            PlayerLivesManager.Instance,
                            spawn.position
                        );
                    }
                }
            }

            // =========================
            // SETUP GOAL
            // =========================

            Transform goalTransform =
                gameplayContainer != null
                    ? gameplayContainer.Find("Goal")
                    : null;

            if (goalTransform != null)
            {
                currentGoalObject =
                    goalTransform.gameObject;

                // Pastikan LevelGoalTrigger AKTIF dan siap digunakan
                var goalTrigger =
                    currentGoalObject.GetComponent<LevelGoalTrigger>();

                if (goalTrigger == null)
                {
                    goalTrigger =
                        currentGoalObject.AddComponent<LevelGoalTrigger>();
                }
                goalTrigger.enabled = true;
                goalTrigger.ResetTrigger();

                // Pastikan ExitGateVisual aktif dan terinisialisasi sesuai level
                var exitGate = currentGoalObject.GetComponent<ExitGateVisual>();
                if (exitGate == null)
                {
                    exitGate = currentGoalObject.AddComponent<ExitGateVisual>();
                }
                exitGate.Initialize(level);

                Transform visual =
                    goalTransform.Find("Visual");

                if (visual != null)
                {
                    currentGoalText =
                        visual.GetComponent<TextMeshPro>();

                    if (currentGoalText != null)
                    {
                        currentGoalText.text =
                            "<b>🔒 GATE CLOSED</b>\n" +
                            "<size=2.4>" +
                            "<color=#FCD34D>" +
                            "KUMPULKAN HURUF" +
                            "</color></size>";

                        currentGoalText.color =
                            new Color(
                                0.75f,
                                0.75f,
                                0.80f,
                                0.85f
                            );
                    }
                }
            }

            // Inisialisasi Gameplay Upgrade (Word Power, Gimmick, Secret Room, Combat Feedback)
            MazeSpell.Gameplay.GameplayUpgradeCoordinator.EnsureCoordinatorExists();
        }

        // =====================================================
        // WORD PROGRESS
        // =====================================================

        private void CreateWordProgressUI()
        {
            if (wordProgressObject != null)
            {
                Destroy(wordProgressObject);
                wordProgressObject = null;
            }

            // Hindari duplikasi UI kata target jika GameplayHUD sudah aktif
#if UNITY_2023_1_OR_NEWER
            if (MazeSpell.UI.GameplayHUD.Instance != null || FindAnyObjectByType<MazeSpell.UI.GameplayHUD>() != null)
#else
            if (MazeSpell.UI.GameplayHUD.Instance != null || FindObjectOfType<MazeSpell.UI.GameplayHUD>() != null)
#endif
            {
                return;
            }

#if UNITY_2023_1_OR_NEWER
            var canvas = FindAnyObjectByType<Canvas>();
#else
            var canvas = FindObjectOfType<Canvas>();
#endif

            if (canvas == null)
            {
                Debug.LogWarning(
                    "[MazeGameplayManager] Canvas tidak ditemukan."
                );

                return;
            }

            wordProgressObject =
                new GameObject("WordProgressUI");

            wordProgressObject.transform.SetParent(
                canvas.transform,
                false
            );

            RectTransform rect =
                wordProgressObject.AddComponent<RectTransform>();

            rect.anchorMin =
                new Vector2(0.5f, 1f);

            rect.anchorMax =
                new Vector2(0.5f, 1f);

            rect.pivot =
                new Vector2(0.5f, 1f);

            rect.sizeDelta =
                new Vector2(500f, 80f);

            rect.anchoredPosition =
                new Vector2(0f, -25f);

            Image bg =
                wordProgressObject.AddComponent<Image>();

            bg.color =
                new Color(
                    0.03f,
                    0.07f,
                    0.12f,
                    0.90f
                );

            GameObject textObj =
                new GameObject("WordText");

            textObj.transform.SetParent(
                wordProgressObject.transform,
                false
            );

            RectTransform textRect =
                textObj.AddComponent<RectTransform>();

            textRect.anchorMin =
                Vector2.zero;

            textRect.anchorMax =
                Vector2.one;

            textRect.offsetMin =
                new Vector2(15f, 5f);

            textRect.offsetMax =
                new Vector2(-15f, -5f);

            wordProgressText =
                textObj.AddComponent<TextMeshProUGUI>();

            wordProgressText.font =
                TMP_Settings.defaultFontAsset;

            wordProgressText.fontSize = 25f;

            wordProgressText.fontStyle =
                FontStyles.Bold;

            wordProgressText.alignment =
                TextAlignmentOptions.Center;

            wordProgressText.color =
                Color.white;

            wordProgressText.raycastTarget =
                false;
        }

        private IEnumerator UpdateWordProgressRoutine()
        {
            while (true)
            {
                UpdateWordProgressText();

                yield return new WaitForSeconds(0.15f);
            }
        }

        private void UpdateWordProgressText()
        {
            if (wordProgressText == null)
                return;

            if (LevelProgressionManager.Instance == null)
                return;

            int level =
                LevelProgressionManager.Instance.CurrentLevel;

            LevelPlacementData data =
                GetLevelData(level);

            if (data == null)
                return;

            string targetWord =
                data.targetWord;

            string collected =
                GetCollectedLetters();

            string display =
                "";

            for (int i = 0; i < targetWord.Length; i++)
            {
                if (i < collected.Length)
                {
                    display +=
                        $"<color=#6EE7B7>{collected[i]}</color>";
                }
                else
                {
                    display +=
                        "<color=#94A3B8>_</color>";
                }

                if (i < targetWord.Length - 1)
                    display += " ";
            }

            wordProgressText.text =
                $"KATA: {display}";
        }

        private string GetCollectedLetters()
        {
            if (WordCollectionManager.Instance == null)
                return "";

            // Cari informasi huruf yang terkumpul menggunakan reflection
            // agar tidak bergantung pada nama field tertentu.
            object manager =
                WordCollectionManager.Instance;

            Type type =
                manager.GetType();

            string[] possibleNames =
            {
                "collectedLetters",
                "collectedWord",
                "currentWord",
                "lettersCollected",
                "collected"
            };

            foreach (string fieldName in possibleNames)
            {
                var field =
                    type.GetField(
                        fieldName,
                        BindingFlags.NonPublic |
                        BindingFlags.Public |
                        BindingFlags.Instance
                    );

                if (field == null)
                    continue;

                object value =
                    field.GetValue(manager);

                if (value is string str)
                    return str.ToUpper();

                if (value is System.Collections.Generic.List<char> charList)
                    return new string(charList.ToArray()).ToUpper();

                if (value is System.Collections.Generic.List<string> stringList)
                    return string.Concat(stringList).ToUpper();
            }

            return "";
        }

        private LevelPlacementData GetLevelData(int level)
        {
            foreach (var data in AllLevelsData)
            {
                if (data.levelNumber == level)
                    return data;
            }

            return null;
        }

        // =====================================================
        // WORD COMPLETED
        // =====================================================

        private void OnWordCompleted(string completedWord)
        {
            Debug.Log(
                $"[MazeGameplayManager] KATA SELESAI: {completedWord}"
            );

            int currentLevel = LevelProgressionManager.Instance != null ? LevelProgressionManager.Instance.CurrentLevel : 1;
            if (currentLevel == 5)
            {
                if (currentGoalObject != null)
                {
                    currentGoalObject.SetActive(false);
                }
                return;
            }

            if (currentGoalObject != null)
            {
                var goalTrigger =
                    currentGoalObject.GetComponent<LevelGoalTrigger>();

                if (goalTrigger != null)
                {
                    goalTrigger.enabled = true;
                }

                var exitGate = currentGoalObject.GetComponent<ExitGateVisual>();
                if (exitGate != null)
                {
                    exitGate.OnWordCompleted();
                }
            }

            if (currentGoalText != null)
            {
                currentGoalText.text =
                    "<b>★ EXIT OPEN ★</b>\n" +
                    "<size=2.4>" +
                    "<color=#6EE7B7>" +
                    "MENUJU PINTU KELUAR" +
                    "</color></size>";

                currentGoalText.color =
                    new Color(
                        0.20f,
                        0.88f,
                        0.55f,
                        1f
                    );
            }

            if (wordProgressText != null)
            {
                wordProgressText.text =
                    $"<color=#6EE7B7>✓ KATA SELESAI: {completedWord.ToUpper()}</color>";
            }
        }

        // =====================================================
        // GOAL REACHED
        // =====================================================

        public void HandleGoalReached(int currentLevel)
        {
            if (WordCollectionManager.Instance == null)
            {
                Debug.LogWarning(
                    "[MazeGameplayManager] WordCollectionManager tidak ditemukan. Goal diblokir."
                );

                return;
            }

            if (!WordCollectionManager.Instance.IsWordComplete)
            {
                if (GameplayFeedbackController.Instance != null)
                {
                    GameplayFeedbackController.Instance
                        .ShowNoticeMessage(
                            "Kumpulkan semua huruf terlebih dahulu!"
                        );
                }

                return;
            }

            Debug.Log(
                $"[MazeGameplayManager] Goal berhasil dicapai Level {currentLevel}."
            );

            if (currentLevel < 5)
            {
                int nextLevel =
                    currentLevel + 1;

                if (GameplayFeedbackController.Instance != null)
                {
                    GameplayFeedbackController.Instance
                        .ExecuteLevelTransition(
                            nextLevel,
                            () =>
                            {
                                if (LevelProgressionManager.Instance != null)
                                {
                                    LevelProgressionManager.Instance
                                        .CompleteCurrentLevel();
                                }
                            }
                        );
                }
                else
                {
                    if (LevelProgressionManager.Instance != null)
                    {
                        LevelProgressionManager.Instance
                            .CompleteCurrentLevel();
                    }
                }
            }
            else
            {
                if (LevelProgressionManager.Instance != null)
                {
                    LevelProgressionManager.Instance
                        .CompleteCurrentLevel();
                }
            }
        }

        // =====================================================
        // GAME COMPLETION
        // =====================================================

        private void OnAllLevelsCompleted()
        {
            StartCoroutine(
                ShowGameCompletionSequence()
            );
        }

        private IEnumerator ShowGameCompletionSequence()
        {
            yield return new WaitForSeconds(0.4f);

            Transform parentCanvas = null;

#if UNITY_2023_1_OR_NEWER
            var hud = FindAnyObjectByType<GameplayHUD>();
            var anyCanvas = FindAnyObjectByType<Canvas>();
#else
            var hud = FindObjectOfType<GameplayHUD>();
            var anyCanvas = FindObjectOfType<Canvas>();
#endif

            if (hud != null)
                parentCanvas = hud.transform;
            else if (anyCanvas != null)
                parentCanvas = anyCanvas.transform;

            if (parentCanvas == null)
                yield break;

            if (gameCompletionPanel == null)
            {
                BuildGameCompletionPanel(parentCanvas);
            }

            if (gameCompletionPanel != null)
            {
                gameCompletionPanel.SetActive(true);

                if (EventSystem.current != null &&
                    playAgainButton != null)
                {
                    EventSystem.current
                        .SetSelectedGameObject(null);

                    EventSystem.current
                        .SetSelectedGameObject(
                            playAgainButton.gameObject
                        );
                }
            }
        }

        private void BuildGameCompletionPanel(
            Transform canvasParent
        )
        {
            TMP_FontAsset defaultFont =
                TMP_Settings.defaultFontAsset;

            gameCompletionPanel =
                new GameObject(
                    "GameCompletionPanel"
                );

            gameCompletionPanel.transform.SetParent(
                canvasParent,
                false
            );

            RectTransform rootRect =
                gameCompletionPanel.AddComponent<RectTransform>();

            rootRect.anchorMin =
                Vector2.zero;

            rootRect.anchorMax =
                Vector2.one;

            rootRect.offsetMin =
                Vector2.zero;

            rootRect.offsetMax =
                Vector2.zero;

            Image dimBg =
                gameCompletionPanel.AddComponent<Image>();

            dimBg.color =
                new Color(
                    0.04f,
                    0.07f,
                    0.12f,
                    0.92f
                );

            dimBg.raycastTarget = true;

            GameObject cardObj =
                new GameObject("Card");

            cardObj.transform.SetParent(
                gameCompletionPanel.transform,
                false
            );

            RectTransform cardRect =
                cardObj.AddComponent<RectTransform>();

            cardRect.anchorMin =
                new Vector2(0.5f, 0.5f);

            cardRect.anchorMax =
                new Vector2(0.5f, 0.5f);

            cardRect.sizeDelta =
                new Vector2(680f, 440f);

            cardRect.anchoredPosition =
                Vector2.zero;

            Image cardBg =
                cardObj.AddComponent<Image>();

            cardBg.color =
                new Color(
                    0.06f,
                    0.11f,
                    0.19f,
                    0.96f
                );

            CreateTMPText(
                cardObj.transform,
                "Stars",
                "★  ★  ★  ★  ★",
                28,
                new Vector2(0f, 150f),
                new Vector2(500f, 40f),
                new Color(0.99f, 0.85f, 0.35f, 1f),
                defaultFont
            );

            CreateTMPText(
                cardObj.transform,
                "Title",
                "MAZESPELL COMPLETE!",
                44,
                new Vector2(0f, 90f),
                new Vector2(620f, 80f),
                new Color(0.99f, 0.85f, 0.35f, 1f),
                defaultFont
            );

            CreateTMPText(
                cardObj.transform,
                "Subtitle",
                "Kamu berhasil menyelesaikan semua level!",
                22,
                new Vector2(0f, 25f),
                new Vector2(600f, 60f),
                Color.white,
                defaultFont
            );

            GameObject playBtnObj =
                CreateButton(
                    "PlayAgainButton",
                    cardObj.transform,
                    new Vector2(0f, -60f),
                    new Vector2(340f, 60f),
                    "↺  PLAY AGAIN",
                    new Color(0.10f, 0.68f, 0.42f, 1f),
                    new Color(0.18f, 0.82f, 0.52f, 1f),
                    new Color(0.06f, 0.50f, 0.30f, 1f),
                    defaultFont
                );

            playAgainButton =
                playBtnObj.GetComponent<Button>();

            playAgainButton.onClick
                .AddListener(RestartGame);

            GameObject menuBtnObj =
                CreateButton(
                    "MainMenuButton",
                    cardObj.transform,
                    new Vector2(0f, -135f),
                    new Vector2(340f, 60f),
                    "⌂  MAIN MENU",
                    new Color(0.20f, 0.52f, 0.90f, 1f),
                    new Color(0.32f, 0.65f, 0.98f, 1f),
                    new Color(0.12f, 0.38f, 0.70f, 1f),
                    defaultFont
                );

            mainMenuButton =
                menuBtnObj.GetComponent<Button>();

            mainMenuButton.onClick
                .AddListener(ReturnToMainMenu);

            Navigation nav1 =
                new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnDown = mainMenuButton,
                    selectOnUp = mainMenuButton
                };

            Navigation nav2 =
                new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnDown = playAgainButton,
                    selectOnUp = playAgainButton
                };

            playAgainButton.navigation = nav1;
            mainMenuButton.navigation = nav2;
        }

        private GameObject CreateTMPText(
            Transform parent,
            string name,
            string text,
            float fontSize,
            Vector2 position,
            Vector2 size,
            Color color,
            TMP_FontAsset font
        )
        {
            GameObject obj =
                new GameObject(name);

            obj.transform.SetParent(
                parent,
                false
            );

            RectTransform rect =
                obj.AddComponent<RectTransform>();

            rect.anchorMin =
                new Vector2(0.5f, 0.5f);

            rect.anchorMax =
                new Vector2(0.5f, 0.5f);

            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            TextMeshProUGUI tmp =
                obj.AddComponent<TextMeshProUGUI>();

            if (font != null)
                tmp.font = font;

            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment =
                TextAlignmentOptions.Center;

            tmp.color = color;
            tmp.raycastTarget = false;

            return obj;
        }

        private GameObject CreateButton(
            string name,
            Transform parent,
            Vector2 pos,
            Vector2 size,
            string label,
            Color normal,
            Color hover,
            Color pressed,
            TMP_FontAsset font
        )
        {
            GameObject btnObj =
                new GameObject(name);

            btnObj.transform.SetParent(
                parent,
                false
            );

            RectTransform rect =
                btnObj.AddComponent<RectTransform>();

            rect.anchorMin =
                new Vector2(0.5f, 0.5f);

            rect.anchorMax =
                new Vector2(0.5f, 0.5f);

            rect.sizeDelta = size;
            rect.anchoredPosition = pos;

            Image img =
                btnObj.AddComponent<Image>();

            img.color = normal;

            Button btn =
                btnObj.AddComponent<Button>();

            ColorBlock colors =
                btn.colors;

            colors.normalColor = normal;
            colors.highlightedColor = hover;
            colors.pressedColor = pressed;
            colors.selectedColor = hover;
            colors.fadeDuration = 0.08f;

            btn.colors = colors;

            GameObject textObj =
                new GameObject("Text");

            textObj.transform.SetParent(
                btnObj.transform,
                false
            );

            RectTransform textRect =
                textObj.AddComponent<RectTransform>();

            textRect.anchorMin =
                Vector2.zero;

            textRect.anchorMax =
                Vector2.one;

            textRect.offsetMin =
                Vector2.zero;

            textRect.offsetMax =
                Vector2.zero;

            TextMeshProUGUI tmp =
                textObj.AddComponent<TextMeshProUGUI>();

            if (font != null)
                tmp.font = font;

            tmp.text = label;
            tmp.fontSize = 22;
            tmp.fontStyle =
                FontStyles.Bold;

            tmp.alignment =
                TextAlignmentOptions.Center;

            tmp.color = Color.white;
            tmp.raycastTarget = false;

            return btnObj;
        }

        private void RestartGame()
        {
            if (gameCompletionPanel != null)
                gameCompletionPanel.SetActive(false);

            if (LevelProgressionManager.Instance != null)
            {
                LevelProgressionManager.Instance
                    .RestartGame();
            }
            else
            {
                SceneManager.LoadScene(
                    SceneManager.GetActiveScene().buildIndex
                );
            }
        }

        private void ReturnToMainMenu()
        {
            if (gameCompletionPanel != null)
                gameCompletionPanel.SetActive(false);

            Time.timeScale = 1f;

            SceneManager.LoadScene("MainMenu");
        }

        // =====================================================
        // BUILD GAMEPLAY OBJECTS
        // =====================================================

        public static void BuildGameplayObjectsForLevel(
            Transform gridParent,
            int levelNumber
        )
        {
            LevelPlacementData data =
                null;

            foreach (var levelData in AllLevelsData)
            {
                if (levelData.levelNumber == levelNumber)
                {
                    data = levelData;
                    break;
                }
            }

            if (data == null)
                return;

            TMP_FontAsset defaultFont =
                TMP_Settings.defaultFontAsset;

            GameObject gameplayContainer =
                new GameObject(
                    "GameplayObjects"
                );

            gameplayContainer.transform.SetParent(
                gridParent,
                false
            );

            // =========================
            // SPAWN
            // =========================

            GameObject spawnObj =
                new GameObject(
                    "SpawnPoint"
                );

            spawnObj.transform.SetParent(
                gameplayContainer.transform,
                false
            );

            spawnObj.transform.localPosition =
                new Vector3(
                    data.spawnPosition.x,
                    data.spawnPosition.y,
                    0f
                );

            // =========================
            // LETTERS
            // =========================

            GameObject lettersContainer =
                new GameObject("Letters");

            lettersContainer.transform.SetParent(
                gameplayContainer.transform,
                false
            );

            TMP_FontAsset bubbleFont = null;
            Material outlineMat = null;
            Material depthMat = null;
            Material faceMat = null;

#if UNITY_EDITOR
            bubbleFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/TitanOne-Regular SDF.asset");
            outlineMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Fonts/BubbleLetter_Outline.mat");
            depthMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Fonts/BubbleLetter_Depth.mat");
            faceMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Fonts/BubbleLetter_Face.mat");
#endif
            if (bubbleFont == null)
            {
                bubbleFont = Resources.Load<TMP_FontAsset>("Fonts/TitanOne-Regular SDF");
            }
            if (bubbleFont == null)
            {
                bubbleFont = defaultFont;
            }

            for (
                int j = 0;
                j < data.targetWord.Length;
                j++
            )
            {
                char letterChar =
                    data.targetWord[j];

                Vector2 letterPos =
                    data.letterPositions[j];

                GameObject letterObj =
                    new GameObject(
                        $"Letter_{letterChar}_{j}"
                    );

                letterObj.transform.SetParent(
                    lettersContainer.transform,
                    false
                );

                letterObj.transform.localPosition =
                    new Vector3(
                        letterPos.x,
                        letterPos.y,
                        -0.1f
                    );

                CircleCollider2D col =
                    letterObj.AddComponent<CircleCollider2D>();

                col.isTrigger = true;
                col.radius = 0.45f;

                CollectibleLetter cl =
                    letterObj.AddComponent<CollectibleLetter>();

                cl.Letter = letterChar;

                letterObj.AddComponent<LetterHoverAnimation>();

                GameObject visualObj =
                    new GameObject("Visual");

                visualObj.transform.SetParent(
                    letterObj.transform,
                    false
                );
                visualObj.transform.localPosition = Vector3.zero;

                RectTransform tmpRect =
                    visualObj.AddComponent<RectTransform>();
                tmpRect.sizeDelta =
                    new Vector2(1.2f, 1.2f);

                // Layer 1: Outline (Biru Tua - Z = 0f)
                GameObject outlineObj = new GameObject("Outline");
                outlineObj.transform.SetParent(visualObj.transform, false);
                outlineObj.transform.localPosition = new Vector3(0.02f, -0.025f, 0f);
                TextMeshPro tmpOutline = outlineObj.AddComponent<TextMeshPro>();
                tmpOutline.font = bubbleFont;
                if (outlineMat != null) tmpOutline.fontSharedMaterial = outlineMat;
                tmpOutline.text = $"<b>{letterChar}</b>";
                tmpOutline.fontSize = 8.5f;
                tmpOutline.alignment = TextAlignmentOptions.Center;
                tmpOutline.sortingOrder = 5;
                tmpOutline.color = Color.white;

                // Layer 2: Depth (Orange - Z = -0.02f)
                GameObject depthObj = new GameObject("Depth");
                depthObj.transform.SetParent(visualObj.transform, false);
                depthObj.transform.localPosition = new Vector3(0.04f, -0.05f, -0.02f);
                TextMeshPro tmpDepth = depthObj.AddComponent<TextMeshPro>();
                tmpDepth.font = bubbleFont;
                if (depthMat != null) tmpDepth.fontSharedMaterial = depthMat;
                tmpDepth.text = $"<b>{letterChar}</b>";
                tmpDepth.fontSize = 8.5f;
                tmpDepth.alignment = TextAlignmentOptions.Center;
                tmpDepth.sortingOrder = 6;
                tmpDepth.color = Color.white;

                // Layer 3: Face (Kuning - Z = -0.04f)
                GameObject faceObj = new GameObject("Face");
                faceObj.transform.SetParent(visualObj.transform, false);
                faceObj.transform.localPosition = new Vector3(0f, 0f, -0.04f);
                TextMeshPro tmpFace = faceObj.AddComponent<TextMeshPro>();
                tmpFace.font = bubbleFont;
                if (faceMat != null) tmpFace.fontSharedMaterial = faceMat;
                tmpFace.text = $"<b>{letterChar}</b>";
                tmpFace.fontSize = 8.5f;
                tmpFace.alignment = TextAlignmentOptions.Center;
                tmpFace.sortingOrder = 7;
                tmpFace.color = Color.white;
            }

            // =========================
            // ENEMIES (Pengganti Sistem Hazard)
            // =========================

            GameObject enemiesContainer =
                new GameObject("Enemies");

            enemiesContainer.transform.SetParent(
                gameplayContainer.transform,
                false
            );

            Sprite enemySprite = null;
#if UNITY_EDITOR
            enemySprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Enemy.png");
#endif
            if (enemySprite == null)
            {
                enemySprite = Resources.Load<Sprite>("Sprites/Enemy");
            }

            for (
                int h = 0;
                h < data.hazardPositions.Length;
                h++
            )
            {
                Vector2 enemyPos =
                    data.hazardPositions[h];

                GameObject enemyObj =
                    new GameObject(
                        $"Enemy_{h + 1}"
                    );

                enemyObj.transform.SetParent(
                    enemiesContainer.transform,
                    false
                );

                enemyObj.transform.localPosition =
                    new Vector3(
                        enemyPos.x,
                        enemyPos.y,
                        -0.1f
                    );

                SpriteRenderer sr =
                    enemyObj.AddComponent<SpriteRenderer>();

                if (enemySprite != null)
                {
                    sr.sprite = enemySprite;
                }
                sr.sortingOrder = 4;

                CircleCollider2D circleCol =
                    enemyObj.AddComponent<CircleCollider2D>();

                circleCol.isTrigger = true;
                circleCol.radius = 0.38f;

                EnemyCharacter enemyChar =
                    enemyObj.AddComponent<EnemyCharacter>();

                enemyChar.SetBasePosition(
                    new Vector3(
                        enemyPos.x,
                        enemyPos.y,
                        -0.1f
                    )
                );
            }

            // Kontainer Hazards lama tetap dipertahankan kosong / inaktif untuk backward compatibility
            GameObject hazardsContainer =
                new GameObject("Hazards");

            hazardsContainer.transform.SetParent(
                gameplayContainer.transform,
                false
            );

            // =========================
            // GOAL
            // =========================

            GameObject goalObj =
                new GameObject("Goal");

            goalObj.transform.SetParent(
                gameplayContainer.transform,
                false
            );

            goalObj.transform.localPosition =
                new Vector3(
                    data.goalPosition.x,
                    data.goalPosition.y,
                    0f
                );

            BoxCollider2D goalCol =
                goalObj.AddComponent<BoxCollider2D>();

            goalCol.isTrigger = true;

            goalCol.size =
                new Vector2(
                    1.4f,
                    1.4f
                );

            LevelGoalTrigger goalTrigger =
                goalObj.AddComponent<LevelGoalTrigger>();
            goalTrigger.ResetTrigger();

            ExitGateVisual exitGate =
                goalObj.AddComponent<ExitGateVisual>();
            exitGate.Initialize(data.levelNumber);

            GameObject goalVisual =
                new GameObject("Visual");

            goalVisual.transform.SetParent(
                goalObj.transform,
                false
            );

            TextMeshPro goalTmp =
                goalVisual.AddComponent<TextMeshPro>();

            if (defaultFont != null)
                goalTmp.font = defaultFont;

            goalTmp.text =
                "<b>🔒 GATE CLOSED</b>\n" +
                "<size=2.4>" +
                "<color=#FCD34D>" +
                "KUMPULKAN HURUF" +
                "</color></size>";

            goalTmp.fontSize = 4.2f;

            goalTmp.fontStyle =
                FontStyles.Bold;

            goalTmp.alignment =
                TextAlignmentOptions.Center;

            goalTmp.color =
                new Color(
                    0.75f,
                    0.75f,
                    0.80f,
                    0.85f
                );

            goalTmp.sortingOrder = 5;

            RectTransform goalRect =
                goalVisual.GetComponent<RectTransform>();

            goalRect.sizeDelta =
                new Vector2(
                    3.0f,
                    1.2f
                );
        }
    }

    // =========================================================
    // LOCKED GOAL DETECTOR
    // =========================================================

    public class LockedGoalDetector : MonoBehaviour
    {
        private bool isLocked = true;
        private float nextNoticeTime = 0f;

        public void SetLocked(bool locked)
        {
            isLocked = locked;
        }

        private void OnTriggerEnter2D(
            Collider2D collision
        )
        {
            if (
                !collision.CompareTag("Player") &&
                collision.GetComponent<PlayerController>() == null
            )
            {
                return;
            }

            bool isWordComplete =
                WordCollectionManager.Instance != null &&
                WordCollectionManager.Instance.IsWordComplete;

            // =====================================
            // KUNCI
            // =====================================

            if (!isWordComplete || isLocked)
            {
                if (Time.time >= nextNoticeTime)
                {
                    nextNoticeTime =
                        Time.time + 1.2f;

                    if (
                        GameplayFeedbackController.Instance != null
                    )
                    {
                        GameplayFeedbackController.Instance
                            .ShowNoticeMessage(
                                "Kumpulkan semua huruf terlebih dahulu!"
                            );
                    }

                    MazeSpell.Audio.AudioManager
                        .PlaySFXStatic(
                            MazeSpell.Audio.AudioManager.SFXType.GoalLocked
                        );

                    Debug.Log(
                        "[Goal] Pintu masih terkunci!"
                    );
                }

                return;
            }

            // =====================================
            // TERBUKA
            // =====================================

            int currentLevel =
                LevelProgressionManager.Instance != null
                    ? LevelProgressionManager.Instance.CurrentLevel
                    : 1;

            if (MazeGameplayManager.Instance != null)
            {
                MazeGameplayManager.Instance
                    .HandleGoalReached(currentLevel);
            }
        }
    }
}