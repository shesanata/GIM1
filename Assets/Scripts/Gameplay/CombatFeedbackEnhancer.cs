using System.Collections;
using UnityEngine;
using MazeSpell.Audio;

namespace MazeSpell.Gameplay
{
    /// <summary>
    /// Meningkatkan feedback pertarungan (Fitur 6 & Fitur 7):
    /// - Saat Enemy terkena serangan SPACE: Hit spark particles, floating "-1 HP", camera micro-recoil.
    /// - Menangani hit sequence Level 5 Boss (Final Guardian 3 HP -> 2 -> 1 -> 0):
    ///   Menampilkan sisa HP [2/3], [1/3], dan selebrasi kekalahan saat HP = 0.
    /// - Memastikan reward +1 nyawa HANYA diberikan tepat satu kali saat HP benar-benar 0.
    /// </summary>
    public class CombatFeedbackEnhancer : MonoBehaviour
    {
        public static CombatFeedbackEnhancer Instance { get; private set; }

        private EnemyCharacter trackedEnemy;
        private int lastObservedHealth = -1;
        private bool hasHookedDefeat = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            if (!hasHookedDefeat)
            {
                EnemyCharacter.OnEnemyDefeated += OnEnemyDefeatedHandler;
                hasHookedDefeat = true;
            }
        }

        private void OnDisable()
        {
            if (hasHookedDefeat)
            {
                EnemyCharacter.OnEnemyDefeated -= OnEnemyDefeatedHandler;
                hasHookedDefeat = false;
            }
        }

        private void Update()
        {
            var active = EnemyCharacter.ActiveEnemy;
            if (active != null && active != trackedEnemy)
            {
                trackedEnemy = active;
                lastObservedHealth = active.Health;
            }

            if (trackedEnemy != null && !trackedEnemy.IsDefeated)
            {
                // Deteksi pengurangan HP
                if (trackedEnemy.Health < lastObservedHealth)
                {
                    int damageTaken = lastObservedHealth - trackedEnemy.Health;
                    lastObservedHealth = trackedEnemy.Health;
                    OnEnemyDamaged(trackedEnemy, damageTaken, trackedEnemy.Health, trackedEnemy.MaxHealth);
                }
                else if (trackedEnemy.Health > lastObservedHealth)
                {
                    lastObservedHealth = trackedEnemy.Health;
                }
            }
        }

        private void OnEnemyDamaged(EnemyCharacter enemy, int damage, int currentHP, int maxHP)
        {
            Vector3 enemyPos = enemy.transform.position;

            // 1. Partikel percikan benturan pedang (Hit Sparks)
            ElementalParticleSpawner.SpawnHitImpactEffect(enemyPos);

            // 2. Floating HP status
            string hpText = maxHP > 1 ? $"-{damage} HP [{currentHP}/{maxHP}]" : $"-{damage} HP";
            Color textColor = currentHP <= 1 ? new Color(1.0f, 0.25f, 0.15f, 1f) : new Color(1.0f, 0.85f, 0.3f, 1f);
            FloatingTextHelper.Show(hpText, enemyPos + Vector3.up * 0.7f, textColor, 3.2f);

            // 3. Audio & Camera micro-shake
            StartCoroutine(CameraMicroShake(0.12f, 0.06f));
        }

        private void OnEnemyDefeatedHandler(EnemyCharacter enemy)
        {
            if (enemy == null) return;
            Vector3 pos = enemy.transform.position;
            int level = enemy.CurrentLevelNumber;

            Debug.Log($"[CombatFeedbackEnhancer] ★ Enemy Level {level} berhasil dikalahkan!");

            // 1. Ledakan partikel kekalahan
            if (level == 5)
            {
                // Level 5 Boss explosion
                ElementalParticleSpawner.SpawnFlameEffect(pos);
                ElementalParticleSpawner.SpawnVictoryConfetti(pos);
                FloatingTextHelper.Show("★ FINAL GUARDIAN DEFEATED! ★", pos + Vector3.up * 0.8f, new Color(1.0f, 0.85f, 0.2f, 1f), 3.6f);
            }
            else
            {
                ElementalParticleSpawner.SpawnElementEffect(level, pos);
                FloatingTextHelper.Show("ENEMY DEFEATED! +1 ❤️", pos + Vector3.up * 0.7f, new Color(0.4f, 1.0f, 0.5f, 1f), 3.2f);
            }

            // 2. Audio Defeat
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.SFXType.EnemyDefeated, 1.0f);
            }
        }

        private IEnumerator CameraMicroShake(float duration, float magnitude)
        {
            Camera cam = Camera.main;
            if (cam == null) yield break;

            Vector3 origPos = cam.transform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float x = (UnityEngine.Random.value * 2f - 1f) * magnitude;
                float y = (UnityEngine.Random.value * 2f - 1f) * magnitude;
                cam.transform.position = new Vector3(origPos.x + x, origPos.y + y, origPos.z);
                yield return null;
            }
            cam.transform.position = origPos;
        }
    }
}
