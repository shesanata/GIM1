using System.IO;
using UnityEngine;
using UnityEditor;
using TMPro;

namespace MazeSpell.Editor
{
    public static class TestLetterPreviewCapture
    {
        [MenuItem("MazeSpell/Capture Word Preview")]
        public static void CaptureAll()
        {
            string fontPath = "Assets/Fonts/TitanOne-Regular SDF.asset";
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);

            string word = "EARTH";
            int count = word.Length;
            int width = 180 * count + 80;
            int height = 240;

            GameObject stage = new GameObject("Stage");

            Color yellowFill = new Color(0.961f, 0.773f, 0.259f, 1f);       // #F5C542
            Color darkBlueOutline = new Color(0.188f, 0.286f, 0.651f, 1f);   // #3049A6
            Color orangeDepth = new Color(0.851f, 0.584f, 0.184f, 1f);       // #D9952F

            Shader sdfShader = Shader.Find("TextMeshPro/Mobile/Distance Field");
            if (sdfShader == null) sdfShader = Shader.Find("TextMeshPro/Distance Field");

            for (int i = 0; i < count; i++)
            {
                char c = word[i];
                float xPos = (i - (count - 1) * 0.5f) * 1.5f;

                GameObject letterContainer = new GameObject($"Letter_{c}");
                letterContainer.transform.SetParent(stage.transform, false);
                letterContainer.transform.localPosition = new Vector3(xPos, 0f, 0f);

                // 1. Outline Layer (Biru Tua)
                GameObject outlineObj = new GameObject("Outline");
                outlineObj.transform.SetParent(letterContainer.transform, false);
                outlineObj.transform.localPosition = new Vector3(0.02f, -0.025f, 0f);
                TextMeshPro tmpOutline = outlineObj.AddComponent<TextMeshPro>();
                tmpOutline.font = fontAsset;
                tmpOutline.text = $"<b>{c}</b>";
                tmpOutline.fontSize = 8.5f;
                tmpOutline.alignment = TextAlignmentOptions.Center;
                tmpOutline.sortingOrder = 5;

                Material outlineMat = new Material(sdfShader);
                outlineMat.SetTexture(ShaderUtilities.ID_MainTex, fontAsset.atlasTextures[0]);
                outlineMat.EnableKeyword("OUTLINE_ON");
                outlineMat.SetColor(ShaderUtilities.ID_FaceColor, darkBlueOutline);
                outlineMat.SetColor(ShaderUtilities.ID_OutlineColor, darkBlueOutline);
                outlineMat.SetFloat(ShaderUtilities.ID_FaceDilate, 0.55f);
                outlineMat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.25f);
                tmpOutline.fontSharedMaterial = outlineMat;

                // 2. Depth Layer (Orange)
                GameObject depthObj = new GameObject("Depth");
                depthObj.transform.SetParent(letterContainer.transform, false);
                depthObj.transform.localPosition = new Vector3(0.04f, -0.05f, 0f);
                TextMeshPro tmpDepth = depthObj.AddComponent<TextMeshPro>();
                tmpDepth.font = fontAsset;
                tmpDepth.text = $"<b>{c}</b>";
                tmpDepth.fontSize = 8.5f;
                tmpDepth.alignment = TextAlignmentOptions.Center;
                tmpDepth.sortingOrder = 6;

                Material depthMat = new Material(sdfShader);
                depthMat.SetTexture(ShaderUtilities.ID_MainTex, fontAsset.atlasTextures[0]);
                depthMat.SetColor(ShaderUtilities.ID_FaceColor, orangeDepth);
                depthMat.SetFloat(ShaderUtilities.ID_FaceDilate, 0.05f);
                tmpDepth.fontSharedMaterial = depthMat;

                // 3. Face Layer (Kuning)
                GameObject faceObj = new GameObject("Face");
                faceObj.transform.SetParent(letterContainer.transform, false);
                faceObj.transform.localPosition = Vector3.zero;
                TextMeshPro tmpFace = faceObj.AddComponent<TextMeshPro>();
                tmpFace.font = fontAsset;
                tmpFace.text = $"<b>{c}</b>";
                tmpFace.fontSize = 8.5f;
                tmpFace.alignment = TextAlignmentOptions.Center;
                tmpFace.sortingOrder = 7;

                Material faceMat = new Material(sdfShader);
                faceMat.SetTexture(ShaderUtilities.ID_MainTex, fontAsset.atlasTextures[0]);
                faceMat.SetColor(ShaderUtilities.ID_FaceColor, yellowFill);
                tmpFace.fontSharedMaterial = faceMat;
            }

            GameObject camObj = new GameObject("PreviewCam");
            Camera cam = camObj.AddComponent<Camera>();
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.orthographic = true;
            cam.orthographicSize = 1.8f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.92f, 0.94f, 0.97f, 1f);

            RenderTexture rt = new RenderTexture(width, height, 24);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(camObj);
            Object.DestroyImmediate(stage);

            string outPath = "word_earth_preview.png";
            File.WriteAllBytes(outPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            Debug.Log($"[TestLetterPreviewCapture] Saved word preview to {outPath}");
        }
    }
}
