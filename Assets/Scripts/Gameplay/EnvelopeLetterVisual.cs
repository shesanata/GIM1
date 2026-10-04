using System.Collections;
using UnityEngine;
using TMPro;

namespace MazeSpell.Gameplay
{
    /// <summary>
    /// Menghadirkan visual Kop Surat (Secret Document Envelope) untuk huruf koleksi:
    /// 1. Huruf tersembunyi di dalam amplop kop surat dengan segel wax merah.
    /// 2. Animasi floating & breathing lembut menarik perhatian saat pemain menjelajahi ruangan.
    /// 3. Saat pemain mengambilnya, amplop terbuka dengan efek partikel kilau emas,
    ///    mengungkap huruf rahasia yang melayang ke atas dan masuk ke HUD kata.
    /// </summary>
    [RequireComponent(typeof(CollectibleLetter))]
    public class EnvelopeLetterVisual : MonoBehaviour
    {
        [Header("Envelope Visual Settings")]
        [SerializeField] private Vector3 envelopeOffset = Vector3.zero;
        [SerializeField] private float bobSpeed = 2.4f;
        [SerializeField] private float bobHeight = 0.08f;
        [SerializeField] private float wobbleAngle = 4.5f;

        private CollectibleLetter letterComp;
        private GameObject envelopeObj;
        private SpriteRenderer envelopeRenderer;
        private Sprite envelopeSprite;
        private Vector3 startLocalPos;
        private bool isOpening = false;

        private void Awake()
        {
            letterComp = GetComponent<CollectibleLetter>();
            startLocalPos = transform.localPosition;
            LoadSprite();
            CreateEnvelopeObject();
        }

        private void LoadSprite()
        {
#if UNITY_EDITOR
            envelopeSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/RobberyBob/Envelope_SecretLetter.png");
#endif
            if (envelopeSprite == null)
            {
                envelopeSprite = Resources.Load<Sprite>("Envelope_SecretLetter");
            }
        }

        private void CreateEnvelopeObject()
        {
            if (envelopeObj != null) return;

            Transform existing = transform.Find("EnvelopeVisual");
            if (existing != null)
            {
                envelopeObj = existing.gameObject;
                envelopeRenderer = envelopeObj.GetComponent<SpriteRenderer>();
                return;
            }

            envelopeObj = new GameObject("EnvelopeVisual");
            envelopeObj.transform.SetParent(transform, false);
            envelopeObj.transform.localPosition = envelopeOffset + new Vector3(0f, 0f, 0.02f);
            envelopeObj.transform.localScale = new Vector3(1.1f, 1.1f, 1f);

            envelopeRenderer = envelopeObj.AddComponent<SpriteRenderer>();
            if (envelopeSprite != null)
            {
                envelopeRenderer.sprite = envelopeSprite;
            }
            envelopeRenderer.sortingOrder = 3; // Di bawah teks huruf, di atas lantai
        }

        private void Start()
        {
            // Pastikan listener terdaftar saat huruf dikoleksi
            if (letterComp != null)
            {
                letterComp.onCollected.AddListener(OnLetterCollected);
            }
        }

        private void Update()
        {
            if (isOpening || (letterComp != null && letterComp.IsCollected)) return;

            // Animasi melayang / floating kop surat
            float t = Time.time * bobSpeed;
            float yOffset = Mathf.Sin(t) * bobHeight;
            float rotZ = Mathf.Sin(t * 0.7f) * wobbleAngle;

            if (envelopeObj != null)
            {
                envelopeObj.transform.localPosition = envelopeOffset + new Vector3(0f, yOffset, 0.02f);
                envelopeObj.transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
            }
        }

        private void OnLetterCollected(char collectedChar)
        {
            if (isOpening) return;
            StartCoroutine(CollectEnvelopeRoutine(collectedChar));
        }

        private IEnumerator CollectEnvelopeRoutine(char collectedChar)
        {
            isOpening = true;

            // Floating text feedback: ✉ Dokumen Rahasia [E] ditemukan!
            Vector3 textPos = transform.position + Vector3.up * 1.0f;
            FloatingTextHelper.ShowFloatingText(textPos, $"✉ Kop Surat Ditemukan: [{collectedChar}]!", new Color(1f, 0.90f, 0.25f, 1f), 28);

            // Pop and sparkle envelope animation
            float elapsed = 0f;
            float duration = 0.35f;
            Vector3 startScale = envelopeObj != null ? envelopeObj.transform.localScale : Vector3.one;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;

                if (envelopeObj != null)
                {
                    // Scale up then pop
                    float s = Mathf.Sin(progress * Mathf.PI) * 0.4f + 1.0f;
                    envelopeObj.transform.localScale = startScale * s;
                    envelopeObj.transform.localPosition = envelopeOffset + Vector3.up * (progress * 0.5f);

                    if (envelopeRenderer != null)
                    {
                        Color c = envelopeRenderer.color;
                        c.a = Mathf.Clamp01(1f - (progress * 1.2f));
                        envelopeRenderer.color = c;
                    }
                }
                yield return null;
            }

            if (envelopeObj != null)
            {
                envelopeObj.SetActive(false);
            }
        }

        public void ResetVisual()
        {
            isOpening = false;
            if (envelopeObj != null)
            {
                envelopeObj.SetActive(true);
                envelopeObj.transform.localScale = new Vector3(0.95f, 0.95f, 1f);
                envelopeObj.transform.localPosition = envelopeOffset;
                if (envelopeRenderer != null)
                {
                    envelopeRenderer.color = Color.white;
                }
            }
        }
    }
}
