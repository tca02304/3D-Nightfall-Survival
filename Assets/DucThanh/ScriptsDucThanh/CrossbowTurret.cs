using UnityEngine;

namespace DucThanh
{
    /// <summary>
    /// Vũ khí Nỏ (Ballista): bắn mũi tên tốc độ cao về phía enemy gần nhất trong tầm.
    /// </summary>
    [SelectionBase]
    public class CrossbowTurret : SiegeWeaponBase
    {
        private void Reset()
        {
            targetTag      = "Enemy";
            attackRange    = 25f;
            fireRate       = 1.5f;
            attackDamage   = 45f;
            projectileSpeed = 35f;
            rotationSpeed  = 8f;
            projectileSpawnDelay = 0.125f;
            flipAimDirection = true;
        }

        protected override void SpawnAndLaunchProjectile()
        {
            if (currentTarget == null) return;

            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1.5f;
            Vector3 aimDir   = (currentTarget.position + Vector3.up * 0.8f - spawnPos).normalized;
            if (aimDir == Vector3.zero) aimDir = transform.forward;

            GameObject obj = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(aimDir));

            if (!obj.TryGetComponent(out ArrowProjectile arrow))
                arrow = obj.AddComponent<ArrowProjectile>();

            arrow.Initialize(currentTarget, attackDamage, projectileSpeed, targetTag);
        }
    }
}
