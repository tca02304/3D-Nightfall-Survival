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

        [Tooltip("Độ cao nhảy")]
        [SerializeField] private float jumpHeight = 1.5f;

        [Tooltip("Trọng lực")]
        [SerializeField] private float gravity = -19.62f;

        [Header("Animation")]
        [Tooltip("Animator điều khiển hoạt ảnh nhân vật. Nếu để trống sẽ tự tìm trên chính GameObject hoặc con")]
        [SerializeField] private Animator animator;

        [Header("Combat Settings")]
        [Tooltip("Bộ giới hạn tốc độ đánh và phạm vi (Nếu để trống sẽ tự lấy trên Player)")]
        [SerializeField] private AttackRateLimiter attackLimiter;


        [Tooltip("Lượng thể lực tiêu hao cho mỗi đòn tấn công (0 = không tốn)")]
        [SerializeField] private float attackStaminaCost = 0f;


        [Tooltip("Lượng sát thương gây ra mỗi đòn chém")]
        [SerializeField] private float attackDamage = 25f;

        [Tooltip("Khoảng cách tấn công")]
        [SerializeField] private float attackRange = 2.2f;

        [Tooltip("Bán kính vùng quét va chạm đòn đánh")]
        [SerializeField] private float attackRadius = 1.2f;

        [Tooltip("Độ trễ trước khi kiểm tra va chạm (khớp thời điểm tay vung đòn)")]
        [SerializeField] private float attackHitDelay = 0.15f;

        [Tooltip("LayerMask các đối tượng có thể bị tấn công")]
        [SerializeField] private LayerMask targetLayer = ~0;

        private CharacterController characterController;
        private PlayerStats playerStats;
        private Vector3 verticalVelocity;
        private Vector2 moveInput;
        private bool isSprinting;
        private bool jumpTriggered;
        private bool attackTriggered;
        private float attackEndTime;

        [Header("Weapon & Equipment")]
        [Tooltip("Trạng thái đã nhặt vũ khí (Katana) hay chưa")]
        [SerializeField] private bool hasWeapon = false;

        public CharacterController CharacterController => characterController;
        public bool IsGrounded => characterController != null && characterController.isGrounded;
        public Vector3 Velocity => characterController != null ? characterController.velocity : Vector3.zero;
        public bool IsMoving => moveInput.sqrMagnitude > 0.01f;
        public bool IsSprinting => isSprinting;
        public bool IsAttacking => Time.time < attackEndTime;
        public bool HasWeapon => hasWeapon;
        public Animator Animator => animator;

        public event System.Action OnAttack;

        public void SetHasWeapon(bool value)
        {
            hasWeapon = value;
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            playerStats = GetComponent<PlayerStats>();
            attackLimiter = GetComponent<AttackRateLimiter>();
            if (attackLimiter == null)
            {
                attackLimiter = gameObject.AddComponent<AttackRateLimiter>();
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
                playerStats.OnPlayerDied -= StopAnimationOnDeath;
            }
        }

        private void StopAnimationOnDeath()
        {
            attackTriggered = false;
            if (animator != null)
            {
                animator.speed = 0f;
            }
        }

        private void Update()
        {
            GatherInput();
            HandleMovement();
            HandleAttack();
        }

        private void GatherInput()
        {
            if (playerStats == null)
            {
                playerStats = GetComponent<PlayerStats>();
            }

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
#else
            horizontal = Input.GetAxisRaw("Horizontal");
            vertical = Input.GetAxisRaw("Vertical");
            sprintPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            jumpPressed = Input.GetButtonDown("Jump");
#endif

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
                catch { }
            }

            moveInput = new Vector2(horizontal, vertical).normalized;
            isSprinting = sprintPressed && (playerStats == null || playerStats.CanSprint);
            if (jumpPressed)
            {
                jumpTriggered = true;
            }

            bool attackPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                attackPressed = true;
            }
#endif
            if (!attackPressed)
            {
                try
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        attackPressed = true;
                    }
                }
                catch { }
            }

            if (attackPressed)
            {
                attackTriggered = true;
            }
        }

        private void HandleMovement()
        {
            if (characterController == null) return;

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

            // Di chuyển theo hướng nhân vật đang đứng
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;

            Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;
            float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

            Vector3 horizontalMove = moveDirection * currentSpeed;

            verticalVelocity.y += gravity * Time.deltaTime;

            Vector3 finalMotion = (horizontalMove + verticalVelocity) * Time.deltaTime;
            characterController.Move(finalMotion);

            UpdateAnimation();
        }

        private void HandleAttack()
        {
            if (!attackTriggered) return;
            attackTriggered = false;

            if (playerStats != null && playerStats.IsDead) return;

            // Kiểm tra stamina nếu có chi phí đánh
            if (attackStaminaCost > 0f && playerStats != null)
            {
                if (!playerStats.ConsumeStamina(attackStaminaCost))
                {
                    return;
                }
            }

            if (attackLimiter != null)
            {
                if (!attackLimiter.TryAttack())
                {
                    return;
                }
            }

            float cooldownDuration = attackLimiter != null ? attackLimiter.AttackCooldown : 0.8f;
            attackEndTime = Time.time + cooldownDuration;

            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }

            StartCoroutine(PerformAttackHitCheck());

            OnAttack?.Invoke();
        }

        private System.Collections.IEnumerator PerformAttackHitCheck()
        {
            if (attackHitDelay > 0f)
            {
                yield return new WaitForSeconds(attackHitDelay);
            }

            CheckHitAndDamageEnemies();
        }

        private void CheckHitAndDamageEnemies()
        {
            Vector3 origin = transform.position + Vector3.up * 1f;
            Vector3 center = origin + transform.forward * (attackRange * 0.5f);

            Collider[] hits = Physics.OverlapSphere(center, attackRadius, targetLayer);
            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;

                // Gửi thông báo tấn công đến target nếu có Component chịu damage
                hit.SendMessage("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = transform.position + Vector3.up * 1f;
            Vector3 center = origin + transform.forward * (attackRange * 0.5f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(center, attackRadius);
        }

        private void UpdateAnimation()
        {
            if (animator == null) return;

            if (playerStats != null && playerStats.IsDead)
            {
                animator.speed = 0f;
                return;
            }

            if (animator.speed == 0f)
            {
                animator.speed = 1f;
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
            CheckHitAndDamageEnemies();
        }
        public void LeftStep() { }
        public void RightStep() { }
        #endregion
    }
}