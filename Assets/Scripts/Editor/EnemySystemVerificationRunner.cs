using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MazeSpell;

namespace MazeSpell.Editor
{
    /// <summary>
    /// Runner otomatis untuk memverifikasi dan mengeksekusi sistem Enemy 1 per level:
    /// - Menyetel ulang dan menyimpan prefab Level 1 - 5 sehingga tepat memiliki 1 Enemy.
    /// - Menjalankan pengujian 7 TEST wajib sistem Enemy dan mekanik nyawa.
    /// - Menyimpan log hasil eksekusi ke Logs/enemy_verification_result.log.
    /// </summary>
    [InitializeOnLoad]
    public static class EnemySystemVerificationRunner
    {
        private static bool hasExecuted = false;

        static EnemySystemVerificationRunner()
        {
            EditorApplication.delayCall += CheckAndExecute;
        }

        private static void CheckAndExecute()
        {
            if (hasExecuted) return;

            // Jika Unity sedang Play Mode, hentikan terlebih dahulu agar prefab dapat dimodifikasi dengan aman
            if (EditorApplication.isPlaying)
            {
                Debug.Log("[EnemyVerificationRunner] Menghentikan Play Mode untuk eksekusi penyesuaian prefab...");
                EditorApplication.isPlaying = false;
                EditorApplication.delayCall += CheckAndExecute;
                return;
            }

            hasExecuted = true;
            RunEnemySetupAndVerification();
        }

        [MenuItem("MazeSpell/Execute 1 Enemy Per Level & Verify All")]
        public static void RunEnemySetupAndVerification()
        {
            string logDir = Path.Combine(Application.dataPath, "..", "Logs");
            if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
            string logPath = Path.Combine(logDir, "enemy_verification_result.log");
            string rootLogPath = Path.Combine(Application.dataPath, "..", "enemy_verification_result.log");

            using (StringWriter sw = new StringWriter())
            {
                sw.WriteLine("==================================================================");
                sw.WriteLine(">>> HASIL EKSEKUSI & VERIFIKASI SISTEM ENEMY MAZESPELL (1 PER LEVEL) <<<");
                sw.WriteLine($"Waktu Eksekusi: {DateTime.Now}");
                sw.WriteLine("==================================================================");

                int passed = 0;
                int failed = 0;

                void AssertCheck(bool cond, string msg)
                {
                    if (cond)
                    {
                        passed++;
                        sw.WriteLine($"[PASS] {msg}");
                        Debug.Log($"[EnemyVerification PASS] {msg}");
                    }
                    else
                    {
                        failed++;
                        sw.WriteLine($"[FAIL] {msg}");
                        Debug.LogError($"[EnemyVerification FAIL] {msg}");
                    }
                }

                try
                {
                    // 1. SETUP & REBUILD PREFAB LEVEL 1-5 AGAR TEPAT 1 ENEMY
                    sw.WriteLine("\n--- TAHAP 1: REBUILD PREFAB LEVEL 1-5 ---");
                    EnemySystemSetup.SetupAllLevelsAndCapture();
                    sw.WriteLine("[Setup] EnemySystemSetup.SetupAllLevelsAndCapture() berhasil dijalankan.");

                    // 2. VERIFIKASI FILE PREFAB SECARA LANGSUNG
                    sw.WriteLine("\n--- TAHAP 2: INSPEKSI JUMLAH ENEMY DI SETIAP PREFAB ---");
                    for (int lvl = 1; lvl <= 5; lvl++)
                    {
                        string pPath = $"Assets/Prefabs/MazeGrids/Level{lvl}_MazeGrid.prefab";
                        GameObject pRoot = PrefabUtility.LoadPrefabContents(pPath);
                        try
                        {
                            var enemies = pRoot.GetComponentsInChildren<EnemyCharacter>(true);
                            AssertCheck(enemies.Length == 1, $"Prefab Level {lvl} memiliki tepat 1 Enemy (Ditemukan: {enemies.Length})");
                            if (enemies.Length > 0)
                            {
                                Vector3 ePos = enemies[0].transform.localPosition;
                                sw.WriteLine($"    -> Posisi Enemy Level {lvl}: ({ePos.x}, {ePos.y})");
                            }
                        }
                        finally
                        {
                            PrefabUtility.UnloadPrefabContents(pRoot);
                        }
                    }

                    // 3. JALANKAN FULL GAMEPLAY RUNTIME TEST RUNNER
                    sw.WriteLine("\n--- TAHAP 3: EKSEKUSI FULL RUNTIME TEST RUNNER ---");
                    MazeRuntimeTestRunner.RunAllTests();
                    sw.WriteLine("[Runtime Tests] MazeRuntimeTestRunner.RunAllTests() berhasil diselesaikan.");

                    sw.WriteLine("\n==================================================================");
                    sw.WriteLine($">>> RINGKASAN: {passed} PENGUJIAN PREFAB LULUS, {failed} GAGAL <<<");
                    sw.WriteLine("==================================================================");

                    File.WriteAllText(logPath, sw.ToString());
                    File.WriteAllText(rootLogPath, sw.ToString());
                    Debug.Log($"[EnemyVerificationRunner] Verifikasi selesai! Log disimpan di: {logPath}");
                }
                catch (Exception ex)
                {
                    sw.WriteLine($"[FATAL EXCEPTION]: {ex}");
                    File.WriteAllText(logPath, sw.ToString());
                    File.WriteAllText(rootLogPath, sw.ToString());
                    Debug.LogError($"[EnemyVerificationRunner] Gagal: {ex}");
                }
            }
        }
    }
}
