using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MazeSpell.Audio;

namespace MazeSpell.UI
{
    /// <summary>
    /// Mengelola umpan balik gameplay (Gameplay Feedback) untuk MazeSpell:
    /// 1. Collect Letter Feedback (+ LETTER visual popup, pulse, dan audio chime).
    /// 2. Hazard Feedback (Screen flash merah, floating OUCH! -1 NYAWA, player tint, audio thud).
    /// 3. Word Complete Feedback (Banner KATA SELESAI selama 1.5 detik dengan audio fanfare kemenangan).
    /// 4. Level Transition (Transisi fade halus dengan pengumuman nomor level dan nama tema).
    /// Terintegrasi dengan GameplayCanvas tanpa mengubah sistem inti dan tanpa asset audio eksternal.
    /// </summary>
    public class GameplayFeedbackController : MonoBehaviour
    {
        public static GameplayFeedbackController Instance { get; private set; }

        private static readonly string[] DefaultThemeNames = new string[]
        {
            "GROUND / EARTH",
            "STONE / EARTH",
            "GRASS",
            "SNOW / ICE",
            "LAVA / VOLCANO"
        };

        // Komponen UI internal di GameplayCanvas
        private RectTransform feedbackContainer;
        private Image damageVignette;
        private GameObject wordCompletePanel;
        private TextMeshProUGUI wordCompleteTitle;
        private TextMeshProUGUI wordCompleteSubtitle;
        private TextMeshProUGUI wordCompleteWordText;

        // Komponen Transisi Level
        private CanvasGroup transitionOverlay;
        private TextMeshProUGUI transitionLevelText;
        private TextMeshProUGUI transitionThemeText;

        // Audio Prosedural
        private AudioSource audioSource;
        private AudioClip letterChimeClip;
        private AudioClip hazardThudClip;
        private AudioClip victoryFanfareClip;

        // Referensi Player
        private GameObject cachedPlayer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitializeOnSceneLoad()
        {
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (activeScene.name != "SampleScene") return;

#if UNITY_2023_1_OR_NEWER
            if (FindAnyObjectByType<GameplayFeedbackController>() != null) return;
#else
            if (FindObjectOfType<GameplayFeedbackController>() != null) return;
#endif

            GameObject runner = new GameObject("[GameplayFeedbackInitializer]");
            runner.AddComponent<GameplayFeedbackInitializer>();
        }

        private class GameplayFeedbackInitializer : MonoBehaviour
        {
            private IEnumerator Start()
            {
                yield return null; // Tunggu 1 frame agar GameplayCanvas & HUD sudah siap
                CreateRuntimeFeedback();
                Destroy(gameObject);
            }
        }

        public static void CreateRuntimeFeedback()
        {
#if UNITY_2023_1_OR_NEWER
            if (FindAnyObjectByType<GameplayFeedbackController>() != null) return;
#else
            if (FindObjectOfType<GameplayFeedbackController>() != null) return;
#endif

            // Cari GameplayCanvas yang sudah ada
            Transform parentCanvas = null;
#if UNITY_2023_1_OR_NEWER
            var hud = FindAnyObjectByType<GameplayHUD>();
            var anyCanvas = FindAnyObjectByType<Canvas>();
#else
            var hud = FindObjectOfType<GameplayHUD>();
            var anyCanvas = FindObjectOfType<Canvas>();
#endif
            if (hud != null) parentCanvas = hud.transform;
            else if (anyCanvas != null) parentCanvas = anyCanvas.transform;
            else
            {
                GameObject canvasObj = new GameObject("GameplayCanvas");
                Canvas c = canvasObj.AddComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                c.sortingOrder = 15;
                CanvasScaler cs = canvasObj.AddComponent<CanvasScaler>();
                cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                cs.referenceResolution = new Vector2(1920, 1080);
                cs.matchWidthOrHeight = 0.5f;
                canvasObj.AddComponent<GraphicRaycaster>();
                parentCanvas = canvasObj.transform;
            }

            GameObject feedbackObj = new GameObject("GameplayFeedback");
            feedbackObj.transform.SetParent(parentCanvas, false);

            var controller = feedbackObj.AddComponent<GameplayFeedbackController>();
            controller.BuildUIElements(feedbackObj.transform);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            SetupProceduralAudio();
        }

        private void Start()
        {
            FindPlayer();
            HookEvents();
            ConfigureWordCollectionAutoAdvance();
        }

        private void OnDestroy()
        {
            UnhookEvents();
        }

        /// <summary>
        /// Mengatur agar WordCollectionManager tidak memicu pergantian level instan,
        /// sehingga banner 'KATA SELESAI' dapat tampil 1.5 detik terlebih dahulu.
        /// </summary>
        private void ConfigureWordCollectionAutoAdvance()
        {
            if (WordCollectionManager.Instance != null)
            {
                var field = typeof(WordCollectionManager).GetField("autoAdvanceLevelOnCompletion",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(WordCollectionManager.Instance, false);
                }
            }
        }

        private void FindPlayer()
        {
            if (cachedPlayer == null)
            {
                cachedPlayer = GameObject.FindWithTag("Player");
                if (cachedPlayer == null)
                {
#if UNITY_2023_1_OR_NEWER
                    var pc = FindAnyObjectByType<PlayerController>();
#else
                    var pc = FindObjectOfType<PlayerController>();
#endif
                    if (pc != null) cachedPlayer = pc.gameObject;
                }
            }
        }

        private void HookEvents()
        {
            if (WordCollectionManager.Instance != null)
            {
                if (WordCollectionManager.Instance.onLetterCollected != null)
                    WordCollectionManager.Instance.onLetterCollected.AddListener(OnLetterCollected);
                if (WordCollectionManager.Instance.onWordCompleted != null)
                    WordCollectionManager.Instance.onWordCompleted.AddListener(OnWordCompleted);
            }

            if (PlayerLivesManager.Instance != null && PlayerLivesManager.Instance.onDamageTaken != null)
            {
                PlayerLivesManager.Instance.onDamageTaken.AddListener(OnDamageTaken);
            }
        }

        private void UnhookEvents()
        {
            if (WordCollectionManager.Instance != null)
            {
                if (WordCollectionManager.Instance.onLetterCollected != null)
                    WordCollectionManager.Instance.onLetterCollected.RemoveListener(OnLetterCollected);
                if (WordCollectionManager.Instance.onWordCompleted != null)
                    WordCollectionManager.Instance.onWordCompleted.RemoveListener(OnWordCompleted);
            }

            if (PlayerLivesManager.Instance != null && PlayerLivesManager.Instance.onDamageTaken != null)
            {
                PlayerLivesManager.Instance.onDamageTaken.RemoveListener(OnDamageTaken);
            }
        }

        #region UI Construction

        public void BuildUIElements(Transform parentTransform)
        {
            TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;

            // 1. Container untuk floating text popups
            GameObject containerObj = new GameObject("FloatingTextContainer");
            containerObj.transform.SetParent(parentTransform, false);
            feedbackContainer = containerObj.AddComponent<RectTransform>();
            feedbackContainer.anchorMin = Vector2.zero;
            feedbackContainer.anchorMax = Vector2.one;
            feedbackContainer.offsetMin = Vector2.zero;
            feedbackContainer.offsetMax = Vector2.zero;

            // 2. Damage Vignette Flash Overlay
            GameObject vignetteObj = new GameObject("DamageVignette");
            vignetteObj.transform.SetParent(parentTransform, false);
            RectTransform vigRect = vignetteObj.AddComponent<RectTransform>();
            vigRect.anchorMin = Vector2.zero;
            vigRect.anchorMax = Vector2.one;
            vigRect.offsetMin = Vector2.zero;
            vigRect.offsetMax = Vector2.zero;
            damageVignette = vignetteObj.AddComponent<Image>();
            damageVignette.color = new Color(0.9f, 0.1f, 0.1f, 0f);
            damageVignette.raycastTarget = false;

            // 3. Word Complete Panel
            GameObject wordCompleteObj = new GameObject("WordCompletePanel");
            wordCompleteObj.transform.SetParent(parentTransform, false);
            RectTransform wcRect = wordCompleteObj.AddComponent<RectTransform>();
            wcRect.anchorMin = new Vector2(0.5f, 0.5f);
            wcRect.anchorMax = new Vector2(0.5f, 0.5f);
            wcRect.pivot = new Vector2(0.5f, 0.5f);
            wcRect.sizeDelta = new Vector2(620f, 260f);
            wcRect.anchoredPosition = new Vector2(0f, 40f);

            Image wcBg = wordCompleteObj.AddComponent<Image>();
            wcBg.color = new Color(0.05f, 0.09f, 0.16f, 0.94f); // Sleek dark card
            wcBg.raycastTarget = false;

            // Header Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(wordCompleteObj.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.65f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(16f, 0f);
            titleRect.offsetMax = new Vector2(-16f, -10f);
            wordCompleteTitle = titleObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) wordCompleteTitle.font = defaultFont;
            wordCompleteTitle.text = "<b>KATA SELESAI!</b>";
            wordCompleteTitle.fontSize = 44;
            wordCompleteTitle.fontStyle = FontStyles.Bold;
            wordCompleteTitle.alignment = TextAlignmentOptions.Center;
            wordCompleteTitle.color = new Color(0.99f, 0.85f, 0.35f, 1f); // Warm Gold
            wordCompleteTitle.raycastTarget = false;

            // Subtitle
            GameObject subObj = new GameObject("Subtitle");
            subObj.transform.SetParent(wordCompleteObj.transform, false);
            RectTransform subRect = subObj.AddComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0f, 0.38f);
            subRect.anchorMax = new Vector2(1f, 0.68f);
            subRect.offsetMin = new Vector2(16f, 0f);
            subRect.offsetMax = new Vector2(-16f, 0f);
            wordCompleteSubtitle = subObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) wordCompleteSubtitle.font = defaultFont;
            wordCompleteSubtitle.text = "Kamu berhasil menemukan semua huruf!";
            wordCompleteSubtitle.fontSize = 20;
            wordCompleteSubtitle.fontStyle = FontStyles.Bold;
            wordCompleteSubtitle.alignment = TextAlignmentOptions.Center;
            wordCompleteSubtitle.color = new Color(0.58f, 0.77f, 0.99f, 1f); // Soft Blue
            wordCompleteSubtitle.raycastTarget = false;

            // Completed Word Display
            GameObject wordTextObj = new GameObject("WordText");
            wordTextObj.transform.SetParent(wordCompleteObj.transform, false);
            RectTransform wordRect = wordTextObj.AddComponent<RectTransform>();
            wordRect.anchorMin = new Vector2(0f, 0.08f);
            wordRect.anchorMax = new Vector2(1f, 0.40f);
            wordRect.offsetMin = new Vector2(16f, 0f);
            wordRect.offsetMax = new Vector2(-16f, 0f);
            wordCompleteWordText = wordTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) wordCompleteWordText.font = defaultFont;
            wordCompleteWordText.text = "★  E A R T H  ★";
            wordCompleteWordText.fontSize = 28;
            wordCompleteWordText.fontStyle = FontStyles.Bold;
            wordCompleteWordText.alignment = TextAlignmentOptions.Center;
            wordCompleteWordText.color = new Color(0.20f, 0.88f, 0.55f, 1f); // Emerald
            wordCompleteWordText.characterSpacing = 6f;
            wordCompleteWordText.raycastTarget = false;

            wordCompletePanel = wordCompleteObj;
            wordCompletePanel.SetActive(false);

            // 4. Level Transition Overlay
            GameObject transObj = new GameObject("LevelTransitionOverlay");
            transObj.transform.SetParent(parentTransform, false);
            RectTransform transRect = transObj.AddComponent<RectTransform>();
            transRect.anchorMin = Vector2.zero;
            transRect.anchorMax = Vector2.one;
            transRect.offsetMin = Vector2.zero;
            transRect.offsetMax = Vector2.zero;

            Image transBg = transObj.AddComponent<Image>();
            transBg.color = new Color(0.03f, 0.05f, 0.09f, 1f); // Deep dark navy fade
            transBg.raycastTarget = true; // Blokir input selama transisi level

            transitionOverlay = transObj.AddComponent<CanvasGroup>();
            transitionOverlay.alpha = 0f;
            transitionOverlay.blocksRaycasts = false;

            // Transition Level Title
            GameObject lvlObj = new GameObject("TransitionLevelText");
            lvlObj.transform.SetParent(transObj.transform, false);
            RectTransform lvlRect = lvlObj.AddComponent<RectTransform>();
            lvlRect.anchorMin = new Vector2(0.5f, 0.5f);
            lvlRect.anchorMax = new Vector2(0.5f, 0.5f);
            lvlRect.sizeDelta = new Vector2(700f, 90f);
            lvlRect.anchoredPosition = new Vector2(0f, 35f);
            transitionLevelText = lvlObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) transitionLevelText.font = defaultFont;
            transitionLevelText.text = "LEVEL 2";
            transitionLevelText.fontSize = 58;
            transitionLevelText.fontStyle = FontStyles.Bold;
            transitionLevelText.alignment = TextAlignmentOptions.Center;
            transitionLevelText.color = new Color(0.99f, 0.85f, 0.35f, 1f);
            transitionLevelText.raycastTarget = false;

            // Transition Theme Subtitle
            GameObject thmObj = new GameObject("TransitionThemeText");
            thmObj.transform.SetParent(transObj.transform, false);
            RectTransform thmRect = thmObj.AddComponent<RectTransform>();
            thmRect.anchorMin = new Vector2(0.5f, 0.5f);
            thmRect.anchorMax = new Vector2(0.5f, 0.5f);
            thmRect.sizeDelta = new Vector2(700f, 60f);
            thmRect.anchoredPosition = new Vector2(0f, -30f);
            transitionThemeText = thmObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) transitionThemeText.font = defaultFont;
            transitionThemeText.text = "GRASS";
            transitionThemeText.fontSize = 28;
            transitionThemeText.fontStyle = FontStyles.Bold;
            transitionThemeText.alignment = TextAlignmentOptions.Center;
            transitionThemeText.color = new Color(0.35f, 0.75f, 1f, 1f);
            transitionThemeText.raycastTarget = false;
        }

        #endregion

        #region Feedback Audio & Event Handlers

        private void SetupProceduralAudio()
        {
            // Audio kini dikelola terpusat oleh AudioManager
        }

        private void OnLetterCollected(char letter, int collected, int total)
        {
            FindPlayer();
            Vector3 worldPos = cachedPlayer != null ? cachedPlayer.transform.position + Vector3.up * 0.8f : Vector3.zero;
            Vector2 screenPos = WorldToCanvasPosition(worldPos);

            StartCoroutine(AnimateFloatingText($"+ {letter}", screenPos, new Color(0.99f, 0.85f, 0.35f, 1f), 26));

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.SFXType.CollectLetter);
            }
        }

        private void OnDamageTaken()
        {
            FindPlayer();
            Vector3 worldPos = cachedPlayer != null ? cachedPlayer.transform.position + Vector3.up * 0.8f : Vector3.zero;
            Vector2 screenPos = WorldToCanvasPosition(worldPos);

            StartCoroutine(AnimateFloatingText("OUCH! -1 NYAWA", screenPos, new Color(0.95f, 0.30f, 0.30f, 1f), 24));
            StartCoroutine(FlashDamageVignette());

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.SFXType.DamageTaken);
            }
        }

        private IEnumerator FlashDamageVignette()
        {
            if (damageVignette == null) yield break;

            float elapsed = 0f;
            float duration = 0.35f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Sin(elapsed / duration * Mathf.PI) * 0.45f;
                damageVignette.color = new Color(0.9f, 0.1f, 0.1f, alpha);
                yield return null;
            }

            damageVignette.color = new Color(0.9f, 0.1f, 0.1f, 0f);
        }

        public void ShowNoticeMessage(string message)
        {
            FindPlayer();
            Vector3 worldPos = cachedPlayer != null ? cachedPlayer.transform.position + Vector3.up * 0.75f : Vector3.zero;
            Vector2 screenPos = WorldToCanvasPosition(worldPos);

            StartCoroutine(AnimateFloatingText(message, screenPos, new Color(1.0f, 0.82f, 0.32f, 1f), 22));

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.SFXType.GoalLocked);
            }
        }

        #endregion

        #region 3. Word Complete & 4. Level Transition

        private void OnWordCompleted(string completedWord)
        {
            StartCoroutine(WordCompleteSequence(completedWord));
        }

        private IEnumerator WordCompleteSequence(string completedWord)
        {
            if (MazeSpell.Gameplay.WordPowerSpellManager.Instance != null)
            {
                // WordPowerSpellManager menangani animasi konvergensi huruf dan spell unlock secara lebih kaya
                yield return new WaitForSeconds(1.8f);
                ShowNoticeMessage("★ PINTU KELUAR TERBUKA! MENUJU GOAL ★");
                yield break;
            }

            // Tampilkan panel 'KATA SELESAI!' selama 1.5 detik
            if (wordCompletePanel != null)
            {
                if (wordCompleteWordText != null)
                {
                    wordCompleteWordText.text = $"★  {FormatSpacedWord(completedWord)}  ★";
                }

                wordCompletePanel.SetActive(true);
                AudioManager.PlaySFXStatic(AudioManager.SFXType.WordComplete);

                RectTransform wcRect = wordCompletePanel.GetComponent<RectTransform>();
                StartCoroutine(AnimatePopIn(wcRect, 0.28f));

                yield return new WaitForSeconds(1.5f);

                wordCompletePanel.SetActive(false);
            }

            // Beri pesan panduan bahwa pintu keluar / goal kini telah terbuka
            ShowNoticeMessage("★ PINTU KELUAR TERBUKA! MENUJU GOAL ★");
        }

        /// <summary>
        /// Menjalankan transisi fade ke level berikutnya saat pemain mencapai goal.
        /// </summary>
        public void ExecuteLevelTransition(int nextLevel, Action onMidpoint)
        {
            StartCoroutine(LevelTransitionRoutine(nextLevel, onMidpoint));
        }

        private IEnumerator LevelTransitionRoutine(int nextLevel, Action onMidpoint)
        {
            string themeName = GetThemeForLevel(nextLevel);

            if (transitionLevelText != null)
            {
                transitionLevelText.text = $"LEVEL {nextLevel}";
            }
            if (transitionThemeText != null)
            {
                transitionThemeText.text = themeName;
            }

            // 1. Fade to dark
            if (transitionOverlay != null)
            {
                transitionOverlay.blocksRaycasts = true;
                yield return StartCoroutine(FadeCanvasGroup(transitionOverlay, 0f, 1f, 0.35f));
            }

            // Tahan sejenak agar pemain dapat membaca nama level & tema (0.8s)
            yield return new WaitForSeconds(0.8f);

            // Panggil aksi midpoint (memuat level baru)
            onMidpoint?.Invoke();

            // Pastikan WordCollectionManager terkonfigurasi untuk level baru
            ConfigureWordCollectionAutoAdvance();

            // 2. Fade back to gameplay
            if (transitionOverlay != null)
            {
                yield return StartCoroutine(FadeCanvasGroup(transitionOverlay, 1f, 0f, 0.35f));
                transitionOverlay.blocksRaycasts = false;
            }
        }

        private string GetThemeForLevel(int level)
        {
            if (LevelProgressionManager.Instance != null)
            {
                var config = LevelProgressionManager.Instance.GetConfigForLevel(level);
                if (config != null && config.levelData != null && !string.IsNullOrEmpty(config.levelData.themeName))
                {
                    return config.levelData.themeName.ToUpperInvariant();
                }
            }

            int index = Mathf.Clamp(level - 1, 0, DefaultThemeNames.Length - 1);
            return DefaultThemeNames[index];
        }

        private string FormatSpacedWord(string word)
        {
            if (string.IsNullOrEmpty(word)) return "";
            System.Text.StringBuilder sb = new System.Text.StringBuilder(word.Length * 2);
            for (int i = 0; i < word.Length; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(char.ToUpperInvariant(word[i]));
            }
            return sb.ToString();
        }

        #endregion

        #region Animasi Pembantu

        private IEnumerator AnimateFloatingText(string message, Vector2 startPosition, Color textColor, float fontSize)
        {
            if (feedbackContainer == null) yield break;

            GameObject textObj = new GameObject("FloatingText");
            textObj.transform.SetParent(feedbackContainer, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(360f, 60f);
            rect.anchoredPosition = startPosition;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont != null) tmp.font = defaultFont;
            tmp.text = $"<b>{message}</b>";
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = textColor;
            tmp.raycastTarget = false;

            float elapsed = 0f;
            float duration = 0.85f;
            Vector2 initialPos = startPosition;
            Vector2 targetPos = startPosition + new Vector2(0f, 65f);

            rect.localScale = Vector3.one * 0.6f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Punch scale di awal
                if (t < 0.25f)
                {
                    float punchT = t / 0.25f;
                    rect.localScale = Vector3.Lerp(Vector3.one * 0.6f, Vector3.one * 1.25f, punchT);
                }
                else if (t < 0.45f)
                {
                    float settleT = (t - 0.25f) / 0.20f;
                    rect.localScale = Vector3.Lerp(Vector3.one * 1.25f, Vector3.one, settleT);
                }
                else
                {
                    rect.localScale = Vector3.one;
                }

                // Bergerak perlahan ke atas
                rect.anchoredPosition = Vector2.Lerp(initialPos, targetPos, Mathf.SmoothStep(0f, 1f, t));

                // Fade out di bagian akhir
                if (t > 0.45f)
                {
                    float alphaT = (t - 0.45f) / 0.55f;
                    tmp.color = new Color(textColor.r, textColor.g, textColor.b, Mathf.Lerp(1f, 0f, alphaT));
                }

                yield return null;
            }

            Destroy(textObj);
        }

        private IEnumerator AnimatePopIn(RectTransform rect, float duration)
        {
            if (rect == null) yield break;

            float elapsed = 0f;
            rect.localScale = Vector3.one * 0.4f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;

                // Overshoot bounce
                float scale = 0.4f + 0.6f * Mathf.Sin(t * Mathf.PI * 0.5f);
                if (t > 0.7f)
                {
                    scale = Mathf.Lerp(1.08f, 1.0f, (t - 0.7f) / 0.3f);
                }
                rect.localScale = Vector3.one * scale;
                yield return null;
            }

            rect.localScale = Vector3.one;
        }

        private IEnumerator AnimatePunchScale(RectTransform rect, float punchTarget, float duration)
        {
            if (rect == null) yield break;

            float halfDuration = duration * 0.5f;
            float elapsed = 0f;
            Vector3 originalScale = rect.localScale;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                rect.localScale = Vector3.Lerp(originalScale, originalScale * punchTarget, elapsed / halfDuration);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                rect.localScale = Vector3.Lerp(originalScale * punchTarget, originalScale, elapsed / halfDuration);
                yield return null;
            }

            rect.localScale = originalScale;
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
        {
            if (cg == null) yield break;

            float elapsed = 0f;
            cg.alpha = from;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            cg.alpha = to;
        }

        private Vector2 WorldToCanvasPosition(Vector3 worldPos)
        {
            if (Camera.main != null)
            {
                Vector3 screenPoint = Camera.main.WorldToScreenPoint(worldPos);
                // Konversi screen point ke anchored position di dalam CanvasScaler (1920x1080)
                float screenW = Screen.width > 0 ? Screen.width : 1920f;
                float screenH = Screen.height > 0 ? Screen.height : 1080f;

                float canvasX = (screenPoint.x / screenW - 0.5f) * 1920f;
                float canvasY = (screenPoint.y / screenH - 0.5f) * 1080f;

                return new Vector2(canvasX, canvasY);
            }
            return Vector2.zero;
        }

        #endregion
    }
}
