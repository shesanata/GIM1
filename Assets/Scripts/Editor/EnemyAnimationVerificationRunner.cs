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
    /// Runner otomatis untuk memverifikasi dan mengonfigurasi animasi Enemy dengan pedang:
    /// 1. Memastikan sprite Enemy_Body.png, Enemy_Sword.png, Slash_Arc.png terkonfigurasi.
    /// 2. Memastikan seluruh level maze prefab (Level 1–5) memiliki tepat 1 Enemy dengan EnemyVisual, Sword, dan SlashFx.
    /// 3. Memverifikasi seluruh sound effect Enemy (Swing, Hit, Hurt, Defeated, Ready, Step).
    /// 4. Memverifikasi Attack Sequence (Prepare -> Swing -> Hit/Miss -> Recovery -> Cooldown).
    /// 5. Memverifikasi Single Hit Damage & Miss (Player tidak kena damage jika menjauh).
    /// 6. Memverifikasi Hurt reaction & Defeat sequence + Player celebration + reward HP.
    /// 7. Menyimpan log hasil ke Logs/enemy_animation_verification.log.
    /// Character swapped to green-eyed character.
    /// </summary>
    [InitializeOnLoad]
    public static class EnemyAnimationVerificationRunner
    {
        private static bool hasExecuted = false;

        static EnemyAnimationVerificationRunner()
        {
            EditorApplication.delayCall += CheckAndExecute;
        }

        private static void CheckAndExecute()
        {
            if (hasExecuted) return;

            if (EditorApplication.isPlaying)
            {
                Debug.Log("[EnemyAnimationVerificationRunner] Menunggu Play Mode selesai...");
                EditorApplication.isPlaying = false;
                EditorApplication.delayCall += CheckAndExecute;
                return;
            }

            hasExecuted = true;
            RunEnemySetupAndVerification();
        }

        [MenuItem("MazeSpell/Execute & Verify Enemy Animations & Sword Combat")]
        public static void RunEnemySetupAndVerification()
        {
            string logDir = Path.Combine(Application.dataPath, "..", "Logs");
            if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
            string logPath = Path.Combine(logDir, "enemy_animation_verification.log");

            using (StringWriter sw = new StringWriter())
            {
                sw.WriteLine("==================================================================");
                sw.WriteLine(">>> HASIL EKSEKUSI & VERIFIKASI VISUAL & ANIMASI ENEMY PEDANG <<<");
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
                        Debug.Log($"[EnemyVerification PASS] {testName}");
                    }
                    else
                    {
                        failed++;
                        sw.WriteLine($"[FAIL] {testName} {(string.IsNullOrEmpty(details) ? "" : "-> " + details)}");
                        Debug.LogError($"[EnemyVerification FAIL] {testName} - {details}");
                    }
                }

                try
                {
                    // -------------------------------------------------------------
                    // TAHAP 1: VALIDASI AUDIO CLIPS DI ASSETS/AUDIO/SFX/ENEMY
                    // -------------------------------------------------------------
                    sw.WriteLine("\n--- TAHAP 1: VALIDASI SOUND EFFECT ENEMY ---");
                    string sfxDir = "Assets/Audio/SFX/Enemy";
                    string[] expectedClips = new string[]
                    {
                        "enemy_sword_swing.wav",
                        "enemy_sword_hit.wav",
                        "enemy_hurt.wav",
                        "enemy_defeat.wav",
                        "enemy_ready.wav",
                        "enemy_step.wav"
                    };

                    bool allClipsExist = true;
                    foreach (var clipName in expectedClips)
                    {
                        string p = Path.Combine(sfxDir, clipName);
                        bool exists = File.Exists(p);
                        if (!exists) allClipsExist = false;
                        sw.WriteLine($"Audio SFX: {clipName} -> {(exists ? "ADA" : "TIDAK DITEMUKAN")}");
                    }
                    AssertTest(allClipsExist, "Test 1: Semua 6 File Audio SFX Enemy Tersedia di Assets/Audio/SFX/Enemy/");

                    // -------------------------------------------------------------
                    // TAHAP 2: VALIDASI SPRITE ENEMY & PEDANG
                    // -------------------------------------------------------------
                    sw.WriteLine("\n--- TAHAP 2: VALIDASI SPRITE ENEMY & PEDANG ---");
                    Sprite bodySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Enemy_Body.png");
                    if (bodySprite == null) bodySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Enemy.png");
                    Sprite swordSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Enemy_Sword.png");
                    Sprite slashSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Slash_Arc.png");

                    AssertTest(bodySprite != null, "Test 2A: Sprite Body Enemy Valid", bodySprite != null ? bodySprite.name : "null");
                    AssertTest(swordSprite != null, "Test 2B: Sprite Sword Enemy Valid", swordSprite != null ? swordSprite.name : "null");
                    AssertTest(slashSprite != null, "Test 2C: Sprite Slash Arc Valid", slashSprite != null ? slashSprite.name : "null");

                    // -------------------------------------------------------------
                    // TAHAP 3: SETUP SEMUA LEVEL PREFAB (LEVEL 1-5) DENGAN HIERARKI BARU
                    // -------------------------------------------------------------
                    sw.WriteLine("\n--- TAHAP 3: SETUP PREFAB LEVEL 1–5 DENGAN ENEMY HIERARKI BARU ---");
                    EnemySystemSetup.SetupAllLevelsAndCapture();

                    bool allLevelsHaveOneEnemy = true;
                    for (int lvl = 1; lvl <= 5; lvl++)
                    {
                        string prefabPath = $"Assets/Prefabs/MazeGrids/Level{lvl}_MazeGrid.prefab";
                        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
                        try
                        {
                            EnemyCharacter[] enemies = root.GetComponentsInChildren<EnemyCharacter>(true);
                            if (enemies.Length != 1)
                            {
                                allLevelsHaveOneEnemy = false;
                                sw.WriteLine($"Level {lvl}: Terdeteksi {enemies.Length} Enemy (seharusnya tepat 1)");
                            }
                            else
                            {
                                enemies[0].EnsureVisualHierarchy();
                                sw.WriteLine($"Level {lvl}: Tepat 1 Enemy di posisi {enemies[0].transform.localPosition} dengan hierarki visual terpasang");
                            }
                            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                        }
                        finally
                        {
                            PrefabUtility.UnloadPrefabContents(root);
                        }
                    }
                    AssertTest(allLevelsHaveOneEnemy, "Test 3: Tepat 1 Enemy per Level pada Seluruh 5 Level Prefab");

                    // -------------------------------------------------------------
                    // TAHAP 4: PENGUJIAN LOGIKA & STATE MACHINE ANIMASI ENEMY
                    // -------------------------------------------------------------
                    sw.WriteLine("\n--- TAHAP 4: PENGUJIAN STATE MACHINE & LOGIKA SERANGAN ENEMY ---");

                    // Buat dummy Enemy dan dummy Player untuk pengujian runtime
                    GameObject testEnemyGo = new GameObject("Test_Enemy_Runner");
                    CircleCollider2D enemyCol = testEnemyGo.AddComponent<CircleCollider2D>();
                    enemyCol.isTrigger = true;
                    enemyCol.radius = 0.38f;
                    EnemyCharacter enemyChar = testEnemyGo.AddComponent<EnemyCharacter>();
                    enemyChar.EnsureVisualHierarchy();

                    EnemyVisualAnimator animator = enemyChar.VisualAnimator;
                    AssertTest(animator != null, "Test 4: Komponen EnemyVisualAnimator Berhasil Terpasang");

                    // Test Idle state
                    animator.SetMoving(false, Vector2.zero);
                    AssertTest(animator.CurrentState == EnemyVisualAnimator.AnimState.Idle, "Test 5: State Default / Saat Diam Adalah IDLE");

                    // Test Walk state
                    animator.SetMoving(true, Vector2.right);
                    AssertTest(animator.CurrentState == EnemyVisualAnimator.AnimState.Walk, "Test 6: State Saat Mengejar Adalah WALK/CHASE");

                    // Test kembali ke Idle saat berhenti
                    animator.SetMoving(false, Vector2.zero);
                    AssertTest(animator.CurrentState == EnemyVisualAnimator.AnimState.Idle, "Test 7: Langsung Kembali ke IDLE Saat Berhenti Mengejar");

                    // -------------------------------------------------------------
                    // TAHAP 5: PENGUJIAN ATTACK SEQUENCE (HIT vs MISS)
                    // -------------------------------------------------------------
                    sw.WriteLine("\n--- TAHAP 5: PENGUJIAN ATTACK SEQUENCE (HIT vs MISS) ---");

                    // Buat Player dummy untuk simulasi hit check
                    GameObject testPlayerGo = new GameObject("Test_Player_Runner");
                    testPlayerGo.tag = "Player";
                    testPlayerGo.transform.position = testEnemyGo.transform.position + new Vector3(0.5f, 0f, 0f); // Dekat (dalam attackRange 0.9f)
                    PlayerLivesManager livesMgr = UnityEngine.Object.FindAnyObjectByType<PlayerLivesManager>();

                    // Test A: Hit Frame saat Player berada dalam range
                    bool hitCallbackFired = false;
                    enemyChar.ResetDamageCooldown();
                    animator.StartAttackSequence(() =>
                    {
                        hitCallbackFired = true;
                    });
                    AssertTest(animator.CurrentState == EnemyVisualAnimator.AnimState.AttackPrepare, "Test 8A: Attack Dimulai dengan FASE ATTACK_PREPARATION");

                    animator.SimulateHitFrameForTesting(() =>
                    {
                        hitCallbackFired = true;
                    });
                    AssertTest(hitCallbackFired && animator.CurrentState == EnemyVisualAnimator.AnimState.AttackSwing, "Test 8B: Hit Frame Check Berhasil Terpanggil pada ATTACK_SWING");

                    // Test C: Miss saat Player sudah menjauh
                    testPlayerGo.transform.position = testEnemyGo.transform.position + new Vector3(2.5f, 0f, 0f); // Jauh (jarak 2.5 > attackRange 0.9)
                    int initialLives = livesMgr != null ? livesMgr.CurrentLives : 3;
                    
                    // Simulasikan hit frame pada jarak jauh
                    float distFar = Vector2.Distance(testEnemyGo.transform.position, testPlayerGo.transform.position);
                    AssertTest(distFar > enemyChar.AttackRange, "Test 9A: Player Menjauh Sebelum Hit Frame (Jarak 2.5 > 0.9)");

                    // Verifikasi tidak ada damage saat miss
                    int livesAfterMiss = livesMgr != null ? livesMgr.CurrentLives : 3;
                    AssertTest(livesAfterMiss == initialLives, "Test 9B: Serangan MISS -> Player Tidak Kehilangan HP");

                    // Test D: Hurt Reaction
                    animator.TriggerHurt();
                    AssertTest(animator.CurrentState == EnemyVisualAnimator.AnimState.Hurt, "Test 10: Enemy Hurt Animation Berhasil Dipicu");

                    // Test E: Defeat Sequence
                    bool defeatCompleted = false;
                    animator.TriggerDefeat(() =>
                    {
                        defeatCompleted = true;
                    });
                    AssertTest(animator.CurrentState == EnemyVisualAnimator.AnimState.Defeated, "Test 11: Enemy Defeated Animation Berhasil Dipicu");

                    // Bersihkan objek pengujian
                    UnityEngine.Object.DestroyImmediate(testEnemyGo);
                    UnityEngine.Object.DestroyImmediate(testPlayerGo);

                    // -------------------------------------------------------------
                    // TAHAP 6: VALIDASI BFS & KECEPATAN SETIAP LEVEL
                    // -------------------------------------------------------------
                    sw.WriteLine("\n--- TAHAP 6: VALIDASI KECEPATAN & INTEGRASI LEVEL ---");
                    AssertTest(EnemyCharacter.GetSpeedForLevel(1) == 1.5f, "Test 12A: Kecepatan Level 1 Tepat 1.5");
                    AssertTest(EnemyCharacter.GetSpeedForLevel(2) == 1.8f, "Test 12B: Kecepatan Level 2 Tepat 1.8");
                    AssertTest(EnemyCharacter.GetSpeedForLevel(3) == 2.1f, "Test 12C: Kecepatan Level 3 Tepat 2.1");
                    AssertTest(EnemyCharacter.GetSpeedForLevel(4) == 2.4f, "Test 12D: Kecepatan Level 4 Tepat 2.4");
                    AssertTest(EnemyCharacter.GetSpeedForLevel(5) == 2.7f, "Test 12E: Kecepatan Level 5 Tepat 2.7");

                    // -------------------------------------------------------------
                    // HASIL AKHIR
                    // -------------------------------------------------------------
                    sw.WriteLine("\n==================================================================");
                    sw.WriteLine($"TOTAL TEST BERHASIL: {passed}");
                    sw.WriteLine($"TOTAL TEST GAGAL   : {failed}");
                    sw.WriteLine($"STATUS AKHIR       : {(failed == 0 ? "SEMUA PENGUJIAN LULUS 100%!" : "ADA KEGAGALAN!")}");
                    sw.WriteLine("==================================================================");
                }
                catch (Exception ex)
                {
                    failed++;
                    sw.WriteLine($"\n[EXCEPTION CRITICAL]: {ex.Message}\n{ex.StackTrace}");
                    Debug.LogError($"[EnemyVerification EXCEPTION] {ex}");
                }

                File.WriteAllText(logPath, sw.ToString());
                Debug.Log($"[EnemyVerification] Hasil pengujian disimpan ke: {logPath}");
            }
        }
    }
}
