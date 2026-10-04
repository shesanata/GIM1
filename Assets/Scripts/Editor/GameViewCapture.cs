using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace MazeSpell.Editor
{
    public static class GameViewCapture
    {
        [MenuItem("MazeSpell/Capture Game View Screenshot")]
        public static void CaptureScreenshot()
        {
            string scenePath = "Assets/Scenes/SampleScene.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Muat ulang prefab grid Level 1 ke scene agar selalu menggunakan versi prefab terbaru
            GameObject oldGrid = GameObject.Find("Level1_MazeGrid");
            if (oldGrid != null)
            {
                Object.DestroyImmediate(oldGrid);
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MazeGrids/Level1_MazeGrid.prefab");
            GameObject grid = null;
            if (prefab != null)
            {
                grid = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                grid.name = "Level1_MazeGrid";
            }

            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("[GameViewCapture] Camera.main tidak ditemukan!");
                return;
            }

            // Atur kamera agar membingkai seluruh labirin 17x11 dengan sempurna
            // Titik tengah: X = 8.0, Y = 5.0
            cam.transform.position = new Vector3(8.0f, 5.0f, -10f);
            cam.orthographic = true;
            cam.orthographicSize = 6.2f;
            cam.backgroundColor = new Color(0.56f, 0.64f, 0.36f, 1f); // Warna rumput hijau

            // Pastikan Player di posisi spawn (2.5, 1.5)
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                player.transform.position = new Vector3(2.5f, 1.5f, 0f);
            }

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
            Object.DestroyImmediate(rt);

            string outPath = "c:/Users/ASUS/Mazell/level1_gameview_screenshot.png";
            File.WriteAllBytes(outPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            // Screenshot Closeup Huruf E & A di Level 1
            cam.transform.position = new Vector3(5.0f, 6.0f, -10f);
            cam.orthographicSize = 2.2f;

            RenderTexture rtClose = new RenderTexture(600, 600, 24);
            cam.targetTexture = rtClose;
            cam.Render();

            RenderTexture.active = rtClose;
            Texture2D texClose = new Texture2D(600, 600, TextureFormat.RGB24, false);
            texClose.ReadPixels(new Rect(0, 0, 600, 600), 0, 0);
            texClose.Apply();

            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rtClose);

            string outPathClose = "c:/Users/ASUS/Mazell/letter_gameplay_closeup.png";
            File.WriteAllBytes(outPathClose, texClose.EncodeToPNG());
            Object.DestroyImmediate(texClose);

            Debug.Log($"[GameViewCapture] Screenshot Game View & Closeup berhasil disimpan!");
        }
    }
}
