using System;
using UnityEditor;
using UnityEngine;

namespace MazeSpell.Editor
{
    /// <summary>
    /// Editor script untuk mengonfigurasi difficulty Enemy bertahap di Level 1-5:
    /// - Level 1: HP = 1, Speed = 1.5, Cooldown = 1.0s, 1 hit = kalah
    /// - Level 2: HP = 1, Speed = 1.8, Cooldown = 1.0s, 1 hit = kalah
    /// - Level 3: HP = 1, Speed = 2.1, Cooldown = 1.0s, 1 hit = kalah
    /// - Level 4: HP = 2, Speed = 2.4, Cooldown = 0.85s, 2 hits = kalah
    /// - Level 5: HP = 3, Speed = 2.7, Cooldown = 0.75s, 3 hits = kalah (FINAL CHALLENGE)
    /// Menjamin struktur hierarki visual lengkap dengan HealthIndicator (♥ ♥ ♥).
    /// </summary>
    public static class EnemyDifficultySetup
    {
        private static readonly string[] PREFAB_PATHS = new string[]
        {
            "Assets/Prefabs/MazeGrids/Level1_MazeGrid.prefab",
            "Assets/Prefabs/MazeGrids/Level2_MazeGrid.prefab",
            "Assets/Prefabs/MazeGrids/Level3_MazeGrid.prefab",
            "Assets/Prefabs/MazeGrids/Level4_MazeGrid.prefab",
            "Assets/Prefabs/MazeGrids/Level5_MazeGrid.prefab"
        };

        [MenuItem("MazeSpell/Apply Enemy Difficulty Progression (Levels 1-5)")]
        public static void ApplyDifficultyToAllPrefabs()
        {
            Debug.Log("==================================================================");
            Debug.Log(">>> MENERAPKAN DIFFICULTY ENEMY BERTAHAP (LEVEL 1–5) <<<");
            Debug.Log("==================================================================");

            for (int i = 0; i < PREFAB_PATHS.Length; i++)
            {
                int levelNum = i + 1;
                string path = PREFAB_PATHS[i];
                UpgradeLevelPrefab(path, levelNum);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("==================================================================");
            Debug.Log(">>> SEMUA 5 LEVEL PREFAB BERHASIL DI-UPGRADE DENGAN DIFFICULTY ENEMY! <<<");
            Debug.Log("==================================================================");
        }

        private static void UpgradeLevelPrefab(string prefabPath, int levelNum)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
            {
                Debug.LogError($"[EnemyDifficultySetup] Gagal memuat prefab {prefabPath}");
                return;
            }

            try
            {
                EnemyCharacter[] enemies = root.GetComponentsInChildren<EnemyCharacter>(true);
                if (enemies == null || enemies.Length == 0)
                {
                    Debug.LogWarning($"[EnemyDifficultySetup] Tidak ada EnemyCharacter pada {prefabPath}");
                    return;
                }

                foreach (EnemyCharacter enemy in enemies)
                {
                    // Update visual hierarchy dan health indicator
                    enemy.EnsureVisualHierarchy();
                    enemy.EnsureHealthIndicator();

                    // Konfigurasi stats eksplisit sesuai level
                    SerializedObject so = new SerializedObject(enemy);
                    so.Update();

                    float speed = EnemyCharacter.GetSpeedForLevel(levelNum);
                    int hp = EnemyCharacter.GetMaxHealthForLevel(levelNum);
                    float cooldown = EnemyCharacter.GetAttackCooldownForLevel(levelNum);

                    SerializedProperty spSpeed = so.FindProperty("chaseSpeed");
                    if (spSpeed != null) spSpeed.floatValue = speed;

                    SerializedProperty spMaxHp = so.FindProperty("maxHealth");
                    if (spMaxHp != null) spMaxHp.intValue = hp;

                    SerializedProperty spCurHp = so.FindProperty("currentHealth");
                    if (spCurHp != null) spCurHp.intValue = hp;

                    SerializedProperty spCooldown = so.FindProperty("damageCooldown");
                    if (spCooldown != null) spCooldown.floatValue = cooldown;

                    SerializedProperty spAttackRange = so.FindProperty("attackRange");
                    if (spAttackRange != null) spAttackRange.floatValue = 0.9f;

                    SerializedProperty spPlayerAttackRange = so.FindProperty("playerAttackRange");
                    if (spPlayerAttackRange != null) spPlayerAttackRange.floatValue = 1.4f;

                    SerializedProperty spOverride = so.FindProperty("overrideLevel");
                    if (spOverride != null) spOverride.intValue = levelNum;

                    so.ApplyModifiedProperties();

                    // Update Health Indicator
                    enemy.UpdateHealthIndicator();

                    Debug.Log($"[EnemyDifficultySetup] Level {levelNum} Enemy: HP={hp}, Speed={speed}, Cooldown={cooldown}s (Tersimpan ke Prefab)");
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
