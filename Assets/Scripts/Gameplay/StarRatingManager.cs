using UnityEngine;

namespace MazeSpell.Gameplay
{
    /// <summary>
    /// Mengatur kalkulasi dan penyimpanan Star Rating (⭐⭐⭐) untuk setiap level:
    /// - 3 Bintang: Menyelesaikan level tanpa pernah kehilangan nyawa (0 damage).
    /// - 2 Bintang: Menyelesaikan level dengan kehilangan maksimal 1 nyawa (1 damage).
    /// - 1 Bintang: Menyelesaikan level dengan kehilangan lebih dari 1 nyawa (> 1 damage).
    /// Disimpan di PlayerPrefs per level untuk nilai replayabilitas (Replay Value).
    /// </summary>
    public static class StarRatingManager
    {
        private const string PREFS_STAR_PREFIX = "MazeSpell_Level_Stars_";

        public static bool HasSecretFoundThisLevel { get; set; } = false;

        public static void ResetLevelTracking()
        {
            HasSecretFoundThisLevel = false;
            if (PlayerLivesManager.Instance != null)
            {
                // Reset damage tracker di PlayerLivesManager
                var resetProp = typeof(PlayerLivesManager).GetProperty("DamageTakenThisLevel");
                // Reset lives otomatis mereset DamageTakenThisLevel
            }
        }

        /// <summary>
        /// Menghitung jumlah bintang yang diperoleh pemain berdasarkan damage yang diterima:
        /// 0 damage -> 3 Bintang
        /// 1 damage -> 2 Bintang
        /// > 1 damage -> 1 Bintang
        /// </summary>
        public static int CalculateStars(int damageTaken)
        {
            if (damageTaken <= 0)
                return 3;
            if (damageTaken == 1)
                return 2;
            return 1;
        }

        /// <summary>
        /// Mengambil bintang yang tersimpan untuk level tertentu (0 jika belum dimainkan).
        /// </summary>
        public static int GetStarsForLevel(int level)
        {
            return PlayerPrefs.GetInt(PREFS_STAR_PREFIX + level, 0);
        }

        /// <summary>
        /// Menyimpan bintang ke PlayerPrefs jika hasil kali ini lebih baik dari sebelumnya.
        /// Mengembalikan true jika berhasil mencetak rekor bintang baru.
        /// </summary>
        public static bool SaveStarsForLevel(int level, int stars)
        {
            int currentBest = GetStarsForLevel(level);
            if (stars > currentBest)
            {
                PlayerPrefs.SetInt(PREFS_STAR_PREFIX + level, stars);
                PlayerPrefs.Save();
                Debug.Log($"[StarRatingManager] ★ REKOR BINTANG BARU Level {level}: {stars} Bintang!");
                return true;
            }
            return false;
        }

        public static string FormatStarsString(int stars)
        {
            switch (stars)
            {
                case 3: return "★★★";
                case 2: return "★★☆";
                case 1: return "★☆☆";
                default: return "☆☆☆";
            }
        }

        public static string FormatStars(int stars) => FormatStarsString(stars);

        public static int CalculateLevelStars()
        {
            int damage = PlayerLivesManager.Instance != null ? PlayerLivesManager.Instance.DamageTakenThisLevel : 0;
            return CalculateStars(damage);
        }

        public static bool SaveLevelStars(int level, int stars) => SaveStarsForLevel(level, stars);
    }
}
