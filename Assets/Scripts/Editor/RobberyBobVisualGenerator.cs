using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

namespace MazeSpell.Editor
{
    /// <summary>
    /// Generator aset visual dan tekstur bertema Robbery Bob Top-Down House Layout:
    /// - Floor: Oranye motif karpet retro, Hijau ruang kerja/kamar, Biru kamar mandi, Kayu/Rak dapur
    /// - Wall: Dinding partisi krem/beige interior & Dinding luar biru/navy
    /// - Kop Surat / Secret Document Envelope Collectible
    /// - Alert Balloons ('!' dan '?') & Vision Cone
    /// - Dekorasi ruangan: Meja, Sofa, Kloset Toilet, Rak Buku, Tanaman Pot, Jejak Kaki Biru
    /// </summary>
    public static class RobberyBobVisualGenerator
    {
        private const string SPRITES_DIR = "Assets/Sprites/RobberyBob";
        private const string TILEMAPS_DIR = "Assets/Tilemaps/RobberyBob";

        [MenuItem("MazeSpell/Generate Robbery Bob Visual Assets")]
        public static void GenerateAllRobberyBobAssets()
        {
            if (!Directory.Exists(SPRITES_DIR)) Directory.CreateDirectory(SPRITES_DIR);
            if (!Directory.Exists(TILEMAPS_DIR)) Directory.CreateDirectory(TILEMAPS_DIR);

            Debug.Log("[RobberyBobVisualGenerator] Membuat aset visual bertema Robbery Bob...");

            // 1. Generate Floor Textures
            GenerateOrangeFloor();
            GenerateGreenFloor();
            GenerateBathroomFloor();
            GenerateKitchenWoodFloor();

            // 2. Generate Wall Textures
            GenerateInteriorWall();
            GenerateExteriorWall();

            // 3. Generate Kop Surat / Secret Document Envelope
            GenerateEnvelopeSprite();

            // 4. Generate Alert Balloons & Vision Cone
            GenerateAlertBalloonExclamation();
            GenerateAlertBalloonQuestion();
            GenerateVisionConeSprite();

            // 5. Generate Furniture & Room Decorations
            GenerateDeskDeco();
            GenerateToiletDeco();
            GenerateSofaDeco();
            GenerateShelfDeco();
            GeneratePottedPlantDeco();
            GenerateFootstepsDeco();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 6. Generate Tile Assets
            CreateTileAssets();

            Debug.Log("[RobberyBobVisualGenerator] ★ Semua aset visual Robbery Bob berhasil dibuat!");
        }

        #region Texture Generation

        private static void GenerateOrangeFloor()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color baseCol = new Color(0.97f, 0.62f, 0.15f, 1f); // Vibrant orange carpet
            Color darkCol = new Color(0.88f, 0.52f, 0.08f, 1f); // Darker orange pattern
            Color lightCol = new Color(1.0f, 0.72f, 0.28f, 1f); // Highlight

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Diamond & circular retro carpet pattern
                    float u = (x % 64) / 64f;
                    float v = (y % 64) / 64f;
                    float distCenter = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f;

                    Color c = baseCol;
                    if (distCenter < 0.75f)
                    {
                        c = Color.Lerp(lightCol, baseCol, distCenter / 0.75f);
                    }
                    else if (distCenter > 0.95f)
                    {
                        c = darkCol;
                    }

                    // Border grid line
                    if (x % 64 == 0 || y % 64 == 0 || (x + y) % 64 == 0 || (x - y + 64) % 64 == 0)
                    {
                        c = Color.Lerp(c, darkCol, 0.45f);
                    }

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            SavePNG($"{SPRITES_DIR}/Floor_Robbery_Orange.png", tex);
        }

        private static void GenerateGreenFloor()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color baseCol = new Color(0.38f, 0.68f, 0.20f, 1f); // Green room carpet
            Color darkCol = new Color(0.30f, 0.56f, 0.14f, 1f);
            Color lightCol = new Color(0.48f, 0.76f, 0.28f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x % 64) / 64f;
                    float v = (y % 64) / 64f;
                    float distCenter = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f;

                    Color c = baseCol;
                    if (distCenter < 0.65f)
                    {
                        c = Color.Lerp(lightCol, baseCol, distCenter / 0.65f);
                    }
                    else if (distCenter > 0.92f)
                    {
                        c = darkCol;
                    }

                    if (x % 32 == 0 || y % 32 == 0)
                    {
                        c = Color.Lerp(c, darkCol, 0.35f);
                    }

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            SavePNG($"{SPRITES_DIR}/Floor_Robbery_Green.png", tex);
        }

        private static void GenerateBathroomFloor()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color darkBlue = new Color(0.12f, 0.22f, 0.38f, 1f); // Dark blue tile
            Color lightBlue = new Color(0.18f, 0.32f, 0.52f, 1f); // Highlight tile
            Color groutCol = new Color(0.08f, 0.14f, 0.24f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int tileX = x / 32;
                    int tileY = y / 32;
                    bool isChecker = (tileX + tileY) % 2 == 0;
                    Color c = isChecker ? lightBlue : darkBlue;

                    // Diamond pattern within tile
                    int localX = x % 32;
                    int localY = y % 32;
                    if (Mathf.Abs(localX - 16) + Mathf.Abs(localY - 16) <= 12)
                    {
                        c = isChecker ? Color.Lerp(lightBlue, Color.white, 0.15f) : Color.Lerp(darkBlue, lightBlue, 0.35f);
                    }

                    if (localX == 0 || localY == 0 || localX == 31 || localY == 31)
                    {
                        c = groutCol;
                    }

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            SavePNG($"{SPRITES_DIR}/Floor_Robbery_Bathroom.png", tex);
        }

        private static void GenerateKitchenWoodFloor()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color woodBase = new Color(0.85f, 0.72f, 0.52f, 1f);
            Color woodDark = new Color(0.72f, 0.58f, 0.38f, 1f);
            Color woodLight = new Color(0.92f, 0.82f, 0.65f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int plankIndex = y / 24;
                    float plankShade = (plankIndex % 2 == 0) ? 0.05f : -0.05f;
                    Color c = (plankIndex % 2 == 0) ? woodBase : woodLight;

                    if (y % 24 == 0 || y % 24 == 23)
                    {
                        c = woodDark;
                    }
                    else if ((x + plankIndex * 40) % 64 == 0)
                    {
                        c = woodDark;
                    }

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            SavePNG($"{SPRITES_DIR}/Floor_Robbery_Kitchen.png", tex);
        }

        private static void GenerateInteriorWall()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color wallTop = new Color(0.95f, 0.88f, 0.70f, 1f); // Beige / warm cream top wall
            Color wallOutline = new Color(0.25f, 0.18f, 0.10f, 1f); // Bold outline
            Color wallBevel = new Color(0.85f, 0.76f, 0.55f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Color c = wallTop;
                    if (x < 6 || x >= size - 6 || y < 6 || y >= size - 6)
                    {
                        c = wallOutline;
                    }
                    else if (x < 12 || y >= size - 12)
                    {
                        c = Color.Lerp(wallTop, Color.white, 0.25f);
                    }
                    else if (x >= size - 12 || y < 12)
                    {
                        c = wallBevel;
                    }

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            SavePNG($"{SPRITES_DIR}/Wall_Robbery_Interior.png", tex);
        }

        private static void GenerateExteriorWall()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color wallBlue = new Color(0.15f, 0.45f, 0.88f, 1f); // Bold Blue exterior wall
            Color wallDark = new Color(0.08f, 0.25f, 0.55f, 1f);
            Color wallHighlight = new Color(0.35f, 0.65f, 1.0f, 1f);
            Color wallBorder = new Color(0.04f, 0.12f, 0.30f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Color c = wallBlue;
                    if (x < 6 || x >= size - 6 || y < 6 || y >= size - 6)
                    {
                        c = wallBorder;
                    }
                    else if (x < 14 || y >= size - 14)
                    {
                        c = wallHighlight;
                    }
                    else if (x >= size - 14 || y < 14)
                    {
                        c = wallDark;
                    }

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            SavePNG($"{SPRITES_DIR}/Wall_Robbery_Exterior.png", tex);
        }

        private static void GenerateEnvelopeSprite()
        {
            // Kop Surat / Secret Document Envelope Sprite (128x128)
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color envBg = new Color(0.98f, 0.96f, 0.88f, 1f); // Clean paper cream
            Color envShade = new Color(0.90f, 0.85f, 0.72f, 1f);
            Color envBorder = new Color(0.35f, 0.25f, 0.12f, 1f); // Dark outline
            Color redSeal = new Color(0.88f, 0.18f, 0.18f, 1f); // Red wax stamp seal
            Color goldStamp = new Color(0.98f, 0.78f, 0.15f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    tex.SetPixel(x, y, clear);
                }
            }

            // Envelope rectangle (x: 20 to 108, y: 28 to 100)
            for (int y = 28; y <= 100; y++)
            {
                for (int x = 20; x <= 108; x++)
                {
                    bool isBorder = (x <= 23 || x >= 105 || y <= 31 || y >= 97);
                    if (isBorder)
                    {
                        tex.SetPixel(x, y, envBorder);
                    }
                    else
                    {
                        // Flap diagonal lines (V fold)
                        float midX = 64f;
                        float flapLine = Mathf.Abs(x - midX) * 0.7f + 45f;
                        bool onFlap = Mathf.Abs(y - flapLine) < 2f && y >= 45 && y <= 97;

                        if (onFlap)
                        {
                            tex.SetPixel(x, y, envBorder);
                        }
                        else if (y > flapLine)
                        {
                            tex.SetPixel(x, y, envShade);
                        }
                        else
                        {
                            tex.SetPixel(x, y, envBg);
                        }
                    }
                }
            }

            // Red Wax Seal in Center (64, 62)
            Vector2 sealCenter = new Vector2(64f, 62f);
            for (int y = 48; y <= 76; y++)
            {
                for (int x = 50; x <= 78; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), sealCenter);
                    if (dist <= 13f)
                    {
                        if (dist >= 11.5f)
                            tex.SetPixel(x, y, envBorder);
                        else if (dist <= 6f)
                            tex.SetPixel(x, y, goldStamp);
                        else
                            tex.SetPixel(x, y, redSeal);
                    }
                }
            }

            tex.Apply();
            SavePNG($"{SPRITES_DIR}/Envelope_SecretLetter.png", tex);
        }

        private static void GenerateAlertBalloonExclamation()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color bgRed = new Color(0.92f, 0.18f, 0.18f, 1f); // Alert red
            Color outline = new Color(0.15f, 0.05f, 0.05f, 1f);
            Color white = Color.white;

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, clear);

            Vector2 center = new Vector2(64f, 70f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    // Bubble body + tail
                    bool inBubble = dist <= 42f;
                    bool inTail = (y < 45 && y > 15 && Mathf.Abs(x - 50) < (45 - y) * 0.45f);

                    if (inBubble || inTail)
                    {
                        if (dist >= 39f || (inTail && (x <= 40 || x >= 60 || y <= 20)))
                        {
                            tex.SetPixel(x, y, outline);
                        }
                        else
                        {
                            tex.SetPixel(x, y, bgRed);
                        }
                    }

                    // Exclamation mark '!' in center
                    if (x >= 58 && x <= 70 && y >= 62 && y <= 94)
                    {
                        tex.SetPixel(x, y, white);
                    }
                    if (x >= 58 && x <= 70 && y >= 46 && y <= 56)
                    {
                        tex.SetPixel(x, y, white);
                    }
                }
            }
            tex.Apply();
            SavePNG($"{SPRITES_DIR}/Alert_Exclamation.png", tex);
        }

        private static void GenerateAlertBalloonQuestion()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color bgBlue = new Color(0.18f, 0.62f, 0.95f, 1f); // Curious blue
            Color outline = new Color(0.08f, 0.15f, 0.35f, 1f);
            Color yellow = new Color(1.0f, 0.88f, 0.15f, 1f);

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, clear);

            Vector2 center = new Vector2(64f, 70f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    bool inBubble = dist <= 42f;
                    bool inTail = (y < 45 && y > 15 && Mathf.Abs(x - 50) < (45 - y) * 0.45f);

                    if (inBubble || inTail)
                    {
                        if (dist >= 39f || (inTail && (x <= 40 || x >= 60 || y <= 20)))
                        {
                            tex.SetPixel(x, y, outline);
                        }
                        else
                        {
                            tex.SetPixel(x, y, bgBlue);
                        }
                    }
                }
            }

            // Draw '?' in yellow
            Vector2 qCenter = new Vector2(64f, 78f);
            for (int y = 44; y <= 96; y++)
            {
                for (int x = 46; x <= 82; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), qCenter);
                    bool topArc = d <= 15f && d >= 7f && y >= 72;
                    bool stem = (x >= 60 && x <= 68 && y >= 58 && y <= 74);
                    bool dot = (x >= 60 && x <= 68 && y >= 46 && y <= 54);

                    if (topArc || stem || dot)
                    {
                        tex.SetPixel(x, y, yellow);
                    }
                }
            }

            tex.Apply();
            SavePNG($"{SPRITES_DIR}/Alert_Question.png", tex);
        }

        private static void GenerateVisionConeSprite()
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color coneYellow = new Color(1.0f, 0.92f, 0.35f, 0.28f); // Soft semi-transparent yellow cone
            Color coneEdge = new Color(1.0f, 0.85f, 0.15f, 0.55f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Cone pointing Up from (128, 10) with ~60 degree angle
                    float dx = x - 128f;
                    float dy = y - 10f;
                    if (dy > 0)
                    {
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        float angle = Mathf.Abs(Mathf.Atan2(dx, dy) * Mathf.Rad2Deg);

                        if (angle <= 35f && dist <= 235f)
                        {
                            float fadeDist = Mathf.Clamp01(1f - (dist / 235f));
                            float fadeAngle = Mathf.Clamp01(1f - (angle / 35f));
                            float alpha = fadeDist * fadeAngle * 0.35f;

                            if (angle > 32f || dist > 225f)
                            {
                                tex.SetPixel(x, y, new Color(coneEdge.r, coneEdge.g, coneEdge.b, alpha * 1.5f));
                            }
                            else
                            {
                                tex.SetPixel(x, y, new Color(coneYellow.r, coneYellow.g, coneYellow.b, alpha));
                            }
                        }
                        else
                        {
                            tex.SetPixel(x, y, clear);
                        }
                    }
                    else
                    {
                        tex.SetPixel(x, y, clear);
                    }
                }
            }
            tex.Apply();
            SavePNG($"{SPRITES_DIR}/Vision_Cone.png", tex);
        }

        private static void GenerateDeskDeco()
        {
            // Wood study/office desk with papers & sandwich
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color deskWood = new Color(0.68f, 0.45f, 0.22f, 1f);
            Color deskTop = new Color(0.78f, 0.55f, 0.28f, 1f);
            Color border = new Color(0.25f, 0.15f, 0.08f, 1f);
            Color paperWhite = new Color(0.95f, 0.95f, 0.90f, 1f);
            Color breadBrown = new Color(0.88f, 0.62f, 0.25f, 1f);

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, clear);

            for (int y = 16; y <= 112; y++)
            {
                for (int x = 20; x <= 108; x++)
                {
                    if (x <= 23 || x >= 105 || y <= 19 || y >= 109)
                        tex.SetPixel(x, y, border);
                    else
                        tex.SetPixel(x, y, deskTop);
                }
            }

            // Papers on desk
            for (int y = 30; y <= 60; y++)
                for (int x = 30; x <= 55; x++)
                    tex.SetPixel(x, y, paperWhite);

            // Sandwich plate on desk
            Vector2 plate = new Vector2(80f, 70f);
            for (int y = 55; y <= 85; y++)
            {
                for (int x = 65; x <= 95; x++)
                {
                    if (Vector2.Distance(new Vector2(x, y), plate) <= 12f)
                        tex.SetPixel(x, y, breadBrown);
                }
            }

            tex.Apply();
            SavePNG($"{SPRITES_DIR}/Deco_Robbery_Desk.png", tex);
        }

        private static void GenerateToiletDeco()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color porcelain = new Color(0.96f, 0.98f, 1.0f, 1f);
            Color waterBlue = new Color(0.22f, 0.58f, 0.88f, 1f);
            Color outline = new Color(0.18f, 0.24f, 0.35f, 1f);

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, clear);

            // Water tank (top rectangle)
            for (int y = 75; y <= 105; y++)
            {
                for (int x = 36; x <= 92; x++)
                {
                    if (x <= 38 || x >= 90 || y <= 77 || y >= 103)
                        tex.SetPixel(x, y, outline);
                    else
                        tex.SetPixel(x, y, porcelain);
                }
            }

            // Toilet bowl (oval bottom)
            Vector2 bowlCenter = new Vector2(64f, 48f);
            for (int y = 18; y <= 75; y++)
            {
                for (int x = 36; x <= 92; x++)
                {
                    float dx = (x - bowlCenter.x) / 24f;
                    float dy = (y - bowlCenter.y) / 28f;
                    float d = dx * dx + dy * dy;

                    if (d <= 1.0f)
                    {
                        if (d >= 0.85f)
                            tex.SetPixel(x, y, outline);
                        else if (d <= 0.45f)
                            tex.SetPixel(x, y, waterBlue);
                        else
                            tex.SetPixel(x, y, porcelain);
                    }
                }
            }

            tex.Apply();
            SavePNG($"{SPRITES_DIR}/Deco_Robbery_Toilet.png", tex);
        }

        private static void GenerateSofaDeco()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color sofaGrey = new Color(0.72f, 0.75f, 0.78f, 1f);
            Color sofaDark = new Color(0.55f, 0.58f, 0.62f, 1f);
            Color outline = new Color(0.20f, 0.22f, 0.25f, 1f);

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, clear);

            // Sofa backrest
            for (int y = 20; y <= 108; y++)
            {
                for (int x = 20; x <= 50; x++)
                {
                    if (x <= 23 || y <= 23 || y >= 105)
                        tex.SetPixel(x, y, outline);
                    else
                        tex.SetPixel(x, y, sofaDark);
                }
            }

            // Sofa cushions
            for (int y = 24; y <= 104; y++)
            {
                for (int x = 48; x <= 98; x++)
                {
                    if (x >= 95 || y <= 27 || y >= 101)
                        tex.SetPixel(x, y, outline);
                    else if (y % 40 <= 2)
                        tex.SetPixel(x, y, sofaDark);
                    else
                        tex.SetPixel(x, y, sofaGrey);
                }
            }

            tex.Apply();
            SavePNG($"{SPRITES_DIR}/Deco_Robbery_Sofa.png", tex);
        }

        private static void GenerateShelfDeco()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color wood = new Color(0.52f, 0.35f, 0.18f, 1f);
            Color outline = new Color(0.18f, 0.10f, 0.05f, 1f);
            Color[] items = new Color[] {
                new Color(0.88f, 0.22f, 0.22f, 1f),
                new Color(0.22f, 0.65f, 0.88f, 1f),
                new Color(0.88f, 0.78f, 0.18f, 1f),
                new Color(0.35f, 0.75f, 0.28f, 1f),
                new Color(0.82f, 0.38f, 0.82f, 1f)
            };

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, clear);

            for (int y = 20; y <= 108; y++)
            {
                for (int x = 15; x <= 113; x++)
                {
                    if (x <= 18 || x >= 110 || y <= 23 || y >= 105)
                        tex.SetPixel(x, y, outline);
                    else if (y % 42 <= 4)
                        tex.SetPixel(x, y, wood);
                    else
                    {
                        int itemIdx = (x / 18) % items.Length;
                        tex.SetPixel(x, y, items[itemIdx]);
                    }
                }
            }

            tex.Apply();
            SavePNG($"{SPRITES_DIR}/Deco_Robbery_Shelves.png", tex);
        }

        private static void GeneratePottedPlantDeco()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color potBrown = new Color(0.72f, 0.42f, 0.20f, 1f);
            Color leafGreen = new Color(0.25f, 0.72f, 0.20f, 1f);
            Color leafDark = new Color(0.15f, 0.48f, 0.12f, 1f);
            Color outline = new Color(0.12f, 0.22f, 0.08f, 1f);

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, clear);

            Vector2 center = new Vector2(64f, 64f);

            // Pot circle
            for (int y = 40; y <= 88; y++)
            {
                for (int x = 40; x <= 88; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    if (d <= 20f)
                    {
                        if (d >= 18f) tex.SetPixel(x, y, outline);
                        else tex.SetPixel(x, y, potBrown);
                    }
                }
            }

            // 6 Leaf petals radiating outward
            for (int a = 0; a < 6; a++)
            {
                float ang = a * 60f * Mathf.Deg2Rad;
                Vector2 leafTip = center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 36f;
                for (int y = 10; y < 118; y++)
                {
                    for (int x = 10; x < 118; x++)
                    {
                        Vector2 pt = new Vector2(x, y);
                        float d1 = Vector2.Distance(pt, center);
                        float d2 = Vector2.Distance(pt, leafTip);
                        if (d1 + d2 <= 44f)
                        {
                            tex.SetPixel(x, y, (a % 2 == 0) ? leafGreen : leafDark);
                        }
                    }
                }
            }

            tex.Apply();
            SavePNG($"{SPRITES_DIR}/Deco_Robbery_Plant.png", tex);
        }

        private static void GenerateFootstepsDeco()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color stepBlue = new Color(0.18f, 0.65f, 0.98f, 0.85f);
            Color stepBorder = new Color(0.08f, 0.35f, 0.65f, 0.95f);

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, clear);

            // Left Footprint
            Vector2 leftSole = new Vector2(45f, 55f);
            Vector2 leftHeel = new Vector2(45f, 32f);
            // Right Footprint
            Vector2 rightSole = new Vector2(80f, 75f);
            Vector2 rightHeel = new Vector2(80f, 52f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    bool onLeft = (Vector2.Distance(p, leftSole) <= 14f || Vector2.Distance(p, leftHeel) <= 10f);
                    bool onRight = (Vector2.Distance(p, rightSole) <= 14f || Vector2.Distance(p, rightHeel) <= 10f);

                    if (onLeft || onRight)
                    {
                        tex.SetPixel(x, y, stepBlue);
                    }
                }
            }
            tex.Apply();
            SavePNG($"{SPRITES_DIR}/Deco_Robbery_Footsteps.png", tex);
        }

        private static void SavePNG(string assetPath, Texture2D tex)
        {
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(assetPath, bytes);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 128;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }

        #endregion

        #region Tile Assets Setup

        private static void CreateTileAssets()
        {
            CreateOrUpdateTile("Floor_Orange_Tile.asset", $"{SPRITES_DIR}/Floor_Robbery_Orange.png", Tile.ColliderType.None);
            CreateOrUpdateTile("Floor_Green_Tile.asset", $"{SPRITES_DIR}/Floor_Robbery_Green.png", Tile.ColliderType.None);
            CreateOrUpdateTile("Floor_Bathroom_Tile.asset", $"{SPRITES_DIR}/Floor_Robbery_Bathroom.png", Tile.ColliderType.None);
            CreateOrUpdateTile("Floor_Kitchen_Tile.asset", $"{SPRITES_DIR}/Floor_Robbery_Kitchen.png", Tile.ColliderType.None);

            CreateOrUpdateTile("Wall_Interior_Tile.asset", $"{SPRITES_DIR}/Wall_Robbery_Interior.png", Tile.ColliderType.Grid);
            CreateOrUpdateTile("Wall_Exterior_Tile.asset", $"{SPRITES_DIR}/Wall_Robbery_Exterior.png", Tile.ColliderType.Grid);
        }

        private static void CreateOrUpdateTile(string tileName, string spritePath, Tile.ColliderType colType)
        {
            string fullPath = $"{TILEMAPS_DIR}/{tileName}";
            Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sp == null)
            {
                Debug.LogWarning($"[RobberyBobVisualGenerator] Sprite {spritePath} belum siap.");
                return;
            }

            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(fullPath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, fullPath);
            }

            tile.sprite = sp;
            tile.color = Color.white;
            tile.colliderType = colType;
            EditorUtility.SetDirty(tile);
        }

        #endregion
    }
}
