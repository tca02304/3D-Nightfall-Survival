
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DefaultExecutionOrder(-100)]
public class FirstPersonCamera : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private Transform cameraEffects;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private CharacterController controller;

    // =========================================================
    // MOUSE LOOK
    // =========================================================

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float minPitch = -85f;
    [SerializeField] private float maxPitch = 85f;
    [SerializeField] private bool invertMouseY = false;

    [Tooltip("Hệ số chuyển đổi Mouse Delta của New Input System")]
    [SerializeField] private float inputSystemMouseScale = 0.02f;

    // =========================================================
    // FIELD OF VIEW
    // =========================================================

    [Header("Field Of View")]
    [SerializeField] private float fieldOfView = 85f;

    // =========================================================
    // CAMERA BOBBING
    // =========================================================

    [Header("Camera Bobbing")]
    [SerializeField] private bool enableBobbing = true;
    [SerializeField] private float walkFrequency = 8f;
    [SerializeField] private float runFrequency = 12f;
    [SerializeField] private float walkAmount = 0.025f;
    [SerializeField] private float runAmount = 0.045f;
    [SerializeField] private float runThreshold = 5f;
    [SerializeField] private float bobSmooth = 8f;

    // =========================================================
    // CAMERA SHAKE
    // =========================================================

    [Header("Camera Shake")]
    [SerializeField] private bool enableShake = true;
    [SerializeField] private float shakeRecoverySpeed = 15f;

    // =========================================================
    // INTERNAL STATE
    // =========================================================

    private float pitch;
    private float bobTimer;

    private Vector3 startPosition;
    private Vector3 bobOffset;
    private Vector3 shakeOffset;

    private float shakeTimer;
    private float shakeDuration;
    private float shakeMagnitude;

    public float MouseX { get; private set; }
    public float MouseY { get; private set; }

    // =========================================================
    // INITIALIZATION
    // =========================================================

    private void Awake()
    {
        if (cameraEffects != null)
            startPosition = cameraEffects.localPosition;

        if (cameraPivot != null)
            pitch = Mathf.DeltaAngle(
                0f,
                cameraPivot.localEulerAngles.x
            );
    }

    private void Start()
    {
        SetFOV(fieldOfView);
        LockCursor();
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        HandleCursor();

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            HandleMouseLook();
            HandleBobbing();
        }
        else
        {
            MouseX = 0f;
            MouseY = 0f;

            bobOffset = Vector3.Lerp(
                bobOffset,
                Vector3.zero,
                Time.deltaTime * bobSmooth
            );
        }

        HandleShake();
    }

    private void LateUpdate()
    {
        ApplyCameraEffects();
    }

    // =========================================================
    // INPUT
    // =========================================================

    private Vector2 ReadMouseDelta()
    {
#if ENABLE_INPUT_SYSTEM

        Mouse mouse = Mouse.current;

        if (mouse == null)
            return Vector2.zero;

        return mouse.delta.ReadValue() * inputSystemMouseScale;

#elif ENABLE_LEGACY_INPUT_MANAGER

        return new Vector2(
            Input.GetAxisRaw("Mouse X"),
            Input.GetAxisRaw("Mouse Y")
        );

#else

        return Vector2.zero;

#endif
    }

    private void HandleMouseLook()
    {
        Vector2 delta = ReadMouseDelta() * mouseSensitivity;

        MouseX = delta.x;
        MouseY = invertMouseY ? -delta.y : delta.y;

        // Xoay ngang Player
        transform.Rotate(
            Vector3.up * MouseX,
            Space.Self
        );

        // Xoay dọc Camera
        pitch -= MouseY;

        pitch = Mathf.Clamp(
            pitch,
            minPitch,
            maxPitch
        );

        if (cameraPivot != null)
        {
            cameraPivot.localRotation =
                Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    // =========================================================
    // CAMERA BOBBING
    // =========================================================

    private void HandleBobbing()
    {
        if (!enableBobbing || controller == null)
        {
            bobOffset = Vector3.zero;
            return;
        }

        Vector3 velocity = controller.velocity;
        velocity.y = 0f;

        float speed = velocity.magnitude;

        bool moving = controller.isGrounded && speed > 0.1f;

        if (!moving)
        {
            bobOffset = Vector3.Lerp(
                bobOffset,
                Vector3.zero,
                Time.deltaTime * bobSmooth
            );

            return;
        }

        bool running = speed > runThreshold;

        float frequency = running
            ? runFrequency
            : walkFrequency;

        float amount = running
            ? runAmount
            : walkAmount;

        bobTimer += Time.deltaTime * frequency;

        float x = Mathf.Cos(bobTimer * 0.5f) * amount * 0.5f;

        float y = Mathf.Abs(Mathf.Sin(bobTimer)) * amount;

        Vector3 target = new Vector3(x, y, 0f);

        bobOffset = Vector3.Lerp(
            bobOffset,
            target,
            Time.deltaTime * bobSmooth
        );
    }

    // =========================================================
    // CAMERA SHAKE
    // =========================================================

    private void HandleShake()
    {
        if (!enableShake || shakeTimer <= 0f)
        {
            shakeOffset = Vector3.Lerp(
                shakeOffset,
                Vector3.zero,
                Time.deltaTime * shakeRecoverySpeed
            );

            return;
        }

        shakeTimer -= Time.deltaTime;

        float fade = shakeDuration > 0f
            ? Mathf.Clamp01(shakeTimer / shakeDuration)
            : 0f;

        shakeOffset =
            Random.insideUnitSphere *
            shakeMagnitude *
            fade;
    }

    public void ShakeCamera(float duration, float magnitude)
    {
        if (!enableShake || duration <= 0f || magnitude <= 0f)
            return;

        shakeDuration = duration;
        shakeTimer = duration;
        shakeMagnitude = magnitude;
    }

    // =========================================================
    // APPLY CAMERA EFFECTS
    // =========================================================

    private void ApplyCameraEffects()
    {
        if (cameraEffects == null)
            return;

        cameraEffects.localPosition =
            startPosition + bobOffset + shakeOffset;
    }

    // =========================================================
    // CURSOR
    // =========================================================

    private void HandleCursor()
    {
        bool unlockPressed = false;
        bool lockPressed = false;

#if ENABLE_INPUT_SYSTEM

        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (keyboard != null)
            unlockPressed = keyboard.escapeKey.wasPressedThisFrame;

        if (mouse != null)
            lockPressed = mouse.leftButton.wasPressedThisFrame;

#elif ENABLE_LEGACY_INPUT_MANAGER

        unlockPressed = Input.GetKeyDown(KeyCode.Escape);
        lockPressed = Input.GetMouseButtonDown(0);

#endif

        if (unlockPressed)
            UnlockCursor();
        else if (lockPressed)
            LockCursor();
    }

    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // =========================================================
    // SETTINGS API
    // =========================================================

    public void SetSensitivity(float value)
    {
        mouseSensitivity = Mathf.Max(0f, value);
    }

    public void SetFOV(float value)
    {
        fieldOfView = Mathf.Clamp(value, 60f, 110f);

        if (playerCamera != null)
            playerCamera.fieldOfView = fieldOfView;
    }

    public void SetInvertMouseY(bool value)
    {
        invertMouseY = value;
    }

    public void SetBobbing(bool value)
    {
        enableBobbing = value;

        if (!value)
            bobOffset = Vector3.zero;
    }

    public void SetCameraShake(bool value)
    {
        enableShake = value;

        if (!value)
        {
            shakeTimer = 0f;
            shakeOffset = Vector3.zero;
        }
    }
}