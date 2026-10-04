using System;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

namespace MazeSpell.Editor
{
    /// <summary>
    /// Generator komprehensif untuk menghasilkan seluruh texture, sprite furniture lengkap,
    /// wall directional tiles, seamless floors, outdoor decor, dan exit marker sesuai referensi Robbery Bob.
    /// </summary>
    public static class HouseMapAssetGenerator
    {
        private const string SpritesDir = "Assets/Sprites/HouseMap";
        private const string TilemapsDir = "Assets/Tilemaps/HouseMap";

        [MenuItem("MazeSpell/Generate All House Map Assets")]
        public static void GenerateAllAssets()
        {
            if (!Directory.Exists(SpritesDir)) Directory.CreateDirectory(SpritesDir);
            if (!Directory.Exists(TilemapsDir)) Directory.CreateDirectory(TilemapsDir);

            Debug.Log("[HouseMapAssetGenerator] Menghasilkan Texture High-Quality Cartoon House, Furniture & Tiles...");

            // 1. Generate Seamless Floor Textures (64x64)
            GenerateFloors();

            // 2. Generate Directional Wall & Fence Textures (64x64)
            GenerateWalls();

            // 3. Generate Rug & Carpet Sprites
            GenerateRugs();

            // 4. Generate Comprehensive Furniture & Outdoor Sprites
            GenerateFurnitureSprites();

            // 5. Generate Vision Cone & Exit Marker
            GenerateVisionConeAndExit();

            AssetDatabase.Refresh();

            // 6. Create Unity Tile Assets
            CreateTileAssets();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[HouseMapAssetGenerator] ✓ Seluruh Asset Rumah Cartoon Berhasil Dibuat!");
        }

        #region Floor Textures

        private static void GenerateFloors()
        {
            // Floor_Wood_Warm (Papan Kayu Hangat Seamless)
            CreateTexture("Floor_Wood_Warm.png", 64, 64, (x, y) =>
            {
                int plankH = 16;
                int plankIdx = y / plankH;
                int py = y % plankH;
                bool isPlankSeamH = (py == 0 || py == 15);

                int offset = (plankIdx % 2 == 0) ? 0 : 32;
                int px = (x + offset) % 64;
                bool isPlankSeamV = (px == 0 && py > 0 && py < 15);

                if (isPlankSeamH || isPlankSeamV)
                {
                    return new Color(0.60f, 0.44f, 0.28f, 1f); // Dark wood groove seam
                }

                // Wood grain gradient & Perlin noise
                float grain = Mathf.PerlinNoise(x * 0.12f, (y + plankIdx * 20) * 0.04f) * 0.05f;
                float plankVariation = (plankIdx % 3 == 0) ? 0.03f : ((plankIdx % 3 == 1) ? -0.02f : 0f);
                float topPlankBevel = (py == 14) ? 0.04f : ((py == 1) ? -0.03f : 0f);

                float r = 0.88f + plankVariation + topPlankBevel - grain;
                float g = 0.72f + plankVariation * 0.9f + topPlankBevel * 0.8f - grain;
                float b = 0.52f + plankVariation * 0.8f + topPlankBevel * 0.7f - grain;

                return new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b), 1f);
            });

            // Floor_Brown_Wood (Kayu Cokelat Gelap / Walnut)
            CreateTexture("Floor_Brown_Wood.png", 64, 64, (x, y) =>
            {
                int plankH = 16;
                int plankIdx = y / plankH;
                int py = y % plankH;
                bool isPlankSeamH = (py == 0 || py == 15);

                int offset = (plankIdx % 2 == 0) ? 16 : 48;
                int px = (x + offset) % 64;
                bool isPlankSeamV = (px == 0 && py > 0 && py < 15);

                if (isPlankSeamH || isPlankSeamV) return new Color(0.40f, 0.26f, 0.16f, 1f);

                float grain = Mathf.PerlinNoise(x * 0.14f, (y + plankIdx * 20) * 0.05f) * 0.04f;
                float plankVariation = (plankIdx % 2 == 0) ? 0.02f : -0.02f;
                float topBevel = (py == 14) ? 0.03f : 0f;

                return new Color(0.62f + plankVariation + topBevel - grain,
                                 0.42f + plankVariation * 0.8f + topBevel * 0.8f - grain,
                                 0.28f + plankVariation * 0.7f + topBevel * 0.7f - grain, 1f);
            });

            // Floor_Blue_Bathroom (Keramik Kamar Mandi Halus)
            CreateTexture("Floor_Blue_Bathroom.png", 64, 64, (x, y) =>
            {
                int tileSize = 16;
                int tx = x % tileSize;
                int ty = y % tileSize;
                bool isGrout = (tx == 0 || ty == 0);
                if (isGrout) return new Color(0.85f, 0.90f, 0.96f, 1f); // White grout

                bool isTileShine = (tx == 1 || ty == tileSize - 1);
                float shine = isTileShine ? 0.06f : 0f;

                int cellX = x / tileSize;
                int cellY = y / tileSize;
                float alt = ((cellX + cellY) % 2 == 0) ? 0.03f : 0f;

                return new Color(0.48f + alt + shine, 0.68f + alt + shine, 0.85f + alt + shine, 1f);
            });

            // Floor_Corridor_Tile (Keramik Halus Lorong / Aula / Teras)
            CreateTexture("Floor_Corridor_Tile.png", 64, 64, (x, y) =>
            {
                int tileSize = 32;
                int tx = x % tileSize;
                int ty = y % tileSize;
                bool isGrout = (tx == 0 || ty == 0);
                if (isGrout) return new Color(0.72f, 0.70f, 0.68f, 1f);

                float noise = Mathf.PerlinNoise(x * 0.2f, y * 0.2f) * 0.03f;
                bool isTileEdge = (tx == 1 || ty == tileSize - 1);
                float bevel = isTileEdge ? 0.03f : 0f;

                return new Color(0.86f + bevel - noise, 0.84f + bevel - noise, 0.80f + bevel - noise, 1f);
            });

            // Floor_Yard_Grass (Rumput Taman Hijau dengan Bunga Kecil)
            CreateTexture("Floor_Yard_Grass.png", 64, 64, (x, y) =>
            {
                float noise = Mathf.PerlinNoise(x * 0.18f, y * 0.18f) * 0.08f;
                float blade = ((x * 11 + y * 17) % 23 == 0) ? 0.05f : 0f;

                // Tiny white daisy flowers scattered
                if ((x == 18 && y == 24) || (x == 45 && y == 50) || (x == 32 && y == 12))
                {
                    return new Color(0.98f, 0.98f, 0.95f, 1f);
                }
                if ((x == 17 && y == 24) || (x == 19 && y == 24) || (x == 18 && y == 23) || (x == 18 && y == 25))
                {
                    return new Color(0.95f, 0.85f, 0.20f, 1f); // Flower yellow center
                }

                return new Color(0.32f + blade - noise, 0.62f + blade - noise, 0.20f + blade - noise, 1f);
            });

            // Floor_Stone_Path (Batu Paving Jalan Setapak Halaman)
            CreateTexture("Floor_Stone_Path.png", 64, 64, (x, y) =>
            {
                int cx = x - 32;
                int cy = y - 32;
                float dist = Mathf.Sqrt(cx * cx + cy * cy);
                if (dist <= 24f)
                {
                    bool isBorder = (dist >= 21f);
                    if (isBorder) return new Color(0.55f, 0.58f, 0.60f, 1f); // Stone edge shadow
                    float noise = Mathf.PerlinNoise(x * 0.25f, y * 0.25f) * 0.05f;
                    float topLight = (cy > 0) ? 0.04f : -0.02f;
                    return new Color(0.84f + topLight - noise, 0.86f + topLight - noise, 0.88f + topLight - noise, 1f);
                }

                // Grass around stone
                float gNoise = Mathf.PerlinNoise(x * 0.18f, y * 0.18f) * 0.08f;
                return new Color(0.32f - gNoise, 0.62f - gNoise, 0.20f - gNoise, 1f);
            });

            // Floor_Carport_Dark (Aspal Carport Garasi)
            CreateTexture("Floor_Carport_Dark.png", 64, 64, (x, y) =>
            {
                float noise = Mathf.PerlinNoise(x * 0.35f, y * 0.35f) * 0.05f;
                bool isJoint = (x % 32 == 0 || y % 32 == 0);
                if (isJoint) return new Color(0.18f, 0.20f, 0.22f, 1f);
                return new Color(0.28f - noise, 0.30f - noise, 0.32f - noise, 1f);
            });

            // Floor_Water_Pond (Air Kolam Biru Jernih dengan Teratai)
            CreateTexture("Floor_Water_Pond.png", 64, 64, (x, y) =>
            {
                int cx = x - 32;
                int cy = y - 32;
                float dist = Mathf.Sqrt(cx * cx + cy * cy);
                if (dist >= 26f) return new Color(0.58f, 0.56f, 0.52f, 1f); // Stone rim around pond

                // Water ripple
                float wave = Mathf.Sin(dist * 0.6f) * 0.04f;

                // Lily pad in pond
                int lcx = x - 22; int lcy = y - 26;
                if (lcx * lcx + lcy * lcy <= 25) return new Color(0.25f, 0.65f, 0.22f, 1f);

                return new Color(0.24f + wave, 0.55f + wave, 0.82f + wave, 1f);
            });
        }

        #endregion

        #region Wall Textures (Continuous Directional Cartoon House Walls)

        private static void GenerateWalls()
        {
            // Tailored 2D Cartoon House Palette
            Color wallBase = new Color(0.925f, 0.885f, 0.810f, 1f);       // Soft Cream/Beige Body (#ECE2CF)
            Color wallTopHighlight = new Color(0.970f, 0.950f, 0.900f, 1f); // Top Bevel Highlight (#F7F2E6)
            Color wallDepth = new Color(0.780f, 0.710f, 0.590f, 1f);      // South-facing Depth Face (#C7B597)
            Color wallBaseboard = new Color(0.500f, 0.420f, 0.300f, 1f);  // Baseboard Line (#806B4D)
            Color outerRoofTrim = new Color(0.360f, 0.280f, 0.190f, 1f);  // Dark Exterior Roof Fascia (#5C4730)
            Color outerRoofRidge = new Color(0.480f, 0.390f, 0.280f, 1f); // Roof Ridge Highlight (#7A6347)
            Color outerRoofShadow = new Color(0.240f, 0.180f, 0.120f, 1f);// Roof Bottom Shadow (#3D2E1F)
            Color doorWood = new Color(0.500f, 0.340f, 0.190f, 1f);       // Door Jamb Post Wood (#805730)
            Color doorWoodHi = new Color(0.680f, 0.490f, 0.290f, 1f);     // Door Jamb Highlight (#AD7D4A)
            Color grassColor = new Color(0.320f, 0.620f, 0.200f, 1f);     // Fence Grass Base

            // 1. Wall_Int_H (Partisi Horizontal Dalam Ruangan - Invariant terhadap X, Seamless Beam)
            CreateTexture("Wall_Int_H.png", 64, 64, (x, y) =>
            {
                if (y >= 58) return Color.Lerp(wallBase, wallTopHighlight, (y - 58f) / 5f);
                if (y >= 14) return wallBase;
                if (y >= 4) return Color.Lerp(wallBaseboard, wallDepth, (y - 4f) / 9f);
                return wallBaseboard;
            });

            // 2. Wall_Int_V (Partisi Vertikal Dalam Ruangan - Invariant terhadap Y, Seamless Beam)
            CreateTexture("Wall_Int_V.png", 64, 64, (x, y) =>
            {
                if (x <= 6) return Color.Lerp(wallTopHighlight, wallBase, x / 6f);
                if (x <= 53) return wallBase;
                if (x <= 59) return Color.Lerp(wallDepth, wallBaseboard, (x - 54f) / 5f);
                return wallBaseboard;
            });

            // 2b. Wall_Face_Left (Partisi Vertikal menghadap Room di Kiri, menyatu dengan Dinding Luar di Kanan)
            CreateTexture("Wall_Face_Left.png", 64, 64, (x, y) =>
            {
                if (x <= 6) return Color.Lerp(wallTopHighlight, wallBase, x / 6f);
                return wallBase;
            });

            // 2c. Wall_Face_Right (Partisi Vertikal menghadap Room di Kanan, menyatu dengan Dinding Luar di Kiri)
            CreateTexture("Wall_Face_Right.png", 64, 64, (x, y) =>
            {
                if (x <= 53) return wallBase;
                if (x <= 59) return Color.Lerp(wallDepth, wallBaseboard, (x - 54f) / 5f);
                return wallBaseboard;
            });

            // 3. Wall_Corner_TL (Sudut Dalam Top-Left / Menghubungkan South & East)
            CreateTexture("Wall_Corner_TL.png", 64, 64, (x, y) =>
            {
                if (x <= 6 && y >= 14) return Color.Lerp(wallTopHighlight, wallBase, x / 6f);
                if (y >= 58 && x <= 53) return Color.Lerp(wallBase, wallTopHighlight, (y - 58f) / 5f);
                if (x <= 53 && y >= 14) return wallBase;

                // Cabang East (x > 53)
                if (x > 53 && y >= 14 && y <= 57) return wallBase;
                if (x > 53 && y >= 58) return Color.Lerp(wallBase, wallTopHighlight, (y - 58f) / 5f);
                if (x > 53 && y >= 4 && y < 14) return Color.Lerp(wallBaseboard, wallDepth, (y - 4f) / 9f);
                if (x > 53 && y < 4) return wallBaseboard;

                // Cabang South (y < 14)
                if (y < 14 && x <= 53 && x >= 7) return wallBase;
                if (y < 14 && x <= 6) return Color.Lerp(wallTopHighlight, wallBase, x / 6f);
                if (y < 14 && x >= 54 && x <= 59) return Color.Lerp(wallDepth, wallBaseboard, (x - 54f) / 5f);
                if (y < 14 && x >= 60) return wallBaseboard;

                return wallBase;
            });

            // 4. Wall_Corner_TR (Sudut Dalam Top-Right / Menghubungkan South & West)
            CreateTexture("Wall_Corner_TR.png", 64, 64, (x, y) =>
            {
                if (y >= 58 && x <= 53) return Color.Lerp(wallBase, wallTopHighlight, (y - 58f) / 5f);
                if (x <= 53 && y >= 14) return wallBase;
                if (x >= 54 && x <= 59 && y >= 14) return Color.Lerp(wallDepth, wallBaseboard, (x - 54f) / 5f);
                if (x >= 60 && y >= 14) return wallBaseboard;

                // Cabang South (y < 14)
                if (y < 14 && x <= 53 && x >= 7) return wallBase;
                if (y < 14 && x <= 6) return Color.Lerp(wallTopHighlight, wallBase, x / 6f);
                if (y < 14 && x >= 54 && x <= 59) return Color.Lerp(wallDepth, wallBaseboard, (x - 54f) / 5f);
                if (y < 14 && x >= 60) return wallBaseboard;

                return wallBaseboard;
            });

            // 5. Wall_Corner_BL (Sudut Dalam Bottom-Left / Menghubungkan North & East)
            CreateTexture("Wall_Corner_BL.png", 64, 64, (x, y) =>
            {
                if (x <= 6 && y >= 14) return Color.Lerp(wallTopHighlight, wallBase, x / 6f);
                if (x <= 53 && y >= 14) return wallBase;
                if (x > 53 && y >= 14 && y <= 57) return wallBase;
                if (x > 53 && y >= 58) return Color.Lerp(wallBase, wallTopHighlight, (y - 58f) / 5f);

                // South depth face & baseboard
                if (y >= 4 && y < 14) return Color.Lerp(wallBaseboard, wallDepth, (y - 4f) / 9f);
                if (y < 4) return wallBaseboard;

                return wallBase;
            });

            // 6. Wall_Corner_BR (Sudut Dalam Bottom-Right / Menghubungkan North & West)
            CreateTexture("Wall_Corner_BR.png", 64, 64, (x, y) =>
            {
                if (x <= 53 && y >= 14) return wallBase;
                if (x >= 54 && x <= 59 && y >= 14) return Color.Lerp(wallDepth, wallBaseboard, (x - 54f) / 5f);
                if (x >= 60 && y >= 14) return wallBaseboard;

                if (y >= 4 && y < 14) return Color.Lerp(wallBaseboard, wallDepth, (y - 4f) / 9f);
                if (y < 4) return wallBaseboard;

                return wallBase;
            });

            // 7. Wall_TJunc_Down (T-Junction ke Bawah / W, E, S menyatu)
            CreateTexture("Wall_TJunc_Down.png", 64, 64, (x, y) =>
            {
                if (y >= 58) return Color.Lerp(wallBase, wallTopHighlight, (y - 58f) / 5f);
                if (y >= 14) return wallBase;

                // Cabang vertikal ke bawah (x: 7..53)
                if (x >= 7 && x <= 53) return wallBase;
                if (x < 7)
                {
                    if (y >= 4) return Color.Lerp(wallBaseboard, wallDepth, (y - 4f) / 9f);
                    return wallBaseboard;
                }
                if (x >= 54 && x <= 59) return Color.Lerp(wallDepth, wallBaseboard, (x - 54f) / 5f);
                return wallBaseboard;
            });

            // 8. Wall_TJunc_Up (T-Junction ke Atas / W, E, N menyatu)
            CreateTexture("Wall_TJunc_Up.png", 64, 64, (x, y) =>
            {
                if (y >= 58)
                {
                    if (x >= 7 && x <= 53) return wallBase;
                    return Color.Lerp(wallBase, wallTopHighlight, (y - 58f) / 5f);
                }
                if (y >= 14) return wallBase;
                if (y >= 4) return Color.Lerp(wallBaseboard, wallDepth, (y - 4f) / 9f);
                return wallBaseboard;
            });

            // 9. Wall_TJunc_Right (T-Junction ke Kanan / N, S, E menyatu)
            CreateTexture("Wall_TJunc_Right.png", 64, 64, (x, y) =>
            {
                if (x <= 6) return Color.Lerp(wallTopHighlight, wallBase, x / 6f);
                if (x <= 53) return wallBase;

                // Cabang East (x > 53, y: 14..57)
                if (y >= 14 && y <= 57) return wallBase;
                if (y >= 58) return Color.Lerp(wallBase, wallTopHighlight, (y - 58f) / 5f);
                if (y >= 4 && y < 14) return Color.Lerp(wallBaseboard, wallDepth, (y - 4f) / 9f);
                if (y < 4) return wallBaseboard;

                if (x >= 54 && x <= 59) return Color.Lerp(wallDepth, wallBaseboard, (x - 54f) / 5f);
                return wallBaseboard;
            });

            // 10. Wall_TJunc_Left (T-Junction ke Kiri / N, S, W menyatu)
            CreateTexture("Wall_TJunc_Left.png", 64, 64, (x, y) =>
            {
                if (x >= 54 && x <= 59) return Color.Lerp(wallDepth, wallBaseboard, (x - 54f) / 5f);
                if (x >= 60) return wallBaseboard;
                if (x >= 7) return wallBase;

                // Cabang West (x < 7, y: 14..57)
                if (y >= 14 && y <= 57) return wallBase;
                if (y >= 58) return Color.Lerp(wallBase, wallTopHighlight, (y - 58f) / 5f);
                if (y >= 4 && y < 14) return Color.Lerp(wallBaseboard, wallDepth, (y - 4f) / 9f);
                if (y < 4) return wallBaseboard;

                return wallBase;
            });

            // 11. Wall_Cross (Persimpangan 4 Arah / N, S, E, W menyatu)
            CreateTexture("Wall_Cross.png", 64, 64, (x, y) =>
            {
                if (y >= 58 && (x < 7 || x > 53)) return Color.Lerp(wallBase, wallTopHighlight, (y - 58f) / 5f);
                if (x <= 6 && (y < 14 || y > 57)) return Color.Lerp(wallTopHighlight, wallBase, x / 6f);
                if (x >= 54 && x <= 59 && (y < 14 || y > 57)) return Color.Lerp(wallDepth, wallBaseboard, (x - 54f) / 5f);
                if (x >= 60 && (y < 14 || y > 57)) return wallBaseboard;
                if (y >= 4 && y < 14 && (x < 7 || x > 53)) return Color.Lerp(wallBaseboard, wallDepth, (y - 4f) / 9f);
                if (y < 4 && (x < 7 || x > 53)) return wallBaseboard;
                return wallBase;
            });

            // 12. Wall_End_E (Ujung Dinding Menghadap Pintu Timur - Kusen Kayu Halus)
            CreateTexture("Wall_End_E.png", 64, 64, (x, y) =>
            {
                // Kusen pintu kayu di ujung timur (x: 44..56, y: 8..58)
                if (x >= 44 && x <= 56 && y >= 8 && y <= 58)
                {
                    bool isBorder = (x == 44 || x == 56 || y == 8 || y == 58);
                    if (isBorder) return new Color(0.35f, 0.22f, 0.12f, 1f);
                    bool isHi = (x >= 46 && x <= 48);
                    return isHi ? doorWoodHi : doorWood;
                }

                if (x <= 43)
                {
                    if (y >= 58) return Color.Lerp(wallBase, wallTopHighlight, (y - 58f) / 5f);
                    if (y >= 14) return wallBase;
                    if (y >= 4) return Color.Lerp(wallBaseboard, wallDepth, (y - 4f) / 9f);
                    return wallBaseboard;
                }

                return new Color(0f, 0f, 0f, 0f);
            });

            // 13. Wall_End_W (Ujung Dinding Menghadap Pintu Barat - Kusen Kayu Halus)
            CreateTexture("Wall_End_W.png", 64, 64, (x, y) =>
            {
                // Kusen pintu kayu di ujung barat (x: 8..20, y: 8..58)
                if (x >= 8 && x <= 20 && y >= 8 && y <= 58)
                {
                    bool isBorder = (x == 8 || x == 20 || y == 8 || y == 58);
                    if (isBorder) return new Color(0.35f, 0.22f, 0.12f, 1f);
                    bool isHi = (x >= 10 && x <= 12);
                    return isHi ? doorWoodHi : doorWood;
                }

                if (x >= 21)
                {
                    if (y >= 58) return Color.Lerp(wallBase, wallTopHighlight, (y - 58f) / 5f);
                    if (y >= 14) return wallBase;
                    if (y >= 4) return Color.Lerp(wallBaseboard, wallDepth, (y - 4f) / 9f);
                    return wallBaseboard;
                }

                return new Color(0f, 0f, 0f, 0f);
            });

            // 14. Wall_End_S (Ujung Dinding Menghadap Pintu Selatan - Kusen Kayu Halus)
            CreateTexture("Wall_End_S.png", 64, 64, (x, y) =>
            {
                // Kusen pintu kayu di ujung selatan (x: 8..53, y: 8..20)
                if (x >= 8 && x <= 53 && y >= 8 && y <= 20)
                {
                    bool isBorder = (x == 8 || x == 53 || y == 8 || y == 20);
                    if (isBorder) return new Color(0.35f, 0.22f, 0.12f, 1f);
                    bool isHi = (y >= 16 && y <= 18);
                    return isHi ? doorWoodHi : doorWood;
                }

                if (y >= 21)
                {
                    if (x <= 6) return Color.Lerp(wallTopHighlight, wallBase, x / 6f);
                    if (x <= 53) return wallBase;
                    if (x <= 59) return Color.Lerp(wallDepth, wallBaseboard, (x - 54f) / 5f);
                    return wallBaseboard;
                }

                return new Color(0f, 0f, 0f, 0f);
            });

            // 15. Wall_End_N (Ujung Dinding Menghadap Pintu Utara - Kusen Kayu Halus)
            CreateTexture("Wall_End_N.png", 64, 64, (x, y) =>
            {
                // Kusen pintu kayu di ujung utara (x: 8..53, y: 44..56)
                if (x >= 8 && x <= 53 && y >= 44 && y <= 56)
                {
                    bool isBorder = (x == 8 || x == 53 || y == 44 || y == 56);
                    if (isBorder) return new Color(0.35f, 0.22f, 0.12f, 1f);
                    bool isHi = (y >= 52 && y <= 54);
                    return isHi ? doorWoodHi : doorWood;
                }

                if (y <= 43)
                {
                    if (x <= 6) return Color.Lerp(wallTopHighlight, wallBase, x / 6f);
                    if (x <= 53) return wallBase;
                    if (x <= 59) return Color.Lerp(wallDepth, wallBaseboard, (x - 54f) / 5f);
                    return wallBaseboard;
                }

                return new Color(0f, 0f, 0f, 0f);
            });

            // 16. Wall_Ext_Top (Dinding Luar Atas - Roof Trim di atas, Cream di bawah - Invariant terhadap X)
            CreateTexture("Wall_Ext_Top.png", 64, 64, (x, y) =>
            {
                if (y >= 60) return outerRoofRidge;
                if (y >= 50) return outerRoofTrim;
                if (y >= 46) return outerRoofShadow;
                if (y >= 14) return wallBase;
                if (y >= 4) return Color.Lerp(wallBaseboard, wallDepth, (y - 4f) / 9f);
                return wallBaseboard;
            });

            // 17. Wall_Ext_Bottom (Dinding Luar Bawah - Invariant terhadap X)
            CreateTexture("Wall_Ext_Bottom.png", 64, 64, (x, y) =>
            {
                if (y >= 58) return Color.Lerp(wallBase, wallTopHighlight, (y - 58f) / 5f);
                if (y >= 16) return wallBase;
                if (y >= 13) return outerRoofRidge;
                return outerRoofTrim;
            });

            // 18. Wall_Ext_Left (Dinding Luar Kiri - Invariant terhadap Y, menyatu mulus dengan col 1)
            CreateTexture("Wall_Ext_Left.png", 64, 64, (x, y) =>
            {
                if (x <= 13) return outerRoofTrim;
                if (x <= 15) return outerRoofRidge;
                return wallBase;
            });

            // 19. Wall_Ext_Right (Dinding Luar Kanan - Invariant terhadap Y, menyatu mulus dengan col 18)
            CreateTexture("Wall_Ext_Right.png", 64, 64, (x, y) =>
            {
                if (x <= 48) return wallBase;
                if (x <= 50) return outerRoofShadow;
                if (x <= 52) return outerRoofRidge;
                return outerRoofTrim;
            });

            // 20. Wall_Ext_Corner_TL (Sudut Luar Top-Left)
            CreateTexture("Wall_Ext_Corner_TL.png", 64, 64, (x, y) =>
            {
                if (y >= 46 || x <= 15)
                {
                    if (y >= 60 || x <= 3) return outerRoofRidge;
                    if (y <= 49 && x >= 13) return outerRoofShadow;
                    return outerRoofTrim;
                }
                if (x <= 53 && y >= 14) return wallBase;
                if (x > 53 && y >= 14) return wallBase;
                if (y < 14 && x <= 53) return wallBase;
                if (y >= 4 && y < 14) return Color.Lerp(wallBaseboard, wallDepth, (y - 4f) / 9f);
                return wallBaseboard;
            });

            // 21. Wall_Ext_Corner_TR (Sudut Luar Top-Right)
            CreateTexture("Wall_Ext_Corner_TR.png", 64, 64, (x, y) =>
            {
                if (y >= 46 || x >= 48)
                {
                    if (y >= 60 || x >= 60) return outerRoofRidge;
                    if (y <= 49 && x <= 50) return outerRoofShadow;
                    return outerRoofTrim;
                }
                if (y >= 14) return wallBase;
                if (y >= 4 && y < 14) return Color.Lerp(wallBaseboard, wallDepth, (y - 4f) / 9f);
                return wallBaseboard;
            });

            // 22. Wall_Ext_Corner_BL (Sudut Luar Bottom-Left)
            CreateTexture("Wall_Ext_Corner_BL.png", 64, 64, (x, y) =>
            {
                if (y <= 15 || x <= 15)
                {
                    if (y <= 3 || x <= 3) return outerRoofRidge;
                    return outerRoofTrim;
                }
                if (y >= 58) return Color.Lerp(wallBase, wallTopHighlight, (y - 58f) / 5f);
                return wallBase;
            });

            // 23. Wall_Ext_Corner_BR (Sudut Luar Bottom-Right)
            CreateTexture("Wall_Ext_Corner_BR.png", 64, 64, (x, y) =>
            {
                if (y <= 15 || x >= 48)
                {
                    if (y <= 3 || x >= 60) return outerRoofRidge;
                    return outerRoofTrim;
                }
                if (y >= 58) return Color.Lerp(wallBase, wallTopHighlight, (y - 58f) / 5f);
                return wallBase;
            });

            // 24. Wall_Ext_Window_H (Jendela Kartun Dinding Luar Horizontal - Mulus menyatu dengan Wall_Ext_Top)
            CreateTexture("Wall_Ext_Window_H.png", 64, 64, (x, y) =>
            {
                if (y >= 60) return outerRoofRidge;
                if (y >= 50) return outerRoofTrim;
                if (y >= 46) return outerRoofShadow;

                // Kaca Jendela Biru dengan Refleksi Putih & Kusen Putih (x: 10..53, y: 16..44)
                if (x >= 10 && x <= 53 && y >= 16 && y <= 44)
                {
                    bool isGlassFrame = (x <= 13 || x >= 50 || y <= 18 || y >= 42 || x == 31 || x == 32);
                    if (isGlassFrame) return new Color(0.96f, 0.96f, 0.96f, 1f); // White window frame
                    bool isReflection = ((x - y) >= 2 && (x - y) <= 10) || ((x - y) >= -22 && (x - y) <= -16);
                    return isReflection ? new Color(0.92f, 0.97f, 1.0f, 1f) : new Color(0.38f, 0.65f, 0.88f, 1f);
                }

                if (y >= 14) return wallBase;
                if (y >= 4) return Color.Lerp(wallBaseboard, wallDepth, (y - 4f) / 9f);
                return wallBaseboard;
            });

            // 25. Wall_Solid_Roof (Atap Luar Padat / Row 0 Perimeter - Invariant terhadap X)
            CreateTexture("Wall_Solid_Roof.png", 64, 64, (x, y) =>
            {
                if (y >= 58) return outerRoofRidge;
                if (y <= 5) return outerRoofShadow;
                return outerRoofTrim;
            });

            // 26. Wall_Solid (Blok Tembok Padat Dalam)
            CreateTexture("Wall_Solid.png", 64, 64, (x, y) =>
            {
                if (y >= 58) return Color.Lerp(wallBase, wallTopHighlight, (y - 58f) / 5f);
                if (y >= 14) return wallBase;
                if (y >= 4) return Color.Lerp(wallBaseboard, wallDepth, (y - 4f) / 9f);
                return wallBaseboard;
            });

            // 27. Wall_House_Fence_H (Pagar Kayu Horizontal Halaman - Papan & Palang Seamless)
            CreateTexture("Wall_House_Fence_H.png", 64, 64, (x, y) =>
            {
                // Palang kayu horizontal atas (y: 38..44) dan bawah (y: 20..26)
                bool isRail = (y >= 20 && y <= 26) || (y >= 38 && y <= 44);
                int slatX = x % 16;
                bool isPicket = (slatX >= 5 && slatX <= 10 && y >= 12 && y <= 52);

                if (isPicket)
                {
                    if (y >= 48 && (slatX == 5 || slatX == 10) && y > (56 - Mathf.Abs(slatX - 7.5f) * 2.5f))
                        return grassColor;
                    bool isPicketEdge = (slatX == 5 || slatX == 10);
                    return isPicketEdge ? new Color(0.62f, 0.44f, 0.26f, 1f) : new Color(0.82f, 0.64f, 0.44f, 1f);
                }

                if (isRail)
                {
                    bool isRailEdge = (y == 20 || y == 26 || y == 38 || y == 44);
                    return isRailEdge ? new Color(0.58f, 0.40f, 0.24f, 1f) : new Color(0.78f, 0.58f, 0.38f, 1f);
                }

                return grassColor;
            });

            // 28. Wall_House_Fence_V (Pagar Kayu Vertikal Halaman)
            CreateTexture("Wall_House_Fence_V.png", 64, 64, (x, y) =>
            {
                bool isRail = (x >= 20 && x <= 26) || (x >= 38 && x <= 44);
                int slatY = y % 16;
                bool isPicket = (slatY >= 5 && slatY <= 10 && x >= 12 && x <= 52);

                if (isPicket)
                {
                    bool isEdge = (slatY == 5 || slatY == 10);
                    return isEdge ? new Color(0.62f, 0.44f, 0.26f, 1f) : new Color(0.82f, 0.64f, 0.44f, 1f);
                }

                if (isRail)
                {
                    bool isRailEdge = (x == 20 || x == 26 || x == 38 || x == 44);
                    return isRailEdge ? new Color(0.58f, 0.40f, 0.24f, 1f) : new Color(0.78f, 0.58f, 0.38f, 1f);
                }

                return grassColor;
            });

            // 29. Wall_House_Fence_Corner_TL (Sudut Pagar Kayu Top-Left dengan Tiang Kokoh)
            CreateTexture("Wall_House_Fence_Corner_TL.png", 64, 64, (x, y) =>
            {
                // Tiang sudut tengah (x: 20..44, y: 20..44)
                int cx = x - 32; int cy = y - 32;
                if (cx * cx + cy * cy <= 144)
                {
                    bool isBorder = (cx * cx + cy * cy >= 100);
                    return isBorder ? new Color(0.52f, 0.34f, 0.18f, 1f) : new Color(0.75f, 0.55f, 0.35f, 1f);
                }

                // Palang ke East (x > 32, y: 24..40)
                if (x > 32 && y >= 24 && y <= 40) return new Color(0.78f, 0.58f, 0.38f, 1f);
                // Palang ke South (y < 32, x: 24..40)
                if (y < 32 && x >= 24 && x <= 40) return new Color(0.78f, 0.58f, 0.38f, 1f);

                return grassColor;
            });

            // 30. Wall_House_Fence_Corner_BL (Sudut Pagar Kayu Bottom-Left)
            CreateTexture("Wall_House_Fence_Corner_BL.png", 64, 64, (x, y) =>
            {
                int cx = x - 32; int cy = y - 32;
                if (cx * cx + cy * cy <= 144)
                {
                    bool isBorder = (cx * cx + cy * cy >= 100);
                    return isBorder ? new Color(0.52f, 0.34f, 0.18f, 1f) : new Color(0.75f, 0.55f, 0.35f, 1f);
                }

                if (x > 32 && y >= 24 && y <= 40) return new Color(0.78f, 0.58f, 0.38f, 1f);
                if (y > 32 && x >= 24 && x <= 40) return new Color(0.78f, 0.58f, 0.38f, 1f);

                return grassColor;
            });

            // 31. Wall_House_Fence_Corner_BR (Sudut Pagar Kayu Bottom-Right)
            CreateTexture("Wall_House_Fence_Corner_BR.png", 64, 64, (x, y) =>
            {
                int cx = x - 32; int cy = y - 32;
                if (cx * cx + cy * cy <= 144)
                {
                    bool isBorder = (cx * cx + cy * cy >= 100);
                    return isBorder ? new Color(0.52f, 0.34f, 0.18f, 1f) : new Color(0.75f, 0.55f, 0.35f, 1f);
                }

                if (x < 32 && y >= 24 && y <= 40) return new Color(0.78f, 0.58f, 0.38f, 1f);
                if (y > 32 && x >= 24 && x <= 40) return new Color(0.78f, 0.58f, 0.38f, 1f);

                return grassColor;
            });

            // 32. Deco_Doorway_H (Threshold / Ambang Pintu Kayu Horizontal Antar Ruangan)
            CreateTexture("Deco_Doorway_H.png", 64, 64, (x, y) =>
            {
                if (y < 20 || y > 44) return new Color(0f, 0f, 0f, 0f);
                bool isBorder = (y == 20 || y == 44);
                if (isBorder) return new Color(0.38f, 0.24f, 0.14f, 1f);
                bool isHighlight = (y == 42 || y == 43);
                return isHighlight ? new Color(0.72f, 0.52f, 0.32f, 1f) : new Color(0.58f, 0.40f, 0.24f, 1f);
            });

            // 33. Deco_Doorway_V (Threshold / Ambang Pintu Kayu Vertikal Antar Ruangan)
            CreateTexture("Deco_Doorway_V.png", 64, 64, (x, y) =>
            {
                if (x < 20 || x > 44) return new Color(0f, 0f, 0f, 0f);
                bool isBorder = (x == 20 || x == 44);
                if (isBorder) return new Color(0.38f, 0.24f, 0.14f, 1f);
                bool isHighlight = (x == 21 || x == 22);
                return isHighlight ? new Color(0.72f, 0.52f, 0.32f, 1f) : new Color(0.58f, 0.40f, 0.24f, 1f);
            });
        }

        #endregion

        #region Rug & Carpet Sprites

        private static void GenerateRugs()
        {
            // 1. Deco_Rug_Ornate_Red_Large (128x128 - Karpet Merah Megah dengan Motif Emas & Rumbai)
            CreateTexture("Deco_Rug_Ornate_Red_Large.png", 128, 128, (x, y) =>
            {
                if (x < 6 || x > 121 || y < 6 || y > 121) return Color.clear;

                // Rumbai / fringe di atas & bawah
                if (y <= 9 || y >= 118)
                {
                    bool isFringe = (x % 4 == 0 || x % 4 == 1);
                    return isFringe ? new Color(0.95f, 0.88f, 0.65f, 1f) : new Color(0.75f, 0.65f, 0.45f, 1f);
                }

                // Border Emas Dekoratif (tebal 8px)
                bool isOuterGold = (x <= 16 || x >= 111 || y <= 19 || y >= 108);
                if (isOuterGold)
                {
                    bool isGoldPattern = ((x + y) % 8 == 0 || (x - y) % 8 == 0);
                    return isGoldPattern ? new Color(0.95f, 0.82f, 0.28f, 1f) : new Color(0.72f, 0.18f, 0.18f, 1f);
                }

                // Inner Gold Trim Line
                bool isInnerTrim = (x == 20 || x == 107 || y == 23 || y == 104);
                if (isInnerTrim) return new Color(0.95f, 0.82f, 0.28f, 1f);

                // Motif Medali Emas di Tengah
                int cx = x - 64; int cy = y - 64;
                float centerDist = Mathf.Sqrt(cx * cx + cy * cy);
                if (centerDist <= 18f)
                {
                    if (centerDist >= 15f || centerDist <= 4f) return new Color(0.95f, 0.82f, 0.28f, 1f);
                    float angle = Mathf.Atan2(cy, cx);
                    bool flower = (Mathf.Sin(angle * 8f) > 0f);
                    return flower ? new Color(0.95f, 0.82f, 0.28f, 1f) : new Color(0.68f, 0.16f, 0.16f, 1f);
                }

                // Red velvet damask field
                float grain = Mathf.PerlinNoise(x * 0.2f, y * 0.2f) * 0.04f;
                return new Color(0.72f - grain, 0.18f - grain, 0.18f - grain, 1f);
            });

            // 2. Deco_Rug_Blue_Oval (96x96 - Karpet Oval Biru Lembut)
            CreateTexture("Deco_Rug_Blue_Oval.png", 96, 96, (x, y) =>
            {
                float cx = (x - 48f) / 44f;
                float cy = (y - 48f) / 38f;
                float dist = cx * cx + cy * cy;
                if (dist > 1f) return Color.clear;
                if (dist >= 0.85f) return new Color(0.85f, 0.90f, 0.96f, 1f); // White/cream border
                if (dist >= 0.75f) return new Color(0.25f, 0.42f, 0.65f, 1f); // Dark blue accent

                float grain = Mathf.PerlinNoise(x * 0.25f, y * 0.25f) * 0.04f;
                return new Color(0.38f - grain, 0.55f - grain, 0.78f - grain, 1f);
            });

            // 3. Deco_Rug_Gold_Large (96x96 - Karpet Emas/Krem Ruang Makan)
            CreateTexture("Deco_Rug_Gold_Large.png", 96, 96, (x, y) =>
            {
                if (x < 6 || x > 89 || y < 6 || y > 89) return Color.clear;
                bool isBorder = (x <= 12 || x >= 83 || y <= 12 || y >= 83);
                if (isBorder) return new Color(0.55f, 0.42f, 0.22f, 1f);
                bool isInnerBorder = (x == 15 || x == 80 || y == 15 || y == 80);
                if (isInnerBorder) return new Color(0.88f, 0.75f, 0.38f, 1f);

                float grain = Mathf.PerlinNoise(x * 0.2f, y * 0.2f) * 0.04f;
                return new Color(0.82f - grain, 0.74f - grain, 0.48f - grain, 1f);
            });
        }

        #endregion

        #region Furniture & Environment Sprites

        private static void GenerateFurnitureSprites()
        {
            // 1. Deco_Bed_Red (64x64 - Double Bed Kasur Merah Berkualitas)
            CreateTexture("Deco_Bed_Red.png", 64, 64, (x, y) =>
            {
                if (x < 6 || x > 57 || y < 4 || y > 59) return Color.clear;

                // Headboard kayu atas (y: 54..59)
                if (y >= 54) return new Color(0.48f, 0.30f, 0.18f, 1f);

                // Dua Bantal Putih Empuk (y: 44..52, x: 10..29 dan x: 34..53)
                if (y >= 44 && y <= 52)
                {
                    if ((x >= 10 && x <= 29) || (x >= 34 && x <= 53))
                    {
                        bool isPillowEdge = (y == 44 || x == 10 || x == 29 || x == 34 || x == 53);
                        return isPillowEdge ? new Color(0.88f, 0.88f, 0.90f, 1f) : new Color(0.98f, 0.98f, 0.99f, 1f);
                    }
                }

                // Lipatan Sprei Putih Atas (y: 38..43)
                if (y >= 38 && y <= 43 && x >= 8 && x <= 55)
                {
                    return new Color(0.95f, 0.94f, 0.92f, 1f);
                }

                // Selimut Merah Mengkilap dengan Bayangan Lipatan (y: 6..37)
                if (y >= 6 && y <= 37 && x >= 8 && x <= 55)
                {
                    bool isBorder = (x <= 10 || x >= 53 || y <= 8);
                    if (isBorder) return new Color(0.62f, 0.16f, 0.16f, 1f);
                    float fold = (y % 10 == 0) ? 0.05f : 0f;
                    return new Color(0.78f + fold, 0.22f + fold, 0.22f + fold, 1f);
                }

                // Footboard & Side Rails Kayu
                return new Color(0.55f, 0.35f, 0.20f, 1f);
            });

            // 2. Deco_Bed_Blue (64x64 - Double Bed Kasur Biru)
            CreateTexture("Deco_Bed_Blue.png", 64, 64, (x, y) =>
            {
                if (x < 6 || x > 57 || y < 4 || y > 59) return Color.clear;
                if (y >= 54) return new Color(0.48f, 0.30f, 0.18f, 1f);

                if (y >= 44 && y <= 52)
                {
                    if ((x >= 10 && x <= 29) || (x >= 34 && x <= 53))
                    {
                        bool isPillowEdge = (y == 44 || x == 10 || x == 29 || x == 34 || x == 53);
                        return isPillowEdge ? new Color(0.88f, 0.88f, 0.90f, 1f) : new Color(0.98f, 0.98f, 0.99f, 1f);
                    }
                }

                if (y >= 38 && y <= 43 && x >= 8 && x <= 55) return new Color(0.95f, 0.94f, 0.92f, 1f);

                if (y >= 6 && y <= 37 && x >= 8 && x <= 55)
                {
                    bool isBorder = (x <= 10 || x >= 53 || y <= 8);
                    if (isBorder) return new Color(0.20f, 0.35f, 0.55f, 1f);
                    float fold = (y % 10 == 0) ? 0.05f : 0f;
                    return new Color(0.32f + fold, 0.52f + fold, 0.78f + fold, 1f);
                }

                return new Color(0.55f, 0.35f, 0.20f, 1f);
            });

            // 3. Deco_Bed_Brown (64x64 - Double Bed Kasur Krem/Emas)
            CreateTexture("Deco_Bed_Brown.png", 64, 64, (x, y) =>
            {
                if (x < 6 || x > 57 || y < 4 || y > 59) return Color.clear;
                if (y >= 54) return new Color(0.45f, 0.28f, 0.16f, 1f);

                if (y >= 44 && y <= 52 && ((x >= 10 && x <= 29) || (x >= 34 && x <= 53)))
                {
                    return new Color(0.98f, 0.98f, 0.99f, 1f);
                }

                if (y >= 38 && y <= 43 && x >= 8 && x <= 55) return new Color(0.95f, 0.92f, 0.85f, 1f);

                if (y >= 6 && y <= 37 && x >= 8 && x <= 55)
                {
                    bool isBorder = (x <= 10 || x >= 53 || y <= 8);
                    if (isBorder) return new Color(0.58f, 0.45f, 0.25f, 1f);
                    return new Color(0.82f, 0.72f, 0.48f, 1f);
                }

                return new Color(0.50f, 0.32f, 0.18f, 1f);
            });

            // 4. Deco_Nightstand (32x32 - Meja Nakas Kayu + Lampu Tidur Menyala)
            CreateTexture("Deco_Nightstand.png", 32, 32, (x, y) =>
            {
                if (x < 4 || x > 27 || y < 4 || y > 27) return Color.clear;
                bool isBorder = (x <= 5 || x >= 26 || y <= 5 || y >= 26);
                if (isBorder) return new Color(0.48f, 0.32f, 0.18f, 1f);

                // Lampu bulat menyala di tengah
                int cx = x - 16; int cy = y - 16;
                if (cx * cx + cy * cy <= 16) return new Color(0.99f, 0.90f, 0.35f, 1f); // Lamp glow
                if (cx * cx + cy * cy <= 25) return new Color(0.85f, 0.72f, 0.25f, 1f); // Lamp base

                return new Color(0.70f, 0.50f, 0.32f, 1f); // Wood top
            });

            // 5. Deco_Wardrobe (64x32 - Lemari Pakaian Kayu Solid)
            CreateTexture("Deco_Wardrobe.png", 64, 32, (x, y) =>
            {
                if (x < 4 || x > 59 || y < 4 || y > 27) return Color.clear;
                bool isBorder = (x <= 6 || x >= 57 || y <= 6 || y >= 25);
                if (isBorder) return new Color(0.42f, 0.26f, 0.15f, 1f);
                bool isCenterLine = (x == 31 || x == 32);
                if (isCenterLine) return new Color(0.42f, 0.26f, 0.15f, 1f);

                // Gagang pintu emas
                if ((x == 27 || x == 36) && y >= 13 && y <= 18) return new Color(0.95f, 0.82f, 0.25f, 1f);

                return new Color(0.62f, 0.42f, 0.26f, 1f);
            });

            // 6. Deco_TV_Unit (64x32 - Meja TV + Televisi Flat Screen Biru)
            CreateTexture("Deco_TV_Unit.png", 64, 32, (x, y) =>
            {
                if (x < 4 || x > 59 || y < 4 || y > 27) return Color.clear;

                // Layar Flat TV (x: 10..53, y: 14..24)
                if (x >= 10 && x <= 53 && y >= 14 && y <= 24)
                {
                    bool isBezel = (x <= 12 || x >= 51 || y <= 15 || y >= 23);
                    if (isBezel) return new Color(0.12f, 0.12f, 0.14f, 1f);
                    // Blue TV screen glow
                    bool isGlow = ((x - y) % 12 <= 3);
                    return isGlow ? new Color(0.42f, 0.68f, 0.95f, 1f) : new Color(0.22f, 0.42f, 0.70f, 1f);
                }

                // Kaki TV
                if (x >= 28 && x <= 35 && y >= 10 && y <= 13) return new Color(0.15f, 0.15f, 0.18f, 1f);

                // Meja TV kayu
                bool isTableBorder = (x <= 6 || x >= 57 || y <= 6 || y >= 25);
                if (isTableBorder) return new Color(0.45f, 0.30f, 0.18f, 1f);
                return new Color(0.65f, 0.46f, 0.30f, 1f);
            });

            // 7. Deco_Sofa_Red (64x48 - Sofa Empuk Merah 3 Dudukan)
            CreateTexture("Deco_Sofa_Red.png", 64, 48, (x, y) =>
            {
                if (x < 4 || x > 59 || y < 4 || y > 43) return Color.clear;

                // Sandaran belakang empuk melengkung (y: 32..43)
                if (y >= 32)
                {
                    bool isBackEdge = (y == 43 || y == 32);
                    return isBackEdge ? new Color(0.55f, 0.14f, 0.14f, 1f) : new Color(0.75f, 0.22f, 0.22f, 1f);
                }

                // Sandaran Tangan Kiri & Kanan (x: 4..14 dan x: 49..59)
                if (x <= 14 || x >= 49)
                {
                    bool isArmEdge = (x == 4 || x == 14 || x == 49 || x == 59 || y == 4 || y == 31);
                    return isArmEdge ? new Color(0.55f, 0.14f, 0.14f, 1f) : new Color(0.72f, 0.20f, 0.20f, 1f);
                }

                // 3 Bantal Dudukan (x: 15..48, y: 5..31)
                int cushionIdx = (x - 15) / 11;
                int cx = (x - 15) % 11;
                bool isCushionSeam = (cx == 0 || y == 5 || y == 31);
                if (isCushionSeam) return new Color(0.50f, 0.12f, 0.12f, 1f);

                return new Color(0.85f, 0.26f, 0.26f, 1f);
            });

            // 8. Deco_Sofa_Blue (64x48 - Sofa Biru)
            CreateTexture("Deco_Sofa_Blue.png", 64, 48, (x, y) =>
            {
                if (x < 4 || x > 59 || y < 4 || y > 43) return Color.clear;
                if (y >= 32) return (y == 43 || y == 32) ? new Color(0.18f, 0.32f, 0.52f, 1f) : new Color(0.28f, 0.46f, 0.72f, 1f);
                if (x <= 14 || x >= 49) return new Color(0.25f, 0.42f, 0.68f, 1f);

                int cx = (x - 15) % 11;
                if (cx == 0 || y == 5 || y == 31) return new Color(0.16f, 0.28f, 0.48f, 1f);
                return new Color(0.38f, 0.58f, 0.88f, 1f);
            });

            // 9. Deco_Sofa_Green (64x48 - Sofa Hijau Sage)
            CreateTexture("Deco_Sofa_Green.png", 64, 48, (x, y) =>
            {
                if (x < 4 || x > 59 || y < 4 || y > 43) return Color.clear;
                if (y >= 32) return (y == 43 || y == 32) ? new Color(0.22f, 0.38f, 0.18f, 1f) : new Color(0.35f, 0.55f, 0.28f, 1f);
                if (x <= 14 || x >= 49) return new Color(0.30f, 0.50f, 0.25f, 1f);

                int cx = (x - 15) % 11;
                if (cx == 0 || y == 5 || y == 31) return new Color(0.18f, 0.32f, 0.15f, 1f);
                return new Color(0.44f, 0.68f, 0.36f, 1f);
            });

            // 10. Deco_Armchair (32x32 - Kursi Single Armchair Merah)
            CreateTexture("Deco_Armchair.png", 32, 32, (x, y) =>
            {
                if (x < 4 || x > 27 || y < 4 || y > 27) return Color.clear;
                bool isBack = (y >= 20);
                bool isArm = (x <= 8 || x >= 23);
                if (isBack || isArm) return new Color(0.62f, 0.18f, 0.18f, 1f);
                return new Color(0.82f, 0.28f, 0.28f, 1f);
            });

            // 11. Deco_CoffeeTable (48x32 - Meja Kopi Ruang Tamu)
            CreateTexture("Deco_CoffeeTable.png", 48, 32, (x, y) =>
            {
                if (x < 4 || x > 43 || y < 4 || y > 27) return Color.clear;
                bool isBorder = (x <= 6 || x >= 41 || y <= 6 || y >= 25);
                if (isBorder) return new Color(0.48f, 0.32f, 0.18f, 1f);

                // Majalah & Cangkir Kopi di atas meja
                if (x >= 20 && x <= 28 && y >= 11 && y <= 20) return new Color(0.95f, 0.92f, 0.85f, 1f); // Magazine
                if (x >= 32 && x <= 36 && y >= 13 && y <= 17) return new Color(0.85f, 0.25f, 0.25f, 1f); // Red cup

                return new Color(0.72f, 0.52f, 0.34f, 1f);
            });

            // 12. Deco_Table_Dining (64x64 - Meja Makan Lengkap dengan 4 Kursi Rapi)
            CreateTexture("Deco_Table_Dining.png", 64, 64, (x, y) =>
            {
                // Kursi Atas (y: 52..60, x: 20..43)
                if (y >= 52 && y <= 60 && x >= 20 && x <= 43)
                {
                    bool isBack = (y >= 57);
                    return isBack ? new Color(0.48f, 0.30f, 0.16f, 1f) : new Color(0.85f, 0.30f, 0.30f, 1f); // Red cushion
                }

                // Kursi Bawah (y: 3..11, x: 20..43)
                if (y >= 3 && y <= 11 && x >= 20 && x <= 43)
                {
                    bool isBack = (y <= 6);
                    return isBack ? new Color(0.48f, 0.30f, 0.16f, 1f) : new Color(0.85f, 0.30f, 0.30f, 1f);
                }

                // Kursi Kiri (x: 3..11, y: 20..43)
                if (x >= 3 && x <= 11 && y >= 20 && y <= 43)
                {
                    bool isBack = (x <= 6);
                    return isBack ? new Color(0.48f, 0.30f, 0.16f, 1f) : new Color(0.85f, 0.30f, 0.30f, 1f);
                }

                // Kursi Kanan (x: 52..60, y: 20..43)
                if (x >= 52 && x <= 60 && y >= 20 && y <= 43)
                {
                    bool isBack = (x >= 57);
                    return isBack ? new Color(0.48f, 0.30f, 0.16f, 1f) : new Color(0.85f, 0.30f, 0.30f, 1f);
                }

                // Meja Kayu Utama di Tengah (x: 14..49, y: 14..49)
                if (x >= 14 && x <= 49 && y >= 14 && y <= 49)
                {
                    bool isBorder = (x <= 16 || x >= 47 || y <= 16 || y >= 47);
                    if (isBorder) return new Color(0.50f, 0.32f, 0.18f, 1f);

                    // Taplak meja putih di tengah (x: 22..41, y: 17..46)
                    if (x >= 22 && x <= 41 && y >= 17 && y <= 46) return new Color(0.96f, 0.95f, 0.92f, 1f);

                    return new Color(0.74f, 0.54f, 0.35f, 1f);
                }

                return Color.clear;
            });

            // 13. Deco_Kitchen_Counter (64x32 - Counter Dapur + Kompor Gas 2 Tungku + Sink)
            CreateTexture("Deco_Kitchen_Counter.png", 64, 32, (x, y) =>
            {
                if (x < 4 || x > 59 || y < 4 || y > 27) return Color.clear;
                bool isBorder = (x <= 5 || x >= 58 || y <= 5 || y >= 26);
                if (isBorder) return new Color(0.35f, 0.35f, 0.38f, 1f);

                // Sink cuci piring (kiri, x: 8..24, y: 8..23)
                if (x >= 8 && x <= 24 && y >= 8 && y <= 23)
                {
                    bool isSinkRim = (x == 8 || x == 24 || y == 8 || y == 23);
                    if (isSinkRim) return new Color(0.65f, 0.68f, 0.72f, 1f);
                    // Keran air
                    if (x >= 15 && x <= 17 && y >= 19 && y <= 22) return new Color(0.85f, 0.88f, 0.92f, 1f);
                    return new Color(0.45f, 0.72f, 0.88f, 1f); // Water in sink
                }

                // Kompor Gas 2 Tungku (kanan, x: 36..54, y: 8..23)
                if (x >= 36 && x <= 54 && y >= 8 && y <= 23)
                {
                    int c1x = x - 41; int c1y = y - 15;
                    int c2x = x - 49; int c2y = y - 15;
                    if (c1x * c1x + c1y * c1y <= 12 || c2x * c2x + c2y * c2y <= 12)
                    {
                        if (c1x * c1x + c1y * c1y <= 3 || c2x * c2x + c2y * c2y <= 3) return new Color(0.95f, 0.50f, 0.15f, 1f); // Flame/igniter
                        return new Color(0.12f, 0.12f, 0.15f, 1f); // Burner ring
                    }
                    return new Color(0.25f, 0.25f, 0.28f, 1f); // Black stove plate
                }

                // Granit Putih Bersih
                return new Color(0.88f, 0.90f, 0.92f, 1f);
            });

            // 14. Deco_Kitchen_Fridge (32x48 - Kulkas 2 Pintu Stainless Steel)
            CreateTexture("Deco_Kitchen_Fridge.png", 32, 48, (x, y) =>
            {
                if (x < 4 || x > 27 || y < 4 || y > 43) return Color.clear;
                bool isBorder = (x <= 5 || x >= 26 || y <= 5 || y >= 42);
                if (isBorder) return new Color(0.65f, 0.70f, 0.75f, 1f);
                bool isDoorSplit = (y == 28 || y == 29);
                if (isDoorSplit) return new Color(0.50f, 0.55f, 0.60f, 1f);

                // Gagang pintu vertikal
                if ((x >= 22 && x <= 24) && ((y >= 32 && y <= 38) || (y >= 14 && y <= 20)))
                {
                    return new Color(0.35f, 0.40f, 0.45f, 1f);
                }

                return new Color(0.92f, 0.94f, 0.96f, 1f);
            });

            // 15. Deco_Bathroom_Tub (64x64 - Bathtub Putih + Air Mandi Biru)
            CreateTexture("Deco_Bathroom_Tub.png", 64, 64, (x, y) =>
            {
                if (x < 6 || x > 57 || y < 10 || y > 53) return Color.clear;
                bool isOuter = (x <= 10 || x >= 53 || y <= 14 || y >= 49);
                if (isOuter) return new Color(0.95f, 0.96f, 0.98f, 1f); // White porcelain rim

                // Keran air
                if (x >= 11 && x <= 14 && y >= 29 && y <= 34) return new Color(0.75f, 0.80f, 0.85f, 1f);

                // Air mandi biru berkilau
                float wave = Mathf.Sin((x + y) * 0.4f) * 0.03f;
                return new Color(0.42f + wave, 0.74f + wave, 0.92f + wave, 1f);
            });

            // 16. Deco_Bathroom_Toilet (48x48 - Kloset Duduk Porselen)
            CreateTexture("Deco_Bathroom_Toilet.png", 48, 48, (x, y) =>
            {
                if (x < 10 || x > 37 || y < 4 || y > 43) return Color.clear;
                // Tangki air di atas
                if (y >= 32)
                {
                    bool isTankBorder = (x <= 12 || x >= 35 || y == 43 || y == 32);
                    if (isTankBorder) return new Color(0.85f, 0.88f, 0.92f, 1f);
                    // Flush button
                    if (x >= 22 && x <= 25 && y >= 36 && y <= 39) return new Color(0.75f, 0.80f, 0.85f, 1f);
                    return new Color(0.95f, 0.96f, 0.98f, 1f);
                }

                // Dudukan oval
                int cx = x - 24; int cy = y - 18;
                if (cx * cx + cy * cy <= 125)
                {
                    if (cx * cx + cy * cy <= 45) return new Color(0.38f, 0.68f, 0.88f, 1f); // Water
                    return new Color(0.98f, 0.98f, 0.99f, 1f); // Porcelain bowl
                }
                return Color.clear;
            });

            // 17. Deco_Bathroom_Sink (40x40 - Wastafel Kamar Mandi)
            CreateTexture("Deco_Bathroom_Sink.png", 40, 40, (x, y) =>
            {
                if (x < 6 || x > 33 || y < 6 || y > 33) return Color.clear;
                bool isEdge = (x <= 8 || x >= 31 || y <= 8 || y >= 31);
                if (isEdge) return new Color(0.92f, 0.94f, 0.96f, 1f);

                int cx = x - 20; int cy = y - 20;
                if (cx * cx + cy * cy <= 50)
                {
                    if (cx * cx + cy * cy <= 8) return new Color(0.65f, 0.70f, 0.75f, 1f); // Drain
                    return new Color(0.48f, 0.75f, 0.92f, 1f); // Water
                }
                return new Color(0.97f, 0.98f, 0.99f, 1f);
            });

            // 18. Deco_Bathroom_Mat (48x32 - Keset Biru Lembut)
            CreateTexture("Deco_Bathroom_Mat.png", 48, 32, (x, y) =>
            {
                if (x < 4 || x > 43 || y < 4 || y > 27) return Color.clear;
                float noise = Mathf.PerlinNoise(x * 0.35f, y * 0.35f) * 0.05f;
                bool isStripe = (x % 6 == 0);
                float str = isStripe ? 0.04f : 0f;
                return new Color(0.38f + str - noise, 0.62f + str - noise, 0.82f + str - noise, 1f);
            });

            // 19. Deco_Cardboard_Box (48x48 - Kardus Ngumpet)
            CreateTexture("Deco_Cardboard_Box.png", 48, 48, (x, y) =>
            {
                if (x < 4 || x > 43 || y < 4 || y > 43) return Color.clear;
                bool isBorder = (x <= 6 || x >= 41 || y <= 6 || y >= 41);
                if (isBorder) return new Color(0.52f, 0.36f, 0.20f, 1f);

                // Selotip packing cokelat muda di tengah
                bool isTapeH = (y >= 22 && y <= 25);
                bool isTapeV = (x >= 22 && x <= 25);
                if (isTapeH || isTapeV) return new Color(0.88f, 0.75f, 0.48f, 1f);

                // Stiker panah/fragile
                if (x >= 10 && x <= 16 && y >= 30 && y <= 36) return new Color(0.92f, 0.90f, 0.82f, 1f);

                float grain = Mathf.PerlinNoise(x * 0.25f, y * 0.25f) * 0.04f;
                return new Color(0.74f - grain, 0.54f - grain, 0.34f - grain, 1f);
            });

            // 20. Deco_Cardboard_Box_Hiding (48x48 - Kardus saat Player Sembunyi / Mata Mengintip)
            CreateTexture("Deco_Cardboard_Box_Hiding.png", 48, 48, (x, y) =>
            {
                if (x < 4 || x > 43 || y < 4 || y > 43) return Color.clear;
                bool isBorder = (x <= 6 || x >= 41 || y <= 6 || y >= 41);
                if (isBorder) return new Color(0.52f, 0.36f, 0.20f, 1f);

                // Celah gelap mengintip (y: 20..27, x: 13..34)
                if (y >= 20 && y <= 27 && x >= 13 && x <= 34)
                {
                    // Mata kiri
                    if (x >= 17 && x <= 20 && y >= 22 && y <= 25) return Color.white;
                    if (x >= 18 && x <= 19 && y >= 23 && y <= 24) return new Color(0.2f, 0.6f, 0.9f, 1f);
                    // Mata kanan
                    if (x >= 27 && x <= 30 && y >= 22 && y <= 25) return Color.white;
                    if (x >= 28 && x <= 29 && y >= 23 && y <= 24) return new Color(0.2f, 0.6f, 0.9f, 1f);
                    return new Color(0.12f, 0.12f, 0.15f, 1f);
                }

                float grain = Mathf.PerlinNoise(x * 0.25f, y * 0.25f) * 0.04f;
                return new Color(0.74f - grain, 0.54f - grain, 0.34f - grain, 1f);
            });

            // 21. Deco_Garden_Plant (48x48 - Tanaman Pot Hias Indoor / Outdoor)
            CreateTexture("Deco_Garden_Plant.png", 48, 48, (x, y) =>
            {
                int cx = x - 24; int cy = y - 24;
                float dist = Mathf.Sqrt(cx * cx + cy * cy);
                if (dist > 20f) return Color.clear;

                // Pot terracotta bulat di tengah
                if (dist <= 7f) return new Color(0.85f, 0.45f, 0.20f, 1f);

                // Daun-daun hijau melingkar
                float angle = Mathf.Atan2(cy, cx);
                float leaves = Mathf.Sin(angle * 6f);
                float highlight = (leaves > 0.3f) ? 0.08f : -0.04f;

                return new Color(0.28f + highlight, 0.68f + highlight, 0.22f + highlight, 1f);
            });

            // 22. Deco_Tree_Large (64x64 - Pohon Rindang Halaman)
            CreateTexture("Deco_Tree_Large.png", 64, 64, (x, y) =>
            {
                int cx = x - 32; int cy = y - 32;
                float dist = Mathf.Sqrt(cx * cx + cy * cy);
                if (dist > 28f) return Color.clear;

                // Canopy clouds / foliage clusters
                float angle = Mathf.Atan2(cy, cx);
                float bump = Mathf.Sin(angle * 7f) * 3.5f;
                if (dist > 25f + bump) return Color.clear;

                bool isOuterShadow = (dist >= 22f + bump);
                if (isOuterShadow) return new Color(0.18f, 0.45f, 0.15f, 1f);

                // Top highlight
                float topLight = (cy > 0) ? 0.08f : -0.05f;
                return new Color(0.28f + topLight, 0.65f + topLight, 0.20f + topLight, 1f);
            });

            // 23. Deco_Garden_Bench (64x32 - Bangku Taman Kayu)
            CreateTexture("Deco_Garden_Bench.png", 64, 32, (x, y) =>
            {
                if (x < 6 || x > 57 || y < 6 || y > 25) return Color.clear;
                // Kaki bangku besi hitam di samping
                if (x <= 10 || x >= 53) return new Color(0.22f, 0.22f, 0.25f, 1f);

                // Bilah kayu horizontal bangku
                bool isSlatSeam = (y % 5 == 0);
                if (isSlatSeam) return new Color(0.48f, 0.30f, 0.16f, 1f);

                return new Color(0.72f, 0.48f, 0.28f, 1f);
            });

            // 24. Deco_Patio_Umbrella (64x64 - Payung Taman Meja Teras untuk Level 3)
            CreateTexture("Deco_Patio_Umbrella.png", 64, 64, (x, y) =>
            {
                int cx = x - 32; int cy = y - 32;
                float dist = Mathf.Sqrt(cx * cx + cy * cy);
                if (dist > 28f) return Color.clear;
                if (dist >= 26f) return new Color(0.68f, 0.58f, 0.42f, 1f); // Rim

                // 8 Segmen payung belang krem dan kuning
                float angle = Mathf.Atan2(cy, cx) * Mathf.Rad2Deg;
                if (angle < 0) angle += 360f;
                int segment = (int)(angle / 45f);
                bool isStripe = (segment % 2 == 0);

                if (dist <= 4f) return new Color(0.85f, 0.75f, 0.35f, 1f); // Center cap
                return isStripe ? new Color(0.96f, 0.92f, 0.82f, 1f) : new Color(0.95f, 0.78f, 0.28f, 1f);
            });

            // 25. Deco_Car_Blue (64x64 - Mobil Biru di Carport Level 4)
            CreateTexture("Deco_Car_Blue.png", 64, 64, (x, y) =>
            {
                if (x < 12 || x > 51 || y < 4 || y > 59) return Color.clear;

                // Kaca depan & belakang
                if ((y >= 38 && y <= 45) || (y >= 15 && y <= 21)) return new Color(0.20f, 0.35f, 0.55f, 1f);

                // Atap mobil tengah
                if (y >= 22 && y <= 37 && x >= 15 && x <= 48) return new Color(0.28f, 0.52f, 0.82f, 1f);

                // Bodi mobil
                bool isBorder = (x <= 14 || x >= 49 || y <= 6 || y >= 57);
                if (isBorder) return new Color(0.15f, 0.30f, 0.58f, 1f);

                // Lampu depan kuning (y: 54..58, x: 15..20 dan x: 43..48)
                if (y >= 54 && ((x >= 15 && x <= 20) || (x >= 43 && x <= 48))) return new Color(0.98f, 0.92f, 0.35f, 1f);

                // Lampu belakang merah
                if (y <= 6 && ((x >= 15 && x <= 20) || (x >= 43 && x <= 48))) return new Color(0.92f, 0.20f, 0.20f, 1f);

                return new Color(0.22f, 0.44f, 0.74f, 1f);
            });

            // 26. Deco_Car_Dark (64x64 - Mobil Hitam Mewah Mansion Level 5)
            CreateTexture("Deco_Car_Dark.png", 64, 64, (x, y) =>
            {
                if (x < 12 || x > 51 || y < 4 || y > 59) return Color.clear;
                if ((y >= 38 && y <= 45) || (y >= 15 && y <= 21)) return new Color(0.18f, 0.22f, 0.28f, 1f);
                if (y >= 22 && y <= 37 && x >= 15 && x <= 48) return new Color(0.28f, 0.30f, 0.35f, 1f);

                bool isBorder = (x <= 14 || x >= 49 || y <= 6 || y >= 57);
                if (isBorder) return new Color(0.12f, 0.14f, 0.16f, 1f);

                if (y >= 54 && ((x >= 15 && x <= 20) || (x >= 43 && x <= 48))) return new Color(0.98f, 0.92f, 0.35f, 1f);
                if (y <= 6 && ((x >= 15 && x <= 20) || (x >= 43 && x <= 48))) return new Color(0.92f, 0.20f, 0.20f, 1f);

                return new Color(0.20f, 0.22f, 0.25f, 1f);
            });
        }

        #endregion

        #region Vision Cone & Exit Marker

        private static void GenerateVisionConeAndExit()
        {
            // Vision_Cone_Red (128x128 - Kerucut Penglihatan Musuh Merah Transparan)
            CreateTexture("Vision_Cone_Red.png", 128, 128, (x, y) =>
            {
                float nx = (x - 64f) / 64f;
                float ny = (128f - y) / 128f;

                float dist = Mathf.Sqrt(nx * nx + ny * ny);
                if (dist > 0.98f || ny < 0.05f) return Color.clear;

                float angle = Mathf.Abs(Mathf.Atan2(nx, ny) * Mathf.Rad2Deg);
                if (angle > 36f) return Color.clear;

                float alpha = (1f - dist) * (1f - (angle / 36f)) * 0.45f;
                return new Color(0.95f, 0.2f, 0.2f, Mathf.Clamp01(alpha));
            });

            // Deco_Exit_Door (64x64 - Pintu Keluar Rumah dengan Panah Hijau & Badge EXIT)
            CreateTexture("Deco_Exit_Door.png", 64, 64, (x, y) =>
            {
                // Background keset hijau dengan border
                bool isBorder = (x <= 4 || x >= 59 || y <= 4 || y >= 59);
                if (isBorder) return new Color(0.12f, 0.55f, 0.28f, 1f); // Dark green border

                int cx = x - 32;
                int cy = y - 32;

                // Lingkaran hijau cerah di tengah
                if (cx * cx + cy * cy <= 450)
                {
                    // Panah ke bawah hijau terang (x: 24..40, y: 16..46)
                    bool inStem = (Mathf.Abs(cx) <= 5 && cy >= -6 && cy <= 16);
                    bool inHead = (cy < -6 && cy >= -20 && Mathf.Abs(cx) <= (cy + 20) * 0.85f);
                    if (inStem || inHead) return new Color(0.98f, 0.98f, 0.98f, 1f); // White arrow
                    return new Color(0.20f, 0.85f, 0.38f, 1f); // Bright green circle
                }

                return new Color(0.15f, 0.45f, 0.25f, 0.95f); // Green doormat
            });
        }

        #endregion

        #region Tile Creation

        private static void CreateTileAssets()
        {
            string[] floorNames = new string[]
            {
                "Floor_Wood_Warm", "Floor_Brown_Wood", "Floor_Blue_Bathroom",
                "Floor_Corridor_Tile", "Floor_Yard_Grass", "Floor_Stone_Path",
                "Floor_Carport_Dark", "Floor_Water_Pond"
            };

            foreach (var name in floorNames)
            {
                string spritePath = $"{SpritesDir}/{name}.png";
                string tilePath = $"{TilemapsDir}/{name}_Tile.asset";
                CreateTileFromSprite(spritePath, tilePath);
            }

            string[] wallNames = new string[]
            {
                "Wall_Int_H", "Wall_Int_V", "Wall_Face_Left", "Wall_Face_Right",
                "Wall_Corner_TL", "Wall_Corner_TR", "Wall_Corner_BL", "Wall_Corner_BR",
                "Wall_TJunc_Down", "Wall_TJunc_Up", "Wall_TJunc_Right", "Wall_TJunc_Left",
                "Wall_Cross",
                "Wall_End_E", "Wall_End_W", "Wall_End_S", "Wall_End_N",
                "Wall_Ext_Top", "Wall_Ext_Bottom", "Wall_Ext_Left", "Wall_Ext_Right",
                "Wall_Ext_Corner_TL", "Wall_Ext_Corner_TR", "Wall_Ext_Corner_BL", "Wall_Ext_Corner_BR",
                "Wall_Ext_Window_H",
                "Wall_Solid_Roof", "Wall_Solid",
                "Wall_House_Fence_H", "Wall_House_Fence_V",
                "Wall_House_Fence_Corner_TL", "Wall_House_Fence_Corner_BL", "Wall_House_Fence_Corner_BR"
            };

            foreach (var name in wallNames)
            {
                string spritePath = $"{SpritesDir}/{name}.png";
                string tilePath = $"{TilemapsDir}/{name}_Tile.asset";
                CreateTileFromSprite(spritePath, tilePath);
            }
        }

        private static void CreateTileFromSprite(string spritePath, string tilePath)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
            {
                Debug.LogWarning($"[HouseMapAssetGenerator] Sprite tidak ditemukan di {spritePath}!");
                return;
            }

            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                AssetDatabase.CreateAsset(tile, tilePath);
            }
            else
            {
                tile.sprite = sprite;
                EditorUtility.SetDirty(tile);
            }
        }

        private static void CreateTexture(string fileName, int width, int height, Func<int, int, Color> colorFunc)
        {
            string fullPath = Path.Combine(SpritesDir, fileName);
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    tex.SetPixel(x, y, colorFunc(x, y));
                }
            }
            tex.Apply();

            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(fullPath, bytes);
            UnityEngine.Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(fullPath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(fullPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 64;
                importer.filterMode = FilterMode.Bilinear;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        #endregion
    }
}
