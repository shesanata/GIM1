using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

namespace MazeSpell.Editor
{
    [InitializeOnLoad]
    public static class MazeGridSetupWindow
    {
        private struct LevelDefinition
        {
            public int levelNumber;
            public string themeName;
            public int columns;
            public int rows;
            public string pathDesc;
            public string wallDesc;

            public LevelDefinition(int lvl, string theme, int cols, int rws, string path, string wall)
            {
                levelNumber = lvl;
                themeName = theme;
                columns = cols;
                rows = rws;
                pathDesc = path;
                wallDesc = wall;
            }
        }

        private static readonly LevelDefinition[] Levels = new[]
        {
            new LevelDefinition(1, "Ground / Earth", 9, 17,
                "Packed brown dirt / soil paths. Warm natural earth tones.",
                "Rocky stone borders and rugged earth walls. Clearly distinct from paths."),
            new LevelDefinition(2, "Ground / Earth", 9, 17,
                "Packed brown dirt / soil paths. Warm natural earth tones.",
                "Rocky stone borders and rugged earth walls. Clearly distinct from paths."),
            new LevelDefinition(3, "Grass", 11, 21,
                "Vibrant lawn paths. Bright, lush, and cheerful atmosphere.",
                "Thick green hedge walls with small colorful flower accents."),
            new LevelDefinition(4, "Snow / Ice", 23, 13,
                "Bright white snow paths. Clean frosty winter atmosphere.",
                "Frozen blue ice crystal walls and frost borders. High contrast with snow."),
            new LevelDefinition(5, "Lava / Volcano", 23, 13,
                "Dark basalt stone paths. Volcanic glowing magma crack borders.",
                "Fiery red/orange obsidian walls with glowing magma crack borders.")
        };

        static MazeGridSetupWindow()
        {
            EditorApplication.delayCall += CheckAndInitialize;
        }

        private static void CheckAndInitialize()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            string testDataPath = "Assets/Tilemaps/Level1/Level1_Data.asset";
            if (!File.Exists(testDataPath))
            {
                GenerateAllLevelStructures();
            }
        }

        [MenuItem("MazeSpell/Generate Level Assets and Prefabs")]
        public static void GenerateAllLevelStructures()
        {
            EnsureDirectoriesExist();

            foreach (var def in Levels)
            {
                // 1. Buat atau update ScriptableObject Level Data
                MazeLevelData dataAsset = CreateOrUpdateLevelData(def);

                // 2. Buat Prefab Tilemap Grid untuk Level tersebut
                CreateLevelGridPrefab(def, dataAsset);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MazeSpell] Berhasil membuat dan mengonfigurasi struktur Tilemap & Level Data untuk Level 1 s/d 5!");
        }

        private static void EnsureDirectoriesExist()
        {
            string[] dirs = new[]
            {
                "Assets/Backgrounds",
                "Assets/Backgrounds/Level1",
                "Assets/Backgrounds/Level2",
                "Assets/Backgrounds/Level3",
                "Assets/Backgrounds/Level4",
                "Assets/Backgrounds/Level5",
                "Assets/Tilemaps",
                "Assets/Tilemaps/Level1",
                "Assets/Tilemaps/Level2",
                "Assets/Tilemaps/Level3",
                "Assets/Tilemaps/Level4",
                "Assets/Tilemaps/Level5",
                "Assets/Prefabs",
                "Assets/Prefabs/MazeGrids",
                "Assets/Scripts/Maze"
            };

            foreach (var dir in dirs)
            {
                if (!AssetDatabase.IsValidFolder(dir))
                {
                    string parent = Path.GetDirectoryName(dir).Replace('\\', '/');
                    string child = Path.GetFileName(dir);
                    if (string.IsNullOrEmpty(parent)) parent = "Assets";
                    AssetDatabase.CreateFolder(parent, child);
                }
            }
        }

        private static MazeLevelData CreateOrUpdateLevelData(LevelDefinition def)
        {
            string assetPath = $"Assets/Tilemaps/Level{def.levelNumber}/Level{def.levelNumber}_Data.asset";
            MazeLevelData data = AssetDatabase.LoadAssetAtPath<MazeLevelData>(assetPath);

            if (data == null)
            {
                data = ScriptableObject.CreateInstance<MazeLevelData>();
                AssetDatabase.CreateAsset(data, assetPath);
            }

            data.levelNumber = def.levelNumber;
            data.themeName = def.themeName;
            data.columns = def.columns;
            data.rows = def.rows;
            data.cellSize = Vector2.one;
            data.pathDescription = def.pathDesc;
            data.wallDescription = def.wallDesc;

            EditorUtility.SetDirty(data);
            return data;
        }

        private static void CreateLevelGridPrefab(LevelDefinition def, MazeLevelData dataAsset)
        {
            string prefabPath = $"Assets/Prefabs/MazeGrids/Level{def.levelNumber}_MazeGrid.prefab";

            // Root Grid GameObject
            GameObject rootGrid = new GameObject($"Level{def.levelNumber}_MazeGrid");
            Grid grid = rootGrid.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);
            grid.cellLayout = GridLayout.CellLayout.Rectangle;
            grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;

            MazeGridBounds bounds = rootGrid.AddComponent<MazeGridBounds>();
            bounds.SetLevelData(dataAsset);

            // 1. Child: Background_Path
            GameObject pathGo = new GameObject("Background_Path");
            pathGo.transform.SetParent(rootGrid.transform, false);
            Tilemap pathTilemap = pathGo.AddComponent<Tilemap>();
            TilemapRenderer pathRenderer = pathGo.AddComponent<TilemapRenderer>();
            pathRenderer.sortingOrder = 0;

            // 2. Child: Walls_Obstacles
            GameObject wallGo = new GameObject("Walls_Obstacles");
            wallGo.transform.SetParent(rootGrid.transform, false);
            Tilemap wallTilemap = wallGo.AddComponent<Tilemap>();
            TilemapRenderer wallRenderer = wallGo.AddComponent<TilemapRenderer>();
            wallRenderer.sortingOrder = 1;
            wallGo.AddComponent<TilemapCollider2D>();

            // 3. Child: Overlay_Decor
            GameObject decorGo = new GameObject("Overlay_Decor");
            decorGo.transform.SetParent(rootGrid.transform, false);
            Tilemap decorTilemap = decorGo.AddComponent<Tilemap>();
            TilemapRenderer decorRenderer = decorGo.AddComponent<TilemapRenderer>();
            decorRenderer.sortingOrder = 2;

            // Save as Prefab
            PrefabUtility.SaveAsPrefabAsset(rootGrid, prefabPath);
            Object.DestroyImmediate(rootGrid);
        }
    }
}
