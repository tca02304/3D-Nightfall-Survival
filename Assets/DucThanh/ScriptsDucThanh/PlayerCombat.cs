using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DucThanh
{
    /// <summary>
    /// Component chuyên biệt quản lý toàn bộ hệ thống tấn công, sát thương (Damage),
    /// tầm quét đòn đánh và nhịp chém của Player.
    /// Tách biệt hoàn toàn khỏi logic di chuyển của PlayerController.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerCombat : MonoBehaviour
    {
        [Header("Combat Settings")]
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

        [Tooltip("Lượng thể lực tiêu hao cho mỗi đòn tấn công (0 = không tốn)")]
        [SerializeField] private float attackStaminaCost = 0f;

        [Header("Components")]
        [Tooltip("Bộ giới hạn tốc độ đánh và phạm vi")]
        [SerializeField] private AttackRateLimiter attackLimiter;

        [Tooltip("Animator nhân vật để kích hoạt hoạt ảnh chém")]
        [SerializeField] private Animator animator;

        [Header("Weapon Status")]
        [Tooltip("Trạng thái đã nhặt vũ khí (Katana) hay chưa")]
        [SerializeField] private bool hasWeapon = false;

        private PlayerStats playerStats;
        private bool attackTriggered;
        private float attackEndTime;

        public float AttackDamage => attackDamage;
        public float AttackRange => attackRange;
        public float AttackRadius => attackRadius;
        public LayerMask TargetLayer => targetLayer;
        public bool HasWeapon => hasWeapon;
        public bool IsAttacking => Time.time < attackEndTime;

        public event System.Action OnAttack;

        public void SetHasWeapon(bool value)
        {
            hasWeapon = value;
        }

        public void SetAttackDamage(float newDamage)
        {
            attackDamage = Mathf.Max(0f, newDamage);
        }

        private void Awake()
        {
            playerStats = GetComponent<PlayerStats>();

            if (attackLimiter == null)
            {
                attackLimiter = GetComponent<AttackRateLimiter>();
                if (attackLimiter == null)
                {
                    attackLimiter = gameObject.AddComponent<AttackRateLimiter>();
                }
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        private void Update()
        {
            GatherAttackInput();
            HandleAttack();
        }

        private void GatherAttackInput()
        {
            if (playerStats == null)
            {
                playerStats = GetComponent<PlayerStats>();
            }

            // Nếu player đã chết thì khóa toàn bộ input tấn công
            if (playerStats != null && playerStats.IsDead)
            {
                attackTriggered = false;
                return;
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
                catch
                {
                    // Tránh lỗi nếu Legacy Input bị tắt trong Unity Settings
                }
            }

            // Bỏ qua nếu click chuột trên giao diện UI
            if (attackPressed)
            {
                if (UnityEngine.EventSystems.EventSystem.current != null &&
                    UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    attackPressed = false;
                }
            }

            if (attackPressed)
            {
                attackTriggered = true;
            }
        }

        private void HandleAttack()
        {
            if (!attackTriggered) return;
            attackTriggered = false;

            if (playerStats != null && playerStats.IsDead) return;

            // Kiểm tra cooldown thông qua AttackRateLimiter
            if (attackLimiter != null)
            {
                if (!attackLimiter.TryAttack())
                {
                    return; // Đang trong cooldown
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

        private IEnumerator PerformAttackHitCheck()
        {
            if (attackHitDelay > 0f)
            {
                yield return new WaitForSeconds(attackHitDelay);
            }

            CheckHitAndDamageEnemies();
        }

        /// <summary>
        /// Thực hiện quét mục tiêu và áp dụng sát thương
        /// </summary>
        public void CheckHitAndDamageEnemies()
        {
            if (attackLimiter == null)
            {
                attackLimiter = GetComponent<AttackRateLimiter>();
            }

            if (attackLimiter == null) return;

            List<EnemyTest> targets = attackLimiter.GetTargetsToDamage();
            foreach (var enemy in targets)
            {
                if (enemy != null && !enemy.IsDead)
                {
                    enemy.TakeDamage(attackDamage);
                }
            }
        }

        /// <summary>
        /// Gọi từ AnimationEvent "UseHitbox" trên clip tấn công nếu có
        /// </summary>
        public void UseHitbox()
        {
            CheckHitAndDamageEnemies();
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = transform.position + Vector3.up * 1f;
            Vector3 center = origin + transform.forward * (attackRange * 0.5f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(center, attackRadius);
        }
    }
}
