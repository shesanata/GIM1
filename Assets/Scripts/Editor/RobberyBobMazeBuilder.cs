using System;
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
    /// Builder komprehensif untuk membangun seluruh level (Level 1 - 5) menggunakan tata letak
    /// Mansion / House bergaya Robbery Bob:
    /// - Ruang Hijau (Green study/office) dengan meja kayu & tanaman
    /// - Ruang Tengah Oranye (Orange retro carpet hall) dengan jejak kaki & sofa
    /// - Kamar Mandi Biru (Blue diamond bathroom) dengan kloset & wastafel
    /// - Dapur & Rak Toko Atas (Kitchen shelves & office desk)
    /// - Kop Surat Tersembunyi (Secret Document Envelopes) untuk seluruh 5 huruf kata target
    /// - Musuh Satpam/Polisi dengan Vision Cone, Alert Balloons ('!' & '?'), dan Chase & Kill mechanics
    /// </summary>
    [InitializeOnLoad]
    public static class RobberyBobMazeBuilder
    {
        static RobberyBobMazeBuilder()
        {
            EditorApplication.delayCall += AutoBuildIfNeeded;
        }

        private static void AutoBuildIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            if (!SessionState.GetBool("MazeSpell_RobberyBob_Built_V1", false))
            {
                SessionState.SetBool("MazeSpell_RobberyBob_Built_V1", true);
                BuildAllRobberyBobLevels();
            }
        }

        [MenuItem("MazeSpell/Build All Robbery Bob Levels (L1 - L5)")]
        public static void BuildAllRobberyBobLevels()
        {
            Debug.Log("=================================================");
            Debug.Log(">>> MEMBANGUN SELURUH LEVEL ROBBERY BOB (L1 - L5) <<<");
            Debug.Log("=================================================");

            // 1. Generate All Textures & Tiles
            RobberyBobVisualGenerator.GenerateAllRobberyBobAssets();

            // 2. Build Level 1 s/d Level 5
            for (int lvl = 1; lvl <= 5; lvl++)
            {
                BuildLevel(lvl);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(">>> ★ SELESAI MEMBANGUN SELURUH 5 LEVEL ROBBERY BOB! ★ <<<");
        }

        public static void BuildLevel(int level)
        {
            Debug.Log($"[RobberyBobMazeBuilder] Membangun Level {level} (Robbery Bob Mansion 22x13)...");

            string prefabPath = $"Assets/Prefabs/MazeGrids/Level{level}_MazeGrid.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
            {
                Debug.LogError($"[RobberyBobMazeBuilder] Gagal memuat prefab di {prefabPath}!");
                return;
            }

            try
            {
                // Tiles
                Tile orangeTile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/RobberyBob/Floor_Orange_Tile.asset");
                Tile greenTile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/RobberyBob/Floor_Green_Tile.asset");
                Tile bathroomTile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/RobberyBob/Floor_Bathroom_Tile.asset");
                Tile kitchenTile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/RobberyBob/Floor_Kitchen_Tile.asset");
                Tile wallInteriorTile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/RobberyBob/Wall_Interior_Tile.asset");
                Tile wallExteriorTile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/RobberyBob/Wall_Exterior_Tile.asset");

                // Sprites
                Sprite deskSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/RobberyBob/Deco_Robbery_Desk.png");
                Sprite toiletSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/RobberyBob/Deco_Robbery_Toilet.png");
                Sprite sofaSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/RobberyBob/Deco_Robbery_Sofa.png");
                Sprite shelfSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/RobberyBob/Deco_Robbery_Shelves.png");
                Sprite plantSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/RobberyBob/Deco_Robbery_Plant.png");
                Sprite footstepsSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/RobberyBob/Deco_Robbery_Footsteps.png");
                Sprite envelopeSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/RobberyBob/Envelope_SecretLetter.png");
                Sprite enemySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Enemy.png");

                // Fonts
                TMP_FontAsset bubbleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BubbleLetterStyleSetup.FONT_PATH);
                Material outlineMat = AssetDatabase.LoadAssetAtPath<Material>(BubbleLetterStyleSetup.OUTLINE_MAT_PATH);
                Material depthMat = AssetDatabase.LoadAssetAtPath<Material>(BubbleLetterStyleSetup.DEPTH_MAT_PATH);
                Material faceMat = AssetDatabase.LoadAssetAtPath<Material>(BubbleLetterStyleSetup.FACE_MAT_PATH);
                if (bubbleFont != null)
                {
                    BubbleLetterStyleSetup.EnsureMaterialsExist(bubbleFont, out outlineMat, out depthMat, out faceMat);
                }

                // 1. Setup Tilemaps (Background_Path & Walls_Obstacles)
                Transform pathTr = root.transform.Find("Background_Path");
                Transform wallsTr = root.transform.Find("Walls_Obstacles");
                if (pathTr == null || wallsTr == null)
                {
                    Debug.LogError("[RobberyBobMazeBuilder] Background_Path atau Walls_Obstacles tidak ditemukan!");
                    return;
                }

                Tilemap pathTilemap = pathTr.GetComponent<Tilemap>();
                Tilemap wallsTilemap = wallsTr.GetComponent<Tilemap>();
                pathTilemap.ClearAllTiles();
                wallsTilemap.ClearAllTiles();

                int[,] matrix = MazeMatrixRegistry.RobberyBobMansionMatrix;
                int rows = 13;
                int cols = 22;

                for (int row = 0; row < rows; row++)
                {
                    int y = (rows - 1) - row;
                    for (int x = 0; x < cols; x++)
                    {
                        Vector3Int cellPos = new Vector3Int(x, y, 0);
                        bool isWall = matrix[row, x] == 1;
                        bool isExterior = (x == 0 || x == cols - 1 || y == 0 || y == rows - 1);

                        // Tentukan jenis lantai berdasarkan area ruangan
                        Tile floorTile = orangeTile;
                        if (x >= 1 && x <= 7 && y >= 1 && y <= 6)
                        {
                            floorTile = greenTile; // Ruang Hijau (Study / Office)
                        }
                        else if (x >= 14 && x <= 20 && y >= 6 && y <= 11)
                        {
                            floorTile = bathroomTile; // Kamar Mandi Biru
                        }
                        else if (x >= 1 && x <= 7 && y >= 8 && y <= 11)
                        {
                            floorTile = kitchenTile; // Dapur / Counter Atas
                        }

                        if (isWall)
                        {
                            Tile wTile = isExterior ? wallExteriorTile : wallInteriorTile;
                            pathTilemap.SetTile(cellPos, floorTile);
                            wallsTilemap.SetTile(cellPos, wTile);
                        }
                        else
                        {
                            pathTilemap.SetTile(cellPos, floorTile);
                            wallsTilemap.SetTile(cellPos, null);
                        }
                    }
                }

                // 2. Setup Level Data & Bounds (22x13)
                MazeGridBounds bounds = root.GetComponent<MazeGridBounds>();
                string dataPath = $"Assets/Tilemaps/Level{level}/Level{level}_Data.asset";
                MazeLevelData data = AssetDatabase.LoadAssetAtPath<MazeLevelData>(dataPath);
                if (data != null)
                {
                    data.columns = cols;
                    data.rows = rows;
                    data.themeName = "Robbery Bob Mansion";
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

                // 3. Setup Spawn Point di Ruang Hijau (2.5, 2.5)
                Transform spawn = gameplayContainer.Find("SpawnPoint");
                if (spawn == null)
                {
                    GameObject spObj = new GameObject("SpawnPoint");
                    spObj.transform.SetParent(gameplayContainer, false);
                    spawn = spObj.transform;
                }
                spawn.localPosition = new Vector3(2.5f, 2.5f, 0f);

                // 4. Setup Goal / Exit Gate di Living Room (19.5, 2.5)
                Transform goal = gameplayContainer.Find("Goal");
                if (goal == null)
                {
                    GameObject gObj = new GameObject("Goal");
                    gObj.transform.SetParent(gameplayContainer, false);
                    goal = gObj.transform;
                }
                goal.localPosition = new Vector3(19.5f, 2.5f, 0f);
                BoxCollider2D goalCol = goal.GetComponent<BoxCollider2D>();
                if (goalCol == null) goalCol = goal.gameObject.AddComponent<BoxCollider2D>();
                goalCol.isTrigger = true;
                goalCol.size = new Vector2(1.5f, 1.6f);

                LevelGoalTrigger goalTrigger = goal.GetComponent<LevelGoalTrigger>();
                if (goalTrigger == null) goalTrigger = goal.gameObject.AddComponent<LevelGoalTrigger>();
                goalTrigger.ResetTrigger();

                ExitGateVisual exitGate = goal.GetComponent<ExitGateVisual>();
                if (exitGate == null) exitGate = goal.gameObject.AddComponent<ExitGateVisual>();
                exitGate.Initialize(level);

                // 5. Setup Furniture & Room Decorations
                Transform decoContainer = gameplayContainer.Find("RoomDecorations");
                if (decoContainer != null) UnityEngine.Object.DestroyImmediate(decoContainer.gameObject);
                GameObject decoGo = new GameObject("RoomDecorations");
                decoGo.transform.SetParent(gameplayContainer, false);
                decoContainer = decoGo.transform;

                // Meja Kerja di Ruang Hijau (2.5, 4.5)
                CreateDecoObject(decoContainer, "Desk_GreenRoom", deskSprite, new Vector3(2.5f, 4.5f, 0.05f), new Vector3(1.2f, 1.2f, 1f));
                // Tanaman Pot di Ruang Hijau & Koridor
                CreateDecoObject(decoContainer, "Plant_GreenRoom", plantSprite, new Vector3(1.8f, 1.8f, 0.04f), new Vector3(0.9f, 0.9f, 1f));
                CreateDecoObject(decoContainer, "Plant_Hallway", plantSprite, new Vector3(8.8f, 1.8f, 0.04f), new Vector3(0.9f, 0.9f, 1f));
                CreateDecoObject(decoContainer, "Plant_Bathroom", plantSprite, new Vector3(14.8f, 1.8f, 0.04f), new Vector3(0.9f, 0.9f, 1f));
                // Kloset Toilet di Kamar Mandi (18.5, 9.5)
                CreateDecoObject(decoContainer, "Toilet_Bathroom", toiletSprite, new Vector3(18.5f, 9.5f, 0.05f), new Vector3(1.1f, 1.1f, 1f));
                // Sofa Abu-Abu di Living Room (16.0, 3.0)
                CreateDecoObject(decoContainer, "Sofa_LivingRoom", sofaSprite, new Vector3(16.0f, 3.0f, 0.05f), new Vector3(1.1f, 1.1f, 1f));
                // Rak Dapur di Atas (4.0, 11.2) & (9.0, 11.2)
                CreateDecoObject(decoContainer, "Shelf_Kitchen_1", shelfSprite, new Vector3(4.0f, 11.2f, 0.05f), new Vector3(1.2f, 1.2f, 1f));
                CreateDecoObject(decoContainer, "Shelf_Kitchen_2", shelfSprite, new Vector3(9.0f, 11.2f, 0.05f), new Vector3(1.2f, 1.2f, 1f));
                // Jejak Kaki Biru di Koridor (10.5, 6.0)
                CreateDecoObject(decoContainer, "Footsteps_Hall", footstepsSprite, new Vector3(10.5f, 6.0f, 0.08f), new Vector3(1.2f, 1.2f, 1f));

                // 6. Setup Kop Surat (Collectible Letters in Secret Envelopes)
                Transform lettersContainer = gameplayContainer.Find("Letters");
                if (lettersContainer != null) UnityEngine.Object.DestroyImmediate(lettersContainer.gameObject);
                GameObject lGo = new GameObject("Letters");
                lGo.transform.SetParent(gameplayContainer, false);
                lettersContainer = lGo.transform;

                var letterPositions = LetterPlacementSetup.CanonicalLetterPositions[level];
                string[] lettersOrder = GetLettersOrderForLevel(level);

                for (int i = 0; i < lettersOrder.Length; i++)
                {
                    string key = lettersOrder[i];
                    Vector3 pos = letterPositions[key];
                    char ch = key.StartsWith("S_") ? 'S' : key[0];

                    string nodeName = (key == "S_1" || key == "S_2") ? $"Letter_S_{i - 2}" : $"Letter_{ch}";
                    GameObject lObj = new GameObject(nodeName);
                    lObj.transform.SetParent(lettersContainer, false);
                    lObj.transform.localPosition = pos;

                    // Setup Visual Bubble Letter + Kop Surat Envelope
                    BubbleLetterStyleSetup.SetupLetterVisualHierarchy(lObj, ch, bubbleFont, outlineMat, depthMat, faceMat);

                    GameObject envGo = new GameObject("EnvelopeVisual");
                    envGo.transform.SetParent(lObj.transform, false);
                    envGo.transform.localPosition = new Vector3(0f, 0f, 0.05f);
                    envGo.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
                    SpriteRenderer envSr = envGo.AddComponent<SpriteRenderer>();
                    if (envelopeSprite != null) envSr.sprite = envelopeSprite;
                    envSr.sortingOrder = 3;

                    var envVisual = lObj.AddComponent<EnvelopeLetterVisual>();
                }

                // 7. Setup Musuh Satpam/Polisi (Enemy Guard with Vision & Chase Mechanics)
                Transform enemiesContainer = gameplayContainer.Find("Enemies");
                if (enemiesContainer != null) UnityEngine.Object.DestroyImmediate(enemiesContainer.gameObject);
                GameObject newEnemiesGo = new GameObject("Enemies");
                newEnemiesGo.transform.SetParent(gameplayContainer, false);

                GameObject enemyObj = new GameObject("Enemy_Guard");
                enemyObj.transform.SetParent(newEnemiesGo.transform, false);
                enemyObj.transform.localPosition = new Vector3(11.5f, 4.5f, -0.1f);

                SpriteRenderer sr = enemyObj.AddComponent<SpriteRenderer>();
                if (enemySprite != null) sr.sprite = enemySprite;
                sr.sortingOrder = 4;

                CircleCollider2D col = enemyObj.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.35f;

                EnemyCharacter enemyChar = enemyObj.AddComponent<EnemyCharacter>();
                enemyChar.SetBasePosition(new Vector3(11.5f, 4.5f, -0.1f));

                // Tambahkan Kontrol Penglihatan & Alert Robbery Bob
                EnemyVisionController vision = enemyObj.AddComponent<EnemyVisionController>();

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log($"[RobberyBobMazeBuilder] Level {level} berhasil disimpan ke {prefabPath}!");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void CreateDecoObject(Transform parent, string name, Sprite sprite, Vector3 pos, Vector3 scale)
        {
            if (sprite == null) return;
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 1; // Di atas lantai, di bawah karakter/huruf
        }

        private static string[] GetLettersOrderForLevel(int level)
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
    }
}
