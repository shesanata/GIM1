using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

namespace MazeSpell.Editor
{
    /// <summary>
    /// Generator aset visual tematik (Floor, Wall, Glow, Shadow, Start Pedestal, Central Medallion)
    /// untuk mewujudkan visual bergaya Cartoon Fantasy Adventure Puzzle di seluruh Level 1 - 5.
    /// </summary>
    [InitializeOnLoad]
    public static class MazeThematicVisualGenerator
    {
        private const int TileSize = 128;

        static MazeThematicVisualGenerator()
        {
            EditorApplication.delayCall += AutoGenerateIfNeeded;
        }

        private static void AutoGenerateIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            if (!SessionState.GetBool("MazeSpell_ThematicVisualsGenerated_V1", false))
            {
                SessionState.SetBool("MazeSpell_ThematicVisualsGenerated_V1", true);
                GenerateAllVisualAssets();
            }
        }

        [MenuItem("MazeSpell/Generate All Thematic Visual Assets & Tiles")]
        public static void GenerateAllVisualAssets()
        {
            Debug.Log("[MazeThematicVisualGenerator] Memulai pembuatan aset visual tematik Level 1 - 5...");

            // 1. Generate Texture Sprites
            GenerateCommonVisualSprites();
            GenerateLevel1EarthTextures();
            GenerateLevel2StoneTextures();
            GenerateLevel3GrassTextures();
            GenerateLevel4FrostTextures();
            GenerateLevel5FlameTextures();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 2. Setup Tiles
            SetupAllLevelTiles();

            Debug.Log("[MazeThematicVisualGenerator] ★ Semua aset visual dan Tile tematik berhasil dibuat!");
        }

        #region Common Visual Sprites (Glow, Shadow, Pedestal, Medallion)

        private static void GenerateCommonVisualSprites()
        {
            // 1. Letter Shadow (Soft blurred dark ellipse)
            {
                Texture2D tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                Color32[] cols = new Color32[64 * 64];
                Vector2 center = new Vector2(31.5f, 31.5f);
                for (int y = 0; y < 64; y++)
                {
                    for (int x = 0; x < 64; x++)
                    {
                        float dx = (x - center.x) / 24f;
                        float dy = (y - center.y) / 12f; // Flattened ellipse
                        float distSq = dx * dx + dy * dy;
                        float alpha = Mathf.Clamp01(1f - Mathf.Sqrt(distSq));
                        alpha = alpha * alpha * 0.45f; // Soft ambient shadow
                        cols[y * 64 + x] = new Color(0f, 0f, 0f, alpha);
                    }
                }
                tex.SetPixels32(cols);
                tex.Apply();
                SavePNG("Assets/Sprites/Letter_Shadow.png", tex);
            }

            // 2. Letter Glow (Radial golden/yellow soft halo)
            {
                Texture2D tex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
                Color32[] cols = new Color32[128 * 128];
                Vector2 center = new Vector2(63.5f, 63.5f);
                for (int y = 0; y < 128; y++)
                {
                    for (int x = 0; x < 128; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), center) / 60f;
                        float alpha = Mathf.Clamp01(1f - dist);
                        alpha = Mathf.Pow(alpha, 1.8f) * 0.7f;
                        cols[y * 128 + x] = new Color(1f, 0.88f, 0.35f, alpha);
                    }
                }
                tex.SetPixels32(cols);
                tex.Apply();
                SavePNG("Assets/Sprites/Letter_Glow.png", tex);
            }

            // 3. Start Pedestal (Cartoon Magic Circle with Rune Rings)
            {
                Texture2D tex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
                Color32[] cols = new Color32[128 * 128];
                Vector2 center = new Vector2(63.5f, 63.5f);
                for (int y = 0; y < 128; y++)
                {
                    for (int x = 0; x < 128; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), center);
                        float angle = Mathf.Atan2(y - center.y, x - center.x);
                        Color c = Color.clear;

                        if (dist <= 58f)
                        {
                            // Outer ring
                            if (dist >= 52f)
                            {
                                c = new Color(1f, 0.92f, 0.55f, 0.85f);
                            }
                            // Inner pattern ring
                            else if (dist >= 44f && dist <= 48f)
                            {
                                c = new Color(1f, 0.85f, 0.35f, 0.75f);
                            }
                            // Star / 8-point ray pattern
                            else if (dist <= 44f && dist >= 16f)
                            {
                                float ray = Mathf.Cos(angle * 8f);
                                if (ray > 0.6f)
                                {
                                    c = new Color(1f, 0.95f, 0.65f, 0.6f);
                                }
                                else
                                {
                                    c = new Color(1f, 0.85f, 0.4f, 0.25f);
                                }
                            }
                            // Center circle
                            else if (dist <= 16f)
                            {
                                c = new Color(1f, 0.95f, 0.7f, 0.9f);
                            }
                        }
                        cols[y * 128 + x] = c;
                    }
                }
                tex.SetPixels32(cols);
                tex.Apply();
                SavePNG("Assets/Sprites/Start_Pedestal.png", tex);
            }

            // 4. Central Medallion (Floor Accent Emblem)
            {
                Texture2D tex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
                Color32[] cols = new Color32[128 * 128];
                Vector2 center = new Vector2(63.5f, 63.5f);
                for (int y = 0; y < 128; y++)
                {
                    for (int x = 0; x < 128; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), center);
                        float angle = Mathf.Atan2(y - center.y, x - center.x);
                        Color c = Color.clear;

                        if (dist <= 56f)
                        {
                            // Outer decorative ring
                            if (dist >= 50f)
                            {
                                c = new Color(0.95f, 0.82f, 0.4f, 0.8f);
                            }
                            else if (dist >= 32f && dist <= 46f)
                            {
                                float flower = Mathf.Sin(angle * 6f);
                                if (flower > 0.2f)
                                    c = new Color(1f, 0.9f, 0.5f, 0.65f);
                                else
                                    c = new Color(0.85f, 0.7f, 0.35f, 0.35f);
                            }
                            else if (dist <= 24f)
                            {
                                c = new Color(1f, 0.95f, 0.6f, 0.85f);
                            }
                        }
                        cols[y * 128 + x] = c;
                    }
                }
                tex.SetPixels32(cols);
                tex.Apply();
                SavePNG("Assets/Sprites/Central_Medallion.png", tex);
            }
        }

        #endregion

        #region Level 1: Earth (Warm Earth, Roots, Moss, Cobbles)

        private static void GenerateLevel1EarthTextures()
        {
            // Floor: Warm terracotta / sand cobblestones with soft moss joints
            {
                Texture2D tex = new Texture2D(TileSize, TileSize, TextureFormat.RGBA32, false);
                for (int y = 0; y < TileSize; y++)
                {
                    for (int x = 0; x < TileSize; x++)
                    {
                        // Cobblestone cell grid (4x4 stone pattern)
                        int gx = x % 32;
                        int gy = y % 32;
                        int stoneIdxX = x / 32;
                        int stoneIdxY = y / 32;
                        if ((stoneIdxY % 2) == 1) gx = (x + 16) % 32;

                        bool isJoint = (gx < 3 || gx > 29 || gy < 3 || gy > 29);
                        float noise = Mathf.PerlinNoise(x * 0.08f, y * 0.08f) * 0.12f;

                        Color c;
                        if (isJoint)
                        {
                            // Joint: rich soil with mossy greenish-brown
                            float mossN = Mathf.PerlinNoise(x * 0.15f, y * 0.15f);
                            c = Color.Lerp(new Color(0.36f, 0.26f, 0.18f, 1f), new Color(0.42f, 0.52f, 0.25f, 1f), mossN);
                        }
                        else
                        {
                            // Stone face: warm earthy sand stone with top-left bevel highlight
                            float bevel = ((32 - gx) + (32 - gy)) / 64f;
                            float brightness = 0.58f + bevel * 0.18f + noise;
                            c = new Color(brightness * 0.92f, brightness * 0.78f, brightness * 0.58f, 1f);
                        }
                        tex.SetPixel(x, y, c);
                    }
                }
                tex.Apply();
                SavePNG("Assets/Sprites/Floor_Earth.png", tex);
            }

            // Wall: Earth mound with rich moss cap, stones, and deep soil shadow
            {
                Texture2D tex = new Texture2D(TileSize, TileSize, TextureFormat.RGBA32, false);
                for (int y = 0; y < TileSize; y++)
                {
                    for (int x = 0; x < TileSize; x++)
                    {
                        float n = Mathf.PerlinNoise(x * 0.07f, y * 0.07f);
                        float detail = Mathf.PerlinNoise(x * 0.2f, y * 0.2f) * 0.1f;
                        Color c;

                        // Top 35% has lush vibrant green moss/grass cap
                        if (y > 75)
                        {
                            float grassShade = 0.48f + n * 0.25f + detail;
                            c = new Color(grassShade * 0.65f, grassShade * 0.88f, grassShade * 0.35f, 1f);
                            // Highlight top edge
                            if (y > 120) c = Color.Lerp(c, new Color(0.75f, 0.92f, 0.45f, 1f), 0.5f);
                        }
                        else if (y > 60)
                        {
                            // Transition edge (moss dripping into earth)
                            float blend = (y - 60f) / 15f;
                            Color earthCol = new Color(0.42f + n * 0.1f, 0.28f + n * 0.08f, 0.16f + n * 0.05f, 1f);
                            Color mossCol = new Color(0.38f, 0.62f, 0.22f, 1f);
                            c = Color.Lerp(earthCol, mossCol, blend);
                        }
                        else
                        {
                            // Deep earthen rock base with shadow at bottom
                            float depth = y / 60f;
                            float baseB = 0.30f + depth * 0.15f + n * 0.1f;
                            c = new Color(baseB * 1.1f, baseB * 0.75f, baseB * 0.45f, 1f);
                            // Bottom shadow
                            if (y < 12) c *= 0.65f;
                        }

                        // Outer border bevel
                        if (x < 4 || x > 123 || y < 4 || y > 123)
                        {
                            c = (x < 4 || y > 123) ? Color.Lerp(c, Color.white, 0.22f) : Color.Lerp(c, Color.black, 0.35f);
                        }

                        tex.SetPixel(x, y, c);
                    }
                }
                tex.Apply();
                SavePNG("Assets/Sprites/Wall_Earth.png", tex);
            }
        }

        #endregion

        #region Level 2: Stone (Ancient Temple, Rune Carvings, Blue Magical Accents)

        private static void GenerateLevel2StoneTextures()
        {
            // Floor: Polished blue-gray temple stones with subtle glowing runic line
            {
                Texture2D tex = new Texture2D(TileSize, TileSize, TextureFormat.RGBA32, false);
                for (int y = 0; y < TileSize; y++)
                {
                    for (int x = 0; x < TileSize; x++)
                    {
                        int gx = x % 64;
                        int gy = y % 64;
                        bool isJoint = (gx < 3 || gx > 60 || gy < 3 || gy > 60);
                        float n = Mathf.PerlinNoise(x * 0.09f, y * 0.09f) * 0.1f;

                        Color c;
                        if (isJoint)
                        {
                            // Deep stone mortar with subtle cyan luminescence
                            c = new Color(0.18f, 0.24f, 0.32f, 1f);
                        }
                        else
                        {
                            float bevel = ((64 - gx) + (64 - gy)) / 128f;
                            float b = 0.44f + bevel * 0.16f + n;
                            c = new Color(b * 0.85f, b * 0.95f, b * 1.12f, 1f);

                            // Subtle embedded ancient rune lines
                            bool isRuneLine = (Mathf.Abs(gx - gy) < 2 || Mathf.Abs(gx + gy - 64) < 2) && (gx > 16 && gx < 48);
                            if (isRuneLine)
                            {
                                c = Color.Lerp(c, new Color(0.35f, 0.85f, 1f, 1f), 0.45f);
                            }
                        }
                        tex.SetPixel(x, y, c);
                    }
                }
                tex.Apply();
                SavePNG("Assets/Sprites/Floor_Stone.png", tex);
            }

            // Wall: Ancient temple stone masonry with chiseled rune carvings and glowing blue crystals
            {
                Texture2D tex = new Texture2D(TileSize, TileSize, TextureFormat.RGBA32, false);
                for (int y = 0; y < TileSize; y++)
                {
                    for (int x = 0; x < TileSize; x++)
                    {
                        float n = Mathf.PerlinNoise(x * 0.06f, y * 0.06f);
                        int blockX = x % 64;
                        int blockY = y % 64;
                        bool isBlockJoint = (blockX < 3 || blockY < 3);

                        Color c;
                        if (isBlockJoint)
                        {
                            c = new Color(0.12f, 0.16f, 0.22f, 1f);
                        }
                        else
                        {
                            float bevel = ((64 - blockX) + (64 - blockY)) / 128f;
                            float b = 0.32f + bevel * 0.22f + n * 0.1f;
                            c = new Color(b * 0.8f, b * 0.9f, b * 1.05f, 1f);

                            // Glowing ancient rune carving in center
                            if (blockX >= 22 && blockX <= 42 && blockY >= 22 && blockY <= 42)
                            {
                                bool runePattern = (blockX == 32 || blockY == 32 || Mathf.Abs(blockX - blockY) < 2);
                                if (runePattern)
                                {
                                    c = Color.Lerp(c, new Color(0.25f, 0.80f, 1f, 1f), 0.75f);
                                }
                            }

                            // Top block highlight
                            if (blockY > 56) c = Color.Lerp(c, new Color(0.65f, 0.80f, 0.95f, 1f), 0.4f);
                        }

                        // Outer tile rim
                        if (x < 4 || x > 123 || y < 4 || y > 123)
                        {
                            c = (x < 4 || y > 123) ? Color.Lerp(c, new Color(0.75f, 0.90f, 1f, 1f), 0.25f) : Color.Lerp(c, Color.black, 0.4f);
                        }

                        tex.SetPixel(x, y, c);
                    }
                }
                tex.Apply();
                SavePNG("Assets/Sprites/Wall_Stone.png", tex);
            }
        }

        #endregion

        #region Level 3: Grass (Lush Lawn, Clover, Small Flowers, Overgrown Hedge)

        private static void GenerateLevel3GrassTextures()
        {
            // Floor: Golden-sand overgrown pavers with clover tufts and pastel petals
            {
                Texture2D tex = new Texture2D(TileSize, TileSize, TextureFormat.RGBA32, false);
                for (int y = 0; y < TileSize; y++)
                {
                    for (int x = 0; x < TileSize; x++)
                    {
                        float n = Mathf.PerlinNoise(x * 0.08f, y * 0.08f);
                        float detail = Mathf.PerlinNoise(x * 0.25f, y * 0.25f) * 0.1f;

                        // Warm pleasant stone-sand floor base
                        float b = 0.68f + n * 0.15f + detail;
                        Color c = new Color(b * 0.95f, b * 0.88f, b * 0.65f, 1f);

                        // Scattered clover grass tufts
                        float grassClumps = Mathf.PerlinNoise(x * 0.18f + 5f, y * 0.18f + 5f);
                        if (grassClumps > 0.68f)
                        {
                            c = Color.Lerp(c, new Color(0.42f, 0.76f, 0.28f, 1f), 0.8f);
                        }

                        // Cute tiny flowers (dots)
                        int flowerModX = x % 37;
                        int flowerModY = y % 37;
                        if ((flowerModX == 18 && flowerModY == 18) || (flowerModX == 6 && flowerModY == 30))
                        {
                            c = new Color(1f, 0.85f, 0.3f, 1f); // Yellow flower
                        }
                        else if (flowerModX == 32 && flowerModY == 8)
                        {
                            c = new Color(1f, 0.6f, 0.75f, 1f); // Pink petal
                        }

                        tex.SetPixel(x, y, c);
                    }
                }
                tex.Apply();
                SavePNG("Assets/Sprites/Floor_Grass.png", tex);
            }

            // Wall: Lush cartoon hedge wall with leafy highlights and floral accents
            {
                Texture2D tex = new Texture2D(TileSize, TileSize, TextureFormat.RGBA32, false);
                for (int y = 0; y < TileSize; y++)
                {
                    for (int x = 0; x < TileSize; x++)
                    {
                        float n1 = Mathf.PerlinNoise(x * 0.07f, y * 0.07f);
                        float n2 = Mathf.PerlinNoise(x * 0.22f, y * 0.22f) * 0.15f;
                        float leafDensity = n1 + n2;

                        // Vibrant fantasy green
                        float g = 0.52f + leafDensity * 0.25f;
                        Color c = new Color(g * 0.45f, g * 0.95f, g * 0.28f, 1f);

                        // Top sunlight leaf highlights
                        if (y > 80)
                        {
                            float topGlow = (y - 80f) / 48f;
                            c = Color.Lerp(c, new Color(0.72f, 0.98f, 0.38f, 1f), topGlow * 0.6f);
                        }
                        // Bottom deep foliage shadow
                        if (y < 25)
                        {
                            float botShadow = (25f - y) / 25f;
                            c = Color.Lerp(c, new Color(0.16f, 0.38f, 0.12f, 1f), botShadow * 0.7f);
                        }

                        // Scattered red/pink berries / tiny flowers
                        int dotX = x % 29;
                        int dotY = y % 29;
                        if ((dotX == 14 && dotY == 14 && y > 30) || (dotX == 5 && dotY == 22 && y > 40))
                        {
                            c = new Color(1f, 0.4f, 0.45f, 1f);
                        }

                        // Bevel rim
                        if (x < 4 || x > 123 || y < 4 || y > 123)
                        {
                            c = (x < 4 || y > 123) ? Color.Lerp(c, new Color(0.85f, 1f, 0.5f, 1f), 0.3f) : Color.Lerp(c, new Color(0.1f, 0.25f, 0.08f, 1f), 0.45f);
                        }

                        tex.SetPixel(x, y, c);
                    }
                }
                tex.Apply();
                SavePNG("Assets/Sprites/Wall_Grass.png", tex);
            }
        }

        #endregion

        #region Level 4: Frost (Glacial Ice, Diamond Crystal Grid, Snow Stalagmites)

        private static void GenerateLevel4FrostTextures()
        {
            // Floor: Crystalline glacial ice with subtle diamond grid and frost sparkle
            {
                Texture2D tex = new Texture2D(TileSize, TileSize, TextureFormat.RGBA32, false);
                for (int y = 0; y < TileSize; y++)
                {
                    for (int x = 0; x < TileSize; x++)
                    {
                        float n = Mathf.PerlinNoise(x * 0.08f, y * 0.08f) * 0.12f;
                        int gx = x % 64;
                        int gy = y % 64;

                        // Diamond crystal facet lines
                        bool isDiamondLine = Mathf.Abs(gx - gy) < 2 || Mathf.Abs(gx + gy - 64) < 2;

                        float b = 0.72f + n;
                        Color c = new Color(b * 0.78f, b * 0.92f, b * 1.05f, 1f);

                        if (isDiamondLine)
                        {
                            c = Color.Lerp(c, new Color(0.95f, 0.98f, 1f, 1f), 0.55f);
                        }

                        // Sparkle star points
                        if ((x % 43 == 21 && y % 43 == 21) || (x % 31 == 10 && y % 31 == 15))
                        {
                            c = new Color(1f, 1f, 1f, 1f);
                        }

                        tex.SetPixel(x, y, c);
                    }
                }
                tex.Apply();
                SavePNG("Assets/Sprites/Floor_Frost.png", tex);
            }

            // Wall: Sculpted snow-ice stalagmite blocks with bright cyan rim and deep arctic blue shadows
            {
                Texture2D tex = new Texture2D(TileSize, TileSize, TextureFormat.RGBA32, false);
                for (int y = 0; y < TileSize; y++)
                {
                    for (int x = 0; x < TileSize; x++)
                    {
                        float n = Mathf.PerlinNoise(x * 0.06f, y * 0.06f);
                        float detail = Mathf.PerlinNoise(x * 0.18f, y * 0.18f) * 0.15f;

                        // Deep glacial blue core
                        float depth = (y + 20f) / 148f;
                        float b = 0.40f + depth * 0.35f + n * 0.1f + detail;
                        Color c = new Color(b * 0.55f, b * 0.85f, b * 1.15f, 1f);

                        // Top frosted snow crest (white/cyan)
                        if (y > 70)
                        {
                            float crest = (y - 70f) / 58f;
                            c = Color.Lerp(c, new Color(0.92f, 0.97f, 1f, 1f), crest * 0.75f);
                        }

                        // Crystal shard facets inside wall
                        int facetX = x % 40;
                        int facetY = y % 40;
                        if (facetX > 15 && facetX < 25 && facetY > 15 && facetY < 35)
                        {
                            c = Color.Lerp(c, new Color(0.45f, 0.92f, 1f, 1f), 0.4f);
                        }

                        // Bottom deep shadow
                        if (y < 15) c *= 0.6f;

                        // Bevel highlight
                        if (x < 4 || x > 123 || y < 4 || y > 123)
                        {
                            c = (x < 4 || y > 123) ? Color.Lerp(c, new Color(0.9f, 0.98f, 1f, 1f), 0.4f) : Color.Lerp(c, new Color(0.1f, 0.2f, 0.4f, 1f), 0.5f);
                        }

                        tex.SetPixel(x, y, c);
                    }
                }
                tex.Apply();
                SavePNG("Assets/Sprites/Wall_Frost.png", tex);
            }
        }

        #endregion

        #region Level 5: Flame (Volcanic Basalt, Ember Veins, Molten Lava)

        private static void GenerateLevel5FlameTextures()
        {
            // Floor: Dark volcanic basalt slabs with glowing ember fissures
            {
                Texture2D tex = new Texture2D(TileSize, TileSize, TextureFormat.RGBA32, false);
                for (int y = 0; y < TileSize; y++)
                {
                    for (int x = 0; x < TileSize; x++)
                    {
                        float n = Mathf.PerlinNoise(x * 0.08f, y * 0.08f) * 0.1f;
                        int gx = x % 64;
                        int gy = y % 64;
                        bool isJoint = (gx < 3 || gx > 60 || gy < 3 || gy > 60);

                        Color c;
                        if (isJoint)
                        {
                            // Glowing fiery ember seam in the joints
                            float emberNoise = Mathf.PerlinNoise(x * 0.15f, y * 0.15f);
                            c = Color.Lerp(new Color(0.85f, 0.25f, 0.05f, 1f), new Color(1f, 0.75f, 0.15f, 1f), emberNoise);
                        }
                        else
                        {
                            float bevel = ((64 - gx) + (64 - gy)) / 128f;
                            float b = 0.26f + bevel * 0.12f + n;
                            // Dark charcoal basalt
                            c = new Color(b * 1.15f, b * 0.85f, b * 0.80f, 1f);

                            // Subtle glowing magma crack in some tiles
                            float crack = Mathf.PerlinNoise(x * 0.1f + 10f, y * 0.1f + 10f);
                            if (crack > 0.72f)
                            {
                                c = Color.Lerp(c, new Color(0.95f, 0.45f, 0.10f, 1f), 0.65f);
                            }
                        }
                        tex.SetPixel(x, y, c);
                    }
                }
                tex.Apply();
                SavePNG("Assets/Sprites/Floor_Flame.png", tex);
            }

            // Wall: Volcanic obsidian rock with glowing magma fissures and fiery rim highlights
            {
                Texture2D tex = new Texture2D(TileSize, TileSize, TextureFormat.RGBA32, false);
                for (int y = 0; y < TileSize; y++)
                {
                    for (int x = 0; x < TileSize; x++)
                    {
                        float n = Mathf.PerlinNoise(x * 0.07f, y * 0.07f);
                        float detail = Mathf.PerlinNoise(x * 0.2f, y * 0.2f) * 0.1f;

                        // Charcoal obsidian base
                        float b = 0.18f + n * 0.12f + detail;
                        Color c = new Color(b * 1.3f, b * 0.8f, b * 0.75f, 1f);

                        // Molten lava fissures
                        float lavaFissure = Mathf.PerlinNoise(x * 0.12f + 20f, y * 0.12f + 20f);
                        if (lavaFissure > 0.65f && y > 15 && y < 110)
                        {
                            float intensity = (lavaFissure - 0.65f) / 0.35f;
                            Color lavaCol = Color.Lerp(new Color(0.9f, 0.2f, 0.05f, 1f), new Color(1f, 0.85f, 0.25f, 1f), intensity);
                            c = Color.Lerp(c, lavaCol, 0.85f);
                        }

                        // Fiery orange rim on top
                        if (y > 90)
                        {
                            float rim = (y - 90f) / 38f;
                            c = Color.Lerp(c, new Color(0.95f, 0.55f, 0.15f, 1f), rim * 0.5f);
                        }

                        // Bottom shadow
                        if (y < 15) c *= 0.5f;

                        // Bevel
                        if (x < 4 || x > 123 || y < 4 || y > 123)
                        {
                            c = (x < 4 || y > 123) ? Color.Lerp(c, new Color(1f, 0.7f, 0.25f, 1f), 0.4f) : Color.Lerp(c, Color.black, 0.6f);
                        }

                        tex.SetPixel(x, y, c);
                    }
                }
                tex.Apply();
                SavePNG("Assets/Sprites/Wall_Flame.png", tex);
            }
        }

        #endregion

        #region Tile Setup & Importer

        private static void SavePNG(string path, Texture2D tex)
        {
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            UnityEngine.Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 128f;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        private static void SetupAllLevelTiles()
        {
            // Level 1: Earth / Ground (Classic Reference Visual)
            EnsureTileAsset("Assets/Tilemaps/Level1/Floor_Tile.asset", "Assets/Sprites/Ground.png", Tile.ColliderType.None);
            EnsureTileAsset("Assets/Tilemaps/Level1/Wall_Tile.asset", "Assets/Sprites/Path_Stone_Blue.png", Tile.ColliderType.Grid);

            // Backward compatibility aliases
            EnsureTileAsset("Assets/Tilemaps/Level1/Path_Stone_Tile.asset", "Assets/Sprites/Ground.png", Tile.ColliderType.None);
            EnsureTileAsset("Assets/Tilemaps/Level1/Grass_Wall_Tile.asset", "Assets/Sprites/Path_Stone_Blue.png", Tile.ColliderType.Grid);
            EnsureTileAsset("Assets/Tilemaps/Level1/Ground_Tile.asset", "Assets/Sprites/Ground.png", Tile.ColliderType.None);

            // Level 2: Stone
            EnsureTileAsset("Assets/Tilemaps/Level2/Floor_Tile.asset", "Assets/Sprites/Floor_Stone.png", Tile.ColliderType.None);
            EnsureTileAsset("Assets/Tilemaps/Level2/Wall_Tile.asset", "Assets/Sprites/Wall_Stone.png", Tile.ColliderType.Grid);
            EnsureTileAsset("Assets/Tilemaps/Level2/Path_Stone_Tile.asset", "Assets/Sprites/Floor_Stone.png", Tile.ColliderType.None);
            EnsureTileAsset("Assets/Tilemaps/Level2/Grass_Wall_Tile.asset", "Assets/Sprites/Wall_Stone.png", Tile.ColliderType.Grid);
            EnsureTileAsset("Assets/Tilemaps/Level2/Ground_Tile.asset", "Assets/Sprites/Wall_Stone.png", Tile.ColliderType.None);

            // Level 3: Grass
            EnsureTileAsset("Assets/Tilemaps/Level3/Floor_Tile.asset", "Assets/Sprites/Floor_Grass.png", Tile.ColliderType.None);
            EnsureTileAsset("Assets/Tilemaps/Level3/Wall_Tile.asset", "Assets/Sprites/Wall_Grass.png", Tile.ColliderType.Grid);
            EnsureTileAsset("Assets/Tilemaps/Level3/Path_Stone_Sand_Tile.asset", "Assets/Sprites/Floor_Grass.png", Tile.ColliderType.None);
            EnsureTileAsset("Assets/Tilemaps/Level3/Earth_Wall_Tile.asset", "Assets/Sprites/Wall_Grass.png", Tile.ColliderType.Grid);
            EnsureTileAsset("Assets/Tilemaps/Level3/Ground_Earth_Tile.asset", "Assets/Sprites/Wall_Grass.png", Tile.ColliderType.None);

            // Level 4: Frost
            EnsureTileAsset("Assets/Tilemaps/Level4/Floor_Tile.asset", "Assets/Sprites/Floor_Frost.png", Tile.ColliderType.None);
            EnsureTileAsset("Assets/Tilemaps/Level4/Wall_Tile.asset", "Assets/Sprites/Wall_Frost.png", Tile.ColliderType.Grid);
            EnsureTileAsset("Assets/Tilemaps/Level4/Path_Stone_Blue_Tile.asset", "Assets/Sprites/Floor_Frost.png", Tile.ColliderType.None);
            EnsureTileAsset("Assets/Tilemaps/Level4/Snow_Ice_Wall_Tile.asset", "Assets/Sprites/Wall_Frost.png", Tile.ColliderType.Grid);
            EnsureTileAsset("Assets/Tilemaps/Level4/Snow_Ice_Tile.asset", "Assets/Sprites/Wall_Frost.png", Tile.ColliderType.None);

            // Level 5: Flame
            EnsureTileAsset("Assets/Tilemaps/Level5/Floor_Tile.asset", "Assets/Sprites/Floor_Flame.png", Tile.ColliderType.None);
            EnsureTileAsset("Assets/Tilemaps/Level5/Wall_Tile.asset", "Assets/Sprites/Wall_Flame.png", Tile.ColliderType.Grid);
            EnsureTileAsset("Assets/Tilemaps/Level5/Path_Stone_Blue_Tile.asset", "Assets/Sprites/Floor_Flame.png", Tile.ColliderType.None);
            EnsureTileAsset("Assets/Tilemaps/Level5/Lava_Rock_Wall_Tile.asset", "Assets/Sprites/Wall_Flame.png", Tile.ColliderType.Grid);
            EnsureTileAsset("Assets/Tilemaps/Level5/Lava_Rock_Tile.asset", "Assets/Sprites/Wall_Flame.png", Tile.ColliderType.None);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureTileAsset(string assetPath, string spritePath, Tile.ColliderType colType)
        {
            Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sp == null)
            {
                Debug.LogWarning($"[MazeThematicVisualGenerator] Sprite tidak ditemukan di {spritePath}");
                return;
            }

            string dir = Path.GetDirectoryName(assetPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, assetPath);
            }

            tile.sprite = sp;
            tile.colliderType = colType;
            tile.color = Color.white;
            EditorUtility.SetDirty(tile);
        }

        #endregion
    }
}
