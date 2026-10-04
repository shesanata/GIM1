using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Controls 2D top-down character movement for MazeSpell.
/// Supports Gamepad / PlayStation Controller (Left Analog Stick & D-Pad)
/// as well as Keyboard (WASD & Arrow Keys) via Unity's New Input System,
/// with fallback to Legacy Input.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Kecepatan gerak karakter di dalam labirin.")]
    [SerializeField] private float moveSpeed = 6f;

    [Tooltip("Deadzone untuk input analog stick controller agar tidak drifting.")]
    [SerializeField] private float analogDeadzone = 0.15f;

    // Component references
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D col;
    private MazeSpell.PlayerVisualAnimator visualAnimator;

    // Runtime state
    public static PlayerController Instance { get; private set; }
    private Vector2 moveInput = Vector2.zero;
    public bool IsControlLocked { get; set; } = false;
    public bool IsHiding { get; set; } = false;
    public float CurrentSlipperiness { get; set; } = 0f;
    public static Vector2 VirtualJoystickInput { get; set; } = Vector2.zero;

    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = Mathf.Max(0f, value);
    }

    public Vector2 CurrentMoveInput => moveInput;

    private void Awake()
    {
        Instance = this;
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        col = GetComponent<Collider2D>();

        // Pastikan rotasi terkunci agar karakter tidak berputar saat menabrak dinding
        rb.freezeRotation = true;

        // Untuk game maze top-down, nonaktifkan gravitasi agar karakter tidak jatuh ke bawah
        rb.gravityScale = 0f;

        // Hubungkan ke PlayerVisualAnimator
        visualAnimator = GetComponent<MazeSpell.PlayerVisualAnimator>();
        if (visualAnimator == null)
        {
            visualAnimator = GetComponentInChildren<MazeSpell.PlayerVisualAnimator>();
        }
        if (visualAnimator == null)
        {
            visualAnimator = gameObject.AddComponent<MazeSpell.PlayerVisualAnimator>();
        }
    }

    private void Update()
    {
        if (IsControlLocked)
        {
            moveInput = Vector2.zero;
            return;
        }

        ReadInput();
        CheckAttackInput();

        // Balikkan sprite secara horizontal sesuai arah gerak kiri/kanan jika visualAnimator tidak aktif
        if (spriteRenderer != null && visualAnimator == null)
        {
            if (moveInput.x > 0.05f)
            {
                spriteRenderer.flipX = false;
            }
            else if (moveInput.x < -0.05f)
            {
                spriteRenderer.flipX = true;
            }
        }
    }

    private void CheckAttackInput()
    {
        if (IsHiding || IsControlLocked) return;

        bool attackPressed = false;

#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null && kb.spaceKey.wasPressedThisFrame)
        {
            attackPressed = true;
        }

        var gp = Gamepad.current;
        if (gp != null && (gp.buttonSouth.wasPressedThisFrame || gp.buttonWest.wasPressedThisFrame))
        {
            attackPressed = true;
        }
#endif

        if (!attackPressed)
        {
            try
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    attackPressed = true;
                }
            }
            catch
            {
                // Fallback aman
            }
        }

        if (attackPressed)
        {
            if (visualAnimator != null)
            {
                Vector2 attackDir = moveInput.sqrMagnitude > 0.01f ? moveInput : Vector2.zero;
                visualAnimator.RequestAttack(attackDir);
            }

            var enemy = MazeSpell.EnemyCharacter.ActiveEnemy;
            if (enemy != null && !enemy.IsDefeated)
            {
                enemy.TryPlayerAttack(gameObject, 1);
            }
        }
    }

    private void FixedUpdate()
    {
        if (IsControlLocked)
        {
            SetLinearVelocity(Vector2.zero);
            return;
        }

        // Terapkan pergerakan 2D (X dan Y) secara halus menggunakan fisika
        Vector2 targetVelocity = moveInput * moveSpeed;
        if (CurrentSlipperiness > 0.01f)
        {
#if UNITY_6000_0_OR_NEWER
            Vector2 currentVel = rb.linearVelocity;
#else
            Vector2 currentVel = rb.velocity;
#endif
            float responsiveness = Mathf.Lerp(25f, 4.5f, Mathf.Clamp01(CurrentSlipperiness));
            Vector2 smoothedVel = Vector2.Lerp(currentVel, targetVelocity, Time.fixedDeltaTime * responsiveness);
            SetLinearVelocity(smoothedVel);
        }
        else
        {
            SetLinearVelocity(targetVelocity);
        }
    }

    /// <summary>
    /// Membaca input dari Virtual Joystick, Gamepad (Stik PS / Controller), dan Keyboard.
    /// </summary>
    private void ReadInput()
    {
        Vector2 input = Vector2.zero;

        // 0. Virtual Joystick UI (Mobile / Touch / Mouse Drag)
        if (VirtualJoystickInput.sqrMagnitude > 0.01f)
        {
            input = VirtualJoystickInput;
        }

#if ENABLE_INPUT_SYSTEM
        // 1. Prioritas Utama: Gamepad / Stik PS (Left Stick & D-Pad)
        var gamepad = Gamepad.current;
        if (gamepad != null)
        {
            // A. Left Analog Stick (Gerak Analog Presisi)
            Vector2 stick = gamepad.leftStick.ReadValue();
            if (stick.magnitude > analogDeadzone)
            {
                input = stick;
            }
            else
            {
                // B. D-Pad (Alternatif Tombol Arah Digital)
                Vector2 dpad = gamepad.dpad.ReadValue();
                if (dpad.sqrMagnitude > 0.01f)
                {
                    input = dpad;
                }
            }
        }

        // 2. Kontrol Alternatif: Keyboard (WASD & Arrow Keys untuk PC Testing)
        var kb = Keyboard.current;
        if (kb != null)
        {
            Vector2 kbInput = Vector2.zero;

            // Horizontal (A / D atau Panah Kiri / Kanan)
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) kbInput.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) kbInput.x += 1f;

            // Vertikal (W / S atau Panah Atas / Bawah)
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) kbInput.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) kbInput.y -= 1f;

            // Jika ada input keyboard dan gamepad sedang tidak digerakkan, gunakan keyboard
            if (kbInput.sqrMagnitude > 0.01f && input.sqrMagnitude <= 0.01f)
            {
                input = kbInput.normalized;
            }
        }

        // 3. Fallback jika New Input System tidak mendeteksi perangkat aktif
        if (input.sqrMagnitude <= 0.01f && gamepad == null && kb == null)
#endif
        {
            try
            {
                float h = Input.GetAxisRaw("Horizontal");
                float v = Input.GetAxisRaw("Vertical");
                Vector2 legacy = new Vector2(h, v);
                if (legacy.sqrMagnitude > 1f)
                {
                    legacy.Normalize();
                }
                input = legacy;
            }
            catch
            {
                // Fallback aman jika legacy input dinonaktifkan di Player Settings
            }
        }

        // Batasi magnitude maksimum = 1 agar gerakan diagonal tidak melebihi kecepatan lurus
        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        moveInput = input;
    }

    private void SetLinearVelocity(Vector2 vel)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = vel;
#else
        rb.velocity = vel;
#endif
    }
}
