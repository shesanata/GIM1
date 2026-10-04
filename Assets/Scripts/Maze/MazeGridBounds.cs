using UnityEngine;

namespace MazeSpell
{
    /// <summary>
    /// Menampilkan garis panduan visual (Gizmos) di Scene View Unity
    /// untuk batas grid maze sesuai kolom (X) dan baris (Y) masing-masing level.
    /// </summary>
    [ExecuteAlways]
    public class MazeGridBounds : MonoBehaviour
    {
        [Header("Konfigurasi Data Level")]
        [Tooltip("Referensi ke data spesifikasi level (opsional)")]
        [SerializeField] private MazeLevelData levelData;

        [Header("Dimensi Manual (jika LevelData kosong)")]
        [Tooltip("Jumlah kolom (X)")]
        [SerializeField] private int columns = 9;

        [Tooltip("Jumlah baris (Y)")]
        [SerializeField] private int rows = 17;

        [Tooltip("Ukuran per cell")]
        [SerializeField] private Vector2 cellSize = Vector2.one;

        [Header("Pengaturan Tampilan Gizmo")]
        [SerializeField] private Color outerBorderColor = new Color(0.2f, 0.9f, 0.3f, 0.9f);
        [SerializeField] private Color innerGridColor = new Color(0.8f, 0.8f, 0.8f, 0.15f);
        [SerializeField] private Color centerMarkerColor = new Color(1f, 0.8f, 0.1f, 0.8f);
        [SerializeField] private bool showInnerGridLines = true;

        public int Columns => levelData != null ? levelData.columns : columns;
        public int Rows => levelData != null ? levelData.rows : rows;
        public Vector2 CellSize => levelData != null ? levelData.cellSize : cellSize;

        public float TotalWidth => Columns * CellSize.x;
        public float TotalHeight => Rows * CellSize.y;

        public Vector3 CenterPoint
        {
            get
            {
                Vector3 origin = transform.position;
                return origin + new Vector3(TotalWidth * 0.5f, TotalHeight * 0.5f, 0f);
            }
        }

        public void SetLevelData(MazeLevelData data)
        {
            levelData = data;
            if (data != null)
            {
                columns = data.columns;
                rows = data.rows;
                cellSize = data.cellSize;
            }
        }

        private void OnDrawGizmos()
        {
            DrawMazeBoundsGizmo(false);
        }

        private void OnDrawGizmosSelected()
        {
            DrawMazeBoundsGizmo(true);
        }

        private void DrawMazeBoundsGizmo(bool isSelected)
        {
            int cols = Columns;
            int rws = Rows;
            Vector2 size = CellSize;

            if (cols <= 0 || rws <= 0) return;

            Vector3 origin = transform.position;
            float w = cols * size.x;
            float h = rws * size.y;

            // Gambar garis kisi-kisi (inner grid lines) jika dipilih atau diaktifkan
            if (showInnerGridLines)
            {
                Gizmos.color = isSelected ? new Color(innerGridColor.r, innerGridColor.g, innerGridColor.b, 0.35f) : innerGridColor;

                // Garis vertikal (kolom)
                for (int x = 1; x < cols; x++)
                {
                    float posX = origin.x + (x * size.x);
                    Gizmos.DrawLine(new Vector3(posX, origin.y, 0f), new Vector3(posX, origin.y + h, 0f));
                }

                // Garis horizontal (baris)
                for (int y = 1; y < rws; y++)
                {
                    float posY = origin.y + (y * size.y);
                    Gizmos.DrawLine(new Vector3(origin.x, posY, 0f), new Vector3(origin.x + w, posY, 0f));
                }
            }

            // Gambar batas luar (outer border)
            Gizmos.color = isSelected ? Color.cyan : outerBorderColor;
            Vector3 center = origin + new Vector3(w * 0.5f, h * 0.5f, 0f);
            Gizmos.DrawWireCube(center, new Vector3(w, h, 0.1f));

            // Gambar penanda titik pusat (center point)
            Gizmos.color = centerMarkerColor;
            Gizmos.DrawWireSphere(center, 0.25f);
        }
    }
}
