using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MazeSpell;

namespace MazeSpell.Editor
{
    public static class EnemyDifficultyVisualCapture
    {
        private static readonly string ArtifactDir = "C:/Users/ASUS/.gemini/antigravity-ide/brain/b5f93988-9e00-4e56-ace2-431621b57dfb";

        [MenuItem("MazeSpell/Capture Enemy Difficulty Visuals")]
        public static void CaptureAllDifficultyScreenshots()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);

            // 1. Capture Level 4
            CaptureLevelView(4, Path.Combine(ArtifactDir, "level4_enemy_gameplay.png"));

            // 2. Capture Level 5 (Initial 3 hearts)
            CaptureLevelView(5, Path.Combine(ArtifactDir, "level5_enemy_gameplay_3hp.png"));

            // 3. Capture Level 5 (Hit 1 -> 2 hearts)
            CaptureLevelState(5, 2, Path.Combine(ArtifactDir, "level5_enemy_hit1_2hp.png"));

            // 4. Capture Level 5 (Hit 2 -> 1 heart)
            CaptureLevelState(5, 1, Path.Combine(ArtifactDir, "level5_enemy_hit2_1hp.png"));

            Debug.Log("[EnemyDifficultyVisualCapture] All difficulty screenshots captured successfully!");
        }

        private static void CleanupOldGrids()
        {
            for (int i = 1; i <= 5; i++)
            {
                GameObject oldGrid = GameObject.Find($"Level{i}_MazeGrid");
                if (oldGrid != null)
                {
                    Object.DestroyImmediate(oldGrid);
                }
                GameObject oldClone = GameObject.Find($"Level{i}_MazeGrid(Clone)");
                if (oldClone != null)
                {
                    Object.DestroyImmediate(oldClone);
                }
            }
            foreach (var g in Object.FindObjectsByType<Grid>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (g.name.Contains("Level") && g.name.Contains("MazeGrid"))
                {
                    Object.DestroyImmediate(g.gameObject);
                }
            }
        }

        private static void CaptureLevelView(int level, string outputPath)
        {
            CleanupOldGrids();
            var progression = LevelProgressionManager.Instance;
            if (progression != null)
            {
                progression.EnsureLevelConfigsPopulated();
                progression.LoadLevel(level);
            }

            // Focus camera near Enemy
            EnemyCharacter enemy = Object.FindAnyObjectByType<EnemyCharacter>();
            if (enemy != null)
            {
                enemy.UpdateStatsForCurrentLevel();
            }

            Camera cam = Camera.main;
            if (cam == null) cam = Object.FindAnyObjectByType<Camera>();

            if (cam != null && enemy != null)
            {
                cam.transform.position = new Vector3(enemy.transform.position.x, enemy.transform.position.y, -10f);
                cam.orthographicSize = 4.2f;
            }

            RenderToPath(cam, outputPath);
        }

        private static void CaptureLevelState(int level, int hp, string outputPath)
        {
            CleanupOldGrids();
            var progression = LevelProgressionManager.Instance;
            if (progression != null)
            {
                progression.EnsureLevelConfigsPopulated();
                progression.LoadLevel(level);
            }

            EnemyCharacter enemy = Object.FindAnyObjectByType<EnemyCharacter>();
            if (enemy != null)
            {
                // Set health state
                var field = typeof(EnemyCharacter).GetField("currentHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null) field.SetValue(enemy, hp);
                enemy.UpdateHealthIndicator();
            }

            Camera cam = Camera.main;
            if (cam == null) cam = Object.FindAnyObjectByType<Camera>();

            if (cam != null && enemy != null)
            {
                cam.transform.position = new Vector3(enemy.transform.position.x, enemy.transform.position.y, -10f);
                cam.orthographicSize = 3.6f;
            }

            RenderToPath(cam, outputPath);
        }

        private static void RenderToPath(Camera cam, string outputPath)
        {
            if (cam == null) return;

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

            File.WriteAllBytes(outputPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            Debug.Log($"[EnemyDifficultyVisualCapture] Screenshot saved: {outputPath}");
        }
    }
}
