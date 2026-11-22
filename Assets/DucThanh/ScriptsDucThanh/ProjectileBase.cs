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
            speed = moveSpeed;
            if (!string.IsNullOrEmpty(tag)) targetTag = tag;

            if (target != null)
                targetPosition = target.position + Vector3.up * 0.8f;

            isInitialized = true;
            Destroy(gameObject, maxLifeTime);
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
