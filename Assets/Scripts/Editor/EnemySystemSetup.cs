using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using MazeSpell;

namespace MazeSpell.Editor
{
    /// <summary>
    /// Script editor untuk setup sistem Enemy pada semua 5 level MazeSpell (Level 1–5):
    /// 1. Membuat prefab reusable Enemy_Character.prefab
    /// 2. Menanamkan objek Enemy pada kelima level maze prefab di area walkable
    /// 3. Menjaga posisi collectible huruf Level 1 (EARTH) tetap presisi
    /// 4. Mengambil screenshot Game View Level 1 yang memperlihatkan Enemy
    /// </summary>
    public static class EnemySystemSetup
    {
        [MenuItem("MazeSpell/Fix Level 1 Enemies (Safe Non-Blocking 1 Enemy)")]
        public static void SetupLevel1OnlyAndCapture()
        {
            Debug.Log("==================================================================");
            Debug.Log(">>> MEMULAI PENYESUAIAN ENEMY KHUSUS LEVEL 1 (1 ENEMY AMAN NON-BLOCKING) <<<");
            Debug.Log("==================================================================");

            string prefabPath = "Assets/Prefabs/MazeGrids/Level1_MazeGrid.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
            {
                Debug.LogError($"[EnemySetup] Prefab tidak ditemukan di {prefabPath}");
                return;
            }

            try
            {
                Transform gameplayTr = root.transform.Find("GameplayObjects");
                if (gameplayTr == null)
                {
                    Debug.LogError("[EnemySetup] GameplayObjects tidak ditemukan!");
                    return;
                }

                Transform enemiesContainer = gameplayTr.Find("Enemies");
                if (enemiesContainer != null)
                {
                    UnityEngine.Object.DestroyImmediate(enemiesContainer.gameObject);
                }

                GameObject newEnemiesGo = new GameObject("Enemies");
                newEnemiesGo.transform.SetParent(gameplayTr, false);

                // Tepat 1 Enemy di dead-end spur cell (7, 1) -> (7.5, 1.5)
                // Membebaskan koridor X=2 (akses keluar Start) dan koridor X=15 (akses menuju Goal)
                Sprite enemySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Enemy.png");
                Vector3 enemyPos = new Vector3(7.5f, 1.5f, -0.1f);

                GameObject enemyObj = new GameObject("Enemy_1");
                enemyObj.transform.SetParent(newEnemiesGo.transform, false);
                enemyObj.transform.localPosition = enemyPos;

                SpriteRenderer sr = enemyObj.AddComponent<SpriteRenderer>();
                if (enemySprite != null) sr.sprite = enemySprite;
                sr.sortingOrder = 4;

                CircleCollider2D col = enemyObj.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.35f;

                EnemyCharacter enemyChar = enemyObj.AddComponent<EnemyCharacter>();
                enemyChar.SetBasePosition(enemyPos);
                enemyChar.EnsureVisualHierarchy();

                Debug.Log($"[EnemySetup] Level 1 kini memiliki 1 Enemy aman di {enemyPos} (tidak menghalangi jalan utama)!");

                // Pastikan huruf E-A-R-T-H tetap presisi
                EnsureLevel1Letters(gameplayTr);

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log($"[EnemySetup] Prefab {prefabPath} berhasil disimpan dengan 1 Enemy!");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Render Screenshot Game View Level 1
            CaptureSampleSceneScreenshot();
        }

        [MenuItem("MazeSpell/Setup Enemy System Across All Levels")]
        public static void SetupAllLevelsAndCapture()
        {
            Debug.Log("==================================================================");
            Debug.Log(">>> MEMULAI SETUP SISTEM ENEMY SEMUA LEVEL (1–5) <<<");
            Debug.Log("==================================================================");

            // 1. Buat direktori dan prefab reusable Enemy
            EnsureEnemyPrefab();

            // 2. Terapkan Enemy pada seluruh 5 prefab level
            var levelsData = MazeGameplayManager.AllLevelsData;
            Sprite enemySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Enemy.png");

            for (int i = 0; i < levelsData.Length; i++)
            {
                var data = levelsData[i];
                string prefabPath = $"Assets/Prefabs/MazeGrids/Level{data.levelNumber}_MazeGrid.prefab";

                try
                {
                    GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
                    if (root == null)
                    {
                        Debug.LogWarning($"[EnemySetup] Prefab tidak ditemukan: {prefabPath}");
                        continue;
                    }

                    Transform gameplayTr = root.transform.Find("GameplayObjects");
                    if (gameplayTr != null)
                    {
                        // Hapus enemies dan hazards lama jika ada
                        Transform oldEnemies = gameplayTr.Find("Enemies");
                        if (oldEnemies != null) UnityEngine.Object.DestroyImmediate(oldEnemies.gameObject);

                        Transform oldHazards = gameplayTr.Find("Hazards");
                        if (oldHazards != null) UnityEngine.Object.DestroyImmediate(oldHazards.gameObject);
                    }
                    else
                    {
                        var go = new GameObject("GameplayObjects");
                        go.transform.SetParent(root.transform, false);
                        gameplayTr = go.transform;
                    }

                    // Buat Enemies container
                    GameObject enemiesContainer = new GameObject("Enemies");
                    enemiesContainer.transform.SetParent(gameplayTr, false);

                    // ATURAN WAJIB: Tepat 1 Enemy per level (Level 1-4: 1 Enemy, Level 5: 1 Final Guardian)
                    int enemyCount = Mathf.Min(1, data.hazardPositions.Length);
                    for (int h = 0; h < enemyCount; h++)
                    {
                        Vector2 pos = data.hazardPositions[h];
                        GameObject enemyObj = new GameObject($"Enemy_{h + 1}");
                        enemyObj.transform.SetParent(enemiesContainer.transform, false);
                        enemyObj.transform.localPosition = new Vector3(pos.x, pos.y, -0.1f);

                        SpriteRenderer sr = enemyObj.AddComponent<SpriteRenderer>();
                        if (enemySprite != null) sr.sprite = enemySprite;
                        sr.sortingOrder = 4;

                        CircleCollider2D col = enemyObj.AddComponent<CircleCollider2D>();
                        col.isTrigger = true;
                        col.radius = 0.38f;

                        EnemyCharacter enemyChar = enemyObj.AddComponent<EnemyCharacter>();
                        enemyChar.SetBasePosition(new Vector3(pos.x, pos.y, -0.1f));
                        enemyChar.EnsureVisualHierarchy();
                    }

                    // Buat kontainer Hazards kosong untuk backward compatibility
                    GameObject hazardsContainer = new GameObject("Hazards");
                    hazardsContainer.transform.SetParent(gameplayTr, false);

                    // Untuk Level 1: pastikan collectible huruf EARTH tetap presisi
                    if (data.levelNumber == 1)
                    {
                        EnsureLevel1Letters(gameplayTr);
                    }

                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    PrefabUtility.UnloadPrefabContents(root);
                    Debug.Log($"[EnemySetup] ✓ Level {data.levelNumber} diperbarui dengan {data.hazardPositions.Length} Enemy pada {prefabPath}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EnemySetup] Gagal setup Level {data.levelNumber}: {ex}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 3. Render Screenshot Game View Level 1
            CaptureSampleSceneScreenshot();
        }

        [MenuItem("MazeSpell/Setup Enemy 1 Per Level And Verify")]
        public static void SetupAndVerifyAll()
        {
            SetupAllLevelsAndCapture();
            MazeRuntimeTestRunner.RunAllTests();
        }

        private static void EnsureEnemyPrefab()
        {
            string folder = "Assets/Prefabs/Enemy";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "Enemy");
            }

            string prefabPath = $"{folder}/Enemy_Character.prefab";
            GameObject enemyGo = new GameObject("Enemy_Character");
            try
            {
                Sprite enemySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Enemy.png");
                SpriteRenderer sr = enemyGo.AddComponent<SpriteRenderer>();
                if (enemySprite != null) sr.sprite = enemySprite;
                sr.sortingOrder = 4;

                CircleCollider2D col = enemyGo.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.38f;

                EnemyCharacter enemyChar = enemyGo.AddComponent<EnemyCharacter>();
                enemyChar.EnsureVisualHierarchy();

                PrefabUtility.SaveAsPrefabAsset(enemyGo, prefabPath);
                Debug.Log($"[EnemySetup] Reusable Enemy Prefab berhasil dibuat di: {prefabPath}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemyGo);
            }
        }

        private static void EnsureLevel1Letters(Transform gameplayContainer)
        {
            Transform lettersContainer = gameplayContainer.Find("Letters");
            if (lettersContainer == null) return;

            Dictionary<char, Vector3> targetPos = new Dictionary<char, Vector3>
            {
                { 'E', new Vector3(3.5f, 8.5f, -0.1f) },
                { 'A', new Vector3(4.5f, 5.5f, -0.1f) },
                { 'R', new Vector3(9.5f, 7.5f, -0.1f) },
                { 'T', new Vector3(12.5f, 7.5f, -0.1f) },
                { 'H', new Vector3(15.5f, 2.5f, -0.1f) }
            };

            var allLetters = lettersContainer.GetComponentsInChildren<CollectibleLetter>(true);
            foreach (var kvp in targetPos)
            {
                char ch = kvp.Key;
                Vector3 pos = kvp.Value;
                Transform letterTr = null;

                foreach (var cl in allLetters)
                {
                    if (cl.Letter == ch)
                    {
                        letterTr = cl.transform;
                        break;
                    }
                }

                if (letterTr == null)
                {
                    foreach (Transform child in lettersContainer)
                    {
                        if (child.name.StartsWith($"Letter_{ch}"))
                        {
                            letterTr = child;
                            break;
                        }
                    }
                }

                if (letterTr != null)
                {
                    letterTr.localPosition = pos;
                    var col = letterTr.GetComponent<CircleCollider2D>();
                    if (col != null) { col.isTrigger = true; col.radius = 0.45f; }

                    var tmp = letterTr.GetComponentInChildren<TextMeshPro>(true);
                    if (tmp != null)
                    {
                        var mr = tmp.GetComponent<MeshRenderer>();
                        if (mr != null) mr.sortingOrder = 5;
                    }
                }
            }
        }

        private static void CaptureSampleSceneScreenshot()
        {
            string scenePath = "Assets/Scenes/SampleScene.unity";
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Bersihkan instance grid lama di scene
            var existingGrids = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in existingGrids)
            {
                if (go != null && go.name.Contains("MazeGrid"))
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }

            // Instansiasi prefab Level 1 terbaru
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MazeGrids/Level1_MazeGrid.prefab");
            if (prefab != null)
            {
                var grid = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                grid.name = "Level1_MazeGrid";
            }

            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("[EnemySetup] Camera.main tidak ditemukan di SampleScene!");
                return;
            }

            cam.transform.position = new Vector3(8.0f, 5.0f, -10f);
            cam.orthographic = true;
            cam.orthographicSize = 6.2f;
            cam.backgroundColor = new Color(0.56f, 0.64f, 0.36f, 1f);

            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                player.transform.position = new Vector3(2.5f, 1.5f, 0f);
            }

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

            string outPath = "c:/Users/ASUS/Mazell/level1_gameview_screenshot.png";
            File.WriteAllBytes(outPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            Debug.Log($"[EnemySetup] Screenshot Game View terbaru (dengan Enemy) berhasil disimpan ke: {outPath}");
        }
    }
}
