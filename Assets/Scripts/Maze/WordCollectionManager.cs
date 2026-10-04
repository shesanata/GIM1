using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace MazeSpell
{
    /// <summary>
    /// Mengatur kata target dan pengumpulan huruf untuk setiap level.
    /// Player wajib mengumpulkan semua huruf kata target sebelum pintu Goal dapat dibuka.
    /// Progres kata di-reset penuh setiap kali berpindah level atau mengulang level.
    /// </summary>
    public class WordCollectionManager : MonoBehaviour
    {
        private static WordCollectionManager _instance;
        public static WordCollectionManager Instance
        {
            get
            {
                if (_instance == null)
                {
#if UNITY_2023_1_OR_NEWER
                    _instance = Object.FindAnyObjectByType<WordCollectionManager>();
#else
                    _instance = Object.FindObjectOfType<WordCollectionManager>();
#endif
                    if (_instance == null)
                    {
                        var go = new GameObject("WordCollectionManager");
                        _instance = go.AddComponent<WordCollectionManager>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        public static bool HasInstance => _instance != null;

        [System.Serializable]
        public class LevelWordData
        {
            public int levelNumber = 1;
            public string targetWord = "EARTH";
            public string hint = "";

            public LevelWordData(int level, string word, string wordHint = "")
            {
                levelNumber = level;
                targetWord = word;
                hint = wordHint;
            }
        }

        [System.Serializable]
        public class LetterSlot
        {
            public char character;
            public bool isCollected;
            public CollectibleLetter letterObject;

            public LetterSlot(char c)
            {
                character = char.ToUpperInvariant(c);
                isCollected = false;
                letterObject = null;
            }
        }

        [Header("Daftar Kata per Level (1 - 5)")]
        [SerializeField]
        private LevelWordData[] levelWords =
        {
            new LevelWordData(1, "EARTH", "Elemen tanah dan bumi"),
            new LevelWordData(2, "STONE", "Batu alam pelindung"),
            new LevelWordData(3, "GRASS", "Hamparan rumput hijau"),
            new LevelWordData(4, "FROST", "Kristal es yang dingin"),
            new LevelWordData(5, "FLAME", "Kobaran api membara")
        };

        [Header("Debug HUD (IMGUI)")]
        [Tooltip("Aktifkan hanya jika ingin melihat box debug OnGUI")]
        [SerializeField] private bool showHUD = false;

        [Header("Events")]
        public UnityEvent<int, string> onLevelWordLoaded = new UnityEvent<int, string>();
        public UnityEvent<char, int, int> onLetterCollected = new UnityEvent<char, int, int>();
        public UnityEvent<string, string> onProgressChanged = new UnityEvent<string, string>();
        public UnityEvent<string> onWordCompleted = new UnityEvent<string>();

        private int currentLevelNumber = 1;
        private string currentTargetWord = "EARTH";

        private readonly List<LetterSlot> letterSlots = new List<LetterSlot>();
        private readonly List<CollectibleLetter> activeLetters = new List<CollectibleLetter>();

        private int collectedCount = 0;
        private bool isWordComplete = false;

        public int CurrentLevelNumber => currentLevelNumber;
        public string CurrentTargetWord => currentTargetWord;
        public int CollectedCount => collectedCount;
        public int TotalRequired => letterSlots.Count;
        public bool IsWordComplete => isWordComplete;
        public IReadOnlyList<LetterSlot> Slots => letterSlots;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitializeOnSceneLoad()
        {
            var inst = Instance;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }

            EnsureUnityEvents();
            EnsureDefaultWords();
            SetLevelWord(1);
        }

        private void EnsureUnityEvents()
        {
            if (onLevelWordLoaded == null) onLevelWordLoaded = new UnityEvent<int, string>();
            if (onLetterCollected == null) onLetterCollected = new UnityEvent<char, int, int>();
            if (onProgressChanged == null) onProgressChanged = new UnityEvent<string, string>();
            if (onWordCompleted == null) onWordCompleted = new UnityEvent<string>();
        }

        private void Start()
        {
            EnsureUnityEvents();

            if (LevelProgressionManager.HasInstance)
            {
                var lm = LevelProgressionManager.Instance;
                if (lm.onLevelStarted != null)
                {
                    lm.onLevelStarted.RemoveListener(OnLevelStarted);
                    lm.onLevelStarted.AddListener(OnLevelStarted);
                }

                SetLevelWord(lm.CurrentLevel);
            }
            else
            {
                SetLevelWord(1);
            }
        }

        private void OnDestroy()
        {
            if (LevelProgressionManager.HasInstance)
            {
                var levelManager = LevelProgressionManager.Instance;
                if (levelManager != null && levelManager.onLevelStarted != null)
                {
                    levelManager.onLevelStarted.RemoveListener(OnLevelStarted);
                }
            }

            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void EnsureDefaultWords()
        {
            if (levelWords == null || levelWords.Length != 5)
            {
                levelWords = new LevelWordData[]
                {
                    new LevelWordData(1, "EARTH", "Elemen tanah dan bumi"),
                    new LevelWordData(2, "STONE", "Batu alam pelindung"),
                    new LevelWordData(3, "GRASS", "Hamparan rumput hijau"),
                    new LevelWordData(4, "FROST", "Kristal es yang dingin"),
                    new LevelWordData(5, "FLAME", "Kobaran api membara")
                };
            }
        }

        private void OnLevelStarted(int level)
        {
            SetLevelWord(level);
        }

        /// <summary>
        /// Mendaftarkan objek CollectibleLetter yang aktif di level saat ini.
        /// </summary>
        public void RegisterLetter(CollectibleLetter letter)
        {
            if (letter != null && !activeLetters.Contains(letter))
            {
                activeLetters.Add(letter);
            }
        }

        /// <summary>
        /// Menghapus objek CollectibleLetter saat dihancurkan.
        /// </summary>
        public void UnregisterLetter(CollectibleLetter letter)
        {
            if (letter != null)
            {
                activeLetters.Remove(letter);
            }
        }

        /// <summary>
        /// Mengatur dan me-reset target kata untuk level tertentu.
        /// Menghapus progres huruf level sebelumnya secara menyeluruh.
        /// </summary>
        public void SetLevelWord(int levelNumber)
        {
            currentLevelNumber = levelNumber;

            LevelWordData data = GetWordDataForLevel(levelNumber);
            if (data != null)
            {
                currentTargetWord = data.targetWord.Trim().ToUpperInvariant();
            }
            else
            {
                currentTargetWord = "EARTH";
            }

            if (string.IsNullOrEmpty(currentTargetWord))
            {
                currentTargetWord = "EARTH";
            }

            // RESET TOTAL: huruf sebelumnya tidak boleh terbawa
            letterSlots.Clear();
            activeLetters.Clear();

            for (int i = 0; i < currentTargetWord.Length; i++)
            {
                letterSlots.Add(new LetterSlot(currentTargetWord[i]));
            }

            collectedCount = 0;
            isWordComplete = false;

            Debug.Log($"[WordCollectionManager] Level {currentLevelNumber}: Kata Target = {currentTargetWord} (0/{TotalRequired})");

            onLevelWordLoaded?.Invoke(currentLevelNumber, currentTargetWord);
            onProgressChanged?.Invoke(GetProgressString(), currentTargetWord);
        }

        /// <summary>
        /// Dipanggil oleh CollectibleLetter saat disentuh oleh pemain.
        /// </summary>
        public bool CollectLetter(CollectibleLetter letterObject)
        {
            if (letterObject == null)
                return false;

            return CollectLetter(letterObject.Letter, letterObject);
        }

        /// <summary>
        /// Mengumpulkan satu karakter huruf ke dalam progres kata target.
        /// </summary>
        public bool CollectLetter(char character, CollectibleLetter letterObject = null)
        {
            if (isWordComplete)
                return false;

            char upperCharacter = char.ToUpperInvariant(character);
            LetterSlot targetSlot = null;

            // Cari slot pertama yang karakternya sesuai dan belum terkumpul
            for (int i = 0; i < letterSlots.Count; i++)
            {
                if (letterSlots[i].character == upperCharacter && !letterSlots[i].isCollected)
                {
                    targetSlot = letterSlots[i];
                    break;
                }
            }

            // Jika karakter ini tidak ada atau sudah terkumpul semua instance-nya
            if (targetSlot == null)
            {
                Debug.Log($"[WordCollectionManager] Huruf '{upperCharacter}' tidak diperlukan atau sudah terkumpul.");
                return false;
            }

            targetSlot.isCollected = true;
            targetSlot.letterObject = letterObject;
            collectedCount++;

            if (currentTargetWord == "FLAME")
            {
                Debug.Log($"[FLAME]\nCollected {upperCharacter}");
            }

            Debug.Log($"[WordCollectionManager] Huruf '{upperCharacter}' dikumpulkan. Progress: {GetProgressString()} ({collectedCount}/{TotalRequired})");

            onLetterCollected?.Invoke(upperCharacter, collectedCount, TotalRequired);
            onProgressChanged?.Invoke(GetProgressString(), currentTargetWord);

            // Periksa apakah semua huruf kata target sudah lengkap
            if (collectedCount >= TotalRequired)
            {
                CompleteWord();
            }

            return true;
        }

        private void CompleteWord()
        {
            if (isWordComplete)
                return;

            isWordComplete = true;

            if (currentTargetWord == "FLAME")
            {
                Debug.Log("[FLAME]\nWORD COMPLETE");
                Debug.Log("[FLAME]\nSPELL UNLOCK TRIGGERED");
            }

            Debug.Log($"[WordCollectionManager] ★ KATA '{currentTargetWord}' LENGKAP! Pintu Goal kini dapat dimasuki. ★");

            onWordCompleted?.Invoke(currentTargetWord);

            // PENTING:
            // JANGAN langsung memindahkan level di sini.
            // Pemain tetap harus berjalan menuju pintu Goal untuk menyelesaikan level.
        }

        /// <summary>
        /// Menghasilkan format string progres huruf (contoh: "E _ _ _ H" atau "E A R T H").
        /// </summary>
        public string GetProgressString(char placeholder = '_')
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < letterSlots.Count; i++)
            {
                if (i > 0)
                    sb.Append(" ");

                if (letterSlots[i].isCollected)
                    sb.Append(letterSlots[i].character);
                else
                    sb.Append(placeholder);
            }

            return sb.ToString();
        }

        public string GetTargetWordSpaced()
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < currentTargetWord.Length; i++)
            {
                if (i > 0)
                    sb.Append(" ");

                sb.Append(currentTargetWord[i]);
            }

            return sb.ToString();
        }

        public LevelWordData GetWordDataForLevel(int level)
        {
            if (levelWords == null)
                return null;

            for (int i = 0; i < levelWords.Length; i++)
            {
                if (levelWords[i] != null && levelWords[i].levelNumber == level)
                {
                    return levelWords[i];
                }
            }

            return null;
        }

        private void OnGUI()
        {
            if (!showHUD)
                return;

            float x = 10f;
            float y = 145f;
            float width = 350f;
            float height = 115f;

            GUI.Box(new Rect(x, y, width, height), "🔤 KATA MAZESPELL");

            GUI.Label(new Rect(x + 15, y + 30, width - 30, 25), $"Target : {GetTargetWordSpaced()}");
            GUI.Label(new Rect(x + 15, y + 55, width - 30, 25), $"Kumpul : {GetProgressString()}");
            GUI.Label(new Rect(x + 15, y + 80, width - 30, 25), $"Huruf : {collectedCount}/{TotalRequired}");

            if (isWordComplete)
            {
                GUI.color = Color.green;
                GUI.Label(new Rect(x + 190, y + 80, 145, 25), "✓ KATA LENGKAP!");
                GUI.color = Color.white;
            }
        }
    }
}