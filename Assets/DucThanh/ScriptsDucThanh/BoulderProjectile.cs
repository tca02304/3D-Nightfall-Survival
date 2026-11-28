using System.Collections.Generic;
using UnityEngine;

namespace DucThanh
{
    /// <summary>
    /// Đạn đá máy bắn đá: bay theo quỹ đạo parabol đến mục tiêu, nổ AoE khi chạm.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class BoulderProjectile : ProjectileBase
    {
        private static readonly Collider[] ExplosionOverlapBuffer = new Collider[64];
        private static readonly HashSet<GameObject> DamagedBuffer = new HashSet<GameObject>();

        [Header("Arc Trajectory")]
        [Tooltip("Tốc độ tự xoay của hòn đá khi bay")]
        [SerializeField] private Vector3 tumbleSpeed = new Vector3(360f, 180f, 90f);

        [Tooltip("Thời gian bay tối thiểu (giây) để đá ở tầm gần vẫn có độ vọt")]
        [SerializeField, Min(0.1f)] private float minTravelDuration = 0.3f;

        [Tooltip("Tỷ lệ độ cao vòng cung theo khoảng cách ngang")]
        [SerializeField, Range(0.05f, 0.5f)] private float arcHeightFactor = 0.18f;

        [Tooltip("Độ cao vòng cung tối thiểu")]
        [SerializeField, Min(0.5f)] private float minArcHeight = 1.5f;

        [Tooltip("Độ cao vòng cung tối đa (tránh đá bay quá bổng ở tầm xa)")]
        [SerializeField, Min(1f)] private float maxArcHeight = 6f;

        [Header("AoE Explosion")]
        [Tooltip("Bán kính nổ gây sát thương xung quanh điểm chạm")]
        [SerializeField] private float explosionRadius = 4f;

        private Vector3 startPos;
        private Vector3 endPos;
        private float travelDuration;
        private float elapsed;
        private float arcHeight;
        private bool hasLanded;

        public override void Initialize(Transform targetTransform, float damageAmount, float moveSpeed, string tag = "Enemy")
        {
            base.Initialize(targetTransform, damageAmount, moveSpeed, tag);
            EnsureKinematicRigidbody();

            hasLanded = false;
            elapsed = 0f;
            startPos = transform.position;
            endPos = target != null ? target.position : startPos + transform.forward * 15f;

            float hDist = Vector2.Distance(
                new Vector2(startPos.x, startPos.z),
                new Vector2(endPos.x,   endPos.z));

            travelDuration = Mathf.Max(minTravelDuration, hDist / Mathf.Max(5f, speed));
            arcHeight      = Mathf.Clamp(hDist * arcHeightFactor, minArcHeight, maxArcHeight);
        }

        private void Update()
        {
            if (!isInitialized || hasLanded) return;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / travelDuration);

            Vector3 pos = Vector3.Lerp(startPos, endPos, t);
            pos.y += 4f * arcHeight * t * (1f - t);
            transform.position = pos;

            transform.Rotate(tumbleSpeed * Time.deltaTime, Space.Self);

            if (t >= 1f) Explode(transform.position);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasLanded || elapsed < 0.15f) return;
            if (ShouldIgnoreCollision(other.gameObject)) return;
            if (other.isTrigger && !IsEnemyObject(other.gameObject)) return;
            Explode(transform.position);
        }

        private void OnCollisionEnter(Collision col)
        {
            if (hasLanded || elapsed < 0.15f) return;
            if (ShouldIgnoreCollision(col.gameObject)) return;
            Explode(col.contacts.Length > 0 ? col.contacts[0].point : transform.position);
        }

        private void Explode(Vector3 point)
        {
            if (hasLanded) return;
            hasLanded = true;

            SpawnImpactEffect(point, Quaternion.identity);

            int hitCount = Physics.OverlapSphereNonAlloc(
                point, explosionRadius, ExplosionOverlapBuffer, targetLayer, QueryTriggerInteraction.Collide);

            DamagedBuffer.Clear();
            try
            {
                for (int i = 0; i < hitCount; i++)
                {
                    Collider col = ExplosionOverlapBuffer[i];
                    if (col == null || col.gameObject == gameObject) continue;

                    EnemyStats stats = col.GetComponentInParent<EnemyStats>();
                    GameObject enemyObj = stats != null ? stats.gameObject : null;

                    if (enemyObj == null)
                    {
                        EnemyTest test = col.GetComponentInParent<EnemyTest>();
                        if (test != null)
                            enemyObj = test.gameObject;
                        else if (col.CompareTag(targetTag) || col.transform.root.CompareTag(targetTag))
                            enemyObj = col.gameObject;
                    }

                    if (enemyObj != null && DamagedBuffer.Add(enemyObj))
                        ApplyDamage(enemyObj);
                }
            }
            finally
            {
                DamagedBuffer.Clear();
            }

            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}
