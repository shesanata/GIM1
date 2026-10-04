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
    /// Menampilkan perayaan Level Complete (Fitur 9):
    /// - Banner "LEVEL {X} COMPLETE!"
    /// - Subtitle "{WORD} SPELL RESTORED"
    /// - Animasi bintang ⭐⭐⭐ muncul satu per satu dengan efek scale bounce & chime
    /// - Hujan partikel confetti kemenangan
    /// - Setelah 2.5 detik, melanjutkan ke level berikutnya secara mulus.
    /// </summary>
    public class LevelCompletePopup : MonoBehaviour
    {
        public static LevelCompletePopup Instance { get; private set; }

        private bool isShowing = false;
        private Canvas targetCanvas;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        public void ShowLevelComplete(int level, string word, Action onFinished)
        {
            if (isShowing) return;
            isShowing = true;

            int damageTaken = 0;
            if (PlayerLivesManager.Instance != null)
            {
                damageTaken = PlayerLivesManager.Instance.DamageTakenThisLevel;
            }

            int earnedStars = StarRatingManager.CalculateStars(damageTaken);
            StarRatingManager.SaveStarsForLevel(level, earnedStars);

            StartCoroutine(LevelCompleteRoutine(level, word, earnedStars, onFinished));
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
                GameObject canvasObj = new GameObject("VictoryCanvas");
                targetCanvas = canvasObj.AddComponent<Canvas>();
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                targetCanvas.sortingOrder = 50;
                CanvasScaler cs = canvasObj.AddComponent<CanvasScaler>();
                cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                cs.referenceResolution = new Vector2(1920, 1080);
                canvasObj.AddComponent<GraphicRaycaster>();
            }
        }

        private IEnumerator LevelCompleteRoutine(int level, string word, int stars, Action onFinished)
        {
            EnsureCanvas();
            if (targetCanvas == null)
            {
                onFinished?.Invoke();
                yield break;
            }

            // Confetti di posisi pemain
            var playerObj = GameObject.FindWithTag("Player");
            Vector3 playerPos = playerObj != null ? playerObj.transform.position : Vector3.zero;
            ElementalParticleSpawner.SpawnVictoryConfetti(playerPos);

            // Audio Kemenangan
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.SFXType.GameComplete);
            }

            // Root Container
            GameObject popupRoot = new GameObject("LevelCompletePopupContainer");
            popupRoot.transform.SetParent(targetCanvas.transform, false);

            RectTransform rootRect = popupRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            // Background Panel
            GameObject panelObj = new GameObject("ModalPanel");
            panelObj.transform.SetParent(popupRoot.transform, false);
            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(760f, 420f);
            panelRect.anchoredPosition = new Vector2(0f, 20f);

            Image panelBg = panelObj.AddComponent<Image>();
            panelBg.color = new Color(0.05f, 0.09f, 0.16f, 0.96f);

            // Title
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(panelObj.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.68f);
            titleRect.anchorMax = new Vector2(1f, 0.95f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = $"LEVEL {level} COMPLETE!";
            titleTmp.fontSize = 54;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(1.0f, 0.88f, 0.35f, 1.0f);

            // Subtitle
            GameObject subObj = new GameObject("SubtitleText");
            subObj.transform.SetParent(panelObj.transform, false);
            RectTransform subRect = subObj.AddComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0f, 0.50f);
            subRect.anchorMax = new Vector2(1f, 0.68f);
            subRect.offsetMin = Vector2.zero;
            subRect.offsetMax = Vector2.zero;
            TextMeshProUGUI subTmp = subObj.AddComponent<TextMeshProUGUI>();
            subTmp.text = $"{word} SPELL RESTORED";
            subTmp.fontSize = 30;
            subTmp.fontStyle = FontStyles.Bold;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.color = new Color(0.40f, 0.85f, 1.0f, 1.0f);

            // Star Container
            GameObject starContainer = new GameObject("StarContainer");
            starContainer.transform.SetParent(panelObj.transform, false);
            RectTransform starContRect = starContainer.AddComponent<RectTransform>();
            starContRect.anchorMin = new Vector2(0.5f, 0.25f);
            starContRect.anchorMax = new Vector2(0.5f, 0.25f);
            starContRect.sizeDelta = new Vector2(360f, 90f);
            starContRect.anchoredPosition = Vector2.zero;

            List<TextMeshProUGUI> starList = new List<TextMeshProUGUI>();
            for (int i = 0; i < 3; i++)
            {
                GameObject starObj = new GameObject($"Star_{i + 1}");
                starObj.transform.SetParent(starContainer.transform, false);
                RectTransform starRect = starObj.AddComponent<RectTransform>();
                starRect.anchorMin = new Vector2(0.5f, 0.5f);
                starRect.anchorMax = new Vector2(0.5f, 0.5f);
                starRect.sizeDelta = new Vector2(90f, 90f);
                starRect.anchoredPosition = new Vector2((i - 1) * 110f, 0f);
                starRect.localScale = Vector3.zero;

                TextMeshProUGUI starTmp = starObj.AddComponent<TextMeshProUGUI>();
                starTmp.text = "★";
                starTmp.fontSize = 76;
                starTmp.fontStyle = FontStyles.Bold;
                starTmp.alignment = TextAlignmentOptions.Center;
                starTmp.color = (i < stars) ? new Color(1.0f, 0.85f, 0.2f, 1f) : new Color(0.4f, 0.4f, 0.45f, 0.6f);
                starList.Add(starTmp);
            }

            // Punch Modal Panel In
            yield return StartCoroutine(AnimatePopIn(panelRect, 0.35f));

            // Stars Pop in Sequentially
            for (int i = 0; i < 3; i++)
            {
                yield return new WaitForSeconds(0.22f);
                RectTransform sRect = starList[i].GetComponent<RectTransform>();
                StartCoroutine(AnimatePopIn(sRect, 0.25f));
                if (i < stars && AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(AudioManager.SFXType.ButtonClick, 1.1f);
                }
            }

            // Display for remaining duration
            yield return new WaitForSeconds(1.8f);

            // Fade out
            CanvasGroup cg = popupRoot.AddComponent<CanvasGroup>();
            float fadeElapsed = 0f;
            float fadeDuration = 0.35f;
            while (fadeElapsed < fadeDuration)
            {
                fadeElapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(1f, 0f, fadeElapsed / fadeDuration);
                yield return null;
            }

            Destroy(popupRoot);
            isShowing = false;

            onFinished?.Invoke();
        }

        private IEnumerator AnimatePopIn(RectTransform rect, float duration)
        {
            if (rect == null) yield break;
            float elapsed = 0f;
            rect.localScale = Vector3.one * 0.3f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float scale = 0.3f + 0.7f * Mathf.Sin(t * Mathf.PI * 0.5f);
                if (t > 0.7f)
                {
                    scale = Mathf.Lerp(1.12f, 1.0f, (t - 0.7f) / 0.3f);
                }
                rect.localScale = Vector3.one * scale;
                yield return null;
            }
            rect.localScale = Vector3.one;
        }
    }
}
