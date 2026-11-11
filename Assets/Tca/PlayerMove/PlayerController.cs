
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
        // =========================================================
        // MOVEMENT SETTINGS
        // =========================================================

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float sprintSpeed = 8f;
        [SerializeField] private float groundAcceleration = 45f;
        [SerializeField] private float groundDeceleration = 55f;
        [SerializeField] private float airAcceleration = 12f;

        [Range(0f, 1f)]
        [SerializeField] private float moveSpeedWhileAttacking = 1f;

        // =========================================================
        // JUMP & GRAVITY
        // =========================================================

        [Header("Jump & Gravity")]
        [SerializeField] private float jumpHeight = 1.5f;
        [SerializeField] private float gravity = -19.62f;
        [SerializeField] private float coyoteTime = 0.12f;
        [SerializeField] private float jumpBufferTime = 0.12f;
        [SerializeField] private float groundStickForce = 2f;

        // =========================================================
        // ANIMATION
        // =========================================================

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private bool freezeAnimationOnDeath = true;

        // =========================================================
        // COMBAT
        // =========================================================

        [Header("Combat")]
        [SerializeField] private AttackRateLimiter attackLimiter;
        [SerializeField] private float attackStaminaCost = 0f;
        [SerializeField] private float attackDamage = 25f;
        [SerializeField] private float attackRange = 2.2f;
        [SerializeField] private float attackRadius = 1.2f;
        [SerializeField] private float attackOriginHeight = 1f;
        [SerializeField] private LayerMask targetLayer = ~0;

        [SerializeField] private bool useAnimationEventForHit = false;
        [SerializeField] private float attackHitDelay = 0.15f;

        // =========================================================
        // WEAPON
        // =========================================================

        [Header("Weapon")]
        [SerializeField] private bool hasWeapon = false;
        [SerializeField] private bool requireWeapon = false;
        [SerializeField] private bool autoSetupKatana = true;

        // =========================================================
        // COMPONENTS
        // =========================================================

        private CharacterController characterController;
        private PlayerStats playerStats;

        // =========================================================
        // MOVEMENT STATE
        // =========================================================

        private Vector2 moveInput;
        private Vector3 horizontalVelocity;

        private float verticalVelocity;
        private float lastGroundedTime = -999f;
        private float lastJumpPressedTime = -999f;

        private Vector3 groundNormal = Vector3.up;

        private bool isSprinting;

        // =========================================================
        // COMBAT STATE
        // =========================================================

        private float attackEndTime;

        private WaitForSeconds hitDelayWait;

        private readonly HashSet<GameObject> hitThisSwing = new();

        private readonly Collider[] hitBuffer = new Collider[32];

        // =========================================================
        // ANIMATOR HASHES
        // =========================================================

        private static readonly int SpeedHash =
            Animator.StringToHash("Speed");

        private static readonly int GroundedHash =
            Animator.StringToHash("Grounded");

        private static readonly int JumpingHash =
            Animator.StringToHash("Jumping");

        private static readonly int FallSpeedHash =
            Animator.StringToHash("FallSpeed");

        private static readonly int AnimationSpeedHash =
            Animator.StringToHash("AnimationSpeed");

        private static readonly int AttackHash =
            Animator.StringToHash("Attack");

        // =========================================================
        // PUBLIC API
        // =========================================================

        public CharacterController CharacterController => characterController;

        public bool IsGrounded =>
            characterController != null && characterController.isGrounded;

        public Vector3 Velocity =>
            characterController != null ? characterController.velocity : Vector3.zero;

        public bool IsMoving => moveInput.sqrMagnitude > 0.01f;

        public bool IsSprinting => isSprinting;

        public bool IsAttacking => Time.time < attackEndTime;

        public bool HasWeapon => hasWeapon;

        public Animator Animator => animator;

        public event Action OnAttack;

        private bool IsDead =>
            playerStats != null && playerStats.IsDead;

        public void SetHasWeapon(bool value)
        {
            hasWeapon = value;
        }

        // =========================================================
        // UNITY LIFECYCLE
        // =========================================================

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            playerStats = GetComponent<PlayerStats>();

            if (attackLimiter == null)
                attackLimiter = GetComponent<AttackRateLimiter>();

            if (attackLimiter == null)
                attackLimiter = gameObject.AddComponent<AttackRateLimiter>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            hitDelayWait = new WaitForSeconds(attackHitDelay);
        }

        private void OnEnable()
        {
            if (playerStats != null)
                playerStats.OnPlayerDied += HandlePlayerDied;
        }

        private void OnDisable()
        {
            if (playerStats != null)
                playerStats.OnPlayerDied -= HandlePlayerDied;
        }

        private void Start()
        {
            if (autoSetupKatana)
                EnsureKatanaSetup();
        }

        private void Update()
        {
            GatherInput();
            HandleMovement();
            UpdateAnimation();
        }

        // =========================================================
        // INPUT
        // =========================================================

        private void GatherInput()
        {
            moveInput = Vector2.zero;
            isSprinting = false;
            if (IsDead || Time.timeScale == 0f || Cursor.lockState != CursorLockMode.Locked)
                return;

            float horizontal = 0f;
            float vertical = 0f;

            bool sprintHeld = false;
            bool jumpPressed = false;
            bool attackPressed = false;

#if ENABLE_INPUT_SYSTEM

            Keyboard kb = Keyboard.current;

            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed)
                    vertical += 1f;

                if (kb.sKey.isPressed || kb.downArrowKey.isPressed)
                    vertical -= 1f;

                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)
                    horizontal -= 1f;

                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed)
                    horizontal += 1f;

                sprintHeld =
                    kb.leftShiftKey.isPressed ||
                    kb.rightShiftKey.isPressed;

                jumpPressed = kb.spaceKey.wasPressedThisFrame;
            }

            Mouse mouse = Mouse.current;

            if (mouse != null)
                attackPressed = mouse.leftButton.wasPressedThisFrame;

#elif ENABLE_LEGACY_INPUT_MANAGER

            horizontal = Input.GetAxisRaw("Horizontal");
            vertical = Input.GetAxisRaw("Vertical");

            sprintHeld =
                Input.GetKey(KeyCode.LeftShift) ||
                Input.GetKey(KeyCode.RightShift);

            jumpPressed = Input.GetButtonDown("Jump");
            attackPressed = Input.GetMouseButtonDown(0);

#endif

            moveInput = new Vector2(horizontal, vertical);

            if (moveInput.sqrMagnitude > 1f)
                moveInput.Normalize();

            isSprinting =
                sprintHeld &&
                IsMoving &&
                (playerStats == null || playerStats.CanSprint);

            if (jumpPressed)
                lastJumpPressedTime = Time.time;

            if (attackPressed)
                TryAttack();
        }

        // =========================================================
        // MOVEMENT
        // =========================================================


        private void HandleMovement()
        {
            if (characterController == null)
                return;

            float dt = Time.deltaTime;
            bool grounded = characterController.isGrounded;

            if (grounded)
                lastGroundedTime = Time.time;

            grounded = HandleJump(grounded);

            Vector3 wishDir =
                transform.right * moveInput.x +
                transform.forward * moveInput.y;

            if (wishDir.sqrMagnitude > 1f)
                wishDir.Normalize();

            float speed = isSprinting ? sprintSpeed : walkSpeed;

            if (IsAttacking)
                speed *= moveSpeedWhileAttacking;

            Vector3 targetVelocity = wishDir * speed;

            float acceleration = !grounded
                ? airAcceleration
                : targetVelocity.sqrMagnitude > 0.0001f
                    ? groundAcceleration
                    : groundDeceleration;

            horizontalVelocity = Vector3.MoveTowards(
                horizontalVelocity,
                targetVelocity,
                acceleration * dt
            );

            HandleGravity(grounded, dt);
        }

        private bool HandleJump(bool grounded)
        {
            bool jumpBuffered =
                Time.time - lastJumpPressedTime <= jumpBufferTime;

            bool canJump =
                Time.time - lastGroundedTime <= coyoteTime;

            if (jumpBuffered && canJump && !IsDead)
            {
                verticalVelocity = Mathf.Sqrt(
                    jumpHeight * -2f * gravity
                );

                lastJumpPressedTime = -999f;
                lastGroundedTime = -999f;

                return false;
            }

            return grounded;
        }

        private void HandleGravity(bool grounded, float dt)
        {
            float motionY;

            if (grounded && verticalVelocity <= 0f)
            {
                verticalVelocity = -groundStickForce;
                motionY = verticalVelocity;

                Vector3 normal = groundNormal;

                if (normal.y > 0.01f)
                {
                    float slopeY =
                        -(normal.x * horizontalVelocity.x +
                          normal.z * horizontalVelocity.z) / normal.y;

                    if (slopeY < 0f)
                        motionY += slopeY;
                }
            }
            else
            {
                float previousVelocity = verticalVelocity;

                verticalVelocity += gravity * dt;

                motionY = (previousVelocity + verticalVelocity) * 0.5f;
            }

            groundNormal = Vector3.up;

            Vector3 motion = new Vector3(
                horizontalVelocity.x,
                motionY,
                horizontalVelocity.z
            ) * dt;

            CollisionFlags flags = characterController.Move(motion);

            if ((flags & CollisionFlags.Sides) != 0)
            {
                Vector3 actual = characterController.velocity;

                horizontalVelocity = new Vector3(
                    actual.x, 0f, actual.z
                );
            }

            if ((flags & CollisionFlags.Above) != 0 &&
                verticalVelocity > 0f)
            {
                verticalVelocity = 0f;
            }
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.normal.y > 0.2f)
                groundNormal = hit.normal;
        }

        // =========================================================
        // ANIMATION
        // =========================================================

        private void UpdateAnimation()
        {
            if (animator == null)
                return;

            if (IsDead && freezeAnimationOnDeath)
            {
                animator.speed = 0f;
                return;
            }

            if (animator.speed == 0f)
                animator.speed = 1f;

            float animSpeed = Mathf.Clamp(
                horizontalVelocity.magnitude /
                Mathf.Max(0.01f, walkSpeed),
                0f,
                1.5f
            );

            bool grounded = Time.time - lastGroundedTime <= 0.08f;

            animator.SetFloat(
                SpeedHash, animSpeed, 0.05f, Time.deltaTime
            );

            animator.SetBool(GroundedHash, grounded);

            animator.SetBool(
                JumpingHash,
                !grounded && verticalVelocity > 0f
            );

            animator.SetFloat(FallSpeedHash, verticalVelocity);

            animator.SetFloat(
                AnimationSpeedHash,
                isSprinting ? 1.3f : 1f
            );
        }

        private void HandlePlayerDied()
        {
            if (animator != null && freezeAnimationOnDeath)
                animator.speed = 0f;
        }

        // =========================================================
        // COMBAT
        // =========================================================

        private void TryAttack()
        {
            if (IsDead || (requireWeapon && !hasWeapon))
                return;

            if (attackLimiter != null && !attackLimiter.TryAttack())
                return;

            if (attackStaminaCost > 0f &&
                playerStats != null &&
                !playerStats.ConsumeStamina(attackStaminaCost))
                return;

            float cooldown = attackLimiter != null
                ? attackLimiter.AttackCooldown
                : 0.8f;

            attackEndTime = Time.time + cooldown;

            hitThisSwing.Clear();

            if (animator != null)
                animator.SetTrigger(AttackHash);

            if (!useAnimationEventForHit)
                StartCoroutine(HitAfterDelay());


            OnAttack?.Invoke();
        }

        private IEnumerator HitAfterDelay()
        {
            if (attackHitDelay > 0f)
                yield return hitDelayWait;

            CheckHitAndDamageEnemies();
        }

        private void CheckHitAndDamageEnemies()
        {
            if (IsDead || Time.timeScale == 0f || Cursor.lockState != CursorLockMode.Locked)
                return;

            Vector3 origin =
                transform.position +
                Vector3.up * attackOriginHeight;

            Vector3 center =
                origin +
                transform.forward * (attackRange * 0.5f);

            int count = Physics.OverlapSphereNonAlloc(
                center,
                attackRadius,
                hitBuffer,
                targetLayer,
                QueryTriggerInteraction.Ignore
            );

            for (int i = 0; i < count; i++)
            {
                Collider col = hitBuffer[i];

                if (col == null || col.transform.IsChildOf(transform))
                    continue;

                GameObject key = col.attachedRigidbody != null
                    ? col.attachedRigidbody.gameObject
                    : col.gameObject;

                if (!hitThisSwing.Add(key))
                    continue;

                ApplyDamage(col, attackDamage);
            }
        }

        private void ApplyDamage(Collider target, float amount)
        {
            target.SendMessageUpwards(
                "TakeDamage",
                amount,
                SendMessageOptions.DontRequireReceiver
            );
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin =
                transform.position +
                Vector3.up * attackOriginHeight;

            Vector3 center =
                origin +
                transform.forward * (attackRange * 0.5f);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(center, attackRadius);
        }

        // =========================================================
        // WEAPON SETUP
        // =========================================================

        private void EnsureKatanaSetup()
        {
            if (FindAnyObjectByType<KatanaPickup>() != null)
                return;

            GameObject katanaObj = GameObject.Find("Katana");

            if (katanaObj == null)
            {
                foreach (Transform child in
                         GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == "Katana")
                    {
                        katanaObj = child.gameObject;
                        break;
                    }
                }
            }

            if (katanaObj != null &&
                katanaObj.GetComponent<KatanaPickup>() == null)
            {
                katanaObj.AddComponent<KatanaPickup>();
            }
        }

        // =========================================================
        // ANIMATION EVENTS
        // =========================================================

        public void SpawnSmoke() { }

        public void SpawnFx() { }

        public void LeftStep() { }

        public void RightStep() { }

        public void UseHitbox()
        {
            if (useAnimationEventForHit)
                CheckHitAndDamageEnemies();
        }
    }
}