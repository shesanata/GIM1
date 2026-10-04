using System;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using MazeSpell;
using TMPro;

namespace MazeSpell.Editor
{
    [InitializeOnLoad]
    public static class Level5MazeBuilder
    {
        static Level5MazeBuilder()
        {
            EditorApplication.delayCall += AutoBuildIfNeeded;
        }

        private static void AutoBuildIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            if (!SessionState.GetBool("MazeSpell_Level5LayoutBuilt_OldRef_V1", false))
            {
                SessionState.SetBool("MazeSpell_Level5LayoutBuilt_OldRef_V1", true);
                BuildLevel5();
            }
        }

        public static readonly int[,] Level5Matrix = MazeMatrixRegistry.Level5;

        [MenuItem("MazeSpell/Build Level 5 Maze Layout")]
        public static void BuildLevel5()
        {
            Debug.Log("[Level5MazeBuilder] Memulai pembuatan layout labirin Level 5 (21x11 Flame Theme)...");

            MazeThematicVisualGenerator.GenerateAllVisualAssets();

            Tile floorTile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/Level5/Floor_Tile.asset")
                             ?? AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/Level5/Path_Stone_Blue_Tile.asset");
            Tile wallTile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/Level5/Wall_Tile.asset")
                            ?? AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/Level5/Lava_Rock_Wall_Tile.asset");

            Sprite enemySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Enemy.png");
            Sprite startPedestalSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Start_Pedestal.png");
            Sprite centralMedallionSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Central_Medallion.png");

            TMP_FontAsset bubbleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BubbleLetterStyleSetup.FONT_PATH);
            Material outlineMat = AssetDatabase.LoadAssetAtPath<Material>(BubbleLetterStyleSetup.OUTLINE_MAT_PATH);
            Material depthMat = AssetDatabase.LoadAssetAtPath<Material>(BubbleLetterStyleSetup.DEPTH_MAT_PATH);
            Material faceMat = AssetDatabase.LoadAssetAtPath<Material>(BubbleLetterStyleSetup.FACE_MAT_PATH);

            if (bubbleFont != null)
            {
                BubbleLetterStyleSetup.EnsureMaterialsExist(bubbleFont, out outlineMat, out depthMat, out faceMat);
            }

            string prefabPath = "Assets/Prefabs/MazeGrids/Level5_MazeGrid.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
            {
                Debug.LogError($"[Level5MazeBuilder] Gagal memuat prefab di {prefabPath}!");
                return;
            }

            try
            {
                Transform pathTr = root.transform.Find("Background_Path");
                Transform wallsTr = root.transform.Find("Walls_Obstacles");

                if (pathTr == null || wallsTr == null)
                {
                    Debug.LogError("[Level5MazeBuilder] Transform Background_Path atau Walls_Obstacles tidak ditemukan di prefab!");
                    return;
                }

                Tilemap pathTilemap = pathTr.GetComponent<Tilemap>();
                Tilemap wallsTilemap = wallsTr.GetComponent<Tilemap>();

                pathTilemap.ClearAllTiles();
                wallsTilemap.ClearAllTiles();

                for (int row = 0; row < 11; row++)
                {
                    int y = 10 - row;
                    for (int x = 0; x < 21; x++)
                    {
                        Vector3Int cellPos = new Vector3Int(x, y, 0);

                        if (Level5Matrix[row, x] == 0)
                        {
                            pathTilemap.SetTile(cellPos, floorTile);
                            wallsTilemap.SetTile(cellPos, null);
                        }
                        else
                        {
                            pathTilemap.SetTile(cellPos, wallTile);
                            wallsTilemap.SetTile(cellPos, wallTile);
                        }
                    }
                }

                MazeGridBounds bounds = root.GetComponent<MazeGridBounds>();
                MazeLevelData data = AssetDatabase.LoadAssetAtPath<MazeLevelData>("Assets/Tilemaps/Level5/Level5_Data.asset");
                if (data != null)
                {
                    data.columns = 21;
                    data.rows = 11;
                    data.themeName = "Flame / Volcano";
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

                // Spawn Point (1.5, 2.5) + Start Pedestal Visual
                Transform spawn = gameplayContainer.Find("SpawnPoint");
                if (spawn == null)
                {
                    GameObject spObj = new GameObject("SpawnPoint");
                    spObj.transform.SetParent(gameplayContainer, false);
                    spawn = spObj.transform;
                }
                spawn.localPosition = new Vector3(1.5f, 2.5f, 0f);

                Transform startPedestal = spawn.Find("StartPedestal");
                if (startPedestal == null)
                {
                    GameObject pedObj = new GameObject("StartPedestal");
                    pedObj.transform.SetParent(spawn, false);
                    startPedestal = pedObj.transform;
                }
                startPedestal.localPosition = new Vector3(0f, 0f, 0.08f);
                startPedestal.localScale = new Vector3(0.9f, 0.9f, 1f);
                SpriteRenderer pedSr = startPedestal.GetComponent<SpriteRenderer>();
                if (pedSr == null) pedSr = startPedestal.gameObject.AddComponent<SpriteRenderer>();
                if (startPedestalSprite != null) pedSr.sprite = startPedestalSprite;
                pedSr.color = new Color(1f, 0.55f, 0.15f, 0.9f);
                pedSr.sortingOrder = 1;

                // Central Medallion at (9.5, 7.5)
                Transform centralMed = gameplayContainer.Find("CentralMedallion");
                if (centralMed == null)
                {
                    GameObject medObj = new GameObject("CentralMedallion");
                    medObj.transform.SetParent(gameplayContainer, false);
                    centralMed = medObj.transform;
                }
                centralMed.localPosition = new Vector3(9.5f, 7.5f, 0.08f);
                centralMed.localScale = new Vector3(1.1f, 1.1f, 1f);
                SpriteRenderer medSr = centralMed.GetComponent<SpriteRenderer>();
                if (medSr == null) medSr = centralMed.gameObject.AddComponent<SpriteRenderer>();
                if (centralMedallionSprite != null) medSr.sprite = centralMedallionSprite;
                medSr.color = new Color(1f, 0.65f, 0.2f, 0.85f);
                medSr.sortingOrder = 1;

                // Goal (19.5, 2.5) with ExitGateVisual
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
                exitGate.Initialize(5);

                // Collectible Letters: F (3.5, 8.5) -> L (4.5, 5.5) -> A (9.5, 7.5) -> M (12.5, 7.5) -> E (15.5, 2.5)
                Transform lettersContainer = gameplayContainer.Find("Letters");
                if (lettersContainer != null)
                {
                    UnityEngine.Object.DestroyImmediate(lettersContainer.gameObject);
                }
                GameObject lGo = new GameObject("Letters");
                lGo.transform.SetParent(gameplayContainer, false);
                lettersContainer = lGo.transform;

                var l5LetterPos = LetterPlacementSetup.CanonicalLetterPositions[5];
                string[] lettersOrder = { "F", "L", "A", "M", "E" };

                foreach (var charKey in lettersOrder)
                {
                    Vector3 pos = l5LetterPos[charKey];
                    char ch = charKey[0];

                    Transform letterT = lettersContainer.Find($"Letter_{ch}");
                    if (letterT == null) letterT = lettersContainer.Find($"Letter_{ch}_0");
                    if (letterT == null)
                    {
                        GameObject lObj = new GameObject($"Letter_{ch}");
                        lObj.transform.SetParent(lettersContainer, false);
                        letterT = lObj.transform;
                    }

                    letterT.localPosition = pos;
                    BubbleLetterStyleSetup.SetupLetterVisualHierarchy(letterT.gameObject, ch, bubbleFont, outlineMat, depthMat, faceMat);
                }

                // Enemy: (9.5, 2.5)
                Transform enemiesContainer = gameplayContainer.Find("Enemies");
                if (enemiesContainer != null)
                {
                    UnityEngine.Object.DestroyImmediate(enemiesContainer.gameObject);
                }

                GameObject newEnemiesGo = new GameObject("Enemies");
                newEnemiesGo.transform.SetParent(gameplayContainer, false);

                GameObject enemyObj = new GameObject("Enemy_1");
                enemyObj.transform.SetParent(newEnemiesGo.transform, false);
                enemyObj.transform.localPosition = new Vector3(9.5f, 2.5f, -0.1f);

                SpriteRenderer sr = enemyObj.AddComponent<SpriteRenderer>();
                if (enemySprite != null) sr.sprite = enemySprite;
                sr.sortingOrder = 4;

                CircleCollider2D col = enemyObj.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.35f;

                EnemyCharacter enemyChar = enemyObj.AddComponent<EnemyCharacter>();
                enemyChar.SetBasePosition(new Vector3(9.5f, 2.5f, -0.1f));

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log($"[Level5MazeBuilder] Prefab berhasil disimpan ke {prefabPath}!");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Level5MazeBuilder] ★ SELESAI MEMBANGUN LABIRIN LEVEL 5 (FLAME) ★");
        }
    }
}
