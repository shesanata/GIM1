using System.IO;
using UnityEngine;
using UnityEditor;

namespace MazeSpell.Editor
{
    public static class ButtonSpriteExtractor
    {
        [MenuItem("MazeSpell/Extract Buttons from User Sheet")]
        public static void Extract()
        {
            string srcPath = "C:/Users/ASUS/.gemini/antigravity-ide/brain/14201a53-61a5-4de8-80ee-c1684f532002/.user_uploaded/media_1789525087073.png";
            byte[] fileData = File.ReadAllBytes(srcPath);
            Texture2D srcTex = new Texture2D(2, 2);
            srcTex.LoadImage(fileData);

            Debug.Log($"[ButtonSpriteExtractor] Loaded source texture: {srcTex.width} x {srcTex.height}");

            // Crop PLAY button: X=79..271, Y in Unity (from bottom):
            // In image top is Y=0, in Unity bottom is Y=0.
            // Image Y=73..132 -> Unity Y = (height - 1 - 132) to (height - 1 - 73)
            int srcH = srcTex.height;

            // Extract PLAY
            int playX = 79;
            int playW = 271 - 79 + 1; // 193
            int playImgY = 73;
            int playH = 132 - 73 + 1; // 60
            int playUnityY = srcH - playImgY - playH;
            SaveCroppedButton(srcTex, playX, playUnityY, playW, playH, "Assets/Ui/Button/btn_cartoon_play.png");

            // Extract LEVEL: X=61..289, Image Y=385..464
            int lvlX = 61;
            int lvlW = 289 - 61 + 1; // 229
            int lvlImgY = 385;
            int lvlH = 464 - 385 + 1; // 80
            int lvlUnityY = srcH - lvlImgY - lvlH;
            SaveCroppedButton(srcTex, lvlX, lvlUnityY, lvlW, lvlH, "Assets/Ui/Button/btn_cartoon_level.png");

            Object.DestroyImmediate(srcTex);
            AssetDatabase.Refresh();
            Debug.Log("[ButtonSpriteExtractor] Both button sprites extracted and imported into Assets/Ui/Button/ successfully!");
        }

        private static void SaveCroppedButton(Texture2D src, int x, int y, int w, int h, string assetPath)
        {
            Texture2D cropped = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = src.GetPixels(x, y, w, h);

            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                // Pure white or pale background from document: make transparent
                if (c.r > 0.92f && c.g > 0.92f && c.b > 0.92f)
                {
                    pixels[i] = new Color(0, 0, 0, 0);
                }
                // Also remove faint red line if any
                else if (c.r > 0.8f && c.g < 0.6f && c.b < 0.6f)
                {
                    pixels[i] = new Color(0, 0, 0, 0);
                }
                else
                {
                    pixels[i] = c;
                }
            }

            cropped.SetPixels(pixels);
            cropped.Apply();

            byte[] pngData = cropped.EncodeToPNG();
            string fullPath = Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));
            File.WriteAllBytes(fullPath, pngData);
            Object.DestroyImmediate(cropped);
            Debug.Log($"Saved {assetPath} ({w} x {h})");
        }
    }
}
