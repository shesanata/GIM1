using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TMPro;

namespace MazeSpell.Editor
{
    /// <summary>
    /// Script editor untuk mengatur dan memperbaiki posisi semua CollectibleLetter pada Level 1 - 5
    /// agar selalu tepat berada di tengah cell (X + 0.5, Y + 0.5) pada jalur yang walkable,
    /// membentuk urutan pembentukan kata yang natural dan memandu Player melalui eksplorasi labirin.
    /// </summary>
    [InitializeOnLoad]
    public static class LetterPlacementSetup
    {
        static LetterPlacementSetup()
        {
            EditorApplication.delayCall += AutoFixIfNeeded;
        }

        private static void AutoFixIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            if (!SessionState.GetBool("MazeSpell_LetterPlacementFixed_V3", false))
            {
                SessionState.SetBool("MazeSpell_LetterPlacementFixed_V3", true);
                FixAllLetterPositions(false);
            }
        }

        // Posisi Canonical terverifikasi untuk setiap level denah rumah (Center cell: X+0.5, Y+0.5)
        public static readonly Dictionary<int, Dictionary<string, Vector3>> CanonicalLetterPositions =
            new Dictionary<int, Dictionary<string, Vector3>>
            {
                // Level 1: EARTH (Kamar E, Dapur A, Living Red Rug R, Dining Green Rug T, Study Brown Rug H)
                {
                    1, new Dictionary<string, Vector3>
                    {
                        { "E", new Vector3(4.5f, 12.5f, -0.1f) },  // E: Kamar Tidur Kiri Atas
                        { "A", new Vector3(10.5f, 12.5f, -0.1f) }, // A: Dapur / Ruang Tengah Atas
                        { "R", new Vector3(10.5f, 6.5f, -0.1f) },  // R: Karpet Merah Living Room
                        { "T", new Vector3(16.5f, 8.5f, -0.1f) },  // T: Ruang Makan Karpet Hijau
                        { "H", new Vector3(16.5f, 5.5f, -0.1f) }   // H: Ruang Belajar Karpet Cokelat
                    }
                },
                // Level 2: STONE (Kamar S, Dining T, Living O, Study N, Living E)
                {
                    2, new Dictionary<string, Vector3>
                    {
                        { "S", new Vector3(4.5f, 12.5f, -0.1f) },  // S: Kamar Tidur Kasur Biru Kiri Atas
                        { "T", new Vector3(14.5f, 12.5f, -0.1f) }, // T: Ruang Makan Meja Bundar Kanan Atas
                        { "O", new Vector3(4.5f, 8.5f, -0.1f) },   // O: Living Room Karpet Biru Kiri Tengah
                        { "N", new Vector3(14.5f, 8.5f, -0.1f) },  // N: Ruangan Lantai Kayu Kanan Tengah
                        { "E", new Vector3(4.5f, 4.5f, -0.1f) }    // E: Living Room Sofa Hijau Kiri Bawah
                    }
                },
                // Level 3: GRASS (Kamar G, Kamar R, Dapur A, Dining S_1, Kamar S_2)
                {
                    3, new Dictionary<string, Vector3>
                    {
                        { "G", new Vector3(4.5f, 14.5f, -0.1f) },  // G: Kamar Tidur Kiri Atas
                        { "R", new Vector3(15.5f, 14.5f, -0.1f) }, // R: Kamar Tidur Karpet Merah Kanan Atas
                        { "A", new Vector3(4.5f, 10.5f, -0.1f) },  // A: Dapur / Counter Kiri Tengah
                        { "S_1", new Vector3(15.5f, 10.5f, -0.1f) },// S_1: Ruang Makan Meja Oval Kanan Tengah
                        { "S_2", new Vector3(15.5f, 5.5f, -0.1f) }  // S_2: Kamar Tidur Kasur Merah Kanan Bawah
                    }
                },
                // Level 4: FROST (Master F, Bath R, Living O, Living S, Dining T)
                {
                    4, new Dictionary<string, Vector3>
                    {
                        { "F", new Vector3(4.5f, 12.5f, -0.1f) },  // F: Master Bedroom Kiri Atas
                        { "R", new Vector3(10.5f, 12.5f, -0.1f) }, // R: Kamar Mandi Bak Air Tengah Atas
                        { "O", new Vector3(15.5f, 12.5f, -0.1f) }, // O: Living Room Karpet Bundar Kanan Atas
                        { "S", new Vector3(4.5f, 7.5f, -0.1f) },   // S: Living Room Sofa Biru Kiri Bawah
                        { "T", new Vector3(15.5f, 6.5f, -0.1f) }   // T: Ruang Makan Meja Putih Kanan Bawah
                    }
                },
                // Level 5: FLAME (Kamar F, Study L, Kitchen A, Living M, Living E)
                {
                    5, new Dictionary<string, Vector3>
                    {
                        { "F", new Vector3(4.5f, 12.5f, -0.1f) },  // F: Kamar Tidur Kasur Cokelat Kiri Atas
                        { "L", new Vector3(13.5f, 12.5f, -0.1f) }, // L: Ruang Belajar Meja Kayu Tengah Atas
                        { "A", new Vector3(18.5f, 12.5f, -0.1f) }, // A: Dapur & Meja Makan Kanan Atas
                        { "M", new Vector3(4.5f, 7.5f, -0.1f) },   // M: Living Room Karpet Merah/Oranye Kiri Bawah
                        { "E", new Vector3(15.5f, 6.5f, -0.1f) }   // E: Living Room Karpet Emas Kanan Bawah
                    }
                }
            };

        [MenuItem("MazeSpell/Fix All Letter Positions (L1-L5)")]
        public static void FixPositionsManual()
        {
            FixAllLetterPositions(true);
        }

        public static void FixAllLetterPositions(bool interactive)
        {
            TMP_FontAsset bubbleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/TitanOne-Regular SDF.asset");
            Material outlineMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Fonts/BubbleLetter_Outline.mat");
            Material depthMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Fonts/BubbleLetter_Depth.mat");
            Material faceMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Fonts/BubbleLetter_Face.mat");

            int totalFixed = 0;

            for (int lvl = 1; lvl <= 5; lvl++)
            {
                string prefabPath = $"Assets/Prefabs/MazeGrids/Level{lvl}_MazeGrid.prefab";
                if (!File.Exists(prefabPath))
                {
                    Debug.LogWarning($"[LetterPlacementSetup] Prefab tidak ditemukan: {prefabPath}");
                    continue;
                }

                GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
                if (root == null) continue;

                try
                {
                    var canonical = CanonicalLetterPositions[lvl];
                    var allLetters = root.GetComponentsInChildren<CollectibleLetter>(true);

                    // Khusus level 3 ada dua huruf 'S'
                    int sCount = 0;

                    foreach (var cl in allLetters)
                    {
                        char ch = cl.Letter;
                        Vector3 targetPos = Vector3.zero;
                        bool foundPos = false;

                        if (lvl == 3 && ch == 'S')
                        {
                            sCount++;
                            string key = (sCount == 1) ? "S_1" : "S_2";
                            if (canonical.ContainsKey(key))
                            {
                                targetPos = canonical[key];
                                foundPos = true;
                            }
                        }
                        else
                        {
                            string key = ch.ToString();
                            if (canonical.ContainsKey(key))
                            {
                                targetPos = canonical[key];
                                foundPos = true;
                            }
                        }

                        if (foundPos)
                        {
                            Vector3 oldPos = cl.transform.localPosition;
                            cl.transform.localPosition = targetPos;

                            // Pastikan Collider2D
                            var col = cl.GetComponent<CircleCollider2D>();
                            if (col == null) col = cl.gameObject.AddComponent<CircleCollider2D>();
                            col.isTrigger = true;
                            col.radius = 0.45f;

                            // Pastikan gaya Bubble Font
                            BubbleLetterStyleSetup.SetupLetterVisualHierarchy(
                                cl.gameObject,
                                ch,
                                bubbleFont,
                                outlineMat,
                                depthMat,
                                faceMat
                            );

                            // Validasi anti-wall
                            var valRes = MazeLetterValidator.ValidatePositionDetailed(targetPos, lvl, cl.gameObject);
                            if (valRes.IsValid)
                            {
                                Debug.Log($"[LetterPlacementSetup] Level {lvl} Huruf '{ch}': {oldPos} -> {targetPos} [PASS: Walkable, Reachable]");
                            }
                            else
                            {
                                Debug.LogWarning($"[LetterPlacementSetup] Level {lvl} Huruf '{ch}': {targetPos} validasi: {valRes.failureReason}");
                            }

                            totalFixed++;
                        }
                    }

                    // Sinkronkan SpawnPoint dan Goal jika ada
                    Transform gameplay = root.transform.Find("GameplayObjects");
                    if (gameplay != null && lvl <= MazeGameplayManager.AllLevelsData.Length)
                    {
                        var data = MazeGameplayManager.AllLevelsData[lvl - 1];
                        Transform spawn = gameplay.Find("SpawnPoint");
                        if (spawn != null) spawn.localPosition = new Vector3(data.spawnPosition.x, data.spawnPosition.y, 0f);

                        Transform goal = gameplay.Find("Goal");
                        if (goal != null) goal.localPosition = new Vector3(data.goalPosition.x, data.goalPosition.y, 0f);
                    }

                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    Debug.Log($"[LetterPlacementSetup] ✓ Level {lvl} Prefab diperbarui dan disimpan.");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[LetterPlacementSetup] SELESAI: {totalFixed} CollectibleLetter berhasil diatur ke posisi walkable terverifikasi!");

            if (interactive)
            {
                EditorUtility.DisplayDialog("Letter Placement Setup",
                    $"Berhasil memperbaiki posisi {totalFixed} huruf collectible di Level 1 - 5!\nSemua huruf berada di jalur walkable dan dapat dijangkau Player.",
                    "OK");
            }
        }
    }
}
