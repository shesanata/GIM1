using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using MazeSpell.Audio;

namespace MazeSpell.Gameplay
{
    /// <summary>
    /// Objek Kardus (Cardboard Box) untuk fitur bersembunyi Player bergaya Robbery Bob:
    /// - Player mendekati kardus -> Muncul prompt [E] Sembunyi / Hide.
    /// - Tekan E (atau tombol interaksi HUD) -> Player masuk ke dalam kardus, sprite player tersembunyi.
    /// - Musuh (EnemyVisionController / EnemyCharacter) tidak dapat melihat Player (IsHiding = true).
    /// - Jika musuh sedang mengejar, musuh kehilangan jejak ('?' alert) dan kembali berpatroli.
    /// - Tekan E lagi -> Player keluar dari kardus dan dapat bergerak kembali.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class HidingBox : MonoBehaviour
    {
        [Header("Pengaturan Interaksi")]
        [Tooltip("Jarak maksimal interaksi Player dengan kardus")]
        [SerializeField] private float interactionDistance = 1.35f;

        [Header("Visual & Sprites")]
        [SerializeField] private Sprite boxNormalSprite;
        [SerializeField] private Sprite boxOccupiedSprite;
        [SerializeField] private Sprite promptSprite;

        private SpriteRenderer spriteRenderer;
        private GameObject promptObject;
        private SpriteRenderer promptRenderer;

        // Status runtime
        private bool isPlayerInRange = false;
        private bool isPlayerInside = false;
        private PlayerController activePlayer;
        private SpriteRenderer playerSpriteRenderer;
        private float lastInteractTime = -999f;
        private const float INTERACT_COOLDOWN = 0.25f;

        public bool IsPlayerInside => isPlayerInside;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            LoadSprites();
            CreatePromptObject();
        }

        private void LoadSprites()
        {
#if UNITY_EDITOR
            if (boxNormalSprite == null)
                boxNormalSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/HouseMap/Deco_Cardboard_Box.png");
            if (boxOccupiedSprite == null)
                boxOccupiedSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/HouseMap/Deco_Cardboard_Box_Hiding.png");
            if (promptSprite == null)
                promptSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/HouseMap/Deco_Interact_E_Prompt.png");
#endif
            if (boxNormalSprite != null && spriteRenderer != null && spriteRenderer.sprite == null)
            {
                spriteRenderer.sprite = boxNormalSprite;
            }
        }

        private void CreatePromptObject()
        {
            if (promptObject != null) return;

            promptObject = new GameObject("InteractPrompt");
            promptObject.transform.SetParent(transform, false);
            promptObject.transform.localPosition = new Vector3(0f, 0.75f, -0.2f);
            promptObject.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

            promptRenderer = promptObject.AddComponent<SpriteRenderer>();
            if (promptSprite != null)
            {
                promptRenderer.sprite = promptSprite;
            }
            promptRenderer.sortingOrder = 12;
            promptObject.SetActive(false);
        }

        private void Update()
        {
            CheckPlayerProximity();
            CheckInteractInput();
            AnimatePrompt();
        }

        private void CheckPlayerProximity()
        {
            if (isPlayerInside)
            {
                // Player sudah di dalam, tetap tampilkan prompt untuk keluar
                if (promptObject != null && !promptObject.activeSelf)
                {
                    promptObject.SetActive(true);
                }
                return;
            }

            if (activePlayer == null)
            {
                activePlayer = PlayerController.Instance;
                if (activePlayer == null)
                {
                    GameObject p = GameObject.FindWithTag("Player");
                    if (p != null) activePlayer = p.GetComponent<PlayerController>();
                }
            }

            if (activePlayer == null)
            {
                SetPlayerInRange(false);
                return;
            }

            float dist = Vector2.Distance(transform.position, activePlayer.transform.position);
            bool inRange = dist <= interactionDistance;
            SetPlayerInRange(inRange);
        }

        private void SetPlayerInRange(bool inRange)
        {
            if (isPlayerInRange != inRange)
            {
                isPlayerInRange = inRange;
                if (promptObject != null)
                {
                    promptObject.SetActive(inRange || isPlayerInside);
                }
            }
        }

        private void CheckInteractInput()
        {
            if (!isPlayerInRange && !isPlayerInside) return;
            if (Time.time - lastInteractTime < INTERACT_COOLDOWN) return;

            bool interactPressed = false;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.eKey.wasPressedThisFrame)
            {
                interactPressed = true;
            }

            var gp = Gamepad.current;
            if (gp != null && (gp.buttonNorth.wasPressedThisFrame || gp.buttonWest.wasPressedThisFrame))
            {
                interactPressed = true;
            }
#endif
            if (!interactPressed)
            {
                try
                {
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        interactPressed = true;
                    }
                }
                catch { }
            }

            if (interactPressed)
            {
                lastInteractTime = Time.time;
                ToggleHiding();
            }
        }

        public void ToggleHiding()
        {
            if (activePlayer == null)
            {
                activePlayer = PlayerController.Instance;
                if (activePlayer == null)
                {
                    GameObject p = GameObject.FindWithTag("Player");
                    if (p != null) activePlayer = p.GetComponent<PlayerController>();
                }
            }

            if (activePlayer == null) return;

            if (playerSpriteRenderer == null)
            {
                playerSpriteRenderer = activePlayer.GetComponentInChildren<SpriteRenderer>();
            }

            if (!isPlayerInside)
            {
                // Masuk ke dalam kardus
                EnterBox();
            }
            else
            {
                // Keluar dari kardus
                ExitBox();
            }
        }

        private void EnterBox()
        {
            isPlayerInside = true;
            activePlayer.IsHiding = true;
            activePlayer.IsControlLocked = true;

            // Posisikan player tepat di dalam kardus
            activePlayer.transform.position = new Vector3(transform.position.x, transform.position.y, activePlayer.transform.position.z);

            // Sembunyikan sprite karakter
            if (playerSpriteRenderer != null)
            {
                playerSpriteRenderer.enabled = false;
            }

            // Ganti visual kardus menjadi occupied
            if (spriteRenderer != null && boxOccupiedSprite != null)
            {
                spriteRenderer.sprite = boxOccupiedSprite;
            }

            // Goyang kecil pada kardus saat dimasuki
            StartCoroutine(WobbleRoutine());

            // Play SFX
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.SFXType.ButtonClick, 1.1f);
            }

            // Beritahu semua musuh bahwa player menghilang (hilangkan jejak)
            NotifyEnemiesPlayerHidden();

            Debug.Log("[HidingBox] Player masuk ke dalam kardus! Status IsHiding = TRUE.");
        }

        private void ExitBox()
        {
            isPlayerInside = false;
            activePlayer.IsHiding = false;
            activePlayer.IsControlLocked = false;

            // Tampilkan kembali sprite karakter
            if (playerSpriteRenderer != null)
            {
                playerSpriteRenderer.enabled = true;
            }

            // Kembalikan visual kardus normal
            if (spriteRenderer != null && boxNormalSprite != null)
            {
                spriteRenderer.sprite = boxNormalSprite;
            }

            // Goyang kecil saat keluar
            StartCoroutine(WobbleRoutine());

            // Play SFX
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.SFXType.ButtonClick, 0.9f);
            }

            Debug.Log("[HidingBox] Player keluar dari kardus! Status IsHiding = FALSE.");
        }

        private void NotifyEnemiesPlayerHidden()
        {
            EnemyVisionController[] allVision = FindObjectsByType<EnemyVisionController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var vis in allVision)
            {
                if (vis != null)
                {
                    vis.ForceLosePlayer();
                }
            }
        }

        private IEnumerator WobbleRoutine()
        {
            Vector3 originalScale = transform.localScale;
            float dur = 0.2f;
            float el = 0f;
            while (el < dur)
            {
                el += Time.deltaTime;
                float t = el / dur;
                float offset = Mathf.Sin(t * Mathf.PI * 3f) * 0.12f;
                transform.localScale = new Vector3(originalScale.x + offset, originalScale.y - offset, originalScale.z);
                yield return null;
            }
            transform.localScale = originalScale;
        }

        private void AnimatePrompt()
        {
            if (promptObject != null && promptObject.activeSelf)
            {
                float floatOffset = Mathf.Sin(Time.time * 4f) * 0.06f;
                promptObject.transform.localPosition = new Vector3(0f, 0.75f + floatOffset, -0.2f);
            }
        }
    }
}
