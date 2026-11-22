using System.Collections;
using UnityEngine;

namespace DucThanh
{
    /// <summary>
    /// Base class cho vũ khí công thành bắn tự động (Nỏ, Máy bắn đá,...).
    /// Tự tìm mục tiêu gần nhất trong tầm, xoay về phía mục tiêu và khai hỏa theo chu kỳ.
    /// </summary>
    public abstract class SiegeWeaponBase : MonoBehaviour
    {
        [Header("Targeting & Range")]
        [Tooltip("Tag của mục tiêu cần bắn")]
        [SerializeField] protected string targetTag = "Enemy";

        [Tooltip("Bán kính phát hiện và tấn công mục tiêu")]
        [SerializeField] protected float attackRange = 25f;

        [Tooltip("LayerMask quét mục tiêu")]
        [SerializeField] protected LayerMask targetLayer = ~0;

        [Header("Weapon Stats")]
        [Tooltip("Thời gian hồi giữa các phát bắn (giây)")]
        [SerializeField] protected float fireRate = 2f;

        [Tooltip("Sát thương mỗi phát bắn")]
        [SerializeField] protected float attackDamage = 50f;

        [Tooltip("Tốc độ đạn bay")]
        [SerializeField] protected float projectileSpeed = 25f;

        [Header("Aiming")]
        [Tooltip("Phần thân xoay ngang để ngắm (thường là root của model)")]
        [SerializeField] protected Transform rotatingPart;

        [Tooltip("Tốc độ xoay về phía mục tiêu")]
        [SerializeField] protected float rotationSpeed = 10f;

        [Tooltip("Bật nếu phần ĐẰNG SAU của model đang quay về enemy (FBX xuất ngược 180°)")]
        [SerializeField] protected bool flipAimDirection = false;

        [Tooltip("Điểm spawn đạn. Nếu để trống sẽ dùng vị trí vũ khí + offset")]
        [SerializeField] protected Transform firePoint;

        [Tooltip("Prefab đạn")]
        [SerializeField] protected GameObject projectilePrefab;

        [Header("Animation & Audio")]
        [Tooltip("Thời gian delay từ khi kích hoạt animation đến khi sinh đạn")]
        [SerializeField] protected float projectileSpawnDelay = 0.15f;

        [SerializeField] protected Animator animator;
        [SerializeField] protected string fireAnimationTrigger = "Fire";
        [SerializeField] protected AudioSource audioSource;
        [SerializeField] protected AudioClip fireSound;

        protected Transform currentTarget;
        protected float nextFireTime;
        protected bool isFiringInProgress;

        public float AttackRange => attackRange;
        public float AttackDamage => attackDamage;
        public Transform CurrentTarget => currentTarget;

        protected virtual void Awake()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            // Tự tìm FirePoint nếu chưa gán trong Inspector
            if (firePoint == null)
                firePoint = FindFirePoint();
        }

        private Transform FindFirePoint()
        {
            foreach (Transform t in GetComponentsInChildren<Transform>())
            {
                string n = t.name.ToLowerInvariant();
                if (n.Contains("firepoint") || n.Contains("socket_bolt") ||
                    n.Contains("socket_cup") || n.Contains("projectile_anchor"))
                    return t;
            }
            return null;
        }

        protected virtual void Update()
        {
            UpdateTarget();

            if (currentTarget == null) return;

            AimAtTarget();

            if (Time.time >= nextFireTime && !isFiringInProgress)
            {
                nextFireTime = Time.time + fireRate;
                StartCoroutine(ExecuteFireSequence());
            }
        }

        private IEnumerator ExecuteFireSequence()
        {
            isFiringInProgress = true;

            // Phát animation + âm thanh
            if (animator != null)
            {
                animator.ResetTrigger(fireAnimationTrigger);
                animator.SetTrigger(fireAnimationTrigger);
            }
            if (audioSource != null && fireSound != null)
                audioSource.PlayOneShot(fireSound);

            // Chờ khớp nhịp animation
            if (projectileSpawnDelay > 0f)
                yield return new WaitForSeconds(projectileSpawnDelay);

            if (currentTarget != null)
                SpawnAndLaunchProjectile();

            isFiringInProgress = false;
        }

        protected abstract void SpawnAndLaunchProjectile();

        protected virtual void UpdateTarget()
        {
            // Giữ target hiện tại nếu vẫn hợp lệ và trong tầm
            if (IsValidTarget(currentTarget))
            {
                if ((currentTarget.position - transform.position).sqrMagnitude <= attackRange * attackRange)
                    return;
            }

            currentTarget = null;
            float bestDistSqr = attackRange * attackRange;

            Collider[] hits = Physics.OverlapSphere(transform.position, attackRange, targetLayer, QueryTriggerInteraction.Collide);
            foreach (var col in hits)
            {
                if (col.transform.IsChildOf(transform)) continue;

                Transform root = FindEnemyRoot(col.gameObject);
                if (root == null || !IsValidTarget(root)) continue;

                float dSqr = (root.position - transform.position).sqrMagnitude;
                if (dSqr < bestDistSqr)
                {
                    bestDistSqr = dSqr;
                    currentTarget = root;
                }
            }
        }

        protected virtual Transform FindEnemyRoot(GameObject obj)
        {
            if (obj == null) return null;
            if (obj.CompareTag(targetTag)) return obj.transform;
            if (obj.transform.root.CompareTag(targetTag)) return obj.transform.root;

            EnemyStats stats = obj.GetComponentInParent<EnemyStats>();
            return stats != null ? stats.transform : null;
        }

        protected virtual bool IsValidTarget(Transform t)
        {
            if (t == null || !t.gameObject.activeInHierarchy) return false;
            EnemyStats stats = t.GetComponentInParent<EnemyStats>();
            return stats == null || !stats.IsDead;
        }

        protected virtual void AimAtTarget()
        {
            if (currentTarget == null) return;

            Transform aimT = rotatingPart != null ? rotatingPart : transform;
            Vector3 dir = currentTarget.position - aimT.position;
            dir.y = 0f;
            if (flipAimDirection) dir = -dir;

            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion target = Quaternion.LookRotation(dir);
                aimT.rotation = Quaternion.Slerp(aimT.rotation, target, Time.deltaTime * rotationSpeed);
            }
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            if (currentTarget != null)
            {
                Gizmos.color = Color.red;
                Vector3 from = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1.5f;
                Gizmos.DrawLine(from, currentTarget.position);
            }
        }
    }
}
