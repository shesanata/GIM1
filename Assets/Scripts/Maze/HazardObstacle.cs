using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MazeSpell
{
    /// <summary>
    /// Komponen modular untuk objek bahaya / rintangan (spikes, lava, trap, dsb).
    /// CATATAN: Sistem damage rintangan telah digantikan oleh sistem EnemyCharacter.
    /// HazardObstacle tetap dipertahankan untuk kompatibilitas, namun fungsi damage dinonaktifkan secara default.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class HazardObstacle : MonoBehaviour
    {
        [Header("Pengaturan Bahaya")]
        [Tooltip("Apakah rintangan ini memberikan damage ke Player (Default: false, karena damage digantikan oleh Enemy)")]
        [SerializeField] private bool dealsDamage = false;

        [Tooltip("Jumlah nyawa yang dikurangi jika dealsDamage diaktifkan")]
        [SerializeField] private int damageAmount = 1;

        [Tooltip("Nama rintangan (opsional, untuk keperluan identifikasi/log)")]
        [SerializeField] private string hazardName = "Hazard Trap (Inactive Damage)";

        [Header("Visual Editor Gizmo")]
        [SerializeField] private Color gizmoColor = new Color(1f, 0.6f, 0.2f, 0.5f);

        public bool DealsDamage
        {
            get => dealsDamage;
            set => dealsDamage = value;
        }

        public int DamageAmount => damageAmount;

        private void OnTriggerEnter2D(Collider2D other)
        {
            CheckAndDamagePlayer(other.gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            CheckAndDamagePlayer(collision.gameObject);
        }

        private void CheckAndDamagePlayer(GameObject target)
        {
            if (target == null) return;

            // Jika fungsi damage dinonaktifkan, hazard tidak mengurangi nyawa Player
            if (!dealsDamage)
            {
                return;
            }

            if (target.CompareTag("Player") || target.GetComponent<PlayerController>() != null)
            {
                Debug.Log($"[HazardObstacle] Pemain terkena rintangan '{hazardName}'!");

                if (PlayerLivesManager.Instance != null)
                {
                    PlayerLivesManager.Instance.TakeDamage(damageAmount);
                }
                else
                {
                    Debug.LogWarning("[HazardObstacle] PlayerLivesManager.Instance tidak ditemukan!");
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
            }
            else
            {
                Gizmos.DrawWireSphere(transform.position, 0.5f);
            }

#if UNITY_EDITOR
            GUIStyle style = new GUIStyle();
            style.fontSize = 9;
            style.fontStyle = FontStyle.Normal;
            style.normal.textColor = new Color(0.9f, 0.6f, 0.2f, 0.7f);
            style.alignment = TextAnchor.MiddleCenter;

            Handles.Label(transform.position + Vector3.up * 0.35f, "HAZARD (No Damage)", style);
#endif
        }
    }
}
