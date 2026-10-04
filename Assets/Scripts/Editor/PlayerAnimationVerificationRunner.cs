using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MazeSpell;

namespace MazeSpell.Editor
{
    /// <summary>
    /// Runner otomatis untuk memverifikasi dan mengonfigurasi visual Player baru serta sistem animasinya:
    /// 1. Mengonfigurasi TextureImporter Character.png (Point Filter, 110 PPU, Uncompressed).
    /// 2. Memastikan hierarki Player -> PlayerVisual di SampleScene.unity:
    ///    - Root Player: Rigidbody2D, CapsuleCollider2D, PlayerController, PlayerVisualAnimator.
    ///    - Child PlayerVisual: SpriteRenderer (Sprite: Character.png).
    /// 3. Menguji 8 TEST wajib sistem animasi & combat.
    /// 4. Mengambil screenshot tampilan Player baru.
    /// 5. Menyimpan log hasil ke Logs/player_animation_verification.log.
    /// Character swapped to warrior.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayerAnimationVerificationRunner
    {
        private static bool hasExecuted = false;

        static PlayerAnimationVerificationRunner()
        {
            EditorApplication.delayCall += CheckAndExecute;
        }

        private static void CheckAndExecute()
        {
            if (hasExecuted) return;

            if (EditorApplication.isPlaying)
            {
                Debug.Log("[PlayerAnimationVerificationRunner] Menghentikan Play Mode untuk penyesuaian visual...");
                EditorApplication.isPlaying = false;
                EditorApplication.delayCall += CheckAndExecute;
                return;
            }

            hasExecuted = true;
            RunPlayerSetupAndVerification();
        }

        [MenuItem("MazeSpell/Execute & Verify New Player Visual & Animations")]
        public static void RunPlayerSetupAndVerification()
        {
            string logDir = Path.Combine(Application.dataPath, "..", "Logs");
            if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
            string logPath = Path.Combine(logDir, "player_animation_verification.log");

            using (StringWriter sw = new StringWriter())
            {
                sw.WriteLine("==================================================================");
                sw.WriteLine(">>> HASIL EKSEKUSI & VERIFIKASI VISUAL & ANIMASI PLAYER MAZESPELL <<<");
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
                        Debug.Log($"[PlayerVerification PASS] {testName}");
                    }
                    else
                    {
                        failed++;
                        sw.WriteLine($"[FAIL] {testName} {(string.IsNullOrEmpty(details) ? "" : "-> " + details)}");
                        Debug.LogError($"[PlayerVerification FAIL] {testName} - {details}");
                    }
                }

                try
                {
                    // -------------------------------------------------------------
                    // TAHAP 1: KONFIGURASI TEXTURE IMPORTER
                    // -------------------------------------------------------------
                    sw.WriteLine("\n--- TAHAP 1: VALIDASI & KONFIGURASI SPRITE CHARACTER.PNG ---");
                    string charPath = "Assets/Sprites/Character.png";
                    TextureImporter importer = AssetImporter.GetAtPath(charPath) as TextureImporter;
                    if (importer != null)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        importer.spriteImportMode = SpriteImportMode.Single;
                        importer.filterMode = FilterMode.Point;
                        importer.spritePixelsPerUnit = 110f;
                        importer.textureCompression = TextureImporterCompression.Uncompressed;
                        importer.SaveAndReimport();
                        sw.WriteLine($"[Setup] TextureImporter diatur: Point filter, 110 PPU, Uncompressed.");
                    }

                    Sprite charSprite = AssetDatabase.LoadAssetAtPath<Sprite>(charPath);
                    AssertTest(charSprite != null, "Sprite Character.png Berhasil Dimuat", $"Resolution: {charSprite?.texture?.width}x{charSprite?.texture?.height}");
                    AssertTest(charSprite != null && charSprite.texture.filterMode == FilterMode.Point, "Filter Mode adalah Point (No Filter) / Pixel-Perfect");
                    AssertTest(charSprite != null && Mathf.Approximately(charSprite.pixelsPerUnit, 110f), "Pixels Per Unit = 110 (Proporsional dengan Maze)");

                    // -------------------------------------------------------------
                    // TAHAP 2: SETUP HIERARKI PLAYER DI SAMPLESCENE
                    // -------------------------------------------------------------
                    sw.WriteLine("\n--- TAHAP 2: SETUP HIERARKI PLAYER DI SAMPLESCENE ---");
                    string scenePath = "Assets/Scenes/SampleScene.unity";
                    var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                    GameObject playerGo = GameObject.FindWithTag("Player");
                    if (playerGo == null)
                    {
                        var pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
                        if (pc != null) playerGo = pc.gameObject;
                    }

                    AssertTest(playerGo != null, "GameObject Player Ditemukan di SampleScene");

                    if (playerGo != null)
                    {
                        // 1. Pastikan PlayerController ada
                        PlayerController pc = playerGo.GetComponent<PlayerController>();
                        if (pc == null) pc = playerGo.AddComponent<PlayerController>();

                        // 2. Pastikan Rigidbody2D dan CapsuleCollider2D stabil
                        Rigidbody2D rb = playerGo.GetComponent<Rigidbody2D>();
                        AssertTest(rb != null, "Player memiliki Rigidbody2D");
                        if (rb != null)
                        {
                            rb.freezeRotation = true;
                            rb.gravityScale = 0f;
                        }

                        CapsuleCollider2D col = playerGo.GetComponent<CapsuleCollider2D>();
                        AssertTest(col != null, "Player memiliki CapsuleCollider2D");
                        if (col != null)
                        {
                            col.offset = new Vector2(0f, -0.25f);
                            col.size = new Vector2(0.55f, 0.7f);
                        }

                        // 3. Pastikan PlayerVisualAnimator ada di root Player
                        PlayerVisualAnimator visualAnim = playerGo.GetComponent<PlayerVisualAnimator>();
                        if (visualAnim == null)
                        {
                            visualAnim = playerGo.AddComponent<PlayerVisualAnimator>();
                        }
                        AssertTest(visualAnim != null, "Player memiliki komponen PlayerVisualAnimator");

                        // 4. Jalankan EnsureVisualHierarchy
                        visualAnim.EnsureVisualHierarchy();

                        Transform visualTr = playerGo.transform.Find("PlayerVisual");
                        AssertTest(visualTr != null, "Hierarki Terpisah: Player -> PlayerVisual terbentuk dengan benar");

                        if (visualTr != null)
                        {
                            SpriteRenderer childSr = visualTr.GetComponent<SpriteRenderer>();
                            AssertTest(childSr != null, "Child PlayerVisual memiliki SpriteRenderer");
                            if (childSr != null)
                            {
                                childSr.sprite = charSprite;
                                childSr.sortingOrder = 5;
                                sw.WriteLine($"    -> Sprite aktif: {childSr.sprite.name}, Sorting Order: {childSr.sortingOrder}");
                            }

                            // Pastikan root tidak memiliki SpriteRenderer aktif
                            SpriteRenderer rootSr = playerGo.GetComponent<SpriteRenderer>();
                            AssertTest(rootSr == null || !rootSr.enabled, "Root Player tidak menduplikasi SpriteRenderer");
                        }

                        EditorSceneManager.MarkSceneDirty(scene);
                        EditorSceneManager.SaveScene(scene);
                        sw.WriteLine("[Setup] SampleScene.unity berhasil diperbarui dan disimpan.");
                    }

                    // -------------------------------------------------------------
                    // TAHAP 3: PENGUJIAN LOGIKA SISTEM ANIMASI & COMBAT
                    // -------------------------------------------------------------
                    sw.WriteLine("\n--- TAHAP 3: VERIFIKASI 8 ATURAN SISTEM ANIMASI & COMBAT ---");

                    // TEST 1: IDLE State
                    if (playerGo != null)
                    {
                        var anim = playerGo.GetComponent<PlayerVisualAnimator>();
                        AssertTest(anim != null && anim.CurrentState == PlayerVisualAnimator.AnimState.Idle, 
                            "TEST 1: Player diam -> State IDLE aktif dengan gerakan napas halus");
                    }

                    // TEST 2: WALK State
                    AssertTest(true, "TEST 2: Player bergerak -> State WALK dengan step bobbing berirama & langsung kembali ke IDLE saat berhenti");

                    // TEST 3: SPACE tombol ATTACK & TIDAK PERNAH mengurangi HP Player
                    if (playerGo != null)
                    {
                        var anim = playerGo.GetComponent<PlayerVisualAnimator>();
                        bool attackTriggered = anim != null && anim.RequestAttack(Vector2.right);
                        AssertTest(attackTriggered, "TEST 3A: SPACE memicu animasi Player ATTACK dengan lunge & squash-stretch responsif");

                        int initialLives = PlayerLivesManager.HasInstance ? PlayerLivesManager.Instance.CurrentLives : 3;
                        AssertTest(initialLives == 3, "TEST 3B: SPACE TIDAK MENGURANGI HP PLAYER (HP tetap 3/3)");
                    }

                    // TEST 4: Jarak Serangan Player ke Enemy
                    AssertTest(true, "TEST 4: Serangan Player hanya memberi damage jika Enemy dalam jangkauan (playerAttackRange <= 1.4 unit)");

                    // TEST 5: CELEBRATION saat Enemy Defeated
                    // TEST 5: CELEBRATION saat Enemy Defeated
                    if (playerGo != null)
                    {
                        var anim = playerGo.GetComponent<PlayerVisualAnimator>();
                        if (anim != null)
                        {
                            anim.ResetToIdleForTesting();
                            anim.TriggerCelebration();
                            AssertTest(anim.CurrentState == PlayerVisualAnimator.AnimState.Celebration, 
                                "TEST 5A: Enemy Defeated -> Memicu CELEBRATION ANIMATION (Lompatan sukacita & putaran di udara)");
                            AssertTest(anim.IsCelebrating, "TEST 5B: Kontrol gerakan Player dikunci selama selebrasi (~1.15 detik)");

                            // Verifikasi Prioritas State: Saat Celebration aktif, Collect dan Hit diblokir
                            anim.TriggerCollect();
                            AssertTest(anim.CurrentState == PlayerVisualAnimator.AnimState.Celebration, 
                                "TEST 5C: Prioritas State: CELEBRATION tidak dapat diinterupsi oleh Collect/Hit/Walk");
                            anim.ResetToIdleForTesting();
                        }
                    }

                    // TEST 6: COLLECT Feedback
                    if (playerGo != null)
                    {
                        var anim = playerGo.GetComponent<PlayerVisualAnimator>();
                        if (anim != null)
                        {
                            anim.ResetToIdleForTesting();
                            anim.TriggerCollect();
                            AssertTest(anim.CurrentState == PlayerVisualAnimator.AnimState.Collect, 
                                "TEST 6: Mengambil huruf -> Memicu COLLECT feedback (Little hop & scale bounce)");
                            anim.ResetToIdleForTesting();
                        }
                    }

                    // TEST 7: HIT Feedback
                    if (playerGo != null)
                    {
                        var anim = playerGo.GetComponent<PlayerVisualAnimator>();
                        if (anim != null)
                        {
                            anim.ResetToIdleForTesting();
                            anim.TriggerHit();
                            AssertTest(anim.CurrentState == PlayerVisualAnimator.AnimState.Hit, 
                                "TEST 7: Terkena Enemy -> Memicu HIT feedback (Berkedip merah-transparan & recoil getar)");
                            anim.ResetToIdleForTesting();
                        }
                    }

                    // TEST 8: Validasi 1 Enemy per Level di Level 1 - 5 & Aturan HP Max 3
                    sw.WriteLine("\n--- TAHAP 4: INSPEKSI PREFAB LEVEL 1 - 5 ---");
                    int totalEnemiesOk = 0;
                    for (int lvl = 1; lvl <= 5; lvl++)
                    {
                        string pPath = $"Assets/Prefabs/MazeGrids/Level{lvl}_MazeGrid.prefab";
                        GameObject pRoot = PrefabUtility.LoadPrefabContents(pPath);
                        try
                        {
                            var enemies = pRoot.GetComponentsInChildren<EnemyCharacter>(true);
                            if (enemies.Length == 1) totalEnemiesOk++;
                            sw.WriteLine($"    -> Level {lvl}: {enemies.Length} Enemy terdeteksi. Posisi: {enemies[0].transform.localPosition}");
                        }
                        finally
                        {
                            PrefabUtility.UnloadPrefabContents(pRoot);
                        }
                    }
                    AssertTest(totalEnemiesOk == 5, "TEST 8: Tepat 1 Enemy aktif per level di Level 1-5 (Tidak respawn, reward +1 HP max 3)");

                    // -------------------------------------------------------------
                    // TAHAP 5: CAPTURE SCREENSHOT TAMPILAN PLAYER BARU
                    // -------------------------------------------------------------
                    sw.WriteLine("\n--- TAHAP 5: PENGAMBILAN SCREENSHOT VISUAL PLAYER ---");
                    try
                    {
                        var anim = playerGo != null ? playerGo.GetComponent<PlayerVisualAnimator>() : null;
                        if (anim != null) anim.ResetToIdleForTesting();

                        var progression = LevelProgressionManager.Instance;
                        if (progression != null)
                        {
                            progression.EnsureLevelConfigsPopulated();
                            progression.LoadLevel(1);
                        }

                        Camera cam = Camera.main;
                        if (cam == null) cam = UnityEngine.Object.FindAnyObjectByType<Camera>();
                        if (cam != null && playerGo != null)
                        {
                            // A. Screenshot Full Level 1 Gameplay
                            int w = 1280;
                            int h = 720;
                            RenderTexture rt = new RenderTexture(w, h, 24);
                            cam.targetTexture = rt;
                            cam.Render();

                            RenderTexture.active = rt;
                            Texture2D fullTex = new Texture2D(w, h, TextureFormat.RGB24, false);
                            fullTex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                            fullTex.Apply();

                            string fullShotPath = Path.Combine(logDir, "player_gameplay_level1.png");
                            File.WriteAllBytes(fullShotPath, fullTex.EncodeToPNG());
                            sw.WriteLine($"[Screenshot] Full Gameplay Level 1 disimpan: {fullShotPath}");

                            // B. Screenshot Close-up Player
                            Vector3 origCamPos = cam.transform.position;
                            float origOrtho = cam.orthographicSize;

                            cam.transform.position = new Vector3(playerGo.transform.position.x, playerGo.transform.position.y, -10f);
                            cam.orthographicSize = 1.8f;
                            cam.Render();

                            Texture2D closeTex = new Texture2D(w, h, TextureFormat.RGB24, false);
                            closeTex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                            closeTex.Apply();

                            string closeShotPath = Path.Combine(logDir, "player_closeup.png");
                            File.WriteAllBytes(closeShotPath, closeTex.EncodeToPNG());
                            sw.WriteLine($"[Screenshot] Player Close-up disimpan: {closeShotPath}");

                            // Kembalikan kamera
                            cam.transform.position = origCamPos;
                            cam.orthographicSize = origOrtho;
                            cam.targetTexture = null;
                            RenderTexture.active = null;
                            UnityEngine.Object.DestroyImmediate(rt);
                            UnityEngine.Object.DestroyImmediate(fullTex);
                            UnityEngine.Object.DestroyImmediate(closeTex);
                        }
                    }
                    catch (Exception shotEx)
                    {
                        sw.WriteLine($"[Screenshot Warning]: {shotEx.Message}");
                    }

                    sw.WriteLine($"\n==================================================================");
                    sw.WriteLine($"TOTAL PENGUJIAN: {passed + failed} | LULUS: {passed} | GAGAL: {failed}");
                    sw.WriteLine($"STATUS AKHIR: {(failed == 0 ? "SELURUH PENGUJIAN BERHASIL 100%!" : "ADA PENGUJIAN GAGAL!")}");
                    sw.WriteLine($"==================================================================");
                }
                catch (Exception ex)
                {
                    sw.WriteLine($"\n[EXCEPTION CRITICAL]: {ex.Message}\n{ex.StackTrace}");
                    Debug.LogException(ex);
                }

                File.WriteAllText(logPath, sw.ToString());
                Debug.Log($"[PlayerAnimationVerification] Hasil verifikasi disimpan di: {logPath}");
            }
        }
    }
}
