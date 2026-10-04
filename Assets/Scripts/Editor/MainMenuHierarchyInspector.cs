using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

namespace MazeSpell.Editor
{
    public static class MainMenuHierarchyInspector
    {
        [MenuItem("MazeSpell/Inspect MainMenu")]
        public static void Inspect()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
            var rootObjects = scene.GetRootGameObjects();
            Debug.Log($"=== MAIN MENU SCENE INSPECTION ({rootObjects.Length} root objects) ===");
            foreach (var root in rootObjects)
            {
                PrintHierarchy(root.transform, "");
            }
        }

        private static void PrintHierarchy(Transform t, string indent)
        {
            var btn = t.GetComponent<Button>();
            var img = t.GetComponent<Image>();
            string extra = "";
            if (btn != null) extra += " [Button]";
            if (img != null) extra += $" [Image sprite={(img.sprite != null ? img.sprite.name : "null")} color={img.color}]";
            Debug.Log($"{indent}{t.name} (Active: {t.gameObject.activeSelf}){extra}");

            for (int i = 0; i < t.childCount; i++)
            {
                PrintHierarchy(t.GetChild(i), indent + "  ");
            }
        }
    }
}
