using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using MazeSpell;
using MazeSpell.Gameplay;
using TMPro;

namespace MazeSpell.Editor
{
    /// <summary>
    /// Builder komprehensif untuk membangun seluruh 5 Level Peta Rumah (Top-Down Cartoon House Maps)
    /// mengikuti visual reference Robbery Bob:
    /// - Denah gameplay & matrix 100% dipertahankan (tanpa mengubah walkthrough, cell, player spawn, enemy, letters, exit).
    /// - Dinding directional tebal bergaya cutaway rumah cartoon (cream beige + roof trim exterior + ambient shadow).
    /// - Lantai seamless (kayu oak hangat, walnut tua, karpet merah berhias emas, keramik biru kamar mandi, rumput taman).
    /// - Furniture lengkap ditempatkan rapi menempel di dinding/sudut sehingga jalur pemain tetap terbuka.
    /// - Kotak kardus untuk ngumpet [E] (HidingBox).
    /// - Musuh penjaga patroli dengan Vision Cone merah (Standby horizontal & Circuit roaming).
    /// - Pintu keluar rumah dengan green exit welcome doormat.
    /// </summary>
    [InitializeOnLoad]
    public static class HouseMapBuilder
    {
        static HouseMapBuilder()
        {
            EditorApplication.delayCall += AutoBuildIfNeeded;
        }

        private static void AutoBuildIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            if (!SessionState.GetBool("MazeSpell_HouseMap_Built_V3", false))
            {
                SessionState.SetBool("MazeSpell_HouseMap_Built_V3", true);
                BuildAllHouseLevels();
            }
        }

        [MenuItem("MazeSpell/Build All House Levels (L1 - L5)")]
        public static void BuildAllHouseLevels()
        {
            Debug.Log("=================================================");
            Debug.Log(">>> MEMBANGUN SELURUH LEVEL DENAH RUMAH (L1 - L5) <<<");
            Debug.Log("=================================================");

            // 1. Generate All Textures & Tiles
            HouseMapAssetGenerator.GenerateAllAssets();

            // 2. Build Level 1 s/d Level 5
            for (int lvl = 1; lvl <= 5; lvl++)
            {
                BuildLevel(lvl);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(">>> ★ SELESAI MEMBANGUN SELURUH 5 LEVEL DENAH RUMAH LENGKAP! ★ <<<");
        }

        public class HouseWallTileSet
        {
            public Tile wallIntH;
            public Tile wallIntV;
            public Tile wallFaceLeft;
            public Tile wallFaceRight;
            public Tile wallCornerTL;
            public Tile wallCornerTR;
            public Tile wallCornerBL;
            public Tile wallCornerBR;
            public Tile wallTJuncDown;
            public Tile wallTJuncUp;
            public Tile wallTJuncRight;
            public Tile wallTJuncLeft;
            public Tile wallCross;
            public Tile wallEndE;
            public Tile wallEndW;
            public Tile wallEndS;
            public Tile wallEndN;
            public Tile wallExtTop;
            public Tile wallExtBottom;
            public Tile wallExtLeft;
            public Tile wallExtRight;
            public Tile wallExtCornerTL;
            public Tile wallExtCornerTR;
            public Tile wallExtCornerBL;
            public Tile wallExtCornerBR;
            public Tile wallExtWindowH;
            public Tile wallSolidRoof;
            public Tile wallSolid;
            public Tile wallFenceH;
            public Tile wallFenceV;
            public Tile wallFenceCornerTL;
            public Tile wallFenceCornerBL;
            public Tile wallFenceCornerBR;

            public static HouseWallTileSet Load()
            {
                HouseWallTileSet s = new HouseWallTileSet();
                s.wallIntH = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Wall_Int_H_Tile.asset");
                s.wallIntV = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Wall_Int_V_Tile.asset");
                s.wallFaceLeft = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Wall_Face_Left_Tile.asset");
                s.wallFaceRight = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Wall_Face_Right_Tile.asset");
                s.wallCornerTL = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Wall_Corner_TL_Tile.asset");
                s.wallCornerTR = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Wall_Corner_TR_Tile.asset");
                s.wallCornerBL = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Wall_Corner_BL_Tile.asset");
                s.wallCornerBR = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Wall_Corner_BR_Tile.asset");
                s.wallTJuncDown = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Wall_TJunc_Down_Tile.asset");
                s.wallTJuncUp = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Wall_TJunc_Up_Tile.asset");
                s.wallTJuncRight = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Wall_TJunc_Right_Tile.asset");
                s.wallTJuncLeft = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Wall_TJunc_Left_Tile.asset");
                s.wallCross = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Wall_Cross_Tile.asset");
                s.wallEndE = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Wall_End_E_Tile.asset");
                s.wallEndW = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Wall_End_W_Tile.asset");
                s.wallEndS = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Wall_End_S_Tile.asset");
                s.wallEndN = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Wall_End_N_Tile.asset");
                s.wallExtTop = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Wall_Ext_Top_Tile.asset");
                s.wallExtBottom = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Wall_Ext_Bottom_Tile.asset");
                s.wallExtLeft = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Wall_Ext_Left_Tile.asset");
                s.wallExtRight = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Wall_Ext_Right_Tile.asset");
                s.wallExtCornerTL = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Wall_Ext_Corner_TL_Tile.asset");
                s.wallExtCornerTR = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Wall_Ext_Corner_TR_Tile.asset");
                s.wallExtCornerBL = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Wall_Ext_Corner_BL_Tile.asset");
                s.wallExtCornerBR = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Wall_Ext_Corner_BR_Tile.asset");
                s.wallExtWindowH = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Wall_Ext_Window_H_Tile.asset");
                s.wallSolidRoof = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Wall_Solid_Roof_Tile.asset");
                s.wallSolid = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Wall_Solid_Tile.asset");
                s.wallFenceH = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Wall_House_Fence_H_Tile.asset");
                s.wallFenceV = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Wall_House_Fence_V_Tile.asset");
                s.wallFenceCornerTL = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Wall_House_Fence_Corner_TL_Tile.asset");
                s.wallFenceCornerBL = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Wall_House_Fence_Corner_BL_Tile.asset");
                s.wallFenceCornerBR = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Wall_House_Fence_Corner_BR_Tile.asset");
                return s;
            }
        }

        public static void BuildLevel(int level)
        {
            Debug.Log($"[HouseMapBuilder] Membangun Level {level} Denah Rumah...");

            string prefabPath = $"Assets/Prefabs/MazeGrids/Level{level}_MazeGrid.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
            {
                Debug.LogError($"[HouseMapBuilder] Gagal memuat prefab di {prefabPath}!");
                return;
            }

            try
            {
                // Load Floor Tiles
                Tile floorWoodWarm = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Floor_Wood_Warm_Tile.asset");
                Tile floorBrownWood = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Floor_Brown_Wood_Tile.asset");
                Tile floorBlueBathroom = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Floor_Blue_Bathroom_Tile.asset");
                Tile floorCorridorTile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Floor_Corridor_Tile_Tile.asset");
                Tile floorYardGrass = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Floor_Yard_Grass_Tile.asset");
                Tile floorStonePath = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Floor_Stone_Path_Tile.asset");
                Tile floorCarportDark = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Floor_Carport_Dark_Tile.asset");
                Tile floorWaterPond = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/HouseMap/Floor_Water_Pond_Tile.asset");

                // Load Wall Tiles Suite
                HouseWallTileSet wallSet = HouseWallTileSet.Load();

                // Load Sprites
                Sprite rugRedOrnate = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/HouseMap/Deco_Rug_Ornate_Red_Large.png");
                Sprite rugBlueOval = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/HouseMap/Deco_Rug_Blue_Oval.png");
                Sprite rugGoldLarge = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/HouseMap/Deco_Rug_Gold_Large.png");

                Sprite bedRed = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/HouseMap/Deco_Bed_Red.png");
                Sprite bedBlue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/HouseMap/Deco_Bed_Blue.png");
                Sprite bedBrown = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/HouseMap/Deco_Bed_Brown.png");
                Sprite nightstand = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/HouseMap/Deco_Nightstand.png");
                Sprite wardrobe = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/HouseMap/Deco_Wardrobe.png");
                Sprite tvUnit = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/HouseMap/Deco_TV_Unit.png");
                Sprite sofaRed = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/HouseMap/Deco_Sofa_Red.png");
                Sprite sofaBlue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/HouseMap/Deco_Sofa_Blue.png");
                Sprite sofaGreen = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/HouseMap/Deco_Sofa_Green.png");
                Sprite armchair = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/HouseMap/Deco_Armchair.png");
                Sprite coffeeTable = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/HouseMap/Deco_CoffeeTable.png");
                Sprite diningTable = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/HouseMap/Deco_Table_Dining.png");
                Sprite kitchenCounter = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/HouseMap/Deco_Kitchen_Counter.png");
                Sprite fridge = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/HouseMap/Deco_Kitchen_Fridge.png");
                Sprite toilet = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/HouseMap/Deco_Bathroom_Toilet.png");
                Sprite tub = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/HouseMap/Deco_Bathroom_Tub.png");
                Sprite sink = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/HouseMap/Deco_Bathroom_Sink.png");
                Sprite bathMat = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/HouseMap/Deco_Bathroom_Mat.png");
                Sprite cardboardBox = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/HouseMap/Deco_Cardboard_Box.png");
                Sprite plant = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/HouseMap/Deco_Garden_Plant.png");
                Sprite treeLarge = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/HouseMap/Deco_Tree_Large.png");
                Sprite gardenBench = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/HouseMap/Deco_Garden_Bench.png");
                Sprite patioUmbrella = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/HouseMap/Deco_Patio_Umbrella.png");
                Sprite carBlue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/HouseMap/Deco_Car_Blue.png");
                Sprite carDark = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/HouseMap/Deco_Car_Dark.png");
                Sprite exitDoor = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/HouseMap/Deco_Exit_Door.png");
                Sprite doorwayH = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/HouseMap/Deco_Doorway_H.png");
                Sprite doorwayV = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/HouseMap/Deco_Doorway_V.png");
                Sprite enemySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Enemy.png");

                // Fonts & Bubble Letter Materials
                TMP_FontAsset bubbleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BubbleLetterStyleSetup.FONT_PATH);
                Material outlineMat, depthMat, faceMat;
                BubbleLetterStyleSetup.EnsureMaterialsExist(bubbleFont, out outlineMat, out depthMat, out faceMat);

                // 1. Setup Tilemaps
                Transform pathTr = root.transform.Find("Background_Path");
                Transform wallsTr = root.transform.Find("Walls_Obstacles");
                if (pathTr == null || wallsTr == null)
                {
                    Debug.LogError("[HouseMapBuilder] Background_Path atau Walls_Obstacles tidak ditemukan!");
                    return;
                }

                Tilemap pathTilemap = pathTr.GetComponent<Tilemap>();
                Tilemap wallsTilemap = wallsTr.GetComponent<Tilemap>();
                pathTilemap.ClearAllTiles();
                wallsTilemap.ClearAllTiles();

                int[,] matrix;
                int cols, rows;
                GetLevelMatrix(level, out matrix, out cols, out rows);

                for (int row = 0; row < rows; row++)
                {
                    int y = (rows - 1) - row;
                    for (int x = 0; x < cols; x++)
                    {
                        Vector3Int cellPos = new Vector3Int(x, y, 0);
                        bool isWall = matrix[row, x] == 1;

                        Tile fTile = GetFloorTileForCell(level, x, y, floorWoodWarm, floorBrownWood, floorBlueBathroom,
                            floorCorridorTile, floorYardGrass, floorStonePath, floorCarportDark, floorWaterPond);

                        pathTilemap.SetTile(cellPos, fTile);

                        if (isWall)
                        {
                            Tile wTile = GetDirectionalWallTile(level, matrix, x, y, cols, rows, wallSet);
                            wallsTilemap.SetTile(cellPos, wTile);
                        }
                        else
                        {
                            wallsTilemap.SetTile(cellPos, null);
                        }
                    }
                }

                // 2. Setup Level Data & Bounds
                MazeGridBounds bounds = root.GetComponent<MazeGridBounds>();
                string dataPath = $"Assets/Tilemaps/Level{level}/Level{level}_Data.asset";
                MazeLevelData data = AssetDatabase.LoadAssetAtPath<MazeLevelData>(dataPath);
                if (data != null)
                {
                    data.columns = cols;
                    data.rows = rows;
                    data.themeName = "Cartoon House";
                    EditorUtility.SetDirty(data);
                    if (bounds != null) bounds.SetLevelData(data);
                }

                Transform gameplayContainer = root.transform.Find("GameplayObjects");
                if (gameplayContainer == null)
                {
                    GameObject go = new GameObject("GameplayObjects");
                    go.transform.SetParent(root.transform, false);
                    gameplayContainer = go.transform;
                }

                // 3. Setup Spawn Point
                Vector3 spawnPos = GetSpawnPositionForLevel(level);
                Transform spawn = gameplayContainer.Find("SpawnPoint");
                if (spawn == null)
                {
                    GameObject spObj = new GameObject("SpawnPoint");
                    spObj.transform.SetParent(gameplayContainer, false);
                    spawn = spObj.transform;
                }
                spawn.localPosition = spawnPos;

                // 4. Setup Goal / Exit Door
                Vector3 goalPos = GetGoalPositionForLevel(level);
                Transform goal = gameplayContainer.Find("Goal");
                if (goal == null)
                {
                    GameObject gObj = new GameObject("Goal");
                    gObj.transform.SetParent(gameplayContainer, false);
                    goal = gObj.transform;
                }
                goal.localPosition = goalPos;
                BoxCollider2D goalCol = goal.GetComponent<BoxCollider2D>();
                if (goalCol == null) goalCol = goal.gameObject.AddComponent<BoxCollider2D>();
                goalCol.isTrigger = true;
                goalCol.size = new Vector2(1.4f, 1.4f);

                LevelGoalTrigger goalTrigger = goal.GetComponent<LevelGoalTrigger>();
                if (goalTrigger == null) goalTrigger = goal.gameObject.AddComponent<LevelGoalTrigger>();
                goalTrigger.ResetTrigger();

                ExitGateVisual exitGate = goal.GetComponent<ExitGateVisual>();
                if (exitGate == null) exitGate = goal.gameObject.AddComponent<ExitGateVisual>();
                exitGate.Initialize(level);

                SpriteRenderer goalSr = goal.GetComponent<SpriteRenderer>();
                if (goalSr == null) goalSr = goal.gameObject.AddComponent<SpriteRenderer>();
                if (exitDoor != null) goalSr.sprite = exitDoor;
                goalSr.sortingOrder = 3;

                // 5. Setup Furniture & Environment Decorations
                Transform decoContainer = gameplayContainer.Find("RoomDecorations");
                if (decoContainer != null) UnityEngine.Object.DestroyImmediate(decoContainer.gameObject);
                GameObject decoGo = new GameObject("RoomDecorations");
                decoGo.transform.SetParent(gameplayContainer, false);
                decoContainer = decoGo.transform;

                BuildDecorationsForLevel(level, decoContainer, rugRedOrnate, rugBlueOval, rugGoldLarge,
                    bedRed, bedBlue, bedBrown, nightstand, wardrobe, tvUnit, sofaRed, sofaBlue, sofaGreen,
                    armchair, coffeeTable, diningTable, kitchenCounter, fridge, toilet, tub, sink, bathMat,
                    cardboardBox, plant, treeLarge, gardenBench, patioUmbrella, carBlue, carDark, doorwayH, doorwayV);

                // 6. Setup Collectible Letters
                Transform lettersContainer = gameplayContainer.Find("Letters");
                if (lettersContainer != null) UnityEngine.Object.DestroyImmediate(lettersContainer.gameObject);
                GameObject lGo = new GameObject("Letters");
                lGo.transform.SetParent(gameplayContainer, false);
                lettersContainer = lGo.transform;

                var canonical = LetterPlacementSetup.CanonicalLetterPositions[level];
                string[] letterKeys = GetLetterKeysForLevel(level);

                for (int i = 0; i < letterKeys.Length; i++)
                {
                    string key = letterKeys[i];
                    Vector3 pos = canonical[key];
                    char ch = key.StartsWith("S_") ? 'S' : key[0];

                    string nodeName = (key == "S_1" || key == "S_2") ? $"Letter_S_{i - 2}" : $"Letter_{ch}";
                    GameObject lObj = new GameObject(nodeName);
                    lObj.transform.SetParent(lettersContainer, false);
                    lObj.transform.localPosition = pos;

                    // Setup Bubble Letter Visual
                    BubbleLetterStyleSetup.SetupLetterVisualHierarchy(lObj, ch, bubbleFont, outlineMat, depthMat, faceMat);

                    CircleCollider2D col = lObj.GetComponent<CircleCollider2D>();
                    if (col == null) col = lObj.AddComponent<CircleCollider2D>();
                    col.isTrigger = true;
                    col.radius = 0.45f;

                    CollectibleLetter cl = lObj.GetComponent<CollectibleLetter>();
                    if (cl == null) cl = lObj.AddComponent<CollectibleLetter>();
                    cl.SetLetter(ch);
                }

                // 7. Setup Multi-Enemies sesuai Level
                Transform enemiesContainer = gameplayContainer.Find("Enemies");
                if (enemiesContainer != null) UnityEngine.Object.DestroyImmediate(enemiesContainer.gameObject);
                GameObject newEnemiesGo = new GameObject("Enemies");
                newEnemiesGo.transform.SetParent(gameplayContainer, false);

                BuildEnemiesForLevel(level, newEnemiesGo.transform, enemySprite);

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log($"[HouseMapBuilder] Level {level} berhasil disimpan ke {prefabPath}!");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        #region Level Specific Matrix, Floors & Directional Walls

        private static void GetLevelMatrix(int level, out int[,] matrix, out int cols, out int rows)
        {
            switch (level)
            {
                case 1: matrix = MazeMatrixRegistry.Level1HouseMatrix; cols = 20; rows = 16; break;
                case 2: matrix = MazeMatrixRegistry.Level2HouseMatrix; cols = 18; rows = 16; break;
                case 3: matrix = MazeMatrixRegistry.Level3HouseMatrix; cols = 20; rows = 18; break;
                case 4: matrix = MazeMatrixRegistry.Level4HouseMatrix; cols = 20; rows = 16; break;
                case 5: matrix = MazeMatrixRegistry.Level5HouseMatrix; cols = 22; rows = 16; break;
                default: matrix = MazeMatrixRegistry.Level1HouseMatrix; cols = 20; rows = 16; break;
            }
        }

        private static Tile GetFloorTileForCell(int level, int x, int y,
            Tile woodWarm, Tile brownWood, Tile blueBath, Tile corridorTile,
            Tile yardGrass, Tile stonePath, Tile carportDark, Tile waterPond)
        {
            switch (level)
            {
                case 1: // EARTH (20x16)
                    if (x <= 1 || y <= 3 || (x <= 5 && y <= 8))
                    {
                        if (x == 2 && y >= 3 && y <= 8) return stonePath;
                        return yardGrass;
                    }
                    if (x >= 2 && x <= 6 && y >= 9 && y <= 14) return woodWarm; // Bedroom E
                    if (x >= 7 && x <= 12 && y >= 10 && y <= 14) return corridorTile; // Kitchen A
                    if (x >= 13 && x <= 18 && y >= 10 && y <= 14) return blueBath; // Bath
                    if (x >= 7 && x <= 12 && y >= 4 && y <= 9) return woodWarm; // Living R
                    if (x >= 13 && x <= 18 && y >= 7 && y <= 9) return woodWarm; // Dining T
                    if (x >= 13 && x <= 18 && y >= 4 && y <= 6) return brownWood; // Study H
                    return corridorTile;

                case 2: // STONE (18x16)
                    if (y <= 1 || x <= 1 || x >= 16) return corridorTile; // Sidewalk
                    if (x >= 2 && x <= 7 && y >= 10 && y <= 14) return brownWood; // Bed S
                    if (x >= 10 && x <= 15 && y >= 10 && y <= 14) return woodWarm; // Dining T
                    if (x >= 2 && x <= 7 && y >= 5 && y <= 9) return woodWarm; // Living O
                    if (x >= 10 && x <= 15 && y >= 5 && y <= 9) return brownWood; // Study N
                    if (x >= 2 && x <= 7 && y >= 2 && y <= 4) return woodWarm; // Lounge E
                    return corridorTile;

                case 3: // GRASS (20x18)
                    if (x <= 2 || y <= 3 || (x <= 6 && y <= 6))
                    {
                        if (x == 11 && y >= 1 && y <= 3) return stonePath;
                        if (x >= 17 && y >= 1 && y <= 2) return waterPond; // Kolam ikan sudut kanan bawah
                        return yardGrass;
                    }
                    if (x >= 2 && x <= 7 && y >= 12 && y <= 16) return woodWarm; // Bed G
                    if (x >= 8 && x <= 11 && y >= 13 && y <= 16) return blueBath; // Bath
                    if (x >= 12 && x <= 17 && y >= 12 && y <= 16) return brownWood; // Bed R
                    if (x >= 2 && x <= 7 && y >= 7 && y <= 11) return corridorTile; // Kitchen A
                    if (x >= 12 && x <= 17 && y >= 7 && y <= 11) return woodWarm; // Dining S1
                    if (x >= 12 && x <= 17 && y >= 2 && y <= 6) return brownWood; // Bed S2
                    return corridorTile;

                case 4: // FROST (20x16)
                    if (x <= 6 && y <= 3) return carportDark; // Carport kiri bawah
                    if (x >= 7 && y <= 2) return yardGrass; // Halaman depan
                    if (x >= 2 && x <= 7 && y >= 10 && y <= 14) return brownWood; // Master F
                    if (x >= 8 && x <= 11 && y >= 10 && y <= 14) return blueBath; // Bath R
                    if (x >= 12 && x <= 17 && y >= 10 && y <= 14) return woodWarm; // Bed O
                    if (x >= 2 && x <= 7 && y >= 4 && y <= 9) return woodWarm; // Living S
                    if (x >= 10 && x <= 17 && y >= 3 && y <= 7) return woodWarm; // Dining T
                    return corridorTile;

                case 5: // FLAME (22x16)
                    if (x <= 7 && y <= 3) return carportDark; // Carport kiri bawah
                    if (x >= 8 && y <= 2) return corridorTile; // Teras depan
                    if (x >= 2 && x <= 7 && y >= 10 && y <= 14) return brownWood; // Bed F
                    if (x >= 8 && x <= 12 && y >= 10 && y <= 14) return woodWarm; // Study L
                    if (x >= 13 && x <= 19 && y >= 10 && y <= 14) return corridorTile; // Kitchen A
                    if (x >= 2 && x <= 8 && y >= 4 && y <= 9) return woodWarm; // Lounge M
                    if (x >= 12 && x <= 19 && y >= 3 && y <= 7) return woodWarm; // Living E
                    return corridorTile;

                default:
                    return woodWarm;
            }
        }

        private static Tile GetDirectionalWallTile(int level, int[,] matrix, int x, int y, int cols, int rows, HouseWallTileSet wallSet)
        {
            int row = (rows - 1) - y;
            int col = x;

            // 1. Garden boundary fence (L1 & L3 perimeter)
            if (level == 1)
            {
                if (col == 0 && row == 15) return wallSet.wallFenceCornerBL ?? wallSet.wallFenceH;
                if (col == cols - 1 && row == 15) return wallSet.wallFenceCornerBR ?? wallSet.wallFenceH;
                if (row == 15) return wallSet.wallFenceH;
                if (col == 0 && row >= 7) return wallSet.wallFenceV;
            }
            else if (level == 3)
            {
                if (col == 0 && row == 17) return wallSet.wallFenceCornerBL ?? wallSet.wallFenceH;
                if (col == cols - 1 && row == 17) return wallSet.wallFenceCornerBR ?? wallSet.wallFenceH;
                if (row == 17) return wallSet.wallFenceH;
                if (col == 0 && row >= 9) return wallSet.wallFenceV;
            }

            // 2. Row 0 is the top exterior roof ridge
            if (row == 0)
            {
                if (col == 0) return wallSet.wallExtCornerTL ?? wallSet.wallSolidRoof;
                if (col == cols - 1) return wallSet.wallExtCornerTR ?? wallSet.wallSolidRoof;
                return wallSet.wallSolidRoof ?? wallSet.wallExtTop;
            }

            // 3. Row 1 is the house top exterior wall
            if (row == 1)
            {
                if (col == 0) return wallSet.wallExtCornerTL;
                if (col == cols - 1) return wallSet.wallExtCornerTR;
                if (col == 4 || col == 10 || col == 16) return wallSet.wallExtWindowH;
                return wallSet.wallExtTop;
            }

            // 4. Raw 4-way neighbors
            bool N = (row > 0 && matrix[row - 1, col] == 1);
            bool S = (row < rows - 1 && matrix[row + 1, col] == 1);
            bool W = (col > 0 && matrix[row, col - 1] == 1);
            bool E = (col < cols - 1 && matrix[row, col + 1] == 1);

            // 5. Exterior boundaries
            if (col == 0)
            {
                if (row == rows - 1) return wallSet.wallExtCornerBL;
                return wallSet.wallExtLeft;
            }
            if (col == cols - 1)
            {
                if (row == rows - 1) return wallSet.wallExtCornerBR;
                return wallSet.wallExtRight;
            }
            if (row == rows - 1 && !S)
            {
                return wallSet.wallExtBottom;
            }

            // Level 1 exterior house wall facing garden (col = 6, row 7..11)
            if (level == 1 && col == 6 && row >= 7 && row <= 11)
            {
                if (row == 11) return wallSet.wallCornerTL;
                if (!N && S) return wallSet.wallEndN;
                return wallSet.wallExtLeft;
            }

            // 6. Filter out parallel solid wall columns / rows (prevents spurious T-junction stripes on 2-tile thick walls)
            bool isSolidColWest = (col > 0 && matrix[row, col - 1] == 1 &&
                (row == 0 || matrix[row - 1, col - 1] == 1) &&
                (row == rows - 1 || matrix[row + 1, col - 1] == 1) &&
                N && S);

            bool isSolidColEast = (col < cols - 1 && matrix[row, col + 1] == 1 &&
                (row == 0 || matrix[row - 1, col + 1] == 1) &&
                (row == rows - 1 || matrix[row + 1, col + 1] == 1) &&
                N && S);

            bool isSolidRowNorth = (row > 0 && matrix[row - 1, col] == 1 &&
                (col == 0 || matrix[row - 1, col - 1] == 1) &&
                (col == cols - 1 || matrix[row - 1, col + 1] == 1) &&
                W && E);

            bool isSolidRowSouth = (row < rows - 1 && matrix[row + 1, col] == 1 &&
                (col == 0 || matrix[row + 1, col - 1] == 1) &&
                (col == cols - 1 || matrix[row + 1, col + 1] == 1) &&
                W && E);

            bool bN = N && !isSolidRowNorth;
            bool bS = S && !isSolidRowSouth;
            bool bW = W && !isSolidColWest;
            bool bE = E && !isSolidColEast;

            // 7. Connectivity Autotiling
            if (bN && bS && bW && bE) return wallSet.wallCross;
            if (bW && bE && bS && !bN) return wallSet.wallTJuncDown;
            if (bW && bE && bN && !bS) return wallSet.wallTJuncUp;
            if (bN && bS && bE && !bW) return wallSet.wallTJuncRight;
            if (bN && bS && bW && !bE) return wallSet.wallTJuncLeft;

            if (bW && bE && !bN && !bS) return wallSet.wallIntH;
            if (bN && bS && !bW && !bE)
            {
                if (col > 0 && matrix[row, col - 1] == 1 && (col == cols - 1 || matrix[row, col + 1] == 0))
                    return wallSet.wallFaceRight ?? wallSet.wallIntV;
                if (col < cols - 1 && matrix[row, col + 1] == 1 && (col == 0 || matrix[row, col - 1] == 0))
                    return wallSet.wallFaceLeft ?? wallSet.wallIntV;
                return wallSet.wallIntV;
            }

            if (bS && bE && !bN && !bW) return wallSet.wallCornerTL;
            if (bS && bW && !bN && !bE) return wallSet.wallCornerTR;
            if (bN && bE && !bS && !bW) return wallSet.wallCornerBL;
            if (bN && bW && !bS && !bE) return wallSet.wallCornerBR;

            if (bW && !bE && !bN && !bS) return wallSet.wallEndE; // Doorway on East
            if (bE && !bW && !bN && !bS) return wallSet.wallEndW; // Doorway on West
            if (bN && !bS && !bW && !bE) return wallSet.wallEndS; // Doorway on South
            if (bS && !bN && !bW && !bE) return wallSet.wallEndN; // Doorway on North

            // Fallback for solid sections
            if (N && S)
            {
                if (col > 0 && matrix[row, col - 1] == 1 && (col == cols - 1 || matrix[row, col + 1] == 0))
                    return wallSet.wallFaceRight ?? wallSet.wallIntV;
                if (col < cols - 1 && matrix[row, col + 1] == 1 && (col == 0 || matrix[row, col - 1] == 0))
                    return wallSet.wallFaceLeft ?? wallSet.wallIntV;
                return wallSet.wallIntV;
            }
            if (W && E) return wallSet.wallIntH;
            return wallSet.wallSolid;
        }

        private static Vector3 GetSpawnPositionForLevel(int level)
        {
            switch (level)
            {
                case 1: return new Vector3(2.5f, 7.5f, 0f);
                case 2: return new Vector3(9.5f, 1.5f, 0f);
                case 3: return new Vector3(11.5f, 3.5f, 0f);
                case 4: return new Vector3(7.5f, 2.5f, 0f);
                case 5: return new Vector3(9.5f, 2.5f, 0f);
                default: return new Vector3(2.5f, 2.5f, 0f);
            }
        }

        private static Vector3 GetGoalPositionForLevel(int level)
        {
            switch (level)
            {
                case 1: return new Vector3(18.5f, 4.5f, 0f);
                case 2: return new Vector3(16.5f, 3.5f, 0f);
                case 3: return new Vector3(17.5f, 6.5f, 0f);
                case 4: return new Vector3(17.5f, 4.5f, 0f);
                case 5: return new Vector3(20.5f, 4.5f, 0f);
                default: return new Vector3(16.5f, 4.5f, 0f);
            }
        }

        private static string[] GetLetterKeysForLevel(int level)
        {
            switch (level)
            {
                case 1: return new string[] { "E", "A", "R", "T", "H" };
                case 2: return new string[] { "S", "T", "O", "N", "E" };
                case 3: return new string[] { "G", "R", "A", "S_1", "S_2" };
                case 4: return new string[] { "F", "R", "O", "S", "T" };
                case 5: return new string[] { "F", "L", "A", "M", "E" };
                default: return new string[] { "E", "A", "R", "T", "H" };
            }
        }

        #endregion

        #region Comprehensive Room Decoration Builder

        private static void BuildDecorationsForLevel(int level, Transform container,
            Sprite rugRedOrnate, Sprite rugBlueOval, Sprite rugGoldLarge,
            Sprite bedRed, Sprite bedBlue, Sprite bedBrown, Sprite nightstand, Sprite wardrobe,
            Sprite tvUnit, Sprite sofaRed, Sprite sofaBlue, Sprite sofaGreen, Sprite armchair,
            Sprite coffeeTable, Sprite diningTable, Sprite kitchenCounter, Sprite fridge,
            Sprite toilet, Sprite tub, Sprite sink, Sprite bathMat, Sprite cardboardBox,
            Sprite plant, Sprite treeLarge, Sprite gardenBench, Sprite patioUmbrella,
            Sprite carBlue, Sprite carDark, Sprite doorwayH, Sprite doorwayV)
        {
            switch (level)
            {
                case 1: // EARTH (Level 1)
                    // ==============================
                    // 1. Bedroom E (Kamar Tidur Kiri Atas)
                    // ==============================
                    CreateDeco(container, "Rug_Bed_E", rugRedOrnate, new Vector3(4.0f, 12.0f, 0.05f), new Vector3(1.2f, 1.2f, 1f), sortingOrder: 1);
                    CreateDeco(container, "Bed_E", bedRed, new Vector3(3.5f, 12.8f, 0.05f), new Vector3(1.1f, 1.1f, 1f), sortingOrder: 2);
                    CreateDeco(container, "Nightstand_E", nightstand, new Vector3(5.4f, 13.2f, 0.05f), new Vector3(0.9f, 0.9f, 1f), sortingOrder: 2);
                    CreateDeco(container, "Wardrobe_E", wardrobe, new Vector3(3.5f, 9.8f, 0.05f), new Vector3(1f, 1f, 1f), sortingOrder: 2);

                    // Doorway Thresholds
                    CreateDeco(container, "Door_Garden_Living", doorwayV, new Vector3(6.0f, 7.0f, 0.02f), Vector3.one, sortingOrder: 1);
                    CreateDeco(container, "Door_Bed_E", doorwayV, new Vector3(6.0f, 10.0f, 0.02f), Vector3.one, sortingOrder: 1);
                    CreateDeco(container, "Door_Bathroom", doorwayV, new Vector3(16.0f, 10.0f, 0.02f), Vector3.one, sortingOrder: 1);
                    CreateDeco(container, "Door_Dining_T", doorwayH, new Vector3(13.0f, 8.0f, 0.02f), Vector3.one, sortingOrder: 1);
                    CreateDeco(container, "Door_Study_H_Top", doorwayV, new Vector3(16.0f, 7.0f, 0.02f), Vector3.one, sortingOrder: 1);
                    CreateDeco(container, "Door_Study_H_Left", doorwayH, new Vector3(13.0f, 6.0f, 0.02f), Vector3.one, sortingOrder: 1);

                    // ==============================
                    // 2. Kitchen A (Dapur Tengah Atas)
                    // ==============================
                    CreateDeco(container, "Kitchen_A", kitchenCounter, new Vector3(10.0f, 13.3f, 0.05f), new Vector3(1.1f, 1.1f, 1f), sortingOrder: 2);
                    CreateDeco(container, "Fridge_A", fridge, new Vector3(7.6f, 13.3f, 0.05f), new Vector3(0.9f, 0.9f, 1f), sortingOrder: 2);

                    // ==============================
                    // 3. Bathroom (Kamar Mandi Kanan Atas)
                    // ==============================
                    CreateDeco(container, "Tub_Bath", tub, new Vector3(15.2f, 13.2f, 0.05f), new Vector3(1.05f, 1.05f, 1f), sortingOrder: 2);
                    CreateDeco(container, "Toilet_Bath", toilet, new Vector3(17.4f, 13.2f, 0.05f), new Vector3(0.9f, 0.9f, 1f), sortingOrder: 2);
                    CreateDeco(container, "Sink_Bath", sink, new Vector3(14.0f, 11.2f, 0.05f), new Vector3(0.85f, 0.85f, 1f), sortingOrder: 2);
                    CreateDeco(container, "Mat_Bath", bathMat, new Vector3(16.0f, 11.2f, 0.05f), new Vector3(0.9f, 0.9f, 1f), sortingOrder: 1);

                    // ==============================
                    // 4. Living Room R (Ruang Tamu Utama Tengah)
                    // ==============================
                    CreateDeco(container, "Rug_Living_R", rugRedOrnate, new Vector3(9.5f, 6.2f, 0.05f), new Vector3(1.8f, 1.8f, 1f), sortingOrder: 1);
                    CreateDeco(container, "Sofa_R_1", sofaRed, new Vector3(9.5f, 8.2f, 0.05f), new Vector3(1.2f, 1.1f, 1f), sortingOrder: 2);
                    CreateDeco(container, "Sofa_R_2", sofaRed, new Vector3(9.5f, 4.8f, 0.05f), new Vector3(1.2f, 1.1f, 1f), sortingOrder: 2);
                    CreateDeco(container, "CoffeeTable_R", coffeeTable, new Vector3(7.6f, 6.5f, 0.05f), new Vector3(0.85f, 0.85f, 1f), sortingOrder: 2);
                    CreateDeco(container, "TV_R", tvUnit, new Vector3(7.4f, 4.8f, 0.05f), new Vector3(0.95f, 0.95f, 1f), sortingOrder: 2);

                    // ==============================
                    // 5. Dining Room T (Ruang Makan Kanan Tengah)
                    // ==============================
                    CreateDeco(container, "Rug_Dining_T", rugGoldLarge, new Vector3(15.5f, 8.0f, 0.05f), new Vector3(1.3f, 1.3f, 1f), sortingOrder: 1);
                    CreateDeco(container, "Table_T", diningTable, new Vector3(15.5f, 8.0f, 0.05f), new Vector3(1.1f, 1.1f, 1f), sortingOrder: 2);

                    // ==============================
                    // 6. Study / Lobby H (Kanan Bawah)
                    // ==============================
                    CreateDeco(container, "Rug_Study_H", rugBlueOval, new Vector3(15.5f, 5.0f, 0.05f), new Vector3(1.2f, 1.2f, 1f), sortingOrder: 1);
                    CreateDeco(container, "Plant_H", plant, new Vector3(17.4f, 5.6f, 0.05f), new Vector3(0.85f, 0.85f, 1f), sortingOrder: 2);

                    // ==============================
                    // 7. Outdoor Garden & Yard (Halaman Kiri)
                    // ==============================
                    CreateDeco(container, "Tree_Garden_1", treeLarge, new Vector3(1.5f, 7.5f, 0.05f), new Vector3(1.3f, 1.3f, 1f), sortingOrder: 3);
                    CreateDeco(container, "Tree_Garden_2", treeLarge, new Vector3(1.5f, 1.8f, 0.05f), new Vector3(1.2f, 1.2f, 1f), sortingOrder: 3);
                    CreateDeco(container, "Bench_Garden", gardenBench, new Vector3(4.5f, 4.2f, 0.05f), new Vector3(1.1f, 1.1f, 1f), sortingOrder: 2);
                    CreateDeco(container, "Plant_Garden", plant, new Vector3(2.2f, 3.8f, 0.05f), new Vector3(0.9f, 0.9f, 1f), sortingOrder: 2);

                    // ==============================
                    // 8. Hiding Boxes [E]
                    // ==============================
                    CreateHidingBox(container, "HidingBox_Kitchen", cardboardBox, new Vector3(12.4f, 10.5f, 0f));
                    CreateHidingBox(container, "HidingBox_Living", cardboardBox, new Vector3(12.4f, 5.2f, 0f));
                    break;

                case 2: // STONE (Level 2)
                    // Master Bed S
                    CreateDeco(container, "Rug_Bed_S", rugBlueOval, new Vector3(4.5f, 12.2f, 0.05f), new Vector3(1.4f, 1.4f, 1f), sortingOrder: 1);
                    CreateDeco(container, "Bed_S", bedBlue, new Vector3(3.5f, 12.8f, 0.05f), new Vector3(1.1f, 1.1f, 1f), sortingOrder: 2);
                    CreateDeco(container, "Nightstand_S", nightstand, new Vector3(5.8f, 13.2f, 0.05f), new Vector3(0.9f, 0.9f, 1f), sortingOrder: 2);
                    CreateDeco(container, "Wardrobe_S", wardrobe, new Vector3(3.5f, 10.2f, 0.05f), new Vector3(1f, 1f, 1f), sortingOrder: 2);

                    // Dining T
                    CreateDeco(container, "Rug_Dining_T", rugGoldLarge, new Vector3(13.0f, 12.2f, 0.05f), new Vector3(1.3f, 1.3f, 1f), sortingOrder: 1);
                    CreateDeco(container, "Table_T", diningTable, new Vector3(13.0f, 12.2f, 0.05f), new Vector3(1.1f, 1.1f, 1f), sortingOrder: 2);
                    CreateDeco(container, "Plant_T", plant, new Vector3(15.2f, 13.2f, 0.05f), new Vector3(0.9f, 0.9f, 1f), sortingOrder: 2);

                    // Living O
                    CreateDeco(container, "Rug_Living_O", rugBlueOval, new Vector3(4.5f, 7.5f, 0.05f), new Vector3(1.4f, 1.4f, 1f), sortingOrder: 1);
                    CreateDeco(container, "Sofa_O", sofaBlue, new Vector3(3.5f, 7.5f, 0.05f), new Vector3(1.1f, 1.1f, 1f), sortingOrder: 2);
                    CreateDeco(container, "TV_O", tvUnit, new Vector3(6.5f, 7.5f, 0.05f), new Vector3(0.9f, 0.9f, 1f), sortingOrder: 2);

                    // Study N
                    CreateDeco(container, "Rug_Study_N", rugRedOrnate, new Vector3(13.0f, 7.5f, 0.05f), new Vector3(1.2f, 1.2f, 1f), sortingOrder: 1);
                    CreateDeco(container, "Wardrobe_N", wardrobe, new Vector3(14.5f, 8.8f, 0.05f), new Vector3(1f, 1f, 1f), sortingOrder: 2);

                    // Lounge E
                    CreateDeco(container, "Rug_Lounge_E", rugGoldLarge, new Vector3(4.5f, 2.5f, 0.05f), new Vector3(1.2f, 1.2f, 1f), sortingOrder: 1);
                    CreateDeco(container, "Sofa_E", sofaGreen, new Vector3(3.5f, 2.5f, 0.05f), new Vector3(1.1f, 1.1f, 1f), sortingOrder: 2);
                    CreateDeco(container, "CoffeeTable_E", coffeeTable, new Vector3(5.5f, 2.5f, 0.05f), new Vector3(0.85f, 0.85f, 1f), sortingOrder: 2);

                    // Doorway Thresholds
                    CreateDeco(container, "Door_Bed_S", doorwayH, new Vector3(8.0f, 11.0f, 0.02f), Vector3.one, sortingOrder: 1);
                    CreateDeco(container, "Door_Dining_T", doorwayH, new Vector3(11.0f, 11.0f, 0.02f), Vector3.one, sortingOrder: 1);
                    CreateDeco(container, "Door_Living_O", doorwayH, new Vector3(8.0f, 7.0f, 0.02f), Vector3.one, sortingOrder: 1);
                    CreateDeco(container, "Door_Study_N", doorwayH, new Vector3(11.0f, 7.0f, 0.02f), Vector3.one, sortingOrder: 1);
                    CreateDeco(container, "Door_Lounge_E", doorwayH, new Vector3(8.0f, 4.0f, 0.02f), Vector3.one, sortingOrder: 1);

                    // Hiding Boxes
                    CreateHidingBox(container, "HidingBox_Hallway", cardboardBox, new Vector3(8.5f, 7.5f, 0f));
                    CreateHidingBox(container, "HidingBox_Study", cardboardBox, new Vector3(14.2f, 2.5f, 0f));
                    break;

                case 3: // GRASS (Level 3)
                    // Bed G
                    CreateDeco(container, "Rug_Bed_G", rugGoldLarge, new Vector3(4.5f, 14.2f, 0.05f), new Vector3(1.3f, 1.3f, 1f), sortingOrder: 1);
                    CreateDeco(container, "Bed_G", bedBlue, new Vector3(3.5f, 14.8f, 0.05f), new Vector3(1.1f, 1.1f, 1f), sortingOrder: 2);
                    CreateDeco(container, "Wardrobe_G", wardrobe, new Vector3(6.5f, 14.8f, 0.05f), new Vector3(0.9f, 0.9f, 1f), sortingOrder: 2);

                    // Bathroom
                    CreateDeco(container, "Tub_Bath", tub, new Vector3(8.6f, 15.2f, 0.05f), new Vector3(1f, 1f, 1f), sortingOrder: 2);
                    CreateDeco(container, "Toilet_Bath", toilet, new Vector3(10.2f, 15.2f, 0.05f), new Vector3(0.9f, 0.9f, 1f), sortingOrder: 2);
                    CreateDeco(container, "Sink_Bath", sink, new Vector3(9.5f, 13.2f, 0.05f), new Vector3(0.85f, 0.85f, 1f), sortingOrder: 2);

                    // Bed R
                    CreateDeco(container, "Rug_Bed_R", rugRedOrnate, new Vector3(14.5f, 14.2f, 0.05f), new Vector3(1.3f, 1.3f, 1f), sortingOrder: 1);
                    CreateDeco(container, "Bed_R", bedRed, new Vector3(14.5f, 14.8f, 0.05f), new Vector3(1.1f, 1.1f, 1f), sortingOrder: 2);

                    // Kitchen A
                    CreateDeco(container, "Kitchen_A", kitchenCounter, new Vector3(4.2f, 10.8f, 0.05f), new Vector3(1f, 1f, 1f), sortingOrder: 2);
                    CreateDeco(container, "Fridge_A", fridge, new Vector3(6.5f, 10.8f, 0.05f), new Vector3(0.9f, 0.9f, 1f), sortingOrder: 2);

                    // Dining S1
                    CreateDeco(container, "Rug_Dining_S1", rugGoldLarge, new Vector3(14.5f, 8.5f, 0.05f), new Vector3(1.3f, 1.3f, 1f), sortingOrder: 1);
                    CreateDeco(container, "Table_S1", diningTable, new Vector3(14.5f, 8.5f, 0.05f), new Vector3(1.1f, 1.1f, 1f), sortingOrder: 2);

                    // Bed S2
                    CreateDeco(container, "Rug_Bed_S2", rugRedOrnate, new Vector3(14.5f, 3.5f, 0.05f), new Vector3(1.3f, 1.3f, 1f), sortingOrder: 1);
                    CreateDeco(container, "Bed_S2", bedRed, new Vector3(14.5f, 3.8f, 0.05f), new Vector3(1.1f, 1.1f, 1f), sortingOrder: 2);

                    // Outdoor Garden (Level 3 - Patio Umbrella & Tree)
                    CreateDeco(container, "Patio_Umbrella", patioUmbrella, new Vector3(4.2f, 4.2f, 0.05f), new Vector3(1.3f, 1.3f, 1f), sortingOrder: 3);
                    CreateDeco(container, "Tree_Garden_1", treeLarge, new Vector3(1.8f, 6.8f, 0.05f), new Vector3(1.2f, 1.2f, 1f), sortingOrder: 3);
                    CreateDeco(container, "Plant_Garden", plant, new Vector3(6.2f, 2.5f, 0.05f), new Vector3(0.9f, 0.9f, 1f), sortingOrder: 2);

                    // Doorway Thresholds
                    CreateDeco(container, "Door_Bed_G", doorwayH, new Vector3(7.0f, 13.0f, 0.02f), Vector3.one, sortingOrder: 1);
                    CreateDeco(container, "Door_Bath", doorwayH, new Vector3(9.0f, 13.0f, 0.02f), Vector3.one, sortingOrder: 1);
                    CreateDeco(container, "Door_Bed_R", doorwayH, new Vector3(12.0f, 13.0f, 0.02f), Vector3.one, sortingOrder: 1);
                    CreateDeco(container, "Door_Kitchen_A", doorwayH, new Vector3(7.0f, 9.0f, 0.02f), Vector3.one, sortingOrder: 1);
                    CreateDeco(container, "Door_Dining_S1", doorwayH, new Vector3(12.0f, 9.0f, 0.02f), Vector3.one, sortingOrder: 1);
                    CreateDeco(container, "Door_Bed_S2", doorwayH, new Vector3(12.0f, 5.0f, 0.02f), Vector3.one, sortingOrder: 1);

                    // Hiding Boxes
                    CreateHidingBox(container, "HidingBox_Hall_1", cardboardBox, new Vector3(9.5f, 11.5f, 0f));
                    CreateHidingBox(container, "HidingBox_Hall_2", cardboardBox, new Vector3(9.5f, 5.5f, 0f));
                    break;

                case 4: // FROST (Level 4)
                    // Master Bed F
                    CreateDeco(container, "Rug_Bed_F", rugBlueOval, new Vector3(4.5f, 12.2f, 0.05f), new Vector3(1.3f, 1.3f, 1f), sortingOrder: 1);
                    CreateDeco(container, "Bed_F", bedBlue, new Vector3(3.5f, 12.8f, 0.05f), new Vector3(1.1f, 1.1f, 1f), sortingOrder: 2);
                    CreateDeco(container, "Nightstand_F", nightstand, new Vector3(5.8f, 13.2f, 0.05f), new Vector3(0.9f, 0.9f, 1f), sortingOrder: 2);

                    // Bath R
                    CreateDeco(container, "Tub_Bath", tub, new Vector3(9.5f, 12.8f, 0.05f), new Vector3(1.05f, 1.05f, 1f), sortingOrder: 2);
                    CreateDeco(container, "Toilet_Bath", toilet, new Vector3(7.6f, 13.2f, 0.05f), new Vector3(0.9f, 0.9f, 1f), sortingOrder: 2);
                    CreateDeco(container, "Sink_Bath", sink, new Vector3(8.5f, 10.8f, 0.05f), new Vector3(0.85f, 0.85f, 1f), sortingOrder: 2);

                    // Bed O
                    CreateDeco(container, "Rug_Bed_O", rugBlueOval, new Vector3(15.0f, 12.2f, 0.05f), new Vector3(1.3f, 1.3f, 1f), sortingOrder: 1);
                    CreateDeco(container, "Bed_O", bedBrown, new Vector3(15.5f, 12.8f, 0.05f), new Vector3(1.1f, 1.1f, 1f), sortingOrder: 2);
                    CreateDeco(container, "Wardrobe_O", wardrobe, new Vector3(13.2f, 13.2f, 0.05f), new Vector3(1f, 1f, 1f), sortingOrder: 2);

                    // Living S
                    CreateDeco(container, "Rug_Living_S", rugBlueOval, new Vector3(4.5f, 4.8f, 0.05f), new Vector3(1.5f, 1.5f, 1f), sortingOrder: 1);
                    CreateDeco(container, "Sofa_S", sofaBlue, new Vector3(4.5f, 4.8f, 0.05f), new Vector3(1.1f, 1.1f, 1f), sortingOrder: 2);
                    CreateDeco(container, "TV_S", tvUnit, new Vector3(6.5f, 6.5f, 0.05f), new Vector3(0.9f, 0.9f, 1f), sortingOrder: 2);

                    // Dining T
                    CreateDeco(container, "Rug_Dining_T", rugGoldLarge, new Vector3(14.5f, 4.5f, 0.05f), new Vector3(1.3f, 1.3f, 1f), sortingOrder: 1);
                    CreateDeco(container, "Table_T", diningTable, new Vector3(14.5f, 4.5f, 0.05f), new Vector3(1.1f, 1.1f, 1f), sortingOrder: 2);
                    CreateDeco(container, "Kitchen_T", kitchenCounter, new Vector3(11.5f, 5.8f, 0.05f), new Vector3(0.9f, 0.9f, 1f), sortingOrder: 2);

                    // Carport & Garden
                    CreateDeco(container, "Car_Blue", carBlue, new Vector3(2.5f, 2.0f, 0.05f), new Vector3(1.2f, 1.2f, 1f), sortingOrder: 2);
                    CreateDeco(container, "Tree_Garden", treeLarge, new Vector3(18.5f, 2.0f, 0.05f), new Vector3(1.2f, 1.2f, 1f), sortingOrder: 3);

                    // Doorway Thresholds
                    CreateDeco(container, "Door_Master_F", doorwayH, new Vector3(7.0f, 11.0f, 0.02f), Vector3.one, sortingOrder: 1);
                    CreateDeco(container, "Door_Bath_R", doorwayH, new Vector3(9.0f, 11.0f, 0.02f), Vector3.one, sortingOrder: 1);
                    CreateDeco(container, "Door_Bed_O", doorwayH, new Vector3(12.0f, 11.0f, 0.02f), Vector3.one, sortingOrder: 1);
                    CreateDeco(container, "Door_Living_S", doorwayH, new Vector3(7.0f, 7.0f, 0.02f), Vector3.one, sortingOrder: 1);
                    CreateDeco(container, "Door_Dining_T", doorwayH, new Vector3(12.0f, 7.0f, 0.02f), Vector3.one, sortingOrder: 1);

                    // Hiding Boxes
                    CreateHidingBox(container, "HidingBox_Hall", cardboardBox, new Vector3(8.5f, 8.5f, 0f));
                    CreateHidingBox(container, "HidingBox_Dining", cardboardBox, new Vector3(11.5f, 3.5f, 0f));
                    break;

                case 5: // FLAME (Level 5 Mansion)
                    // Master Bed F
                    CreateDeco(container, "Rug_Bed_F", rugRedOrnate, new Vector3(4.5f, 12.2f, 0.05f), new Vector3(1.4f, 1.4f, 1f), sortingOrder: 1);
                    CreateDeco(container, "Bed_F", bedRed, new Vector3(3.5f, 12.8f, 0.05f), new Vector3(1.1f, 1.1f, 1f), sortingOrder: 2);
                    CreateDeco(container, "Wardrobe_F", wardrobe, new Vector3(6.5f, 12.8f, 0.05f), new Vector3(0.9f, 0.9f, 1f), sortingOrder: 2);

                    // Study L
                    CreateDeco(container, "Rug_Study_L", rugGoldLarge, new Vector3(10.5f, 12.2f, 0.05f), new Vector3(1.2f, 1.2f, 1f), sortingOrder: 1);
                    CreateDeco(container, "Wardrobe_L", wardrobe, new Vector3(9.5f, 13.2f, 0.05f), new Vector3(1f, 1f, 1f), sortingOrder: 2);
                    CreateDeco(container, "Armchair_L", armchair, new Vector3(11.5f, 11.5f, 0.05f), new Vector3(0.9f, 0.9f, 1f), sortingOrder: 2);

                    // Kitchen & Dining A
                    CreateDeco(container, "Rug_Dining_A", rugGoldLarge, new Vector3(17.0f, 11.5f, 0.05f), new Vector3(1.3f, 1.3f, 1f), sortingOrder: 1);
                    CreateDeco(container, "Table_A", diningTable, new Vector3(17.0f, 11.5f, 0.05f), new Vector3(1.1f, 1.1f, 1f), sortingOrder: 2);
                    CreateDeco(container, "Kitchen_A", kitchenCounter, new Vector3(14.5f, 13.3f, 0.05f), new Vector3(1f, 1f, 1f), sortingOrder: 2);
                    CreateDeco(container, "Fridge_A", fridge, new Vector3(18.5f, 13.3f, 0.05f), new Vector3(0.9f, 0.9f, 1f), sortingOrder: 2);

                    // Grand Lounge M
                    CreateDeco(container, "Rug_Lounge_M", rugRedOrnate, new Vector3(5.5f, 5.8f, 0.05f), new Vector3(1.7f, 1.7f, 1f), sortingOrder: 1);
                    CreateDeco(container, "Sofa_M", sofaRed, new Vector3(4.5f, 4.8f, 0.05f), new Vector3(1.1f, 1.1f, 1f), sortingOrder: 2);
                    CreateDeco(container, "CoffeeTable_M", coffeeTable, new Vector3(6.5f, 4.8f, 0.05f), new Vector3(0.85f, 0.85f, 1f), sortingOrder: 2);
                    CreateDeco(container, "TV_M", tvUnit, new Vector3(4.5f, 7.5f, 0.05f), new Vector3(1f, 1f, 1f), sortingOrder: 2);

                    // Living E
                    CreateDeco(container, "Rug_Living_E", rugGoldLarge, new Vector3(16.0f, 5.0f, 0.05f), new Vector3(1.5f, 1.5f, 1f), sortingOrder: 1);
                    CreateDeco(container, "Sofa_E", sofaGreen, new Vector3(15.5f, 4.8f, 0.05f), new Vector3(1.1f, 1.1f, 1f), sortingOrder: 2);
                    CreateDeco(container, "CoffeeTable_E", coffeeTable, new Vector3(17.5f, 4.8f, 0.05f), new Vector3(0.85f, 0.85f, 1f), sortingOrder: 2);

                    // Carport & Garden
                    CreateDeco(container, "Car_Dark", carDark, new Vector3(3.5f, 1.5f, 0.05f), new Vector3(1.2f, 1.2f, 1f), sortingOrder: 2);
                    CreateDeco(container, "Tree_Mansion", treeLarge, new Vector3(20.5f, 1.8f, 0.05f), new Vector3(1.3f, 1.3f, 1f), sortingOrder: 3);

                    // Doorway Thresholds
                    CreateDeco(container, "Door_Bed_F", doorwayH, new Vector3(7.0f, 11.0f, 0.02f), Vector3.one, sortingOrder: 1);
                    CreateDeco(container, "Door_Study_L", doorwayH, new Vector3(10.0f, 11.0f, 0.02f), Vector3.one, sortingOrder: 1);
                    CreateDeco(container, "Door_Kitchen_A", doorwayH, new Vector3(15.0f, 11.0f, 0.02f), Vector3.one, sortingOrder: 1);
                    CreateDeco(container, "Door_Lounge_M", doorwayH, new Vector3(8.0f, 7.0f, 0.02f), Vector3.one, sortingOrder: 1);
                    CreateDeco(container, "Door_Living_E", doorwayH, new Vector3(14.0f, 7.0f, 0.02f), Vector3.one, sortingOrder: 1);

                    // Hiding Boxes
                    CreateHidingBox(container, "HidingBox_Hall_1", cardboardBox, new Vector3(8.5f, 9.5f, 0f));
                    CreateHidingBox(container, "HidingBox_Hall_2", cardboardBox, new Vector3(13.5f, 8.5f, 0f));
                    CreateHidingBox(container, "HidingBox_Lounge", cardboardBox, new Vector3(8.5f, 4.5f, 0f));
                    break;
            }
        }

        private static void CreateDeco(Transform parent, string name, Sprite sprite, Vector3 pos, Vector3 scale, int sortingOrder = 2)
        {
            if (sprite == null) return;
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
        }

        private static void CreateHidingBox(Transform parent, string name, Sprite boxSprite, Vector3 pos)
        {
            GameObject boxGo = new GameObject(name);
            boxGo.transform.SetParent(parent, false);
            boxGo.transform.localPosition = pos;

            SpriteRenderer sr = boxGo.AddComponent<SpriteRenderer>();
            if (boxSprite != null) sr.sprite = boxSprite;
            sr.sortingOrder = 2; // Di atas lantai/karpet dan di bawah player

            CircleCollider2D col = boxGo.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.55f;

            boxGo.AddComponent<HidingBox>();
        }

        #endregion

        #region Multi-Enemy Builder (Patrol AI Standby vs Circuit Roam)

        private static void BuildEnemiesForLevel(int level, Transform container, Sprite enemySprite)
        {
            switch (level)
            {
                case 1: // LEVEL 1: 2 Musuh (1 Standby + 1 Muter)
                    CreateEnemyGuard(container, "Enemy_Guard_Standby_1", enemySprite,
                        new Vector3(10.5f, 11.5f, -0.1f), EnemyCharacter.PatrolType.StandbyHorizontal, range: 1.8f);

                    CreateEnemyGuard(container, "Enemy_Guard_Muter_2", enemySprite,
                        new Vector3(10.5f, 6.5f, -0.1f), EnemyCharacter.PatrolType.CircuitRoam,
                        waypoints: new List<Vector3> {
                            new Vector3(10.5f, 6.5f, 0f),
                            new Vector3(16.5f, 6.5f, 0f),
                            new Vector3(16.5f, 10.5f, 0f),
                            new Vector3(10.5f, 10.5f, 0f)
                        });
                    break;

                case 2: // LEVEL 2: 3 Musuh (2 Standby + 1 Muter)
                    CreateEnemyGuard(container, "Enemy_Guard_Standby_1", enemySprite,
                        new Vector3(5.5f, 11.5f, -0.1f), EnemyCharacter.PatrolType.StandbyHorizontal, range: 1.5f);

                    CreateEnemyGuard(container, "Enemy_Guard_Standby_2", enemySprite,
                        new Vector3(12.5f, 7.5f, -0.1f), EnemyCharacter.PatrolType.StandbyHorizontal, range: 1.5f);

                    CreateEnemyGuard(container, "Enemy_Guard_Muter_3", enemySprite,
                        new Vector3(8.5f, 4.5f, -0.1f), EnemyCharacter.PatrolType.CircuitRoam,
                        waypoints: new List<Vector3> {
                            new Vector3(8.5f, 3.5f, 0f),
                            new Vector3(8.5f, 12.5f, 0f),
                            new Vector3(10.5f, 12.5f, 0f),
                            new Vector3(10.5f, 3.5f, 0f)
                        });
                    break;

                case 3: // LEVEL 3: 5 Musuh (3 Standby + 2 Muter)
                    CreateEnemyGuard(container, "Enemy_Guard_Standby_1", enemySprite,
                        new Vector3(5.5f, 13.5f, -0.1f), EnemyCharacter.PatrolType.StandbyHorizontal, range: 1.4f);

                    CreateEnemyGuard(container, "Enemy_Guard_Standby_2", enemySprite,
                        new Vector3(14.5f, 13.5f, -0.1f), EnemyCharacter.PatrolType.StandbyHorizontal, range: 1.4f);

                    CreateEnemyGuard(container, "Enemy_Guard_Standby_3", enemySprite,
                        new Vector3(14.5f, 4.5f, -0.1f), EnemyCharacter.PatrolType.StandbyHorizontal, range: 1.4f);

                    CreateEnemyGuard(container, "Enemy_Guard_Muter_4", enemySprite,
                        new Vector3(9.5f, 8.5f, -0.1f), EnemyCharacter.PatrolType.CircuitRoam,
                        waypoints: new List<Vector3> {
                            new Vector3(9.5f, 6.5f, 0f),
                            new Vector3(9.5f, 12.5f, 0f),
                            new Vector3(4.5f, 12.5f, 0f),
                            new Vector3(4.5f, 6.5f, 0f)
                        });

                    CreateEnemyGuard(container, "Enemy_Guard_Muter_5", enemySprite,
                        new Vector3(11.5f, 8.5f, -0.1f), EnemyCharacter.PatrolType.CircuitRoam,
                        waypoints: new List<Vector3> {
                            new Vector3(11.5f, 4.5f, 0f),
                            new Vector3(11.5f, 12.5f, 0f),
                            new Vector3(15.5f, 8.5f, 0f)
                        });
                    break;

                case 4: // LEVEL 4: 6 Musuh (4 Standby + 2 Muter)
                    CreateEnemyGuard(container, "Enemy_Guard_Standby_1", enemySprite,
                        new Vector3(5.5f, 11.5f, -0.1f), EnemyCharacter.PatrolType.StandbyHorizontal, range: 1.4f);

                    CreateEnemyGuard(container, "Enemy_Guard_Standby_2", enemySprite,
                        new Vector3(14.5f, 11.5f, -0.1f), EnemyCharacter.PatrolType.StandbyHorizontal, range: 1.4f);

                    CreateEnemyGuard(container, "Enemy_Guard_Standby_3", enemySprite,
                        new Vector3(5.5f, 5.5f, -0.1f), EnemyCharacter.PatrolType.StandbyHorizontal, range: 1.4f);

                    CreateEnemyGuard(container, "Enemy_Guard_Standby_4", enemySprite,
                        new Vector3(14.5f, 5.5f, -0.1f), EnemyCharacter.PatrolType.StandbyHorizontal, range: 1.4f);

                    CreateEnemyGuard(container, "Enemy_Guard_Muter_5", enemySprite,
                        new Vector3(8.5f, 6.5f, -0.1f), EnemyCharacter.PatrolType.CircuitRoam,
                        waypoints: new List<Vector3> {
                            new Vector3(8.5f, 4.5f, 0f),
                            new Vector3(8.5f, 11.5f, 0f),
                            new Vector3(12.5f, 11.5f, 0f),
                            new Vector3(12.5f, 4.5f, 0f)
                        });

                    CreateEnemyGuard(container, "Enemy_Guard_Muter_6", enemySprite,
                        new Vector3(10.5f, 8.5f, -0.1f), EnemyCharacter.PatrolType.CircuitRoam,
                        waypoints: new List<Vector3> {
                            new Vector3(6.5f, 8.5f, 0f),
                            new Vector3(15.5f, 8.5f, 0f),
                            new Vector3(15.5f, 11.5f, 0f),
                            new Vector3(6.5f, 11.5f, 0f)
                        });
                    break;

                case 5: // LEVEL 5: 7 Musuh (4 Standby + 3 Muter termasuk Final Guardian)
                    CreateEnemyGuard(container, "Enemy_Guard_Standby_1", enemySprite,
                        new Vector3(5.5f, 12.5f, -0.1f), EnemyCharacter.PatrolType.StandbyHorizontal, range: 1.4f);

                    CreateEnemyGuard(container, "Enemy_Guard_Standby_2", enemySprite,
                        new Vector3(16.5f, 12.5f, -0.1f), EnemyCharacter.PatrolType.StandbyHorizontal, range: 1.4f);

                    CreateEnemyGuard(container, "Enemy_Guard_Standby_3", enemySprite,
                        new Vector3(5.5f, 5.5f, -0.1f), EnemyCharacter.PatrolType.StandbyHorizontal, range: 1.4f);

                    CreateEnemyGuard(container, "Enemy_Guard_Standby_4", enemySprite,
                        new Vector3(16.5f, 5.5f, -0.1f), EnemyCharacter.PatrolType.StandbyHorizontal, range: 1.4f);

                    CreateEnemyGuard(container, "Enemy_Guard_Muter_5", enemySprite,
                        new Vector3(10.5f, 12.5f, -0.1f), EnemyCharacter.PatrolType.CircuitRoam,
                        waypoints: new List<Vector3> {
                            new Vector3(8.5f, 12.5f, 0f),
                            new Vector3(14.5f, 12.5f, 0f),
                            new Vector3(14.5f, 8.5f, 0f),
                            new Vector3(8.5f, 8.5f, 0f)
                        });

                    CreateEnemyGuard(container, "Enemy_Guard_Muter_6", enemySprite,
                        new Vector3(10.5f, 4.5f, -0.1f), EnemyCharacter.PatrolType.CircuitRoam,
                        waypoints: new List<Vector3> {
                            new Vector3(8.5f, 3.5f, 0f),
                            new Vector3(8.5f, 8.5f, 0f),
                            new Vector3(14.5f, 8.5f, 0f),
                            new Vector3(14.5f, 3.5f, 0f)
                        });

                    // Musuh 7: FINAL GUARDIAN
                    CreateBossGuardian(container, "Enemy_FinalGuardian", enemySprite,
                        new Vector3(11.5f, 7.5f, -0.1f),
                        waypoints: new List<Vector3> {
                            new Vector3(10.5f, 5.5f, 0f),
                            new Vector3(10.5f, 9.5f, 0f),
                            new Vector3(12.5f, 9.5f, 0f),
                            new Vector3(12.5f, 5.5f, 0f)
                        });
                    break;
            }
        }

        private static void CreateEnemyGuard(Transform parent, string name, Sprite sprite, Vector3 pos,
            EnemyCharacter.PatrolType patrolType, List<Vector3> waypoints = null, float range = 1.6f)
        {
            GameObject enemyObj = new GameObject(name);
            enemyObj.transform.SetParent(parent, false);
            enemyObj.transform.localPosition = pos;

            SpriteRenderer sr = enemyObj.AddComponent<SpriteRenderer>();
            if (sprite != null) sr.sprite = sprite;
            sr.sortingOrder = 4;

            CircleCollider2D col = enemyObj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.35f;

            EnemyCharacter enemyChar = enemyObj.AddComponent<EnemyCharacter>();
            enemyChar.SetBasePosition(pos);
            enemyChar.SetPatrolConfig(patrolType, pos, waypoints, range);

            enemyObj.AddComponent<EnemyVisionController>();
        }

        private static void CreateBossGuardian(Transform parent, string name, Sprite sprite, Vector3 pos, List<Vector3> waypoints)
        {
            GameObject bossObj = new GameObject(name);
            bossObj.transform.SetParent(parent, false);
            bossObj.transform.localPosition = pos;

            SpriteRenderer sr = bossObj.AddComponent<SpriteRenderer>();
            if (sprite != null) sr.sprite = sprite;
            sr.sortingOrder = 4;
            sr.color = new Color(1f, 0.65f, 0.65f, 1f);

            CircleCollider2D col = bossObj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.45f;

            EnemyCharacter bossChar = bossObj.AddComponent<EnemyCharacter>();
            bossChar.SetBasePosition(pos);
            bossChar.SetPatrolConfig(EnemyCharacter.PatrolType.CircuitRoam, pos, waypoints, 1.8f);

            bossObj.AddComponent<EnemyVisionController>();
        }

        #endregion
    }
}
