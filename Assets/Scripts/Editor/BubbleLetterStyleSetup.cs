using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using TMPro;

namespace MazeSpell.Editor
{
    [InitializeOnLoad]
    public static class BubbleLetterStyleSetup
    {
        public const string FONT_PATH = "Assets/Fonts/TitanOne-Regular SDF.asset";
        public const string OUTLINE_MAT_PATH = "Assets/Fonts/BubbleLetter_Outline.mat";
        public const string DEPTH_MAT_PATH = "Assets/Fonts/BubbleLetter_Depth.mat";
        public const string FACE_MAT_PATH = "Assets/Fonts/BubbleLetter_Face.mat";

        public static readonly Color YellowFill = new Color(0.99f, 0.85f, 0.22f, 1f);       // #FDD835 (Vibrant Golden Yellow)
        public static readonly Color DarkBlueOutline = new Color(0.12f, 0.18f, 0.38f, 1f);   // #1E2E61 (Dark Navy Blue)
        public static readonly Color OrangeDepth = new Color(0.92f, 0.52f, 0.12f, 1f);       // #EB841F (Warm Amber Depth)

        static BubbleLetterStyleSetup()
        {
            EditorApplication.delayCall += AutoSetupIfNeeded;
        }

        private static void AutoSetupIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            if (!SessionState.GetBool("MazeSpell_BubbleLetterSetup_Done_V2", false))
            {
                SessionState.SetBool("MazeSpell_BubbleLetterSetup_Done_V2", true);
                ApplyStyleToAllLevels(false);
            }
        }

        [MenuItem("MazeSpell/Apply Bubble Letter Style to All Levels")]
        public static void ApplyStyleMenu()
        {
            ApplyStyleToAllLevels(true);
        }

        public static void EnsureMaterialsExist(TMP_FontAsset fontAsset, out Material outlineMat, out Material depthMat, out Material faceMat)
        {
            Shader sdfShader = Shader.Find("TextMeshPro/Mobile/Distance Field");
            if (sdfShader == null) sdfShader = Shader.Find("TextMeshPro/Distance Field");

            Texture2D atlasTex = fontAsset.atlasTextures[0];

            // 1. Outline Material
            outlineMat = AssetDatabase.LoadAssetAtPath<Material>(OUTLINE_MAT_PATH);
            if (outlineMat == null)
            {
                outlineMat = new Material(sdfShader);
                AssetDatabase.CreateAsset(outlineMat, OUTLINE_MAT_PATH);
            }
            outlineMat.SetTexture(ShaderUtilities.ID_MainTex, atlasTex);
            outlineMat.EnableKeyword("OUTLINE_ON");
            outlineMat.SetColor(ShaderUtilities.ID_FaceColor, DarkBlueOutline);
            outlineMat.SetColor(ShaderUtilities.ID_OutlineColor, DarkBlueOutline);
            outlineMat.SetFloat(ShaderUtilities.ID_FaceDilate, 0.55f);
            outlineMat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.25f);
            outlineMat.SetFloat(ShaderUtilities.ID_OutlineSoftness, 0f);
            EditorUtility.SetDirty(outlineMat);

            // 2. Depth Material
            depthMat = AssetDatabase.LoadAssetAtPath<Material>(DEPTH_MAT_PATH);
            if (depthMat == null)
            {
                depthMat = new Material(sdfShader);
                AssetDatabase.CreateAsset(depthMat, DEPTH_MAT_PATH);
            }
            depthMat.SetTexture(ShaderUtilities.ID_MainTex, atlasTex);
            depthMat.SetColor(ShaderUtilities.ID_FaceColor, OrangeDepth);
            depthMat.SetFloat(ShaderUtilities.ID_FaceDilate, 0.05f);
            depthMat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);
            EditorUtility.SetDirty(depthMat);

            // 3. Face Material
            faceMat = AssetDatabase.LoadAssetAtPath<Material>(FACE_MAT_PATH);
            if (faceMat == null)
            {
                faceMat = new Material(sdfShader);
                AssetDatabase.CreateAsset(faceMat, FACE_MAT_PATH);
            }
            faceMat.SetTexture(ShaderUtilities.ID_MainTex, atlasTex);
            faceMat.SetColor(ShaderUtilities.ID_FaceColor, YellowFill);
            faceMat.SetFloat(ShaderUtilities.ID_FaceDilate, 0f);
            faceMat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);
            EditorUtility.SetDirty(faceMat);

            AssetDatabase.SaveAssets();
        }

        public static void ApplyStyleToAllLevels(bool interactive = false)
        {
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
            if (fontAsset == null)
            {
                fontAsset = BubbleFontAssetGenerator.GenerateFontAsset("Assets/Fonts/TitanOne-Regular.ttf", FONT_PATH);
            }

            if (fontAsset == null)
            {
                Debug.LogError("[BubbleLetterStyleSetup] Gagal memuat/membuat TitanOne SDF font asset!");
                return;
            }

            EnsureMaterialsExist(fontAsset, out Material outlineMat, out Material depthMat, out Material faceMat);

            int totalUpdated = 0;

            for (int level = 1; level <= 5; level++)
            {
                string prefabPath = $"Assets/Prefabs/MazeGrids/Level{level}_MazeGrid.prefab";
                if (!File.Exists(prefabPath))
                {
                    Debug.LogWarning($"[BubbleLetterStyleSetup] Prefab tidak ditemukan di {prefabPath}");
                    continue;
                }

                GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
                if (root == null) continue;

                var allLetters = root.GetComponentsInChildren<CollectibleLetter>(true);
                foreach (var cl in allLetters)
                {
                    SetupLetterVisualHierarchy(cl.gameObject, cl.Letter, fontAsset, outlineMat, depthMat, faceMat);
                    totalUpdated++;
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                PrefabUtility.UnloadPrefabContents(root);
                Debug.Log($"[BubbleLetterStyleSetup] ✓ Level {level} berhasil diperbarui ({allLetters.Length} huruf).");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[BubbleLetterStyleSetup] SELESAI: {totalUpdated} CollectibleLetter berhasil di-upgrade ke gaya cartoon bubble font!");

            if (interactive)
            {
                EditorUtility.DisplayDialog("Bubble Letter Setup",
                    $"Berhasil menerapkan gaya huruf cartoon bubble (Kuning + Outline Biru + Inner Depth Orange + Glow & Shadow) pada {totalUpdated} huruf di Level 1-5!",
                    "OK");
            }
        }

        public static void SetupLetterVisualHierarchy(
            GameObject letterObj,
            char letterChar,
            TMP_FontAsset fontAsset,
            Material outlineMat,
            Material depthMat,
            Material faceMat)
        {
            // 1. Pastikan Collider2D
            CircleCollider2D col = letterObj.GetComponent<CircleCollider2D>();
            if (col == null) col = letterObj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.45f;

            // 2. Pastikan CollectibleLetter
            CollectibleLetter cl = letterObj.GetComponent<CollectibleLetter>();
            if (cl == null) cl = letterObj.AddComponent<CollectibleLetter>();
            cl.Letter = letterChar;

            // 3. Pastikan Ground Shadow Sprite
            Sprite shadowSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Letter_Shadow.png");
            Transform oldShadow = letterObj.transform.Find("Shadow");
            if (oldShadow != null)
            {
                UnityEngine.Object.DestroyImmediate(oldShadow.gameObject);
            }

            GameObject shadowObj = new GameObject("Shadow");
            shadowObj.transform.SetParent(letterObj.transform, false);
            shadowObj.transform.localPosition = new Vector3(0f, -0.32f, 0.05f);
            shadowObj.transform.localScale = new Vector3(0.65f, 0.32f, 1f);
            SpriteRenderer shadowSr = shadowObj.AddComponent<SpriteRenderer>();
            if (shadowSprite != null) shadowSr.sprite = shadowSprite;
            shadowSr.color = new Color(0f, 0f, 0f, 0.45f);
            shadowSr.sortingOrder = 3;

            // 4. Pastikan LetterHoverAnimation
            LetterHoverAnimation hoverAnim = letterObj.GetComponent<LetterHoverAnimation>();
            if (hoverAnim == null) hoverAnim = letterObj.AddComponent<LetterHoverAnimation>();

            // 5. Bangun atau bersihkan child "Visual"
            Transform oldVisual = letterObj.transform.Find("Visual");
            if (oldVisual != null)
            {
                UnityEngine.Object.DestroyImmediate(oldVisual.gameObject);
            }

            GameObject visualObj = new GameObject("Visual");
            visualObj.transform.SetParent(letterObj.transform, false);
            visualObj.transform.localPosition = Vector3.zero;
            visualObj.transform.localScale = Vector3.one;
            visualObj.transform.localRotation = Quaternion.identity;

            RectTransform rootRect = visualObj.AddComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(1.2f, 1.2f);

            // Layer 0: Halo Glow behind letter
            Sprite glowSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Letter_Glow.png");
            GameObject glowObj = new GameObject("Glow");
            glowObj.transform.SetParent(visualObj.transform, false);
            glowObj.transform.localPosition = new Vector3(0f, 0.02f, 0.01f);
            glowObj.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
            SpriteRenderer glowSr = glowObj.AddComponent<SpriteRenderer>();
            if (glowSprite != null) glowSr.sprite = glowSprite;
            glowSr.color = new Color(1f, 0.88f, 0.35f, 0.65f);
            glowSr.sortingOrder = 4;

            // Layer 1: Outline (Biru Tua - Z = 0f)
            GameObject outlineObj = new GameObject("Outline");
            outlineObj.transform.SetParent(visualObj.transform, false);
            outlineObj.transform.localPosition = new Vector3(0.02f, -0.025f, 0f);
            TextMeshPro tmpOutline = outlineObj.AddComponent<TextMeshPro>();
            tmpOutline.font = fontAsset;
            if (outlineMat != null) tmpOutline.fontSharedMaterial = outlineMat;
            tmpOutline.text = $"<b>{letterChar}</b>";
            tmpOutline.fontSize = 8.5f;
            tmpOutline.alignment = TextAlignmentOptions.Center;
            tmpOutline.sortingOrder = 5;
            tmpOutline.color = Color.white;

            // Layer 2: Depth (Orange Inner Shadow - Z = -0.02f)
            GameObject depthObj = new GameObject("Depth");
            depthObj.transform.SetParent(visualObj.transform, false);
            depthObj.transform.localPosition = new Vector3(0.04f, -0.05f, -0.02f);
            TextMeshPro tmpDepth = depthObj.AddComponent<TextMeshPro>();
            tmpDepth.font = fontAsset;
            if (depthMat != null) tmpDepth.fontSharedMaterial = depthMat;
            tmpDepth.text = $"<b>{letterChar}</b>";
            tmpDepth.fontSize = 8.5f;
            tmpDepth.alignment = TextAlignmentOptions.Center;
            tmpDepth.sortingOrder = 6;
            tmpDepth.color = Color.white;

            // Layer 3: Face (Kuning Golden Fill - Z = -0.04f)
            GameObject faceObj = new GameObject("Face");
            faceObj.transform.SetParent(visualObj.transform, false);
            faceObj.transform.localPosition = new Vector3(0f, 0f, -0.04f);
            TextMeshPro tmpFace = faceObj.AddComponent<TextMeshPro>();
            tmpFace.font = fontAsset;
            if (faceMat != null) tmpFace.fontSharedMaterial = faceMat;
            tmpFace.text = $"<b>{letterChar}</b>";
            tmpFace.fontSize = 8.5f;
            tmpFace.alignment = TextAlignmentOptions.Center;
            tmpFace.sortingOrder = 7;
            tmpFace.color = Color.white;
        }
    }
}
