using System;
using UnityEngine;

namespace MazeSpell
{
    /// <summary>
    /// Registry terpusat matriks denah rumah (Top-Down House Map) untuk Level 1 s/d 5.
    /// Nilai 0 = Jalur Walkable (lantai kayu, karpet, keramik, rumput halaman, jalan paving).
    /// Nilai 1 = Dinding & Rintangan Non-walkable ber-collider (dinding luar, sekat partisi, pagar).
    /// </summary>
    public static class MazeMatrixRegistry
    {
        // ==========================================
        // LEVEL 1 (EARTH) - 20 Kolom x 16 Baris
        // ==========================================
        public static readonly int[,] Level1Matrix = new int[16, 20]
        {
            // y = 15 (Top boundary)
            { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            // y = 14 (House top wall)
            { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            // y = 13 (E room, Kitchen, Bathroom)
            { 1, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 1 },
            // y = 12
            { 1, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 1 },
            // y = 11
            { 1, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 1 },
            // y = 10 (Doorway E room at 4, Bathroom door at 16)
            { 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 1, 1, 1 },
            // y = 9 (Kitchen bottom wall, Living room, Dining T)
            { 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 1 },
            // y = 8 (Garden path on left, Living, Door to Dining at 13)
            { 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1 },
            // y = 7 (Door from garden at 6, Study door at 16)
            { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 0, 1, 1, 1 },
            // y = 6 (Garden, Living Red Rug R, Door to Study at 13, Exit right at 18,6)
            { 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
            // y = 5 (Garden, Living Red Rug R, Study H)
            { 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 1 },
            // y = 4 (Player Start at 4,4, Living bottom wall, Study bottom)
            { 1, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 1 },
            // y = 3 (Garden lawn bottom)
            { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1 },
            // y = 2 (Garden bench & path)
            { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1 },
            // y = 1 (Garden bottom edge)
            { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1 },
            // y = 0 (Bottom fence)
            { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }
        };

        // ==========================================
        // LEVEL 2 (STONE) - 18 Kolom x 16 Baris
        // ==========================================
        public static readonly int[,] Level2Matrix = new int[16, 18]
        {
            // y = 15
            { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            // y = 14 (House Top Wall)
            { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            // y = 13 (Top-Left S, Corridor, Top-Right T)
            { 1, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1, 1 },
            // y = 12
            { 1, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1, 1 },
            // y = 11 (Doors to S and T)
            { 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1 },
            // y = 10 (Partition wall)
            { 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1 },
            // y = 9 (Middle-Left O, Corridor, Middle-Right N)
            { 1, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1, 1 },
            // y = 8
            { 1, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1, 1 },
            // y = 7 (Doors to O and N)
            { 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1 },
            // y = 6 (Partition wall)
            { 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1 },
            // y = 5 (Bottom-Left E, Corridor, Bottom-Right Garage)
            { 1, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1, 1 },
            // y = 4 (Door to E, Door to Garage)
            { 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1 },
            // y = 3 (Exit at 16,3)
            { 1, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 1 },
            // y = 2 (Entrance steps Player start at 9,2)
            { 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1 },
            // y = 1 (Sidewalk)
            { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
            // y = 0
            { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }
        };

        // ==========================================
        // LEVEL 3 (GRASS) - 20 Kolom x 18 Baris
        // ==========================================
        public static readonly int[,] Level3Matrix = new int[18, 20]
        {
            // y = 17
            { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            // y = 16 (Top Wall)
            { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            // y = 15 (Top-Left G, Bathroom, Top-Right R)
            { 1, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0, 0, 0, 0, 0, 1, 1, 1 },
            // y = 14
            { 1, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0, 0, 0, 0, 0, 1, 1, 1 },
            // y = 13 (Doors to G, Bath, R)
            { 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1 },
            // y = 12 (Partition wall)
            { 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            // y = 11 (Kitchen A, Corridor, Dining S)
            { 1, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0, 0, 0, 0, 0, 1, 1, 1 },
            // y = 10
            { 1, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0, 0, 0, 0, 0, 1, 1, 1 },
            // y = 9 (Doors to A and Dining S)
            { 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1 },
            // y = 8
            { 1, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            // y = 7 (Patio garden left, Corridor, Bedroom S)
            { 1, 0, 0, 0, 0, 0, 1, 1, 0, 0, 1, 1, 0, 0, 0, 0, 0, 1, 1, 1 },
            // y = 6
            { 1, 0, 0, 0, 0, 0, 1, 1, 0, 0, 1, 1, 0, 0, 0, 0, 0, 1, 1, 1 },
            // y = 5 (Door to Bedroom S, Exit at 18,5)
            { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
            // y = 4 (Bedroom S bottom)
            { 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0, 0, 0, 0, 0, 1, 1, 1 },
            // y = 3 (Garden Entrance Player start at 9,3)
            { 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            // y = 2 (Garden path & pond)
            { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
            // y = 1
            { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
            // y = 0 (Fence)
            { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }
        };

        // ==========================================
        // LEVEL 4 (FROST) - 20 Kolom x 16 Baris
        // ==========================================
        public static readonly int[,] Level4Matrix = new int[16, 20]
        {
            // y = 15
            { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            // y = 14 (House Top Wall)
            { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            // y = 13 (Master Bedroom F, Bath R, Living O)
            { 1, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 1 },
            // y = 12
            { 1, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 1 },
            // y = 11 (Doors to F, R, O)
            { 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1 },
            // y = 10 (Partition wall)
            { 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1 },
            // y = 9 (Hallway, Living O bottom)
            { 1, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1 },
            // y = 8 (Living S, Corridor, Dining T)
            { 1, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 1 },
            // y = 7 (Door to S, Door to Dining T, Exit at 18,7)
            { 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
            // y = 6 (Living S, Dining T)
            { 1, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 1 },
            // y = 5 (Living S bottom, Dining T bottom)
            { 1, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 1 },
            // y = 4 (House bottom wall)
            { 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1 },
            // y = 3 (Driveway / Carport, Player start at 9,3)
            { 1, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1 },
            // y = 2 (Garden path)
            { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1 },
            // y = 1
            { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1 },
            // y = 0
            { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }
        };

        // ==========================================
        // LEVEL 5 (FLAME) - 22 Kolom x 16 Baris
        // ==========================================
        public static readonly int[,] Level5Matrix = new int[16, 22]
        {
            // y = 15
            { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            // y = 14 (House Top Wall)
            { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            // y = 13 (Bedroom F, Study L, Kitchen A)
            { 1, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 1 },
            // y = 12
            { 1, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 1 },
            // y = 11 (Doors to F, L, A)
            { 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1 },
            // y = 10 (Partition wall)
            { 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 1 },
            // y = 9 (Living M, Central hall / Guardian, Kitchen A bottom)
            { 1, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1 },
            // y = 8 (Living M, Corridor, Living E)
            { 1, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1 },
            // y = 7 (Door to M, Door to E, Exit at 20,7)
            { 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
            // y = 6 (Living M, Living E)
            { 1, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1 },
            // y = 5 (Living M bottom, Living E bottom)
            { 1, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1 },
            // y = 4 (House bottom wall)
            { 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            // y = 3 (Driveway / Carport, Player start at 9,3)
            { 1, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1 },
            // y = 2 (Garden path)
            { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1 },
            // y = 1
            { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1 },
            // y = 0
            { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }
        };

        public static int[,] Level1 => Level1Matrix;
        public static int[,] Level2 => Level2Matrix;
        public static int[,] Level3 => Level3Matrix;
        public static int[,] Level4 => Level4Matrix;
        public static int[,] Level5 => Level5Matrix;

        public static int[,] Level1HouseMatrix => Level1Matrix;
        public static int[,] Level2HouseMatrix => Level2Matrix;
        public static int[,] Level3HouseMatrix => Level3Matrix;
        public static int[,] Level4HouseMatrix => Level4Matrix;
        public static int[,] Level5HouseMatrix => Level5Matrix;
        public static int[,] RobberyBobMansionMatrix => Level5Matrix;

        public static int[,] GetMatrix(int level)
        {
            switch (level)
            {
                case 1: return Level1Matrix;
                case 2: return Level2Matrix;
                case 3: return Level3Matrix;
                case 4: return Level4Matrix;
                case 5: return Level5Matrix;
                default: return Level1Matrix;
            }
        }

        public static int GetWidth(int level)
        {
            var mat = GetMatrix(level);
            return mat.GetLength(1);
        }

        public static int GetHeight(int level)
        {
            var mat = GetMatrix(level);
            return mat.GetLength(0);
        }

        public static bool IsCellWithinBounds(int level, int cellX, int cellY)
        {
            int w = GetWidth(level);
            int h = GetHeight(level);
            return cellX >= 0 && cellX < w && cellY >= 0 && cellY < h;
        }

        public static bool IsCellWalkable(int level, int cellX, int cellY)
        {
            if (!IsCellWithinBounds(level, cellX, cellY)) return false;
            var mat = GetMatrix(level);
            int h = mat.GetLength(0);
            int row = (h - 1) - cellY;
            return mat[row, cellX] == 0;
        }

        public static Vector2Int WorldToCell(Vector3 worldPos)
        {
            return new Vector2Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y));
        }

        public static Vector3 CellToWorldCenter(int cellX, int cellY, float z = -0.1f)
        {
            return new Vector3(cellX + 0.5f, cellY + 0.5f, z);
        }
    }
}
