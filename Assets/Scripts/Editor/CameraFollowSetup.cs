using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MazeSpell;

namespace MazeSpell.Editor
{
    /// <summary>
    /// Tool Editor untuk memasang, mengonfigurasi, dan memverifikasi sistem CameraFollow
    /// pada Main Camera di scene gameplay (SampleScene.unity).
    /// Memastikan kamera tidak terpasang di MainMenu.unity maupun LevelSelect.unity.
    /// </summary>
    [InitializeOnLoad]
    public static class CameraFollowSetup
    {
        private const string GameplayScenePath = "Assets/Scenes/SampleScene.unity";
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string LevelSelectScenePath = "Assets/Scenes/LevelSelect.unity";

        static CameraFollowSetup()
        {
            EditorApplication.delayCall += AutoSetupIfNeeded;
        }

        private static void AutoSetupIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            if (!SessionState.GetBool("MazeSpell_CameraFollowSetupDone_V1", false))
            {
                SessionState.SetBool("MazeSpell_CameraFollowSetupDone_V1", true);
                SetupCameraFollowInScene(false);
            }
        }

        [MenuItem("MazeSpell/Setup Camera Follow in Scene")]
        public static void SetupCameraFollowManual()
        {
            SetupCameraFollowInScene(true);
        }

        public static void SetupCameraFollowInScene(bool interactive = false)
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
                if (!scene.IsValid())
                {
                    Debug.LogError($"[CameraFollowSetup] Scene gameplay tidak ditemukan di '{GameplayScenePath}'!");
                    return;
                }

                // 1. Cari Main Camera
                GameObject camObj = GameObject.FindWithTag("MainCamera");
                if (camObj == null)
                {
                    camObj = GameObject.Find("Main Camera");
                }

                if (camObj == null)
                {
                    Debug.LogError("[CameraFollowSetup] Main Camera tidak ditemukan di SampleScene!");
                    return;
                }

                // Pastikan Tag adalah MainCamera
                camObj.tag = "MainCamera";

                Camera cam = camObj.GetComponent<Camera>();
                if (cam == null)
                {
                    cam = camObj.AddComponent<Camera>();
                }
                cam.orthographic = true;

                // 2. Bersihkan duplikat script camera follow jika ada di objek lain
                var allFollows = UnityEngine.Object.FindObjectsByType<CameraFollow>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var f in allFollows)
                {
                    if (f.gameObject != camObj)
                    {
                        Debug.Log($"[CameraFollowSetup] Menghapus duplikat CameraFollow dari '{f.gameObject.name}'.");
                        UnityEngine.Object.DestroyImmediate(f);
                    }
                }

                // 3. Pasang atau perbarui komponen CameraFollow pada Main Camera
                CameraFollow follow = camObj.GetComponent<CameraFollow>();
                if (follow == null)
                {
                    follow = camObj.AddComponent<CameraFollow>();
                    Debug.Log("[CameraFollowSetup] Komponen CameraFollow berhasil ditambahkan ke Main Camera.");
                }

                // 4. Hubungkan target Player
                GameObject player = GameObject.FindWithTag("Player");
                if (player == null)
                {
                    player = GameObject.Find("Player");
                }

                if (player != null)
                {
                    follow.Target = player.transform;
                    Debug.Log($"[CameraFollowSetup] Target kamera diatur ke Player: '{player.name}'");
                }

                // 5. Konfigurasi parameter smoothing & bounds
                follow.SmoothTime = 0.22f;
                follow.UseBounds = true;
                follow.AutoDetectMazeBounds();

                EditorUtility.SetDirty(camObj);
                EditorUtility.SetDirty(follow);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[CameraFollowSetup] SampleScene.unity berhasil disimpan dengan CameraFollow terkonfigurasi!");

                // 6. Verifikasi scene non-gameplay (MainMenu & LevelSelect) agar tidak memiliki CameraFollow
                VerifyNonGameplayScene(MainMenuScenePath);
                VerifyNonGameplayScene(LevelSelectScenePath);

                // Kembalikan scene aktif ke SampleScene
                EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);

                if (interactive)
                {
                    EditorUtility.DisplayDialog("Camera Follow", "Camera Follow berhasil dipasang dan dikonfigurasi pada Main Camera di SampleScene!", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CameraFollowSetup] Terjadi kesalahan: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void VerifyNonGameplayScene(string scenePath)
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                if (!scene.IsValid()) return;

                var follows = UnityEngine.Object.FindObjectsByType<CameraFollow>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (follows.Length > 0)
                {
                    foreach (var f in follows)
                    {
                        UnityEngine.Object.DestroyImmediate(f);
                    }
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log($"[CameraFollowSetup] Membersihkan CameraFollow dari {scenePath}.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CameraFollowSetup] Gagal memverifikasi scene '{scenePath}': {ex.Message}");
            }
        }

        [MenuItem("MazeSpell/Verify Camera Follow System")]
        public static void VerifyCameraFollowRunner()
        {
            Debug.Log("=================================================");
            Debug.Log(">>> MEMULAI VERIFIKASI SISTEM CAMERA FOLLOW <<<");
            Debug.Log("=================================================");

            int totalTests = 0;
            int passedTests = 0;
            int failedTests = 0;

            void Check(bool condition, string testName)
            {
                totalTests++;
                if (condition)
                {
                    passedTests++;
                    Debug.Log($"[PASS] {testName}");
                }
                else
                {
                    failedTests++;
                    Debug.LogError($"[FAIL] {testName}");
                }
            }

            try
            {
                // Test 1: Buka SampleScene
                var scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
                Check(scene.IsValid(), "SampleScene.unity berhasil dimuat");

                // Test 2: Main Camera
                GameObject camObj = GameObject.FindWithTag("MainCamera");
                Check(camObj != null, "Main Camera bertag 'MainCamera' ditemukan");

                Camera cam = camObj != null ? camObj.GetComponent<Camera>() : null;
                Check(cam != null && cam.orthographic, "Main Camera menggunakan proyeksi Orthographic");

                // Test 3: CameraFollow Component
                CameraFollow follow = camObj != null ? camObj.GetComponent<CameraFollow>() : null;
                Check(follow != null, "Komponen CameraFollow terpasang pada Main Camera");
                Check(follow != null && follow.enabled, "Komponen CameraFollow aktif (enabled)");

                // Test 4: Single system check (tidak ada duplikat)
                var allFollows = UnityEngine.Object.FindObjectsByType<CameraFollow>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Check(allFollows.Length == 1, $"Hanya ada 1 komponen CameraFollow di seluruh scene (Ditemukan: {allFollows.Length})");

                // Test 5: Target Player
                GameObject player = GameObject.FindWithTag("Player");
                Check(player != null, "GameObject Player dengan tag 'Player' ada di gameplay");
                Check(follow != null && follow.Target != null, "Target CameraFollow terhubung ke Player");

                // Test 6: Rotasi Kamera terkunci (top-down, tidak berputar)
                Check(camObj.transform.rotation == Quaternion.identity, "Rotasi kamera terkunci pada Quaternion.identity (0,0,0)");

                // Test 7: Z position terkunci (-10f)
                Check(Mathf.Approximately(camObj.transform.position.z, -10f), "Posisi Z kamera terkunci pada -10f");

                // Test 8: MainMenu tidak memiliki CameraFollow
                var menuScene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
                Check(menuScene.IsValid(), "MainMenu.unity berhasil dimuat");
                var menuFollows = UnityEngine.Object.FindObjectsByType<CameraFollow>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Check(menuFollows.Length == 0, "MainMenu.unity TIDAK memiliki komponen CameraFollow");

                // Test 9: LevelSelect tidak memiliki CameraFollow
                var selectScene = EditorSceneManager.OpenScene(LevelSelectScenePath, OpenSceneMode.Single);
                Check(selectScene.IsValid(), "LevelSelect.unity berhasil dimuat");
                var selectFollows = UnityEngine.Object.FindObjectsByType<CameraFollow>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Check(selectFollows.Length == 0, "LevelSelect.unity TIDAK memiliki komponen CameraFollow");

                // Kembalikan ke SampleScene
                EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);

                Debug.Log("=================================================");
                Debug.Log($"HASIL VERIFIKASI: {passedTests}/{totalTests} LULUS (Gagal: {failedTests})");
                Debug.Log("=================================================");

                if (failedTests > 0)
                {
                    EditorApplication.Exit(1);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Verification Error] {ex.Message}\n{ex.StackTrace}");
                EditorApplication.Exit(2);
            }
        }
    }
}
