using System.IO;
using UnityEngine;
using UnityEditor;
using TMPro;

namespace MazeSpell.Editor
{
    public static class BubbleFontAssetGenerator
    {
        [MenuItem("MazeSpell/Generate All Bubble Fonts")]
        public static void GenerateAll()
        {
            GenerateFontAsset("Assets/Fonts/Sniglet-ExtraBold.ttf", "Assets/Fonts/Sniglet-ExtraBold SDF.asset");
            GenerateFontAsset("Assets/Fonts/LilitaOne-Regular.ttf", "Assets/Fonts/LilitaOne-Regular SDF.asset");
            GenerateFontAsset("Assets/Fonts/TitanOne-Regular.ttf", "Assets/Fonts/TitanOne-Regular SDF.asset");
            GenerateFontAsset("Assets/Fonts/ArialRoundedBold.ttf", "Assets/Fonts/ArialRoundedBold SDF.asset");
            GenerateFontAsset("Assets/Fonts/Fredoka.ttf", "Assets/Fonts/Fredoka SDF.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BubbleFontAssetGenerator] Semua font asset berhasil dibuat!");
        }

        public static TMP_FontAsset GenerateFontAsset(string ttfPath, string destinationAssetPath)
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (font == null)
            {
                Debug.LogWarning($"[BubbleFontAssetGenerator] Font file tidak ditemukan: {ttfPath}");
                return null;
            }

            if (File.Exists(destinationAssetPath))
            {
                AssetDatabase.DeleteAsset(destinationAssetPath);
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(font);
            if (fontAsset == null)
            {
                Debug.LogError($"[BubbleFontAssetGenerator] Gagal membuat TMP_FontAsset untuk {font.name}");
                return null;
            }

            AssetDatabase.CreateAsset(fontAsset, destinationAssetPath);
            if (fontAsset.material != null)
            {
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }
            if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0 && fontAsset.atlasTextures[0] != null)
            {
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
            }

            fontAsset.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!?,.-_ ");

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();

            Debug.Log($"[BubbleFontAssetGenerator] Berhasil membuat: {destinationAssetPath}");
            return fontAsset;
        }
    }
}
