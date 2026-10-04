using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MazeSpell
{
    /// <summary>
    /// Sistem validasi anti-wall dan penempatan collectible letter.
    /// Memastikan seluruh huruf berada tepat di tengah cell walkable,
    /// terhubung (reachable) dari posisi Spawn Player melalui BFS,
    /// serta tidak bertumpuk dengan wall, obstacle, spawn, goal, atau enemy.
    /// </summary>
    public static class MazeLetterValidator
    {
        public struct ValidationResult
        {
            public bool isWalkable;
            public bool isWithinBounds;
            public bool isNotOnWall;
            public bool isReachable;
            public bool isFreeFromObstacles;
            public string failureReason;

            public bool IsValid => isWalkable && isWithinBounds && isNotOnWall && isReachable && isFreeFromObstacles;
        }

        /// <summary>
        /// Mengambil nomor level dari objek labirin atau LevelProgressionManager.
        /// </summary>
        public static int InferLevel(GameObject contextObj = null)
        {
            if (contextObj != null)
            {
                Transform curr = contextObj.transform;
                while (curr != null)
                {
                    string name = curr.name;
                    for (int lvl = 1; lvl <= 5; lvl++)
                    {
                        if (name.Contains($"Level{lvl}_") || name.Equals($"Level{lvl}"))
                        {
                            return lvl;
                        }
                    }
                    curr = curr.parent;
                }
            }

            if (LevelProgressionManager.HasInstance && LevelProgressionManager.Instance != null)
            {
                return LevelProgressionManager.Instance.CurrentLevel;
            }

            return 1;
        }

        /// <summary>
        /// Mengecek apakah suatu posisi world adalah walkable, valid, dan reachable.
        /// </summary>
        public static bool IsLetterPositionWalkable(Vector3 worldPos, int level, out string failureReason)
        {
            var res = ValidatePositionDetailed(worldPos, level, null);
            failureReason = res.failureReason;
            return res.IsValid;
        }

        /// <summary>
        /// Mengecek posisi CollectibleLetter saat ini.
        /// </summary>
        public static bool IsLetterPositionWalkable(CollectibleLetter letter, out string failureReason)
        {
            if (letter == null)
            {
                failureReason = "Letter component is null";
                return false;
            }

            int level = InferLevel(letter.gameObject);
            var res = ValidatePositionDetailed(letter.transform.position, level, letter.gameObject);
            failureReason = res.failureReason;
            return res.IsValid;
        }

        /// <summary>
        /// Evaluasi komprehensif posisi letter (Walkable, Bounds, Wall, Reachable, Obstacle).
        /// </summary>
        public static ValidationResult ValidatePositionDetailed(Vector3 worldPos, int level, GameObject letterContext = null)
        {
            ValidationResult res = new ValidationResult
            {
                isWalkable = false,
                isWithinBounds = false,
                isNotOnWall = false,
                isReachable = false,
                isFreeFromObstacles = false,
                failureReason = ""
            };

            int cellX = Mathf.FloorToInt(worldPos.x);
            int cellY = Mathf.FloorToInt(worldPos.y);

            // 1. Cek Bounds
            if (!MazeMatrixRegistry.IsCellWithinBounds(level, cellX, cellY))
            {
                res.failureReason = $"Di luar batas labirin Level {level} (Cell: {cellX}, {cellY})";
                return res;
            }
            res.isWithinBounds = true;

            // 2. Cek Matriks Walkable
            if (!MazeMatrixRegistry.IsCellWalkable(level, cellX, cellY))
            {
                res.failureReason = $"Cell ({cellX}, {cellY}) bernilai WALL/OBSTACLE pada matriks Level {level}";
                return res;
            }
            res.isWalkable = true;

            // 3. Cek Tilemap Walls fisik jika objek labirin ada
            Tilemap wallsTilemap = FindWallsTilemap(level, letterContext);
            if (wallsTilemap != null)
            {
                Vector3Int tilePos = new Vector3Int(cellX, cellY, 0);
                if (wallsTilemap.HasTile(tilePos))
                {
                    res.failureReason = $"Cell ({cellX}, {cellY}) memiliki tile dinding pada Walls_Obstacles";
                    return res;
                }
            }
            res.isNotOnWall = true;

            // 4. Cek Tumpang tindih dengan Spawn, Goal, atau Enemy
            Vector2 spawnPos = GetSpawnPosition(level, letterContext);
            int spawnCellX = Mathf.FloorToInt(spawnPos.x);
            int spawnCellY = Mathf.FloorToInt(spawnPos.y);

            Vector2 goalPos = GetGoalPosition(level, letterContext);
            int goalCellX = Mathf.FloorToInt(goalPos.x);
            int goalCellY = Mathf.FloorToInt(goalPos.y);

            if (cellX == spawnCellX && cellY == spawnCellY)
            {
                res.failureReason = $"Cell ({cellX}, {cellY}) bertumpuk dengan posisi SpawnPoint";
                return res;
            }

            if (cellX == goalCellX && cellY == goalCellY)
            {
                res.failureReason = $"Cell ({cellX}, {cellY}) bertumpuk dengan posisi Goal/ExitGate";
                return res;
            }

            var enemies = GetEnemyCells(level, letterContext);
            foreach (var enCell in enemies)
            {
                if (cellX == enCell.x && cellY == enCell.y)
                {
                    res.failureReason = $"Cell ({cellX}, {cellY}) bertumpuk dengan Enemy";
                    return res;
                }
            }
            res.isFreeFromObstacles = true;

            // 5. Cek Reachable dari Player Spawn via BFS flood fill
            HashSet<Vector2Int> reachableSet = ComputeReachableCells(level, new Vector2Int(spawnCellX, spawnCellY));
            if (!reachableSet.Contains(new Vector2Int(cellX, cellY)))
            {
                res.failureReason = $"Cell ({cellX}, {cellY}) terisolasi dan tidak dapat dijangkau dari Spawn ({spawnCellX}, {spawnCellY})";
                return res;
            }
            res.isReachable = true;

            return res;
        }

        /// <summary>
        /// Validasi posisi letter. Jika ditemukan di wall/obstacle/unreachable/out of bounds,
        /// otomatis memindahkannya ke CENTER cell walkable terdekat yang reachable dan me-log warning.
        /// </summary>
        public static bool ValidateAndFixLetterPosition(CollectibleLetter letter)
        {
            if (letter == null) return false;

            int level = InferLevel(letter.gameObject);
            Vector3 currentPos = letter.transform.position;
            var valResult = ValidatePositionDetailed(currentPos, level, letter.gameObject);

            if (valResult.IsValid)
            {
                // Pastikan koordinat tersnap rapi di cell center (cellX + 0.5, cellY + 0.5, -0.1f)
                int cx = Mathf.FloorToInt(currentPos.x);
                int cy = Mathf.FloorToInt(currentPos.y);
                Vector3 snappedCenter = MazeMatrixRegistry.CellToWorldCenter(cx, cy, currentPos.z);
                if (Vector2.Distance(new Vector2(currentPos.x, currentPos.y), new Vector2(snappedCenter.x, snappedCenter.y)) > 0.01f)
                {
                    letter.transform.position = snappedCenter;
                }
                return true;
            }

            // Temukan cell walkable terdekat yang reachable
            Vector3 oldPos = currentPos;
            Vector2 spawnPos = GetSpawnPosition(level, letter.gameObject);
            Vector2Int spawnCell = new Vector2Int(Mathf.FloorToInt(spawnPos.x), Mathf.FloorToInt(spawnPos.y));
            HashSet<Vector2Int> reachableCells = ComputeReachableCells(level, spawnCell);

            // Kumpulkan cell terlarang (Spawn, Goal, Enemy, dan huruf lain yang sudah valid)
            HashSet<Vector2Int> blockedCells = new HashSet<Vector2Int>();
            blockedCells.Add(spawnCell);

            Vector2 goalPos = GetGoalPosition(level, letter.gameObject);
            blockedCells.Add(new Vector2Int(Mathf.FloorToInt(goalPos.x), Mathf.FloorToInt(goalPos.y)));

            foreach (var en in GetEnemyCells(level, letter.gameObject))
            {
                blockedCells.Add(en);
            }

            // Cek huruf lain di kontainer agar tidak saling bertumpuk
            if (letter.transform.parent != null)
            {
                foreach (Transform sibling in letter.transform.parent)
                {
                    if (sibling != letter.transform && sibling.GetComponent<CollectibleLetter>() != null)
                    {
                        blockedCells.Add(new Vector2Int(Mathf.FloorToInt(sibling.position.x), Mathf.FloorToInt(sibling.position.y)));
                    }
                }
            }

            // Cari cell di reachableCells yang terdekat dengan oldPos dan tidak ada di blockedCells
            Vector2 targetRef = new Vector2(oldPos.x, oldPos.y);
            Vector2Int bestCell = spawnCell;
            float bestDistSq = float.MaxValue;

            foreach (var cell in reachableCells)
            {
                if (blockedCells.Contains(cell)) continue;

                Vector2 center = new Vector2(cell.x + 0.5f, cell.y + 0.5f);
                float distSq = (center - targetRef).sqrMagnitude;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestCell = cell;
                }
            }

            Vector3 newCenterPos = MazeMatrixRegistry.CellToWorldCenter(bestCell.x, bestCell.y, -0.1f);

            // Pindahkan ke center cell yang valid
            if (letter.transform.parent != null)
            {
                // Jika child dari GameplayObjects/Letters, transform.position atau localPosition
                letter.transform.localPosition = new Vector3(bestCell.x + 0.5f, bestCell.y + 0.5f, -0.1f);
            }
            else
            {
                letter.transform.position = newCenterPos;
            }

            Debug.LogWarning($"[AntiWall] Huruf '{letter.Letter}' terdeteksi tidak valid ({valResult.failureReason}). " +
                             $"Posisi lama: {oldPos} -> Dipindahkan ke CENTER cell terdekat yang walkable: {newCenterPos} (Cell: {bestCell.x}, {bestCell.y})");

            return false;
        }

        /// <summary>
        /// Validasi seluruh letter yang ada di dalam hierarki grid.
        /// </summary>
        public static int ValidateAllLettersInGrid(GameObject gridRoot, int levelNumber = -1)
        {
            if (gridRoot == null) return 0;
            if (levelNumber <= 0) levelNumber = InferLevel(gridRoot);

            var letters = gridRoot.GetComponentsInChildren<CollectibleLetter>(true);
            int fixedCount = 0;

            foreach (var l in letters)
            {
                bool wasValid = ValidateAndFixLetterPosition(l);
                if (!wasValid)
                {
                    fixedCount++;
                }
            }

            return fixedCount;
        }

        /// <summary>
        /// Menghitung seluruh cell walkable yang terhubung dengan spawn menggunakan BFS flood-fill.
        /// </summary>
        public static HashSet<Vector2Int> ComputeReachableCells(int level, Vector2Int startCell)
        {
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            int w = MazeMatrixRegistry.GetWidth(level);
            int h = MazeMatrixRegistry.GetHeight(level);

            if (!MazeMatrixRegistry.IsCellWalkable(level, startCell.x, startCell.y))
            {
                // Jika startCell sendiri di wall, cari titik walkable terdekat sebagai fallback
                for (int r = 1; r < 5; r++)
                {
                    bool found = false;
                    for (int dx = -r; dx <= r && !found; dx++)
                    {
                        for (int dy = -r; dy <= r && !found; dy++)
                        {
                            Vector2Int cand = new Vector2Int(startCell.x + dx, startCell.y + dy);
                            if (MazeMatrixRegistry.IsCellWalkable(level, cand.x, cand.y))
                            {
                                startCell = cand;
                                found = true;
                            }
                        }
                    }
                    if (found) break;
                }
            }

            Queue<Vector2Int> queue = new Queue<Vector2Int>();
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
                    if (next.x >= 0 && next.x < w && next.y >= 0 && next.y < h)
                    {
                        if (MazeMatrixRegistry.IsCellWalkable(level, next.x, next.y) && !visited.Contains(next))
                        {
                            visited.Add(next);
                            queue.Enqueue(next);
                        }
                    }
                }
            }

            return visited;
        }

        private static Tilemap FindWallsTilemap(int level, GameObject context)
        {
            if (context != null)
            {
                Transform curr = context.transform;
                while (curr != null)
                {
                    Transform walls = curr.Find("Walls_Obstacles");
                    if (walls != null)
                    {
                        var tm = walls.GetComponent<Tilemap>();
                        if (tm != null) return tm;
                    }
                    curr = curr.parent;
                }
            }

            GameObject gridGo = GameObject.Find($"Level{level}_MazeGrid");
            if (gridGo != null)
            {
                Transform walls = gridGo.transform.Find("Walls_Obstacles");
                if (walls != null) return walls.GetComponent<Tilemap>();
            }

            return null;
        }

        public static Vector2 GetSpawnPosition(int level, GameObject context = null)
        {
            if (context != null)
            {
                Transform curr = context.transform;
                while (curr != null)
                {
                    Transform sp = curr.Find("GameplayObjects/SpawnPoint");
                    if (sp == null) sp = curr.Find("SpawnPoint");
                    if (sp != null) return sp.position;
                    curr = curr.parent;
                }
            }

            GameObject gridGo = GameObject.Find($"Level{level}_MazeGrid");
            if (gridGo != null)
            {
                Transform sp = gridGo.transform.Find("GameplayObjects/SpawnPoint");
                if (sp == null) sp = gridGo.transform.Find("SpawnPoint");
                if (sp != null) return sp.position;
            }

            if (level >= 1 && level <= MazeGameplayManager.AllLevelsData.Length)
            {
                return MazeGameplayManager.AllLevelsData[level - 1].spawnPosition;
            }

            return new Vector2(2.5f, 2.5f);
        }

        public static Vector2 GetGoalPosition(int level, GameObject context = null)
        {
            if (context != null)
            {
                Transform curr = context.transform;
                while (curr != null)
                {
                    Transform g = curr.Find("GameplayObjects/Goal");
                    if (g == null) g = curr.Find("Goal");
                    if (g != null) return g.position;
                    curr = curr.parent;
                }
            }

            GameObject gridGo = GameObject.Find($"Level{level}_MazeGrid");
            if (gridGo != null)
            {
                Transform g = gridGo.transform.Find("GameplayObjects/Goal");
                if (g == null) g = gridGo.transform.Find("Goal");
                if (g != null) return g.position;
            }

            if (level >= 1 && level <= MazeGameplayManager.AllLevelsData.Length)
            {
                return MazeGameplayManager.AllLevelsData[level - 1].goalPosition;
            }

            return new Vector2(27.5f, 3.5f);
        }

        public static List<Vector2Int> GetEnemyCells(int level, GameObject context = null)
        {
            List<Vector2Int> enemyCells = new List<Vector2Int>();

            if (context != null)
            {
                Transform curr = context.transform;
                while (curr != null)
                {
                    Transform enemies = curr.Find("GameplayObjects/Enemies");
                    if (enemies != null)
                    {
                        foreach (Transform e in enemies)
                        {
                            enemyCells.Add(new Vector2Int(Mathf.FloorToInt(e.position.x), Mathf.FloorToInt(e.position.y)));
                        }
                        if (enemyCells.Count > 0) return enemyCells;
                    }
                    curr = curr.parent;
                }
            }

            GameObject gridGo = GameObject.Find($"Level{level}_MazeGrid");
            if (gridGo != null)
            {
                Transform enemies = gridGo.transform.Find("GameplayObjects/Enemies");
                if (enemies != null)
                {
                    foreach (Transform e in enemies)
                    {
                        enemyCells.Add(new Vector2Int(Mathf.FloorToInt(e.position.x), Mathf.FloorToInt(e.position.y)));
                    }
                    if (enemyCells.Count > 0) return enemyCells;
                }
            }

            if (level >= 1 && level <= MazeGameplayManager.AllLevelsData.Length)
            {
                foreach (var pos in MazeGameplayManager.AllLevelsData[level - 1].hazardPositions)
                {
                    enemyCells.Add(new Vector2Int(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y)));
                }
            }

            return enemyCells;
        }
    }
}
