using UnityEngine;

namespace MazeSpell.UI
{
    /// <summary>
    /// Memberikan animasi subtle breathing / idle bobbing pada visual Player saat adegan Prologue (Scene 5).
    /// Berjalan secara mandiri tanpa memerlukan Rigidbody, Collider, atau PlayerController.
    /// </summary>
    public class ProloguePlayerIdleBob : MonoBehaviour
    {
        [SerializeField] private Transform visualTransform;
        [SerializeField] private float bobFrequency = 2.8f;
        [SerializeField] private float bobHeight = 0.035f;
        [SerializeField] private float scalePulse = 0.025f;

        private Vector3 initialPos;
        private Vector3 initialScale = Vector3.one;

        public void Setup(Transform visualTr)
        {
            visualTransform = visualTr;
            if (visualTransform != null)
            {
                initialPos = visualTransform.localPosition;
                initialScale = visualTransform.localScale;
            }
        }

        private void Awake()
        {
            if (visualTransform == null)
            {
                Transform child = transform.Find("PlayerVisual");
                if (child != null) visualTransform = child;
                else visualTransform = transform;
            }

            if (visualTransform != null)
            {
                initialPos = visualTransform.localPosition;
                initialScale = visualTransform.localScale;
            }
        }

        private void Update()
        {
            if (visualTransform == null) return;

            float t = Time.time * bobFrequency;
            float yOffset = Mathf.Sin(t) * bobHeight;
            float scaleY = 1f + Mathf.Sin(t) * scalePulse;

            visualTransform.localPosition = initialPos + new Vector3(0f, yOffset, 0f);
            visualTransform.localScale = new Vector3(initialScale.x, initialScale.y * scaleY, initialScale.z);
        }
    }
}
