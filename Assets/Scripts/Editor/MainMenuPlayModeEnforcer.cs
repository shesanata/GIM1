using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace MazeSpell.Editor
{
    /// <summary>
    /// Memastikan secara konsisten bahwa Prologue.unity menjadi scene startup pertama
    /// saat game dijalankan di Unity Editor maupun saat build:
    /// GAME START -> PROLOGUE -> MAIN MENU -> PLAY / LEVEL SELECT.
    /// Mengunci EditorSceneManager.playModeStartScene ke Assets/Scenes/Prologue.unity.
    /// </summary>
    [InitializeOnLoad]
    public static class MainMenuPlayModeEnforcer
    {
        private const string PrologueScenePath = "Assets/Scenes/Prologue.unity";
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string GameplayScenePath = "Assets/Scenes/SampleScene.unity";
        private const string LevelSelectScenePath = "Assets/Scenes/LevelSelect.unity";
        private const string LegacyLevelSelectScenePath = "Assets/Ui/Screens/LevelSelect.unity";

        static MainMenuPlayModeEnforcer()
        {
            EnforceStartupFlow();
            EditorApplication.delayCall += EnforceStartupFlow;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                EnforceStartupFlow();
            }
        }

        [MenuItem("MazeSpell/Enforce Prologue As Startup Scene")]
        public static void EnforceStartupFlowManual()
        {
            EnforceStartupFlow();
            Debug.Log("[MainMenuPlayModeEnforcer] Prologue berhasil dipastikan sebagai startup scene game!");
        }

        public static void EnforceStartupFlow()
        {
            try
            {
                // 1. Kunci EditorSceneManager.playModeStartScene ke Prologue.unity jika scene ada
                if (File.Exists(PrologueScenePath))
                {
                    SceneAsset prologueSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(PrologueScenePath);
                    if (prologueSceneAsset != null)
                    {
                        if (EditorSceneManager.playModeStartScene != prologueSceneAsset)
                        {
                            EditorSceneManager.playModeStartScene = prologueSceneAsset;
                            Debug.Log($"[MainMenuPlayModeEnforcer] playModeStartScene berhasil dikunci ke: {PrologueScenePath}");
                        }
                    }
                }
                else
                {
                    SceneAsset mainSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath);
                    if (mainSceneAsset != null)
                    {
                        EditorSceneManager.playModeStartScene = mainSceneAsset;
                    }
                }

                // 2. Verifikasi & tata urutan Build Settings:
                // [0] Assets/Scenes/Prologue.unity
                // [1] Assets/Scenes/MainMenu.unity
                // [2] Assets/Scenes/SampleScene.unity
                // [3] Assets/Scenes/LevelSelect.unity
                var currentScenes = EditorBuildSettings.scenes;
                bool needsUpdate = false;

                if (currentScenes == null || currentScenes.Length != 4)
                {
                    needsUpdate = true;
                }
                else if (currentScenes[0].path != PrologueScenePath || !currentScenes[0].enabled)
                {
                    needsUpdate = true;
                }
                else if (currentScenes[1].path != MainMenuScenePath || !currentScenes[1].enabled)
                {
                    needsUpdate = true;
                }
                else if (currentScenes[2].path != GameplayScenePath || !currentScenes[2].enabled)
                {
                    needsUpdate = true;
                }
                else if (currentScenes[3].path != LevelSelectScenePath || !currentScenes[3].enabled)
                {
                    needsUpdate = true;
                }

                if (needsUpdate)
                {
                    var newScenes = new EditorBuildSettingsScene[]
                    {
                        new EditorBuildSettingsScene(PrologueScenePath, true),
                        new EditorBuildSettingsScene(MainMenuScenePath, true),
                        new EditorBuildSettingsScene(GameplayScenePath, true),
                        new EditorBuildSettingsScene(LevelSelectScenePath, true)
                    };
                    EditorBuildSettings.scenes = newScenes;
                    Debug.Log("[MainMenuPlayModeEnforcer] Build Settings diperbarui: [0] Prologue, [1] MainMenu, [2] SampleScene, [3] LevelSelect");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MainMenuPlayModeEnforcer] Warning: {ex.Message}");
            }
        }
    }
}
