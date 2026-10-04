using System.Collections;
using UnityEngine;
using MazeSpell.Audio;

namespace MazeSpell.Gameplay
{
    /// <summary>
    /// Mengelola gimmick khas untuk masing-masing level:
    /// - Level 1 (EARTH): Partikel debu tanah halus di koridor.
    /// - Level 2 (STONE): Magical Stone Gate yang terbuka saat 2 huruf terkumpul, membuka jalan ke Secret Bonus.
    /// - Level 3 (GRASS): Briar Hazard Zone yang memberikan warning dan cooldown damage (tidak setiap frame).
    /// - Level 4 (FROST): Ice Zone yang membuat pergerakan licin (slippery) namun tetap terkontrol.
    /// - Level 5 (FLAME): Flame Hazard Geyser dengan visual warning sebelum erupsi api.
    /// </summary>
    public static class LevelGimmickController
    {
        public static void SetupGimmicksForLevel(int level, Transform parent = null)
        {
            // Bersihkan gimmick lama jika ada
            var oldGimmick = GameObject.Find("ActiveLevelGimmick");
            if (oldGimmick != null) Object.Destroy(oldGimmick);

            GameObject gimmickRoot = new GameObject("ActiveLevelGimmick");
            if (parent != null) gimmickRoot.transform.SetParent(parent, true);

            switch (level)
            {
                case 1:
                    SetupEarthGimmick(gimmickRoot);
                    break;
                case 2:
                    SetupStoneGimmick(gimmickRoot);
                    break;
                case 3:
                    SetupGrassGimmick(gimmickRoot);
                    break;
                case 4:
                    SetupFrostGimmick(gimmickRoot);
                    break;
                case 5:
                    SetupFlameGimmick(gimmickRoot);
                    break;
            }
        }

        #region Level 1 - Earth
        private static void SetupEarthGimmick(GameObject root)
        {
            // Partikel debu tanah halus di Central Courtyard
            GameObject dustObj = new GameObject("EarthAmbientDust");
            dustObj.transform.SetParent(root.transform, false);
            dustObj.transform.position = new Vector3(14.5f, 7.5f, 0f);
            ElementalParticleSpawner.SpawnEarthEffect(dustObj.transform.position);
            Debug.Log("[LevelGimmickController] Level 1 (EARTH): Tutorial & Earth dust gimmick siap di (14.5, 7.5).");
        }
        #endregion

        #region Level 2 - Stone Gate
        private static void SetupStoneGimmick(GameObject root)
        {
            // Gerbang batu yang menjaga akses ke Secret Bonus di (30.5, 14.5)
            Vector2 gatePos = new Vector2(27.5f, 14.5f);
            GameObject gateObj = new GameObject("MagicalStoneGate");
            gateObj.transform.SetParent(root.transform, false);
            gateObj.transform.position = new Vector3(gatePos.x, gatePos.y, 0f);

            var gateComp = gateObj.AddComponent<MagicalStoneGateComponent>();
            gateComp.Initialize(gatePos);
            Debug.Log("[LevelGimmickController] Level 2 (STONE): Magical Stone Gate aktif di " + gatePos);
        }
        #endregion

        #region Level 3 - Grass Hazard Zone
        private static void SetupGrassGimmick(GameObject root)
        {
            // Briar / Bramble Hazard patch di (18.5, 5.5)
            Vector2 hazardPos = new Vector2(18.5f, 5.5f);
            GameObject hazardObj = new GameObject("GrassBriarHazard");
            hazardObj.transform.SetParent(root.transform, false);
            hazardObj.transform.position = new Vector3(hazardPos.x, hazardPos.y, 0f);

            hazardObj.AddComponent<BriarHazardZoneComponent>();
            Debug.Log("[LevelGimmickController] Level 3 (GRASS): Briar Hazard Zone aktif di " + hazardPos);
        }
        #endregion

        #region Level 4 - Frost Ice Zone
        private static void SetupFrostGimmick(GameObject root)
        {
            // Area es licin di Frozen Lake Cavern (21.5, 10.5)
            Vector2 icePos = new Vector2(21.5f, 10.5f);
            GameObject iceObj = new GameObject("FrostIceZone");
            iceObj.transform.SetParent(root.transform, false);
            iceObj.transform.position = new Vector3(icePos.x, icePos.y, 0f);

            iceObj.AddComponent<IceZoneComponent>();
            Debug.Log("[LevelGimmickController] Level 4 (FROST): Ice Slippery Zone aktif di " + icePos);
        }
        #endregion

        #region Level 5 - Flame Hazard
        private static void SetupFlameGimmick(GameObject root)
        {
            // Geyser api dengan siklus warning -> erupsi di South-West Geyser Basin (10.5, 4.5)
            Vector2 flamePos = new Vector2(10.5f, 4.5f);
            GameObject flameObj = new GameObject("FlameGeyserHazard");
            flameObj.transform.SetParent(root.transform, false);
            flameObj.transform.position = new Vector3(flamePos.x, flamePos.y, 0f);

            flameObj.AddComponent<FlameGeyserComponent>();
            Debug.Log("[LevelGimmickController] Level 5 (FLAME): Flame Geyser Hazard aktif di " + flamePos);
        }
        #endregion
    }

    #region Komponen Pembantu Gimmick

    /// <summary>
    /// Pintu Batu Ajaib Level 2:
    /// Menutup jalur samping menuju Secret Bonus. Terbuka otomatis saat pemain mengumpulkan 2 huruf.
    /// </summary>
    public class MagicalStoneGateComponent : MonoBehaviour
    {
        private BoxCollider2D boxCol;
        private SpriteRenderer spriteRenderer;
        private bool isOpened = false;

        public void Initialize(Vector2 pos)
        {
            boxCol = gameObject.AddComponent<BoxCollider2D>();
            boxCol.size = new Vector2(1.0f, 1.0f);
            boxCol.isTrigger = false; // Solid wall sebelum terbuka

            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            CreateStoneVisual();

            if (WordCollectionManager.Instance != null)
            {
                WordCollectionManager.Instance.onLetterCollected.AddListener(OnLetterCollected);
            }
        }

        private void OnDestroy()
        {
            if (WordCollectionManager.HasInstance && WordCollectionManager.Instance != null)
            {
                WordCollectionManager.Instance.onLetterCollected.RemoveListener(OnLetterCollected);
            }
        }

        private void OnLetterCollected(char letter, int collected, int total)
        {
            // Terbuka jika sudah mengumpulkan minimal 2 huruf atau seluruh kata
            if (!isOpened && (collected >= 2 || collected >= total))
            {
                OpenGate();
            }
        }

        public void OpenGate()
        {
            if (isOpened) return;
            isOpened = true;

            Debug.Log("[MagicalStoneGate] ★ GERBANG BATU TERBUKA! Akses ke bonus rahasia terbuka.");

            // Efek hancur / crumble batu
            ElementalParticleSpawner.SpawnStoneEffect(transform.position);
            FloatingTextHelper.Show("BATU TERBUKA!", transform.position, new Color(0.8f, 0.85f, 0.9f, 1f), 2.8f);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.SFXType.GoalUnlocked, 0.9f);
            }

            // Nonaktifkan collider solid
            if (boxCol != null) boxCol.enabled = false;

            // Fading out
            StartCoroutine(FadeOutRoutine());
        }

        private IEnumerator FadeOutRoutine()
        {
            float elapsed = 0f;
            Color c = spriteRenderer != null ? spriteRenderer.color : Color.white;
            while (elapsed < 0.6f)
            {
                elapsed += Time.deltaTime;
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = new Color(c.r, c.g, c.b, Mathf.Lerp(1f, 0f, elapsed / 0.6f));
                }
                yield return null;
            }
            Destroy(gameObject);
        }

        private void CreateStoneVisual()
        {
            Texture2D tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color slate = new Color(0.55f, 0.60f, 0.68f, 1f);
            Color dark = new Color(0.35f, 0.40f, 0.45f, 1f);
            Color rune = new Color(0.40f, 0.85f, 1.0f, 1f);

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    if (x == 0 || x == 31 || y == 0 || y == 31) tex.SetPixel(x, y, dark);
                    else if ((x == 16 || y == 16) && (x > 8 && x < 24 && y > 8 && y < 24)) tex.SetPixel(x, y, rune);
                    else tex.SetPixel(x, y, slate);
                }
            }
            tex.Apply();
            spriteRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
            spriteRenderer.sortingOrder = 10;
        }
    }

    /// <summary>
    /// Rintangan Duri Level 3:
    /// Memberi peringatan saat disentuh. Hanya memberikan 1 damage jika pemain bertahan > 0.75s di dalamnya.
    /// Tidak pernah memberikan damage setiap frame!
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class BriarHazardZoneComponent : MonoBehaviour
    {
        private float timeInside = 0f;
        private bool playerInside = false;
        private SpriteRenderer sr;

        private void Awake()
        {
            var col = GetComponent<BoxCollider2D>();
            if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.2f, 1.2f);
            col.isTrigger = true;

            sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            CreateBriarVisual();
        }

        private void CreateBriarVisual()
        {
            Texture2D tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color darkGreen = new Color(0.18f, 0.45f, 0.18f, 0.85f);
            Color thornRed = new Color(0.85f, 0.25f, 0.25f, 1f);

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    if ((x + y) % 7 == 0) tex.SetPixel(x, y, thornRed);
                    else tex.SetPixel(x, y, darkGreen);
                }
            }
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
            sr.sortingOrder = 5;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
            {
                playerInside = true;
                timeInside = 0f;
                FloatingTextHelper.Show("⚠️ AWAS DURI!", transform.position, new Color(1f, 0.6f, 0.2f, 1f), 2.5f);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
            {
                playerInside = false;
                timeInside = 0f;
            }
        }

        private void Update()
        {
            if (!playerInside) return;

            timeInside += Time.deltaTime;
            // Cooldown aman: 0.75 detik berdiri di duri baru terkena 1 damage
            if (timeInside >= 0.75f)
            {
                timeInside = 0f;
                if (PlayerLivesManager.Instance != null && !PlayerLivesManager.Instance.IsInvulnerable)
                {
                    Debug.Log("[BriarHazard] Pemain terkena duri semak!");
                    PlayerLivesManager.Instance.TakeDamage(1);
                }
            }
        }
    }

    /// <summary>
    /// Zona Es Level 4:
    /// Menjadikan pergerakan pemain sedikit licin (CurrentSlipperiness = 0.85f).
    /// Mengembalikan kontrol penuh saat keluar dari area.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class IceZoneComponent : MonoBehaviour
    {
        private void Awake()
        {
            var col = GetComponent<BoxCollider2D>();
            if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(3.5f, 1.2f);
            col.isTrigger = true;

            var sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            CreateIceVisual(sr);
        }

        private void CreateIceVisual(SpriteRenderer sr)
        {
            Texture2D tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color iceBlue = new Color(0.40f, 0.85f, 1.0f, 0.55f);
            Color iceGleam = new Color(0.90f, 0.98f, 1.0f, 0.85f);

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    if (x == y || x == 31 - y) tex.SetPixel(x, y, iceGleam);
                    else tex.SetPixel(x, y, iceBlue);
                }
            }
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
            sr.sortingOrder = 4;
            transform.localScale = new Vector3(3.5f, 1.2f, 1f);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var pc = other.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.CurrentSlipperiness = 0.85f;
                FloatingTextHelper.Show("❄️ LICIN!", pc.transform.position + Vector3.up * 0.5f, new Color(0.4f, 0.9f, 1.0f, 1f), 2.4f);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var pc = other.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.CurrentSlipperiness = 0f;
            }
        }
    }

    /// <summary>
    /// Bahaya Geyser Api Level 5:
    /// Memiliki siklus telegraphed:
    /// Safe (2.0s) -> Warning Orange (1.2s) -> Erupsi Api (0.8s, deals damage).
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class FlameGeyserComponent : MonoBehaviour
    {
        private SpriteRenderer sr;
        private BoxCollider2D col;
        private bool isErupting = false;

        private void Awake()
        {
            col = GetComponent<BoxCollider2D>();
            if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.2f, 1.2f);
            col.isTrigger = true;

            sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            CreateGeyserVisual();

            StartCoroutine(GeyserCycleRoutine());
        }

        private void CreateGeyserVisual()
        {
            Texture2D tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color lavaDark = new Color(0.25f, 0.10f, 0.08f, 0.9f);
            Color ember = new Color(0.95f, 0.40f, 0.10f, 1f);

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    if ((x * y) % 9 == 0) tex.SetPixel(x, y, ember);
                    else tex.SetPixel(x, y, lavaDark);
                }
            }
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
            sr.sortingOrder = 5;
        }

        private IEnumerator GeyserCycleRoutine()
        {
            while (true)
            {
                // 1. Idle Safe (2.0s)
                isErupting = false;
                sr.color = new Color(1f, 1f, 1f, 0.6f);
                yield return new WaitForSeconds(2.0f);

                // 2. Warning Pulses (1.2s)
                float warnElapsed = 0f;
                FloatingTextHelper.Show("⚠️ AWAS API!", transform.position, new Color(1f, 0.45f, 0.1f, 1f), 2.5f);
                while (warnElapsed < 1.2f)
                {
                    warnElapsed += Time.deltaTime;
                    float pulse = 0.5f + 0.5f * Mathf.Sin(warnElapsed * 15f);
                    sr.color = Color.Lerp(Color.yellow, new Color(1f, 0.2f, 0.1f, 1f), pulse);
                    yield return null;
                }

                // 3. Erupsi Api (0.8s)
                isErupting = true;
                sr.color = Color.white;
                ElementalParticleSpawner.SpawnFlameEffect(transform.position);

                yield return new WaitForSeconds(0.8f);
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!isErupting) return;

            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
            {
                if (PlayerLivesManager.Instance != null && !PlayerLivesManager.Instance.IsInvulnerable)
                {
                    Debug.Log("[FlameGeyser] Pemain terkena semburan api geyser!");
                    PlayerLivesManager.Instance.TakeDamage(1);
                }
            }
        }
    }

    #endregion
}
