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
        [Header("Arc Trajectory")]
        [Tooltip("Tốc độ tự xoay của hòn đá khi bay")]
        [SerializeField] private Vector3 tumbleSpeed = new Vector3(360f, 180f, 90f);

        [Header("AoE Explosion")]
        [Tooltip("Bán kính nổ gây sát thương xung quanh điểm chạm")]
        [SerializeField] private float explosionRadius = 4f;

        private Vector3 startPos;
        private Vector3 endPos;
        private float travelDuration;
        private float elapsed;
        private float arcHeight;
        private bool hasLanded;
        private Rigidbody rb;

        public override void Initialize(Transform targetTransform, float damageAmount, float moveSpeed, string tag = "Enemy")
        {
            base.Initialize(targetTransform, damageAmount, moveSpeed, tag);

            // Nếu có Rigidbody thì đặt kinematic để tránh physics override vị trí
            rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity  = false;
            }

            // Chốt vị trí xuất phát đúng từ firePoint (đã được Instantiate đặt sẵn)
            startPos = transform.position;

            // Điểm đến là vị trí mặt đất của mục tiêu (không cộng +Y),
            // vì đá parabol rơi xuống đất chứ không nhắm vào ngực quái
            endPos = target != null ? target.position : startPos + transform.forward * 15f;

            float hDist = Vector2.Distance(
                new Vector2(startPos.x, startPos.z),
                new Vector2(endPos.x,   endPos.z));

            travelDuration = Mathf.Max(0.5f, hDist / Mathf.Max(5f, speed));
            arcHeight      = Mathf.Max(2f, hDist * 0.25f);
        }

        private void Update()
        {
            if (!isInitialized || hasLanded) return;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / travelDuration);

            // Nội suy parabol từ startPos → endPos
            Vector3 pos = Vector3.Lerp(startPos, endPos, t);
            pos.y += 4f * arcHeight * t * (1f - t);
            transform.position = pos;

            transform.Rotate(tumbleSpeed * Time.deltaTime, Space.Self);

            if (t >= 1f) Explode(transform.position);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasLanded || elapsed < 0.15f) return;
            if (other.GetComponentInParent<SiegeWeaponBase>() != null) return;
            Explode(transform.position);
        }

        private void OnCollisionEnter(Collision col)
        {
            if (hasLanded || elapsed < 0.15f) return;
            if (col.gameObject.GetComponentInParent<SiegeWeaponBase>() != null) return;
            Explode(col.contacts.Length > 0 ? col.contacts[0].point : transform.position);
        }

        private void Explode(Vector3 point)
        {
            if (hasLanded) return;
            hasLanded = true;

            SpawnImpactEffect(point, Quaternion.identity);

            Collider[] hits = Physics.OverlapSphere(point, explosionRadius, targetLayer, QueryTriggerInteraction.Collide);
            HashSet<GameObject> damaged = new HashSet<GameObject>();

            foreach (var col in hits)
            {
                if (col.gameObject == gameObject) continue;

                EnemyStats stats = col.GetComponentInParent<EnemyStats>();
                GameObject enemyObj = stats != null ? stats.gameObject : null;

                if (enemyObj == null && (col.CompareTag(targetTag) || col.transform.root.CompareTag(targetTag)))
                    enemyObj = col.gameObject;

                if (enemyObj != null && damaged.Add(enemyObj))
                    ApplyDamage(enemyObj);
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
