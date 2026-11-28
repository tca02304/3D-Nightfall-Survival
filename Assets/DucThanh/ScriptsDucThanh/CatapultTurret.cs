using UnityEngine;

namespace DucThanh
{
    /// <summary>
    /// Vũ khí Máy Bắn Đá (Catapult): phóng đá theo quỹ đạo parabol, gây sát thương AoE khi chạm đất.
    /// </summary>
    [SelectionBase]
    public class CatapultTurret : SiegeWeaponBase
    {
        private static readonly string[] SocketKeywords =
        {
            "socket_cup", "socket_onager_cup", "projectile_anchor", "socket_onager_projectile_anchor", "firepoin"
        };
        private static readonly string[] IgnorePrefixes = { "geo_onager", "sm_onager" };
        private static readonly string[] StoneNameKeywords = { "đá", "da", "stone", "rock", "boulder", "sphere" };

        [Header("Catapult Specific")]
        [Tooltip("Khoảng cách tối thiểu để bắn (đá cần đủ tầm bay)")]
        [SerializeField] private float minAttackRange = 5f;

        [Header("Basket Stone Visual")]
        [Tooltip("Viên đá hiển thị trong rổ đựng (để trống sẽ tự tìm viên đá đã đặt trong rổ)")]
        [SerializeField] private GameObject loadedStoneVisual;

        [Tooltip("Tên state animation nạp/hạ cần trong Animator Controller")]
        [SerializeField] private string reloadStateName = "Idle";

        [Tooltip("Thời điểm normalizedTime trong state nạp đạn để hiện lại viên đá mới trong rổ (khi cần đã hạ xuống)")]
        [SerializeField, Range(0f, 1f)] private float reloadStoneShowNormalizedTime = 0.78f;

        [Tooltip("Giữ nguyên kích thước hiển thị của viên đá trong rổ khi bắn ra")]
        [SerializeField] private bool matchLoadedStoneScale = true;

        [Tooltip("Thời điểm normalizedTime trong animation Fire để phóng đá (Onager_Fire vung hết tầm khoảng 0.375 - 0.42)")]
        [SerializeField, Range(0.1f, 1f)] private float catapultReleaseNormalizedTime = 0.4f;

        [Tooltip("Độ trễ thêm (giây) sau khi cần hạ xuống rồi mới hiện lại viên đá mới trong rổ")]
        [SerializeField, Min(0f)] private float reloadStoneExtraDelay = 0.1f;

        protected override float FireReleaseNormalizedTime => catapultReleaseNormalizedTime;

        private Transform throwingArmBone;
        private Transform animatedCupBone;

        protected override void Awake()
        {
            base.Awake();
            CacheAnimatedRigBones();

            if (animatedCupBone != null && (firePoint == null || !IsChildOfAny(firePoint, animatedCupBone, throwingArmBone)))
                firePoint = animatedCupBone;

            if (loadedStoneVisual == null)
            {
                loadedStoneVisual = FindLoadedProjectileVisual<BoulderProjectile>(
                    animatedCupBone, SocketKeywords, IgnorePrefixes, StoneNameKeywords);
            }

            if (loadedStoneVisual != null)
            {
                PrepareLoadedProjectileVisual(loadedStoneVisual, animatedCupBone, throwingArmBone);
                SetVisualVisible(loadedStoneVisual, true);
            }
        }

        private void CacheAnimatedRigBones()
        {
            if (throwingArmBone != null && animatedCupBone != null) return;

            Transform[] children = GetComponentsInChildren<Transform>(true);
            throwingArmBone = FindChildByExactName(children, "throwing_arm");
            animatedCupBone = FindChildByExactName(children, "socket_cup")
                           ?? FindChildByExactName(children, "projectile_anchor")
                           ?? throwingArmBone;
        }

        private void Reset()
        {
            targetTag      = "Enemy";
            attackRange    = 35f;
            fireRate       = 3.5f;
            attackDamage   = 80f;
            projectileSpeed = 28f;
            rotationSpeed  = 5f;
            spawnAtAnimationEnd = true;
            fireReleaseNormalizedTime = 0.4f;
            catapultReleaseNormalizedTime = 0.4f;
            reloadStoneExtraDelay = 0.1f;
            projectileSpawnDelay = 0.375f;
            flipAimDirection = true;
            minAttackRange = 5f;
        }

        protected override Transform FindFirePoint()
        {
            CacheAnimatedRigBones();
            return animatedCupBone != null ? animatedCupBone : base.FindFirePoint();
        }

        protected override bool IsValidTarget(Transform target)
        {
            if (!base.IsValidTarget(target)) return false;
            return (target.position - transform.position).sqrMagnitude >= minAttackRange * minAttackRange;
        }

        protected override void OnFireSequenceStarted()
        {
            CancelReloadVisual(loadedStoneVisual);
        }

        protected override void SpawnAndLaunchProjectile()
        {
            if (currentTarget == null || projectilePrefab == null) return;

            Vector3 spawnPos = loadedStoneVisual != null
                ? loadedStoneVisual.transform.position
                : (firePoint != null ? firePoint.position : transform.position + Vector3.up * 2f);

            Vector3 aimDir = (currentTarget.position - spawnPos).normalized;
            if (aimDir == Vector3.zero) aimDir = transform.forward;

            Quaternion spawnRot = loadedStoneVisual != null
                ? loadedStoneVisual.transform.rotation
                : Quaternion.LookRotation(aimDir);

            GameObject obj = Instantiate(projectilePrefab, spawnPos, spawnRot);

            if (matchLoadedStoneScale)
                ApplyMatchingWorldScale(obj, loadedStoneVisual);

            if (!obj.TryGetComponent(out BoulderProjectile boulder))
                boulder = obj.AddComponent<BoulderProjectile>();

            boulder.Initialize(currentTarget, attackDamage, projectileSpeed, targetTag);

            if (loadedStoneVisual != null)
            {
                SetVisualVisible(loadedStoneVisual, false);
                ScheduleReloadVisual(
                    loadedStoneVisual, reloadStateName, reloadStoneShowNormalizedTime,
                    reloadStoneExtraDelay, Mathf.Max(0.2f, fireRate * 0.6f), 4.0f);
            }
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, minAttackRange);
        }
    }
}
