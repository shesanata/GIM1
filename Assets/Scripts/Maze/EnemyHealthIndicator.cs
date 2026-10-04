using UnityEngine;

namespace MazeSpell
{
    /// <summary>
    /// Menampilkan indikator HP berupa deretan ikon hati (♥ ♥ ♥) kecil di atas kepala Enemy.
    /// Khusus untuk Level multi-hit (Level 4: 2 HP, Level 5: 3 HP).
    /// Hati berkurang satu per satu saat Enemy menerima serangan SPACE.
    /// Indikator hilang sepenuhnya saat Enemy defeated (HP = 0).
    /// </summary>
    public class EnemyHealthIndicator : MonoBehaviour
    {
        [Header("Pengaturan Tampilan")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 0.72f, -0.05f);
        [SerializeField] private float heartSpacing = 0.22f;
        [SerializeField] private Sprite heartSprite;

        private GameObject[] heartObjects;
        private SpriteRenderer[] heartRenderers;
        private int currentMaxHealth = 3;
        private int currentHp = 3;

        public int CurrentHealth => currentHp;
        public int MaxHealth => currentMaxHealth;

        private void Awake()
        {
            EnsureHeartSprite();
            EnsureHeartsCreated();
        }

        private void Start()
        {
            EnsureHeartSprite();
            EnsureHeartsCreated();
        }

        private void LateUpdate()
        {
            // Pertahankan skala normal dan rotasi tegak agar hati tidak terbalik saat Enemy berbalik arah
            if (transform.parent != null)
            {
                float parentSign = Mathf.Sign(transform.parent.lossyScale.x);
                if (Mathf.Abs(parentSign) > 0.001f)
                {
                    transform.localScale = new Vector3(parentSign, 1f, 1f);
                }
                transform.rotation = Quaternion.identity;
                transform.localPosition = offset;
            }
        }

        private void EnsureHeartSprite()
        {
            if (heartSprite != null) return;

#if UNITY_EDITOR
            heartSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/heart_small.png");
#endif
            if (heartSprite == null)
            {
                // Fallback procedural 16x16 red heart texture jika asset belum ter-load
                Texture2D tex = CreateProceduralHeartTexture();
                heartSprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 64f);
            }
        }

        private Texture2D CreateProceduralHeartTexture()
        {
            Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color T = Color.clear;
            Color O = new Color(58f / 255f, 8f / 255f, 18f / 255f, 1f);
            Color R = new Color(238f / 255f, 38f / 255f, 68f / 255f, 1f);
            Color H = new Color(255f / 255f, 145f / 255f, 165f / 255f, 1f);
            Color S = new Color(168f / 255f, 18f / 255f, 42f / 255f, 1f);

            string[] map = {
                "................",
                "................",
                "....OO....OO....",
                "...OHRO..OHRO...",
                "..OHRRROOHRRRO..",
                "..OHRRRRRRRRRO..",
                "..OHRRRRRRRRRO..",
                "...ORRRRRRRRSO..",
                "...ORRRRRRRSO...",
                "....ORRRRRSO....",
                ".....ORRRSO.....",
                "......ORSO......",
                ".......OO.......",
                "................",
                "................",
                "................"
            };

            for (int y = 0; y < 16; y++)
            {
                string row = map[15 - y];
                for (int x = 0; x < 16; x++)
                {
                    char c = row[x];
                    Color col = T;
                    if (c == 'O') col = O;
                    else if (c == 'R') col = R;
                    else if (c == 'H') col = H;
                    else if (c == 'S') col = S;
                    tex.SetPixel(x, y, col);
                }
            }
            tex.Apply();
            return tex;
        }

        private void EnsureHeartsCreated()
        {
            EnsureHeartSprite();

            if (heartObjects != null && heartObjects.Length == 3)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (heartRenderers != null && heartRenderers[i] != null && heartRenderers[i].sprite == null)
                    {
                        heartRenderers[i].sprite = heartSprite;
                    }
                }
                return;
            }

            heartObjects = new GameObject[3];
            heartRenderers = new SpriteRenderer[3];

            for (int i = 0; i < 3; i++)
            {
                Transform existing = transform.Find($"Heart_{i + 1}");
                GameObject hObj = existing != null ? existing.gameObject : new GameObject($"Heart_{i + 1}");
                hObj.transform.SetParent(transform, false);

                SpriteRenderer sr = hObj.GetComponent<SpriteRenderer>();
                if (sr == null) sr = hObj.AddComponent<SpriteRenderer>();
                if (heartSprite != null) sr.sprite = heartSprite;
                sr.sortingOrder = 20; // Selalu di atas sprite enemy dan lorong

                heartObjects[i] = hObj;
                heartRenderers[i] = sr;
            }
        }

        /// <summary>
        /// Memperbarui tampilan indikator HP sesuai kesehatan saat ini dan kesehatan maksimal.
        /// </summary>
        public void UpdateHealth(int currentHealth, int maxHealth)
        {
            currentHp = Mathf.Max(0, currentHealth);
            currentMaxHealth = Mathf.Max(1, maxHealth);

            EnsureHeartSprite();
            EnsureHeartsCreated();

            // Jika HP 0 atau Enemy 1-HP (Level 1-3), sembunyikan semua hati agar visual bersih
            if (currentHp <= 0 || currentMaxHealth <= 1)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (heartObjects[i] != null) heartObjects[i].SetActive(false);
                }
                return;
            }

            // Posisikan hati secara simetris di tengah
            float totalWidth = (currentMaxHealth - 1) * heartSpacing;
            float startX = -totalWidth * 0.5f;

            for (int i = 0; i < 3; i++)
            {
                if (heartObjects[i] == null) continue;

                if (i < currentMaxHealth)
                {
                    heartObjects[i].transform.localPosition = new Vector3(startX + i * heartSpacing, 0f, 0f);
                    bool isAlive = i < currentHp;
                    heartObjects[i].SetActive(isAlive);

                    if (isAlive && heartRenderers[i] != null)
                    {
                        heartRenderers[i].color = Color.white;
                    }
                }
                else
                {
                    heartObjects[i].SetActive(false);
                }
            }
        }
    }
}
