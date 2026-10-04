using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MazeSpell.UI;

namespace MazeSpell.Editor
{
    /// <summary>
    /// Editor tool untuk menyiapkan sistem GameplayFeedback di dalam GameplayCanvas di SampleScene.unity.
    /// Memastikan tidak ada duplikasi Canvas maupun EventSystem.
    /// </summary>
    [InitializeOnLoad]
    public static class GameplayFeedbackSetup
    {
        private const string GameplayScenePath = "Assets/Scenes/SampleScene.unity";

        static GameplayFeedbackSetup()
        {
            EditorApplication.delayCall += AutoSetupIfNeeded;
        }

        private static void AutoSetupIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            if (!SessionState.GetBool("MazeSpell_GameplayFeedbackSetupDone_V1", false))
            {
                SessionState.SetBool("MazeSpell_GameplayFeedbackSetupDone_V1", true);
                SetupFeedbackInScene(false);
            }
        }

        [MenuItem("MazeSpell/Setup Gameplay Feedback in Scene")]
        public static void SetupFeedbackManual()
        {
            SetupFeedbackInScene(true);
        }

        public static void SetupFeedbackInScene(bool interactive)
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
                if (!scene.IsValid())
                {
                    Debug.LogWarning($"[GameplayFeedbackSetup] Scene gameplay tidak ditemukan di '{GameplayScenePath}'");
                    return;
                }

                // Cek apakah controller feedback sudah ada
                var existingController = UnityEngine.Object.FindAnyObjectByType<GameplayFeedbackController>();
                if (existingController != null)
                {
                    Debug.Log("[GameplayFeedbackSetup] GameplayFeedbackController sudah terpasang di scene.");
                    if (interactive)
                    {
                        EditorUtility.DisplayDialog("Gameplay Feedback", "Gameplay Feedback sudah terpasang di scene gameplay!", "OK");
                    }
                    return;
                }

                // Cari GameplayCanvas yang sudah ada
                Transform targetCanvas = null;
                var hud = UnityEngine.Object.FindAnyObjectByType<GameplayHUD>();
                if (hud != null) targetCanvas = hud.transform;
                else
                {
                    var anyCanvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
                    if (anyCanvas != null) targetCanvas = anyCanvas.transform;
                }

                if (targetCanvas == null)
                {
                    Debug.Log("[GameplayFeedbackSetup] Canvas belum ditemukan. Menjalankan Setup Gameplay HUD terlebih dahulu...");
                    GameplayHUDSetup.SetupHUDInScene(false);
                    var newCanvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
                    if (newCanvas != null) targetCanvas = newCanvas.transform;
                }

                if (targetCanvas != null)
                {
                    GameObject feedbackObj = new GameObject("GameplayFeedback");
                    feedbackObj.transform.SetParent(targetCanvas, false);

                    var controller = feedbackObj.AddComponent<GameplayFeedbackController>();
                    controller.BuildUIElements(feedbackObj.transform);

                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);

                    Debug.Log("[GameplayFeedbackSetup] Gameplay Feedback berhasil disiapkan dan disimpan di scene gameplay!");
                    if (interactive)
                    {
                        EditorUtility.DisplayDialog("Gameplay Feedback", "Gameplay Feedback berhasil disiapkan dan disimpan di scene gameplay!", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameplayFeedbackSetup] Gagal menyusun Gameplay Feedback: {ex}");
            }
        }
    }
}
