using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MazeSpell.Audio;

namespace MazeSpell.Gameplay
{
    /// <summary>
    /// Mengelola animasi Word Power / Spell Unlock saat semua huruf terkumpul:
    /// 1. Huruf-huruf melayang dan berkumpul menuju tengah layar (Convergence).
    /// 2. Huruf menyatu menjadi kata utuh dengan efek scale bounce & glow.
    /// 3. Memancarkan ledakan partikel elemen sesuai tema level (Earth, Stone, Grass, Frost, Flame).
    /// 4. Menampilkan banner "{WORD} SPELL UNLOCKED!".
    /// 5. Khusus Level 5: Memicu cinematic singkat "FLAME SPELL UNLOCKED!" disusul instruksi
    ///    "⚔ DEFEAT THE FINAL GUARDIAN ⚔" sebelum pertarungan pamungkas.
    /// 6. Menjamin tidak terjadi double-trigger pada level completion.
    /// </summary>
    public class WordPowerSpellManager : MonoBehaviour
    {
        private static WordPowerSpellManager _instance;
        public static WordPowerSpellManager Instance
        {
            get
            {
                if (_instance == null)
                {
#if UNITY_2023_1_OR_NEWER
                    _instance = FindAnyObjectByType<WordPowerSpellManager>();
#else
                    _instance = FindObjectOfType<WordPowerSpellManager>();
#endif
                }
                return _instance;
            }
            private set => _instance = value;
        }

        private bool hasTriggeredSpellThisLevel = false;
        private Canvas targetCanvas;
        private GameObject spellSequenceContainer;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }
            _instance = this;
        }

        private void Start()
        {
            HookEvents();
        }

        private void OnDestroy()
        {
            UnhookEvents();
            if (_instance == this) _instance = null;
        }

        public void HookEvents()
        {
            UnhookEvents();
            if (WordCollectionManager.Instance != null)
            {
                WordCollectionManager.Instance.onWordCompleted.AddListener(OnWordCompletedReceived);
            }

            if (LevelProgressionManager.Instance != null)
            {
                LevelProgressionManager.Instance.onLevelStarted.AddListener(OnLevelStarted);
            }
        }

        private void UnhookEvents()
        {
            if (WordCollectionManager.HasInstance && WordCollectionManager.Instance != null)
            {
                WordCollectionManager.Instance.onWordCompleted.RemoveListener(OnWordCompletedReceived);
            }

            if (LevelProgressionManager.HasInstance && LevelProgressionManager.Instance != null)
            {
                LevelProgressionManager.Instance.onLevelStarted.RemoveListener(OnLevelStarted);
            }
        }

        private void OnLevelStarted(int level)
        {
            hasTriggeredSpellThisLevel = false;
            if (spellSequenceContainer != null)
            {
                Destroy(spellSequenceContainer);
            }
        }

        private void OnWordCompletedReceived(string word)
        {
            if (hasTriggeredSpellThisLevel) return;
            hasTriggeredSpellThisLevel = true;

            int currentLevel = 1;
            if (LevelProgressionManager.HasInstance)
            {
                currentLevel = LevelProgressionManager.Instance.CurrentLevel;
            }

            StartCoroutine(ExecuteWordPowerSequence(word, currentLevel));
        }

        private void EnsureCanvas()
        {
            if (targetCanvas != null) return;
#if UNITY_2023_1_OR_NEWER
            targetCanvas = FindAnyObjectByType<Canvas>();
#else
            targetCanvas = FindObjectOfType<Canvas>();
#endif
            if (targetCanvas == null)
            {
                GameObject canvasObj = new GameObject("SpellCanvas");
                targetCanvas = canvasObj.AddComponent<Canvas>();
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                targetCanvas.sortingOrder = 40;
                CanvasScaler cs = canvasObj.AddComponent<CanvasScaler>();
                cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                cs.referenceResolution = new Vector2(1920, 1080);
                canvasObj.AddComponent<GraphicRaycaster>();
            }
        }

        private IEnumerator ExecuteWordPowerSequence(string targetWord, int level)
        {
            EnsureCanvas();
            if (targetCanvas == null) yield break;

            if (spellSequenceContainer != null) Destroy(spellSequenceContainer);
            spellSequenceContainer = new GameObject("WordPowerSpellContainer");
            spellSequenceContainer.transform.SetParent(targetCanvas.transform, false);

            if (level == 5)
            {
                Debug.Log("[BOSS]\nARENA TRANSITION STARTED");
            }

            RectTransform rootRect = spellSequenceContainer.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            // 1. Subtle Screen Flash
            GameObject flashObj = new GameObject("SpellFlash");
            flashObj.transform.SetParent(spellSequenceContainer.transform, false);
            RectTransform flashRect = flashObj.AddComponent<RectTransform>();
            flashRect.anchorMin = Vector2.zero;
            flashRect.anchorMax = Vector2.one;
            flashRect.offsetMin = Vector2.zero;
            flashRect.offsetMax = Vector2.zero;
            Image flashImg = flashObj.AddComponent<Image>();
            flashImg.color = level == 5 ? new Color(1f, 0.4f, 0.1f, 0.45f) : new Color(1f, 1f, 0.8f, 0.4f);
            flashImg.raycastTarget = false;

            // Audio Feedback
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.SFXType.WordComplete);
            }

            // 2. Spawn Flying Letter Badges
            int letterCount = targetWord.Length;
            List<RectTransform> letterRects = new List<RectTransform>();
            List<Vector2> startOffsets = new List<Vector2>();

            float spreadX = 140f;
            float totalWidth = (letterCount - 1) * spreadX;
            float startX = -totalWidth * 0.5f;

            for (int i = 0; i < letterCount; i++)
            {
                GameObject letterBadge = new GameObject($"FlyLetter_{i}");
                letterBadge.transform.SetParent(spellSequenceContainer.transform, false);

                RectTransform badgeRect = letterBadge.AddComponent<RectTransform>();
                badgeRect.anchorMin = new Vector2(0.5f, 0.5f);
                badgeRect.anchorMax = new Vector2(0.5f, 0.5f);
                badgeRect.sizeDelta = new Vector2(85f, 85f);

                // Start from outer positions
                float angle = (i / (float)letterCount) * Mathf.PI * 2f;
                Vector2 initialOffset = new Vector2(Mathf.Cos(angle) * 450f, Mathf.Sin(angle) * 320f);
                badgeRect.anchoredPosition = initialOffset;
                startOffsets.Add(initialOffset);

                Image badgeBg = letterBadge.AddComponent<Image>();
                badgeBg.color = new Color(0.12f, 0.18f, 0.28f, 0.95f);

                GameObject txtObj = new GameObject("Text");
                txtObj.transform.SetParent(letterBadge.transform, false);
                RectTransform txtRect = txtObj.AddComponent<RectTransform>();
                txtRect.anchorMin = Vector2.zero;
                txtRect.anchorMax = Vector2.one;
                txtRect.offsetMin = Vector2.zero;
                txtRect.offsetMax = Vector2.zero;

                TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
                tmp.text = targetWord[i].ToString();
                tmp.fontSize = 46;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = new Color(1.0f, 0.88f, 0.35f, 1.0f);

                letterRects.Add(badgeRect);
            }

            // 3. Letters Converge into Center Line
            float convergeDuration = 0.65f;
            float elapsed = 0f;

            while (elapsed < convergeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / convergeDuration;
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                // Fade out initial flash
                if (flashImg != null)
                {
                    flashImg.color = new Color(flashImg.color.r, flashImg.color.g, flashImg.color.b, Mathf.Lerp(0.4f, 0f, t));
                }

                for (int i = 0; i < letterCount; i++)
                {
                    Vector2 targetPos = new Vector2(startX + i * spreadX, 60f);
                    letterRects[i].anchoredPosition = Vector2.Lerp(startOffsets[i], targetPos, smoothT);
                    letterRects[i].localScale = Vector3.Lerp(Vector3.one * 0.4f, Vector3.one * 1.15f, smoothT);
                }

                yield return null;
            }

            // 4. Merge into Glowing Word Banner
            for (int i = 0; i < letterCount; i++)
            {
                Destroy(letterRects[i].gameObject);
            }

            // Spawn Elemental Particle Burst at Player position
            Vector3 playerWorldPos = Vector3.zero;
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) playerWorldPos = playerObj.transform.position;
            ElementalParticleSpawner.SpawnElementEffect(level, playerWorldPos);

            // Camera subtle shake for impact
            if (level == 5)
            {
                StartCoroutine(CameraShakeRoutine(0.4f, 0.15f));
            }

            // Spell Unlocked Banner
            GameObject bannerObj = new GameObject("SpellUnlockedBanner");
            bannerObj.transform.SetParent(spellSequenceContainer.transform, false);

            RectTransform bannerRect = bannerObj.AddComponent<RectTransform>();
            bannerRect.anchorMin = new Vector2(0.5f, 0.5f);
            bannerRect.anchorMax = new Vector2(0.5f, 0.5f);
            bannerRect.sizeDelta = new Vector2(720f, 160f);
            bannerRect.anchoredPosition = new Vector2(0f, 40f);

            Image bannerBg = bannerObj.AddComponent<Image>();
            bannerBg.color = new Color(0.06f, 0.10f, 0.18f, 0.95f);

            // Golden Border Line
            GameObject glowLine = new GameObject("GlowLine");
            glowLine.transform.SetParent(bannerObj.transform, false);
            RectTransform glowRect = glowLine.AddComponent<RectTransform>();
            glowRect.anchorMin = new Vector2(0.05f, 0f);
            glowRect.anchorMax = new Vector2(0.95f, 0.05f);
            glowRect.offsetMin = Vector2.zero;
            glowRect.offsetMax = Vector2.zero;
            Image glowImg = glowLine.AddComponent<Image>();
            glowImg.color = GetElementThemeColor(level);

            // Word Title
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(bannerObj.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.45f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = $"★  {targetWord}  ★";
            titleTmp.fontSize = 58;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.characterSpacing = 8f;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = GetElementThemeColor(level);

            // Subtitle
            GameObject subObj = new GameObject("SubtitleText");
            subObj.transform.SetParent(bannerObj.transform, false);
            RectTransform subRect = subObj.AddComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0f, 0f);
            subRect.anchorMax = new Vector2(1f, 0.5f);
            subRect.offsetMin = Vector2.zero;
            subRect.offsetMax = Vector2.zero;
            TextMeshProUGUI subTmp = subObj.AddComponent<TextMeshProUGUI>();
            subTmp.text = $"{targetWord} SPELL UNLOCKED!";
            subTmp.fontSize = 28;
            subTmp.fontStyle = FontStyles.Bold;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.color = Color.white;

            // Punch Bounce Animation
            yield return StartCoroutine(AnimatePunchScale(bannerRect, 1.35f, 0.35f));

            // Wait 1.6s
            yield return new WaitForSeconds(1.6f);

            // FITUR 8: Khusus Level 5 -> Tampilkan Prompt FINAL GUARDIAN
            if (level == 5)
            {
                subTmp.text = "⚔ DEFEAT THE FINAL GUARDIAN ⚔";
                subTmp.color = new Color(1.0f, 0.35f, 0.15f, 1.0f);
                subTmp.fontSize = 32;
                yield return StartCoroutine(AnimatePunchScale(bannerRect, 1.25f, 0.3f));
                yield return new WaitForSeconds(1.8f);
            }

            // Smooth Fade Out
            CanvasGroup cg = bannerObj.AddComponent<CanvasGroup>();
            float fadeElapsed = 0f;
            float fadeDuration = 0.45f;
            while (fadeElapsed < fadeDuration)
            {
                fadeElapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(1f, 0f, fadeElapsed / fadeDuration);
                yield return null;
            }

            Destroy(spellSequenceContainer);

            // Khusus Level 5: Pemicu Transisi FINAL BOSS ARENA & GUARDIAN
            if (level == 5)
            {
                var bossMgr = FinalBossBattleManager.Instance != null ? FinalBossBattleManager.Instance : FindAnyObjectByType<FinalBossBattleManager>();
                if (bossMgr != null)
                {
                    bossMgr.StartBossArenaTransition();
                }
                else
                {
                    Debug.LogError("[WordPowerSpellManager] FinalBossBattleManager tidak ditemukan saat memicu arena bos Level 5!");
                }
            }
        }

        private Color GetElementThemeColor(int level)
        {
            switch (level)
            {
                case 1: return new Color(0.85f, 0.65f, 0.35f, 1f); // Earth Amber
                case 2: return new Color(0.70f, 0.78f, 0.85f, 1f); // Stone Slate
                case 3: return new Color(0.35f, 0.90f, 0.45f, 1f); // Grass Emerald
                case 4: return new Color(0.40f, 0.85f, 1.00f, 1f); // Frost Cyan
                case 5: return new Color(1.00f, 0.45f, 0.15f, 1f); // Flame Fire
                default: return new Color(1.0f, 0.85f, 0.3f, 1f);
            }
        }

        private IEnumerator AnimatePunchScale(RectTransform rect, float punchTarget, float duration)
        {
            if (rect == null) yield break;
            float half = duration * 0.5f;
            float elapsed = 0f;

            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                rect.localScale = Vector3.Lerp(Vector3.one, Vector3.one * punchTarget, elapsed / half);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                rect.localScale = Vector3.Lerp(Vector3.one * punchTarget, Vector3.one, elapsed / half);
                yield return null;
            }
            rect.localScale = Vector3.one;
        }

        private IEnumerator CameraShakeRoutine(float duration, float magnitude)
        {
            Camera cam = Camera.main;
            if (cam == null) yield break;
            Vector3 originalPos = cam.transform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float damper = 1f - (elapsed / duration);
                float x = (UnityEngine.Random.value * 2f - 1f) * magnitude * damper;
                float y = (UnityEngine.Random.value * 2f - 1f) * magnitude * damper;
                cam.transform.position = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
                yield return null;
            }
            cam.transform.position = originalPos;
        }
    }
}
