using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using MazeSpell.UI;

namespace MazeSpell.Editor
{
    public static class MainMenuLayoutAdjuster
    {
        // Auto-run dinonaktifkan agar tidak mengubah logo atau desain tombol yang sudah ada
        [MenuItem("MazeSpell/Revamp MainMenu To Reference")]
        public static void ApplyMainMenuRevamp()
        {
            string logPath = "c:/Users/ASUS/Mazell/mainmenu_update_log.txt";
            var logMessages = new System.Text.StringBuilder();
            void Log(string msg)
            {
                Debug.Log(msg);
                logMessages.AppendLine(msg);
            }

            Log(">>> [MainMenuLayoutAdjuster] Starting Main Menu revamp to match user reference visual...");

            try
            {
                // 1. Konfigurasi Texture Importer untuk Sprite Tombol dan Background
                ConfigureSpriteImporter("Assets/Ui/Button/btn_cartoon_play.png", Log);
                ConfigureSpriteImporter("Assets/Ui/Button/btn_cartoon_level.png", Log);
                ConfigureSpriteImporter("Assets/Ui/Background/menuBG.jpeg", Log);

                AssetDatabase.Refresh();

                Sprite playSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Ui/Button/btn_cartoon_play.png");
                Sprite levelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Ui/Button/btn_cartoon_level.png");
                Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Ui/Background/menuBG.jpeg");

                if (playSprite == null) Log("[WARN] playSprite is null after import!");
                if (levelSprite == null) Log("[WARN] levelSprite is null after import!");
                if (bgSprite == null) Log("[WARN] bgSprite is null after import!");

                // 2. Buka scene MainMenu.unity
                string scenePath = "Assets/Scenes/MainMenu.unity";
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                if (!scene.IsValid())
                {
                    Log($"[ERROR] Failed to open scene {scenePath}!");
                    File.WriteAllText(logPath, logMessages.ToString());
                    return;
                }

                GameObject canvasObj = GameObject.Find("Canvas");
                if (canvasObj == null)
                {
                    Log("[ERROR] Canvas GameObject not found in MainMenu.unity!");
                    File.WriteAllText(logPath, logMessages.ToString());
                    return;
                }

                var menuController = canvasObj.GetComponent<MainMenuController>();
                if (menuController == null)
                {
                    menuController = canvasObj.AddComponent<MainMenuController>();
                }

                // 3. Hapus semua tombol dan UI lama selain PLAY dan LEVEL
                // Aturan: HANYA ADA 2 BUTTON (PLAY dan LEVEL). Hapus ExitButton, AudioSettingsButton, Logo, VignetteOverlay.
                string[] objectsToDelete = new string[]
                {
                    "AudioSettingsButton",
                    "ExitButton",
                    "Logo",
                    "SettingsButton",
                    "BackButton",
                    "OptionsButton"
                };

                foreach (string objName in objectsToDelete)
                {
                    Transform t = canvasObj.transform.Find(objName);
                    if (t != null)
                    {
                        Log($"[DELETE] Removing old/redundant UI: {objName}");
                        UnityEngine.Object.DestroyImmediate(t.gameObject);
                    }
                }

                // Hapus VignetteOverlay di dalam Background
                Transform bgTransform = canvasObj.transform.Find("Background");
                if (bgTransform != null)
                {
                    Transform vig = bgTransform.Find("VignetteOverlay");
                    if (vig != null)
                    {
                        Log("[DELETE] Removing VignetteOverlay to keep background clear and bright.");
                        UnityEngine.Object.DestroyImmediate(vig.gameObject);
                    }

                    Image bgImg = bgTransform.GetComponent<Image>();
                    if (bgImg != null)
                    {
                        if (bgSprite != null) bgImg.sprite = bgSprite;
                        bgImg.color = Color.white;
                        bgImg.raycastTarget = false;
                    }

                    RectTransform bgRect = bgTransform.GetComponent<RectTransform>();
                    if (bgRect != null)
                    {
                        bgRect.anchorMin = Vector2.zero;
                        bgRect.anchorMax = Vector2.one;
                        bgRect.offsetMin = Vector2.zero;
                        bgRect.offsetMax = Vector2.zero;
                    }
                }

                // 4. Konfigurasi Tombol PLAY (Cartoon Green Button)
                // Posisi di lantai lorong batu tengah: Y = -50, Size: 450 x 142 (Aspect 3.18)
                Transform playT = canvasObj.transform.Find("PlayButton");
                if (playT == null)
                {
                    GameObject newPlay = new GameObject("PlayButton");
                    newPlay.transform.SetParent(canvasObj.transform, false);
                    playT = newPlay.transform;
                }

                playT.gameObject.SetActive(true);
                RectTransform playRect = playT.GetComponent<RectTransform>();
                if (playRect == null) playRect = playT.gameObject.AddComponent<RectTransform>();
                playRect.anchorMin = new Vector2(0.5f, 0.5f);
                playRect.anchorMax = new Vector2(0.5f, 0.5f);
                playRect.pivot = new Vector2(0.5f, 0.5f);
                playRect.anchoredPosition = new Vector2(0f, -50f);
                playRect.sizeDelta = new Vector2(450f, 142f);

                // Hapus child Text agar grafis kartun putih 3D asli tidak tertumpuk teks standar
                for (int i = playT.childCount - 1; i >= 0; i--)
                {
                    UnityEngine.Object.DestroyImmediate(playT.GetChild(i).gameObject);
                }

                Image playImg = playT.GetComponent<Image>();
                if (playImg == null) playImg = playT.gameObject.AddComponent<Image>();
                if (playSprite != null) playImg.sprite = playSprite;
                playImg.color = Color.white;
                playImg.type = Image.Type.Simple;
                playImg.preserveAspect = true;
                playImg.raycastTarget = true;

                Button playBtn = playT.GetComponent<Button>();
                if (playBtn == null) playBtn = playT.gameObject.AddComponent<Button>();
                playBtn.targetGraphic = playImg;
                playBtn.transition = Selectable.Transition.ColorTint;
                ColorBlock playColors = playBtn.colors;
                playColors.normalColor = Color.white;
                playColors.highlightedColor = new Color(0.92f, 1f, 0.85f, 1f);
                playColors.pressedColor = new Color(0.78f, 0.85f, 0.70f, 1f);
                playColors.selectedColor = new Color(0.92f, 1f, 0.85f, 1f);
                playColors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                playColors.fadeDuration = 0.08f;
                playBtn.colors = playColors;

                // Hubungkan event onClick ke MainMenuController.PlayGame
                while (playBtn.onClick.GetPersistentEventCount() > 0)
                {
                    UnityEventTools.RemovePersistentListener(playBtn.onClick, 0);
                }
                UnityEventTools.AddPersistentListener(playBtn.onClick, menuController.PlayGame);

                Log("[CONFIG] PlayButton configured with cartoon green sprite at (0, -50), size (450, 142).");

                // 5. Konfigurasi Tombol LEVEL (Cartoon Green Button)
                // Posisi tepat di bawah PLAY: Y = -220, Size: 490 x 137 (Aspect 3.57)
                Transform levelT = canvasObj.transform.Find("LevelButton");
                if (levelT == null)
                {
                    GameObject newLevel = new GameObject("LevelButton");
                    newLevel.transform.SetParent(canvasObj.transform, false);
                    levelT = newLevel.transform;
                }

                levelT.gameObject.SetActive(true);
                RectTransform levelRect = levelT.GetComponent<RectTransform>();
                if (levelRect == null) levelRect = levelT.gameObject.AddComponent<RectTransform>();
                levelRect.anchorMin = new Vector2(0.5f, 0.5f);
                levelRect.anchorMax = new Vector2(0.5f, 0.5f);
                levelRect.pivot = new Vector2(0.5f, 0.5f);
                levelRect.anchoredPosition = new Vector2(0f, -220f);
                levelRect.sizeDelta = new Vector2(490f, 137f);

                // Hapus child Text
                for (int i = levelT.childCount - 1; i >= 0; i--)
                {
                    UnityEngine.Object.DestroyImmediate(levelT.GetChild(i).gameObject);
                }

                Image levelImg = levelT.GetComponent<Image>();
                if (levelImg == null) levelImg = levelT.gameObject.AddComponent<Image>();
                if (levelSprite != null) levelImg.sprite = levelSprite;
                levelImg.color = Color.white;
                levelImg.type = Image.Type.Simple;
                levelImg.preserveAspect = true;
                levelImg.raycastTarget = true;

                Button levelBtn = levelT.GetComponent<Button>();
                if (levelBtn == null) levelBtn = levelT.gameObject.AddComponent<Button>();
                levelBtn.targetGraphic = levelImg;
                levelBtn.transition = Selectable.Transition.ColorTint;
                ColorBlock levelColors = levelBtn.colors;
                levelColors.normalColor = Color.white;
                levelColors.highlightedColor = new Color(0.92f, 1f, 0.85f, 1f);
                levelColors.pressedColor = new Color(0.78f, 0.85f, 0.70f, 1f);
                levelColors.selectedColor = new Color(0.92f, 1f, 0.85f, 1f);
                levelColors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                levelColors.fadeDuration = 0.08f;
                levelBtn.colors = levelColors;

                // Hubungkan event onClick ke MainMenuController.OpenLevelSelect
                while (levelBtn.onClick.GetPersistentEventCount() > 0)
                {
                    UnityEventTools.RemovePersistentListener(levelBtn.onClick, 0);
                }
                UnityEventTools.AddPersistentListener(levelBtn.onClick, menuController.OpenLevelSelect);

                Log("[CONFIG] LevelButton configured with cartoon green sprite at (0, -220), size (490, 137).");

                // 6. Setup Navigasi Eksklusif 2 Tombol: PLAY <-> LEVEL
                Navigation navPlay = new Navigation { mode = Navigation.Mode.Explicit, selectOnDown = levelBtn, selectOnUp = levelBtn };
                Navigation navLevel = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = playBtn, selectOnDown = playBtn };
                playBtn.navigation = navPlay;
                levelBtn.navigation = navLevel;

                // 7. Simpan Scene
                EditorSceneManager.MarkSceneDirty(scene);
                bool saved = EditorSceneManager.SaveScene(scene);
                Log($"[SUCCESS] MainMenu.unity successfully revamped and saved! (Saved = {saved})");

                // 8. Capture screenshot verifikasi
                CaptureSceneScreenshot("c:/Users/ASUS/Mazell/mainmenu_revamped.png");
                Log("[CAPTURE] MainMenu screenshot rendered to c:/Users/ASUS/Mazell/mainmenu_revamped.png");
            }
            catch (Exception ex)
            {
                Log($"[EXCEPTION] Error revamping Main Menu: {ex}");
            }

            File.WriteAllText(logPath, logMessages.ToString());
        }

        private static void ConfigureSpriteImporter(string path, Action<string> log)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                bool changed = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    changed = true;
                }
                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    changed = true;
                }
                if (!importer.alphaIsTransparency)
                {
                    importer.alphaIsTransparency = true;
                    changed = true;
                }
                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    changed = true;
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                    log($"[IMPORTER] Reimported {path} as Single Sprite with AlphaTransparency.");
                }
            }
        }

        private static void CaptureSceneScreenshot(string targetPath)
        {
            try
            {
                int width = 1920;
                int height = 1080;
                Camera cam = Camera.main;
                if (cam == null) cam = UnityEngine.Object.FindAnyObjectByType<Camera>();
                if (cam == null) return;

                RenderTexture rt = new RenderTexture(width, height, 24);
                RenderTexture previous = RenderTexture.active;
                cam.targetTexture = rt;
                RenderTexture.active = rt;

                cam.Render();

                Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGB24, false);
                screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                screenShot.Apply();

                cam.targetTexture = null;
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(rt);

                byte[] bytes = screenShot.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(screenShot);

                File.WriteAllBytes(targetPath, bytes);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Screenshot capture exception: {e.Message}");
            }
        }
    }
}
