using UnityEngine;
using MazeSpell.UI;

namespace MazeSpell
{
    /// <summary>
    /// Pintu keluar labirin (Goal).
    /// Player HANYA dapat menyelesaikan level dan pindah ke level berikutnya
    /// jika seluruh huruf kata target level tersebut sudah berhasil dikumpulkan.
    /// Jika belum lengkap, pintu terkunci dan menampilkan pesan peringatan.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class LevelGoalTrigger : MonoBehaviour
    {
        [Header("Pengaturan Goal")]
        [SerializeField] private bool triggerOnce = true;

        [Header("Gizmo Visual")]
        [SerializeField] private Color gizmoColor = new Color(0.2f, 1f, 0.4f, 0.7f);

        private bool hasTriggered = false;
        private float messageTimer = 0f;
        private Collider2D col;

        private void Awake()
        {
            col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void Update()
        {
            if (messageTimer > 0f)
            {
                messageTimer -= Time.deltaTime;
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            HandlePlayerContact(collision);
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            // Tangani jika pemain tetap berdiri di area Goal saat kata baru saja menjadi lengkap
            if (!hasTriggered)
            {
                HandlePlayerContact(collision);
            }
        }

        private void HandlePlayerContact(Collider2D collision)
        {
            if (triggerOnce && hasTriggered)
                return;

            // Pastikan objek yang menyentuh Goal adalah Player
            bool isPlayer = collision.CompareTag("Player") || collision.GetComponent<PlayerController>() != null;
            if (!isPlayer)
                return;

            // 1. Validasi keberadaan WordCollectionManager
            if (WordCollectionManager.Instance == null)
            {
                Debug.LogWarning("[LevelGoalTrigger] WordCollectionManager tidak ditemukan!");
                return;
            }

            // 2. Cek apakah seluruh huruf kata target sudah terkumpul
            if (!WordCollectionManager.Instance.IsWordComplete)
            {
                Debug.Log(
                    $"[LevelGoalTrigger] Pintu Goal TERKUNCI! Huruf belum lengkap. " +
                    $"Progress: {WordCollectionManager.Instance.GetProgressString()}"
                );

                messageTimer = 2.0f;

                // Visual & Audio getar terkunci di World Space
                var gateVisual = GetComponentInChildren<ExitGateVisual>() ?? GetComponent<ExitGateVisual>();
                if (gateVisual != null)
                {
                    gateVisual.OnPlayerTouchLocked();
                }

                // Tampilkan notifikasi di HUD jika ada GameplayFeedbackController
                if (GameplayFeedbackController.Instance != null)
                {
                    GameplayFeedbackController.Instance.ShowNoticeMessage("Kumpulkan semua huruf terlebih dahulu!");
                }

                return;
            }

            // 2.5 Khusus Level 5: Goal terkunci sampai Final Guardian dikalahkan
            if (LevelProgressionManager.Instance != null && LevelProgressionManager.Instance.CurrentLevel == 5)
            {
                if (MazeSpell.Gameplay.FinalBossBattleManager.Instance != null && !MazeSpell.Gameplay.FinalBossBattleManager.Instance.IsBossDefeated)
                {
                    Debug.Log("[LevelGoalTrigger] Pintu Goal Level 5 TERKUNCI! Kalahkan Final Guardian terlebih dahulu.");
                    messageTimer = 2.0f;

                    var gateVisual = GetComponentInChildren<ExitGateVisual>() ?? GetComponent<ExitGateVisual>();
                    if (gateVisual != null)
                    {
                        gateVisual.OnPlayerTouchLocked();
                    }

                    if (GameplayFeedbackController.Instance != null)
                    {
                        GameplayFeedbackController.Instance.ShowNoticeMessage("Kalahkan Final Guardian terlebih dahulu!");
                    }
                    return;
                }
            }

            // 3. Kata sudah LENGKAP: izinkan pemain menyelesaikan level
            hasTriggered = true;
            messageTimer = 0f;

            Debug.Log("[LevelGoalTrigger] ★ PLAYER BERHASIL MENCAPAI GOAL DENGAN KATA LENGKAP! ★");

            var exitGate = GetComponentInChildren<ExitGateVisual>() ?? GetComponent<ExitGateVisual>();
            if (exitGate != null && Application.isPlaying)
            {
                StartCoroutine(exitGate.PlayOpenAnimationRoutine(() =>
                {
                    CompleteGoalSequence();
                }));
            }
            else
            {
                CompleteGoalSequence();
            }
        }

        private void CompleteGoalSequence()
        {
            if (LevelProgressionManager.Instance != null)
            {
                if (MazeSpell.Gameplay.LevelCompletePopup.Instance != null)
                {
                    int lvl = LevelProgressionManager.Instance.CurrentLevel;
                    string word = WordCollectionManager.Instance != null ? WordCollectionManager.Instance.CurrentTargetWord : "EARTH";
                    MazeSpell.Gameplay.LevelCompletePopup.Instance.ShowLevelComplete(lvl, word, () =>
                    {
                        if (LevelProgressionManager.Instance != null)
                        {
                            LevelProgressionManager.Instance.CompleteCurrentLevel();
                        }
                    });
                }
                else
                {
                    LevelProgressionManager.Instance.CompleteCurrentLevel();
                }
            }
            else
            {
                Debug.LogWarning("[LevelGoalTrigger] LevelProgressionManager tidak ditemukan!");
            }
        }

        public void ResetTrigger()
        {
            hasTriggered = false;
            messageTimer = 0f;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;

            Collider2D c = GetComponent<Collider2D>();
            if (c != null)
            {
                Gizmos.DrawWireCube(c.bounds.center, c.bounds.size);
            }
            else
            {
                Gizmos.DrawWireSphere(transform.position, 0.5f);
            }
        }
    }
}