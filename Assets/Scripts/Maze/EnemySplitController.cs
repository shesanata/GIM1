using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MazeSpell.Audio;
using MazeSpell.Gameplay;

namespace MazeSpell
{
    /// <summary>
    /// Mengelola sistem Enemy Split dan batasan pembelahan untuk MazeSpell:
    /// - Saat Enemy utama kehabisan HP (L1-3: hit 1, L4: hit 2), Enemy memicu efek pecah,
    ///   mengecil, menghilang, dan membelah menjadi tepat 2 Mini Enemy.
    /// - Mini Enemy (~50% ukuran Enemy utama) menyebar selama 0.35s ke posisi valid tanpa menembus dinding,
    ///   lalu mengejar Player menggunakan BFS pathfinding.
    /// - Memiliki batasan ketat (CanSplit, HasSplit, SplitGeneration) agar Mini Enemy TIDAK BISA membelah lagi (maksimal 1 -> 2).
    /// - Level 5 Final Guardian TIDAK BOLEH membelah, melainkan memicu Enrage Phase saat HP=1 dan Final Defeat saat HP=0.
    /// </summary>
    public class EnemySplitController : MonoBehaviour
    {
        [Header("Split Configuration")]
        [Tooltip("Apakah Enemy ini diizinkan untuk membelah saat HP habis")]
        [SerializeField] private bool canSplit = true;

        [Tooltip("Apakah Enemy ini sudah pernah membelah sebelumnya")]
        [SerializeField] private bool hasSplit = false;

        [Tooltip("Generasi Enemy (0 = Enemy Utama, 1 = Mini Enemy)")]
        [SerializeField] private int splitGeneration = 0;

        [Tooltip("Apakah instansi ini merupakan Mini Enemy hasil pembelahan")]
        [SerializeField] private bool isMiniEnemy = false;

        [Header("Mini Enemy Attributes")]
        [SerializeField] private float miniScale = 0.52f;
        [SerializeField] private float miniSpeedMultiplier = 1.20f;
        [SerializeField] private float miniAttackCooldown = 1.35f;

        private EnemyCharacter parentEnemy;

        public bool CanSplit => canSplit && !hasSplit && splitGeneration == 0 && (parentEnemy == null || !parentEnemy.IsBoss);
        public bool HasSplit => hasSplit;
        public int SplitGeneration => splitGeneration;
        public bool IsMiniEnemy => isMiniEnemy;

        private void Awake()
        {
            if (parentEnemy == null)
            {
                parentEnemy = GetComponent<EnemyCharacter>();
            }
        }

        /// <summary>
        /// Menginisialisasi parameter split controller.
        /// </summary>
        public void Initialize(EnemyCharacter enemy, bool canSplitVal, bool isMiniVal, int generation)
        {
            parentEnemy = enemy;
            isMiniEnemy = isMiniVal;
            splitGeneration = generation;
            canSplit = canSplitVal && (enemy == null || !enemy.IsBoss);

            if (isMiniVal || generation > 0)
            {
                hasSplit = true;
                canSplit = false;
            }
        }

        /// <summary>
        /// Memicu urutan pembelahan Enemy menjadi 2 Mini Enemy.
        /// </summary>
        public void TriggerSplit(EnemyCharacter sourceEnemy)
        {
            if (!CanSplit || hasSplit) return;

            hasSplit = true;
            canSplit = false;

            if (Application.isPlaying)
            {
                StartCoroutine(SplitAndSpawnMiniRoutine(sourceEnemy));
            }
            else
            {
                ExecuteSplitSync(sourceEnemy);
            }
        }

        /// <summary>
        /// Mengeksekusi proses split secara instan tanpa menunggu coroutine frame (berguna untuk test runner).
        /// </summary>
        public void ExecuteSplitSync(EnemyCharacter sourceEnemy)
        {
            if (sourceEnemy == null) return;

            int level = sourceEnemy.CurrentLevelNumber;
            Vector3 centerPos = sourceEnemy.transform.position;
            Transform parentContainer = sourceEnemy.transform.parent;

            GameObject playerObj = GameObject.FindWithTag("Player");
            Vector3 targetSpread1 = FindSafeSpreadPosition(centerPos, 0, playerObj, sourceEnemy);
            Vector3 targetSpread2 = FindSafeSpreadPosition(centerPos, 1, playerObj, sourceEnemy);

            SpawnMiniEnemyInstance(centerPos, targetSpread1, 1, level, parentContainer, sourceEnemy);
            SpawnMiniEnemyInstance(centerPos, targetSpread2, 2, level, parentContainer, sourceEnemy);

            DestroyImmediate(sourceEnemy.gameObject);
        }

        private IEnumerator SplitAndSpawnMiniRoutine(EnemyCharacter sourceEnemy)
        {
            if (sourceEnemy == null) yield break;

            int level = sourceEnemy.CurrentLevelNumber;
            Vector3 centerPos = sourceEnemy.transform.position;
            Transform parentContainer = sourceEnemy.transform.parent;

            // 1. Matikan collider agar tidak menerima serangan ganda saat proses split
            Collider2D col = sourceEnemy.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            // 2. Mainkan Audio & Efek Visual Split
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.SFXType.EnemySplit, 1.0f);
            }

            ElementalParticleSpawner.SpawnElementEffect(level, centerPos);
            ElementalParticleSpawner.SpawnHitImpactEffect(centerPos);
            FloatingTextHelper.ShowFloatingText(centerPos + Vector3.up * 0.6f, "★ ENEMY SPLIT! ★", new Color(1f, 0.85f, 0.2f), 26);

            // 3. Efek animasi Enemy utama mengecil lalu menghilang (0.20 detik)
            float shrinkDur = 0.20f;
            float el = 0f;
            Vector3 startScale = sourceEnemy.transform.localScale;

            while (el < shrinkDur)
            {
                el += Time.deltaTime;
                float t = Mathf.Clamp01(el / shrinkDur);
                sourceEnemy.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t * t);
                yield return null;
            }

            // 4. Cari posisi sebar yang aman dan valid di dalam koridor labirin
            GameObject playerObj = GameObject.FindWithTag("Player");
            Vector3 targetSpread1 = FindSafeSpreadPosition(centerPos, 0, playerObj, sourceEnemy);
            Vector3 targetSpread2 = FindSafeSpreadPosition(centerPos, 1, playerObj, sourceEnemy);

            // 5. Spawn 2 Mini Enemy
            SpawnMiniEnemyInstance(centerPos, targetSpread1, 1, level, parentContainer, sourceEnemy);
            SpawnMiniEnemyInstance(centerPos, targetSpread2, 2, level, parentContainer, sourceEnemy);

            // 6. Audio Mini Enemy Spawn
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.SFXType.MiniEnemySpawn, 0.95f);
            }

            // 7. Hancurkan Enemy utama
            Destroy(sourceEnemy.gameObject);
        }

        private GameObject SpawnMiniEnemyInstance(Vector3 spawnCenter, Vector3 targetSpread, int index, int level, Transform parentContainer, EnemyCharacter template)
        {
            GameObject miniGo = new GameObject($"MiniEnemy_L{level}_{index}");
            if (parentContainer != null)
            {
                miniGo.transform.SetParent(parentContainer, false);
            }
            miniGo.transform.position = spawnCenter;
            miniGo.transform.localScale = Vector3.one * miniScale;

            // 1. SpriteRenderer & Theme Tint
            SpriteRenderer sr = miniGo.AddComponent<SpriteRenderer>();
            SpriteRenderer tempSr = template != null ? template.GetComponent<SpriteRenderer>() : null;
            if (tempSr != null && tempSr.sprite != null)
            {
                sr.sprite = tempSr.sprite;
            }
            else
            {
#if UNITY_EDITOR
                sr.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Enemy.png");
#endif
                if (sr.sprite == null) sr.sprite = Resources.Load<Sprite>("Sprites/Enemy");
            }
            sr.sortingOrder = 4;
            sr.color = GetThemeTintForLevel(level);

            // 2. Collider2D
            CircleCollider2D circleCol = miniGo.AddComponent<CircleCollider2D>();
            circleCol.isTrigger = true;
            circleCol.radius = 0.38f;

            // 3. EnemyCharacter Component
            EnemyCharacter.IsSpawningMiniEnemy = true;
            EnemyCharacter miniEnemy = miniGo.AddComponent<EnemyCharacter>();
            EnemyCharacter.IsSpawningMiniEnemy = false;
            miniEnemy.SetSplitProperties(false, true, 1);
            miniEnemy.SetLevel(level);
            miniEnemy.SetMiniEnemyStats(miniSpeedMultiplier, miniAttackCooldown);

            // 4. EnemySplitController pada Mini Enemy (Strict: cannot split again)
            EnemySplitController miniSplit = miniGo.AddComponent<EnemySplitController>();
            miniSplit.Initialize(miniEnemy, false, true, 1);

            // 5. Animasi menyebar keluar (Pop & Spread) selama 0.35s
            miniEnemy.StartCoroutine(MiniSpreadAnimationRoutine(miniGo.transform, spawnCenter, targetSpread, 0.35f, miniEnemy));

            return miniGo;
        }

        private IEnumerator MiniSpreadAnimationRoutine(Transform miniTransform, Vector3 start, Vector3 target, float duration, EnemyCharacter enemyChar)
        {
            if (enemyChar != null)
            {
                enemyChar.SetStagger(duration); // Tahan pergerakan normal selama animasi sebar berlangsung
            }

            float el = 0f;
            Vector3 baseSc = Vector3.one * miniScale;

            while (el < duration)
            {
                if (miniTransform == null) yield break;

                el += Time.deltaTime;
                float t = Mathf.Clamp01(el / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                miniTransform.position = Vector3.Lerp(start, target, smoothT);
                // Efek membal (bounce pop)
                float pop = 1f + Mathf.Sin(smoothT * Mathf.PI) * 0.28f;
                miniTransform.localScale = baseSc * pop;

                yield return null;
            }

            if (miniTransform != null)
            {
                miniTransform.position = target;
                miniTransform.localScale = baseSc;
            }

            if (enemyChar != null)
            {
                enemyChar.SetStagger(0f); // Selesai menyebar, siap mengejar Player
            }
        }

        /// <summary>
        /// Mencari posisi sebar yang aman (tidak di dalam tembok, tidak di atas player, tidak di goal).
        /// </summary>
        private Vector3 FindSafeSpreadPosition(Vector3 center, int index, GameObject playerObj, EnemyCharacter enemy)
        {
            Vector2 playerPos = playerObj != null ? (Vector2)playerObj.transform.position : (Vector2)center + Vector2.down;
            Vector2 toPlayer = ((Vector2)center - playerPos).normalized;
            if (toPlayer.sqrMagnitude < 0.001f) toPlayer = Vector2.up;

            Vector2 perp = new Vector2(-toPlayer.y, toPlayer.x);
            Vector2 primaryDir = (index == 0) ? perp : -perp;

            float spreadDist = 0.55f;

            // Daftar arah alternatif yang dicoba secara berurutan
            List<Vector2> candidateDirs = new List<Vector2>
            {
                primaryDir,
                (primaryDir + toPlayer * 0.45f).normalized,
                (primaryDir - toPlayer * 0.45f).normalized,
                (index == 0 ? Vector2.left : Vector2.right),
                (index == 0 ? Vector2.up : Vector2.down),
                -toPlayer
            };

            foreach (var dir in candidateDirs)
            {
                Vector3 candidatePos = center + (Vector3)(dir * spreadDist);
                Vector3Int cell = new Vector3Int(Mathf.FloorToInt(candidatePos.x), Mathf.FloorToInt(candidatePos.y), 0);

                if (enemy != null && !enemy.IsCellWalkable(cell))
                {
                    continue;
                }

                // Jangan spawn tepat di atas Player
                if (playerObj != null && Vector2.Distance(candidatePos, playerObj.transform.position) < 0.60f)
                {
                    continue;
                }

                // Jangan spawn tepat di atas Goal
                GameObject goal = GameObject.Find("Goal");
                if (goal != null && Vector2.Distance(candidatePos, goal.transform.position) < 0.85f)
                {
                    continue;
                }

                return candidatePos;
            }

            // Fallback: geser sangat kecil di dalam sel yang sama
            return center + (index == 0 ? Vector3.left * 0.18f : Vector3.right * 0.18f);
        }

        private Color GetThemeTintForLevel(int level)
        {
            switch (level)
            {
                case 1: return new Color(0.92f, 0.82f, 0.70f, 1f); // Earth: loam tint
                case 2: return new Color(0.80f, 0.88f, 0.98f, 1f); // Stone: granite tint
                case 3: return new Color(0.72f, 0.98f, 0.72f, 1f); // Grass: emerald tint
                case 4: return new Color(0.75f, 0.95f, 1.00f, 1f); // Frost: ice cyan tint
                case 5: return new Color(1.00f, 0.70f, 0.50f, 1f); // Flame: ember tint
                default: return Color.white;
            }
        }
    }
}
