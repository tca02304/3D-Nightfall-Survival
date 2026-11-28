using UnityEngine;

namespace DucThanh
{
    /// <summary>
    /// Base class cho các loại đạn (Mũi tên nỏ, Đá bắn đá,...).
    /// Hỗ trợ gây sát thương đơn mục tiêu hoặc AoE, tự hủy khi va chạm hoặc hết vòng đời.
    /// </summary>
    public abstract class ProjectileBase : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Tag của đối tượng chịu sát thương")]
        [SerializeField] protected string targetTag = "Enemy";

        [Tooltip("LayerMask quét va chạm khi bay hoặc nổ")]
        [SerializeField] protected LayerMask targetLayer = ~0;

        [Tooltip("Thời gian tồn tại tối đa trước khi tự hủy (giây)")]
        [SerializeField] protected float maxLifeTime = 8f;

        [Tooltip("Tốc độ bay riêng của đạn (> 0 sẽ ưu tiên dùng giá trị này; để 0 sẽ dùng Projectile Speed của Nỏ / Máy Bắn Đá)")]
        [SerializeField, Min(0f)] protected float flightSpeedOverride = 0f;

        [Tooltip("Hiệu ứng va chạm hoặc nổ (tùy chọn)")]
        [SerializeField] protected GameObject impactEffectPrefab;

        [Tooltip("Âm thanh khi va chạm (tùy chọn)")]
        [SerializeField] protected AudioClip impactSound;

        protected float damage;
        protected float speed;
        protected Transform target;
        protected Vector3 targetPosition;
        protected bool isInitialized = false;

        public virtual void Initialize(Transform targetTransform, float damageAmount, float moveSpeed, string tag = "Enemy")
        {
            target = targetTransform;
            damage = damageAmount;
            speed = flightSpeedOverride > 0f ? flightSpeedOverride : moveSpeed;
            if (!string.IsNullOrEmpty(tag)) targetTag = tag;

            if (target != null)
                targetPosition = target.position + Vector3.up * 0.8f;

            isInitialized = true;
            Destroy(gameObject, maxLifeTime);
        }

        protected Rigidbody EnsureKinematicRigidbody(bool continuousSpeculative = false)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
                rb = gameObject.AddComponent<Rigidbody>();

            rb.isKinematic = true;
            rb.useGravity = false;
            if (continuousSpeculative)
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            return rb;
        }

        protected bool ShouldIgnoreCollision(GameObject obj)
        {
            if (obj == null || obj == gameObject) return true;
            return obj.GetComponentInParent<SiegeWeaponBase>() != null
                || obj.GetComponentInParent<ProjectileBase>() != null;
        }

        protected bool IsEnemyObject(GameObject obj)
        {
            if (obj == null) return false;
            return obj.CompareTag(targetTag)
                || obj.transform.root.CompareTag(targetTag)
                || obj.GetComponentInParent<EnemyStats>() != null
                || obj.GetComponentInParent<EnemyTest>() != null;
        }

        protected static Collider FindPrimaryTargetCollider(Transform targetTransform)
        {
            if (targetTransform == null) return null;

            foreach (Collider c in targetTransform.GetComponentsInChildren<Collider>())
            {
                if (c != null && !c.isTrigger)
                    return c;
            }

            return null;
        }

        protected virtual void ApplyDamage(GameObject hitObject)
        {
            if (hitObject == null) return;

            EnemyStats enemyStats = hitObject.GetComponentInParent<EnemyStats>();
            if (enemyStats != null)
            {
                if (!enemyStats.IsDead)
                    enemyStats.TakeDamage(damage);
                return;
            }

            EnemyTest enemyTest = hitObject.GetComponentInParent<EnemyTest>();
            if (enemyTest != null)
                enemyTest.TakeDamage(damage);
        }

        protected virtual void SpawnImpactEffect(Vector3 position, Quaternion rotation)
        {
            if (impactEffectPrefab != null)
                Destroy(Instantiate(impactEffectPrefab, position, rotation), 3f);

            if (impactSound != null)
                AudioSource.PlayClipAtPoint(impactSound, position);
        }
    }
}
