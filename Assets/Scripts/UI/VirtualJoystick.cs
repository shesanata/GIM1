using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace MazeSpell.UI
{
    /// <summary>
    /// Virtual Joystick untuk kontrol gerakan karakter di gameplay MazeSpell.
    /// Mendukung Drag, Touch, dan Mouse pada area layar kiri bawah.
    /// Terhubung langsung dengan PlayerController.VirtualJoystickInput.
    /// Hanya aktif saat scene gameplay (SampleScene).
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("Joystick RectTransforms")]
        [SerializeField] private RectTransform backgroundRect;
        [SerializeField] private RectTransform handleRect;

        [Header("Settings")]
        [SerializeField] private float handleRange = 60f;
        [SerializeField] private float deadZone = 0.05f;

        private Vector2 inputVector = Vector2.zero;
        private Canvas parentCanvas;

        public Vector2 InputVector => inputVector;

        private void Awake()
        {
            if (backgroundRect == null) backgroundRect = GetComponent<RectTransform>();
            if (handleRect == null && transform.childCount > 0)
            {
                handleRect = transform.GetChild(0).GetComponent<RectTransform>();
            }
            parentCanvas = GetComponentInParent<Canvas>();
        }

        private void OnDisable()
        {
            ResetJoystick();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (backgroundRect == null || handleRect == null) return;

            Camera cam = null;
            if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                cam = parentCanvas.worldCamera;
            }

            Vector2 position;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(backgroundRect, eventData.position, cam, out position))
            {
                float radius = handleRange;
                inputVector = position / radius;

                if (inputVector.magnitude > 1f)
                {
                    inputVector = inputVector.normalized;
                }

                handleRect.anchoredPosition = inputVector * radius;

                // Set input ke PlayerController
                if (inputVector.magnitude < deadZone)
                {
                    PlayerController.VirtualJoystickInput = Vector2.zero;
                }
                else
                {
                    PlayerController.VirtualJoystickInput = inputVector;
                }
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ResetJoystick();
        }

        private void ResetJoystick()
        {
            inputVector = Vector2.zero;
            if (handleRect != null) handleRect.anchoredPosition = Vector2.zero;
            PlayerController.VirtualJoystickInput = Vector2.zero;
        }
    }
}
