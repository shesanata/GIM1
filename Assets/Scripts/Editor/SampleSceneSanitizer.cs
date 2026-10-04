using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MazeSpell;
using MazeSpell.UI;

namespace MazeSpell.Editor
{
    public static class SampleSceneSanitizer
    {
        [MenuItem("MazeSpell/Sanitize SampleScene")]
        public static void Sanitize()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();

            Debug.Log($"=== SANITIZING SAMPLE SCENE ({roots.Length} roots) ===");

            foreach (var root in roots)
            {
                Debug.Log($"Root: {root.name}");
                // Hapus grid lama atau canvas lama yang statically saved di scene
                if (root.name.Contains("MazeGrid") || root.name == "Canvas" || root.name == "WordProgressUI" || root.name == "Grid")
                {
                    Debug.Log($"-> Menghapus static old object: {root.name}");
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }

            // Pastikan Setup GameplayHUD
            GameplayHUDSetup.SetupHUDInScene(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("=== SAMPLE SCENE SANITIZED & SAVED ===");
        }
    }
}
