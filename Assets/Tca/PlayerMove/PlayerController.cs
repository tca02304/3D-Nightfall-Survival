using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DucThanh
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float sprintSpeed = 8f;
        [SerializeField] private float groundAcceleration = 45f;
        [SerializeField] private float groundDeceleration = 55f;
        [SerializeField] private float airAcceleration = 12f;
        [Tooltip("Bật nếu chỉ muốn cho phép di chuyển khi chuột đã bị khóa (CursorLockMode.Locked)")]
        [SerializeField] private bool requireCursorLocked = false;

        [Header("Jump & Gravity")]
        [SerializeField] private float jumpHeight = 2.2f;
        [SerializeField] private float gravity = -25f;
        [SerializeField] private float fallGravityMultiplier = 2.2f;
        [SerializeField] private float maxFallSpeed = 40f;
        [SerializeField] private float coyoteTime = 0.12f;
        [SerializeField] private float jumpBufferTime = 0.12f;
        [SerializeField] private float groundStickForce = 2f;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private bool freezeAnimationOnDeath = true;

        // Components
        private CharacterController characterController;
        private PlayerStats playerStats;

        // Movement State
        private Vector2 moveInput;
        private Vector3 horizontalVelocity;
        private Vector3 groundNormal = Vector3.up;
        private float verticalVelocity;
        private float lastGroundedTime = -999f;
        private float lastJumpPressedTime = -999f;
        private bool isSprinting;

        // Animator Hashes
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int GroundedHash = Animator.StringToHash("Grounded");
        private static readonly int JumpingHash = Animator.StringToHash("Jumping");
        private static readonly int FallSpeedHash = Animator.StringToHash("FallSpeed");
        private static readonly int AnimationSpeedHash = Animator.StringToHash("AnimationSpeed");

        // Public API
        public CharacterController CharacterController => characterController;
        public Animator Animator => animator;
        public bool IsGrounded => characterController != null && characterController.isGrounded;
        public Vector3 Velocity => characterController != null ? characterController.velocity : Vector3.zero;
        public bool IsMoving => moveInput.sqrMagnitude > 0.01f;
        public bool IsSprinting => isSprinting;
        private bool IsDead => playerStats != null && playerStats.IsDead;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            playerStats = GetComponent<PlayerStats>();
            if (animator == null) animator = GetComponentInChildren<Animator>();

            // Khóa Rigidbody nếu có gắn kèm để tránh xung đột kéo nhân vật xuyên map
            if (TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        private void OnEnable()
        {
            if (playerStats != null) playerStats.OnPlayerDied += HandlePlayerDied;
        }

        private void OnDisable()
        {
            if (playerStats != null) playerStats.OnPlayerDied -= HandlePlayerDied;
        }

        private void Update()
        {
            GatherInput();
            HandleMovement();
            UpdateAnimation();
        }

        private void GatherInput()
        {
            moveInput = Vector2.zero;
            isSprinting = false;

            if (IsDead || Time.timeScale == 0f)
                return;

            if (requireCursorLocked && Cursor.lockState != CursorLockMode.Locked)
                return;

            float horizontal = 0f;
            float vertical = 0f;
            bool sprintHeld = false;
            bool jumpPressed = false;

#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) vertical += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) vertical -= 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) horizontal -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) horizontal += 1f;

                sprintHeld = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
                jumpPressed = kb.spaceKey.wasPressedThisFrame;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Mathf.Approximately(horizontal, 0f) && Mathf.Approximately(vertical, 0f))
            {
                horizontal = Input.GetAxisRaw("Horizontal");
                vertical = Input.GetAxisRaw("Vertical");
            }
            if (!sprintHeld)
                sprintHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (!jumpPressed)
                jumpPressed = Input.GetKeyDown(KeyCode.Space);
#endif

            moveInput = Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);
            isSprinting = sprintHeld && IsMoving && (playerStats == null || playerStats.CanSprint);

            if (jumpPressed) lastJumpPressedTime = Time.time;
        }

        private void HandleMovement()
        {
            if (characterController == null || !characterController.enabled) return;

            // Giới hạn dt tối đa 0.05s để tránh giật frame đầu làm nhân vật lún xuyên đất
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            bool grounded = characterController.isGrounded;
            if (grounded) lastGroundedTime = Time.time;

            grounded = TryJump(grounded);

            Vector3 wishDir = Vector3.ClampMagnitude(transform.right * moveInput.x + transform.forward * moveInput.y, 1f);
            float targetSpeed = isSprinting ? sprintSpeed : walkSpeed;
            Vector3 targetVelocity = wishDir * targetSpeed;

            float acceleration = !grounded
                ? airAcceleration
                : (targetVelocity.sqrMagnitude > 0.0001f ? groundAcceleration : groundDeceleration);

            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, acceleration * dt);
            ApplyGravityAndMove(grounded, dt);
        }

        private bool TryJump(bool grounded)
        {
            bool jumpBuffered = Time.time - lastJumpPressedTime <= jumpBufferTime;
            bool canJump = Time.time - lastGroundedTime <= coyoteTime;

            if (jumpBuffered && canJump && !IsDead)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                lastJumpPressedTime = -999f;
                lastGroundedTime = -999f;
                return false;
            }

            return grounded;
        }

        private void ApplyGravityAndMove(bool grounded, float dt)
        {
            float motionY;

            if (grounded && verticalVelocity <= 0f)
            {
                verticalVelocity = -groundStickForce;
                motionY = verticalVelocity;

                if (groundNormal.y > 0.01f)
                {
                    float slopeY = -(groundNormal.x * horizontalVelocity.x + groundNormal.z * horizontalVelocity.z) / groundNormal.y;
                    if (slopeY < 0f) motionY += slopeY;
                }
            }
            else
            {
                float prevVelocity = verticalVelocity;
                float currentGravity = verticalVelocity < 0f ? gravity * fallGravityMultiplier : gravity;
                verticalVelocity = Mathf.Max(verticalVelocity + currentGravity * dt, -maxFallSpeed);
                motionY = (prevVelocity + verticalVelocity) * 0.5f;
            }

            groundNormal = Vector3.up;

            Vector3 motion = new Vector3(horizontalVelocity.x, motionY, horizontalVelocity.z) * dt;
            CollisionFlags flags = characterController.Move(motion);

            if ((flags & CollisionFlags.Sides) != 0)
            {
                Vector3 actual = characterController.velocity;
                horizontalVelocity = new Vector3(actual.x, 0f, actual.z);
            }

            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
                verticalVelocity = 0f;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.normal.y > 0.2f) groundNormal = hit.normal;
        }

        private void UpdateAnimation()
        {
            if (animator == null) return;

            if (IsDead && freezeAnimationOnDeath)
            {
                animator.speed = 0f;
                return;
            }

            if (animator.speed == 0f) animator.speed = 1f;

            float animSpeed = Mathf.Clamp(horizontalVelocity.magnitude / Mathf.Max(0.01f, walkSpeed), 0f, 1.5f);
            bool grounded = Time.time - lastGroundedTime <= 0.08f;

            animator.SetFloat(SpeedHash, animSpeed, 0.05f, Time.deltaTime);
            animator.SetBool(GroundedHash, grounded);
            animator.SetBool(JumpingHash, !grounded && verticalVelocity > 0f);
            animator.SetFloat(FallSpeedHash, verticalVelocity);
            animator.SetFloat(AnimationSpeedHash, isSprinting ? 1.3f : 1f);
        }

        private void HandlePlayerDied()
        {
            if (animator != null && freezeAnimationOnDeath)
                animator.speed = 0f;
        }

        // Animation Events
        public void SpawnSmoke() { }
        public void SpawnFx() { }
        public void LeftStep() { }
        public void RightStep() { }
    }
}