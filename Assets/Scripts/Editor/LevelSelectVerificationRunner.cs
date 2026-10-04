using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using MazeSpell.UI;

namespace MazeSpell.Editor
{
    public static class LevelSelectVerificationRunner
    {
        private static bool hasRun = false;

        [MenuItem("MazeSpell/Verify Level Select UI")]
        public static void RunLevelSelectSetupAndVerification()
        {
            string logDir = Path.Combine(Application.dataPath, "..", "Logs");
            if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
            string logPath = Path.Combine(logDir, "levelselect_verification.log");

            StringWriter sw = new StringWriter();
            sw.WriteLine("=== MAZESPELL LEVEL SELECT VERIFICATION ===");
            sw.WriteLine($"Timestamp: {DateTime.Now}");

            int passed = 0;
            int failed = 0;

            void Check(bool cond, string msg)
            {
                if (cond)
                {
                    passed++;
                    sw.WriteLine($"[PASS] {msg}");
                    Debug.Log($"[LevelSelect Test PASS] {msg}");
                }
                else
                {
                    failed++;
                    sw.WriteLine($"[FAIL] {msg}");
                    Debug.LogError($"[LevelSelect Test FAIL] {msg}");
                }
            }

            try
            {
                string[] scenesToTest = new string[]
                {
                    "Assets/Scenes/LevelSelect.unity",
                    "Assets/Ui/Screens/LevelSelect.unity"
                };

                foreach (var scenePath in scenesToTest)
                {
                    if (!File.Exists(scenePath)) continue;

                    var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    Check(scene.IsValid(), $"Buka scene: {scenePath}");

                    var controller = UnityEngine.Object.FindAnyObjectByType<LevelSelectController>();
                    if (controller == null)
                    {
                        var canvas = GameObject.Find("Canvas");
                        if (canvas != null)
                        {
                            controller = canvas.AddComponent<LevelSelectController>();
                        }
                    }
                    Check(controller != null, $"LevelSelectController tersedia di {scenePath}");

                    // Pastikan referensi asset sprite tombol level terhubung
                    controller.EnsureSpritesAssigned();

                    // Jalankan refresh UI tombol level
                    controller.RefreshLevelButtonsUI();

                    // Periksa 5 Tombol Level
                    Button[] btns = new Button[5];
                    for (int i = 1; i <= 5; i++)
                    {
                        var go = GameObject.Find($"Level_{i}_Button");
                        Check(go != null, $"GameObject Level_{i}_Button ada");
                        btns[i - 1] = go != null ? go.GetComponent<Button>() : null;
                        Check(btns[i - 1] != null, $"Komponen Button pada Level_{i}_Button valid");
                    }

                    // Uji Status Awal: Level 1 Terbuka, Level 2-5 Terkunci
                    MazeSpell.LevelProgressionManager.ResetProgression();
                    controller.RefreshLevelButtonsUI();

                    Check(btns[0].interactable == true, "Awal Game: Level 1 TERBUKA (interactable = true)");
                    Check(btns[1].interactable == false, "Awal Game: Level 2 TERKUNCI (interactable = false)");
                    Check(btns[2].interactable == false, "Awal Game: Level 3 TERKUNCI (interactable = false)");
                    Check(btns[3].interactable == false, "Awal Game: Level 4 TERKUNCI (interactable = false)");
                    Check(btns[4].interactable == false, "Awal Game: Level 5 TERKUNCI (interactable = false)");

                    // Periksa Sprite
                    var img1 = btns[0].GetComponent<Image>();
                    var img2 = btns[1].GetComponent<Image>();
                    Check(img1 != null && img1.sprite != null, "Sprite Level 1 valid");
                    Check(img2 != null && img2.sprite != null, "Sprite Level 2 (Locked) valid");
                    Check(img1.sprite.name.Contains("btn_level1") || img1.sprite.name.Contains("btn_level_1"), "Level 1 menggunakan sprite terbuka btn_level1");
                    Check(img2.sprite.name.Contains("btn_level_locked"), "Level 2 menggunakan sprite gembok terkunci btn_level_locked");

                    // Uji Klik Level Terbuka
                    try
                    {
                        btns[0].onClick.Invoke();
                        Check(true, "Level 1 (Terbuka) dapat diklik tanpa error");
                    }
                    catch (Exception ex)
                    {
                        Check(false, $"Error saat klik Level 1: {ex.Message}");
                    }

                    // Uji Level Terkunci tidak bisa diklik (tidak ada persistent listener)
                    Check(btns[1].onClick.GetPersistentEventCount() == 0, "Level 2 (Terkunci) tidak memiliki listener klik aktif");

                    // Uji Progres Sekuensial 1 -> 5
                    MazeSpell.LevelProgressionManager.UnlockLevel(2);
                    controller.RefreshLevelButtonsUI();
                    Check(btns[1].interactable == true && btns[2].interactable == false, "Setelah Level 1 selesai: Level 2 TERBUKA, Level 3 TERKUNCI");
                    Check(btns[1].GetComponent<Image>().sprite.name.Contains("btn_level2") || btns[1].GetComponent<Image>().sprite.name.Contains("btn_level_2"), "Level 2 terbuka menggunakan sprite btn_level2");

                    MazeSpell.LevelProgressionManager.UnlockLevel(3);
                    controller.RefreshLevelButtonsUI();
                    Check(btns[2].interactable == true && btns[3].interactable == false, "Setelah Level 2 selesai: Level 3 TERBUKA, Level 4 TERKUNCI");
                    Check(btns[2].GetComponent<Image>().sprite.name.Contains("btn_level3") || btns[2].GetComponent<Image>().sprite.name.Contains("btn_level_3"), "Level 3 terbuka menggunakan sprite btn_level3");

                    MazeSpell.LevelProgressionManager.UnlockLevel(4);
                    controller.RefreshLevelButtonsUI();
                    Check(btns[3].interactable == true && btns[4].interactable == false, "Setelah Level 3 selesai: Level 4 TERBUKA, Level 5 TERKUNCI");
                    Check(btns[3].GetComponent<Image>().sprite.name.Contains("btn_level4") || btns[3].GetComponent<Image>().sprite.name.Contains("btn_level_4"), "Level 4 terbuka menggunakan sprite btn_level4");

                    MazeSpell.LevelProgressionManager.UnlockLevel(5);
                    controller.RefreshLevelButtonsUI();
                    Check(btns[4].interactable == true, "Setelah Level 4 selesai: Level 5 TERBUKA");
                    Check(btns[4].GetComponent<Image>().sprite.name.Contains("btn_level5") || btns[4].GetComponent<Image>().sprite.name.Contains("btn_level_5"), "Level 5 terbuka menggunakan sprite btn_level5");

                    // Reset progres ke kondisi awal
                    MazeSpell.LevelProgressionManager.ResetProgression();
                    controller.RefreshLevelButtonsUI();
                    Check(btns[0].interactable == true && btns[1].interactable == false, "Reset progres berhasil: Level 1 terbuka, Level 2-5 terkunci");

                    // HomeButton
                    var homeBtn = GameObject.Find("HomeButton")?.GetComponent<Button>();
                    Check(homeBtn != null, "HomeButton ada dan memiliki Button");

                    // Simpan scene permanen dalam kondisi default
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    sw.WriteLine($"Scene {scenePath} berhasil disimpan.");
                }

                // Sangat penting: Selalu kembalikan MainMenu sebagai scene aktif di editor
                if (File.Exists("Assets/Scenes/MainMenu.unity"))
                {
                    EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
                    sw.WriteLine("MainMenu.unity berhasil dibuka kembali sebagai active startup scene.");
                }

                sw.WriteLine($"TOTAL HASIL: {passed} LULUS, {failed} GAGAL");
            }
            catch (Exception ex)
            {
                sw.WriteLine($"EXCEPTION: {ex}");
                Debug.LogError($"[LevelSelect Verification Exception] {ex}");
            }
            finally
            {
                File.WriteAllText(logPath, sw.ToString());
            }
        }
    }
}
