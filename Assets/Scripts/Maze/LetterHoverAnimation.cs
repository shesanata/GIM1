using System;
using System.Collections;
using UnityEngine;

namespace MazeSpell
{
    /// <summary>
    /// Memberikan efek visual mengambang (hovering / bobbing) halus, rotasi goyangan (wobble),
    /// pulsasi bayangan lantai (ground shadow), aura glow, serta reaksi proximity saat Player mendekat,
    /// dan animasi Collect (shrink -> pop -> bounce -> sparkle -> disappear).
    /// </summary>
    public class LetterHoverAnimation : MonoBehaviour
    {
        [Header("Osilasi Mengambang (Idle)")]
        [Tooltip("Kecepatan osilasi melayang")]
        [SerializeField] private float bobSpeed = 2.8f;

        [Tooltip("Ketinggian maksimum osilasi melayang")]
        [SerializeField] private float bobAmplitude = 0.08f;

        [Tooltip("Kecepatan goyangan rotasi kiri-kanan")]
        [SerializeField] private float wobbleSpeed = 2.2f;

        [Tooltip("Sudut rotasi goyangan maksimum (derajat)")]
        [SerializeField] private float maxWobbleAngle = 4.0f;

        [Header("Reaksi Proximity")]
        [Tooltip("Aktifkan reaksi saat player berada di dekat objek")]
        [SerializeField] private bool enableProximityReaction = true;

        [Tooltip("Jarak deteksi kedekatan pemain")]
        [SerializeField] private float proximityRadius = 2.2f;

        [Tooltip("Faktor perbesaran saat player berada di dekat objek")]
        [SerializeField] private float proximityScaleMultiplier = 1.16f;

        private Transform visualContainer;
        private Transform shadowTransform;
        private SpriteRenderer shadowSr;
        private SpriteRenderer glowSr;

        private Vector3 initialVisualLocalPos;
        private Vector3 initialVisualScale;
        private Vector3 initialShadowScale;
        private float timeOffset;

        private static GameObject cachedPlayer;
        private Coroutine collectCoroutine;
        private bool isCollecting = false;

        private void Awake()
        {
            FindChildComponents();
        }

        private void Start()
        {
            FindChildComponents();
            timeOffset = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            FindPlayer();
        }

        private void FindChildComponents()
        {
            if (visualContainer == null)
            {
                visualContainer = transform.Find("Visual");
            }

            if (visualContainer != null)
            {
                initialVisualLocalPos = visualContainer.localPosition;
                initialVisualScale = visualContainer.localScale;

                Transform glowT = visualContainer.Find("Glow");
                if (glowT != null) glowSr = glowT.GetComponent<SpriteRenderer>();
            }
            else
            {
                initialVisualLocalPos = Vector3.zero;
                initialVisualScale = Vector3.one;
            }

            if (shadowTransform == null)
            {
                shadowTransform = transform.Find("Shadow");
            }

            if (shadowTransform != null)
            {
                initialShadowScale = shadowTransform.localScale;
                shadowSr = shadowTransform.GetComponent<SpriteRenderer>();
            }
            else
            {
                initialShadowScale = new Vector3(0.6f, 0.3f, 1f);
            }
        }

        private void FindPlayer()
        {
            if (cachedPlayer == null)
            {
                cachedPlayer = GameObject.FindWithTag("Player");
                if (cachedPlayer == null)
                {
#if UNITY_2023_1_OR_NEWER
                    var pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
#else
                    var pc = UnityEngine.Object.FindObjectOfType<PlayerController>();
#endif
                    if (pc != null) cachedPlayer = pc.gameObject;
                }
            }
        }

        private void Update()
        {
            if (isCollecting) return;

            // 1. Efek mengambang lembut & goyangan rotasi pada visual container
            float yOffset = Mathf.Sin((Time.time * bobSpeed) + timeOffset) * bobAmplitude;
            float zRot = Mathf.Sin((Time.time * wobbleSpeed) + timeOffset) * maxWobbleAngle;

            if (visualContainer != null)
            {
                visualContainer.localPosition = initialVisualLocalPos + new Vector3(0f, yOffset, 0f);
                visualContainer.localRotation = Quaternion.Euler(0f, 0f, zRot);
            }

            // 2. Pulsasi bayangan lantai (shadow mengecil saat huruf naik, membesar saat huruf turun)
            if (shadowTransform != null)
            {
                float shadowScaleFactor = 1f - (yOffset / (bobAmplitude > 0.001f ? bobAmplitude : 0.08f)) * 0.18f;
                shadowTransform.localScale = new Vector3(
                    initialShadowScale.x * shadowScaleFactor,
                    initialShadowScale.y * shadowScaleFactor,
                    1f
                );
            }

            // 3. Glow subtle breath
            if (glowSr != null)
            {
                float glowPulse = 0.55f + Mathf.Sin((Time.time * 3f) + timeOffset) * 0.15f;
                glowSr.color = new Color(1f, 0.88f, 0.35f, glowPulse);
            }

            // 4. Reaksi Proximity pemain
            if (enableProximityReaction && visualContainer != null)
            {
                if (cachedPlayer == null) FindPlayer();

                float currentScaleFactor = 1f;

                if (cachedPlayer != null)
                {
                    float dist = Vector2.Distance(transform.position, cachedPlayer.transform.position);
                    if (dist < proximityRadius)
                    {
                        float proximityProgress = 1f - (dist / proximityRadius);
                        currentScaleFactor = Mathf.Lerp(1f, proximityScaleMultiplier, proximityProgress);
                    }
                }

                visualContainer.localScale = Vector3.Lerp(
                    visualContainer.localScale,
                    initialVisualScale * currentScaleFactor,
                    Time.deltaTime * 8f
                );
            }
        }

        /// <summary>
        /// Memainkan animasi pengambilan huruf: normal -> shrink -> pop & bounce -> disappear.
        /// </summary>
        public void PlayCollectAnimation(Action onComplete)
        {
            if (collectCoroutine != null) StopCoroutine(collectCoroutine);
            collectCoroutine = StartCoroutine(CollectRoutine(onComplete));
        }

        private IEnumerator CollectRoutine(Action onComplete)
        {
            isCollecting = true;
            float elapsed = 0f;

            Transform targetTr = visualContainer != null ? visualContainer : transform;
            Vector3 basePos = targetTr.localPosition;
            Vector3 baseScale = targetTr.localScale;

            // Phase 1: Small shrink anticipation (0.07s) -> 0.82x
            float shrinkDuration = 0.07f;
            while (elapsed < shrinkDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / shrinkDuration;
                targetTr.localScale = Vector3.Lerp(baseScale, baseScale * 0.82f, t);
                if (shadowSr != null) shadowSr.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.45f, 0.3f, t));
                yield return null;
            }

            // Phase 2: Pop & upward bounce (0.14s) -> 1.35x scale, +0.18 Y pos
            elapsed = 0f;
            float popDuration = 0.14f;
            Vector3 currentScale = targetTr.localScale;
            Vector3 currentPos = targetTr.localPosition;
            Vector3 targetPopScale = baseScale * 1.35f;
            Vector3 targetPopPos = basePos + new Vector3(0f, 0.18f, 0f);

            // Munculkan efek partikel sparkle bintang kecil
            SpawnSparkleBurst();

            while (elapsed < popDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / popDuration;
                float ease = Mathf.Sin(t * Mathf.PI * 0.5f);
                targetTr.localScale = Vector3.Lerp(currentScale, targetPopScale, ease);
                targetTr.localPosition = Vector3.Lerp(currentPos, targetPopPos, ease);
                if (shadowSr != null) shadowSr.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.3f, 0f, ease));
                yield return null;
            }

            // Phase 3: Disappear (0.11s) -> 0.0x scale
            elapsed = 0f;
            float disappearDuration = 0.11f;
            currentScale = targetTr.localScale;
            currentPos = targetTr.localPosition;
            while (elapsed < disappearDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / disappearDuration;
                float ease = t * t;
                targetTr.localScale = Vector3.Lerp(currentScale, Vector3.zero, ease);
                targetTr.localPosition = Vector3.Lerp(currentPos, currentPos + new Vector3(0f, 0.08f, 0f), ease);
                yield return null;
            }

            targetTr.localScale = Vector3.zero;
            if (shadowTransform != null) shadowTransform.gameObject.SetActive(false);
            isCollecting = false;
            onComplete?.Invoke();
        }

        private void SpawnSparkleBurst()
        {
            GameObject burstObj = new GameObject("SparkleBurst");
            burstObj.transform.position = transform.position + new Vector3(0f, 0.1f, -0.2f);
            StartCoroutine(SparkleBurstRoutine(burstObj));
        }

        private IEnumerator SparkleBurstRoutine(GameObject burstObj)
        {
            const int count = 8;
            GameObject[] sparkles = new GameObject[count];
            Vector3[] dirs = new Vector3[count];

            Sprite dotSprite = Resources.Load<Sprite>("Sprites/Slash_Arc");
            if (dotSprite == null)
            {
#if UNITY_EDITOR
                dotSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Slash_Arc.png");
#endif
            }

            for (int i = 0; i < count; i++)
            {
                float angle = i * (Mathf.PI * 2f / count);
                dirs[i] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);

                GameObject sp = new GameObject($"Sparkle_{i}");
                sp.transform.SetParent(burstObj.transform, false);
                sp.transform.localPosition = Vector3.zero;
                sp.transform.localScale = new Vector3(0.2f, 0.2f, 1f);

                SpriteRenderer sr = sp.AddComponent<SpriteRenderer>();
                sr.sprite = dotSprite;
                sr.color = new Color(1f, 0.92f, 0.4f, 0.95f); // glowing golden star
                sr.sortingOrder = 15;
                sparkles[i] = sp;
            }

            float duration = 0.28f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                for (int i = 0; i < count; i++)
                {
                    if (sparkles[i] != null)
                    {
                        sparkles[i].transform.localPosition = dirs[i] * (t * 0.55f);
                        sparkles[i].transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 0f, t);
                    }
                }
                yield return null;
            }

            Destroy(burstObj);
        }

        public void ResetAnimation()
        {
            if (collectCoroutine != null) StopCoroutine(collectCoroutine);
            isCollecting = false;
            if (visualContainer != null)
            {
                visualContainer.localPosition = initialVisualLocalPos;
                visualContainer.localScale = initialVisualScale;
                visualContainer.localRotation = Quaternion.identity;
            }
            if (shadowTransform != null)
            {
                shadowTransform.gameObject.SetActive(true);
                shadowTransform.localScale = initialShadowScale;
                if (shadowSr != null) shadowSr.color = new Color(0f, 0f, 0f, 0.45f);
            }
        }
    }
}
