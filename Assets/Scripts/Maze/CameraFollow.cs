using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MazeSpell
{
    /// <summary>
    /// Mengontrol kamera gameplay agar:
    /// 1. Mengikuti posisi Player secara halus (smooth) di LateUpdate.
    /// 2. Memungkinkan pemain menggeser / men-drag kamera (Free Look / Map Peek) menggunakan Mouse atau Touchscreen
    ///    untuk melihat seluruh ruangan, denah rumah, posisi musuh, atau target huruf di sekitar labirin.
    /// 3. Otomatis kembali fokus ke Player begitu pemain bergerak (WASD, Arrow Keys, Joystick, Gamepad)
    ///    atau setelah idle beberapa detik.
    /// 4. Dilengkapi pembatasan bounds labirin agar kamera tidak keluar terlalu jauh dari area permainan.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        public static CameraFollow Instance { get; private set; }

        [Header("Target Settings")]
        [Tooltip("Transform Player yang akan diikuti kamera")]
        [SerializeField] private Transform target;

        [Tooltip("Offset posisi kamera terhadap target (X, Y)")]
        [SerializeField] private Vector2 targetOffset = Vector2.zero;

        [Header("Smoothness Settings")]
        [Tooltip("Waktu redaman pergerakan kamera saat mengikuti Player (default 0.20s)")]
        [Range(0.05f, 0.8f)]
        [SerializeField] private float smoothTime = 0.20f;

        [Tooltip("Waktu redaman pergerakan kamera saat di-drag / digeser manual")]
        [Range(0.02f, 0.5f)]
        [SerializeField] private float panSmoothTime = 0.10f;

        [Tooltip("Kecepatan maksimal perpindahan kamera")]
        [SerializeField] private float maxSpeed = 40f;

        [Header("Map Drag & Free Look Settings")]
        [Tooltip("Aktifkan fitur geser/drag kamera untuk melihat area sekitar")]
        [SerializeField] private bool enableMapDrag = true;

        [Tooltip("Sensitivitas geser kamera")]
        [SerializeField] private float dragSensitivity = 1.0f;

        [Tooltip("Durasi diam (detik) setelah melepas drag sebelum kamera otomatis kembali fokus ke Player")]
        [SerializeField] private float autoRecenterDelay = 2.5f;

        [Tooltip("Aktifkan zoom kamera menggunakan mouse scroll wheel")]
        [SerializeField] private bool enableScrollZoom = true;

        [Tooltip("Ukuran zoom default kamera (orthographic size)")]
        [SerializeField] private float defaultOrthoSize = 5.2f;

        [Tooltip("Batas minimum dan maksimum zoom kamera")]
        [SerializeField] private Vector2 zoomRange = new Vector2(3.8f, 7.2f);

        [Header("Maze Bounds Settings")]
        [Tooltip("Aktifkan pembatasan posisi kamera di sekitar labirin")]
        [SerializeField] private bool useBounds = true;

        [Tooltip("Batas area labirin dalam koordinat dunia (Rect: X, Y, Width, Height)")]
        [SerializeField] private Rect mazeBounds = new Rect(0f, 0f, 22f, 18f);

        [Tooltip("Toleransi padding luar labirin agar pergerakan kamera tetap leluasa namun tidak memperlihatkan area luar terlalu jauh")]
        [SerializeField] private float outsidePadding = 3.0f;

        // Internal state
        private Camera cam;
        private Vector3 currentVelocity = Vector3.zero;
        private float lockedZ = -10f;
        private bool isInitialized = false;

        // Panning / Free Look Runtime State
        private bool isDragging = false;
        private bool isPannedAway = false;
        private Vector2 dragStartScreenPos = Vector2.zero;
        private Vector3 dragStartCamPos = Vector3.zero;
        private Vector3 targetPanPosition = Vector3.zero;
        private float recenterTimer = 0f;
        private int activeTouchId = -1;

        public Transform Target
        {
            get => target;
            set
            {
                target = value;
                if (target != null && !isInitialized)
                {
                    SnapToTarget();
                }
            }
        }

        public float SmoothTime
        {
            get => smoothTime;
            set => smoothTime = Mathf.Clamp(value, 0.05f, 0.8f);
        }

        public bool UseBounds
        {
            get => useBounds;
            set => useBounds = value;
        }

        public Rect MazeBounds
        {
            get => mazeBounds;
            set => mazeBounds = value;
        }

        public bool EnableMapDrag
        {
            get => enableMapDrag;
            set => enableMapDrag = value;
        }

        public bool IsPannedAway => isPannedAway;
        public bool IsDragging => isDragging;

        private void Awake()
        {
            Instance = this;

            // Pengaman: Kamera follow HANYA aktif di scene gameplay/SampleScene
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == "MainMenu" || sceneName == "LevelSelect" || sceneName == "SplashScreen")
            {
                enabled = false;
                return;
            }

            cam = GetComponent<Camera>();
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = defaultOrthoSize;
                lockedZ = transform.position.z;
            }

            // Kunci rotasi kamera top-down agar stabil
            transform.rotation = Quaternion.identity;
        }

        private void OnEnable()
        {
            Instance = this;
            if (LevelProgressionManager.Instance != null)
            {
                LevelProgressionManager.Instance.onLevelStarted.AddListener(OnLevelStarted);
            }
        }

        private void OnDisable()
        {
            if (Instance == this) Instance = null;
            if (LevelProgressionManager.Instance != null)
            {
                LevelProgressionManager.Instance.onLevelStarted.RemoveListener(OnLevelStarted);
            }
        }

        private void Start()
        {
            if (target == null)
            {
                FindPlayerTarget();
            }

            AutoDetectMazeBounds();

            if (target != null)
            {
                SnapToTarget();
                isInitialized = true;
            }
        }

        /// <summary>
        /// Mencari GameObject Player di scene secara otomatis jika referensi target belum terpasang.
        /// </summary>
        public void FindPlayerTarget()
        {
            if (target != null) return;

            // 1. Berdasarkan Instance PlayerController
            if (PlayerController.Instance != null)
            {
                target = PlayerController.Instance.transform;
                return;
            }

            // 2. Berdasarkan Tag "Player"
            GameObject playerObj = GameObject.FindWithTag("Player");

            // 3. Berdasarkan komponen PlayerController
            if (playerObj == null)
            {
#if UNITY_2023_1_OR_NEWER
                PlayerController pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
#else
                PlayerController pc = UnityEngine.Object.FindObjectOfType<PlayerController>();
#endif
                if (pc != null)
                {
                    playerObj = pc.gameObject;
                }
            }

            // 4. Berdasarkan nama objek "Player"
            if (playerObj == null)
            {
                playerObj = GameObject.Find("Player");
            }

            if (playerObj != null)
            {
                target = playerObj.transform;
            }
        }

        /// <summary>
        /// Mengatur bounds labirin berdasarkan data level aktif atau grid di scene.
        /// </summary>
        public void AutoDetectMazeBounds()
        {
            // Coba ambil data dari LevelProgressionManager
            if (LevelProgressionManager.Instance != null)
            {
                MazeLevelData currentData = LevelProgressionManager.Instance.CurrentData;
                if (currentData != null)
                {
                    SetMazeBounds(currentData.TotalWidth, currentData.TotalHeight, Vector2.zero);
                    return;
                }
            }

            // Coba cari dari MazeGridBounds yang ada di scene
#if UNITY_2023_1_OR_NEWER
            MazeGridBounds gridBounds = UnityEngine.Object.FindAnyObjectByType<MazeGridBounds>();
#else
            MazeGridBounds gridBounds = UnityEngine.Object.FindObjectOfType<MazeGridBounds>();
#endif
            if (gridBounds != null)
            {
                SetMazeBounds(gridBounds.TotalWidth, gridBounds.TotalHeight, gridBounds.transform.position);
                return;
            }

            // Fallback default ukuran rumah kartun yang luas (22x18)
            SetMazeBounds(22f, 18f, Vector2.zero);
        }

        /// <summary>
        /// Memperbarui batas labirin dari ukuran dan origin dunia.
        /// </summary>
        public void SetMazeBounds(float width, float height, Vector2 origin)
        {
            mazeBounds = new Rect(origin.x, origin.y, Mathf.Max(width, 10f), Mathf.Max(height, 10f));
        }

        /// <summary>
        /// Dipanggil saat level baru dimulai.
        /// </summary>
        private void OnLevelStarted(int levelNumber)
        {
            FindPlayerTarget();

            if (LevelProgressionManager.Instance != null)
            {
                var config = LevelProgressionManager.Instance.GetConfigForLevel(levelNumber);
                if (config != null && config.levelData != null)
                {
                    SetMazeBounds(config.levelData.TotalWidth, config.levelData.TotalHeight, Vector2.zero);
                }
            }

            isPannedAway = false;
            isDragging = false;
            SnapToTarget();
        }

        /// <summary>
        /// Menggerakkan kamera langsung ke posisi Player tanpa smoothing.
        /// </summary>
        public void SnapToTarget()
        {
            if (target == null)
            {
                FindPlayerTarget();
            }

            if (target == null) return;

            isPannedAway = false;
            isDragging = false;
            Vector3 desired = CalculateDesiredPlayerPosition();
            transform.position = desired;
            targetPanPosition = desired;
            currentVelocity = Vector3.zero;
        }

        /// <summary>
        /// Mengembalikan fokus kamera ke Player secara halus.
        /// </summary>
        public void RecenterOnPlayer()
        {
            isPannedAway = false;
            isDragging = false;
            recenterTimer = 0f;
        }

        private void Update()
        {
            HandleScrollZoom();
            HandleDragAndPanInput();
            HandleAutoRecenter();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                FindPlayerTarget();
                if (target == null && !isPannedAway) return;
            }

            // Kunci rotasi kamera top-down agar selalu stabil
            transform.rotation = Quaternion.identity;

            // Tentukan target posisi: posisi pemain (Follow mode) atau posisi geser (Pan mode)
            Vector3 desiredPosition;
            float currentSmooth = smoothTime;

            if (isPannedAway)
            {
                desiredPosition = targetPanPosition;
                currentSmooth = panSmoothTime;
            }
            else
            {
                desiredPosition = CalculateDesiredPlayerPosition();
                targetPanPosition = desiredPosition;
                currentSmooth = smoothTime;
            }

            // Batasi posisi kamera dengan bounds
            if (useBounds && mazeBounds.width > 0f && mazeBounds.height > 0f)
            {
                desiredPosition = ClampToBounds(desiredPosition);
            }

            // Interpolasi pergerakan kamera secara halus
            Vector3 newPosition = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref currentVelocity,
                currentSmooth,
                maxSpeed,
                Time.deltaTime
            );

            newPosition.z = lockedZ;
            transform.position = newPosition;
        }

        /// <summary>
        /// Menangani input geser / pan kamera melalui Mouse dan Touchscreen.
        /// </summary>
        private void HandleDragAndPanInput()
        {
            if (!enableMapDrag || cam == null) return;

            // 1. Cek apakah ada input pergerakan karakter (WASD, Joystick, Gamepad)
            // Jika pemain menggerakkan karakter, otomatis kembalikan fokus kamera ke pemain
            if (HasPlayerMovementInput())
            {
                isPannedAway = false;
                isDragging = false;
                activeTouchId = -1;
                return;
            }

            // 2. Deteksi Touch Input (Layar Sentuh / Mobile)
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    UnityEngine.Touch touch = Input.GetTouch(i);

                    // Abaikan touch yang berada di atas UI / Tombol / Joystick
                    if (IsPointerOverUI(touch.position, touch.fingerId))
                    {
                        continue;
                    }

                    if (touch.phase == UnityEngine.TouchPhase.Began && !isDragging)
                    {
                        isDragging = true;
                        isPannedAway = true;
                        activeTouchId = touch.fingerId;
                        dragStartScreenPos = touch.position;
                        dragStartCamPos = transform.position;
                        targetPanPosition = transform.position;
                        recenterTimer = autoRecenterDelay;
                    }
                    else if (touch.phase == UnityEngine.TouchPhase.Moved && isDragging && touch.fingerId == activeTouchId)
                    {
                        Vector2 deltaScreen = touch.position - dragStartScreenPos;
                        float unitsPerPixel = (cam.orthographicSize * 2f) / Screen.height;
                        Vector3 worldDelta = new Vector3(deltaScreen.x * unitsPerPixel * dragSensitivity, deltaScreen.y * unitsPerPixel * dragSensitivity, 0f);

                        targetPanPosition = dragStartCamPos - worldDelta;
                        if (useBounds) targetPanPosition = ClampToBounds(targetPanPosition);
                        recenterTimer = autoRecenterDelay;
                    }
                    else if ((touch.phase == UnityEngine.TouchPhase.Ended || touch.phase == UnityEngine.TouchPhase.Canceled) && touch.fingerId == activeTouchId)
                    {
                        isDragging = false;
                        activeTouchId = -1;
                        recenterTimer = autoRecenterDelay;
                    }
                }
                return;
            }

            // 3. Deteksi Mouse Input (Desktop / Editor)
            bool isMouseDown = false;
            bool isMouseHeld = false;
            bool isMouseUp = false;
            Vector2 mousePos = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse != null)
            {
                isMouseDown = mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame || mouse.middleButton.wasPressedThisFrame;
                isMouseHeld = mouse.leftButton.isPressed || mouse.rightButton.isPressed || mouse.middleButton.isPressed;
                isMouseUp = mouse.leftButton.wasReleasedThisFrame || mouse.rightButton.wasReleasedThisFrame || mouse.middleButton.wasReleasedThisFrame;
                mousePos = mouse.position.ReadValue();
            }
#endif
            if (!isMouseDown && !isMouseHeld && !isMouseUp)
            {
                try
                {
                    isMouseDown = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);
                    isMouseHeld = Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2);
                    isMouseUp = Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2);
                    mousePos = Input.mousePosition;
                }
                catch
                {
                    // Fallback
                }
            }

            if (isMouseDown)
            {
                if (!IsPointerOverUI(mousePos))
                {
                    isDragging = true;
                    isPannedAway = true;
                    dragStartScreenPos = mousePos;
                    dragStartCamPos = transform.position;
                    targetPanPosition = transform.position;
                    recenterTimer = autoRecenterDelay;
                }
            }
            else if (isMouseHeld && isDragging)
            {
                Vector2 deltaScreen = mousePos - dragStartScreenPos;
                float unitsPerPixel = (cam.orthographicSize * 2f) / Screen.height;
                Vector3 worldDelta = new Vector3(deltaScreen.x * unitsPerPixel * dragSensitivity, deltaScreen.y * unitsPerPixel * dragSensitivity, 0f);

                targetPanPosition = dragStartCamPos - worldDelta;
                if (useBounds) targetPanPosition = ClampToBounds(targetPanPosition);
                recenterTimer = autoRecenterDelay;
            }
            else if (isMouseUp && isDragging)
            {
                isDragging = false;
                recenterTimer = autoRecenterDelay;
            }
        }

        /// <summary>
        /// Menangani timer kembali ke Player secara otomatis setelah pemain selesai melihat peta.
        /// </summary>
        private void HandleAutoRecenter()
        {
            if (isPannedAway && !isDragging)
            {
                if (autoRecenterDelay > 0f)
                {
                    recenterTimer -= Time.deltaTime;
                    if (recenterTimer <= 0f)
                    {
                        isPannedAway = false;
                    }
                }
            }
        }

        /// <summary>
        /// Menangani zoom in / zoom out kamera menggunakan mouse scroll wheel.
        /// </summary>
        private void HandleScrollZoom()
        {
            if (!enableScrollZoom || cam == null) return;

            float scroll = 0f;
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse != null)
            {
                scroll = mouse.scroll.ReadValue().y * 0.01f;
            }
#endif
            if (Mathf.Approximately(scroll, 0f))
            {
                try
                {
                    scroll = Input.GetAxis("Mouse ScrollWheel");
                }
                catch {}
            }

            if (Mathf.Abs(scroll) > 0.001f)
            {
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - (scroll * 2.5f), zoomRange.x, zoomRange.y);
            }
        }

        /// <summary>
        /// Memeriksa apakah pemain sedang menginput gerakan (WASD, Joystick, Gamepad).
        /// </summary>
        private bool HasPlayerMovementInput()
        {
            if (PlayerController.Instance != null && PlayerController.Instance.CurrentMoveInput.sqrMagnitude > 0.01f)
            {
                return true;
            }

            if (PlayerController.VirtualJoystickInput.sqrMagnitude > 0.01f)
            {
                return true;
            }

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.aKey.isPressed || kb.sKey.isPressed || kb.dKey.isPressed ||
                    kb.upArrowKey.isPressed || kb.downArrowKey.isPressed || kb.leftArrowKey.isPressed || kb.rightArrowKey.isPressed)
                {
                    return true;
                }
            }

            var gp = Gamepad.current;
            if (gp != null)
            {
                if (gp.leftStick.ReadValue().sqrMagnitude > 0.05f || gp.dpad.ReadValue().sqrMagnitude > 0.05f)
                {
                    return true;
                }
            }
#endif

            try
            {
                if (Input.GetAxisRaw("Horizontal") != 0f || Input.GetAxisRaw("Vertical") != 0f)
                {
                    return true;
                }
            }
            catch {}

            return false;
        }

        /// <summary>
        /// Memeriksa apakah pointer berada di atas elemen UI Canvas.
        /// </summary>
        private bool IsPointerOverUI(Vector2 screenPos, int fingerId = -1)
        {
            if (EventSystem.current == null) return false;

            if (fingerId >= 0)
            {
                return EventSystem.current.IsPointerOverGameObject(fingerId);
            }

            return EventSystem.current.IsPointerOverGameObject();
        }

        /// <summary>
        /// Menghitung posisi kamera yang ideal untuk mengikuti Player.
        /// </summary>
        private Vector3 CalculateDesiredPlayerPosition()
        {
            if (target == null) return transform.position;

            Vector3 desired = new Vector3(
                target.position.x + targetOffset.x,
                target.position.y + targetOffset.y,
                lockedZ
            );

            if (useBounds && mazeBounds.width > 0f && mazeBounds.height > 0f)
            {
                desired = ClampToBounds(desired);
            }

            return desired;
        }

        /// <summary>
        /// Membatasi posisi kamera agar tetap berada di area denah labirin / rumah.
        /// </summary>
        private Vector3 ClampToBounds(Vector3 targetPos)
        {
            float halfHeight = cam != null ? cam.orthographicSize : 5f;
            float halfWidth = cam != null ? halfHeight * cam.aspect : halfHeight * (16f / 9f);

            // Hitung rentang horizontal (X) yang diizinkan
            float minAllowedX = mazeBounds.xMin + Mathf.Min(halfWidth, mazeBounds.width * 0.5f) - outsidePadding;
            float maxAllowedX = mazeBounds.xMax - Mathf.Min(halfWidth, mazeBounds.width * 0.5f) + outsidePadding;

            if (minAllowedX <= maxAllowedX)
            {
                targetPos.x = Mathf.Clamp(targetPos.x, minAllowedX, maxAllowedX);
            }
            else
            {
                targetPos.x = mazeBounds.center.x;
            }

            // Hitung rentang vertikal (Y) yang diizinkan
            float minAllowedY = mazeBounds.yMin + Mathf.Min(halfHeight, mazeBounds.height * 0.5f) - outsidePadding;
            float maxAllowedY = mazeBounds.yMax - Mathf.Min(halfHeight, mazeBounds.height * 0.5f) + outsidePadding;

            if (minAllowedY <= maxAllowedY)
            {
                targetPos.y = Mathf.Clamp(targetPos.y, minAllowedY, maxAllowedY);
            }
            else
            {
                targetPos.y = mazeBounds.center.y;
            }

            return targetPos;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!useBounds) return;

            Gizmos.color = Color.yellow;
            Vector3 center = new Vector3(mazeBounds.center.x, mazeBounds.center.y, 0f);
            Vector3 size = new Vector3(mazeBounds.width, mazeBounds.height, 0.1f);
            Gizmos.DrawWireCube(center, size);

            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.5f);
            Vector3 paddedSize = new Vector3(mazeBounds.width + (outsidePadding * 2f), mazeBounds.height + (outsidePadding * 2f), 0.1f);
            Gizmos.DrawWireCube(center, paddedSize);
        }
#endif
    }
}
