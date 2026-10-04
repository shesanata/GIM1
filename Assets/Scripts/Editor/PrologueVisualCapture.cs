using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

namespace MazeSpell.Editor
{
    public static class PrologueVisualCapture
    {
        [MenuItem("MazeSpell/Capture Prologue Visuals")]
        public static void CaptureAllScenes()
        {
            string scenePath = "Assets/Scenes/Prologue.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            string outDir = Path.Combine(Application.dataPath, "..", "Logs", "Screenshots");
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("[PrologueVisualCapture] Camera.main tidak ditemukan!");
                return;
            }

            int width = 960;
            int height = 540;

            GameObject canvasObj = GameObject.Find("PrologueCanvas");
            Canvas canvas = canvasObj != null ? canvasObj.GetComponent<Canvas>() : null;

            RenderMode originalMode = RenderMode.ScreenSpaceOverlay;
            Camera originalCam = null;
            if (canvas != null)
            {
                originalMode = canvas.renderMode;
                originalCam = canvas.worldCamera;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
            }

            var blackOverlay = GameObject.Find("BlackOverlay")?.GetComponent<CanvasGroup>();
            var storyTextGroup = GameObject.Find("StoryText")?.GetComponent<CanvasGroup>();
            var storyText = GameObject.Find("StoryText")?.GetComponent<TextMeshProUGUI>();
            var titleGroup = GameObject.Find("TitleGroup")?.GetComponent<CanvasGroup>();
            var skipHint = GameObject.Find("SkipHint")?.GetComponent<TextMeshProUGUI>();

            var letterGroup = GameObject.Find("LetterGroup");
            var mazeReveal = GameObject.Find("MazeReveal");
            var enemyReveal = GameObject.Find("EnemyReveal");
            var playerReveal = GameObject.Find("PlayerReveal");
            var playerLetters = GameObject.Find("PlayerLetters");

            // Capture 1: Scene 1 (Black Screen Text)
            if (blackOverlay != null) blackOverlay.alpha = 0.95f;
            if (letterGroup != null) letterGroup.SetActive(false);
            if (mazeReveal != null) mazeReveal.SetActive(false);
            if (enemyReveal != null) enemyReveal.SetActive(false);
            if (playerReveal != null) playerReveal.SetActive(false);
            if (titleGroup != null) titleGroup.alpha = 0f;
            if (storyText != null && storyTextGroup != null)
            {
                storyText.text = "Di sebuah dunia yang dipenuhi kata-kata,\nsetiap huruf memiliki kekuatan.";
                storyTextGroup.alpha = 1f;
            }
            if (skipHint != null) skipHint.gameObject.SetActive(true);
            CaptureCamera(cam, width, height, Path.Combine(outDir, "prologue_scene1_intro.png"));

            // Capture 2: Scene 2 (Huruf-huruf Hilang)
            if (blackOverlay != null) blackOverlay.alpha = 0.35f;
            if (letterGroup != null) letterGroup.SetActive(true);
            if (storyText != null && storyTextGroup != null)
            {
                storyText.text = "Namun suatu hari,\nhuruf-huruf penting menghilang...";
                storyTextGroup.alpha = 1f;
            }
            CaptureCamera(cam, width, height, Path.Combine(outDir, "prologue_scene2_letters.png"));

            // Capture 3: Scene 3 (Labirin Misterius)
            if (blackOverlay != null) blackOverlay.alpha = 0.2f;
            if (letterGroup != null) letterGroup.SetActive(false);
            if (mazeReveal != null) mazeReveal.SetActive(true);
            if (storyText != null && storyTextGroup != null)
            {
                storyText.text = "Tanpa huruf-huruf tersebut,\nkata-kata mulai kehilangan maknanya.";
                storyTextGroup.alpha = 1f;
            }
            CaptureCamera(cam, width, height, Path.Combine(outDir, "prologue_scene3_maze.png"));

            // Capture 4: Scene 4 (Penjaga Labirin)
            if (blackOverlay != null) blackOverlay.alpha = 0.15f;
            if (enemyReveal != null) enemyReveal.SetActive(true);
            if (storyText != null && storyTextGroup != null)
            {
                storyText.text = "Setiap labirin memiliki penjaganya.";
                storyTextGroup.alpha = 1f;
            }
            CaptureCamera(cam, width, height, Path.Combine(outDir, "prologue_scene4_enemy.png"));

            // Capture 5: Scene 5 (Player)
            if (enemyReveal != null) enemyReveal.SetActive(false);
            if (playerReveal != null) playerReveal.SetActive(true);
            if (playerLetters != null) playerLetters.SetActive(true);
            if (storyText != null && storyTextGroup != null)
            {
                storyText.text = "Temukan hurufnya.\nSusun katanya.\nTaklukkan labirinnya.";
                storyTextGroup.alpha = 1f;
            }
            CaptureCamera(cam, width, height, Path.Combine(outDir, "prologue_scene5_player.png"));

            // Capture 6: Scene 6 (Title Screen)
            if (blackOverlay != null) blackOverlay.alpha = 0.75f;
            if (playerReveal != null) playerReveal.SetActive(false);
            if (mazeReveal != null) mazeReveal.SetActive(false);
            if (storyTextGroup != null) storyTextGroup.alpha = 0f;
            if (titleGroup != null) titleGroup.alpha = 1f;
            if (skipHint != null) skipHint.gameObject.SetActive(true);
            CaptureCamera(cam, width, height, Path.Combine(outDir, "prologue_scene6_title.png"));

            // Restore canvas render mode
            if (canvas != null)
            {
                canvas.renderMode = originalMode;
                canvas.worldCamera = originalCam;
            }

            // Reset scene state ke default start
            if (blackOverlay != null) blackOverlay.alpha = 1f;
            if (titleGroup != null) titleGroup.alpha = 0f;
            if (letterGroup != null) letterGroup.SetActive(false);
            if (mazeReveal != null) mazeReveal.SetActive(false);
            if (enemyReveal != null) enemyReveal.SetActive(false);
            if (playerReveal != null) playerReveal.SetActive(false);

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[PrologueVisualCapture] Seluruh screenshot adegan prologue berhasil diambil di: " + outDir);
        }

        private static void CaptureCamera(Camera cam, int width, int height, string filePath)
        {
            RenderTexture rt = new RenderTexture(width, height, 24);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);

            byte[] bytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            File.WriteAllBytes(filePath, bytes);
            Debug.Log($"[PrologueVisualCapture] Disimpan: {filePath}");
        }
    }
}
