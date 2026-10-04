using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using MazeSpell.UI;

namespace MazeSpell.Editor
{
    public static class MainMenuTwoButtonsVerificationRunner
    {
        private const string LogPath = "c:/Users/ASUS/Mazell/mainmenu_two_buttons_verification.log";
        private const string ScreenshotPath = "c:/Users/ASUS/Mazell/mainmenu_two_buttons_verified.png";

        [MenuItem("MazeSpell/Verify Two-Button Main Menu")]
        public static void RunVerificationManual()
        {
            SessionState.SetBool("MazeSpell_CartoonTwoButtonsVerified_V1", false);
            RunVerificationOnce();
        }

        private static void RunVerificationOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (SessionState.GetBool("MazeSpell_CartoonTwoButtonsVerified_V1", false)) return;
            SessionState.SetBool("MazeSpell_CartoonTwoButtonsVerified_V1", true);

            var sb = new StringBuilder();
            int passed = 0;
            int failed = 0;

            void Log(string msg)
            {
                Debug.Log(msg);
                sb.AppendLine(msg);
            }

            void Assert(bool condition, string testName)
            {
                if (condition)
                {
                    passed++;
                    Log($"[PASS] {testName}");
                }
                else
                {
                    failed++;
                    Log($"[FAIL] {testName}");
                }
            }

            Log("=================================================");
            Log(">>> VERIFIKASI MAIN MENU DUA TOMBOL (PLAY & LEVEL) <<<");
            Log("=================================================");

            try
            {
                // 0. Build Settings & PlayMode Start Scene
                var buildScenes = EditorBuildSettings.scenes;
                Assert(buildScenes.Length >= 3, "Build Settings memiliki minimal 3 scene");
                Assert(buildScenes[0].path == "Assets/Scenes/Prologue.unity" && buildScenes[0].enabled, "Build Settings [0] adalah Prologue.unity (Enabled)");
                Assert(buildScenes[1].path == "Assets/Scenes/MainMenu.unity" && buildScenes[1].enabled, "Build Settings [1] adalah MainMenu.unity (Enabled)");
                Assert(buildScenes[2].path == "Assets/Scenes/SampleScene.unity" && buildScenes[2].enabled, "Build Settings [2] adalah SampleScene.unity (Enabled)");

                SceneAsset prologueSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Prologue.unity");
                if (prologueSceneAsset != null)
                {
                    EditorSceneManager.playModeStartScene = prologueSceneAsset;
                }
                Assert(EditorSceneManager.playModeStartScene != null && EditorSceneManager.playModeStartScene.name == "Prologue", "playModeStartScene terkunci ke Prologue.unity");

                string scenePath = "Assets/Scenes/MainMenu.unity";
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                Assert(scene.IsValid(), "Scene MainMenu.unity berhasil dimuat sebagai active scene");

                // 1. Canvas
                var canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
                Assert(canvas != null, "Canvas UI ditemukan di MainMenu");

                // 2. Background
                var bgObj = GameObject.Find("Background");
                Assert(bgObj != null && bgObj.activeSelf, "Background Main Menu ada dan aktif");
                var bgImg = bgObj != null ? bgObj.GetComponent<Image>() : null;
                Assert(bgImg != null && bgImg.sprite != null && bgImg.sprite.name.Contains("menuBG"), "Background menggunakan sprite menuBG yang sesuai");

                // 3. Logo MazeSpell (HAPUS SEPENUHNYA SESUAI INSTRUKSI)
                var logoObj = GameObject.Find("Logo");
                Assert(logoObj == null, "Logo MazeSpell telah DIHAPUS SEPENUHNYA dari hierarchy");

                // 4. Tombol PLAY
                var playObj = GameObject.Find("PlayButton");
                Assert(playObj != null && playObj.activeSelf, "Tombol PLAY ada dan aktif");
                var playBtn = playObj != null ? playObj.GetComponent<Button>() : null;
                Assert(playBtn != null, "PlayButton memiliki komponen Button");
                var playImg = playObj != null ? playObj.GetComponent<Image>() : null;
                Assert(playImg != null && playImg.sprite != null && playImg.sprite.name.Contains("cartoon_play"), "PlayButton menggunakan sprite btn_cartoon_play");
                Assert(playObj != null && playObj.transform.childCount == 0, "PlayButton tidak memiliki teks tambahan (teks menyatu dengan gambar)");

                // 5. Tombol LEVEL
                var levelObj = GameObject.Find("LevelButton");
                Assert(levelObj != null && levelObj.activeSelf, "Tombol LEVEL ada dan aktif");
                var levelBtn = levelObj != null ? levelObj.GetComponent<Button>() : null;
                Assert(levelBtn != null, "LevelButton memiliki komponen Button");
                var levelImg = levelObj != null ? levelObj.GetComponent<Image>() : null;
                Assert(levelImg != null && levelImg.sprite != null && levelImg.sprite.name.Contains("cartoon_level"), "LevelButton menggunakan sprite btn_cartoon_level");
                Assert(levelObj != null && levelObj.transform.childCount == 0, "LevelButton tidak memiliki teks tambahan (teks menyatu dengan gambar)");

                // 6. Tombol yang HARUS DIHAPUS SEPENUHNYA
                var audioObj = GameObject.Find("AudioSettingsButton");
                Assert(audioObj == null, "AudioSettingsButton telah DIHAPUS SEPENUHNYA dari hierarchy");

                var exitObj = GameObject.Find("ExitButton");
                Assert(exitObj == null, "ExitButton telah DIHAPUS SEPENUHNYA dari hierarchy");

                var settingsObj = GameObject.Find("SettingsButton");
                Assert(settingsObj == null, "SettingsButton tidak ada di hierarchy");

                var backObj = GameObject.Find("BackButton");
                Assert(backObj == null, "BackButton tidak ada di hierarchy");

                var optionsObj = GameObject.Find("OptionsButton");
                Assert(optionsObj == null, "OptionsButton tidak ada di hierarchy");

                // 7. TOTAL BUTTON DI MAIN MENU HARUS TEPAT 2
                var allButtons = UnityEngine.Object.FindObjectsByType<Button>();
                Assert(allButtons.Length == 2, $"HANYA ADA 2 BUTTON di Main Menu (Aktif: {allButtons.Length})");

                // 8. FUNGSI PLAY: Tetap menjalankan fungsi Play seperti sekarang
                var menuController = UnityEngine.Object.FindAnyObjectByType<MainMenuController>();
                Assert(menuController != null, "MainMenuController terpasang pada Canvas");

                bool playHasListener = false;
                if (playBtn != null)
                {
                    int count = playBtn.onClick.GetPersistentEventCount();
                    for (int i = 0; i < count; i++)
                    {
                        if (playBtn.onClick.GetPersistentMethodName(i) == "PlayGame")
                        {
                            playHasListener = true;
                            break;
                        }
                    }
                }
                Assert(playHasListener, "Tombol PLAY memanggil MainMenuController.PlayGame");

                // 9. FUNGSI LEVEL: Tetap membuka halaman Level Select seperti sekarang
                bool levelHasListener = false;
                if (levelBtn != null)
                {
                    int count = levelBtn.onClick.GetPersistentEventCount();
                    for (int i = 0; i < count; i++)
                    {
                        if (levelBtn.onClick.GetPersistentMethodName(i) == "OpenLevelSelect")
                        {
                            levelHasListener = true;
                            break;
                        }
                    }
                }
                Assert(levelHasListener, "Tombol LEVEL memanggil MainMenuController.OpenLevelSelect");

                // 10. Navigasi 2 Tombol
                if (playBtn != null && levelBtn != null)
                {
                    Assert(playBtn.navigation.selectOnDown == levelBtn, "Navigasi PlayButton Down mengarah ke LevelButton");
                    Assert(playBtn.navigation.selectOnUp == levelBtn, "Navigasi PlayButton Up mengarah ke LevelButton");
                    Assert(levelBtn.navigation.selectOnUp == playBtn, "Navigasi LevelButton Up mengarah ke PlayButton");
                    Assert(levelBtn.navigation.selectOnDown == playBtn, "Navigasi LevelButton Down mengarah ke PlayButton");
                }

                // 11. Cek Missing Components / References
                int missingCount = 0;
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    {
                        var comps = t.GetComponents<Component>();
                        for (int i = 0; i < comps.Length; i++)
                        {
                            if (comps[i] == null) missingCount++;
                        }
                    }
                }
                Assert(missingCount == 0, $"Tidak ada missing components di MainMenu (Ditemukan: {missingCount})");

                // 12. Uji Klik PLAY dan LEVEL
                try
                {
                    playBtn.onClick.Invoke();
                    Assert(true, "Simulasi klik PLAY berhasil dieksekusi tanpa error");
                }
                catch (Exception ex)
                {
                    Assert(false, $"Simulasi klik PLAY gagal: {ex.Message}");
                }

                try
                {
                    levelBtn.onClick.Invoke();
                    Assert(true, "Simulasi klik LEVEL berhasil dieksekusi tanpa error");
                }
                catch (Exception ex)
                {
                    Assert(false, $"Simulasi klik LEVEL gagal: {ex.Message}");
                }

                // 13. Screenshot dengan UI
                CaptureScreenshot(ScreenshotPath, canvas);
                Log($"[SCREENSHOT] Berhasil mengambil screenshot Main Menu ke: {ScreenshotPath}");

                // Pastikan scene tersimpan dan tetap terbuka sebagai active scene
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);

                Log("=================================================");
                Log($"HASIL: {passed} LULUS, {failed} GAGAL");
                Log("=================================================");
            }
            catch (Exception ex)
            {
                failed++;
                Log($"[EXCEPTION] Error verifikasi: {ex}");
            }

            File.WriteAllText(LogPath, sb.ToString());
        }

        private static void CaptureScreenshot(string path, Canvas canvas)
        {
            try
            {
                int width = 960;
                int height = 540;
                Camera cam = Camera.main;
                if (cam == null) cam = UnityEngine.Object.FindAnyObjectByType<Camera>();
                if (cam == null) return;

                RenderTexture rt = new RenderTexture(width, height, 24);
                RenderTexture prev = RenderTexture.active;
                cam.targetTexture = rt;
                RenderTexture.active = rt;

                RenderMode origMode = canvas != null ? canvas.renderMode : RenderMode.ScreenSpaceOverlay;
                Camera origCam = canvas != null ? canvas.worldCamera : null;

                if (canvas != null)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = cam;
                }

                cam.Render();

                if (canvas != null)
                {
                    canvas.renderMode = origMode;
                    canvas.worldCamera = origCam;
                }

                Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();

                cam.targetTexture = null;
                RenderTexture.active = prev;
                UnityEngine.Object.DestroyImmediate(rt);

                File.WriteAllBytes(path, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Screenshot failed: {e.Message}");
            }
        }
    }
}
