using UnityEngine;

namespace DucThanh
{
    /// <summary>
    /// Vũ khí Nỏ (Ballista): bắn mũi tên tốc độ cao về phía enemy gần nhất trong tầm.
    /// </summary>
    [SelectionBase]
    public class CrossbowTurret : SiegeWeaponBase
    {
        private static readonly string[] SocketKeywords =
        {
            "socket_bolt", "socket_ballista_bolt", "projectile_anchor", "socket_ballista_projectile_anchor", "firepoin"
        };
        private static readonly string[] IgnorePrefixes = { "geo_ballista", "sm_ballista" };
        private static readonly string[] BoltNameKeywords = { "tên", "ten", "bolt", "arrow", "spear" };

        [Header("Barrel Bolt Visual")]
        [Tooltip("Mũi tên hiển thị nằm sẵn trong nòng nỏ (để trống sẽ tự tìm hoặc tự tạo từ projectilePrefab)")]
        [SerializeField] private GameObject loadedBoltVisual;

        [Tooltip("Tự động tạo mũi tên hiển thị trong nòng từ projectilePrefab nếu chưa gắn sẵn")]
        [SerializeField] private bool autoCreateLoadedBolt = true;

        [Tooltip("Góc xoay bù cho mũi tên hiển thị tự tạo (khớp với visualRotationOffset của ArrowProjectile)")]
        [SerializeField] private Vector3 loadedBoltEulerOffset = new Vector3(-90f, 0f, 0f);

        [Tooltip("Thời điểm normalizedTime trong animation Fire để phóng tên (Ballista_Fire nhả dây khoảng 0.125)")]
        [SerializeField, Range(0.05f, 1f)] private float crossbowReleaseNormalizedTime = 0.13f;

        [Tooltip("Tên state animation nạp tên trong Animator Controller")]
        [SerializeField] private string reloadStateName = "Idle";

        [Tooltip("Thời điểm normalizedTime trong state nạp tên để hiện lại mũi tên mới trong nòng")]
        [SerializeField, Range(0f, 1f)] private float reloadBoltShowNormalizedTime = 0.15f;

        [Tooltip("Độ trễ thêm (giây) sau khi nạp rồi mới hiện lại mũi tên trong nòng")]
        [SerializeField, Min(0f)] private float reloadBoltExtraDelay = 0.1f;

        [Tooltip("Giữ nguyên kích thước hiển thị của mũi tên trong nòng khi bắn ra")]
        [SerializeField] private bool matchLoadedBoltScale = true;

        protected override float FireReleaseNormalizedTime => crossbowReleaseNormalizedTime;

        private Transform carriageBone;
        private Transform animatedBoltBone;

        protected override void Awake()
        {
            base.Awake();
            CacheAnimatedRigBones();

            if (loadedBoltVisual == null)
            {
                loadedBoltVisual = FindLoadedProjectileVisual<ArrowProjectile>(
                    animatedBoltBone, SocketKeywords, IgnorePrefixes, BoltNameKeywords);
            }

            if (loadedBoltVisual == null && autoCreateLoadedBolt && projectilePrefab != null)
                loadedBoltVisual = CreateLoadedBoltVisual();

            if (loadedBoltVisual != null)
            {
                PrepareLoadedProjectileVisual(loadedBoltVisual, animatedBoltBone, carriageBone);
                SetVisualVisible(loadedBoltVisual, true);
            }
        }

        private void CacheAnimatedRigBones()
        {
            if (carriageBone != null && animatedBoltBone != null) return;

            Transform[] children = GetComponentsInChildren<Transform>(true);
            carriageBone = FindChildByExactName(children, "carriage");
            animatedBoltBone = FindChildByExactName(children, "projectile_anchor")
                            ?? FindChildByExactName(children, "socket_bolt")
                            ?? carriageBone;
        }

        private void Reset()
        {
            targetTag      = "Enemy";
            attackRange    = 25f;
            fireRate       = 1.5f;
            attackDamage   = 45f;
            projectileSpeed = 35f;
            rotationSpeed  = 8f;
            spawnAtAnimationEnd = true;
            fireReleaseNormalizedTime = 0.13f;
            crossbowReleaseNormalizedTime = 0.13f;
            reloadBoltShowNormalizedTime = 0.15f;
            reloadBoltExtraDelay = 0.1f;
            projectileSpawnDelay = 0.125f;
            flipAimDirection = true;
        }

        protected override Transform FindFirePoint()
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            foreach (Transform t in children)
            {
                string n = t.name.ToLowerInvariant();
                if (n.Contains("firepoint") || n.Contains("firepoin"))
                    return t;
            }

            CacheAnimatedRigBones();
            return animatedBoltBone != null ? animatedBoltBone : base.FindFirePoint();
        }

        protected override void OnFireSequenceStarted()
        {
            CancelReloadVisual(loadedBoltVisual);
        }

        protected override void SpawnAndLaunchProjectile()
        {
            if (currentTarget == null || projectilePrefab == null) return;

            Vector3 spawnPos = loadedBoltVisual != null
                ? loadedBoltVisual.transform.position
                : (firePoint != null ? firePoint.position : transform.position + Vector3.up * 1.5f);

            Vector3 aimDir = (currentTarget.position + Vector3.up * 0.8f - spawnPos).normalized;
            if (aimDir == Vector3.zero) aimDir = transform.forward;

            GameObject obj = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(aimDir));

            if (matchLoadedBoltScale)
                ApplyMatchingWorldScale(obj, loadedBoltVisual);

            if (!obj.TryGetComponent(out ArrowProjectile arrow))
                arrow = obj.AddComponent<ArrowProjectile>();

            arrow.Initialize(currentTarget, attackDamage, projectileSpeed, targetTag);

            if (loadedBoltVisual != null)
            {
                SetVisualVisible(loadedBoltVisual, false);
                ScheduleReloadVisual(
                    loadedBoltVisual, reloadStateName, reloadBoltShowNormalizedTime,
                    reloadBoltExtraDelay, Mathf.Max(0.2f, fireRate * 0.5f), 6.0f);
            }
        }

        private GameObject CreateLoadedBoltVisual()
        {
            Transform anchor = animatedBoltBone != null ? animatedBoltBone : firePoint;
            if (anchor == null) return null;

            Vector3 barrelForward = flipAimDirection ? -transform.forward : transform.forward;
            Quaternion rot = Quaternion.LookRotation(barrelForward, transform.up) * Quaternion.Euler(loadedBoltEulerOffset);

            GameObject visual = Instantiate(projectilePrefab, anchor.position, rot);
            visual.name = "LoadedBolt_Visual";
            visual.transform.SetParent(anchor, true);
            return visual;
        }
    }
}
