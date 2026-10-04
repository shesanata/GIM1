using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace MazeSpell.Editor
{
    public static class MainMenuCapture
    {
        [MenuItem("MazeSpell/Capture MainMenu Screenshot")]
        public static void CaptureMenu()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
            
            // Cari Canvas dan Background
            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj != null)
            {
                // Nonaktifkan Logo teks duplikat agar logo asli di gambar terlihat jelas
                Transform logoT = canvasObj.transform.Find("Logo");
                if (logoT != null)
                {
                    logoT.gameObject.SetActive(false);
                    Debug.Log("[MainMenuCapture] Redundant text Logo disabled.");
                }

                // Nonaktifkan VignetteOverlay jika ada agar gambar jernih dan cerah
                Transform bgT = canvasObj.transform.Find("Background");
                if (bgT != null)
                {
                    Transform vigT = bgT.Find("VignetteOverlay");
                    if (vigT != null)
                    {
                        vigT.gameObject.SetActive(false);
                        Debug.Log("[MainMenuCapture] Dark VignetteOverlay disabled.");
                    }

                    Image bgImg = bgT.GetComponent<Image>();
                    if (bgImg != null)
                    {
                        bgImg.color = Color.white;
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Camera cam = Camera.main;
            if (cam == null)
            {
                cam = Object.FindAnyObjectByType<Camera>();
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

            string outPath = "c:/Users/ASUS/Mazell/mainmenu_screenshot.png";
            File.WriteAllBytes(outPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            Debug.Log($"[MainMenuCapture] Screenshot saved to: {outPath}");
        }
    }
}
