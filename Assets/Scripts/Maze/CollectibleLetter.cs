using UnityEngine;
using UnityEngine.Events;
using TMPro;
using MazeSpell.Audio;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MazeSpell
{
    /// <summary>
    /// Huruf yang dapat dikumpulkan oleh Player di dalam labirin.
    /// Ketika disentuh, huruf diserahkan ke WordCollectionManager.
    /// Jika diterima, memicu animasi Collect (pop & bounce) dan menghilang, lalu memperbarui HUD kata.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CollectibleLetter : MonoBehaviour
    {
        [Header("Konfigurasi Huruf")]
        [SerializeField] private char letter = 'A';
        [SerializeField] private bool isCollected = false;

        [Header("Pendaftaran Otomatis")]
        [SerializeField] private bool autoRegisterOnStart = true;

        [Header("Visual Gizmo")]
        [SerializeField] private Color gizmoColor = new Color(1f, 0.85f, 0.2f, 0.8f);
        [SerializeField] private float gizmoRadius = 0.4f;

        [Header("Events")]
        public UnityEvent<char> onCollected = new UnityEvent<char>();

        private Collider2D col;

        public char Letter
        {
            get => char.ToUpperInvariant(letter);
            set
            {
                letter = char.ToUpperInvariant(value);
                gameObject.name = $"Letter_{letter}";
                UpdateVisuals();
            }
        }

        public void SetLetter(char newLetter)
        {
            Letter = newLetter;
        }

        public bool IsCollected => isCollected;

        private void Awake()
        {
            if (onCollected == null) onCollected = new UnityEvent<char>();

            col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
                col.enabled = true;
            }

            isCollected = false;
        }

        private void Start()
        {
            EnsureWalkablePosition();
            UpdateVisuals();

            if (autoRegisterOnStart && WordCollectionManager.Instance != null)
            {
                WordCollectionManager.Instance.RegisterLetter(this);
            }
        }

        /// <summary>
        /// Mengecek apakah posisi huruf saat ini berada di cell yang walkable, bukan wall, dan reachable dari Player spawn.
        /// </summary>
        public bool IsLetterPositionWalkable()
        {
            return MazeLetterValidator.IsLetterPositionWalkable(this, out _);
        }

        /// <summary>
        /// Memastikan huruf berada di jalur walkable. Jika ditemukan di wall, obstacle,
        /// atau unreachable, otomatis memindahkannya ke CENTER cell walkable terdekat.
        /// </summary>
        public bool EnsureWalkablePosition()
        {
            return MazeLetterValidator.ValidateAndFixLetterPosition(this);
        }

        private void OnDestroy()
        {
            if (WordCollectionManager.Instance != null)
            {
                WordCollectionManager.Instance.UnregisterLetter(this);
            }
        }

        private void OnValidate()
        {
            letter = char.ToUpperInvariant(letter);
        }

        /// <summary>
        /// Memperbarui teks pada seluruh layer visual anak (Outline, Depth, Face).
        /// </summary>
        public void UpdateVisuals()
        {
            var tmps = GetComponentsInChildren<TextMeshPro>(true);
            foreach (var t in tmps)
            {
                t.text = $"<b>{Letter}</b>";
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (isCollected)
                return;

            bool isPlayer = collision.CompareTag("Player") || collision.GetComponent<PlayerController>() != null;
            if (!isPlayer)
                return;

            Debug.Log($"[CollectibleLetter] Player menyentuh huruf '{Letter}'");
            Collect();
        }

        /// <summary>
        /// Mengumpulkan huruf ini ke WordCollectionManager.
        /// </summary>
        public void Collect()
        {
            if (isCollected)
                return;

            if (WordCollectionManager.Instance == null)
            {
                Debug.LogWarning("[CollectibleLetter] WordCollectionManager tidak ditemukan!");
                return;
            }

            // Masukkan huruf ke WordCollectionManager
            bool accepted = WordCollectionManager.Instance.CollectLetter(this);

            // Jika huruf tidak dibutuhkan (sudah lengkap / bukan target), jangan hilangkan objek
            if (!accepted)
            {
                Debug.Log($"[CollectibleLetter] Huruf '{Letter}' tidak diterima oleh target kata.");
                return;
            }

            // Huruf berhasil diterima: tandai terkumpul dan matikan collider seketika
            isCollected = true;

            if (col == null)
            {
                col = GetComponent<Collider2D>();
            }

            if (col != null)
            {
                col.enabled = false;
            }

            // Putar efek suara pengambilan huruf
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.SFXType.CollectLetter);
            }

            Debug.Log($"[CollectibleLetter] ✓ Huruf '{Letter}' BERHASIL DIAMBIL!");

            onCollected?.Invoke(Letter);

            // Mainkan animasi Collect (shrink -> pop -> bounce -> disappear)
            LetterHoverAnimation hoverAnim = GetComponent<LetterHoverAnimation>();
            if (hoverAnim != null)
            {
                hoverAnim.PlayCollectAnimation(() =>
                {
                    Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
                    foreach (Renderer r in renderers)
                    {
                        r.enabled = false;
                    }
                });
            }
            else
            {
                Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
                foreach (Renderer r in renderers)
                {
                    r.enabled = false;
                }
            }
        }

        /// <summary>
        /// Mengembalikan huruf ke status belum terkumpul dan menampilkan kembali visualnya (untuk restart level).
        /// </summary>
        public void ResetLetter()
        {
            isCollected = false;

            if (col == null)
            {
                col = GetComponent<Collider2D>();
            }

            if (col != null)
            {
                col.enabled = true;
                col.isTrigger = true;
            }

            LetterHoverAnimation hoverAnim = GetComponent<LetterHoverAnimation>();
            if (hoverAnim != null)
            {
                hoverAnim.ResetAnimation();
            }

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                r.enabled = true;
            }

            UpdateVisuals();
        }

        private void OnDrawGizmos()
        {
            if (isCollected)
                return;

            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, gizmoRadius);

#if UNITY_EDITOR
            GUIStyle labelStyle = new GUIStyle();
            labelStyle.fontSize = 14;
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.normal.textColor = Color.yellow;
            labelStyle.alignment = TextAnchor.MiddleCenter;

            Handles.Label(
                transform.position + Vector3.up * 0.35f,
                Letter.ToString(),
                labelStyle
            );
#endif
        }
    }
}