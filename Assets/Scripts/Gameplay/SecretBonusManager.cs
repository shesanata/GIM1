using UnityEngine;

namespace MazeSpell.Gameplay
{
    /// <summary>
    /// Mengelola penempatan Secret Room / Bonus Collectible per level pada lokasi yang aman dan tidak merusak maze:
    /// - Level 1: (2.5, 9.5) - Ujung lorong kiri atas.
    /// - Level 2: (15.5, 9.5) - Ujung alcove kanan atas (dilindungi Magical Stone Gate).
    /// - Level 3: (16.5, 9.5) - Ujung spur kanan atas.
    /// - Level 4: (1.5, 4.5) - Spur buntu kiri tengah.
    /// - Level 5: (21.5, 11.5) - Alcove vulkanik kanan atas.
    /// </summary>
    public static class SecretBonusManager
    {
        public static readonly Vector2[] SecretPositionsPerLevel = new Vector2[]
        {
            new Vector2(2.5f, 12.5f),  // Level 1: North-West secret alcove
            new Vector2(30.5f, 14.5f), // Level 2: Secret Stone Vault behind Magical Gate
            new Vector2(34.5f, 16.5f), // Level 3: Secluded Overgrown Grove
            new Vector2(2.5f, 18.5f),  // Level 4: Hidden Frost Crystal Cave
            new Vector2(41.5f, 20.5f)  // Level 5: Volcanic Crater Alcove
        };

        public static GameObject SpawnSecretBonusForLevel(int level, Transform parent = null)
        {
            int index = Mathf.Clamp(level - 1, 0, SecretPositionsPerLevel.Length - 1);
            Vector2 pos = SecretPositionsPerLevel[index];

            // Cek apakah sudah ada di scene agar tidak duplicate
            var existing = GameObject.Find($"SecretBonus_Level_{level}");
            if (existing != null) return existing;

            GameObject bonusObj = new GameObject($"SecretBonus_Level_{level}");
            bonusObj.transform.position = new Vector3(pos.x, pos.y, 0f);
            if (parent != null) bonusObj.transform.SetParent(parent, true);

            bonusObj.AddComponent<SecretBonusCollectible>();
            Debug.Log($"[SecretBonusManager] Secret Bonus untuk Level {level} berhasil ditempatkan di {pos}.");
            return bonusObj;
        }
    }
}
