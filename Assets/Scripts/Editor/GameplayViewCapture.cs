using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using MazeSpell;
using MazeSpell.UI;

namespace MazeSpell.Editor
{
    public static class GameplayViewCapture
    {
        [MenuItem("MazeSpell/Capture Gameplay Screenshot")]
        public static void CaptureGameplay()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);

            // Load Level 1
            var progression = LevelProgressionManager.Instance;
            if (progression != null)
            {
                progression.EnsureLevelConfigsPopulated();
                progression.LoadLevel(1);
            }

            Camera cam = Camera.main;
            if (cam == null) cam = Object.FindAnyObjectByType<Camera>();

            // Find or Create GameplayCanvas
            GameObject canvasObj = GameObject.Find("GameplayCanvas");
            if (canvasObj == null)
            {
                var hud = GameplayHUD.CreateRuntimeHUD();
                if (hud != null) canvasObj = hud.gameObject;
            }

            Canvas canvas = canvasObj != null ? canvasObj.GetComponent<Canvas>() : null;
            RenderMode originalMode = RenderMode.ScreenSpaceOverlay;
            Camera originalCam = null;
            if (canvas != null)
            {
                originalMode = canvas.renderMode;
                originalCam = canvas.worldCamera;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1;
            }

            if (cam != null)
            {
                // Focus on Player at Level 1
                cam.transform.position = new Vector3(2.5f, 7.5f, -10f);
                cam.orthographic = true;
                cam.orthographicSize = 4.8f;
            }

            int w = 1920;
            int h = 1080;
            RenderTexture rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();

            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);

            if (canvas != null)
            {
                canvas.renderMode = originalMode;
                canvas.worldCamera = originalCam;
            }

            string outPath = "c:/Users/ASUS/Mazell/gameplay_hud_screenshot.png";
            File.WriteAllBytes(outPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            Debug.Log($"[GameplayViewCapture] Gameplay screenshot saved to: {outPath}");
        }
    }
}
