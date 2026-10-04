using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using MazeSpell.Audio;

namespace MazeSpell.Editor
{
    public static class BubbleLetterVerificationRunner
    {
        private static int passedTests = 0;
        private static int failedTests = 0;
        private static readonly System.Text.StringBuilder log = new System.Text.StringBuilder();

        [MenuItem("MazeSpell/Run Bubble Letter Verification")]
        public static void RunTests()
        {
            passedTests = 0;
            failedTests = 0;
            log.Clear();

            LogHeader("VERIFIKASI SISTEM VISUAL & ANIMASI HURUF COLLECTIBLE MAZESPELL");

            try
            {
                Test1_AssetValidation();
                Test2_AllLevelsLetterHierarchy();
                Test3_VisualStylesAndColors();
                Test4_LetterCollectionAndWordManagerIntegration();
                Test5_CollectAnimationAndReset();
            }
            catch (Exception ex)
            {
                LogFail($"Exception saat verifikasi: {ex}");
            }

            LogSummary();

            string logPath = "Logs/bubble_letter_report.txt";
            File.WriteAllText(logPath, log.ToString());
            Debug.Log($"[BubbleLetterVerificationRunner] Log tersimpan di {logPath}");
        }

        private static void Test1_AssetValidation()
        {
            LogSection("TAHAP 1: VALIDASI ASSET FONT & MATERIAL");

            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BubbleLetterStyleSetup.FONT_PATH);
            Assert(fontAsset != null, "TitanOne-Regular SDF.asset ada dan valid");

            Material outlineMat = AssetDatabase.LoadAssetAtPath<Material>(BubbleLetterStyleSetup.OUTLINE_MAT_PATH);
            Assert(outlineMat != null, "BubbleLetter_Outline.mat ada dan valid");

            Material depthMat = AssetDatabase.LoadAssetAtPath<Material>(BubbleLetterStyleSetup.DEPTH_MAT_PATH);
            Assert(depthMat != null, "BubbleLetter_Depth.mat ada dan valid");

            Material faceMat = AssetDatabase.LoadAssetAtPath<Material>(BubbleLetterStyleSetup.FACE_MAT_PATH);
            Assert(faceMat != null, "BubbleLetter_Face.mat ada dan valid");

            if (outlineMat != null)
            {
                Assert(outlineMat.IsKeywordEnabled("OUTLINE_ON"), "Outline Material mengaktifkan keyword OUTLINE_ON");
            }
        }

        private static void Test2_AllLevelsLetterHierarchy()
        {
            LogSection("TAHAP 2: VALIDASI STRUKTUR HURUF DI PREFAB LEVEL 1-5");

            string[] expectedWords = new string[] { "EARTH", "STONE", "GRASS", "FROST", "FLAME" };

            for (int level = 1; level <= 5; level++)
            {
                string prefabPath = $"Assets/Prefabs/MazeGrids/Level{level}_MazeGrid.prefab";
                GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
                Assert(root != null, $"Prefab Level {level} berhasil dimuat");

                var letters = root.GetComponentsInChildren<CollectibleLetter>(true);
                string expectedWord = expectedWords[level - 1];
                Assert(letters.Length == expectedWord.Length, $"Level {level} memiliki tepat {expectedWord.Length} huruf (Ditemukan: {letters.Length})");

                Dictionary<char, int> letterCounts = new Dictionary<char, int>();
                foreach (var cl in letters)
                {
                    char ch = cl.Letter;
                    if (!letterCounts.ContainsKey(ch)) letterCounts[ch] = 0;
                    letterCounts[ch]++;

                    // Pastikan CircleCollider2D
                    CircleCollider2D col = cl.GetComponent<CircleCollider2D>();
                    Assert(col != null && col.isTrigger, $"Huruf '{ch}' di Level {level} memiliki CircleCollider2D (isTrigger=true)");

                    // Pastikan LetterHoverAnimation
                    LetterHoverAnimation hoverAnim = cl.GetComponent<LetterHoverAnimation>();
                    Assert(hoverAnim != null, $"Huruf '{ch}' di Level {level} memiliki LetterHoverAnimation");

                    // Pastikan 3 Layer Visual
                    Transform vis = cl.transform.Find("Visual");
                    Assert(vis != null, $"Huruf '{ch}' di Level {level} memiliki child 'Visual'");
                    if (vis != null)
                    {
                        Transform outline = vis.Find("Outline");
                        Transform depth = vis.Find("Depth");
                        Transform face = vis.Find("Face");

                        Assert(outline != null, $"Huruf '{ch}' memiliki layer 'Outline'");
                        Assert(depth != null, $"Huruf '{ch}' memiliki layer 'Depth'");
                        Assert(face != null, $"Huruf '{ch}' memiliki layer 'Face'");

                        if (face != null)
                        {
                            var tmpFace = face.GetComponent<TextMeshPro>();
                            Assert(tmpFace != null && tmpFace.text.Contains(ch.ToString()), $"Face TMP menampilkan huruf '{ch}'");
                        }
                    }
                }

                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void Test3_VisualStylesAndColors()
        {
            LogSection("TAHAP 3: VALIDASI WARNA & LAYER DEPTH");

            Material outlineMat = AssetDatabase.LoadAssetAtPath<Material>(BubbleLetterStyleSetup.OUTLINE_MAT_PATH);
            Material depthMat = AssetDatabase.LoadAssetAtPath<Material>(BubbleLetterStyleSetup.DEPTH_MAT_PATH);
            Material faceMat = AssetDatabase.LoadAssetAtPath<Material>(BubbleLetterStyleSetup.FACE_MAT_PATH);

            if (outlineMat != null)
            {
                Color c = outlineMat.GetColor(ShaderUtilities.ID_FaceColor);
                Assert(Mathf.Abs(c.r - BubbleLetterStyleSetup.DarkBlueOutline.r) < 0.05f &&
                       Mathf.Abs(c.b - BubbleLetterStyleSetup.DarkBlueOutline.b) < 0.05f,
                       "Warna Outline Biru Tua sesuai (#3049A6)");
            }

            if (depthMat != null)
            {
                Color c = depthMat.GetColor(ShaderUtilities.ID_FaceColor);
                Assert(Mathf.Abs(c.r - BubbleLetterStyleSetup.OrangeDepth.r) < 0.05f &&
                       Mathf.Abs(c.g - BubbleLetterStyleSetup.OrangeDepth.g) < 0.05f,
                       "Warna Depth Orange sesuai (#D9952F)");
            }

            if (faceMat != null)
            {
                Color c = faceMat.GetColor(ShaderUtilities.ID_FaceColor);
                Assert(Mathf.Abs(c.r - BubbleLetterStyleSetup.YellowFill.r) < 0.05f &&
                       Mathf.Abs(c.g - BubbleLetterStyleSetup.YellowFill.g) < 0.05f,
                       "Warna Face Kuning Golden sesuai (#F5C542)");
            }
        }

        private static void Test4_LetterCollectionAndWordManagerIntegration()
        {
            LogSection("TAHAP 4: INTEGRASI PENGAMBILAN HURUF & WORD COLLECTION MANAGER");

            string scenePath = "Assets/Scenes/SampleScene.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Cari atau buat WordCollectionManager
            WordCollectionManager wordManager = UnityEngine.Object.FindAnyObjectByType<WordCollectionManager>();
            if (wordManager == null)
            {
                GameObject wmObj = new GameObject("WordCollectionManager");
                wordManager = wmObj.AddComponent<WordCollectionManager>();
            }

            // Setup kata "EARTH" untuk Level 1
            wordManager.SetLevelWord(1);
            Assert(wordManager.CurrentTargetWord == "EARTH", "WordCollectionManager terinisialisasi dengan kata 'EARTH'");
            Assert(wordManager.TotalRequired == 5, "Total Required adalah 5 huruf");
            Assert(wordManager.CollectedCount == 0, "Progres awal: 0 huruf terkumpul");

            // Buat CollectibleLetter uji 'E'
            GameObject letterObj = new GameObject("Letter_E_Test");
            var col = letterObj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            var cl = letterObj.AddComponent<CollectibleLetter>();
            cl.Letter = 'E';
            var hover = letterObj.AddComponent<LetterHoverAnimation>();

            // Setup Visual
            BubbleLetterStyleSetup.EnsureMaterialsExist(
                AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BubbleLetterStyleSetup.FONT_PATH),
                out Material outMat, out Material dMat, out Material fMat
            );
            BubbleLetterStyleSetup.SetupLetterVisualHierarchy(letterObj, 'E',
                AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BubbleLetterStyleSetup.FONT_PATH),
                outMat, dMat, fMat);

            // Test Collect
            cl.Collect();

            Assert(cl.IsCollected, "Huruf 'E' berhasil dikoleksi (isCollected = true)");
            Assert(!col.enabled, "Collider huruf dinonaktifkan seketika untuk mencegah double collection");
            Assert(wordManager.CollectedCount == 1, "WordCollectionManager mencatat 1 huruf terkumpul");

            UnityEngine.Object.DestroyImmediate(letterObj);
        }

        private static void Test5_CollectAnimationAndReset()
        {
            LogSection("TAHAP 5: PENGUJIAN RESET HURUF");

            GameObject letterObj = new GameObject("Letter_Reset_Test");
            var col = letterObj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            var cl = letterObj.AddComponent<CollectibleLetter>();
            cl.Letter = 'A';
            var hover = letterObj.AddComponent<LetterHoverAnimation>();

            BubbleLetterStyleSetup.EnsureMaterialsExist(
                AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BubbleLetterStyleSetup.FONT_PATH),
                out Material outMat, out Material dMat, out Material fMat
            );
            BubbleLetterStyleSetup.SetupLetterVisualHierarchy(letterObj, 'A',
                AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BubbleLetterStyleSetup.FONT_PATH),
                outMat, dMat, fMat);

            cl.Collect();
            Assert(cl.IsCollected, "Huruf terkumpul");

            cl.ResetLetter();
            Assert(!cl.IsCollected, "Huruf berhasil di-reset (isCollected = false)");
            Assert(col.enabled, "Collider aktif kembali setelah reset");

            UnityEngine.Object.DestroyImmediate(letterObj);
        }

        private static void Assert(bool condition, string message)
        {
            if (condition)
            {
                passedTests++;
                log.AppendLine($"[PASS] {message}");
            }
            else
            {
                failedTests++;
                log.AppendLine($"[FAIL] {message}");
            }
        }

        private static void LogHeader(string title)
        {
            log.AppendLine("==================================================================");
            log.AppendLine($">>> {title} <<<");
            log.AppendLine($"Waktu: {DateTime.Now}");
            log.AppendLine("==================================================================");
        }

        private static void LogSection(string section)
        {
            log.AppendLine();
            log.AppendLine($"--- {section} ---");
        }

        private static void LogFail(string message)
        {
            failedTests++;
            log.AppendLine($"[CRITICAL FAIL] {message}");
        }

        private static void LogSummary()
        {
            log.AppendLine();
            log.AppendLine("==================================================================");
            log.AppendLine($"TOTAL TEST BERHASIL: {passedTests}");
            log.AppendLine($"TOTAL TEST GAGAL   : {failedTests}");
            log.AppendLine($"STATUS AKHIR       : {(failedTests == 0 ? "SEMUA PENGUJIAN LULUS 100%!" : "ADA TEST YANG GAGAL!")}");
            log.AppendLine("==================================================================");
        }
    }
}
