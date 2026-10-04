using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using MazeSpell.UI;

namespace MazeSpell.Editor
{
    public static class MainMenuVerification
    {
        [MenuItem("MazeSpell/Run Main Menu Verification")]
        public static void RunVerification()
        {
            Debug.Log("=====================================================");
            Debug.Log(">>> MEMULAI VERIFIKASI MAIN MENU & LEVEL SELECT <<<");
            Debug.Log("=====================================================");

            int passCount = 0;
            int failCount = 0;

            void Check(bool condition, string testName)
            {
                if (condition)
                {
                    passCount++;
                    Debug.Log($"[VERIFIKASI LULUS] {testName}");
                }
                else
                {
                    failCount++;
                    Debug.LogError($"[VERIFIKASI GAGAL] {testName}");
                }
            }

            try
            {
                // ==========================================
                // 1. UJI BUILD SETTINGS
                // ==========================================
                var buildScenes = EditorBuildSettings.scenes;
                Check(buildScenes.Length >= 3, "Build Settings memiliki minimal 3 scene terdaftar");
                Check(buildScenes[0].path == "Assets/Scenes/MainMenu.unity" && buildScenes[0].enabled,
                    "Build Settings [0] adalah Assets/Scenes/MainMenu.unity (Enabled)");
                Check(buildScenes[1].path == "Assets/Scenes/SampleScene.unity" && buildScenes[1].enabled,
                    "Build Settings [1] adalah Assets/Scenes/SampleScene.unity (Enabled)");
                Check((buildScenes[2].path == "Assets/Scenes/LevelSelect.unity" || buildScenes[2].path == "Assets/Ui/Screens/LevelSelect.unity") && buildScenes[2].enabled,
                    "Build Settings [2] adalah LevelSelect.unity (Enabled)");

                // ==========================================
                // 2. UJI SCENE MAIN MENU
                // ==========================================
                var mainMenuScene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
                Check(mainMenuScene.IsValid(), "Scene MainMenu.unity berhasil dibuka");

                // Periksa Main Camera
                var mainCam = Camera.main;
                Check(mainCam != null, "Main Camera ada dan bertag MainCamera");
                Check(mainCam != null && mainCam.orthographic, "Main Camera menggunakan proyeksi Orthographic");

                // Periksa Canvas
                var canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
                Check(canvas != null, "Canvas UI ada di scene MainMenu");
                Check(canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay, "Canvas menggunakan Screen Space - Overlay");

                var scaler = UnityEngine.Object.FindAnyObjectByType<CanvasScaler>();
                Check(scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize,
                    "CanvasScaler aktif dengan ScaleWithScreenSize");

                var menuController = UnityEngine.Object.FindAnyObjectByType<MainMenuController>();
                Check(menuController != null, "MainMenuController terpasang pada Canvas di MainMenu.unity");

                // Periksa Background
                var bgObj = GameObject.Find("Background");
                Check(bgObj != null, "GameObject Background ada");
                var bgImage = bgObj != null ? bgObj.GetComponent<Image>() : null;
                Check(bgImage != null && bgImage.sprite != null && bgImage.sprite.name.Contains("menuBG"),
                    $"Background menggunakan sprite menuBG (Aktif: {(bgImage != null && bgImage.sprite != null ? bgImage.sprite.name : "null")})");

                // Periksa Logo MazeSpell telah DIHAPUS SEPENUHNYA
                var logoObj = GameObject.Find("Logo");
                Check(logoObj == null, "Logo MazeSpell telah DIHAPUS SEPENUHNYA dari Main Menu");

                // Periksa Tombol PLAY
                var playBtnObj = GameObject.Find("PlayButton");
                Check(playBtnObj != null && playBtnObj.activeSelf, "GameObject PlayButton ada dan aktif");
                var playBtn = playBtnObj != null ? playBtnObj.GetComponent<Button>() : null;
                Check(playBtn != null, "PlayButton memiliki komponen Button");
                var playImg = playBtnObj != null ? playBtnObj.GetComponent<Image>() : null;
                Check(playImg != null && playImg.sprite != null && playImg.sprite.name.Contains("cartoon_play"), "PlayButton menggunakan sprite btn_cartoon_play");
                Check(playBtnObj != null && playBtnObj.transform.childCount == 0, "PlayButton tidak memiliki teks tambahan (teks menyatu dengan gambar)");

                // Periksa Tombol LEVEL
                var levelBtnObj = GameObject.Find("LevelButton");
                Check(levelBtnObj != null && levelBtnObj.activeSelf, "GameObject LevelButton ada dan aktif");
                var levelBtn = levelBtnObj != null ? levelBtnObj.GetComponent<Button>() : null;
                Check(levelBtn != null, "LevelButton memiliki komponen Button");
                var levelImg = levelBtnObj != null ? levelBtnObj.GetComponent<Image>() : null;
                Check(levelImg != null && levelImg.sprite != null && levelImg.sprite.name.Contains("cartoon_level"), "LevelButton menggunakan sprite btn_cartoon_level");
                Check(levelBtnObj != null && levelBtnObj.transform.childCount == 0, "LevelButton tidak memiliki teks tambahan (teks menyatu dengan gambar)");

                // Periksa Aturan Mutlak: HANYA ADA 2 BUTTON di Main Menu (PLAY & LEVEL)
                var allButtonsInMenu = UnityEngine.Object.FindObjectsByType<Button>();
                Check(allButtonsInMenu.Length == 2,
                    $"HANYA ADA 2 BUTTON di Main Menu (Aktif: {allButtonsInMenu.Length} tombol)");

                // Periksa Tombol-tombol Lama Harus Sudah Dihapus
                var exitBtnObj = GameObject.Find("ExitButton");
                Check(exitBtnObj == null, "Tombol lama ExitButton sudah dihapus bersih");

                var audioBtnObj = GameObject.Find("AudioSettingsButton");
                Check(audioBtnObj == null, "Tombol lama AudioSettingsButton sudah dihapus bersih");

                // Periksa EventSystem
                var eventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
                Check(eventSystem != null, "EventSystem ada di scene MainMenu");

                // Periksa Missing Component / Missing Reference
                CheckNoMissingComponents(mainMenuScene, "MainMenu", Check);

                // Uji Klik Tombol PLAY (Simulasi Pemanggilan Method Gameplay)
                try
                {
                    playBtn.onClick.Invoke();
                    Check(true, "Simulasi klik PLAY berhasil dieksekusi tanpa error runtime");
                }
                catch (Exception ex)
                {
                    Check(false, $"Simulasi klik PLAY gagal: {ex.Message}");
                }

                // Uji Klik Tombol LEVEL (Simulasi Pemanggilan Method Level Select)
                try
                {
                    levelBtn.onClick.Invoke();
                    Check(true, "Simulasi klik LEVEL berhasil dieksekusi tanpa error runtime");
                }
                catch (Exception ex)
                {
                    Check(false, $"Simulasi klik LEVEL gagal: {ex.Message}");
                }

                // Render Screenshot MainMenu
                CaptureSceneScreenshot("c:/Users/ASUS/Mazell/mainmenu_verified.png");

                // ==========================================
                // 3. UJI SCENE LEVEL SELECT
                // ==========================================
                string levelSelectPath = File.Exists("Assets/Scenes/LevelSelect.unity") 
                    ? "Assets/Scenes/LevelSelect.unity" 
                    : "Assets/Ui/Screens/LevelSelect.unity";
                
                var levelSelectScene = EditorSceneManager.OpenScene(levelSelectPath, OpenSceneMode.Single);
                Check(levelSelectScene.IsValid(), $"Scene LevelSelect berhasil dibuka dari {levelSelectPath}");

                var selectController = UnityEngine.Object.FindAnyObjectByType<LevelSelectController>();
                Check(selectController != null, "LevelSelectController terpasang di LevelSelect.unity");

                var lvlBgObj = GameObject.Find("Background");
                var lvlBgImage = lvlBgObj != null ? lvlBgObj.GetComponent<Image>() : null;
                Check(lvlBgImage != null && lvlBgImage.sprite != null && lvlBgImage.sprite.name.Contains("menuBG"),
                    "LevelSelect menggunakan background menuBG");

                var homeBtnObj = GameObject.Find("HomeButton");
                Check(homeBtnObj != null, "GameObject HomeButton (Kembali ke Main Menu) ada");
                var homeBtn = homeBtnObj != null ? homeBtnObj.GetComponent<Button>() : null;
                Check(homeBtn != null, "HomeButton memiliki komponen Button");

                // Uji 5 Tombol Level & Tata Letak
                selectController.RefreshLevelButtonsUI();

                var l1Btn = GameObject.Find("Level_1_Button")?.GetComponent<Button>();
                var l2Btn = GameObject.Find("Level_2_Button")?.GetComponent<Button>();
                var l3Btn = GameObject.Find("Level_3_Button")?.GetComponent<Button>();
                var l4Btn = GameObject.Find("Level_4_Button")?.GetComponent<Button>();
                var l5Btn = GameObject.Find("Level_5_Button")?.GetComponent<Button>();

                Check(l1Btn != null, "Tombol Level 1 ada di LevelSelect");
                Check(l2Btn != null, "Tombol Level 2 ada di LevelSelect");
                Check(l3Btn != null, "Tombol Level 3 ada di LevelSelect");
                Check(l4Btn != null, "Tombol Level 4 ada di LevelSelect");
                Check(l5Btn != null, "Tombol Level 5 ada di LevelSelect");

                // Uji Progres Level Baru (Awal Permainan: Level 1 Terbuka, Level 2-5 Terkunci)
                MazeSpell.LevelProgressionManager.ResetProgression();
                selectController.RefreshLevelButtonsUI();

                Check(l1Btn.interactable == true, "Awal Permainan: Level 1 TERBUKA (interactable = true)");
                Check(l2Btn.interactable == false, "Awal Permainan: Level 2 TERKUNCI (interactable = false)");
                Check(l3Btn.interactable == false, "Awal Permainan: Level 3 TERKUNCI (interactable = false)");
                Check(l4Btn.interactable == false, "Awal Permainan: Level 4 TERKUNCI (interactable = false)");
                Check(l5Btn.interactable == false, "Awal Permainan: Level 5 TERKUNCI (interactable = false)");

                var l1Img = l1Btn.GetComponent<Image>();
                var l2Img = l2Btn.GetComponent<Image>();
                Check(l1Img != null && l1Img.sprite != null, "Level 1 memiliki Sprite yang valid");
                Check(l2Img != null && l2Img.sprite != null, "Level 2 memiliki Sprite yang valid");
                Check(l1Img.sprite.name.Contains("btn_level_1") || l1Img.sprite.name.Contains("btn_level1"), "Level 1 menggunakan sprite terbuka btn_level_1");
                Check(l2Img.sprite.name.Contains("btn_level_locked"), "Level 2 menggunakan sprite terkunci btn_level_locked");

                // Uji Klik Level Terbuka (Level 1)
                try
                {
                    l1Btn.onClick.Invoke();
                    Check(true, "Klik Level 1 (TERBUKA) berhasil dieksekusi tanpa error");
                }
                catch (Exception ex)
                {
                    Check(false, $"Klik Level 1 gagal: {ex.Message}");
                }

                // Uji Klik Level Terkunci (Level 2) - Pastikan tidak ada event listener aktif
                Check(l2Btn.onClick.GetPersistentEventCount() == 0, "Level 2 (TERKUNCI) tidak memiliki listener klik aktif / tidak dapat ditekan");

                // Uji Simulasi Progres Unlock: Level 1 selesai -> Level 2 terbuka
                MazeSpell.LevelProgressionManager.UnlockLevel(2);
                selectController.RefreshLevelButtonsUI();
                Check(l2Btn.interactable == true, "Setelah Level 1 selesai: Level 2 TERBUKA");
                Check(l3Btn.interactable == false, "Setelah Level 1 selesai: Level 3 tetap TERKUNCI");

                // Uji Simulasi: Level 2 selesai -> Level 3 terbuka
                MazeSpell.LevelProgressionManager.UnlockLevel(3);
                selectController.RefreshLevelButtonsUI();
                Check(l3Btn.interactable == true, "Setelah Level 2 selesai: Level 3 TERBUKA");
                Check(l4Btn.interactable == false, "Setelah Level 2 selesai: Level 4 tetap TERKUNCI");

                // Uji Simulasi: Level 3 selesai -> Level 4 terbuka
                MazeSpell.LevelProgressionManager.UnlockLevel(4);
                selectController.RefreshLevelButtonsUI();
                Check(l4Btn.interactable == true, "Setelah Level 3 selesai: Level 4 TERBUKA");
                Check(l5Btn.interactable == false, "Setelah Level 3 selesai: Level 5 tetap TERKUNCI");

                // Uji Simulasi: Level 4 selesai -> Level 5 terbuka
                MazeSpell.LevelProgressionManager.UnlockLevel(5);
                selectController.RefreshLevelButtonsUI();
                Check(l5Btn.interactable == true, "Setelah Level 4 selesai: Level 5 TERBUKA");

                // Reset kembali ke status awal agar user bermain dari kondisi normal
                MazeSpell.LevelProgressionManager.ResetProgression();
                selectController.RefreshLevelButtonsUI();
                Check(l1Btn.interactable == true && l2Btn.interactable == false, "Status reset progres kembali ke default (Level 1 terbuka, Level 2-5 terkunci)");

                // Simpan perubahan scene LevelSelect agar permanen di file .unity
                EditorSceneManager.MarkSceneDirty(levelSelectScene);
                EditorSceneManager.SaveScene(levelSelectScene);

                // Render Screenshot LevelSelect
                CaptureSceneScreenshot("c:/Users/ASUS/Mazell/levelselect_verified.png");

                // Kembalikan ke MainMenu sebagai scene aktif
                EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);

                Debug.Log("=====================================================");
                Debug.Log($"HASIL VERIFIKASI: {passCount} LULUS, {failCount} GAGAL");
                Debug.Log("=====================================================");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VERIFIKASI GAGAL TOTAL] Terjadi exception: {ex}");
            }
        }

        private static void CheckNoMissingComponents(UnityEngine.SceneManagement.Scene scene, string sceneName, Action<bool, string> checkAction)
        {
            var rootObjects = scene.GetRootGameObjects();
            int missingCount = 0;

            foreach (var root in rootObjects)
            {
                var allTransforms = root.GetComponentsInChildren<Transform>(true);
                foreach (var t in allTransforms)
                {
                    var components = t.GetComponents<Component>();
                    for (int i = 0; i < components.Length; i++)
                    {
                        if (components[i] == null)
                        {
                            missingCount++;
                            Debug.LogError($"[MissingComponent] GameObject '{t.name}' di {sceneName} memiliki missing component di index {i}!");
                        }
                    }
                }
            }

            checkAction(missingCount == 0, $"Tidak ada Missing Component / Reference pada {sceneName} (Ditemukan: {missingCount})");
        }

        private static void CaptureSceneScreenshot(string outputPath)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            int width = 960;
            int height = 540;
            RenderTexture rt = new RenderTexture(width, height, 24);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(rt);

            File.WriteAllBytes(outputPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            Debug.Log($"[Screenshot] Screenshot disimpan ke: {outputPath}");
        }
    }
}
