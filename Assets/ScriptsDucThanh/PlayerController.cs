using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DucThanh
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Tốc độ di chuyển bình thường")]
        [SerializeField] private float walkSpeed = 5f;

        [Tooltip("Tốc độ chạy nhanh (Sprint)")]
        [SerializeField] private float sprintSpeed = 8f;

        [Tooltip("Tốc độ xoay người theo hướng di chuyển")]
        [SerializeField] private float rotationSpeed = 12f;

        [Tooltip("Độ cao nhảy")]
        [SerializeField] private float jumpHeight = 1.5f;

        [Tooltip("Trọng lực")]
        [SerializeField] private float gravity = -19.62f;

        [Header("Camera")]
        [Tooltip("Camera tham chiếu hướng di chuyển. Nếu để trống sẽ tự lấy Camera.main")]
        [SerializeField] private Transform cameraTransform;

        [Header("Animation")]
        [Tooltip("Animator điều khiển hoạt ảnh nhân vật. Nếu để trống sẽ tự tìm trên chính GameObject hoặc con")]
        [SerializeField] private Animator animator;

        private CharacterController characterController;
        private PlayerStats playerStats;
        private Vector3 verticalVelocity;
        private Vector2 moveInput;
        private bool isSprinting;
        private bool jumpTriggered;

        public CharacterController CharacterController => characterController;
        public bool IsGrounded => characterController != null && characterController.isGrounded;
        public Vector3 Velocity => characterController != null ? characterController.velocity : Vector3.zero;
        public bool IsMoving => moveInput.sqrMagnitude > 0.01f;
        public bool IsSprinting => isSprinting;
        public Animator Animator => animator;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            playerStats = GetComponent<PlayerStats>();

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (playerStats != null)
            {
                playerStats.OnPlayerDied += StopAnimationOnDeath;
            }
        }

        private void OnDestroy()
        {
            if (playerStats != null)
            {
                playerStats.OnPlayerDied -= StopAnimationOnDeath;
            }
        }

        private void StopAnimationOnDeath()
        {
            if (animator != null)
            {
                animator.speed = 0f;
            }
        }

        private void Update()
        {
            GatherInput();
            HandleMovement();
        }

        private void GatherInput()
        {
            if (playerStats == null)
            {
                playerStats = GetComponent<PlayerStats>();
            }

            // Nếu player đã chết thì khóa toàn bộ phím di chuyển, chạy nhanh, nhảy
            if (playerStats != null && playerStats.IsDead)
            {
                moveInput = Vector2.zero;
                isSprinting = false;
                jumpTriggered = false;
                return;
            }

            // Hỗ trợ cả New Input System lẫn Legacy Input Manager mượt mà và không gây lỗi
            float horizontal = 0f;
            float vertical = 0f;
            bool sprintPressed = false;
            bool jumpPressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) vertical += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) vertical -= 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontal -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontal += 1f;

                if (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed)
                    sprintPressed = true;

                if (Keyboard.current.spaceKey.wasPressedThisFrame)
                    jumpPressed = true;
            }

            if (Gamepad.current != null)
            {
                Vector2 stick = Gamepad.current.leftStick.ReadValue();
                if (stick.sqrMagnitude > 0.01f)
                {
                    horizontal = stick.x;
                    vertical = stick.y;
                }

                if (Gamepad.current.leftStickButton.isPressed || Gamepad.current.buttonSouth.isPressed)
                    sprintPressed = true;

                if (Gamepad.current.buttonSouth.wasPressedThisFrame)
                    jumpPressed = true;
            }
#else
            horizontal = Input.GetAxisRaw("Horizontal");
            vertical = Input.GetAxisRaw("Vertical");
            sprintPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            jumpPressed = Input.GetButtonDown("Jump");
#endif

            // Fallback nếu new input system không lấy được hoặc đang bật cả hai
            if (horizontal == 0f && vertical == 0f)
            {
                try
                {
                    horizontal = Input.GetAxisRaw("Horizontal");
                    vertical = Input.GetAxisRaw("Vertical");
                    if (!sprintPressed)
                        sprintPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                    if (!jumpPressed)
                        jumpPressed = Input.GetButtonDown("Jump");
                }
                catch
                {
                    // Tránh lỗi nếu Legacy Input bị tắt hoàn toàn trong Unity settings
                }
            }

            moveInput = new Vector2(horizontal, vertical).normalized;
            isSprinting = sprintPressed && (playerStats == null || playerStats.CanSprint);
            if (jumpPressed)
            {
                jumpTriggered = true;
            }
        }

        private void HandleMovement()
        {
            if (characterController == null) return;

            // Khi player đã chết: không di chuyển ngang, không nhảy, chỉ chịu tác động trọng lực rơi
            if (playerStats != null && playerStats.IsDead)
            {
                jumpTriggered = false;
                if (characterController.isGrounded)
                {
                    if (verticalVelocity.y < 0f) verticalVelocity.y = -2f;
                }
                else
                {
                    verticalVelocity.y += gravity * Time.deltaTime;
                }

                characterController.Move(verticalVelocity * Time.deltaTime);
                UpdateAnimation();
                return;
            }

            // Kiểm tra chạm đất
            if (characterController.isGrounded)
            {
                if (verticalVelocity.y < 0f)
                {
                    verticalVelocity.y = -2f; // Giữ chặt trên mặt đất dốc
                }

                if (jumpTriggered)
                {
                    verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                }
            }
            jumpTriggered = false; // Reset trigger jump sau frame xử lý

            // Hướng di chuyển tương đối theo Camera
            Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;

            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;
            float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

            Vector3 horizontalMove = moveDirection * currentSpeed;

            // Xoay nhân vật trực tiếp theo hướng di chuyển
            if (moveDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            // Áp dụng trọng lực
            verticalVelocity.y += gravity * Time.deltaTime;

            // Tổng hợp di chuyển
            Vector3 finalMotion = (horizontalMove + verticalVelocity) * Time.deltaTime;
            characterController.Move(finalMotion);

            // Cập nhật Animation
            UpdateAnimation();
        }

        private void UpdateAnimation()
        {
            if (animator == null) return;

            // Dừng toàn bộ hoạt ảnh khi player đã chết
            if (playerStats != null && playerStats.IsDead)
            {
                animator.speed = 0f;
                return;
            }

            // Khôi phục tốc độ chạy hoạt ảnh khi còn sống
            if (animator.speed == 0f)
            {
                animator.speed = 1f;
            }

            // OnlinePlayer controller:
            // - Speed (Float): 0 = Idle, 1 = Walk, >1 = Run/Sprint
            float targetSpeed = 0f;
            if (moveInput.sqrMagnitude > 0.01f)
            {
                targetSpeed = isSprinting ? 1.5f : 1.0f;
            }

            animator.SetFloat("Speed", targetSpeed, 0.1f, Time.deltaTime);
            animator.SetBool("Grounded", characterController.isGrounded);
            animator.SetBool("Jumping", !characterController.isGrounded && verticalVelocity.y > 0f);
            animator.SetFloat("FallSpeed", verticalVelocity.y);
            animator.SetFloat("AnimationSpeed", isSprinting ? 1.3f : 1.0f);
        }

        #region Animation Events Receiver
        /// <summary>
        /// Được gọi từ AnimationEvent trong Jump.anim và Land.anim
        /// </summary>
        public void SpawnSmoke()
        {
            // Placeholder cho hiệu ứng khói/bụi khi nhảy và tiếp đất
        }

        /// <summary>
        /// Được gọi từ AnimationEvent trong Attack.anim và các đòn đánh
        /// </summary>
        public void SpawnFx()
        {
            // Placeholder cho hiệu ứng chém/tấn công
        }

        /// <summary>
        /// Được gọi từ AnimationEvent trong các đòn đánh hitbox
        /// </summary>
        public void UseHitbox()
        {
            // Placeholder cho hitbox tấn công
        }

        /// <summary>
        /// Được gọi từ AnimationEvent bước chân
        /// </summary>
        public void LeftStep() { }
        public void RightStep() { }
        #endregion
    }
}
