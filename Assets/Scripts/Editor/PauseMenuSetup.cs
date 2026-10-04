using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace MazeSpell.Editor
{
    /// <summary>
    /// Editor script untuk mengintegrasikan PausePanel ke dalam Canvas di SampleScene.unity.
    /// Memastikan PausePanel berada di Canvas yang sama tanpa membuat Canvas duplikat.
    /// </summary>
    [InitializeOnLoad]
    public static class PauseMenuSetup
    {
        private const string GameplayScenePath = "Assets/Scenes/SampleScene.unity";

        static PauseMenuSetup()
        {
            EditorApplication.delayCall += AutoSetupIfNeeded;
        }

        private static void AutoSetupIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            if (!SessionState.GetBool("MazeSpell_PauseMenuSetupDone_V1", false))
            {
                SessionState.SetBool("MazeSpell_PauseMenuSetupDone_V1", true);
                SetupPauseMenuInScene(false);
            }
        }

        [MenuItem("MazeSpell/Setup Pause Menu in Scene")]
        public static void SetupPauseMenuManual()
        {
            SetupPauseMenuInScene(true);
        }

        public static void SetupPauseMenuInScene(bool interactive)
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
                if (!scene.IsValid())
                {
                    Debug.LogWarning($"[PauseMenuSetup] Scene gameplay tidak ditemukan di '{GameplayScenePath}'");
                    return;
                }

                // Periksa apakah PausePanel sudah ada untuk mencegah duplikasi
                var existingController = UnityEngine.Object.FindAnyObjectByType<MazeSpell.UI.PauseMenuController>();
                if (existingController != null)
                {
                    Debug.Log("[PauseMenuSetup] PauseMenuController sudah ada di scene. Tidak membuat duplikat.");
                    if (interactive)
                    {
                        EditorUtility.DisplayDialog("Pause Menu", "Pause Menu sudah terpasang di scene gameplay!", "OK");
                    }
                    return;
                }

                // Cari Canvas gameplay yang sudah ada
                Transform targetCanvasTransform = null;
                var hud = UnityEngine.Object.FindAnyObjectByType<MazeSpell.UI.GameplayHUD>();
                if (hud != null)
                {
                    targetCanvasTransform = hud.transform;
                }
                else
                {
                    var anyCanvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
                    if (anyCanvas != null)
                    {
                        targetCanvasTransform = anyCanvas.transform;
                    }
                }

                if (targetCanvasTransform == null)
                {
                    Debug.LogWarning("[PauseMenuSetup] Canvas belum ada di scene. Jalankan Setup Gameplay HUD terlebih dahulu.");
                    // Jalankan setup HUD terlebih dahulu agar Canvas dibuat
                    GameplayHUDSetup.SetupHUDInScene(false);

                    var newCanvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
                    if (newCanvas != null) targetCanvasTransform = newCanvas.transform;
                }

                if (targetCanvasTransform != null)
                {
                    GameObject pausePanel = MazeSpell.UI.PauseMenuController.BuildPausePanelHierarchy(targetCanvasTransform);
                    pausePanel.SetActive(false);

                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);

                    Debug.Log("[PauseMenuSetup] PausePanel berhasil ditambahkan ke Canvas gameplay dan disimpan!");
                    if (interactive)
                    {
                        EditorUtility.DisplayDialog("Pause Menu", "Pause Menu berhasil disiapkan dan diintegrasikan ke Canvas gameplay!", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PauseMenuSetup] Gagal menyusun Pause Menu: {ex}");
            }
        }
    }
}
