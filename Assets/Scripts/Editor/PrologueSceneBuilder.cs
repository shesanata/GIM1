using System;
using System.IO;
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
    /// Editor script otomatis untuk membangun scene Assets/Scenes/Prologue.unity:
    /// - Mengatur Main Camera dengan warna gelap sinematik.
    /// - Menata PrologueCanvas, BlackOverlay, StoryText, TitleGroup, SkipHint, FlashOverlay.
    /// - Menyusun elemen visual World: LetterGroup (Bubble Letters), MazeReveal (Tilemap maze Level 1),
    ///   EnemyReveal (Visual Enemy dengan EnemyVisualAnimator), PlayerReveal (Character.png), dan Particles.
    /// - Menghubungkan semua referensi ke PrologueController.
    /// - Mengonfigurasi Build Settings agar urutan tepat:
    ///   [0] Prologue.unity
    ///   [1] MainMenu.unity
    ///   [2] SampleScene.unity
    ///   [3] LevelSelect.unity
    /// - Mengunci EditorSceneManager.playModeStartScene ke Prologue.unity.
    /// </summary>
    [InitializeOnLoad]
    public static class PrologueSceneBuilder
    {
        public const string PrologueScenePath = "Assets/Scenes/Prologue.unity";
        public const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        public const string GameplayScenePath = "Assets/Scenes/SampleScene.unity";
        public const string LevelSelectScenePath = "Assets/Scenes/LevelSelect.unity";
        public const string LegacyLevelSelectScenePath = "Assets/Ui/Screens/LevelSelect.unity";

        private const string FontPath = "Assets/Fonts/TitanOne-Regular SDF.asset";
        private const string OutlineMatPath = "Assets/Fonts/BubbleLetter_Outline.mat";
        private const string DepthMatPath = "Assets/Fonts/BubbleLetter_Depth.mat";
        private const string FaceMatPath = "Assets/Fonts/BubbleLetter_Face.mat";

        private const string EnemyPrefabPath = "Assets/Prefabs/Enemy/Enemy_Character.prefab";
        private const string MazePrefabPath = "Assets/Prefabs/MazeGrids/Level1_MazeGrid.prefab";
        private const string PlayerSpritePath = "Assets/Sprites/Character.png";
        private const string EnemyBodyPath = "Assets/Sprites/Enemy_Body.png";
        private const string EnemySwordPath = "Assets/Sprites/Enemy_Sword.png";
        private const string SlashArcPath = "Assets/Sprites/Slash_Arc.png";

        private const string SfxReadyPath = "Assets/Audio/SFX/Enemy/enemy_ready.wav";
        private const string SfxSwingPath = "Assets/Audio/SFX/Enemy/enemy_sword_swing.wav";
        private const string SfxHitPath = "Assets/Audio/SFX/Enemy/enemy_sword_hit.wav";

        static PrologueSceneBuilder()
        {
            EditorApplication.delayCall += AutoBuildIfNeeded;
        }

        private static void AutoBuildIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            // Pastikan Build Settings selalu terdaftar dengan Prologue di index 0
            UpdateBuildSettings();

            if (!SessionState.GetBool("MazeSpell_PrologueSceneBuilt_V1", false) || !File.Exists(PrologueScenePath))
            {
                SessionState.SetBool("MazeSpell_PrologueSceneBuilt_V1", true);
                BuildPrologueScene(false);
            }
        }

        [MenuItem("MazeSpell/Build Prologue Scene")]
        public static void BuildPrologueSceneManual()
        {
            BuildPrologueScene(true);
        }

        public static void BuildPrologueScene(bool interactive)
        {
            try
            {
                Debug.Log("[PrologueSceneBuilder] Memulai pembuatan scene Prologue.unity...");

                // 1. Buat scene baru
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                // Load Assets pendukung
                TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
                if (fontAsset == null) fontAsset = TMP_Settings.defaultFontAsset;

                Material outlineMat = AssetDatabase.LoadAssetAtPath<Material>(OutlineMatPath);
                Material depthMat = AssetDatabase.LoadAssetAtPath<Material>(DepthMatPath);
                Material faceMat = AssetDatabase.LoadAssetAtPath<Material>(FaceMatPath);

                Sprite playerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlayerSpritePath);
                Sprite enemyBodySprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnemyBodyPath);
                Sprite enemySwordSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnemySwordPath);
                Sprite slashArcSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SlashArcPath);

                AudioClip readyClip = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxReadyPath);
                AudioClip swingClip = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxSwingPath);
                AudioClip hitClip = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxHitPath);

                // 2. Setup Main Camera (Pure Black Background)
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                Camera cam = camObj.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black; // PURE BLACK
                cam.orthographic = true;
                cam.orthographicSize = 5.5f;
                camObj.transform.position = new Vector3(0f, 0f, -10f);
                camObj.AddComponent<AudioListener>();

                // 3. Setup World Container (Clean Pure Black Background)
                GameObject worldContainer = new GameObject("WorldContainer");
                worldContainer.transform.position = Vector3.zero;

                // 4. Setup Canvas UI (Full Screen Space Overlay with Pure Black)
                GameObject canvasObj = new GameObject("PrologueCanvas");
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 20;

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                canvasObj.AddComponent<GraphicRaycaster>();

                // 4A. Black Overlay
                GameObject blackOvrObj = new GameObject("BlackOverlay");
                blackOvrObj.transform.SetParent(canvasObj.transform, false);
                RectTransform blackOvrRect = blackOvrObj.AddComponent<RectTransform>();
                blackOvrRect.anchorMin = Vector2.zero;
                blackOvrRect.anchorMax = Vector2.one;
                blackOvrRect.offsetMin = Vector2.zero;
                blackOvrRect.offsetMax = Vector2.zero;
                Image blackImg = blackOvrObj.AddComponent<Image>();
                blackImg.color = Color.black;
                blackImg.raycastTarget = false;
                CanvasGroup blackCg = blackOvrObj.AddComponent<CanvasGroup>();
                blackCg.alpha = 1f;

                // 4B. Flash Overlay
                GameObject flashObj = new GameObject("FlashOverlay");
                flashObj.transform.SetParent(canvasObj.transform, false);
                RectTransform flashRect = flashObj.AddComponent<RectTransform>();
                flashRect.anchorMin = Vector2.zero;
                flashRect.anchorMax = Vector2.one;
                flashRect.offsetMin = Vector2.zero;
                flashRect.offsetMax = Vector2.zero;
                Image flashImg = flashObj.AddComponent<Image>();
                flashImg.color = new Color(0.9f, 0.95f, 1f, 1f);
                flashImg.raycastTarget = false;
                CanvasGroup flashCg = flashObj.AddComponent<CanvasGroup>();
                flashCg.alpha = 0f;

                // 4C. Story Text (Non-Monospace Proportional Typography)
                GameObject storyObj = new GameObject("StoryText");
                storyObj.transform.SetParent(canvasObj.transform, false);
                RectTransform storyRect = storyObj.AddComponent<RectTransform>();
                storyRect.anchorMin = new Vector2(0.5f, 0.5f);
                storyRect.anchorMax = new Vector2(0.5f, 0.5f);
                storyRect.sizeDelta = new Vector2(1500f, 300f);
                storyRect.anchoredPosition = new Vector2(0f, 0f);

                CanvasGroup storyCg = storyObj.AddComponent<CanvasGroup>();
                storyCg.alpha = 0f;

                TextMeshProUGUI storyTmp = storyObj.AddComponent<TextMeshProUGUI>();
                if (fontAsset != null) storyTmp.font = fontAsset;
                storyTmp.text = "";
                storyTmp.fontSize = 46;
                storyTmp.fontStyle = FontStyles.Bold;
                storyTmp.alignment = TextAlignmentOptions.Center;
                storyTmp.color = new Color(0.99f, 0.94f, 0.82f, 1f); // Soft golden ivory
                storyTmp.characterSpacing = 0f; // 100% Proportional Non-Monospace
                storyTmp.wordSpacing = 0f;
                storyTmp.lineSpacing = 10f;
                storyTmp.raycastTarget = false;

                // 4D. Title Group: "LABIRIN KATA"
                GameObject titleObj = new GameObject("TitleGroup");
                titleObj.transform.SetParent(canvasObj.transform, false);
                RectTransform titleRect = titleObj.AddComponent<RectTransform>();
                titleRect.anchorMin = new Vector2(0.5f, 0.5f);
                titleRect.anchorMax = new Vector2(0.5f, 0.5f);
                titleRect.sizeDelta = new Vector2(1200f, 280f);
                titleRect.anchoredPosition = new Vector2(0f, 0f);

                CanvasGroup titleCg = titleObj.AddComponent<CanvasGroup>();
                titleCg.alpha = 0f;

                // Main Title: "LABIRIN KATA"
                GameObject mainTitleObj = new GameObject("Logo");
                mainTitleObj.transform.SetParent(titleObj.transform, false);
                RectTransform mainTitleRect = mainTitleObj.AddComponent<RectTransform>();
                mainTitleRect.anchorMin = new Vector2(0f, 0.35f);
                mainTitleRect.anchorMax = new Vector2(1f, 1f);
                mainTitleRect.offsetMin = Vector2.zero;
                mainTitleRect.offsetMax = Vector2.zero;

                TextMeshProUGUI mainTitleTmp = mainTitleObj.AddComponent<TextMeshProUGUI>();
                if (fontAsset != null) mainTitleTmp.font = fontAsset;
                mainTitleTmp.text = "<b>LABIRIN KATA</b>";
                mainTitleTmp.fontSize = 86;
                mainTitleTmp.fontStyle = FontStyles.Bold;
                mainTitleTmp.alignment = TextAlignmentOptions.Center;
                mainTitleTmp.color = new Color(0.99f, 0.85f, 0.35f, 1f); // Warm Gold
                mainTitleTmp.characterSpacing = 0f; // Non-Monospace
                mainTitleTmp.wordSpacing = 0f;
                mainTitleTmp.raycastTarget = false;

                // Subtitle: "MAZESPELL"
                GameObject subtitleObj = new GameObject("Subtitle");
                subtitleObj.transform.SetParent(titleObj.transform, false);
                RectTransform subtitleRect = subtitleObj.AddComponent<RectTransform>();
                subtitleRect.anchorMin = new Vector2(0f, 0f);
                subtitleRect.anchorMax = new Vector2(1f, 0.38f);
                subtitleRect.offsetMin = Vector2.zero;
                subtitleRect.offsetMax = Vector2.zero;

                TextMeshProUGUI subtitleTmp = subtitleObj.AddComponent<TextMeshProUGUI>();
                if (fontAsset != null) subtitleTmp.font = fontAsset;
                subtitleTmp.text = "MAZESPELL";
                subtitleTmp.fontSize = 28;
                subtitleTmp.fontStyle = FontStyles.Bold;
                subtitleTmp.alignment = TextAlignmentOptions.Center;
                subtitleTmp.color = new Color(0.58f, 0.77f, 0.99f, 1f); // Soft Adventure Blue
                subtitleTmp.characterSpacing = 2f;
                subtitleTmp.wordSpacing = 0f;
                subtitleTmp.raycastTarget = false;

                // 4E. Skip Hint Text
                GameObject skipObj = new GameObject("SkipHint");
                skipObj.transform.SetParent(canvasObj.transform, false);
                RectTransform skipRect = skipObj.AddComponent<RectTransform>();
                skipRect.anchorMin = new Vector2(0.5f, 0f);
                skipRect.anchorMax = new Vector2(0.5f, 0f);
                skipRect.sizeDelta = new Vector2(600f, 40f);
                skipRect.anchoredPosition = new Vector2(0f, 50f);

                TextMeshProUGUI skipTmp = skipObj.AddComponent<TextMeshProUGUI>();
                if (fontAsset != null) skipTmp.font = fontAsset;
                skipTmp.text = "TEKAN SPASI / ESC UNTUK LEWATI";
                skipTmp.fontSize = 18;
                skipTmp.fontStyle = FontStyles.Bold;
                skipTmp.alignment = TextAlignmentOptions.Center;
                skipTmp.color = new Color(0.85f, 0.90f, 0.98f, 0.65f);
                skipTmp.characterSpacing = 0f;
                skipTmp.wordSpacing = 0f;
                skipTmp.raycastTarget = false;

                // 5. Setup PrologueController
                GameObject controllerObj = new GameObject("PrologueController");
                AudioSource audioSrc = controllerObj.AddComponent<AudioSource>();
                audioSrc.playOnAwake = false;
                audioSrc.spatialBlend = 0f;

                var controller = controllerObj.AddComponent<PrologueController>();
                controller.ConfigureReferences(
                    canvas,
                    blackCg,
                    storyCg,
                    storyTmp,
                    titleCg,
                    titleRect,
                    skipTmp,
                    flashCg,
                    null, // letterGroupObj
                    null, // mazeRevealObj
                    null, // enemyRevealObj
                    null, // playerRevealObj
                    null, // playerLettersObj
                    null, // enemyAnimator
                    cam,
                    null, // ps
                    readyClip,
                    swingClip,
                    hitClip
                );

                // 6. Setup EventSystem
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                eventSystemObj.AddComponent<InputSystemUIInputModule>();
#else
                eventSystemObj.AddComponent<StandaloneInputModule>();
#endif

                // 7. Simpan scene
                EditorSceneManager.SaveScene(scene, PrologueScenePath);
                Debug.Log($"[PrologueSceneBuilder] Scene berhasil dibuat dan disimpan di '{PrologueScenePath}'!");

                // 8. Daftarkan di Build Settings pada index 0
                UpdateBuildSettings();

                // 9. Kunci playModeStartScene ke Prologue
                SceneAsset prologueAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(PrologueScenePath);
                if (prologueAsset != null)
                {
                    EditorSceneManager.playModeStartScene = prologueAsset;
                }

                if (interactive)
                {
                    EditorUtility.DisplayDialog("Prologue Setup", "Scene Prologue.unity berhasil dibuat dan ditata dengan urutan Build Settings [0] Prologue!", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PrologueSceneBuilder] Gagal membuat scene Prologue: {ex}");
            }
        }

        public static void UpdateBuildSettings()
        {
            EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(PrologueScenePath, true),
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(GameplayScenePath, true),
                new EditorBuildSettingsScene(LevelSelectScenePath, true)
            };

            EditorBuildSettings.scenes = scenes;
            Debug.Log("[PrologueSceneBuilder] Build Settings terdaftar: [0] Prologue, [1] MainMenu, [2] SampleScene, [3] LevelSelect");
        }

        private static GameObject CreateBubbleLetterObject(
            string objName,
            char letterChar,
            Vector3 localPos,
            Transform parent,
            TMP_FontAsset fontAsset,
            Material outlineMat,
            Material depthMat,
            Material faceMat)
        {
            GameObject letterObj = new GameObject(objName);
            letterObj.transform.SetParent(parent, false);
            letterObj.transform.localPosition = localPos;
            letterObj.transform.localScale = Vector3.one;

            GameObject visualObj = new GameObject("Visual");
            visualObj.transform.SetParent(letterObj.transform, false);
            visualObj.transform.localPosition = Vector3.zero;

            // Layer 1: Outline (Dark Blue - Z = 0)
            GameObject outlineObj = new GameObject("Outline");
            outlineObj.transform.SetParent(visualObj.transform, false);
            outlineObj.transform.localPosition = new Vector3(0.02f, -0.025f, 0f);
            TextMeshPro tmpOutline = outlineObj.AddComponent<TextMeshPro>();
            tmpOutline.font = fontAsset;
            if (outlineMat != null) tmpOutline.fontSharedMaterial = outlineMat;
            tmpOutline.text = $"<b>{letterChar}</b>";
            tmpOutline.fontSize = 8.5f;
            tmpOutline.alignment = TextAlignmentOptions.Center;
            tmpOutline.sortingOrder = 15;
            tmpOutline.color = Color.white;

            // Layer 2: Depth (Orange Inner Depth - Z = -0.02)
            GameObject depthObj = new GameObject("Depth");
            depthObj.transform.SetParent(visualObj.transform, false);
            depthObj.transform.localPosition = new Vector3(0.04f, -0.05f, -0.02f);
            TextMeshPro tmpDepth = depthObj.AddComponent<TextMeshPro>();
            tmpDepth.font = fontAsset;
            if (depthMat != null) tmpDepth.fontSharedMaterial = depthMat;
            tmpDepth.text = $"<b>{letterChar}</b>";
            tmpDepth.fontSize = 8.5f;
            tmpDepth.alignment = TextAlignmentOptions.Center;
            tmpDepth.sortingOrder = 16;
            tmpDepth.color = Color.white;

            // Layer 3: Face (Golden Yellow - Z = -0.04)
            GameObject faceObj = new GameObject("Face");
            faceObj.transform.SetParent(visualObj.transform, false);
            faceObj.transform.localPosition = new Vector3(0f, 0f, -0.04f);
            TextMeshPro tmpFace = faceObj.AddComponent<TextMeshPro>();
            tmpFace.font = fontAsset;
            if (faceMat != null) tmpFace.fontSharedMaterial = faceMat;
            tmpFace.text = $"<b>{letterChar}</b>";
            tmpFace.fontSize = 8.5f;
            tmpFace.alignment = TextAlignmentOptions.Center;
            tmpFace.sortingOrder = 17;
            tmpFace.color = Color.white;

            return letterObj;
        }

        private static ParticleSystem SetupMagicParticleSystem(GameObject obj)
        {
            ParticleSystem ps = obj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = 3.5f;
            main.startSpeed = 0.5f;
            main.startSize = 0.12f;
            main.startColor = new Color(0.99f, 0.88f, 0.45f, 0.75f); // Golden sparkle
            main.maxParticles = 50;

            var emission = ps.emission;
            emission.rateOverTime = 12f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(10f, 6f, 1f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(0.99f, 0.88f, 0.45f), 0f), new GradientColorKey(new Color(0.55f, 0.85f, 1f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.8f, 0.3f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;

            var renderer = obj.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = 18;

            return ps;
        }
    }
}
