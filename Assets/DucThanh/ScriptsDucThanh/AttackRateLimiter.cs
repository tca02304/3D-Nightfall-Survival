using System;
using System.Collections.Generic;
using UnityEngine;

namespace DucThanh
{
    /// <summary>
    /// Component chuyên biệt phụ trách Countdown Cooldown (thời gian hồi chiêu),
    /// khoảng cách tấn công, góc quét và điều kiện mục tiêu hợp lệ.
    /// Ngăn chặn triệt để việc spam đánh vượt quá tốc độ cho phép.
    /// </summary>
    [DisallowMultipleComponent]
    public class AttackRateLimiter : MonoBehaviour
    {
        [Header("Cooldown Settings (Giới hạn tốc độ đánh)")]
        [Tooltip("Thời gian hồi giữa mỗi đòn tấn công (giây)")]
        [SerializeField] private float attackCooldown = 0.8f;

        [Header("Target & Range Settings (Khoảng cách & Mục tiêu)")]
        [Tooltip("Khoảng cách tấn công")]
        [SerializeField] private float attackRange = 2.2f;

        [Tooltip("Bán kính vùng quét va chạm đòn đánh")]
        [SerializeField] private float attackRadius = 1.2f;

        [Tooltip("Góc quét hình nón trước mặt (độ)")]
        [Range(30f, 360f)]
        [SerializeField] private float attackAngle = 140f;

        [Tooltip("LayerMask chứa các đối tượng có thể bị tấn công")]
        [SerializeField] private LayerMask targetLayer = ~0;

        [Tooltip("Tag mục tiêu hợp lệ (để trống nếu chấp nhận tất cả layer)")]
        [SerializeField] private string targetTag = "";

        [Tooltip("Bỏ qua các mục tiêu đã chết")]
        [SerializeField] private bool ignoreDeadTargets = true;

        [Tooltip("Bắt buộc phải có mục tiêu hợp lệ trong tầm mới được ra đòn (mặc định false cho phép vung vũ khí tự do)")]
        [SerializeField] private bool requireTargetToAttack = false;

        private float nextAttackTime = 0f;
        private float lastAttackTime = -999f;
        private PlayerStats playerStats;

        public event Action OnAttackStarted;
        public event Action OnAttackBlocked;

        public float AttackCooldown => attackCooldown;
        public float AttackRange => attackRange;
        public float AttackRadius => attackRadius;
        public LayerMask TargetLayer => targetLayer;

        /// <summary>
        /// Thời gian hồi còn lại (giây). Trả về 0 nếu đã sẵn sàng đánh.
        /// </summary>
        public float RemainingCooldown => Mathf.Max(0f, nextAttackTime - Time.time);

        /// <summary>
        /// Tỉ lệ hồi chiêu từ 0 đến 1 (tiện dụng để hiển thị thanh/icon cooldown UI)
        /// </summary>
        public float CooldownProgress
        {
            get
            {
                if (attackCooldown <= 0f) return 1f;
                return Mathf.Clamp01((Time.time - lastAttackTime) / attackCooldown);
            }
        }

        /// <summary>
        /// Trạng thái hiện tại có đang trong cooldown hay không
        /// </summary>
        public bool IsOnCooldown => Time.time < nextAttackTime;

        private void Awake()
        {
            playerStats = GetComponent<PlayerStats>();
        }

        /// <summary>
        /// Kiểm tra xem hiện tại có đủ điều kiện để thực hiện đòn đánh không.
        /// </summary>
        public bool CanAttack(out string failReason)
        {
            if (IsOnCooldown)
            {
                failReason = $"Đang trong cooldown ({RemainingCooldown:F2}s)";
                return false;
            }

            if (playerStats != null && playerStats.IsDead)
            {
                failReason = "Player đã chết";
                return false;
            }

            if (requireTargetToAttack && !HasValidTargetInRange())
            {
                failReason = "Không có mục tiêu trong tầm";
                return false;
            }

            failReason = string.Empty;
            return true;
        }

        /// <summary>
        /// Thử thực hiện đòn đánh. Nếu thành công sẽ reset cooldown và trả về true.
        /// </summary>
        public bool TryAttack()
        {
            if (!CanAttack(out _))
            {
                OnAttackBlocked?.Invoke();
                return false;
            }

            lastAttackTime = Time.time;
            nextAttackTime = Time.time + attackCooldown;
            OnAttackStarted?.Invoke();
            return true;
        }

        /// <summary>
        /// Quét và trả về danh sách các EnemyTest hợp lệ trong vùng đánh.
        /// </summary>
        public List<EnemyTest> GetTargetsToDamage()
        {
            var targets = new List<EnemyTest>();
            Vector3 origin = transform.position + Vector3.up * 1f;
            Vector3 forward = transform.forward;
            Vector3 center = origin + forward * (attackRange * 0.5f);

            Collider[] hits = Physics.OverlapSphere(center, attackRadius, targetLayer, QueryTriggerInteraction.Ignore);
            var uniqueEnemies = new HashSet<EnemyTest>();

            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject || hit.transform.IsChildOf(transform)) continue;

                EnemyTest enemy = hit.GetComponentInParent<EnemyTest>();
                if (enemy != null && !uniqueEnemies.Contains(enemy))
                {
                    if (IsValidTarget(enemy.gameObject, origin, forward, enemy.IsDead))
                    {
                        uniqueEnemies.Add(enemy);
                        targets.Add(enemy);
                    }
                }
            }

            // Fallback SphereCast nếu OverlapSphere bị hụt góc
            if (targets.Count == 0)
            {
                Ray ray = new Ray(origin, forward);
                if (Physics.SphereCast(ray, 0.5f, out RaycastHit hitInfo, attackRange, targetLayer, QueryTriggerInteraction.Ignore))
                {
                    if (hitInfo.collider.gameObject != gameObject && !hitInfo.collider.transform.IsChildOf(transform))
                    {
                        EnemyTest enemy = hitInfo.collider.GetComponentInParent<EnemyTest>();
                        if (enemy != null && IsValidTarget(enemy.gameObject, origin, forward, enemy.IsDead))
                        {
                            targets.Add(enemy);
                        }
                    }
                }
            }

            return targets;
        }

        /// <summary>
        /// Kiểm tra xem có bất kỳ mục tiêu hợp lệ nào trong tầm đánh hay không.
        /// </summary>
        public bool HasValidTargetInRange()
        {
            Vector3 origin = transform.position + Vector3.up * 1f;
            Vector3 forward = transform.forward;
            Vector3 center = origin + forward * (attackRange * 0.5f);

            Collider[] hits = Physics.OverlapSphere(center, attackRadius, targetLayer, QueryTriggerInteraction.Ignore);
            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject || hit.transform.IsChildOf(transform)) continue;
                EnemyTest enemy = hit.GetComponentInParent<EnemyTest>();
                if (enemy != null && IsValidTarget(enemy.gameObject, origin, forward, enemy.IsDead))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsValidTarget(GameObject obj, Vector3 origin, Vector3 forward, bool isDead)
        {
            if (ignoreDeadTargets && isDead) return false;

            if (!string.IsNullOrEmpty(targetTag) && !obj.CompareTag(targetTag) && !obj.transform.root.CompareTag(targetTag))
            {
                return false;
            }

            Vector3 dir = (obj.transform.position - transform.position);
            dir.y = 0;
            if (dir.sqrMagnitude > 0.001f)
            {
                float angle = Vector3.Angle(forward, dir.normalized);
                if (angle > attackAngle * 0.5f) return false;
            }

            return true;
        }

        public void ResetCooldown()
        {
            nextAttackTime = Time.time;
        }

        public void SetCooldown(float newCooldown)
        {
            attackCooldown = Mathf.Max(0.05f, newCooldown);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = transform.position + Vector3.up * 1f;
            Vector3 center = origin + transform.forward * (attackRange * 0.5f);
            Gizmos.color = IsOnCooldown ? Color.gray : Color.red;
            Gizmos.DrawWireSphere(center, attackRadius);

            Gizmos.color = Color.cyan;
            Vector3 leftRay = Quaternion.Euler(0, -attackAngle * 0.5f, 0) * transform.forward;
            Vector3 rightRay = Quaternion.Euler(0, attackAngle * 0.5f, 0) * transform.forward;
            Gizmos.DrawRay(origin, leftRay * attackRange);
            Gizmos.DrawRay(origin, rightRay * attackRange);
        }
    }
}
