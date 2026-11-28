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
        private static readonly Collider[] OverlapBuffer = new Collider[64];

        [Header("Targeting & Range")]
        [Tooltip("Tag của mục tiêu cần bắn")]
        [SerializeField] protected string targetTag = "Enemy";

        [Tooltip("Bán kính phát hiện và tấn công mục tiêu")]
        [SerializeField] protected float attackRange = 25f;

        [Tooltip("LayerMask quét mục tiêu")]
        [SerializeField] protected LayerMask targetLayer = ~0;

        [Tooltip("Chu kỳ quét lại mục tiêu khi chưa có target (giây)")]
        [SerializeField, Min(0.05f)] protected float targetScanInterval = 0.2f;

        [Header("Weapon Stats")]
        [Tooltip("Thời gian hồi giữa các phát bắn (giây)")]
        [SerializeField] protected float fireRate = 2f;

        [Tooltip("Sát thương mỗi phát bắn")]
        [SerializeField] protected float attackDamage = 50f;

        [Tooltip("Tốc độ đạn bay (có thể chỉnh trực tiếp tại đây hoặc ghi đè bằng Flight Speed Override trên prefab đạn)")]
        [SerializeField, Min(1f)] protected float projectileSpeed = 25f;

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
        [Tooltip("Bật để đạn bắn ra khi kết thúc animation Fire thay vì dùng projectileSpawnDelay")]
        [SerializeField] protected bool spawnAtAnimationEnd = true;

        [Tooltip("Tên state animation bắn trong Animator Controller")]
        [SerializeField] protected string fireStateName = "Fire";

        [Tooltip("Thời điểm normalizedTime trong state Fire để phóng đạn (0.95 = ngay cuối animation trước khi chuyển về Idle)")]
        [SerializeField, Range(0.1f, 1f)] protected float fireReleaseNormalizedTime = 0.95f;

        [Tooltip("Thời gian delay từ khi kích hoạt animation đến khi sinh đạn (dùng khi tắt spawnAtAnimationEnd)")]
        [SerializeField] protected float projectileSpawnDelay = 0.15f;

        [SerializeField] protected Animator animator;
        [SerializeField] protected string fireAnimationTrigger = "Fire";
        [SerializeField] protected AudioSource audioSource;
        [SerializeField] protected AudioClip fireSound;

        protected Transform currentTarget;
        protected EnemyStats currentTargetStats;
        protected float nextFireTime;
        protected bool isFiringInProgress;

        private int fireTriggerHash;
        private int fireStateHash;
        private float nextTargetScanTime;
        private Coroutine reloadVisualCoroutine;

        public float AttackRange => attackRange;
        public float AttackDamage => attackDamage;
        public Transform CurrentTarget => currentTarget;

        protected virtual void Awake()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            fireTriggerHash = Animator.StringToHash(fireAnimationTrigger);
            fireStateHash = Animator.StringToHash(fireStateName);
            nextTargetScanTime = Time.time + Random.Range(0f, targetScanInterval);

            if (firePoint == null)
                firePoint = FindFirePoint();
        }

        protected virtual void OnDisable()
        {
            StopAllCoroutines();
            isFiringInProgress = false;
            reloadVisualCoroutine = null;
        }

        protected virtual Transform FindFirePoint()
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
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
            UpdateTarget(false);

            if (currentTarget == null) return;

            AimAtTarget();

            if (Time.time >= nextFireTime && !isFiringInProgress)
            {
                nextFireTime = Time.time + fireRate;
                StartCoroutine(ExecuteFireSequence());
            }
        }

        protected virtual float FireReleaseNormalizedTime => fireReleaseNormalizedTime;

        protected virtual void OnFireSequenceStarted() { }

        private IEnumerator ExecuteFireSequence()
        {
            isFiringInProgress = true;
            OnFireSequenceStarted();

            if (animator != null)
            {
                animator.ResetTrigger(fireTriggerHash);
                animator.SetTrigger(fireTriggerHash);
            }
            if (audioSource != null && fireSound != null)
                audioSource.PlayOneShot(fireSound);

            if (animator != null && spawnAtAnimationEnd)
                yield return StartCoroutine(WaitForFireAnimationEnd());
            else if (projectileSpawnDelay > 0f)
                yield return StartCoroutine(WaitForDuration(projectileSpawnDelay));

            if (!IsValidTarget(currentTarget))
                UpdateTarget(true);

            if (currentTarget != null)
                SpawnAndLaunchProjectile();

            isFiringInProgress = false;
        }

        private IEnumerator WaitForFireAnimationEnd()
        {
            const float enterTimeout = 1.0f;
            float enterElapsed = 0f;
            while (enterElapsed < enterTimeout)
            {
                if (animator == null) yield break;
                if (animator.GetCurrentAnimatorStateInfo(0).shortNameHash == fireStateHash)
                    break;

                enterElapsed += Time.deltaTime;
                yield return null;
            }

            if (animator == null || animator.GetCurrentAnimatorStateInfo(0).shortNameHash != fireStateHash)
                yield break;

            const float playTimeout = 3.0f;
            float playElapsed = 0f;
            while (playElapsed < playTimeout)
            {
                if (animator == null) yield break;
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.shortNameHash != fireStateHash || stateInfo.normalizedTime >= FireReleaseNormalizedTime)
                    break;

                playElapsed += Time.deltaTime;
                yield return null;
            }
        }

        protected static IEnumerator WaitForDuration(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        protected abstract void SpawnAndLaunchProjectile();

        protected virtual void UpdateTarget(bool forceScan = false)
        {
            float rangeSqr = attackRange * attackRange;
            if (IsValidTarget(currentTarget))
            {
                if ((currentTarget.position - transform.position).sqrMagnitude <= rangeSqr)
                    return;
            }

            currentTarget = null;
            currentTargetStats = null;

            if (!forceScan && Time.time < nextTargetScanTime)
                return;

            nextTargetScanTime = Time.time + targetScanInterval;

            float bestDistSqr = rangeSqr;
            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position, attackRange, OverlapBuffer, targetLayer, QueryTriggerInteraction.Collide);

            for (int i = 0; i < hitCount; i++)
            {
                Collider col = OverlapBuffer[i];
                if (col == null || col.transform.IsChildOf(transform)) continue;

                Transform root = FindEnemyRoot(col.gameObject);
                if (root == null || !IsValidTarget(root)) continue;

                float dSqr = (root.position - transform.position).sqrMagnitude;
                if (dSqr < bestDistSqr)
                {
                    bestDistSqr = dSqr;
                    currentTarget = root;
                }
            }

            if (currentTarget != null)
                currentTargetStats = currentTarget.GetComponentInParent<EnemyStats>();
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
            EnemyStats stats = t == currentTarget ? currentTargetStats : t.GetComponentInParent<EnemyStats>();
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

        #region Shared Loaded Projectile Helpers

        protected static Transform FindChildByExactName(Transform[] children, string exactLowerName)
        {
            foreach (Transform t in children)
            {
                if (t.name.ToLowerInvariant() == exactLowerName)
                    return t;
            }
            return null;
        }

        protected static bool IsChildOfAny(Transform t, Transform a, Transform b)
        {
            if (t == null) return false;
            return (a != null && t.IsChildOf(a)) || (b != null && t.IsChildOf(b));
        }

        protected GameObject FindLoadedProjectileVisual<TProjectile>(
            Transform primaryBone, string[] socketKeywords, string[] ignorePrefixes, string[] nameKeywords)
            where TProjectile : ProjectileBase
        {
            foreach (TProjectile proj in GetComponentsInChildren<TProjectile>(true))
            {
                if (proj != null && proj.gameObject != gameObject)
                    return proj.gameObject;
            }

            Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
            foreach (Transform t in allTransforms)
            {
                string n = t.name.ToLowerInvariant();
                bool isSocket = t == primaryBone || t == firePoint || ContainsAnyKeyword(n, socketKeywords);
                if (!isSocket) continue;

                foreach (Renderer r in t.GetComponentsInChildren<Renderer>(true))
                {
                    if (r != null && r.gameObject != gameObject)
                        return r.gameObject;
                }
            }

            Renderer fallbackRenderer = null;
            foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            {
                if (r == null || r.gameObject == gameObject) continue;
                string n = r.gameObject.name.ToLowerInvariant();
                if (StartsWithAnyPrefix(n, ignorePrefixes)) continue;

                if (ContainsAnyKeyword(n, nameKeywords))
                    return r.gameObject;

                if (fallbackRenderer == null)
                    fallbackRenderer = r;
            }

            return fallbackRenderer != null ? fallbackRenderer.gameObject : null;
        }

        protected void PrepareLoadedProjectileVisual(GameObject visual, Transform attachBone, Transform parentArmBone)
        {
            if (visual == null) return;

            if (attachBone != null && !IsChildOfAny(visual.transform, attachBone, parentArmBone))
                visual.transform.SetParent(attachBone, true);

            foreach (ProjectileBase proj in visual.GetComponentsInChildren<ProjectileBase>(true))
                proj.enabled = false;

            foreach (Collider col in visual.GetComponentsInChildren<Collider>(true))
                col.enabled = false;

            foreach (Rigidbody rb in visual.GetComponentsInChildren<Rigidbody>(true))
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.detectCollisions = false;
            }
        }

        protected static void ApplyMatchingWorldScale(GameObject spawned, GameObject visual)
        {
            if (spawned == null || visual == null) return;

            Vector3 worldScale = visual.transform.lossyScale;
            if (worldScale.x > 0.01f && worldScale.y > 0.01f && worldScale.z > 0.01f)
                spawned.transform.localScale = worldScale;
        }

        protected static void SetVisualVisible(GameObject visual, bool visible)
        {
            if (visual != null && visual.activeSelf != visible)
                visual.SetActive(visible);
        }

        protected void CancelReloadVisual(GameObject visual)
        {
            if (reloadVisualCoroutine != null)
            {
                StopCoroutine(reloadVisualCoroutine);
                reloadVisualCoroutine = null;
            }

            SetVisualVisible(visual, true);
        }

        protected void ScheduleReloadVisual(
            GameObject visual, string reloadStateName, float showNormalizedTime,
            float extraDelay, float fallbackDelay, float playTimeout)
        {
            if (visual == null) return;

            if (reloadVisualCoroutine != null)
                StopCoroutine(reloadVisualCoroutine);

            reloadVisualCoroutine = StartCoroutine(
                ShowVisualWhenReloadsRoutine(visual, reloadStateName, showNormalizedTime, extraDelay, fallbackDelay, playTimeout));
        }

        private IEnumerator ShowVisualWhenReloadsRoutine(
            GameObject visual, string reloadStateName, float showNormalizedTime,
            float extraDelay, float fallbackDelay, float playTimeout)
        {
            if (animator != null)
            {
                yield return null;

                int reloadStateHash = Animator.StringToHash(reloadStateName);
                const float enterTimeout = 1.5f;
                float enterElapsed = 0f;
                while (enterElapsed < enterTimeout)
                {
                    if (animator == null) yield break;
                    if (animator.GetCurrentAnimatorStateInfo(0).shortNameHash == reloadStateHash)
                        break;

                    enterElapsed += Time.deltaTime;
                    yield return null;
                }

                float playElapsed = 0f;
                while (playElapsed < playTimeout)
                {
                    if (animator == null) yield break;
                    AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                    if (stateInfo.shortNameHash != reloadStateHash || stateInfo.normalizedTime >= showNormalizedTime)
                        break;

                    playElapsed += Time.deltaTime;
                    yield return null;
                }
            }
            else
            {
                yield return StartCoroutine(WaitForDuration(fallbackDelay));
            }

            if (extraDelay > 0f)
                yield return StartCoroutine(WaitForDuration(extraDelay));

            SetVisualVisible(visual, true);
            reloadVisualCoroutine = null;
        }

        private static bool ContainsAnyKeyword(string text, string[] keywords)
        {
            if (keywords == null) return false;
            for (int i = 0; i < keywords.Length; i++)
            {
                if (text.Contains(keywords[i]))
                    return true;
            }
            return false;
        }

        private static bool StartsWithAnyPrefix(string text, string[] prefixes)
        {
            if (prefixes == null) return false;
            for (int i = 0; i < prefixes.Length; i++)
            {
                if (text.StartsWith(prefixes[i]))
                    return true;
            }
            return false;
        }

        #endregion

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
