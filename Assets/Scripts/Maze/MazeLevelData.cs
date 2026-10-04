using UnityEngine;

namespace MazeSpell
{
    [CreateAssetMenu(fileName = "NewMazeLevelData", menuName = "MazeSpell/Level Data", order = 1)]
    public class MazeLevelData : ScriptableObject
    {
        [Header("Level Info")]
        [Tooltip("Nomor level (1 - 5)")]
        public int levelNumber = 1;

        [Tooltip("Tema visual level")]
        public string themeName = "Ground / Earth";

        [Header("Grid Dimensions (X = Kolom, Y = Baris)")]
        [Tooltip("Jumlah kolom horizontal (X)")]
        public int columns = 9;

        [Tooltip("Jumlah baris vertikal (Y)")]
        public int rows = 17;

        [Tooltip("Ukuran tiap tile cell dalam unit Unity")]
        public Vector2 cellSize = Vector2.one;

        [Header("Visual Guidelines")]
        [TextArea(2, 4)]
        public string pathDescription;

        [TextArea(2, 4)]
        public string wallDescription;

        /// <summary>
        /// Total lebar maze dalam unit dunia
        /// </summary>
        public float TotalWidth => columns * cellSize.x;

        /// <summary>
        /// Total tinggi maze dalam unit dunia
        /// </summary>
        public float TotalHeight => rows * cellSize.y;

        /// <summary>
        /// Titik tengah maze relatif terhadap titik awal (0,0) sudut kiri-bawah
        /// </summary>
        public Vector3 GetCenterOffset()
        {
            return new Vector3((TotalWidth - cellSize.x) * 0.5f, (TotalHeight - cellSize.y) * 0.5f, 0f);
        }

        /// <summary>
        /// Menghitung orthographic size kamera yang ideal agar seluruh maze terlihat pas di layar
        /// </summary>
        public float CalculateOrthographicSize(float targetAspectRatio = 16f / 9f, float padding = 1.0f)
        {
            float halfHeight = (TotalHeight * 0.5f) + padding;
            float halfWidthNeeded = ((TotalWidth * 0.5f) + padding) / targetAspectRatio;
            return Mathf.Max(halfHeight, halfWidthNeeded);
        }
    }
}
