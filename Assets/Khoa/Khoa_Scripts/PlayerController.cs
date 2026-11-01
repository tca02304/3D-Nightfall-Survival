using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DucThanh
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        // =====================================================================
        //  SETTINGS
        // =====================================================================
        [Header("Movement")]
        [Tooltip("Tốc độ di chuyển bình thường")]
        [SerializeField] private float walkSpeed = 5f;

        [Tooltip("Tốc độ chạy nhanh (Sprint)")]
        [SerializeField] private float sprintSpeed = 8f;

        [Tooltip("Độ tăng tốc trên mặt đất (m/s²). Càng cao càng nhạy")]
        [SerializeField] private float groundAcceleration = 45f;

        [Tooltip("Độ giảm tốc khi thả phím trên mặt đất (m/s²). Càng thấp càng trượt")]
        [SerializeField] private float groundDeceleration = 55f;

        [Tooltip("Khả năng điều khiển trên không (m/s²). Thấp = giữ quán tính")]
        [SerializeField] private float airAcceleration = 12f;

        [Tooltip("Hệ số tốc độ khi đang vung đòn (1 = không bị chậm, 0.5 = chậm một nửa)")]
        [Range(0f, 1f)]
        [SerializeField] private float moveSpeedWhileAttacking = 1f;

        [Header("Jump & Gravity")]
        [SerializeField] private float jumpHeight = 1.5f;
        [SerializeField] private float gravity = -19.62f;

        [Tooltip("Thời gian (s) vẫn được nhảy sau khi rời mép (coyote time)")]
        [SerializeField] private float coyoteTime = 0.12f;

        [Tooltip("Thời gian (s) nhớ phím nhảy bấm sớm trước khi chạm đất")]
        [SerializeField] private float jumpBufferTime = 0.12f;

        [Tooltip("Lực ép xuống đất giúp bám dốc, không bị nảy khi đi xuống")]
        [SerializeField] private float groundStickForce = 2f;

        [Header("Look (First Person)")]
        [Tooltip("Bật để Player tự xoay theo chuột (góc nhìn thứ nhất). Tắt nếu script camera khác đang xoay Player")]
        [SerializeField] private bool enableMouseLook = false;

        [Tooltip("Khoá và ẩn con trỏ chuột khi bật Mouse Look")]
        [SerializeField] private bool lockCursor = true;

        [Tooltip("Object con chứa Camera (xoay lên/xuống). Để trống = chỉ xoay ngang")]
        [SerializeField] private Transform cameraPivot;

        [Tooltip("Độ nhạy chuột")]
        [SerializeField] private float lookSensitivity = 0.15f;

        [Tooltip("Làm mượt chuột: 0 = tắt, 15-30 = mượt (số càng lớn càng bám sát chuột)")]
        [SerializeField] private float lookSmoothing = 0f;

        [SerializeField] private float minPitch = -85f;
        [SerializeField] private float maxPitch = 85f;

        [Header("Animation")]
        [Tooltip("Animator điều khiển hoạt ảnh. Để trống sẽ tự tìm trên chính GameObject hoặc con")]
        [SerializeField] private Animator animator;

        [Tooltip("Đóng băng animation khi chết (hành vi cũ). Tắt nếu muốn chạy animation chết")]
        [SerializeField] private bool freezeAnimationOnDeath = true;

        [Header("Combat Settings")]
        [Tooltip("Bộ giới hạn tốc độ đánh (nếu để trống sẽ tự lấy / tự thêm trên Player)")]
        [SerializeField] private AttackRateLimiter attackLimiter;

        [Tooltip("Lượng thể lực tiêu hao mỗi đòn tấn công (0 = không tốn)")]
        [SerializeField] private float attackStaminaCost = 0f;

        [Tooltip("Sát thương mỗi đòn chém")]
        [SerializeField] private float attackDamage = 25f;

        [Tooltip("Khoảng cách tấn công")]
        [SerializeField] private float attackRange = 2.2f;

        [Tooltip("Bán kính vùng quét va chạm đòn đánh")]
        [SerializeField] private float attackRadius = 1.2f;

        [Tooltip("Độ cao điểm bắt đầu quét so với chân Player")]
        [SerializeField] private float attackOriginHeight = 1f;

        [Tooltip("LayerMask các đối tượng có thể bị tấn công (nên đặt riêng layer Enemy)")]
        [SerializeField] private LayerMask targetLayer = ~0;

        [Tooltip("Bật = sát thương tính tại Animation Event 'UseHitbox' trong clip (khớp frame vung kiếm).\nTắt = tính sau 'Attack Hit Delay' giây.")]
        [SerializeField] private bool useAnimationEventForHit = false;

        [Tooltip("Độ trễ trước khi kiểm tra va chạm (chỉ dùng khi KHÔNG dùng Animation Event)")]
        [SerializeField] private float attackHitDelay = 0.15f;

        [Header("Weapon & Equipment")]
        [Tooltip("Trạng thái đã nhặt vũ khí (Katana) hay chưa")]
        [SerializeField] private bool hasWeapon = false;

        [Tooltip("Bật = bắt buộc có vũ khí mới đánh được")]
        [SerializeField] private bool requireWeapon = false;

        [Header("Setup")]
        [Tooltip("Tự tìm / gắn KatanaPickup khi bắt đầu (hành vi cũ)")]
        [SerializeField] private bool autoSetupKatana = true;

        // =====================================================================
        //  STATE
        // =====================================================================
        private CharacterController characterController;
        private PlayerStats playerStats;

        // Input
        private Vector2 moveInput;
        private Vector2 lookDelta;
        private Vector2 smoothedLook;
        private bool isSprinting;
        private float lastJumpPressedTime = -999f;

        // Motion
        private Vector3 horizontalVelocity;
        private float verticalVelocity;
        private float lastGroundedTime = -999f;
        private Vector3 groundNormal = Vector3.up;
        private float pitch;

        // Combat
        private float attackEndTime;
        private WaitForSeconds hitDelayWait;
        private readonly HashSet<GameObject> hitThisSwing = new HashSet<GameObject>();
        private readonly Collider[] hitBuffer = new Collider[32];

        // Animator hashes
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int GroundedHash = Animator.StringToHash("Grounded");
        private static readonly int JumpingHash = Animator.StringToHash("Jumping");
        private static readonly int FallSpeedHash = Animator.StringToHash("FallSpeed");
        private static readonly int AnimationSpeedHash = Animator.StringToHash("AnimationSpeed");
        private static readonly int AttackHash = Animator.StringToHash("Attack");

        // =====================================================================
        //  PUBLIC API (giữ tương thích với script cũ)
        // =====================================================================
        public CharacterController CharacterController => characterController;
        public bool IsGrounded => characterController != null && characterController.isGrounded;
        public Vector3 Velocity => characterController != null ? characterController.velocity : Vector3.zero;
        public bool IsMoving => moveInput.sqrMagnitude > 0.01f;
        public bool IsSprinting => isSprinting;
        public bool IsAttacking => Time.time < attackEndTime;
        public bool HasWeapon => hasWeapon;
        public Animator Animator => animator;

        public event Action OnAttack;

        public void SetHasWeapon(bool value)
        {
            hasWeapon = value;
        }

        private bool IsDead => playerStats != null && playerStats.IsDead;

        // =====================================================================
        //  UNITY LIFECYCLE
        // =====================================================================
        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            playerStats = GetComponent<PlayerStats>();

            // Chỉ tự lấy khi chưa gán trong Inspector
            if (attackLimiter == null) attackLimiter = GetComponent<AttackRateLimiter>();
            if (attackLimiter == null) attackLimiter = gameObject.AddComponent<AttackRateLimiter>();

            if (animator == null) animator = GetComponentInChildren<Animator>();

            hitDelayWait = new WaitForSeconds(attackHitDelay);
        }

        private void OnEnable()
        {
            if (playerStats != null) playerStats.OnPlayerDied += HandlePlayerDied;

            if (enableMouseLook && lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void OnDisable()
        {
            if (playerStats != null) playerStats.OnPlayerDied -= HandlePlayerDied;
        }

        private void Start()
        {
            if (cameraPivot != null)
            {
                pitch = Mathf.DeltaAngle(0f, cameraPivot.localEulerAngles.x);
            }

            if (autoSetupKatana) EnsureKatanaSetup();
        }

        private void Update()
        {
            GatherInput();
            HandleLook();
            HandleMovement();
            UpdateAnimation();
        }

        private void HandlePlayerDied()
        {
            if (animator != null && freezeAnimationOnDeath) animator.speed = 0f;
        }

        // =====================================================================
        //  INPUT
        // =====================================================================
        private void GatherInput()
        {
            moveInput = Vector2.zero;
            lookDelta = Vector2.zero;
            isSprinting = false;

            if (IsDead) return;

            float horizontal = 0f;
            float vertical = 0f;
            bool sprintHeld = false;
            bool jumpPressed = false;
            bool attackPressed = false;

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

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                attackPressed = mouse.leftButton.wasPressedThisFrame;
                lookDelta = mouse.delta.ReadValue();
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            horizontal = Input.GetAxisRaw("Horizontal");
            vertical = Input.GetAxisRaw("Vertical");
            sprintHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            jumpPressed = Input.GetButtonDown("Jump");
            attackPressed = Input.GetMouseButtonDown(0);
            lookDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
#endif

            moveInput = new Vector2(horizontal, vertical);
            if (moveInput.sqrMagnitude > 1f) moveInput.Normalize();

            isSprinting = sprintHeld && IsMoving && (playerStats == null || playerStats.CanSprint);

            if (jumpPressed) lastJumpPressedTime = Time.time;
            if (attackPressed) TryAttack();
        }

        // =====================================================================
        //  LOOK (First Person)
        // =====================================================================
        private void HandleLook()
        {
            if (!enableMouseLook || IsDead) return;

            Vector2 delta = lookDelta * lookSensitivity;

            if (lookSmoothing > 0f)
            {
                float t = 1f - Mathf.Exp(-lookSmoothing * Time.deltaTime); // độc lập FPS
                smoothedLook = Vector2.Lerp(smoothedLook, delta, t);
                delta = smoothedLook;
            }

            transform.Rotate(0f, delta.x, 0f, Space.Self);

            pitch = Mathf.Clamp(pitch - delta.y, minPitch, maxPitch);
            if (cameraPivot != null)
            {
                cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }

        // =====================================================================
        //  MOVEMENT
        // =====================================================================
        private void HandleMovement()
        {
            if (characterController == null) return;

            float dt = Time.deltaTime;
            bool grounded = characterController.isGrounded;
            if (grounded) lastGroundedTime = Time.time;

            // ---- Nhảy (có coyote time + jump buffer) ----
            bool jumpBuffered = Time.time - lastJumpPressedTime <= jumpBufferTime;
            bool canJump = Time.time - lastGroundedTime <= coyoteTime;
            if (jumpBuffered && canJump && !IsDead)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                lastJumpPressedTime = -999f;
                lastGroundedTime = -999f;
                grounded = false;
            }

            // ---- Vận tốc ngang: tăng / giảm tốc mượt ----
            Vector3 wishDir = transform.right * moveInput.x + transform.forward * moveInput.y;
            if (wishDir.sqrMagnitude > 1f) wishDir.Normalize();

            float speed = isSprinting ? sprintSpeed : walkSpeed;
            if (IsAttacking) speed *= moveSpeedWhileAttacking;

            Vector3 targetVelocity = wishDir * speed;
            float accel;
            if (!grounded) accel = airAcceleration;
            else accel = targetVelocity.sqrMagnitude > 0.0001f ? groundAcceleration : groundDeceleration;

            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, accel * dt);

            // ---- Vận tốc dọc ----
            float motionY;
            if (grounded && verticalVelocity <= 0f)
            {
                verticalVelocity = -groundStickForce;
                motionY = verticalVelocity;

                // Đi xuống dốc: ép bám theo mặt dốc để không bị "nảy"
                Vector3 n = groundNormal;
                if (n.y > 0.01f)
                {
                    float slopeY = -(n.x * horizontalVelocity.x + n.z * horizontalVelocity.z) / n.y;
                    if (slopeY < 0f) motionY += slopeY;
                }
            }
            else
            {
                float prev = verticalVelocity;
                verticalVelocity += gravity * dt;
                motionY = (prev + verticalVelocity) * 0.5f; // tích phân trung điểm: nhảy ổn định mọi FPS
            }

            // ---- Di chuyển ----
            groundNormal = Vector3.up;
            Vector3 motion = new Vector3(horizontalVelocity.x, motionY, horizontalVelocity.z) * dt;
            CollisionFlags flags = characterController.Move(motion);

            // Đụng tường -> đồng bộ vận tốc để trượt dọc tường tự nhiên
            if ((flags & CollisionFlags.Sides) != 0)
            {
                Vector3 actual = characterController.velocity;
                horizontalVelocity = new Vector3(actual.x, 0f, actual.z);
            }

            // Đụng trần -> mất đà nhảy
            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
            {
                verticalVelocity = 0f;
            }
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.normal.y > 0.2f) groundNormal = hit.normal;
        }

        // =====================================================================
        //  ANIMATION
        // =====================================================================
        private void UpdateAnimation()
        {
            if (animator == null) return;

            if (IsDead && freezeAnimationOnDeath)
            {
                animator.speed = 0f;
                return;
            }
            if (animator.speed == 0f) animator.speed = 1f;

            // Speed bám theo vận tốc thật (0 = đứng, 1 = đi, ~1.5 = chạy)
            float animSpeed = Mathf.Clamp(horizontalVelocity.magnitude / Mathf.Max(0.01f, walkSpeed), 0f, 1.5f);
            bool animGrounded = Time.time - lastGroundedTime <= 0.08f;

            animator.SetFloat(SpeedHash, animSpeed, 0.05f, Time.deltaTime);
            animator.SetBool(GroundedHash, animGrounded);
            animator.SetBool(JumpingHash, !animGrounded && verticalVelocity > 0f);
            animator.SetFloat(FallSpeedHash, verticalVelocity);
            animator.SetFloat(AnimationSpeedHash, isSprinting ? 1.3f : 1.0f);
        }

        // =====================================================================
        //  COMBAT  (tách riêng để sau này chuyển sang PlayerCombat dễ dàng)
        // =====================================================================
        #region Combat

        private void TryAttack()
        {
            if (IsDead) return;
            if (requireWeapon && !hasWeapon) return;

            // Kiểm tra giãn cách trước, để bấm spam không bị trừ stamina oan
            if (attackLimiter != null && !attackLimiter.TryAttack()) return;

            if (attackStaminaCost > 0f && playerStats != null && !playerStats.ConsumeStamina(attackStaminaCost))
                return;

            float cooldown = attackLimiter != null ? attackLimiter.AttackCooldown : 0.8f;
            attackEndTime = Time.time + cooldown;

            hitThisSwing.Clear();

            if (animator != null) animator.SetTrigger(AttackHash);

            if (!useAnimationEventForHit) StartCoroutine(HitAfterDelay());

            OnAttack?.Invoke();
        }

        private IEnumerator HitAfterDelay()
        {
            if (attackHitDelay > 0f) yield return hitDelayWait;
            CheckHitAndDamageEnemies();
        }

        /// <summary>Quét va chạm đòn đánh. Mỗi mục tiêu chỉ nhận sát thương 1 lần cho mỗi đòn.</summary>
        private void CheckHitAndDamageEnemies()
        {
            if (IsDead) return;

            Vector3 origin = transform.position + Vector3.up * attackOriginHeight;
            Vector3 center = origin + transform.forward * (attackRange * 0.5f);

            int count = Physics.OverlapSphereNonAlloc(center, attackRadius, hitBuffer, targetLayer, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                Collider col = hitBuffer[i];
                if (col == null || col.transform.IsChildOf(transform)) continue; // bỏ qua chính Player

                // Khóa chống trùng: Rigidbody gốc nếu có, không thì chính collider
                GameObject key = col.attachedRigidbody != null ? col.attachedRigidbody.gameObject : col.gameObject;
                if (!hitThisSwing.Add(key)) continue;

                ApplyDamage(col, attackDamage);
            }
        }

        /// <summary>Điểm duy nhất gửi sát thương tới mục tiêu. Sau này đổi sang IDamageable chỉ cần sửa hàm này.</summary>
        private void ApplyDamage(Collider target, float amount)
        {
            target.SendMessageUpwards("TakeDamage", amount, SendMessageOptions.DontRequireReceiver);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = transform.position + Vector3.up * attackOriginHeight;
            Vector3 center = origin + transform.forward * (attackRange * 0.5f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(center, attackRadius);
        }

        #endregion

        // =====================================================================
        //  SETUP
        // =====================================================================
        private void EnsureKatanaSetup()
        {
            if (FindAnyObjectByType<KatanaPickup>() != null) return;

            GameObject katanaObj = GameObject.Find("Katana");
            if (katanaObj == null)
            {
                foreach (Transform child in GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == "Katana")
                    {
                        katanaObj = child.gameObject;
                        break;
                    }
                }
            }

            if (katanaObj != null && katanaObj.GetComponent<KatanaPickup>() == null)
            {
                katanaObj.AddComponent<KatanaPickup>();
            }
        }

        // =====================================================================
        //  ANIMATION EVENTS RECEIVER
        // =====================================================================
        #region Animation Events Receiver
        public void SpawnSmoke() { }
        public void SpawnFx() { }
        public void LeftStep() { }
        public void RightStep() { }

        /// <summary>Animation Event 'UseHitbox' (bật "Use Animation Event For Hit" để dùng).</summary>
        public void UseHitbox()
        {
            if (useAnimationEventForHit) CheckHitAndDamageEnemies();
        }
        #endregion
    }
}