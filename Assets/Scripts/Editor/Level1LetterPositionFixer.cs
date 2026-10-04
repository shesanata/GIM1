using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using MazeSpell;

namespace MazeSpell.Editor
{
    /// <summary>
    /// Script editor khusus untuk memperbaiki posisi spawn collectible huruf (E, A, R, T, H)
    /// pada Maze Level 1 agar tepat di tengah jalur batu biru yang walkable.
    /// </summary>
    public static class Level1LetterPositionFixer
    {
        // Posisi target tepat di tengah sel batu biru (X + 0.5, Y + 0.5)
        public static readonly Dictionary<char, Vector3> TargetPositions = new Dictionary<char, Vector3>
        {
            { 'E', new Vector3(3.5f, 8.5f, -0.1f) },   // Top-Left Corridor (cell 3, 8)
            { 'A', new Vector3(4.5f, 5.5f, -0.1f) },   // Left Inner Chamber (cell 4, 5)
            { 'R', new Vector3(9.5f, 7.5f, -0.1f) },   // Central Upper Highway (cell 9, 7)
            { 'T', new Vector3(12.5f, 7.5f, -0.1f) },  // Top-Right Arch (cell 12, 7)
            { 'H', new Vector3(15.5f, 2.5f, -0.1f) }   // East Approach to Exit (cell 15, 2)
        };

        [MenuItem("MazeSpell/Fix Level 1 Letter Positions")]
        public static void FixPositionsAndCapture()
        {
            Debug.Log("==================================================================");
            Debug.Log(">>> MEMULAI PENYESUAIAN POSISI COLLECTIBLE HURUF LEVEL 1 <<<");
            Debug.Log("==================================================================");

            string prefabPath = "Assets/Prefabs/MazeGrids/Level1_MazeGrid.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
            {
                Debug.LogError($"[Fixer] Gagal memuat prefab {prefabPath}!");
                return;
            }

            try
            {
                Transform gameplayContainer = root.transform.Find("GameplayObjects");
                Transform lettersContainer = gameplayContainer != null ? gameplayContainer.Find("Letters") : root.transform.Find("Letters");

                var allLetters = root.GetComponentsInChildren<CollectibleLetter>(true);
                Debug.Log($"[Fixer] Ditemukan {allLetters.Length} CollectibleLetter di prefab.");

                // Pastikan Tilemap walls dan path
                Tilemap wallsTilemap = null;
                Tilemap pathTilemap = null;
                Transform wallsTr = root.transform.Find("Walls_Obstacles");
                Transform pathTr = root.transform.Find("Background_Path");
                if (wallsTr != null) wallsTilemap = wallsTr.GetComponent<Tilemap>();
                if (pathTr != null) pathTilemap = pathTr.GetComponent<Tilemap>();

                foreach (var letterItem in TargetPositions)
                {
                    char ch = letterItem.Key;
                    Vector3 targetPos = letterItem.Value;

                    // 1. Cari objek huruf berdasarkan CollectibleLetter.Letter atau nama GameObject
                    Transform letterTransform = null;
                    CollectibleLetter foundCl = null;

                    foreach (var cl in allLetters)
                    {
                        if (cl.Letter == ch)
                        {
                            letterTransform = cl.transform;
                            foundCl = cl;
                            break;
                        }
                    }

                    if (letterTransform == null && lettersContainer != null)
                    {
                        foreach (Transform child in lettersContainer)
                        {
                            if (child.name.Equals($"Letter_{ch}", StringComparison.OrdinalIgnoreCase) ||
                                child.name.StartsWith($"Letter_{ch}_", StringComparison.OrdinalIgnoreCase))
                            {
                                letterTransform = child;
                                foundCl = child.GetComponent<CollectibleLetter>();
                                break;
                            }
                        }
                    }

                    if (letterTransform == null)
                    {
                        // Cari di seluruh hierarki root
                        foreach (Transform tr in root.GetComponentsInChildren<Transform>(true))
                        {
                            if (tr.name.Equals($"Letter_{ch}", StringComparison.OrdinalIgnoreCase) ||
                                tr.name.StartsWith($"Letter_{ch}_", StringComparison.OrdinalIgnoreCase))
                            {
                                letterTransform = tr;
                                foundCl = tr.GetComponent<CollectibleLetter>();
                                break;
                            }
                        }
                    }

                    if (letterTransform != null)
                    {
                        // Set posisi lokal tepat di tengah batu (Z = -0.1f)
                        letterTransform.localPosition = targetPos;

                        // Pastikan CollectibleLetter terisi karakter yang tepat
                        if (foundCl != null)
                        {
                            foundCl.Letter = ch;
                        }

                        // Pastikan Collider2D adalah Trigger dan radius sesuai
                        var col = letterTransform.GetComponent<CircleCollider2D>();
                        if (col != null)
                        {
                            col.isTrigger = true;
                            col.radius = 0.45f;
                        }

                        // Pastikan SortingOrder teks di atas batu (Order in Layer 5)
                        var tmp = letterTransform.GetComponentInChildren<TextMeshPro>(true);
                        if (tmp != null)
                        {
                            var renderer = tmp.GetComponent<MeshRenderer>();
                            if (renderer != null)
                            {
                                renderer.sortingOrder = 5;
                            }
                        }

                        // Hitung cell koordinat grid dan validasi
                        int cellX = Mathf.FloorToInt(targetPos.x);
                        int cellY = Mathf.FloorToInt(targetPos.y);
                        int matrixRow = 14 - cellY;
                        int matrixVal = MazeMatrixRegistry.Level1[matrixRow, cellX];
                        string matrixStatus = (matrixVal == 0) ? "WALKABLE (BATU BIRU)" : "NON-WALKABLE (RUMPUT)";

                        // Validasi tilemap nyata
                        bool noWallCollider = true;
                        bool hasPathTile = true;
                        if (wallsTilemap != null)
                        {
                            TileBase wallTile = wallsTilemap.GetTile(new Vector3Int(cellX, cellY, 0));
                            noWallCollider = (wallTile == null);
                        }
                        if (pathTilemap != null)
                        {
                            TileBase pTile = pathTilemap.GetTile(new Vector3Int(cellX, cellY, 0));
                            hasPathTile = (pTile != null);
                        }

                        Debug.Log($"[Fixer] Huruf '{ch}' -> Posisi: {targetPos} -> Cell ({cellX}, {cellY}) -> Matriks: {matrixStatus} | NoWallCol: {noWallCollider} | PathTile: {hasPathTile}");
                    }
                    else
                    {
                        Debug.LogError($"[Fixer] Objek huruf '{ch}' tidak ditemukan!");
                    }
                }

                // Sinkronkan SpawnPoint dan Goal jika ada di gameplayContainer
                if (gameplayContainer != null)
                {
                    Transform spawn = gameplayContainer.Find("SpawnPoint");
                    if (spawn != null) spawn.localPosition = new Vector3(2.5f, 2.5f, 0f);

                    Transform goal = gameplayContainer.Find("Goal");
                    if (goal != null) goal.localPosition = new Vector3(27.5f, 3.5f, 0f);
                }

                // 2. Jalankan Uji Pathfinding BFS dari Spawn (2, 2) ke semua huruf dan goal
                RunPathfindingVerification();

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log($"[Fixer] Prefab {prefabPath} berhasil diperbarui dan disimpan!");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Render visual screenshot Game View Level 1 di SampleScene
            CaptureSampleSceneScreenshot();
        }

        private static void RunPathfindingVerification()
        {
            var matrix = MazeMatrixRegistry.Level1;
            Vector2Int startCell = new Vector2Int(2, 2);

            // BFS dari startCell
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

            queue.Enqueue(startCell);
            visited.Add(startCell);

            Vector2Int[] dirs = new Vector2Int[]
            {
                new Vector2Int(0, 1),
                new Vector2Int(0, -1),
                new Vector2Int(1, 0),
                new Vector2Int(-1, 0)
            };

            while (queue.Count > 0)
            {
                Vector2Int curr = queue.Dequeue();

                foreach (var d in dirs)
                {
                    Vector2Int next = curr + d;
                    if (next.x >= 0 && next.x < 29 && next.y >= 0 && next.y < 15)
                    {
                        int row = 14 - next.y;
                        if (matrix[row, next.x] == 0 && !visited.Contains(next))
                        {
                            visited.Add(next);
                            queue.Enqueue(next);
                        }
                    }
                }
            }

            Debug.Log($"[BFS Verification] Total walkable cells reachable from Start (2, 2): {visited.Count}");

            foreach (var kvp in TargetPositions)
            {
                int cellX = Mathf.FloorToInt(kvp.Value.x);
                int cellY = Mathf.FloorToInt(kvp.Value.y);
                Vector2Int letterCell = new Vector2Int(cellX, cellY);

                bool reachable = visited.Contains(letterCell);
                if (reachable)
                {
                    Debug.Log($"[BFS Verification] Huruf '{kvp.Key}' di Cell ({cellX}, {cellY}) TERHUBUNG & DAPAT DIJANGKAU PEMAIN!");
                }
                else
                {
                    Debug.LogError($"[BFS Verification] Huruf '{kvp.Key}' di Cell ({cellX}, {cellY}) TIDAK DAPAT DIJANGKAU DARI START!");
                }
            }
        }

        private static void CaptureSampleSceneScreenshot()
        {
            string scenePath = "Assets/Scenes/SampleScene.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Bersihkan instance grid lama jika ada di scene
            var existingGrids = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in existingGrids)
            {
                if (go.name.Contains("MazeGrid"))
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }

            // Instansiasi prefab Level 1 baru
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MazeGrids/Level1_MazeGrid.prefab");
            if (prefab != null)
            {
                var grid = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                grid.name = "Level1_MazeGrid";
            }

            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("[Fixer] Camera.main tidak ditemukan di SampleScene!");
                return;
            }

            // Atur posisi dan ukuran orthographic kamera agar membingkai seluruh labirin 17x11
            cam.transform.position = new Vector3(8.0f, 5.0f, -10f);
            cam.orthographic = true;
            cam.orthographicSize = 6.2f;
            cam.backgroundColor = new Color(0.56f, 0.64f, 0.36f, 1f);

            // Letakkan Player di SpawnPoint (2.5, 1.5)
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                player.transform.position = new Vector3(2.5f, 1.5f, 0f);
            }

            int width = 960;
            int height = 540;
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

            string outPath = "c:/Users/ASUS/Mazell/level1_gameview_screenshot.png";
            File.WriteAllBytes(outPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            Debug.Log($"[Fixer] Screenshot Game View terbaru berhasil disimpan ke: {outPath}");
        }
    }
}
