using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MazeSpell.Gameplay
{
    /// <summary>
    /// HUD khusus untuk Boss Battle Level 5 (Final Guardian):
    /// - Menampilkan nama boss: "🔥 FINAL GUARDIAN 🔥"
    /// - Indikator HP 3 hati (♥ ♥ ♥) yang tersinkronisasi presisi dengan EnemyCharacter.
    /// - Animasi scale punch dan flash saat menerima damage.
    /// - Notifikasi fase (Phase 1 -> Phase 2 -> Final Phase).
    /// </summary>
    public class FinalGuardianHUD : MonoBehaviour
    {
        private static FinalGuardianHUD _instance;
        public static FinalGuardianHUD Instance
        {
            get
            {
                if (_instance == null)
                {
#if UNITY_2023_1_OR_NEWER
                    _instance = FindAnyObjectByType<FinalGuardianHUD>();
#else
                    _instance = FindObjectOfType<FinalGuardianHUD>();
#endif
                }
                return _instance;
            }
            private set => _instance = value;
        }

        private GameObject hudContainer;
        private CanvasGroup canvasGroup;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI phaseText;
        private List<TextMeshProUGUI> heartTexts = new List<TextMeshProUGUI>();
        private List<RectTransform> heartRects = new List<RectTransform>();
        private RectTransform panelRect;

        private int lastHp = 3;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }
            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        public bool IsShowing => hudContainer != null && hudContainer.activeInHierarchy;

        public void EnsureHUD()
        {
            if (hudContainer != null) return;

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
#if UNITY_2023_1_OR_NEWER
                canvas = FindAnyObjectByType<Canvas>();
#else
                canvas = FindObjectOfType<Canvas>();
#endif
            }

            if (canvas == null)
            {
                GameObject canGo = new GameObject("BossHUDCanvas");
                canvas = canGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 35;
                CanvasScaler cs = canGo.AddComponent<CanvasScaler>();
                cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                cs.referenceResolution = new Vector2(1920, 1080);
                canGo.AddComponent<GraphicRaycaster>();
            }

            hudContainer = new GameObject("FinalGuardianHUD");
            hudContainer.transform.SetParent(canvas.transform, false);

            canvasGroup = hudContainer.AddComponent<CanvasGroup>();
            panelRect = hudContainer.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 1f);
            panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = new Vector2(0f, -25f);
            panelRect.sizeDelta = new Vector2(460f, 100f);

            // Background panel
            Image bg = hudContainer.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.05f, 0.07f, 0.92f);

            // Fiery outline
            GameObject border = new GameObject("FieryBorder");
            border.transform.SetParent(hudContainer.transform, false);
            RectTransform borderRect = border.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = new Vector2(-2f, -2f);
            borderRect.offsetMax = new Vector2(2f, 2f);
            Image borderImg = border.AddComponent<Image>();
            borderImg.color = new Color(1.0f, 0.35f, 0.1f, 0.75f);
            border.transform.SetAsFirstSibling();

            // Title
            GameObject titleGo = new GameObject("BossTitle");
            titleGo.transform.SetParent(hudContainer.transform, false);
            RectTransform titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.55f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            titleText = titleGo.AddComponent<TextMeshProUGUI>();
            titleText.text = "🔥 FINAL GUARDIAN 🔥";
            titleText.fontSize = 26;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(1.0f, 0.85f, 0.35f, 1f);

            // Phase status text
            GameObject phaseGo = new GameObject("BossPhase");
            phaseGo.transform.SetParent(hudContainer.transform, false);
            RectTransform phaseRect = phaseGo.AddComponent<RectTransform>();
            phaseRect.anchorMin = new Vector2(0f, 0.05f);
            phaseRect.anchorMax = new Vector2(1f, 0.45f);
            phaseRect.offsetMin = Vector2.zero;
            phaseRect.offsetMax = Vector2.zero;

            phaseText = phaseGo.AddComponent<TextMeshProUGUI>();
            phaseText.text = "PHASE 1";
            phaseText.fontSize = 17;
            phaseText.fontStyle = FontStyles.Bold;
            phaseText.alignment = TextAlignmentOptions.Center;
            phaseText.color = new Color(1f, 0.55f, 0.2f, 1f);

            // 3 Hearts row
            GameObject heartsRow = new GameObject("HeartsRow");
            heartsRow.transform.SetParent(hudContainer.transform, false);
            RectTransform rowRect = heartsRow.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.5f, 0.55f);
            rowRect.anchorMax = new Vector2(0.5f, 0.55f);
            rowRect.sizeDelta = new Vector2(160f, 36f);
            rowRect.anchoredPosition = new Vector2(0f, -12f);

            heartTexts.Clear();
            heartRects.Clear();

            float heartSpacing = 48f;
            float startX = -heartSpacing;

            for (int i = 0; i < 3; i++)
            {
                GameObject hObj = new GameObject($"Heart_{i + 1}");
                hObj.transform.SetParent(heartsRow.transform, false);
                RectTransform hRect = hObj.AddComponent<RectTransform>();
                hRect.anchoredPosition = new Vector2(startX + i * heartSpacing, 0f);
                hRect.sizeDelta = new Vector2(36f, 36f);

                TextMeshProUGUI hTmp = hObj.AddComponent<TextMeshProUGUI>();
                hTmp.text = "♥";
                hTmp.fontSize = 32;
                hTmp.alignment = TextAlignmentOptions.Center;
                hTmp.color = new Color(1.0f, 0.2f, 0.25f, 1.0f);

                heartTexts.Add(hTmp);
                heartRects.Add(hRect);
            }

            hudContainer.SetActive(false);
        }

        public void ShowHUD(int initialHealth = 3)
        {
            EnsureHUD();
            if (hudContainer != null)
            {
                hudContainer.SetActive(true);
                canvasGroup.alpha = 1f;
                lastHp = initialHealth;
                UpdateHealth(initialHealth, 3);
                SetPhaseText("PHASE 1", new Color(1f, 0.7f, 0.2f, 1f));
                StartCoroutine(PopInRoutine());
            }
        }

        public void HideHUD()
        {
            if (hudContainer != null)
            {
                hudContainer.SetActive(false);
            }
        }

        public void UpdateHealth(int currentHealth, int maxHealth = 3)
        {
            EnsureHUD();

            currentHealth = Mathf.Clamp(currentHealth, 0, 3);
            for (int i = 0; i < heartTexts.Count; i++)
            {
                if (i < currentHealth)
                {
                    heartTexts[i].text = "♥";
                    heartTexts[i].color = new Color(1.0f, 0.2f, 0.25f, 1.0f);
                }
                else
                {
                    heartTexts[i].text = "♡";
                    heartTexts[i].color = new Color(0.4f, 0.4f, 0.45f, 0.6f);
                }
            }

            if (currentHealth < lastHp)
            {
                int lostIndex = Mathf.Clamp(lastHp - 1, 0, heartRects.Count - 1);
                StartCoroutine(AnimateHeartLost(heartRects[lostIndex]));
                StartCoroutine(PunchPanel());
            }

            lastHp = currentHealth;
        }

        public void SetPhaseText(string text, Color color)
        {
            EnsureHUD();
            if (phaseText != null)
            {
                phaseText.text = text;
                phaseText.color = color;
                StartCoroutine(AnimatePhaseFlash(color));
            }
        }

        private IEnumerator PopInRoutine()
        {
            if (panelRect == null) yield break;
            panelRect.localScale = Vector3.one * 0.5f;
            float elapsed = 0f;
            float duration = 0.35f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float scale = Mathf.Lerp(0.5f, 1.0f, Mathf.Sin(t * Mathf.PI * 0.5f));
                panelRect.localScale = Vector3.one * scale;
                yield return null;
            }
            panelRect.localScale = Vector3.one;
        }

        private IEnumerator PunchPanel()
        {
            if (panelRect == null) yield break;
            panelRect.localScale = Vector3.one * 1.15f;
            yield return new WaitForSeconds(0.12f);
            panelRect.localScale = Vector3.one;
        }

        private IEnumerator AnimateHeartLost(RectTransform heart)
        {
            if (heart == null) yield break;
            float elapsed = 0f;
            float duration = 0.3f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                heart.localScale = Vector3.Lerp(Vector3.one * 1.6f, Vector3.one, t);
                yield return null;
            }
            heart.localScale = Vector3.one;
        }

        private IEnumerator AnimatePhaseFlash(Color color)
        {
            if (phaseText == null) yield break;
            for (int i = 0; i < 3; i++)
            {
                phaseText.color = Color.white;
                yield return new WaitForSeconds(0.08f);
                phaseText.color = color;
                yield return new WaitForSeconds(0.08f);
            }
        }
    }
}
