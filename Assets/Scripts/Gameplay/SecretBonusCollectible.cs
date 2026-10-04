using System.Collections;
using UnityEngine;
using MazeSpell.Audio;

namespace MazeSpell.Gameplay
{
    /// <summary>
    /// Objek Bonus Rahasia (⭐ Star / 💎 Gem) yang ditempatkan di area rahasia / alcove:
    /// - Mengapung dan berputar perlahan dengan efek cahaya.
    /// - Saat disentuh Player: memicu ledakan partikel berkilau, SFX chime,
    ///   dan floating text "+ BONUS ⭐ SECRET FOUND!".
    /// - Tidak wajib untuk menyelesaikan level (hanya untuk kepuasan eksplorasi & bonus rating).
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class SecretBonusCollectible : MonoBehaviour
    {
        [SerializeField] private float bobSpeed = 2.5f;
        [SerializeField] private float bobHeight = 0.12f;
        [SerializeField] private float rotateSpeed = 60f;

        private Vector3 startPos;
        private bool isCollected = false;
        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            var col = GetComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.45f;
            startPos = transform.position;

            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            CreateStarVisual();
        }

        private void CreateStarVisual()
        {
            // Buat sprite bintang/kristal kuning keemasan secara prosedural jika belum ada
            if (spriteRenderer.sprite == null)
            {
                Texture2D tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                Color gold = new Color(1.0f, 0.85f, 0.20f, 1f);
                Color clear = new Color(1f, 1f, 1f, 0f);

                for (int y = 0; y < 32; y++)
                {
                    for (int x = 0; x < 32; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f));
                        // Bintang 4 sisi
                        float angle = Mathf.Atan2(y - 15.5f, x - 15.5f);
                        float starRadius = 12f * (0.6f + 0.4f * Mathf.Abs(Mathf.Cos(angle * 2f)));
                        if (dist <= starRadius)
                        {
                            tex.SetPixel(x, y, gold);
                        }
                        else
                        {
                            tex.SetPixel(x, y, clear);
                        }
                    }
                }
                tex.Apply();
                spriteRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
                spriteRenderer.sortingOrder = 15;
            }
        }

        private void Update()
        {
            if (isCollected) return;

            // Bobbing & Gentle sway
            float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(startPos.x, newY, startPos.z);
            transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (isCollected) return;
            if (collision.CompareTag("Player") || collision.GetComponent<PlayerController>() != null)
            {
                CollectBonus();
            }
        }

        public void CollectBonus()
        {
            if (isCollected) return;
            isCollected = true;

            Debug.Log("[SecretBonusCollectible] ★ SECRET BONUS FOUND! Pemain menemukan bonus rahasia.");

            // 1. Partikel emas
            ElementalParticleSpawner.SpawnSecretFoundEffect(transform.position);

            // 2. Floating text pop
            FloatingTextHelper.Show("+ BONUS ⭐ SECRET FOUND!", transform.position, new Color(1.0f, 0.88f, 0.20f, 1.0f), 3.4f);

            // 3. Audio
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.SFXType.GoalUnlocked, 1.0f);
            }

            // 4. Star Rating tracking
            StarRatingManager.HasSecretFoundThisLevel = true;

            // 5. Hancurkan objek bonus
            Destroy(gameObject, 0.1f);
        }
    }
}
