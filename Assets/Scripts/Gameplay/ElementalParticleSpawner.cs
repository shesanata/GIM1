using System.Collections;
using UnityEngine;

namespace MazeSpell.Gameplay
{
    /// <summary>
    /// Menghasilkan partikel visual berbasis elemen untuk identitas setiap level:
    /// - Level 1 (EARTH): Partikel debu tanah & percikan amber.
    /// - Level 2 (STONE): Serpihan batu & impact dust abu-abu/slate.
    /// - Level 3 (GRASS): Daun hijau segar & nature petals melayang.
    /// - Level 4 (FROST): Kristal es cyan berkilau & butiran salju.
    /// - Level 5 (FLAME): Percikan bara api oranye/merah membara.
    /// Serta efek tempur (Hit sparks), Secret Found, dan Victory Confetti.
    /// Seluruh partikel ringan, menggunakan tekstur prosedural, dan otomatis dihancurkan setelah selesai.
    /// </summary>
    public static class ElementalParticleSpawner
    {
        private static Texture2D circleTexture;
        private static Texture2D starTexture;
        private static Material particleMaterial;

        private static void EnsureResources()
        {
            if (circleTexture == null)
            {
                circleTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                circleTexture.filterMode = FilterMode.Bilinear;
                for (int y = 0; y < 32; y++)
                {
                    for (int x = 0; x < 32; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f));
                        float alpha = Mathf.Clamp01(1f - (dist / 14f));
                        circleTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
                    }
                }
                circleTexture.Apply();
            }

            if (particleMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null) shader = Shader.Find("UI/Default");
                if (shader == null) shader = Shader.Find("Mobile/Particles/Additive");
                particleMaterial = new Material(shader);
                particleMaterial.mainTexture = circleTexture;
            }
        }

        public static ParticleSystem SpawnElementEffect(int level, Vector3 worldPosition)
        {
            switch (level)
            {
                case 1:
                    return SpawnEarthEffect(worldPosition);
                case 2:
                    return SpawnStoneEffect(worldPosition);
                case 3:
                    return SpawnGrassEffect(worldPosition);
                case 4:
                    return SpawnFrostEffect(worldPosition);
                case 5:
                    return SpawnFlameEffect(worldPosition);
                default:
                    return SpawnEarthEffect(worldPosition);
            }
        }

        public static ParticleSystem SpawnEarthEffect(Vector3 position)
        {
            EnsureResources();
            Color startColor = new Color(0.72f, 0.52f, 0.28f, 0.9f); // Warm earth brown
            Color endColor = new Color(0.85f, 0.70f, 0.35f, 0.4f);   // Amber dust
            return CreateBurstSystem("EarthParticles", position, startColor, endColor, 35, 1.2f, 0.25f, 2.5f);
        }

        public static ParticleSystem SpawnStoneEffect(Vector3 position)
        {
            EnsureResources();
            Color startColor = new Color(0.65f, 0.70f, 0.75f, 0.9f); // Slate gray stone
            Color endColor = new Color(0.40f, 0.45f, 0.50f, 0.3f);   // Impact dust
            return CreateBurstSystem("StoneParticles", position, startColor, endColor, 30, 0.9f, 0.3f, 3.2f);
        }

        public static ParticleSystem SpawnGrassEffect(Vector3 position)
        {
            EnsureResources();
            Color startColor = new Color(0.35f, 0.85f, 0.30f, 0.9f); // Emerald leaves
            Color endColor = new Color(0.60f, 0.95f, 0.40f, 0.4f);   // Light lime petals
            return CreateBurstSystem("GrassParticles", position, startColor, endColor, 40, 1.4f, 0.22f, 2.0f);
        }

        public static ParticleSystem SpawnFrostEffect(Vector3 position)
        {
            EnsureResources();
            Color startColor = new Color(0.45f, 0.85f, 1.0f, 0.95f); // Ice crystal cyan
            Color endColor = new Color(0.90f, 0.98f, 1.0f, 0.5f);   // White snow drift
            return CreateBurstSystem("FrostParticles", position, startColor, endColor, 45, 1.3f, 0.28f, 2.6f);
        }

        public static ParticleSystem SpawnFlameEffect(Vector3 position)
        {
            EnsureResources();
            Color startColor = new Color(1.0f, 0.40f, 0.10f, 0.95f); // Burning red-orange
            Color endColor = new Color(1.0f, 0.80f, 0.20f, 0.5f);   // Golden heat spark
            return CreateBurstSystem("FlameParticles", position, startColor, endColor, 50, 1.5f, 0.32f, 3.5f);
        }

        public static ParticleSystem SpawnHitImpactEffect(Vector3 position)
        {
            EnsureResources();
            Color startColor = new Color(1.0f, 0.95f, 0.45f, 1.0f); // Bright yellow spark
            Color endColor = new Color(1.0f, 0.35f, 0.15f, 0.6f);   // Orange hit recoil
            return CreateBurstSystem("HitImpactParticles", position, startColor, endColor, 20, 0.45f, 0.2f, 4.0f);
        }

        public static ParticleSystem SpawnSecretFoundEffect(Vector3 position)
        {
            EnsureResources();
            Color startColor = new Color(1.0f, 0.88f, 0.20f, 1.0f); // Radiant gold
            Color endColor = new Color(0.95f, 0.95f, 1.0f, 0.5f);   // White sparkle
            return CreateBurstSystem("SecretFoundParticles", position, startColor, endColor, 45, 1.2f, 0.35f, 3.0f);
        }

        public static ParticleSystem SpawnVictoryConfetti(Vector3 position)
        {
            EnsureResources();
            GameObject go = new GameObject("VictoryConfetti");
            go.transform.position = position;

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 1.0f;
            main.loop = false;
            main.startLifetime = 1.5f;
            main.startSpeed = 4.5f;
            main.startSize = 0.28f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;

            // Multi color gradient
            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 0.85f, 0.2f), 0f),
                    new GradientColorKey(new Color(0.3f, 0.9f, 1f), 0.5f),
                    new GradientColorKey(new Color(1f, 0.3f, 0.5f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.8f, 0.6f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            col.color = grad;

            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 50) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = particleMaterial;
            renderer.sortingOrder = 25;

            ps.Play();
            SafeDestroy(go, 2.0f);
            return ps;
        }

        private static void SafeDestroy(GameObject go, float delay)
        {
            if (go == null) return;
            if (Application.isPlaying)
            {
                Object.Destroy(go, delay);
            }
            // In Edit Mode, do not immediately destroy during creation so asserts/renderers can inspect it
        }

        private static ParticleSystem CreateBurstSystem(string name, Vector3 pos, Color startCol, Color endCol, int count, float lifetime, float size, float speed)
        {
            GameObject go = new GameObject(name);
            go.transform.position = pos;

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.8f;
            main.loop = false;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = startCol;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, count) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.35f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(startCol, 0f), new GradientColorKey(endCol, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(startCol.a, 0f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = grad;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 0.5f);
            curve.AddKey(0.2f, 1.15f);
            curve.AddKey(1f, 0.1f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = particleMaterial;
            renderer.sortingOrder = 20;

            ps.Play();
            SafeDestroy(go, lifetime + 0.5f);
            return ps;
        }

        /// <summary>
        /// Menghasilkan partikel aura api yang melekat pada Boss (Final Guardian).
        /// Intensitas bertambah seiring fase (Phase 1, 2, 3).
        /// </summary>
        public static GameObject SpawnBossAura(Transform parent, int phase)
        {
            EnsureResources();
            if (parent == null) return null;

            // Hapus aura lama jika sudah ada
            Transform existing = parent.Find("BossFlameAura");
            if (existing != null)
            {
                if (Application.isPlaying) Object.Destroy(existing.gameObject);
                else Object.DestroyImmediate(existing.gameObject);
            }

            GameObject go = new GameObject("BossFlameAura");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, -0.1f, 0.05f);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 1.0f;
            main.loop = true;
            main.startLifetime = phase == 3 ? 0.9f : 0.65f;
            main.startSpeed = phase == 3 ? 1.6f : 1.1f;
            main.startSize = phase == 3 ? 0.35f : 0.22f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            Color startCol = phase == 3 ? new Color(1.0f, 0.25f, 0.05f, 0.95f) : new Color(1.0f, 0.55f, 0.15f, 0.85f);
            main.startColor = startCol;

            var emission = ps.emission;
            emission.rateOverTime = phase == 3 ? 35f : (phase == 2 ? 22f : 12f);

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = phase == 3 ? 0.55f : 0.40f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(startCol, 0f), new GradientColorKey(new Color(1f, 0.85f, 0.2f), 0.6f), new GradientColorKey(new Color(0.3f, 0.1f, 0.1f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = grad;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = particleMaterial;
            renderer.sortingOrder = 3; // Tepat di belakang sprite body

            ps.Play();
            return go;
        }

        /// <summary>
        /// Ledakan api dramatis saat Final Guardian dikalahkan.
        /// </summary>
        public static void SpawnFlameDefeatExplosion(Vector3 pos)
        {
            EnsureResources();
            // 1. Core fiery blast
            CreateBurstSystem("DefeatBlast_Core", pos, new Color(1f, 0.9f, 0.5f, 1f), new Color(1f, 0.3f, 0.05f, 0f), 70, 1.4f, 0.55f, 5.5f);
            // 2. Lingering embers
            CreateBurstSystem("DefeatBlast_Embers", pos, new Color(1f, 0.4f, 0.1f, 0.9f), new Color(0.8f, 0.1f, 0f, 0f), 45, 2.0f, 0.25f, 3.2f);
        }
    }
}
