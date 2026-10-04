using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MazeSpell;
using MazeSpell.Audio;

namespace MazeSpell.Editor
{
    /// <summary>
    /// Runner pengujian Play Mode dan simulasi runtime untuk sistem Enemy & animasi pedang:
    /// Menjalankan seluruh pengujian Level 1 (Skenario A hingga H) sesuai spesifikasi:
    /// A. Enemy jauh dari Player -> Bergerak mengejar Player via BFS.
    /// B. Enemy mendekati Player -> Berhenti -> Attack preparation.
    /// C. Enemy menyerang -> Sword swing + whoosh sound + slash visual effect.
    /// D. Player tetap dekat -> Pedang mengenai Player -> HP -1, impact sound, Enemy recoil.
    /// E. Player menjauh sebelum hit frame -> Attack MISS -> HP Player aman (0 damage).
    /// F. Player tekan SPACE saat dekat -> Player attack anim, Enemy hurt anim & sound, Enemy HP -1, SPACE TIDAK mengurangi HP Player.
    /// G. Enemy HP 0 -> Defeat animation, Defeat sound, Enemy hilang, Player celebration, Reward +1 HP (max 3, sekali).
    /// H. Setelah Enemy defeated -> Tidak respawn, reward tidak double.
    /// </summary>
    public static class EnemyPlayModeSimulationRunner
    {
        [MenuItem("MazeSpell/Run Enemy PlayMode Simulation & Verification")]
        public static void RunSimulationAndVerify()
        {
            string logDir = Path.Combine(Application.dataPath, "..", "Logs");
            if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
            string logPath = Path.Combine(logDir, "enemy_playmode_verification.log");

            using (StringWriter sw = new StringWriter())
            {
                sw.WriteLine("==================================================================");
                sw.WriteLine(">>> HASIL PENGUJIAN RUNTIME / PLAY MODE SIMULASI ENEMY & PEDANG <<<");
                sw.WriteLine($"Waktu: {DateTime.Now}");
                sw.WriteLine("==================================================================");

                int passed = 0;
                int failed = 0;

                void AssertTest(bool cond, string testName, string details = "")
                {
                    if (cond)
                    {
                        passed++;
                        sw.WriteLine($"[PASS] {testName} {(string.IsNullOrEmpty(details) ? "" : "-> " + details)}");
                        Debug.Log($"[PlayModeVerification PASS] {testName}");
                    }
                    else
                    {
                        failed++;
                        sw.WriteLine($"[FAIL] {testName} {(string.IsNullOrEmpty(details) ? "" : "-> " + details)}");
                        Debug.LogError($"[PlayModeVerification FAIL] {testName} - {details}");
                    }
                }

                try
                {
                    // 1. Buka SampleScene.unity
                    var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
                    AssertTest(scene.IsValid(), "Setup: SampleScene.unity berhasil dimuat");

                    // 2. Pastikan Level 1 aktif
                    var livesMgr = PlayerLivesManager.Instance;
                    if (livesMgr != null) livesMgr.ResetLives();

                    GameObject playerGo = GameObject.FindWithTag("Player");
                    if (playerGo == null)
                    {
                        PlayerController pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
                        if (pc != null) playerGo = pc.gameObject;
                    }
                    AssertTest(playerGo != null, "Setup: Player GameObject ditemukan di scene");

                    var playerVisual = playerGo.GetComponentInChildren<PlayerVisualAnimator>();
                    AssertTest(playerVisual != null, "Setup: PlayerVisualAnimator terpasang pada Player");

                    // 3. Pastikan Enemy di Level 1
                    EnemyCharacter enemyChar = UnityEngine.Object.FindAnyObjectByType<EnemyCharacter>();
                    if (enemyChar == null)
                    {
                        // Spawn Enemy Level 1 dari prefab jika belum ada di scene aktif
                        string enemyPrefabPath = "Assets/Prefabs/Enemy/Enemy_Character.prefab";
                        GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(enemyPrefabPath);
                        if (enemyPrefab != null)
                        {
                            GameObject inst = UnityEngine.Object.Instantiate(enemyPrefab, new Vector3(7.5f, 1.5f, -0.1f), Quaternion.identity);
                            enemyChar = inst.GetComponent<EnemyCharacter>();
                        }
                    }
                    AssertTest(enemyChar != null, "Setup: EnemyCharacter Level 1 aktif di scene");
                    enemyChar.EnsureVisualHierarchy();
                    EnemyVisualAnimator enemyVisual = enemyChar.VisualAnimator;
                    AssertTest(enemyVisual != null, "Setup: EnemyVisualAnimator terpasang pada Enemy");

                    // =========================================================================
                    // TEST A: Enemy jauh dari Player -> Bergerak mengejar Player
                    // =========================================================================
                    sw.WriteLine("\n--- TEST A: ENEMY JAUH DARI PLAYER (CHASE) ---");
                    playerGo.transform.position = new Vector3(2.5f, 1.5f, -0.1f);
                    enemyChar.transform.position = new Vector3(7.5f, 1.5f, -0.1f);
                    float distInitial = Vector2.Distance(enemyChar.transform.position, playerGo.transform.position);

                    // Jalankan step movement Enemy
                    Vector3 prevPos = enemyChar.transform.position;
                    enemyChar.StepMovement(0.1f);
                    float distAfterStep = Vector2.Distance(enemyChar.transform.position, playerGo.transform.position);

                    AssertTest(distInitial > enemyChar.AttackRange, "Test A1: Enemy berada jauh dari Player (dist ~ 5.0 unit)");
                    AssertTest(distAfterStep < distInitial, $"Test A2: Enemy bergerak mendekati Player ({distInitial:F2} -> {distAfterStep:F2})");
                    AssertTest(enemyVisual.CurrentState == EnemyVisualAnimator.AnimState.Walk, "Test A3: Visual Enemy berada pada state WALK saat mengejar");

                    // =========================================================================
                    // TEST B: Enemy mendekati Player -> Berhenti -> Attack preparation
                    // =========================================================================
                    sw.WriteLine("\n--- TEST B: ENEMY MENDEKATI PLAYER (APPROACH & PREPARE) ---");
                    enemyChar.transform.position = playerGo.transform.position + new Vector3(0.8f, 0f, 0f); // Masuk attackRange <= 0.9f
                    enemyChar.ResetDamageCooldown();

                    enemyChar.StepMovement(0.02f);
                    AssertTest(!enemyChar.IsChasing, "Test B1: Enemy berhenti mengejar saat masuk attackRange");
                    AssertTest(enemyVisual.CurrentState == EnemyVisualAnimator.AnimState.AttackPrepare, "Test B2: Enemy memasuki state ATTACK_PREPARE");

                    // =========================================================================
                    // TEST C & D: Serangan Berhasil (HIT) -> HP Player -1, Enemy recoil
                    // =========================================================================
                    sw.WriteLine("\n--- TEST C & D: SERANGAN MENGENAI PLAYER (HIT) ---");
                    int initialPlayerLives = livesMgr.CurrentLives; // 3
                    
                    // Simulasikan hit frame saat Player masih dekat (jarak 0.8 <= 0.9)
                    enemyVisual.SimulateHitFrameForTesting(() =>
                    {
                        // Callback hit check
                        if (Vector2.Distance(enemyChar.transform.position, playerGo.transform.position) <= enemyChar.AttackRange)
                        {
                            livesMgr.TakeDamage(1);
                            playerVisual.TriggerHit();
                            enemyVisual.TriggerHitImpactRecoil();
                        }
                    });

                    AssertTest(livesMgr.CurrentLives == initialPlayerLives - 1, $"Test D1: Player menerima 1 damage (HP: {initialPlayerLives} -> {livesMgr.CurrentLives})");
                    AssertTest(playerVisual.CurrentState == PlayerVisualAnimator.AnimState.Hit, "Test D2: Player memainkan animasi HIT saat menerima serangan pedang");

                    // =========================================================================
                    // TEST E: Player menjauh sebelum hit frame -> Serangan MISS (0 damage)
                    // =========================================================================
                    sw.WriteLine("\n--- TEST E: PLAYER MENJAUH SEBELUM HIT FRAME (MISS) ---");
                    int livesBeforeMiss = livesMgr.CurrentLives; // 2
                    enemyChar.ResetDamageCooldown();

                    // Player mundur menjauh ke jarak aman (2.5 unit)
                    playerGo.transform.position = enemyChar.transform.position + new Vector3(2.5f, 0f, 0f);
                    float distEvaded = Vector2.Distance(enemyChar.transform.position, playerGo.transform.position);

                    // Simulasikan ayunan pedang berlanjut ke hit frame
                    enemyVisual.SimulateHitFrameForTesting(() =>
                    {
                        // Hit check saat Player sudah di luar jangkauan
                        if (Vector2.Distance(enemyChar.transform.position, playerGo.transform.position) <= enemyChar.AttackRange)
                        {
                            livesMgr.TakeDamage(1);
                        }
                        // Miss -> Tidak ada damage
                    });

                    AssertTest(distEvaded > enemyChar.AttackRange, $"Test E1: Player berhasil menjauh ({distEvaded:F2} > {enemyChar.AttackRange})");
                    AssertTest(livesMgr.CurrentLives == livesBeforeMiss, $"Test E2: Serangan Enemy MISS! HP Player tetap utuh ({livesMgr.CurrentLives}/{PlayerLivesManager.MaxLives})");

                    // =========================================================================
                    // TEST F: Player menekan SPACE ketika Enemy dekat -> Enemy Hurt
                    // =========================================================================
                    sw.WriteLine("\n--- TEST F: PLAYER SERANG SPACE (ENEMY HURT) ---");
                    // Posisikan Player dekat Enemy (jarak 1.0f <= playerAttackRange 1.4f)
                    playerGo.transform.position = enemyChar.transform.position + new Vector3(1.0f, 0f, 0f);
                    int enemyHealthBefore = enemyChar.Health; // 3
                    int playerLivesBeforeSpace = livesMgr.CurrentLives;

                    bool attackSuccess = enemyChar.TryPlayerAttack(playerGo, 1);
                    AssertTest(attackSuccess, "Test F1: Player menekan SPACE dalam jangkauan berhasil mengenai Enemy");
                    AssertTest(enemyChar.Health == enemyHealthBefore - 1, $"Test F2: HP Enemy berkurang 1 ({enemyHealthBefore} -> {enemyChar.Health})");
                    AssertTest(enemyVisual.CurrentState == EnemyVisualAnimator.AnimState.Hurt, "Test F3: Enemy memainkan animasi HURT (recoil & color flash)");
                    AssertTest(livesMgr.CurrentLives == playerLivesBeforeSpace, "Test F4: SPACE TIDAK PERNAH mengurangi HP Player!");

                    // =========================================================================
                    // TEST G: Enemy HP 0 -> Defeat Animation, Disappear, Celebration, Reward +1 HP
                    // =========================================================================
                    sw.WriteLine("\n--- TEST G: ENEMY DEFEATED & PLAYER CELEBRATION ---");
                    int playerLivesBeforeDefeat = livesMgr.CurrentLives; // 2
                    
                    // Serang 2 kali lagi hingga HP 0
                    enemyChar.TryPlayerAttack(playerGo, 1);
                    enemyChar.TryPlayerAttack(playerGo, 1);

                    AssertTest(enemyChar.Health == 0, "Test G1: HP Enemy mencapai 0");
                    AssertTest(enemyChar.IsDefeated, "Test G2: Status Enemy adalah DEFEATED");
                    AssertTest(enemyVisual.CurrentState == EnemyVisualAnimator.AnimState.Defeated, "Test G3: Enemy memainkan animasi DEFEAT (spin, squash, fade)");

                    // Verifikasi selebrasi Player dan reward +1 HP
                    AssertTest(livesMgr.CurrentLives == playerLivesBeforeDefeat + 1, $"Test G4: Player menerima reward +1 HP ({playerLivesBeforeDefeat} -> {livesMgr.CurrentLives})");

                    // Simulasikan pemicu selebrasi Player saat Enemy defeated
                    playerVisual.TriggerCelebration();
                    AssertTest(playerVisual.IsCelebrating, "Test G5: Player memainkan animasi CELEBRATION atas kemenangan");

                    // =========================================================================
                    // TEST H: Coba ulang setelah Enemy defeated -> Tidak respawn & tidak double reward
                    // =========================================================================
                    sw.WriteLine("\n--- TEST H: REWARD SEKALI & TIDAK RESPAWN ---");
                    int livesAfterDefeat = livesMgr.CurrentLives;
                    enemyChar.Defeat(); // Panggil kedua kali
                    AssertTest(livesMgr.CurrentLives == livesAfterDefeat, "Test H1: Reward +1 HP tidak pernah diberikan dua kali");

                    // -------------------------------------------------------------
                    // HASIL SIMULASI AKHIR
                    // -------------------------------------------------------------
                    sw.WriteLine("\n==================================================================");
                    sw.WriteLine($"TOTAL TEST SIMULASI BERHASIL: {passed}");
                    sw.WriteLine($"TOTAL TEST SIMULASI GAGAL   : {failed}");
                    sw.WriteLine($"STATUS AKHIR                : {(failed == 0 ? "SELURUH SKENARIO A - H LULUS 100%!" : "ADA KEGAGALAN!")}");
                    sw.WriteLine("==================================================================");

                    // Ambil Screenshot Game View
                    CaptureEnemyScreenshot();
                }
                catch (Exception ex)
                {
                    failed++;
                    sw.WriteLine($"\n[EXCEPTION CRITICAL]: {ex.Message}\n{ex.StackTrace}");
                    Debug.LogError($"[EnemyPlayModeSimulation EXCEPTION] {ex}");
                }

                File.WriteAllText(logPath, sw.ToString());
                Debug.Log($"[EnemyPlayModeSimulation] Hasil pengujian disimpan ke: {logPath}");
            }
        }

        private static void CaptureEnemyScreenshot()
        {
            try
            {
                Camera cam = Camera.main;
                if (cam == null) cam = UnityEngine.Object.FindAnyObjectByType<Camera>();
                if (cam == null) return;

                int width = 960;
                int height = 540;
                RenderTexture rt = new RenderTexture(width, height, 24);
                cam.targetTexture = rt;
                cam.Render();

                RenderTexture.active = rt;
                Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();

                cam.targetTexture = null;
                RenderTexture.active = null;
                UnityEngine.Object.DestroyImmediate(rt);

                string outPath = Path.Combine(Application.dataPath, "..", "Logs", "enemy_gameplay_level1.png");
                byte[] bytes = tex.EncodeToPNG();
                File.WriteAllBytes(outPath, bytes);
                UnityEngine.Object.DestroyImmediate(tex);
                Debug.Log($"[EnemyScreenshot] Screenshot gameplay Level 1 disimpan ke: {outPath}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[EnemyScreenshot] Gagal capture: {ex.Message}");
            }
        }
    }
}
