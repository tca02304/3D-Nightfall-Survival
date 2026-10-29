using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DucThanh
{
    /// <summary>
    /// Component quản lý chuyên biệt di chuyển, nhảy, chạy nhanh (sprint),
    /// xoay hướng và hoạt ảnh locomotion của Player.
    /// Không chứa logic tính toán sát thương hay lượng máu.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
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

        [Header("Camera Reference")]
        [Tooltip("Camera tham chiếu hướng di chuyển. Nếu để trống sẽ tự lấy Camera.main")]
        [SerializeField] private Transform cameraTransform;

        [Header("Animation Reference")]
        [Tooltip("Animator điều khiển hoạt ảnh nhân vật. Nếu để trống sẽ tự tìm trên chính GameObject hoặc con")]
        [SerializeField] private Animator animator;

        [Tooltip("Tốc độ phát của hoạt ảnh Die (mặc định 0.45f giúp ngã xuống chậm rãi, mượt mà và tự nhiên hơn)")]
        [Range(0.1f, 1f)]
        [SerializeField] private float deathAnimationSpeed = 0.45f;

        [Tooltip("Điểm dừng animation Die (0.5 nghĩa là chỉ chạy 50% clip ban đầu rồi giữ nguyên tư thế nằm ngã, bỏ qua nửa sau bị lỗi)")]
        [Range(0.2f, 1f)]
        [SerializeField] private float deathAnimationCutoff = 0.5f;

        private CharacterController characterController;
        private PlayerStats playerStats;
        private PlayerCombat playerCombat;
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

        // Chuyển tiếp HasWeapon sang PlayerCombat để duy trì tương thích cho KatanaPickup
        public bool HasWeapon => playerCombat != null && playerCombat.HasWeapon;

        public void SetHasWeapon(bool value)
        {
            if (playerCombat == null)
            {
                playerCombat = GetComponent<PlayerCombat>();
            }

            if (playerCombat != null)
            {
                playerCombat.SetHasWeapon(value);
            }
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            playerStats = GetComponent<PlayerStats>();
            playerCombat = GetComponent<PlayerCombat>();
            if (playerCombat == null)
            {
                playerCombat = gameObject.AddComponent<PlayerCombat>();
            }

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
                playerStats.OnPlayerDied += HandleDeathAnimation;
            }
        }

        private void Start()
        {
            EnsureKatanaSetup();
        }

        private void EnsureKatanaSetup()
        {
            KatanaPickup katana = FindAnyObjectByType<KatanaPickup>();
            if (katana == null)
            {
                GameObject katanaObj = GameObject.Find("Katana");
                if (katanaObj == null)
                {
                    Transform[] allChildren = GetComponentsInChildren<Transform>(true);
                    foreach (var child in allChildren)
                    {
                        if (child.name == "Katana")
                        {
                            katanaObj = child.gameObject;
                            break;
                        }
                    }
                }

                if (katanaObj != null)
                {
                    katana = katanaObj.GetComponent<KatanaPickup>();
                    if (katana == null)
                    {
                        katana = katanaObj.AddComponent<KatanaPickup>();
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (playerStats != null)
            {
                playerStats.OnPlayerDied -= HandleDeathAnimation;
            }
        }

        private void HandleDeathAnimation()
        {
            if (animator != null)
            {
                // Cho phép animator chạy với tốc độ làm chậm để ngã xuống từ từ, chân thực
                animator.speed = deathAnimationSpeed;

                // Tắt UpperBody layer (layer 1) để không ghi đè chuyển động ngã toàn thân
                if (animator.layerCount > 1)
                {
                    animator.SetLayerWeight(1, 0f);
                }

                animator.ResetTrigger("Attack");
                animator.SetTrigger("Die");

                // Giữ frame cuối của animation Die khi ngã xuống
                StopAllCoroutines();
                StartCoroutine(HoldDeathPoseRoutine());
            }
        }

        private System.Collections.IEnumerator HoldDeathPoseRoutine()
        {
            if (animator == null) yield break;

            // Đợi một chút để Animator kịp chuyển sang State Die
            yield return null;

            // Chờ cho animation Die chạy đến điểm cutoff (mặc định 0.5f = 50% clip ban đầu)
            float timeout = 4.0f;
            float elapsed = 0f;

            while (elapsed < timeout)
            {
                if (playerStats != null && !playerStats.IsDead)
                {
                    // Nếu hồi sinh thì thoát và trả lại tốc độ chuẩn
                    animator.speed = 1f;
                    yield break;
                }

                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.IsName("Die"))
                {
                    if (stateInfo.normalizedTime >= deathAnimationCutoff)
                    {
                        // Khóa và giữ cố định tư thế tại đúng điểm 1/2 clip ngã xuống (bỏ qua nửa sau lỗi)
                        animator.Play("Die", 0, deathAnimationCutoff);
                        animator.speed = 0f;
                        yield break;
                    }
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Fallback an toàn nếu hết timeout: khóa tại điểm cutoff
            if (playerStats != null && playerStats.IsDead)
            {
                animator.Play("Die", 0, deathAnimationCutoff);
                animator.speed = 0f;
            }
        }

        private void Update()
        {
            GatherMovementInput();
            HandleMovement();
        }

        private void GatherMovementInput()
        {
            if (playerStats == null)
            {
                playerStats = GetComponent<PlayerStats>();
            }

            // Nếu player đã chết thì khóa toàn bộ di chuyển
            if (playerStats != null && playerStats.IsDead)
            {
                moveInput = Vector2.zero;
                isSprinting = false;
                jumpTriggered = false;
                return;
            }

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

            // Fallback nếu Legacy Input đang hoạt động song song
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
                    // Bỏ qua nếu Legacy Input bị tắt
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

            // Khi player đã chết: không di chuyển ngang, chỉ chịu tác động trọng lực rơi chạm đất
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
                    verticalVelocity.y = -2f;
                }

                if (jumpTriggered)
                {
                    verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                }
            }
            jumpTriggered = false;

            // Hướng di chuyển theo góc nhìn Camera
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
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            // Áp dụng trọng lực
            verticalVelocity.y += gravity * Time.deltaTime;

            // Di chuyển CharacterController
            Vector3 finalMotion = (horizontalMove + verticalVelocity) * Time.deltaTime;
            characterController.Move(finalMotion);

            UpdateAnimation();
        }

        private void UpdateAnimation()
        {
            if (animator == null) return;

            // Khi player đã chết: không cập nhật Speed/Grounded đè lên animation Die
            if (playerStats != null && playerStats.IsDead)
            {
                return;
            }

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
        public void SpawnSmoke() { }
        public void SpawnFx() { }
        public void UseHitbox()
        {
            if (playerCombat != null)
            {
                playerCombat.UseHitbox();
            }
        }
        public void LeftStep() { }
        public void RightStep() { }
        #endregion
    }
}
