using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace MazeSpell.Editor
{
    /// <summary>
    /// Menu item dan helper editor untuk mempermudah pemasangan LevelProgressionManager di Scene aktif.
    /// </summary>
    public static class LevelProgressionEditor
    {
        [MenuItem("MazeSpell/Setup Level Progression in Scene")]
        public static void SetupLevelProgressionInActiveScene()
        {
#if UNITY_2023_1_OR_NEWER
            var manager = Object.FindAnyObjectByType<LevelProgressionManager>();
#else
            var manager = Object.FindObjectOfType<LevelProgressionManager>();
#endif
            if (manager == null)
            {
                GameObject go = new GameObject("LevelProgressionManager");
                manager = go.AddComponent<LevelProgressionManager>();
                Undo.RegisterCreatedObjectUndo(go, "Create LevelProgressionManager");
            }

            manager.EnsureLevelConfigsPopulated();

            var currentScene = EditorSceneManager.GetActiveScene();
            if (currentScene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(currentScene);
                EditorSceneManager.SaveScene(currentScene);
            }

            Debug.Log("[MazeSpell] LevelProgressionManager berhasil disiapkan di scene dengan 5 Level!");
        }
    }
}
