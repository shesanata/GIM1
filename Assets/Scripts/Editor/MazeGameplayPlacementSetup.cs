using System;
using UnityEngine;
using UnityEditor;

namespace MazeSpell.Editor
{
    /// <summary>
    /// Tool editor untuk menempatkan seluruh objek gameplay (CollectibleLetter, HazardObstacle,
    /// SpawnPoint, dan LevelGoalTrigger) ke dalam 5 Prefab Maze Grid (Level 1 sampai 5).
    /// </summary>
    [InitializeOnLoad]
    public static class MazeGameplayPlacementSetup
    {
        static MazeGameplayPlacementSetup()
        {
            EditorApplication.delayCall += AutoSetupIfNeeded;
        }

        private static void AutoSetupIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            if (!SessionState.GetBool("MazeSpell_GameplayPlacementDone_V2", false))
            {
                SessionState.SetBool("MazeSpell_GameplayPlacementDone_V2", true);
                PopulateAllLevelPrefabs(false);
            }
        }

        [MenuItem("MazeSpell/Populate Maze Gameplay Objects")]
        public static void PopulateManual()
        {
            PopulateAllLevelPrefabs(true);
        }

        public static void PopulateAllLevelPrefabs(bool interactive)
        {
            int successCount = 0;
            var levelsData = MazeGameplayManager.AllLevelsData;

            for (int i = 0; i < levelsData.Length; i++)
            {
                var data = levelsData[i];
                string prefabPath = $"Assets/Prefabs/MazeGrids/Level{data.levelNumber}_MazeGrid.prefab";

                try
                {
                    GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
                    if (root == null)
                    {
                        Debug.LogWarning($"[MazeGameplayPlacementSetup] Prefab tidak ditemukan di {prefabPath}");
                        continue;
                    }

                    // 1. Bersihkan kontainer GameplayObjects lama jika ada
                    Transform oldContainer = root.transform.Find("GameplayObjects");
                    if (oldContainer != null)
                    {
                        UnityEngine.Object.DestroyImmediate(oldContainer.gameObject);
                    }

                    // 2. Bangun kontainer GameplayObjects baru
                    MazeGameplayManager.BuildGameplayObjectsForLevel(root.transform, data.levelNumber);

                    // 3. Simpan prefab yang telah diperbarui
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    PrefabUtility.UnloadPrefabContents(root);

                    successCount++;
                    Debug.Log($"[MazeGameplayPlacementSetup] Berhasil menyusun objek gameplay pada {prefabPath} (Huruf: {data.targetWord.Length}, Hazard: {data.hazardPositions.Length})");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MazeGameplayPlacementSetup] Gagal menyusun prefab {prefabPath}: {ex}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (interactive)
            {
                EditorUtility.DisplayDialog("Maze Gameplay Setup",
                    $"Berhasil menempatkan objek gameplay (Huruf, Hazard, Goal, Spawn) pada {successCount} level prefab!",
                    "OK");
            }
        }
    }
}
