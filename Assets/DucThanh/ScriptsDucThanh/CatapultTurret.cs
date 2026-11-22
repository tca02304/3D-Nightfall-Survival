using UnityEngine;

namespace DucThanh
{
    /// <summary>
    /// Vũ khí Máy Bắn Đá (Catapult): phóng đá theo quỹ đạo parabol, gây sát thương AoE khi chạm đất.
    /// </summary>
    [SelectionBase]
    public class CatapultTurret : SiegeWeaponBase
    {
        [Header("Catapult Specific")]
        [Tooltip("Khoảng cách tối thiểu để bắn (đá cần đủ tầm bay)")]
        [SerializeField] private float minAttackRange = 5f;

        private void Reset()
        {
            targetTag      = "Enemy";
            attackRange    = 35f;
            fireRate       = 3.5f;
            attackDamage   = 80f;
            projectileSpeed = 18f;
            rotationSpeed  = 5f;
            projectileSpawnDelay = 0.375f;
            flipAimDirection = true;
            minAttackRange = 5f;
        }

        protected override bool IsValidTarget(Transform target)
        {
            if (!base.IsValidTarget(target)) return false;
            return (target.position - transform.position).sqrMagnitude >= minAttackRange * minAttackRange;
        }

        protected override void SpawnAndLaunchProjectile()
        {
            if (currentTarget == null) return;

            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + Vector3.up * 2f;
            Vector3 aimDir   = (currentTarget.position - spawnPos).normalized;
            if (aimDir == Vector3.zero) aimDir = transform.forward;

            GameObject obj = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(aimDir));

            if (!obj.TryGetComponent(out BoulderProjectile boulder))
                boulder = obj.AddComponent<BoulderProjectile>();

            boulder.Initialize(currentTarget, attackDamage, projectileSpeed, targetTag);
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, minAttackRange);
        }
    }
}
