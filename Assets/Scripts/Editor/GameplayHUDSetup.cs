using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using MazeSpell.UI;

namespace MazeSpell.Editor
{
    /// <summary>
    /// Editor script untuk menata Canvas GameplayHUD di scene SampleScene (gameplay):
    /// - TOP LEFT: Tombol Pause (||), Fast-Forward (>>), Action/Bag (⚿) berbentuk lingkaran emas/kuning
    /// - TOP RIGHT: 3 Icon Hati Merah (Hearts) + Tombol Menu Biru
    /// - BOTTOM LEFT: Virtual Joystick untuk kontrol arah karakter
    /// - HAPUS SEMUA panel target word / level lama (LevelPanel, TargetPanel, WordPanel, LivesPanel)
    /// </summary>
    [InitializeOnLoad]
    public static class GameplayHUDSetup
    {
        private const string GameplayScenePath = "Assets/Scenes/SampleScene.unity";

        static GameplayHUDSetup()
        {
            EditorApplication.delayCall += AutoSetupIfNeeded;
        }

        private static void AutoSetupIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            if (!SessionState.GetBool("MazeSpell_GameplayHUDSetupDone_V3", false))
            {
                SessionState.SetBool("MazeSpell_GameplayHUDSetupDone_V3", true);
                SetupHUDInScene(false);
            }
        }

        [MenuItem("MazeSpell/Setup Gameplay HUD in Scene")]
        public static void SetupHUDManual()
        {
            SetupHUDInScene(true);
        }

        public static void SetupHUDInScene(bool interactive = false)
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
                if (!scene.IsValid())
                {
                    Debug.LogWarning($"[GameplayHUDSetup] Scene gameplay tidak ditemukan di '{GameplayScenePath}'");
                    return;
                }

                Debug.Log("[GameplayHUDSetup] Menyusun Canvas Gameplay HUD Bergaya Robbery Top-Down...");

                // 1. Bersihkan semua Canvas lama / duplicate
                var allCanvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                GameObject mainCanvasObj = null;

                foreach (var c in allCanvases)
                {
                    if (c.name == "GameplayCanvas" && mainCanvasObj == null)
                    {
                        mainCanvasObj = c.gameObject;
                    }
                    else if (c.name == "Canvas" || c.name == "GameplayCanvas" || c.name.Contains("HUD"))
                    {
                        UnityEngine.Object.DestroyImmediate(c.gameObject);
                    }
                }

                if (mainCanvasObj == null)
                {
                    mainCanvasObj = new GameObject("GameplayCanvas");
                }

                mainCanvasObj.transform.SetParent(null, false);

                Canvas canvas = mainCanvasObj.GetComponent<Canvas>();
                if (canvas == null) canvas = mainCanvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 25;

                CanvasScaler scaler = mainCanvasObj.GetComponent<CanvasScaler>();
                if (scaler == null) scaler = mainCanvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                GraphicRaycaster raycaster = mainCanvasObj.GetComponent<GraphicRaycaster>();
                if (raycaster == null) raycaster = mainCanvasObj.AddComponent<GraphicRaycaster>();

                // Bersihkan seluruh child lama yang tidak sesuai
                for (int i = mainCanvasObj.transform.childCount - 1; i >= 0; i--)
                {
                    UnityEngine.Object.DestroyImmediate(mainCanvasObj.transform.GetChild(i).gameObject);
                }

                var hudController = mainCanvasObj.GetComponent<GameplayHUD>();
                if (hudController == null) hudController = mainCanvasObj.AddComponent<GameplayHUD>();

                TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;

                // =========================================================
                // 1. TOP-LEFT: ACTION / PAUSE BUTTONS (Yellow/Gold circles)
                // =========================================================
                GameObject topLeftGroup = new GameObject("TopLeftControls");
                topLeftGroup.transform.SetParent(mainCanvasObj.transform, false);
                RectTransform tlRect = topLeftGroup.AddComponent<RectTransform>();
                tlRect.anchorMin = new Vector2(0f, 1f);
                tlRect.anchorMax = new Vector2(0f, 1f);
                tlRect.pivot = new Vector2(0f, 1f);
                tlRect.anchoredPosition = new Vector2(25f, -25f);
                tlRect.sizeDelta = new Vector2(240f, 70f);

                // Pause Button
                Button btnPause = CreateCircleButton(topLeftGroup.transform, "Btn_Pause", new Vector2(0f, 0f), 64f,
                    new Color(0.95f, 0.75f, 0.2f, 1f), new Color(0.6f, 0.35f, 0.05f, 1f), "<b>||</b>", defaultFont, 26);

                // Fast Forward Button
                Button btnFF = CreateCircleButton(topLeftGroup.transform, "Btn_FastForward", new Vector2(75f, 0f), 64f,
                    new Color(0.95f, 0.75f, 0.2f, 1f), new Color(0.6f, 0.35f, 0.05f, 1f), "<b>>></b>", defaultFont, 24);

                // Bag Button
                Button btnBag = CreateCircleButton(topLeftGroup.transform, "Btn_Bag", new Vector2(150f, 0f), 64f,
                    new Color(0.95f, 0.75f, 0.2f, 1f), new Color(0.6f, 0.35f, 0.05f, 1f), "<b>⚿</b>", defaultFont, 28);

                // =========================================================
                // 2. TOP-RIGHT: 3 HEARTS & MENU BUTTON
                // =========================================================
                GameObject topRightGroup = new GameObject("TopRightControls");
                topRightGroup.transform.SetParent(mainCanvasObj.transform, false);
                RectTransform trRect = topRightGroup.AddComponent<RectTransform>();
                trRect.anchorMin = new Vector2(1f, 1f);
                trRect.anchorMax = new Vector2(1f, 1f);
                trRect.pivot = new Vector2(1f, 1f);
                trRect.anchoredPosition = new Vector2(-25f, -25f);
                trRect.sizeDelta = new Vector2(240f, 70f);

                // Hearts Container
                GameObject heartsGo = new GameObject("HeartsContainer");
                heartsGo.transform.SetParent(topRightGroup.transform, false);
                RectTransform hRect = heartsGo.AddComponent<RectTransform>();
                hRect.anchorMin = new Vector2(0f, 0.5f);
                hRect.anchorMax = new Vector2(0f, 0.5f);
                hRect.pivot = new Vector2(0f, 0.5f);
                hRect.anchoredPosition = new Vector2(0f, 0f);
                hRect.sizeDelta = new Vector2(150f, 50f);

                Image[] hearts = new Image[3];
                for (int i = 0; i < 3; i++)
                {
                    GameObject singleHeart = new GameObject($"Heart_{i + 1}");
                    singleHeart.transform.SetParent(heartsGo.transform, false);
                    RectTransform shRect = singleHeart.AddComponent<RectTransform>();
                    shRect.anchorMin = new Vector2(0f, 0.5f);
                    shRect.anchorMax = new Vector2(0f, 0.5f);
                    shRect.pivot = new Vector2(0f, 0.5f);
                    shRect.anchoredPosition = new Vector2(i * 48f, 0f);
                    shRect.sizeDelta = new Vector2(42f, 42f);

                    Image hImg = singleHeart.AddComponent<Image>();
                    hImg.color = new Color(0.95f, 0.2f, 0.2f, 1f);
                    hImg.raycastTarget = false;

                    GameObject hTxtGo = new GameObject("HeartIcon");
                    hTxtGo.transform.SetParent(singleHeart.transform, false);
                    RectTransform htRect = hTxtGo.AddComponent<RectTransform>();
                    htRect.anchorMin = Vector2.zero;
                    htRect.anchorMax = Vector2.one;
                    htRect.offsetMin = Vector2.zero;
                    htRect.offsetMax = Vector2.zero;
                    TextMeshProUGUI hTmp = hTxtGo.AddComponent<TextMeshProUGUI>();
                    if (defaultFont != null) hTmp.font = defaultFont;
                    hTmp.text = "♥";
                    hTmp.fontSize = 34;
                    hTmp.alignment = TextAlignmentOptions.Center;
                    hTmp.color = Color.white;
                    hTmp.raycastTarget = false;

                    hearts[i] = hImg;
                }

                // Top-right Blue Menu Button
                Button btnMenu = CreateCircleButton(topRightGroup.transform, "Btn_Menu", new Vector2(170f, 0f), 64f,
                    new Color(0.2f, 0.45f, 0.85f, 1f), new Color(0.08f, 0.2f, 0.5f, 1f), "<b>||</b>", defaultFont, 26);

                // =========================================================
                // 3. BOTTOM-LEFT: VIRTUAL JOYSTICK
                // =========================================================
                GameObject joystickGo = new GameObject("VirtualJoystick");
                joystickGo.transform.SetParent(mainCanvasObj.transform, false);
                RectTransform joyRect = joystickGo.AddComponent<RectTransform>();
                joyRect.anchorMin = new Vector2(0f, 0f);
                joyRect.anchorMax = new Vector2(0f, 0f);
                joyRect.pivot = new Vector2(0f, 0f);
                joyRect.anchoredPosition = new Vector2(60f, 60f);
                joyRect.sizeDelta = new Vector2(180f, 180f);

                Image joyBg = joystickGo.AddComponent<Image>();
                joyBg.color = new Color(1f, 1f, 1f, 0.25f);

                GameObject arrowsGo = new GameObject("Arrows");
                arrowsGo.transform.SetParent(joystickGo.transform, false);
                RectTransform arrRect = arrowsGo.AddComponent<RectTransform>();
                arrRect.anchorMin = Vector2.zero;
                arrRect.anchorMax = Vector2.one;
                arrRect.offsetMin = Vector2.zero;
                arrRect.offsetMax = Vector2.zero;
                TextMeshProUGUI arrTmp = arrowsGo.AddComponent<TextMeshProUGUI>();
                if (defaultFont != null) arrTmp.font = defaultFont;
                arrTmp.text = "▲\n◄   ►\n▼";
                arrTmp.fontSize = 20;
                arrTmp.alignment = TextAlignmentOptions.Center;
                arrTmp.color = new Color(1f, 1f, 1f, 0.55f);
                arrTmp.raycastTarget = false;

                GameObject handleGo = new GameObject("Handle");
                handleGo.transform.SetParent(joystickGo.transform, false);
                RectTransform handleRect = handleGo.AddComponent<RectTransform>();
                handleRect.anchorMin = new Vector2(0.5f, 0.5f);
                handleRect.anchorMax = new Vector2(0.5f, 0.5f);
                handleRect.pivot = new Vector2(0.5f, 0.5f);
                handleRect.anchoredPosition = Vector2.zero;
                handleRect.sizeDelta = new Vector2(80f, 80f);

                Image handleImg = handleGo.AddComponent<Image>();
                handleImg.color = new Color(1f, 1f, 1f, 0.65f);

                var vJoy = joystickGo.AddComponent<VirtualJoystick>();

                // Pastikan EventSystem
                if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
                {
                    GameObject es = new GameObject("EventSystem");
                    es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                    es.AddComponent<InputSystemUIInputModule>();
#else
                    es.AddComponent<StandaloneInputModule>();
#endif
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[GameplayHUDSetup] ✓ Scene '{GameplayScenePath}' berhasil disimpan dengan HUD Robbery Top-Down!");

                if (interactive)
                {
                    EditorUtility.DisplayDialog("Gameplay HUD Setup", "HUD Robbery Top-Down berhasil disiapkan di SampleScene!", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameplayHUDSetup] Gagal setup HUD: {ex}");
            }
        }

        private static Button CreateCircleButton(Transform parent, string name, Vector2 localPos, float size,
            Color bgColor, Color borderColor, string labelText, TMP_FontAsset font, float fontSize)
        {
            GameObject btnGo = new GameObject(name);
            btnGo.transform.SetParent(parent, false);

            RectTransform rect = btnGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = localPos;
            rect.sizeDelta = new Vector2(size, size);

            Image img = btnGo.AddComponent<Image>();
            img.color = bgColor;

            Button btn = btnGo.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = Color.Lerp(bgColor, Color.white, 0.25f);
            cb.pressedColor = Color.Lerp(bgColor, Color.black, 0.25f);
            btn.colors = cb;

            GameObject textGo = new GameObject("Label");
            textGo.transform.SetParent(btnGo.transform, false);
            RectTransform tRect = textGo.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = labelText;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = borderColor;
            tmp.raycastTarget = false;

            return btn;
        }
    }
}
