using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MazeSpell.Audio;

namespace MazeSpell.Gameplay
{
    /// <summary>
    /// Mengontrol sistem penglihatan (Line of Sight & Red Vision Cone) musuh bergaya top-down stealth:
    /// 1. Red Vision Cone: Area penglihatan berbentuk kipas (fan shape) merah transparan di lantai.
    /// 2. Mengikuti arah hadap Enemy (kanan, kiri, atas, bawah, arah patrol & chase).
    /// 3. Line of Sight (LOS): Raycast terhadap dinding (Walls_Obstacles) agar tidak menembus dinding.
    /// 4. Deteksi & Chase: Saat Player masuk Vision Cone dan tidak terhalang dinding, musuh langsung
    ///    menampilkan alert '!' dan mengejar Player menggunakan pathfinding BFS.
    /// 5. Grace Period: Jika Player keluar dari Vision Cone / bersembunyi di balik dinding, musuh
    ///    masih mengejar posisi terakhir selama 0.8 - 1.5 detik (1.2s) sebelum kehilangan jejak ('?')
    ///    dan kembali ke rute patroli secara wajar tanpa teleportasi.
    /// </summary>
    [RequireComponent(typeof(EnemyCharacter))]
    public class EnemyVisionController : MonoBehaviour
    {
        [Header("Vision Settings")]
        [Tooltip("Jarak maksimal pandangan mata musuh")]
        [SerializeField] private float viewDistance = 7.0f;

        [Tooltip("Sudut kerucut pandang (Field of View dalam derajat, misal 75-80)")]
        [Range(30f, 180f)]
        [SerializeField] private float fieldOfViewAngle = 75f;

        [Tooltip("Layer mask dinding/penghalang untuk Raycast Line of Sight")]
        [SerializeField] private LayerMask obstacleMask;

        [Header("Patrol & Look Settings")]
        [SerializeField] private bool enablePatrolLook = true;
        [SerializeField] private float lookSweepSpeed = 45f;
        [SerializeField] private float sweepAngle = 20f;

        [Header("Vision Cone Visual")]
        [SerializeField] private Color normalConeColor = new Color(0.95f, 0.15f, 0.15f, 0.25f);
        [SerializeField] private Color alertConeColor = new Color(1.0f, 0.10f, 0.10f, 0.45f);

        // Komponen & Referensi
        private EnemyCharacter enemyChar;
        private Transform playerTransform;

        // Visual Vision Cone (Procedural 2D Mesh)
        private GameObject visionConeObj;
        private MeshFilter visionMeshFilter;
        private MeshRenderer visionMeshRenderer;
        private Mesh visionMesh;
        private Material visionMaterial;

        // Visual Balon Deteksi ('!' dan '?')
        private GameObject alertBalloonObj;
        private SpriteRenderer alertBalloonRenderer;
        private Sprite alertExclamationSprite;
        private Sprite alertQuestionSprite;

        // Status Arah & Deteksi
        private Vector2 facingDirection = Vector2.right;
        private Vector2 baseFacingDirection = Vector2.right;
        private Vector2 lastPosition;
        private bool isPlayerSpotted = false;
        private bool wasPlayerSpottedLastFrame = false;
        private Coroutine alertCoroutine;
        private float lastAlertSfxTime = -999f;

        public bool IsPlayerSpotted => isPlayerSpotted;
        public Vector2 FacingDirection => facingDirection;
        public float ViewDistance { get => viewDistance; set => viewDistance = value; }
        public float FieldOfViewAngle { get => fieldOfViewAngle; set => fieldOfViewAngle = value; }

        private void Awake()
        {
            enemyChar = GetComponent<EnemyCharacter>();
            obstacleMask = LayerMask.GetMask("Default", "Wall", "Obstacles");
            if (obstacleMask == 0) obstacleMask = ~0;

            lastPosition = transform.position;

            LoadSprites();
            CreateProceduralVisionCone();
            CreateAlertBalloonObject();
        }

        private void Start()
        {
            FindPlayer();

            // Khusus mode Boss di Level 5: Sesuaikan jarak pandang agar mencakup seluruh arena
            if (enemyChar != null && enemyChar.IsBoss)
            {
                viewDistance = 16f;
                fieldOfViewAngle = 90f;
            }
        }

        private void LoadSprites()
        {
#if UNITY_EDITOR
            // Load dari Multiple Sprite Asset RobberyBob
            UnityEngine.Object[] bobAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/RobberyBob/Alert_Exclamation.png");
            if (bobAssets != null)
            {
                foreach (var obj in bobAssets)
                {
                    if (obj is Sprite s) { alertExclamationSprite = s; break; }
                }
            }

            UnityEngine.Object[] quesAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/RobberyBob/Alert_Question.png");
            if (quesAssets != null)
            {
                foreach (var obj in quesAssets)
                {
                    if (obj is Sprite s) { alertQuestionSprite = s; break; }
                }
            }
#endif
            if (alertExclamationSprite == null)
            {
                alertExclamationSprite = Resources.Load<Sprite>("Alert_Exclamation");
            }
            if (alertQuestionSprite == null)
            {
                alertQuestionSprite = Resources.Load<Sprite>("Alert_Question");
            }

            // Fallback sprite jika tidak ditemukan
            if (alertExclamationSprite == null)
            {
                alertExclamationSprite = CreateFallbackAlertSprite(true);
            }
            if (alertQuestionSprite == null)
            {
                alertQuestionSprite = CreateFallbackAlertSprite(false);
            }
        }

        private Sprite CreateFallbackAlertSprite(bool isExclamation)
        {
            int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color bg = new Color(0.1f, 0.1f, 0.1f, 0.85f);
            Color fg = isExclamation ? new Color(1f, 0.2f, 0.2f, 1f) : new Color(1f, 0.85f, 0.2f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(16, 16));
                    if (d < 14) tex.SetPixel(x, y, bg);
                    else tex.SetPixel(x, y, Color.clear);
                }
            }

            if (isExclamation)
            {
                // Garis seru
                for (int y = 12; y <= 24; y++)
                {
                    for (int x = 14; x <= 17; x++) tex.SetPixel(x, y, fg);
                }
                // Titik seru
                for (int y = 7; y <= 9; y++)
                {
                    for (int x = 14; x <= 17; x++) tex.SetPixel(x, y, fg);
                }
            }
            else
            {
                // Titik tanya
                for (int y = 7; y <= 9; y++)
                {
                    for (int x = 14; x <= 17; x++) tex.SetPixel(x, y, fg);
                }
                for (int y = 13; y <= 16; y++)
                {
                    for (int x = 14; x <= 17; x++) tex.SetPixel(x, y, fg);
                }
                for (int y = 17; y <= 22; y++)
                {
                    for (int x = 17; x <= 21; x++) tex.SetPixel(x, y, fg);
                }
                for (int y = 20; y <= 24; y++)
                {
                    for (int x = 12; x <= 20; x++) tex.SetPixel(x, y, fg);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32);
        }

        public void EnsureVisionConeVisuals()
        {
            LoadSprites();
            CreateProceduralVisionCone();
            CreateAlertBalloonObject();
            UpdateProceduralVisionMesh();
        }

        private void OnEnable()
        {
            EnsureVisionConeVisuals();
        }

        private void CreateProceduralVisionCone()
        {
            if (visionConeObj != null && visionMesh != null) return;

            Transform existing = transform.Find("VisionConeVisual");
            if (existing != null)
            {
                DestroyImmediate(existing.gameObject);
            }

            visionConeObj = new GameObject("VisionConeVisual");
            visionConeObj.transform.SetParent(transform, false);
            visionConeObj.transform.localPosition = Vector3.zero;
            visionConeObj.transform.localRotation = Quaternion.identity;
            visionConeObj.transform.localScale = Vector3.one;

            visionMeshFilter = visionConeObj.AddComponent<MeshFilter>();
            visionMeshRenderer = visionConeObj.AddComponent<MeshRenderer>();

            visionMesh = new Mesh();
            visionMesh.name = "VisionConeMesh";
            visionMeshFilter.sharedMesh = visionMesh;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Transparent");
            visionMaterial = new Material(shader);
            visionMeshRenderer.material = visionMaterial;
            visionMeshRenderer.sortingLayerName = "Default";
            visionMeshRenderer.sortingOrder = 2; // Di atas lantai (0/1), di bawah karakter (4)
        }

        private void CreateAlertBalloonObject()
        {
            if (alertBalloonObj != null) return;

            Transform existing = transform.Find("AlertBalloon");
            if (existing != null)
            {
                DestroyImmediate(existing.gameObject);
            }

            alertBalloonObj = new GameObject("AlertBalloon");
            alertBalloonObj.transform.SetParent(transform, false);
            alertBalloonObj.transform.localPosition = new Vector3(0f, 0.95f, -0.2f);
            alertBalloonObj.transform.localScale = Vector3.zero;

            alertBalloonRenderer = alertBalloonObj.AddComponent<SpriteRenderer>();
            if (alertExclamationSprite != null)
            {
                alertBalloonRenderer.sprite = alertExclamationSprite;
            }
            alertBalloonRenderer.sortingOrder = 12; // Di atas karakter dan HUD lokal
        }

        public void SetPlayerTarget(Transform p)
        {
            playerTransform = p;
        }

        private void FindPlayer()
        {
            if (playerTransform != null && playerTransform.gameObject != null && playerTransform.gameObject.activeInHierarchy) return;

            if (enemyChar != null && enemyChar.PlayerTransform != null)
            {
                playerTransform = enemyChar.PlayerTransform;
                return;
            }

            GameObject p = GameObject.FindWithTag("Player");
            if (p == null)
            {
#if UNITY_2023_1_OR_NEWER
                PlayerController pc = FindAnyObjectByType<PlayerController>();
#else
                PlayerController pc = FindObjectOfType<PlayerController>();
#endif
                if (pc != null) p = pc.gameObject;
            }
            if (p != null) playerTransform = p.transform;
        }

        private void Update()
        {
            EnsureVisionConeVisuals();

            if (enemyChar != null && enemyChar.IsDefeated)
            {
                if (visionConeObj != null && visionConeObj.activeSelf) visionConeObj.SetActive(false);
                if (alertBalloonObj != null && alertBalloonObj.activeSelf) alertBalloonObj.SetActive(false);
                return;
            }

            if (visionConeObj != null && !visionConeObj.activeSelf)
            {
                visionConeObj.SetActive(true);
            }

            FindPlayer();

            // 1. Cek Penglihatan ke Player (Line of Sight & Field of View) berdasarkan arah hadap saat ini
            CheckLineOfSight();

            // 2. Update Arah Hadap (Facing Direction)
            UpdateFacingDirection();

            // 3. Update Visual Kerucut Pandang Kipas Merah
            UpdateProceduralVisionMesh();
        }

        public void SetFacingDirection(Vector2 dir)
        {
            if (dir.sqrMagnitude > 0.001f)
            {
                baseFacingDirection = dir.normalized;
                facingDirection = baseFacingDirection;
                if (enemyChar != null)
                {
                    enemyChar.SetFacingDirection(baseFacingDirection);
                }
            }
        }

        private void UpdateFacingDirection()
        {
            // Jika Player sedang terlihat langsung
            if (isPlayerSpotted && playerTransform != null)
            {
                Vector2 toPlayer = (Vector2)(playerTransform.position - transform.position);
                if (toPlayer.sqrMagnitude > 0.001f)
                {
                    baseFacingDirection = toPlayer.normalized;
                    facingDirection = baseFacingDirection;
                }
            }
            // Jika sedang dalam masa grace period mengejar posisi terakhir
            else if (enemyChar != null && enemyChar.HasLastKnownPosition)
            {
                Vector2 toLastKnown = (Vector2)(enemyChar.LastKnownTargetPosition - transform.position);
                if (toLastKnown.sqrMagnitude > 0.001f)
                {
                    baseFacingDirection = toLastKnown.normalized;
                    facingDirection = baseFacingDirection;
                }
            }
            else
            {
                // Ambil arah hadap dari EnemyCharacter jika tersedia
                if (enemyChar != null && enemyChar.CurrentFacingDirection.sqrMagnitude > 0.001f)
                {
                    baseFacingDirection = enemyChar.CurrentFacingDirection;
                }
                else
                {
                    Vector2 deltaPos = (Vector2)transform.position - lastPosition;
                    if (deltaPos.sqrMagnitude > 0.0001f)
                    {
                        baseFacingDirection = deltaPos.normalized;
                    }
                }

                // Saat patrol idle atau bergerak perlahan, berikan sweep tipis
                if (enablePatrolLook)
                {
                    float sweep = Mathf.Sin(Time.time * (lookSweepSpeed * Mathf.Deg2Rad)) * sweepAngle;
                    float baseAngle = Mathf.Atan2(baseFacingDirection.y, baseFacingDirection.x) * Mathf.Rad2Deg;
                    float sweptAngle = (baseAngle + sweep) * Mathf.Deg2Rad;
                    facingDirection = new Vector2(Mathf.Cos(sweptAngle), Mathf.Sin(sweptAngle)).normalized;
                }
                else
                {
                    facingDirection = baseFacingDirection;
                }
            }

            lastPosition = transform.position;
        }

        /// <summary>
        /// Memeriksa apakah musuh dapat melihat Player secara langsung tanpa terhalang dinding (LOS).
        /// </summary>
        private void CheckLineOfSight()
        {
            if (playerTransform == null || !playerTransform.gameObject.activeInHierarchy)
            {
                SetSpotted(false);
                return;
            }

            // Jika Player sedang bersembunyi di kardus
            PlayerController playerCtrl = playerTransform.GetComponent<PlayerController>();
            if (playerCtrl != null && playerCtrl.IsHiding)
            {
                SetSpotted(false);
                return;
            }

            Vector2 origin = transform.position;
            Vector2 target = playerTransform.position;
            Vector2 toTarget = target - origin;
            float dist = toTarget.magnitude;

            // 1. Cek jarak deteksi
            if (dist > viewDistance)
            {
                SetSpotted(false);
                return;
            }

            // 2. Cek sudut kerucut pandang (FOV)
            float angleToTarget = Vector2.Angle(facingDirection, toTarget.normalized);
            if (angleToTarget > fieldOfViewAngle * 0.5f)
            {
                // Di luar sudut pandang (kecuali Player berada sangat dekat/nempel <= 0.8 unit)
                if (dist > 0.8f)
                {
                    SetSpotted(false);
                    return;
                }
            }

            // 3. Line of Sight Raycast: apakah terhalang dinding (Walls_Obstacles)?
            RaycastHit2D[] hits = Physics2D.RaycastAll(origin, toTarget.normalized, dist);
            bool hitObstacle = false;
            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;
                if (hit.collider.gameObject == gameObject) continue;
                if (hit.collider.transform.IsChildOf(transform)) continue;
                if (hit.collider.transform == playerTransform || hit.collider.CompareTag("Player")) continue;

                // Abaikan trigger collectible (huruf, item)
                if (hit.collider.isTrigger && !hit.collider.CompareTag("Obstacle"))
                {
                    continue;
                }

                // Jika mengenai dinding atau collider solid lainnya
                if (!hit.collider.isTrigger || hit.collider.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>() != null || hit.collider.name.Contains("Wall"))
                {
                    hitObstacle = true;
                    break;
                }
            }

            if (hitObstacle)
            {
                SetSpotted(false);
                return;
            }

            // SEMUA KONDISI DETEKSI TERPENUHI: Player terlihat!
            SetSpotted(true);
        }

        /// <summary>
        /// Memaksa musuh kehilangan jejak Player secara instan (misal saat Player masuk kardus).
        /// </summary>
        public void ForceLosePlayer()
        {
            if (isPlayerSpotted)
            {
                isPlayerSpotted = false;
                wasPlayerSpottedLastFrame = false;
                if (enemyChar != null)
                {
                    enemyChar.ClearTargetImmediate();
                }
                TriggerAlert(false);
            }
        }

        /// <summary>
        /// Dipanggil oleh EnemyCharacter saat grace period habis dan target benar-benar hilang.
        /// </summary>
        public void NotifyTargetLost()
        {
            if (alertBalloonObj != null && gameObject.activeInHierarchy)
            {
                TriggerAlert(false); // Muncul balon '?'
            }
        }

        private void SetSpotted(bool spotted)
        {
            isPlayerSpotted = spotted;

            if (isPlayerSpotted)
            {
                if (enemyChar != null && playerTransform != null)
                {
                    enemyChar.SetTarget(playerTransform);
                }

                if (!wasPlayerSpottedLastFrame)
                {
                    // Player baru saja terdeteksi -> Alert '!' dan SFX
                    TriggerAlert(true);
                }
            }

            wasPlayerSpottedLastFrame = isPlayerSpotted;
        }

        private void TriggerAlert(bool exclamation)
        {
            if (alertCoroutine != null) StopCoroutine(alertCoroutine);
            if (gameObject.activeInHierarchy)
            {
                alertCoroutine = StartCoroutine(ShowAlertAnimation(exclamation));
            }

            if (exclamation)
            {
                if (Time.time - lastAlertSfxTime > 1.5f)
                {
                    lastAlertSfxTime = Time.time;
                    Debug.Log("[EnemyVisionController] ⚠ PLAYER TERDETEKSI! Musuh langsung mengejar!");
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlaySFX(AudioManager.SFXType.EnemyAttackReady);
                    }
                }
            }
            else
            {
                Debug.Log("[EnemyVisionController] ? Musuh kehilangan jejak Player. Kembali ke patroli.");
            }
        }

        private IEnumerator ShowAlertAnimation(bool exclamation)
        {
            if (alertBalloonObj == null || alertBalloonRenderer == null) yield break;

            alertBalloonRenderer.sprite = exclamation ? alertExclamationSprite : alertQuestionSprite;
            alertBalloonObj.SetActive(true);

            // Pop-in bounce animation
            float elapsed = 0f;
            float duration = 0.18f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float scale = Mathf.SmoothStep(0f, 1.25f, elapsed / duration);
                alertBalloonObj.transform.localScale = Vector3.one * scale;
                yield return null;
            }
            alertBalloonObj.transform.localScale = Vector3.one * 1.05f;

            // Waktu tampil di atas kepala musuh
            yield return new WaitForSeconds(exclamation ? 1.3f : 0.85f);

            // Pop-out animation
            elapsed = 0f;
            while (elapsed < 0.14f)
            {
                elapsed += Time.deltaTime;
                float scale = Mathf.SmoothStep(1.05f, 0f, elapsed / 0.14f);
                alertBalloonObj.transform.localScale = Vector3.one * scale;
                yield return null;
            }
            alertBalloonObj.transform.localScale = Vector3.zero;
        }

        /// <summary>
        /// Memperbarui geometri dan warna 2D procedural mesh Vision Cone merah transparan.
        /// </summary>
        private void UpdateProceduralVisionMesh()
        {
            if (visionMesh == null) return;

            int rayCount = 26;
            float halfFov = fieldOfViewAngle * 0.5f;
            float angleStep = fieldOfViewAngle / rayCount;

            Vector3[] vertices = new Vector3[rayCount + 2];
            int[] triangles = new int[rayCount * 3];
            Color[] colors = new Color[rayCount + 2];

            // Titik tengah pada Enemy
            vertices[0] = Vector3.zero;

            Color centerCol = isPlayerSpotted ? alertConeColor : normalConeColor;
            Color edgeCol = new Color(centerCol.r, centerCol.g, centerCol.b, centerCol.a * 0.60f);

            colors[0] = centerCol;

            Vector2 enemyPos = transform.position;
            float baseAngle = Mathf.Atan2(facingDirection.y, facingDirection.x) * Mathf.Rad2Deg;

            for (int i = 0; i <= rayCount; i++)
            {
                float currentAngle = -halfFov + (angleStep * i);
                float rayAngleRad = (baseAngle + currentAngle) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rayAngleRad), Mathf.Sin(rayAngleRad));

                // Raycast Line of Sight untuk menghentikan cone tepat di permukaan dinding
                float dist = viewDistance;
                RaycastHit2D[] hits = Physics2D.RaycastAll(enemyPos, dir, viewDistance);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                foreach (var hit in hits)
                {
                    if (hit.collider == null) continue;
                    if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform)) continue;
                    if (hit.collider.CompareTag("Player")) continue; // Penglihatan visual melewati player
                    if (hit.collider.isTrigger && !hit.collider.CompareTag("Obstacle")) continue;

                    if (!hit.collider.isTrigger || hit.collider.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>() != null || hit.collider.name.Contains("Wall"))
                    {
                        dist = Mathf.Max(0.1f, hit.distance);
                        break;
                    }
                }

                Vector3 worldPoint = (Vector3)(enemyPos + dir * dist);
                vertices[i + 1] = transform.InverseTransformPoint(worldPoint);
                colors[i + 1] = edgeCol;

                if (i < rayCount)
                {
                    triangles[i * 3] = 0;
                    triangles[i * 3 + 1] = i + 1;
                    triangles[i * 3 + 2] = i + 2;
                }
            }

            visionMesh.Clear();
            visionMesh.vertices = vertices;
            visionMesh.triangles = triangles;
            visionMesh.colors = colors;
            visionMesh.RecalculateBounds();

            if (visionMeshFilter != null && visionMeshFilter.sharedMesh != visionMesh)
            {
                visionMeshFilter.sharedMesh = visionMesh;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = isPlayerSpotted ? Color.red : new Color(1f, 0.2f, 0.2f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, viewDistance);

            Vector3 leftRay = Quaternion.Euler(0, 0, fieldOfViewAngle * 0.5f) * (Vector3)facingDirection * viewDistance;
            Vector3 rightRay = Quaternion.Euler(0, 0, -fieldOfViewAngle * 0.5f) * (Vector3)facingDirection * viewDistance;

            Gizmos.DrawRay(transform.position, leftRay);
            Gizmos.DrawRay(transform.position, rightRay);
            Gizmos.DrawRay(transform.position, (Vector3)facingDirection * viewDistance);
        }
    }
}
