using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

namespace MazeSpell.Editor
{
    /// <summary>
    /// Editor script untuk mengotomatisasi setup folder audio, AudioSettings UI di scene,
    /// dan tombol Audio Settings di MainMenu dan Pause Menu di game MazeSpell.
    /// </summary>
    [InitializeOnLoad]
    public static class AudioSetup
    {
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string GameplayScenePath = "Assets/Scenes/SampleScene.unity";

        private const string AudioBgmDir = "Assets/Audio/BGM";
        private const string AudioSfxDir = "Assets/Audio/SFX";
        private const string AudioUiDir = "Assets/Audio/UI";

        static AudioSetup()
        {
            EditorApplication.delayCall += AutoSetupIfNeeded;
        }

        private static void AutoSetupIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            if (!SessionState.GetBool("MazeSpell_AudioSetupDone_V1", false))
            {
                SessionState.SetBool("MazeSpell_AudioSetupDone_V1", true);
                SetupAudioFolders();
                IntegrateAudioIntoScenes(false);
            }
        }

        [MenuItem("MazeSpell/Audio/Setup Audio Folders & System")]
        public static void SetupAudioManual()
        {
            SetupAudioFolders();
            IntegrateAudioIntoScenes(true);
        }

        public static void SetupAudioFolders()
        {
            try
            {
                if (!Directory.Exists(AudioBgmDir)) Directory.CreateDirectory(AudioBgmDir);
                if (!Directory.Exists(AudioSfxDir)) Directory.CreateDirectory(AudioSfxDir);
                if (!Directory.Exists(AudioUiDir)) Directory.CreateDirectory(AudioUiDir);

                AssetDatabase.Refresh();
                Debug.Log("[AudioSetup] Folder audio terstruktur berhasil diverifikasi/dibuat.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AudioSetup] Gagal membuat folder audio: {ex}");
            }
        }

        public static void IntegrateAudioIntoScenes(bool interactive)
        {
            try
            {
                SetupAudioInMainMenu();
                SetupAudioInGameplay();

                if (interactive)
                {
                    EditorUtility.DisplayDialog("MazeSpell Audio Polish",
                        "Sistem Audio & Sound Polish berhasil disiapkan!\n\n" +
                        "1. Folder BGM, SFX, & UI telah dibuat.\n" +
                        "2. Tombol Audio Settings telah diintegrasikan ke Main Menu & Gameplay.\n" +
                        "3. Procedural Audio Synthesizer siap digunakan tanpa aset eksternal.",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AudioSetup] Gagal mengintegrasikan audio ke scene: {ex}");
            }
        }

        private static void SetupAudioInMainMenu()
        {
            if (!File.Exists(MainMenuScenePath)) return;

            var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) return;

            var canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // AudioSettingsButton dan ExitButton TIDAK boleh ada di Main Menu (hanya PLAY dan LEVEL)
            Transform existingBtn = canvas.transform.Find("AudioSettingsButton");
            if (existingBtn != null)
            {
                UnityEngine.Object.DestroyImmediate(existingBtn.gameObject);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static void UpdateMainMenuNavigation(Transform canvasTransform, Button audioBtn)
        {
            Transform playTrans = canvasTransform.Find("PlayButton");
            Transform levelTrans = canvasTransform.Find("LevelButton");
            Transform exitTrans = canvasTransform.Find("ExitButton");

            if (playTrans != null && levelTrans != null && exitTrans != null && audioBtn != null)
            {
                Button playBtn = playTrans.GetComponent<Button>();
                Button levelBtn = levelTrans.GetComponent<Button>();
                Button exitBtn = exitTrans.GetComponent<Button>();

                if (playBtn != null && levelBtn != null && exitBtn != null)
                {
                    Navigation navPlay = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = audioBtn, selectOnDown = levelBtn };
                    Navigation navLevel = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = playBtn, selectOnDown = exitBtn };
                    Navigation navExit = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = levelBtn, selectOnDown = audioBtn };
                    Navigation navAudio = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = exitBtn, selectOnDown = playBtn };

                    playBtn.navigation = navPlay;
                    levelBtn.navigation = navLevel;
                    exitBtn.navigation = navExit;
                    audioBtn.navigation = navAudio;
                }
            }
        }

        private static void SetupAudioInGameplay()
        {
            if (!File.Exists(GameplayScenePath)) return;

            var scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) return;

#if UNITY_6000_0_OR_NEWER
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>();
#elif UNITY_2023_1_OR_NEWER
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
#else
            var canvases = UnityEngine.Object.FindObjectsOfType<Canvas>();
#endif
            Transform targetCanvas = null;
            if (canvases != null && canvases.Length > 0)
            {
                for (int i = 0; i < canvases.Length; i++)
                {
                    if (canvases[i].renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        targetCanvas = canvases[i].transform;
                        break;
                    }
                }
                if (targetCanvas == null) targetCanvas = canvases[0].transform;
            }

            if (targetCanvas != null)
            {
                var existingDialog = UnityEngine.Object.FindAnyObjectByType<MazeSpell.Audio.AudioSettingsController>();
                if (existingDialog == null)
                {
                    MazeSpell.Audio.AudioSettingsController.CreateAudioSettingsPanel(targetCanvas);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[AudioSetup] Audio Settings dialog berhasil dipasang di Gameplay (SampleScene)!");
        }
    }
}
