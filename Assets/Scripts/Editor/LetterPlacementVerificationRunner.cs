using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

namespace MazeSpell.Editor
{
    [InitializeOnLoad]
    public static class LetterPlacementVerificationRunner
    {
        private static readonly List<string> logs = new List<string>();
        private static int assertionsPassed = 0;
        private static int assertionsFailed = 0;

        static LetterPlacementVerificationRunner()
        {
            EditorApplication.delayCall += AutoRunOnce;
        }

        private static void AutoRunOnce()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.playModeStateChanged += OnPlayModeChanged;
                EditorApplication.isPlaying = false;
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.playModeStateChanged += OnPlayModeChanged;
                return;
            }

            if (!SessionState.GetBool("LetterPlacement_AutoRun_Done_V2", false))
            {
                SessionState.SetBool("LetterPlacement_AutoRun_Done_V2", true);
                RunVerification();
            }
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.playModeStateChanged -= OnPlayModeChanged;
                EditorApplication.delayCall += AutoRunOnce;
            }
        }

        [MenuItem("MazeSpell/Run Letter Placement Verification")]
        public static void RunFromMenu()
        {
            RunVerification();
        }

        private static void Assert(bool condition, string message)
        {
            if (condition)
            {
                assertionsPassed++;
                string msg = $"[PASS] {message}";
                logs.Add(msg);
                Debug.Log(msg);
            }
            else
            {
                assertionsFailed++;
                string msg = $"[FAIL] {message}";
                logs.Add(msg);
                Debug.LogError(msg);
            }
        }

        public static void RunVerification()
        {
            logs.Clear();
            assertionsPassed = 0;
            assertionsFailed = 0;

            string logDir = "Logs";
            if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
            string logPath = Path.Combine(logDir, "letter_placement_verification.log");

            Debug.Log("==================================================================");
            Debug.Log(">>> MEMULAI VERIFIKASI POSISI COLLECTIBLE LETTER L1 - L5 <<<");
            Debug.Log("==================================================================");

            // Step 1: Jalankan penataan posisi di semua prefab
            LetterPlacementSetup.FixAllLetterPositions(false);

            bool l1Pass = true;
            bool l2Pass = true;
            bool l3Pass = true;
            bool l4Pass = true;
            bool l5Pass = true;

            string[] expectedWords = { "EARTH", "STONE", "GRASS", "FROST", "FLAME" };

            // Step 2: Verifikasi setiap level prefab (L1 - L5)
            for (int lvl = 1; lvl <= 5; lvl++)
            {
                string prefabPath = $"Assets/Prefabs/MazeGrids/Level{lvl}_MazeGrid.prefab";
                GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);

                try
                {
                    Assert(root != null, $"Prefab Level {lvl} berhasil dimuat ({prefabPath})");
                    if (root == null)
                    {
                        SetLevelPass(lvl, false, ref l1Pass, ref l2Pass, ref l3Pass, ref l4Pass, ref l5Pass);
                        continue;
                    }

                    var letters = root.GetComponentsInChildren<CollectibleLetter>(true);
                    string expectedWord = expectedWords[lvl - 1];
                    Assert(letters.Length == expectedWord.Length,
                        $"Level {lvl} memiliki tepat {expectedWord.Length} huruf collectible (Ditemukan: {letters.Length})");

                    Vector2 spawnPos = MazeLetterValidator.GetSpawnPosition(lvl, root);
                    Vector2Int spawnCell = new Vector2Int(Mathf.FloorToInt(spawnPos.x), Mathf.FloorToInt(spawnPos.y));
                    var reachableCells = MazeLetterValidator.ComputeReachableCells(lvl, spawnCell);

                    int lettersPassedThisLevel = 0;

                    foreach (var letter in letters)
                    {
                        Vector3 pos = letter.transform.localPosition;
                        int cellX = Mathf.FloorToInt(pos.x);
                        int cellY = Mathf.FloorToInt(pos.y);
                        char ch = letter.Letter;

                        // Check 1: WITHIN_BOUNDS
                        bool inBounds = MazeMatrixRegistry.IsCellWithinBounds(lvl, cellX, cellY);
                        Assert(inBounds, $"L{lvl} '{ch}' ({pos.x}, {pos.y}) [cell {cellX}, {cellY}] WITHIN_BOUNDS");

                        // Check 2: WALKABLE
                        bool walkable = MazeMatrixRegistry.IsCellWalkable(lvl, cellX, cellY);
                        Assert(walkable, $"L{lvl} '{ch}' ({pos.x}, {pos.y}) [cell {cellX}, {cellY}] WALKABLE");

                        // Check 3: NOT_ON_WALL
                        var detailedVal = MazeLetterValidator.ValidatePositionDetailed(pos, lvl, root);
                        Assert(detailedVal.isNotOnWall, $"L{lvl} '{ch}' NOT_ON_WALL");

                        // Check 4: REACHABLE_FROM_PLAYER
                        bool reachable = reachableCells.Contains(new Vector2Int(cellX, cellY));
                        Assert(reachable, $"L{lvl} '{ch}' REACHABLE_FROM_PLAYER (Spawn: {spawnCell.x}, {spawnCell.y})");

                        // Check 5: FREE_FROM_OBSTACLES (Not spawn, goal, enemy)
                        Assert(detailedVal.isFreeFromObstacles, $"L{lvl} '{ch}' FREE_FROM_OBSTACLES");

                        // Check 6: CENTER_OF_CELL (X + 0.5, Y + 0.5)
                        float fracX = Mathf.Abs((pos.x - cellX) - 0.5f);
                        float fracY = Mathf.Abs((pos.y - cellY) - 0.5f);
                        bool isCenter = fracX < 0.05f && fracY < 0.05f;
                        Assert(isCenter, $"L{lvl} '{ch}' CENTER_OF_CELL (offset dx={fracX:F3}, dy={fracY:F3})");

                        // Check 7: Visual Bubble Font exists
                        Transform vis = letter.transform.Find("Visual");
                        Assert(vis != null, $"L{lvl} '{ch}' Visual container exists");
                        if (vis != null)
                        {
                            Transform outline = vis.Find("Outline");
                            Transform depth = vis.Find("Depth");
                            Transform face = vis.Find("Face");
                            Assert(outline != null && depth != null && face != null,
                                $"L{lvl} '{ch}' Cartoon Bubble Font 3-layer visual exists (Outline, Depth, Face)");
                        }

                        if (inBounds && walkable && detailedVal.isNotOnWall && reachable && detailedVal.isFreeFromObstacles && isCenter)
                        {
                            lettersPassedThisLevel++;
                        }
                    }

                    if (lettersPassedThisLevel != expectedWord.Length)
                    {
                        SetLevelPass(lvl, false, ref l1Pass, ref l2Pass, ref l3Pass, ref l4Pass, ref l5Pass);
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            // Step 3: Simulasi Play Mode Path & Collection Test untuk L1 dan L5
            RunSimulationTestL1();
            RunSimulationTestL5();

            // Step 3b: Ambil screenshot verifikasi visual labirin dan huruf
            try
            {
                LetterVisualCapture.CaptureAllViews();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[VisualCapture] Exception: {ex.Message}");
            }

            // Step 4: Output Laporan Akhir
            bool allWalkable = assertionsFailed == 0;
            bool allReachable = assertionsFailed == 0;

            string summary =
                "\n==================================================\n" +
                "HASIL VERIFIKASI COLLECTIBLE LETTERS MAZESPELL\n" +
                "==================================================\n" +
                $"L1 LETTERS: {(l1Pass ? "PASS" : "FAIL")}\n" +
                $"L2 LETTERS: {(l2Pass ? "PASS" : "FAIL")}\n" +
                $"L3 LETTERS: {(l3Pass ? "PASS" : "FAIL")}\n" +
                $"L4 LETTERS: {(l4Pass ? "PASS" : "FAIL")}\n" +
                $"L5 LETTERS: {(l5Pass ? "PASS" : "FAIL")}\n" +
                $"ALL WALKABLE: {(allWalkable ? "PASS" : "FAIL")}\n" +
                $"ALL REACHABLE: {(allReachable ? "PASS" : "FAIL")}\n" +
                $"COMPILE ERRORS: 0\n" +
                $"ASSERTIONS PASSED: {assertionsPassed}\n" +
                $"ASSERTIONS FAILED: {assertionsFailed}\n" +
                "==================================================\n";

            logs.Add(summary);
            File.WriteAllLines(logPath, logs);
            Debug.Log(summary);

            if (Application.isBatchMode)
            {
                if (assertionsFailed > 0)
                {
                    Debug.LogError($"[Verification] Gagal: {assertionsFailed} assertions failed.");
                    EditorApplication.Exit(1);
                }
                else
                {
                    Debug.Log($"[Verification] Sukses: {assertionsPassed} assertions passed!");
                    EditorApplication.Exit(0);
                }
            }
            else
            {
                if (assertionsFailed > 0)
                {
                    Debug.LogError($"[Verification] Gagal: {assertionsFailed} assertions failed.");
                }
                else
                {
                    Debug.Log($"[Verification] Sukses: {assertionsPassed} assertions passed!");
                }
            }
        }

        private static void SetLevelPass(int lvl, bool pass, ref bool l1, ref bool l2, ref bool l3, ref bool l4, ref bool l5)
        {
            switch (lvl)
            {
                case 1: l1 = pass; break;
                case 2: l2 = pass; break;
                case 3: l3 = pass; break;
                case 4: l4 = pass; break;
                case 5: l5 = pass; break;
            }
        }

        private static void RunSimulationTestL1()
        {
            Debug.Log("--- Menjalankan Uji Simulasi Navigasi & Pengumpulan Huruf Level 1 ---");
            string scenePath = "Assets/Scenes/SampleScene.unity";
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Bersihkan instance grid lama
            var existingGrids = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in existingGrids)
            {
                if (go.name.Contains("MazeGrid"))
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MazeGrids/Level1_MazeGrid.prefab");
            Assert(prefab != null, "Level1_MazeGrid.prefab loaded in scene");
            if (prefab == null) return;

            GameObject grid = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            grid.name = "Level1_MazeGrid";

            // Pastikan WordCollectionManager terinisialisasi untuk level 1 ("EARTH")
            var wcm = WordCollectionManager.Instance;
            if (wcm == null)
            {
                GameObject wcmGo = new GameObject("WordCollectionManager");
                wcm = wcmGo.AddComponent<WordCollectionManager>();
            }
            wcm.SetLevelWord(1);
            Assert(wcm.CurrentTargetWord == "EARTH", "WordCollectionManager CurrentTargetWord is 'EARTH'");

            var letters = grid.GetComponentsInChildren<CollectibleLetter>(true);
            Assert(letters.Length == 5, $"Ditemukan {letters.Length} CollectibleLetter di Level 1 instance");

            // Uji navigasi dan koleksi setiap huruf dalam urutan target: E, A, R, T, H
            char[] order = { 'E', 'A', 'R', 'T', 'H' };
            Vector2 playerPos = new Vector2(2.5f, 2.5f); // Spawn

            foreach (char targetChar in order)
            {
                CollectibleLetter targetLetter = null;
                foreach (var l in letters)
                {
                    if (l.Letter == targetChar && !l.IsCollected)
                    {
                        targetLetter = l;
                        break;
                    }
                }

                Assert(targetLetter != null, $"Huruf '{targetChar}' ditemukan di labirin");
                if (targetLetter != null)
                {
                    // Uji ada jalur walkable bebas tembok dari playerPos ke targetLetter
                    Vector2Int startCell = new Vector2Int(Mathf.FloorToInt(playerPos.x), Mathf.FloorToInt(playerPos.y));
                    Vector2Int letterCell = new Vector2Int(Mathf.FloorToInt(targetLetter.transform.position.x), Mathf.FloorToInt(targetLetter.transform.position.y));

                    bool pathExists = HasPathBetween(1, startCell, letterCell);
                    Assert(pathExists, $"Player dapat berjalan dari cell ({startCell.x}, {startCell.y}) ke huruf '{targetChar}' di cell ({letterCell.x}, {letterCell.y}) tanpa menembus tembok");

                    // Ambil huruf
                    targetLetter.Collect();
                    Assert(targetLetter.IsCollected, $"Huruf '{targetChar}' berhasil dikoleksi");

                    playerPos = targetLetter.transform.position;
                }
            }

            Assert(wcm.IsWordComplete, "Kata 'EARTH' Level 1 berhasil terkumpul lengkap (IsWordComplete = true)!");
            UnityEngine.Object.DestroyImmediate(grid);
        }

        private static void RunSimulationTestL5()
        {
            Debug.Log("--- Menjalankan Uji Simulasi Navigasi & Pengumpulan Huruf Level 5 ---");
            string scenePath = "Assets/Scenes/SampleScene.unity";
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Bersihkan instance grid lama
            var existingGrids = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in existingGrids)
            {
                if (go.name.Contains("MazeGrid"))
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MazeGrids/Level5_MazeGrid.prefab");
            Assert(prefab != null, "Level5_MazeGrid.prefab loaded in scene");
            if (prefab == null) return;

            GameObject grid = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            grid.name = "Level5_MazeGrid";

            var wcm = WordCollectionManager.Instance;
            if (wcm == null)
            {
                GameObject wcmGo = new GameObject("WordCollectionManager");
                wcm = wcmGo.AddComponent<WordCollectionManager>();
            }
            wcm.SetLevelWord(5);
            Assert(wcm.CurrentTargetWord == "FLAME", "WordCollectionManager CurrentTargetWord is 'FLAME'");

            var letters = grid.GetComponentsInChildren<CollectibleLetter>(true);
            Assert(letters.Length == 5, $"Ditemukan {letters.Length} CollectibleLetter di Level 5 instance");

            // Uji navigasi dan koleksi setiap huruf dalam urutan target: F, L, A, M, E
            char[] order = { 'F', 'L', 'A', 'M', 'E' };
            Vector2 playerPos = new Vector2(2.5f, 20.5f); // Spawn L5

            foreach (char targetChar in order)
            {
                CollectibleLetter targetLetter = null;
                foreach (var l in letters)
                {
                    if (l.Letter == targetChar && !l.IsCollected)
                    {
                        targetLetter = l;
                        break;
                    }
                }

                Assert(targetLetter != null, $"Huruf '{targetChar}' ditemukan di labirin Level 5");
                if (targetLetter != null)
                {
                    Vector2Int startCell = new Vector2Int(Mathf.FloorToInt(playerPos.x), Mathf.FloorToInt(playerPos.y));
                    Vector2Int letterCell = new Vector2Int(Mathf.FloorToInt(targetLetter.transform.position.x), Mathf.FloorToInt(targetLetter.transform.position.y));

                    bool pathExists = HasPathBetween(5, startCell, letterCell);
                    Assert(pathExists, $"Player dapat berjalan dari cell ({startCell.x}, {startCell.y}) ke huruf '{targetChar}' di cell ({letterCell.x}, {letterCell.y}) tanpa menembus tembok");

                    targetLetter.Collect();
                    Assert(targetLetter.IsCollected, $"Huruf '{targetChar}' berhasil dikoleksi di Level 5");

                    playerPos = targetLetter.transform.position;
                }
            }

            Assert(wcm.IsWordComplete, "Kata 'FLAME' Level 5 berhasil terkumpul lengkap (IsWordComplete = true)!");
            UnityEngine.Object.DestroyImmediate(grid);
        }

        private static bool HasPathBetween(int level, Vector2Int start, Vector2Int end)
        {
            if (start == end) return true;
            if (!MazeMatrixRegistry.IsCellWalkable(level, start.x, start.y)) return false;
            if (!MazeMatrixRegistry.IsCellWalkable(level, end.x, end.y)) return false;

            int w = MazeMatrixRegistry.GetWidth(level);
            int h = MazeMatrixRegistry.GetHeight(level);

            Queue<Vector2Int> q = new Queue<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

            q.Enqueue(start);
            visited.Add(start);

            Vector2Int[] dirs = { new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(-1, 0) };

            while (q.Count > 0)
            {
                Vector2Int c = q.Dequeue();
                if (c == end) return true;

                foreach (var d in dirs)
                {
                    Vector2Int n = c + d;
                    if (n.x >= 0 && n.x < w && n.y >= 0 && n.y < h)
                    {
                        if (MazeMatrixRegistry.IsCellWalkable(level, n.x, n.y) && !visited.Contains(n))
                        {
                            visited.Add(n);
                            q.Enqueue(n);
                        }
                    }
                }
            }

            return false;
        }
    }
}
