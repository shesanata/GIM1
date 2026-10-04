using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MazeSpell;

namespace MazeSpell.Editor
{
    public static class LetterVisualCapture
    {
        [MenuItem("MazeSpell/Capture Letter Verification Views")]
        public static void CaptureAllViews()
        {
            string scenePath = "Assets/Scenes/SampleScene.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Bersihkan objek lama
            var existingGrids = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in existingGrids)
            {
                if (go.name.Contains("MazeGrid") || go.name.Contains("PreviewCam"))
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }

            // 1. Capture Level 1 Overview
            CaptureLevelView(1, "Assets/Prefabs/MazeGrids/Level1_MazeGrid.prefab",
                new Vector3(14.5f, 7.5f, -10f), 8.5f, 1280, 720,
                "c:/Users/ASUS/Mazell/level1_letters_overview.png");

            // 2. Capture Level 1 Closeup (Letter E at 8.5, 11.5 and surrounding blue stone path)
            CaptureLevelView(1, "Assets/Prefabs/MazeGrids/Level1_MazeGrid.prefab",
                new Vector3(8.5f, 11.5f, -10f), 2.5f, 800, 800,
                "c:/Users/ASUS/Mazell/level1_letter_E_closeup.png");

            // 3. Capture Level 5 Overview
            CaptureLevelView(5, "Assets/Prefabs/MazeGrids/Level5_MazeGrid.prefab",
                new Vector3(22.5f, 11.5f, -10f), 12.5f, 1280, 720,
                "c:/Users/ASUS/Mazell/level5_letters_overview.png");

            // 4. Capture Level 5 Closeup (Letter F at 8.5, 20.5)
            CaptureLevelView(5, "Assets/Prefabs/MazeGrids/Level5_MazeGrid.prefab",
                new Vector3(8.5f, 20.5f, -10f), 2.5f, 800, 800,
                "c:/Users/ASUS/Mazell/level5_letter_F_closeup.png");

            Debug.Log("[LetterVisualCapture] All visual capture screenshots successfully rendered!");
        }

        private static void CaptureLevelView(int level, string prefabPath, Vector3 camPos, float orthoSize, int width, int height, string outPath)
        {
            // Hapus grid yang ada
            var existingGrids = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in existingGrids)
            {
                if (go.name.Contains("MazeGrid"))
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[LetterVisualCapture] Prefab not found at {prefabPath}");
                return;
            }

            GameObject grid = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            grid.name = $"Level{level}_MazeGrid";

            // Pastikan posisi player
            Vector2 spawn = MazeLetterValidator.GetSpawnPosition(level, grid);
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                player.transform.position = new Vector3(spawn.x, spawn.y, 0f);
            }

            GameObject camObj = new GameObject("CaptureCam");
            Camera cam = camObj.AddComponent<Camera>();
            cam.transform.position = camPos;
            cam.orthographic = true;
            cam.orthographicSize = orthoSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.48f, 0.58f, 0.32f, 1f); // Darker grass tint

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
            UnityEngine.Object.DestroyImmediate(camObj);
            UnityEngine.Object.DestroyImmediate(grid);

            File.WriteAllBytes(outPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            Debug.Log($"[LetterVisualCapture] Saved view to {outPath}");
        }
    }
}
