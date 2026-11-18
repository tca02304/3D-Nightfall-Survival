using UnityEngine;

namespace DucThanh
{
    /// <summary>
    /// Đạn mũi tên nỏ: bay thẳng về phía mục tiêu, hỗ trợ homing nhẹ, gây sát thương đơn khi chạm.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ArrowProjectile : ProjectileBase
    {
        [Header("Arrow Settings")]
        [Tooltip("Bật để đạn tự điều chỉnh hướng bám theo mục tiêu khi bay")]
        [SerializeField] private bool homing = true;

        [Tooltip("Độ nhạy xoay khi bám mục tiêu (độ/giây)")]
        [SerializeField] private float homingSensitivity = 10f;

        [Tooltip("Offset xoay thêm để model mũi tên nằm đúng hướng bay.\n" +
                 "Nếu mũi tên đứng dọc (model FBX hướng lên Y): đặt (-90, 0, 0).\n" +
                 "Nếu mũi tên nằm ngang rồi: để (0, 0, 0).")]
        [SerializeField] private Vector3 visualRotationOffset = new Vector3(-90f, 0f, 0f);

        private Vector3 flightDir;

        public override void Initialize(Transform targetTransform, float damageAmount, float moveSpeed, string tag = "Enemy")
        {
            base.Initialize(targetTransform, damageAmount, moveSpeed, tag);

            flightDir = target != null
                ? (targetPosition - transform.position).normalized
                : transform.forward;

            if (flightDir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(flightDir) * Quaternion.Euler(visualRotationOffset);
        }

        private void Update()
        {
            if (!isInitialized) return;

            // Homing: cập nhật vị trí mục tiêu và dần xoay về phía đó
            if (homing && target != null && target.gameObject.activeInHierarchy)
            {
                EnemyStats stats = target.GetComponentInParent<EnemyStats>();
                if (stats == null || !stats.IsDead)
                {
                    targetPosition = target.position + Vector3.up * 0.8f;
                    Vector3 desired = (targetPosition - transform.position).normalized;
                    if (desired != Vector3.zero)
                        flightDir = Vector3.RotateTowards(flightDir, desired, homingSensitivity * Time.deltaTime, 0f);
                }
            }

            if (flightDir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(flightDir) * Quaternion.Euler(visualRotationOffset);

            transform.position += flightDir * (speed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other) =>
            HandleHit(other.gameObject, transform.position);

        private void OnCollisionEnter(Collision col) =>
            HandleHit(col.gameObject, col.contacts.Length > 0 ? col.contacts[0].point : transform.position);

        private void HandleHit(GameObject hitObj, Vector3 hitPoint)
        {
            if (hitObj == null) return;
            if (hitObj.GetComponentInParent<SiegeWeaponBase>() != null) return;

            bool isEnemy = hitObj.CompareTag(targetTag)
                        || hitObj.transform.root.CompareTag(targetTag)
                        || hitObj.GetComponentInParent<EnemyStats>() != null;

            if (isEnemy)
            {
                ApplyDamage(hitObj);
                SpawnImpactEffect(hitPoint, Quaternion.identity);
                Destroy(gameObject);
            }
            else if (!hitObj.isStatic)
            {
                SpawnImpactEffect(hitPoint, Quaternion.identity);
                Destroy(gameObject);
            }
        }
    }
}
