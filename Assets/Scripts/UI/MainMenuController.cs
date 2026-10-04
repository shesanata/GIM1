using UnityEngine;
using UnityEngine.SceneManagement;

namespace MazeSpell.UI
{
    /// <summary>
    /// Mengontrol interaksi antarmuka Main Menu untuk game MazeSpell.
    /// Mengelola navigasi tombol Play, Level Select, dan Exit.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Konfigurasi Scene")]
        [Tooltip("Nama scene gameplay utama yang sudah ada di project")]
        [SerializeField] private string gameplaySceneName = "SampleScene";

        [Tooltip("Nama scene pemilihan level")]
        [SerializeField] private string levelSelectSceneName = "LevelSelect";

        private void Awake()
        {
            // Pastikan gameplay manager yang terinisialisasi otomatis tidak menampilkan HUD di Main Menu
            CleanupGameplayManagersInMenu();
        }

        private void Start()
        {
            // Lakukan pembersihan tambahan saat Start jika ada manager yang terinisialisasi belakangan
            CleanupGameplayManagersInMenu();
        }

        /// <summary>
        /// Membersihkan manager gameplay runtime yang mungkin ter-spawn secara otomatis
        /// agar tidak menutupi tampilan Main Menu dengan debug OnGUI.
        /// </summary>
        private void CleanupGameplayManagersInMenu()
        {
            // Cari dan bersihkan LevelProgressionManager jika ada di MainMenu
            var progressionManager = Object.FindAnyObjectByType<MazeSpell.LevelProgressionManager>();
            if (progressionManager != null)
            {
                Destroy(progressionManager.gameObject);
            }

            // Cari dan bersihkan WordCollectionManager jika ada di MainMenu
            var wordManager = Object.FindAnyObjectByType<MazeSpell.WordCollectionManager>();
            if (wordManager != null)
            {
                Destroy(wordManager.gameObject);
            }

            // Cari dan bersihkan PlayerLivesManager jika ada di MainMenu
            var livesManager = Object.FindAnyObjectByType<MazeSpell.PlayerLivesManager>();
            if (livesManager != null)
            {
                Destroy(livesManager.gameObject);
            }

            // Hapus objek gameplay sisa (Player dan Ground) jika sempat terbuat di Main Menu
            var playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                Destroy(playerObj);
            }

            var groundObj = GameObject.Find("Ground");
            if (groundObj != null)
            {
                Destroy(groundObj);
            }

            // Hapus grid maze sisa jika sempat ter-instantiate di MainMenu
            for (int i = 1; i <= 5; i++)
            {
                GameObject gridObj = GameObject.Find($"Level{i}_MazeGrid");
                if (gridObj != null)
                {
                    Destroy(gridObj);
                }
            }
        }

        /// <summary>
        /// Memulai permainan dengan memuat scene gameplay utama yang sudah ada di project.
        /// </summary>
        public void PlayGame()
        {
            Debug.Log($"[MainMenu] Memulai permainan... Memuat scene: {gameplaySceneName}");
            if (Application.CanStreamedLevelBeLoaded(gameplaySceneName))
            {
                SceneManager.LoadScene(gameplaySceneName);
            }
            else
            {
                Debug.LogWarning($"[MainMenu] Scene '{gameplaySceneName}' belum terdaftar di Build Settings. Memuat berdasarkan index...");
                SceneManager.LoadScene(1);
            }
        }

        /// <summary>
        /// Membuka scene pemilihan level.
        /// </summary>
        public void OpenLevelSelect()
        {
            Debug.Log($"[MainMenu] Membuka Level Select... Memuat scene: {levelSelectSceneName}");
            if (Application.CanStreamedLevelBeLoaded(levelSelectSceneName))
            {
                SceneManager.LoadScene(levelSelectSceneName);
            }
            else
            {
                Debug.LogWarning($"[MainMenu] Scene '{levelSelectSceneName}' belum terdaftar di Build Settings. Memuat langsung gameplay...");
                PlayGame();
            }
        }

        /// <summary>
        /// Menutup aplikasi game. Di Unity Editor akan menampilkan log dan menghentikan Play Mode.
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("[MainMenu] Tombol Exit ditekan. Menutup game MazeSpell...");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
