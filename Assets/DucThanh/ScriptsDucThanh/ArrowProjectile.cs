using UnityEngine;

namespace DucThanh
{
    /// <summary>
    /// Đạn mũi tên nỏ: bay thẳng về phía mục tiêu, hỗ trợ homing nhẹ, gây sát thương đơn khi chạm.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ArrowProjectile : ProjectileBase
    {
        private static readonly RaycastHit[] HitBuffer = new RaycastHit[16];

        [Header("Arrow Settings")]
        [Tooltip("Bật để đạn tự điều chỉnh hướng bám theo mục tiêu khi bay")]
        [SerializeField] private bool homing = true;

        [Tooltip("Độ nhạy xoay khi bám mục tiêu (độ/giây)")]
        [SerializeField] private float homingSensitivity = 10f;

        [Tooltip("Offset xoay thêm để model mũi tên nằm đúng hướng bay.\n" +
                 "Nếu mũi tên đứng dọc (model FBX hướng lên Y): đặt (-90, 0, 0).\n" +
                 "Nếu mũi tên nằm ngang rồi: để (0, 0, 0).")]
        [SerializeField] private Vector3 visualRotationOffset = new Vector3(-90f, 0f, 0f);

        [Tooltip("Bán kính quét va chạm mỗi frame để tránh tên bay xuyên qua quái khi tốc độ cao")]
        [SerializeField] private float hitSweepRadius = 0.25f;

        [Tooltip("Khoảng cách tối thiểu tới tâm mục tiêu để tính trúng đạn")]
        [SerializeField] private float targetHitRadius = 0.65f;

        private Vector3 flightDir;
        private bool hasHit;
        private bool hasPassedTarget;
        private EnemyStats cachedTargetStats;
        private Collider cachedTargetCollider;
        private Quaternion visualRotationOffsetQuat;

        private void Awake()
        {
            EnsurePhysicsComponents();
        }

        private void EnsurePhysicsComponents()
        {
            EnsureKinematicRigidbody(continuousSpeculative: true);

            Collider col = GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;
        }

        private Vector3 GetCurrentTargetAimPoint()
        {
            if (cachedTargetCollider != null && cachedTargetCollider.enabled)
                return cachedTargetCollider.bounds.center;

            return target != null ? target.position + Vector3.up * 0.8f : targetPosition;
        }

        public override void Initialize(Transform targetTransform, float damageAmount, float moveSpeed, string tag = "Enemy")
        {
            base.Initialize(targetTransform, damageAmount, moveSpeed, tag);
            EnsurePhysicsComponents();

            hasHit = false;
            hasPassedTarget = false;
            visualRotationOffsetQuat = Quaternion.Euler(visualRotationOffset);

            cachedTargetStats = target != null ? target.GetComponentInParent<EnemyStats>() : null;
            cachedTargetCollider = FindPrimaryTargetCollider(target);

            if (target != null)
                targetPosition = GetCurrentTargetAimPoint();

            flightDir = target != null
                ? (targetPosition - transform.position).normalized
                : transform.forward;

            if (flightDir == Vector3.zero)
                flightDir = transform.forward;

            if (flightDir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(flightDir) * visualRotationOffsetQuat;
        }

        private void Update()
        {
            if (!isInitialized || hasHit) return;

            if (homing && !hasPassedTarget && target != null && target.gameObject.activeInHierarchy)
            {
                if (cachedTargetStats != null && cachedTargetStats.IsDead)
                {
                    target = null;
                }
                else
                {
                    targetPosition = GetCurrentTargetAimPoint();
                    Vector3 toTarget = targetPosition - transform.position;
                    if (toTarget.sqrMagnitude > 0.0001f)
                    {
                        if (Vector3.Dot(flightDir, toTarget) < 0f)
                            hasPassedTarget = true;
                        else
                            flightDir = Vector3.RotateTowards(flightDir, toTarget.normalized, homingSensitivity * Time.deltaTime, 0f);
                    }
                }
            }

            Vector3 prevPos = transform.position;
            float stepDist = speed * Time.deltaTime;
            Vector3 nextPos = prevPos + flightDir * stepDist;

            if (stepDist > 0.0001f)
            {
                int hitCount = Physics.SphereCastNonAlloc(
                    prevPos, hitSweepRadius, flightDir, HitBuffer, stepDist, targetLayer, QueryTriggerInteraction.Collide);

                if (hitCount > 0)
                {
                    float closestDist = float.MaxValue;
                    RaycastHit closestHit = default;
                    bool foundHit = false;

                    for (int i = 0; i < hitCount; i++)
                    {
                        RaycastHit hit = HitBuffer[i];
                        if (hit.collider == null || ShouldIgnoreCollision(hit.collider.gameObject)) continue;

                        bool isValidHit = IsEnemyObject(hit.collider.gameObject) || !hit.collider.isTrigger;
                        if (isValidHit && hit.distance < closestDist)
                        {
                            closestDist = hit.distance;
                            closestHit = hit;
                            foundHit = true;
                        }
                    }

                    if (foundHit)
                    {
                        HandleHit(closestHit.collider.gameObject, closestHit.point, false);
                        return;
                    }
                }
            }

            if (target != null && target.gameObject.activeInHierarchy)
            {
                Vector3 seg = nextPos - prevPos;
                float segLenSqr = seg.sqrMagnitude;
                float proj = segLenSqr > 0.000001f
                    ? Mathf.Clamp01(Vector3.Dot(targetPosition - prevPos, seg) / segLenSqr)
                    : 0f;
                Vector3 closestPoint = prevPos + seg * proj;

                if ((closestPoint - targetPosition).sqrMagnitude <= targetHitRadius * targetHitRadius)
                {
                    HandleHit(target.gameObject, targetPosition, false);
                    return;
                }
            }

            if (flightDir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(flightDir) * visualRotationOffsetQuat;

            transform.position = nextPos;
        }

        private void OnTriggerEnter(Collider other) =>
            HandleHit(other.gameObject, transform.position, other.isTrigger);

        private void OnCollisionEnter(Collision col) =>
            HandleHit(col.gameObject, col.contacts.Length > 0 ? col.contacts[0].point : transform.position, false);

        private void HandleHit(GameObject hitObj, Vector3 hitPoint, bool isTriggerVolume)
        {
            if (hasHit || ShouldIgnoreCollision(hitObj)) return;

            if (IsEnemyObject(hitObj))
            {
                hasHit = true;
                ApplyDamage(hitObj);
                SpawnImpactEffect(hitPoint, Quaternion.identity);
                Destroy(gameObject);
            }
            else if (!isTriggerVolume)
            {
                hasHit = true;
                SpawnImpactEffect(hitPoint, Quaternion.identity);
                Destroy(gameObject);
            }
        }
    }
}
