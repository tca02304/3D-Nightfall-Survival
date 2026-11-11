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

        [Tooltip("Tag mục tiêu hợp lệ để nhận sát thương (Mặc định 'Enemy')")]
        [SerializeField] private string targetTag = "Enemy";

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
        /// Quét và trả về danh sách các đối tượng nhận sát thương hợp lệ (dựa trên tag Enemy).
        /// Hỗ trợ cả QueryTriggerInteraction.Collide để chém trúng các mob chỉ có trigger collider (như LilDave).
        /// </summary>
        public List<GameObject> GetTargetsToDamage()
        {
            var targets = new List<GameObject>();
            Vector3 origin = transform.position + Vector3.up * 1f;
            Vector3 forward = transform.forward;
            Vector3 center = origin + forward * (attackRange * 0.5f);

            // Cho phép quét trúng cả Trigger collider để mob như LilDave (chỉ có trigger collider) vẫn nhận sát thương
            Collider[] hits = Physics.OverlapSphere(center, attackRadius, targetLayer, QueryTriggerInteraction.Collide);
            var uniqueRoots = new HashSet<GameObject>();

            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject || hit.transform.IsChildOf(transform)) continue;

                GameObject enemyRoot = GetEnemyRoot(hit.gameObject);
                if (enemyRoot != null && !uniqueRoots.Contains(enemyRoot))
                {
                    if (IsValidTarget(enemyRoot, origin, forward))
                    {
                        uniqueRoots.Add(enemyRoot);
                        targets.Add(enemyRoot);
                    }
                }
            }

            // Fallback SphereCast nếu OverlapSphere bị hụt góc
            if (targets.Count == 0)
            {
                Ray ray = new Ray(origin, forward);
                if (Physics.SphereCast(ray, 0.5f, out RaycastHit hitInfo, attackRange, targetLayer, QueryTriggerInteraction.Collide))
                {
                    if (hitInfo.collider.gameObject != gameObject && !hitInfo.collider.transform.IsChildOf(transform))
                    {
                        GameObject enemyRoot = GetEnemyRoot(hitInfo.collider.gameObject);
                        if (enemyRoot != null && IsValidTarget(enemyRoot, origin, forward))
                        {
                            targets.Add(enemyRoot);
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

            Collider[] hits = Physics.OverlapSphere(center, attackRadius, targetLayer, QueryTriggerInteraction.Collide);
            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject || hit.transform.IsChildOf(transform)) continue;
                GameObject enemyRoot = GetEnemyRoot(hit.gameObject);
                if (enemyRoot != null && IsValidTarget(enemyRoot, origin, forward))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Lấy GameObject gốc của Enemy nếu đối tượng hoặc cha của nó có tag Enemy (hoặc có component Enemy)
        /// </summary>
        private GameObject GetEnemyRoot(GameObject hitObj)
        {
            if (hitObj == null) return null;

            // 1. Kiểm tra tag targetTag ("Enemy") trên hitObj hoặc root/parent
            if (!string.IsNullOrEmpty(targetTag))
            {
                if (hitObj.CompareTag(targetTag)) return hitObj;
                if (hitObj.transform.root.CompareTag(targetTag)) return hitObj.transform.root.gameObject;

                Transform current = hitObj.transform.parent;
                while (current != null)
                {
                    if (current.CompareTag(targetTag)) return current.gameObject;
                    current = current.parent;
                }
            }

            // 2. Fallback nếu prefab chưa kịp gắn tag: kiểm tra xem có EnemyStats / EnemyTest / MobHealthBar không
            EnemyStats stats = hitObj.GetComponentInParent<EnemyStats>();
            if (stats != null) return stats.gameObject;

            EnemyTest test = hitObj.GetComponentInParent<EnemyTest>();
            if (test != null) return test.gameObject;

            return null;
        }

        private bool IsValidTarget(GameObject obj, Vector3 origin, Vector3 forward)
        {
            if (ignoreDeadTargets)
            {
                // Kiểm tra xem mục tiêu đã chết chưa qua EnemyStats / EnemyTest / MobHealthBar
                EnemyStats stats = obj.GetComponent<EnemyStats>();
                if (stats != null && stats.IsDead) return false;

                EnemyTest test = obj.GetComponent<EnemyTest>();
                if (test != null && test.IsDead) return false;
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
