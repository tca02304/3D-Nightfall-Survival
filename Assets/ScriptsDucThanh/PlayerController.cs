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

        [Header("Camera & Visual")]
        [Tooltip("Camera tham chiếu hướng di chuyển. Nếu để trống sẽ tự lấy Camera.main")]
        [SerializeField] private Transform cameraTransform;

        [Tooltip("Transform chứa visual/model/mesh (ví dụ Cube hoặc Model 3D sau này) để tách biệt với logic gốc")]
        [SerializeField] private Transform visualTransform;

        [Tooltip("Có xoay toàn bộ GameObject hay chỉ xoay visualTransform")]
        [SerializeField] private bool rotateVisualOnly = false;

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

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            playerStats = GetComponent<PlayerStats>();

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            // Nếu chưa gán visualTransform và có con bên trong, có thể tự động tìm hoặc mặc định dùng chính transform này
            if (visualTransform == null)
            {
                visualTransform = transform;
            }
        }

        private void Update()
        {
            GatherInput();
            HandleMovement();
        }

        private void GatherInput()
        {
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

            // Xoay nhân vật theo hướng di chuyển
            if (moveDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                Transform targetTransform = rotateVisualOnly && visualTransform != null ? visualTransform : transform;
                targetTransform.rotation = Quaternion.Slerp(targetTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            // Áp dụng trọng lực
            verticalVelocity.y += gravity * Time.deltaTime;

            // Tổng hợp di chuyển
            Vector3 finalMotion = (horizontalMove + verticalVelocity) * Time.deltaTime;
            characterController.Move(finalMotion);
        }

        /// <summary>
        /// Cho phép thay đổi model hiển thị sau này một cách dễ dàng
        /// </summary>
        /// <param name="newVisual">Transform của model 3D mới</param>
        public void SetVisualTransform(Transform newVisual)
        {
            visualTransform = newVisual;
        }
    }
}
