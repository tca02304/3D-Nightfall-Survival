using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshEnemyAI : MonoBehaviour
{
    public enum AIState
    {
        Patrol,  // 1. Tuần tra (Vòng tròn nhỏ khi cách player >= 50m)
        Chase,   // 2. Truy đuổi (Chạy về phía player khi cách <= 49m)
        Attack,  // 3. Tấn công (Chạy animation attack khi chạm trigger collider / áp sát player)
        Blocked, // 4. Bị chặn (Dốc quá cao / không có đường tới player -> đứng ở điểm gần nhất)
        Dead     // 5. Đã chết (gọi Die())
    }

    [Header("Current State")]
    public AIState currentState = AIState.Patrol;

    [Header("Detection & Distance Settings")]
    public float patrolDistance = 50.0f; // Vùng tuần tra (>= 50m)
    public float chaseDistance = 49.0f;  // Vùng truy đuổi (<= 49m)
    [Tooltip("Tầm đánh (m): khoảng cách từ ĐIỂM TẤN CÔNG của quái (mỏ/móng) tới BỀ MẶT collider của Player. " +
             "Nhỏ = phải áp sát mới đánh. Gợi ý 0.3 - 0.8.")]
    public float attackReach = 0.5f;
    [Tooltip("Đang đánh thì chỉ thoát khỏi trạng thái Attack khi Player cách xa hơn attackReach + giá trị này (chống chớp Attack<->Chase ở rìa tầm đánh).")]
    public float attackExitMargin = 0.4f;
    [Tooltip("Quái chỉ ra đòn khi Player nằm trong góc này so với hướng mặt (độ). Ngoài góc thì quay mặt trước.")]
    [Range(5f, 180f)] public float attackAngle = 50f;
    [Tooltip("Điểm tính tầm đánh (mỏ/móng). Để trống = tự tìm object con tên 'AttackPos', không có thì dùng vị trí quái + hitOriginHeight.")]
    public Transform attackPoint;

    [Header("Patrol Circle Settings")]
    public float patrolRadius = 5.0f;    // Bán kính đường tròn tuần tra
    public float patrolSpeed = 2.5f;     // Tốc độ tuần tra
    public float angularSpeed = 1.2f;    // Tốc độ quay quanh tâm tuần tra

    [Header("Chase Settings")]
    public float chaseSpeed = 5.0f;      // Tốc độ chạy truy đuổi

    [Header("Animation (Blend Tree)")]
    [Tooltip("Thời gian làm mượt tham số Speed gửi vào Blend Tree (giây). Blend Tree nên dùng tham số 'Speed': Idle=0, Walk=patrolSpeed, Run=chaseSpeed.")]
    public float speedDampTime = 0.1f;

    [Header("Terrain & Slope Settings")]
    [Tooltip("Góc dốc tối đa quái có thể leo (độ). Dốc hơn mức này sẽ bị block.")]
    [Range(1f, 89f)] public float maxSlopeAngle = 35f;
    [Tooltip("Layer của Terrain / mặt đất (dùng cho Raycast kiểm tra độ dốc). Nên loại layer Player/Enemy ra.")]
    public LayerMask groundMask = ~0;
    [Tooltip("Khoảng cách giữa các điểm lấy mẫu độ dốc dọc đường đi (m).")]
    public float slopeSampleStep = 1.0f;
    [Tooltip("Bán kính tìm điểm NavMesh gần Player nhất (m).")]
    public float navSampleDistance = 3.0f;
    [Tooltip("Bán kính mở rộng khi Player đứng ngoài NavMesh (vách dốc...) để tìm điểm gần nhất.")]
    public float navSampleDistanceFar = 15.0f;
    [Tooltip("Chu kỳ tính lại đường đi tới Player (giây).")]
    public float repathInterval = 0.25f;
    [Tooltip("Chiều cao quái bám mặt đất khi không có NavMesh (chế độ dự phòng).")]
    public float groundOffset = 0.0f;

    [Header("Attack Settings")]
    public float attackCooldown = 1.0f;  // Thời gian giãn cách giữa các đòn đánh
    private float nextAttackTime = 0f;

    [Header("Attack FX Settings")]
    public GameObject fxPrefab;              // Prefab FX sinh ra mỗi lần tấn công
    public Transform fxPoint;                // Vị trí sinh FX (miệng, móng...). Để trống = vị trí quái
    public float fxLifeTime = 2f;            // Thời gian tự xóa FX
    [Tooltip("Bật = FX nổ tại điểm trên người Player gần quái nhất (vị trí gây sát thương). Tắt = nổ tại Fx Point / giữa quái.")]
    public bool spawnFxAtHitPoint = true;
    [Tooltip("Độ cao điểm bắt đầu dò (so với chân quái) để tính điểm chạm. Nên ~ ngang miệng/móng.")]
    public float hitOriginHeight = 1.0f;
    [Tooltip("Đẩy FX ra khỏi bề mặt Player một chút về phía quái để không bị chìm trong model.")]
    public float hitPointPullBack = 0.1f;
    [Tooltip("Bật = FX chạy theo Animation Event 'SpawnFx' trong clip (đúng keyframe, mỗi lần animation chạy). Tắt = FX tự chạy khi bắt đầu đòn đánh.")]
    public bool useAnimationEvent = true;
    private float lastFxTime = -10f;
    private int attackStateHash = 0;   // state tấn công đã tìm thấy trong Animator
    private int attackLayer = 0;

    [Header("Target & Tag")]
    public Transform playerTarget;
    public string playerTag = "Player";

    [Header("Trigger Collider Settings")]
    [Tooltip("Mặc định TẮT: tầm đánh do attackReach quyết định (chính xác hơn hình cầu). Bật nếu muốn Player chạm vùng trigger cũng bị đánh.")]
    public bool useTriggerForAttack = false;
    public float triggerRadius = 2.5f;   // Bán kính Trigger Collider (chỉ dùng khi useTriggerForAttack = true)
    [Tooltip("Bật = chỉ tính collider nằm trên object gốc của Player (thân / CharacterController). " +
             "Collider của vũ khí hay các object con sẽ bị bỏ qua, tránh quái đánh sớm khi mới chạm mũi kiếm.")]
    public bool bodyColliderOnly = true;

    [Header("Death")]
    [Tooltip("Tên Trigger trong Animator để vào state chết (Any State -> Death). Để trống / không có parameter thì script sẽ CrossFade theo tên state bên dưới.")]
    public string dieTriggerName = "Die";
    public string[] deathStateNames = { "Death", "Die", "LilDave_Death" };
    [Tooltip("Sau bao nhiêu giây thì xóa xác. 0 = giữ xác mãi.")]
    public float corpseLifetime = 0f;
    public bool IsDead { get; private set; }

    private NavMeshAgent agent;
    private Animator animator;
    private Vector3 spawnPosition;
    private float patrolAngle = 0f;
    private Collider playerBody;       // collider thân của Player (CharacterController)

    // Danh sách các collider của Player đang nằm trong vùng trigger.
    // Dùng tập hợp thay cho 1 biến bool để nhiều collider (thân + vũ khí...) không ghi đè nhau.
    private readonly HashSet<Collider> touching = new HashSet<Collider>();
    private bool isPlayerInTrigger { get { return touching.Count > 0; } }

    // --- Path finding ---
    private NavMeshPath navPath;
    private float nextRepathTime = 0f;
    private bool pathBlocked = false;
    private Vector3[] debugCorners;
    private float manualSpeed = 0f; // tốc độ khi di chuyển thủ công (không có NavMesh)

    // Animator Hash Parameters
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
    private static readonly int MeleeTriggerHash = Animator.StringToHash("Melee");
    private static readonly int BiteTriggerHash = Animator.StringToHash("Bite");
    private int dieTriggerHash;
    private readonly HashSet<int> animParams = new HashSet<int>();

    private static readonly string[] AttackStateNames = new string[] {
        "GoblinMelee",   // Goblin
        "WolfAttack",    // Wolf
        "AttackMelee",   // LilDave
        "Attack",        // Chung
        "AttackRange",   // LilDave ranged
        "Bite"           // Wolf
    };

    private void Awake()
    {
        // 1. Cấu hình Animator & bật Layer Weight để animation đánh hiển thị 100%
        animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.applyRootMotion = false; // Tắt root motion để NavMesh di chuyển chuẩn
            for (int i = 1; i < animator.layerCount; i++)
            {
                animator.SetLayerWeight(i, 1.0f);
            }

            // Lưu sẵn danh sách parameter (animator.parameters tạo mảng mới mỗi lần gọi)
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                animParams.Add(param.nameHash);
            }
        }
        dieTriggerHash = string.IsNullOrEmpty(dieTriggerName) ? 0 : Animator.StringToHash(dieTriggerName);

        // 2. Rigidbody Kinematic: quái bị NavMeshAgent dịch chuyển liên tục nên cần Rigidbody
        //    để collider được coi là vật thể động và trigger hoạt động ổn định.
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;

        // 3. Cấu hình NavMeshAgent
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
        }

        if (agent != null)
        {
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.stoppingDistance = 1.2f;
            agent.acceleration = 16.0f;
            agent.angularSpeed = 360.0f;
            agent.speed = chaseSpeed;
            agent.autoRepath = true;
            agent.areaMask = NavMesh.AllAreas;
        }

        navPath = new NavMeshPath();

        // 4. Điểm tính tầm đánh (mỏ/móng)
        if (attackPoint == null) attackPoint = FindDeepChild(transform, "AttackPos");

        // 5. Trigger SphereCollider (chỉ cần khi dùng trigger để đánh)
        if (useTriggerForAttack) EnsureTriggerCollider();
    }

    private void Start()
    {
        spawnPosition = transform.position;
        FindPlayer();
    }

    private void Update()
    {
        if (IsDead) return;

        if (playerTarget == null)
        {
            FindPlayer();
        }

        // Collider bị tắt / xóa không gọi OnTriggerExit -> dọn thủ công
        touching.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);

        float distanceToPlayer = (playerTarget != null)
            ? Vector3.Distance(transform.position, playerTarget.position)
            : float.MaxValue;

        // Tầm đánh: từ mỏ/móng tới BỀ MẶT Player. Đang đánh thì nới thêm attackExitMargin (chống chớp trạng thái).
        bool wasAttacking = currentState == AIState.Attack;
        float reach = attackReach + (wasAttacking ? attackExitMargin : 0f);
        bool inAttackRange = playerTarget != null && DistanceToPlayerSurface(GetAttackOrigin()) <= reach;
        if (useTriggerForAttack && isPlayerInTrigger) inAttackRange = true;

        // Đang chạy animation đánh thì "cam kết" ra hết đòn: đứng yên + quay mặt, không bị Chase kéo đi
        bool attackAnimPlaying = IsPlayingAttackState();

        manualSpeed = 0f;

        if ((inAttackRange || attackAnimPlaying) && playerTarget != null)
        {
            currentState = AIState.Attack;
            PerformAttack(inAttackRange);
        }
        else if (distanceToPlayer <= chaseDistance && playerTarget != null)
        {
            // Chase hoặc Blocked (PerformChase sẽ tự đặt currentState)
            PerformChase();
        }
        else
        {
            currentState = AIState.Patrol;
            pathBlocked = false;
            PerformPatrol();
        }

        UpdateLocomotionAnimation();
    }

    // =====================================================================
    //  PATROL
    // =====================================================================
    private void PerformPatrol()
    {
        patrolAngle += angularSpeed * Time.deltaTime;
        if (patrolAngle >= Mathf.PI * 2f) patrolAngle -= Mathf.PI * 2f;

        Vector3 targetOffset = new Vector3(Mathf.Cos(patrolAngle) * patrolRadius, 0, Mathf.Sin(patrolAngle) * patrolRadius);
        Vector3 targetPos = spawnPosition + targetOffset;

        if (agent != null && agent.isOnNavMesh)
        {
            // Bám vào NavMesh để không đi lên chỗ dốc không đi được
            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 3f, agent.areaMask))
                targetPos = hit.position;

            agent.isStopped = false;
            agent.speed = patrolSpeed;
            agent.SetDestination(targetPos);
        }
        else
        {
            if (ManualMoveOnTerrain(targetPos, patrolSpeed)) manualSpeed = patrolSpeed;
        }
    }

    // =====================================================================
    //  CHASE (theo địa hình + raycast/tìm đường gần nhất)
    // =====================================================================
    private void PerformChase()
    {
        if (playerTarget == null) return;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = chaseSpeed;

            // Tính lại đường đi theo chu kỳ (tránh tốn CPU khi gọi mỗi frame)
            if (Time.time >= nextRepathTime)
            {
                nextRepathTime = Time.time + repathInterval;
                EvaluateChasePath();
            }

            if (pathBlocked)
            {
                // Đã tới điểm gần nhất có thể tới -> đứng lại, nhìn về Player
                bool arrived = !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.3f;
                if (arrived)
                {
                    currentState = AIState.Blocked;
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                    FaceTarget(playerTarget.position, 8f);
                    return;
                }
            }

            currentState = AIState.Chase;
        }
        else
        {
            // Không có NavMesh -> di chuyển thủ công bám Terrain + chặn dốc
            bool moved = ManualMoveOnTerrain(playerTarget.position, chaseSpeed);
            currentState = moved ? AIState.Chase : AIState.Blocked;
            if (moved) manualSpeed = chaseSpeed;
        }
    }

    /// <summary>
    /// 1) Raycast NavMesh thẳng tới Player -> nếu thông thì đi thẳng (đường gần nhất).
    /// 2) Nếu bị chắn -> CalculatePath (A*) để lấy đường vòng ngắn nhất.
    /// 3) Kiểm tra độ dốc dọc đường; gặp dốc > maxSlopeAngle thì cắt đường tại điểm an toàn cuối cùng.
    /// 4) Không có đường -> đi tới điểm gần Player nhất có thể tới được và bị Blocked.
    /// </summary>
    private void EvaluateChasePath()
    {
        Vector3 from = transform.position;
        pathBlocked = false;

        // --- Tìm điểm trên NavMesh gần Player ---
        Vector3 dest;
        if (NavMesh.SamplePosition(playerTarget.position, out NavMeshHit hit, navSampleDistance, agent.areaMask))
        {
            dest = hit.position;
        }
        else if (NavMesh.SamplePosition(playerTarget.position, out hit, navSampleDistanceFar, agent.areaMask))
        {
            // Player đứng trên chỗ không đi được (vách dốc) -> chỉ tới được điểm gần nhất
            dest = hit.position;
            pathBlocked = true;
        }
        else
        {
            // Không tìm thấy gì -> đứng yên
            pathBlocked = true;
            debugCorners = null;
            agent.SetDestination(from);
            return;
        }

        // --- 1. Raycast NavMesh: đường thẳng có thông không? ---
        Vector3[] corners;
        if (!NavMesh.Raycast(from, dest, out NavMeshHit _, agent.areaMask))
        {
            corners = new Vector3[] { from, dest };
        }
        else
        {
            // --- 2. Bị chắn -> tìm đường vòng ngắn nhất ---
            if (NavMesh.CalculatePath(from, dest, agent.areaMask, navPath) && navPath.corners.Length >= 2)
            {
                corners = navPath.corners;
                if (navPath.status != NavMeshPathStatus.PathComplete)
                    pathBlocked = true; // Chỉ có đường tới một phần
            }
            else
            {
                pathBlocked = true;
                debugCorners = null;
                agent.SetDestination(from);
                return;
            }
        }

        // --- 3. Kiểm tra độ dốc dọc đường đi ---
        Vector3 finalDest = corners[corners.Length - 1];
        for (int i = 0; i < corners.Length - 1; i++)
        {
            if (!IsSegmentWalkable(corners[i], corners[i + 1], out Vector3 lastSafe))
            {
                finalDest = lastSafe;
                pathBlocked = true;
                // Cắt đường tại điểm an toàn cuối cùng (phục vụ vẽ Gizmo)
                System.Array.Resize(ref corners, i + 2);
                corners[i + 1] = lastSafe;
                break;
            }
        }

        debugCorners = corners;
        agent.SetDestination(finalDest);
    }

    // =====================================================================
    //  SLOPE / TERRAIN HELPERS
    // =====================================================================
    /// <summary>Kiểm tra từng đoạn a->b có đoạn nào dốc hơn maxSlopeAngle không.</summary>
    private bool IsSegmentWalkable(Vector3 a, Vector3 b, out Vector3 lastSafe)
    {
        float dist = Vector3.Distance(a, b);
        int steps = Mathf.Max(1, Mathf.CeilToInt(dist / Mathf.Max(0.25f, slopeSampleStep)));
        Vector3 prev = a;

        for (int i = 1; i <= steps; i++)
        {
            Vector3 p = Vector3.Lerp(a, b, i / (float)steps);
            if (GetSlopeAngle(p) > maxSlopeAngle)
            {
                lastSafe = prev;
                return false;
            }
            prev = p;
        }

        lastSafe = b;
        return true;
    }

    /// <summary>Lấy góc dốc (độ) tại vị trí p. Ưu tiên Terrain, nếu không có thì Raycast xuống.</summary>
    private float GetSlopeAngle(Vector3 p)
    {
        if (TryGetGroundNormal(p, out Vector3 normal, out _))
            return Vector3.Angle(normal, Vector3.up);
        return 0f;
    }

    private bool TryGetGroundNormal(Vector3 p, out Vector3 normal, out float groundY)
    {
        // Ưu tiên Terrain (không bị dính collider của chính quái)
        Terrain terrain = GetTerrainAt(p);
        if (terrain != null)
        {
            TerrainData td = terrain.terrainData;
            Vector3 local = p - terrain.transform.position;
            float nx = Mathf.Clamp01(local.x / td.size.x);
            float nz = Mathf.Clamp01(local.z / td.size.z);
            normal = td.GetInterpolatedNormal(nx, nz);
            groundY = terrain.SampleHeight(p) + terrain.transform.position.y;
            return true;
        }

        // Không có Terrain -> Raycast xuống mặt đất (bỏ qua collider của quái, Player và quái khác)
        RaycastHit[] hits = Physics.RaycastAll(p + Vector3.up * 2f, Vector3.down, 6f, groundMask, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        bool found = false;
        normal = Vector3.up;
        groundY = p.y;

        foreach (RaycastHit h in hits)
        {
            if (h.transform == transform || h.transform.IsChildOf(transform)) continue;
            if (playerTarget != null && (h.transform == playerTarget || h.transform.IsChildOf(playerTarget))) continue;
            if (h.collider.GetComponentInParent<NavMeshEnemyAI>() != null) continue;
            if (h.distance < best)
            {
                best = h.distance;
                normal = h.normal;
                groundY = h.point.y;
                found = true;
            }
        }
        return found;
    }

    private Terrain GetTerrainAt(Vector3 p)
    {
        Terrain t = Terrain.activeTerrain;
        if (t == null) return null;

        Vector3 local = p - t.transform.position;
        Vector3 size = t.terrainData.size;
        if (local.x < 0 || local.z < 0 || local.x > size.x || local.z > size.z) return null;
        return t;
    }

    /// <summary>
    /// Di chuyển dự phòng khi không có NavMesh: bám mặt đất, KHÔNG đi lên dốc > maxSlopeAngle.
    /// Trả về true nếu di chuyển được.
    /// </summary>
    private bool ManualMoveOnTerrain(Vector3 targetPos, float speed)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude < 0.0001f) return false;
        dir.Normalize();

        Vector3 next = transform.position + dir * speed * Time.deltaTime;

        if (TryGetGroundNormal(next, out Vector3 normal, out float groundY))
        {
            if (Vector3.Angle(normal, Vector3.up) > maxSlopeAngle)
            {
                // Dốc quá cao -> chặn, chỉ xoay mặt
                FaceTarget(targetPos, 10f);
                return false;
            }
            next.y = groundY + groundOffset;
        }

        FaceTarget(targetPos, 10f);
        transform.position = next;
        return true;
    }

    private void FaceTarget(Vector3 worldPos, float turnSpeed)
    {
        Vector3 d = worldPos - transform.position;
        d.y = 0;
        if (d != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), Time.deltaTime * turnSpeed);
        }
    }

    // =====================================================================
    //  ATTACK
    // =====================================================================
    private void PerformAttack(bool inRange)
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        if (playerTarget != null)
        {
            FaceTarget(playerTarget.position, 12f);
        }

        // Chỉ ra đòn mới khi: còn trong tầm, hết cooldown, animation đánh trước đã xong, và đang quay mặt về Player
        if (!inRange) return;
        if (Time.time < nextAttackTime) return;
        if (IsPlayingAttackState()) return;
        if (!IsFacingPlayer()) return;

        PlayAttackAnimation();

        // Mỗi lần tấn công -> chạy FX (trừ khi FX do Animation Event đảm nhận)
        if (!useAnimationEvent) SpawnFx();

        nextAttackTime = Time.time + attackCooldown;
    }

    private bool IsFacingPlayer()
    {
        if (playerTarget == null) return false;
        Vector3 d = playerTarget.position - transform.position;
        d.y = 0f;
        if (d.sqrMagnitude < 0.0001f) return true;
        return Vector3.Angle(transform.forward, d) <= attackAngle;
    }

    /// <summary>Điểm xuất phát tính tầm đánh: AttackPos (mỏ/móng) hoặc ngang ngực quái.</summary>
    private Vector3 GetAttackOrigin()
    {
        return attackPoint != null
            ? attackPoint.position
            : transform.position + Vector3.up * hitOriginHeight;
    }

    /// <summary>Khoảng cách từ điểm 'from' tới bề mặt collider thân của Player (0 = đã chạm / nằm trong).</summary>
    private float DistanceToPlayerSurface(Vector3 from)
    {
        if (playerTarget == null) return float.MaxValue;

        if (playerBody == null || !playerBody.transform.IsChildOf(playerTarget))
            playerBody = playerTarget.GetComponent<Collider>();

        if (playerBody == null)
            return Mathf.Max(0f, Vector3.Distance(from, playerTarget.position));

        CharacterController cc = playerBody as CharacterController;
        if (cc != null)
        {
            // Dựng capsule của CharacterController (có tính scale) rồi tìm điểm gần nhất trên trục
            Vector3 sc = cc.transform.lossyScale;
            float radius = cc.radius * Mathf.Max(Mathf.Abs(sc.x), Mathf.Abs(sc.z));
            float height = Mathf.Max(cc.height * Mathf.Abs(sc.y), radius * 2f);
            Vector3 center = cc.transform.TransformPoint(cc.center);
            float half = height * 0.5f - radius;
            Vector3 a = center + Vector3.up * half;
            Vector3 b = center - Vector3.up * half;
            Vector3 ab = a - b;
            float t = ab.sqrMagnitude > 0.0001f ? Mathf.Clamp01(Vector3.Dot(from - b, ab) / ab.sqrMagnitude) : 0f;
            Vector3 closest = b + ab * t;
            return Mathf.Max(0f, Vector3.Distance(from, closest) - radius);
        }

        // Box / Sphere / Capsule / Mesh convex: dùng ClosestPoint của Unity
        return Vector3.Distance(from, playerBody.ClosestPoint(from));
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName) return child;
            Transform found = FindDeepChild(child, childName);
            if (found != null) return found;
        }
        return null;
    }

    /// <summary>
    /// Sinh FX tấn công. Được gọi tự động mỗi lần đánh,
    /// hoặc từ Animation Event tên "SpawnFx" nếu bật useAnimationEvent.
    /// </summary>
    public void SpawnFx()
    {
        if (IsDead || fxPrefab == null) return;

        // Chống bắn trùng (event có thể được gọi 2 lần trong lúc crossfade giữa các clip)
        if (Time.time - lastFxTime < 0.1f) return;
        lastFxTime = Time.time;

        Vector3 pos;
        Quaternion rot;
        GetFxSpawnPose(out pos, out rot);

        GameObject fx = Instantiate(fxPrefab, pos, rot);
        Destroy(fx, fxLifeTime);
    }

    /// <summary>
    /// Tính vị trí + hướng sinh FX. Nếu bật spawnFxAtHitPoint thì lấy điểm trên
    /// collider của Player gần "nanh/móng" quái nhất (nơi thực sự gây sát thương).
    /// </summary>
    private void GetFxSpawnPose(out Vector3 pos, out Quaternion rot)
    {
        // Điểm xuất phát dò: fxPoint (miệng/móng) nếu có, không thì ngang ngực quái
        Vector3 origin = fxPoint != null
            ? fxPoint.position
            : transform.position + Vector3.up * hitOriginHeight;

        pos = origin;
        rot = fxPoint != null ? fxPoint.rotation : transform.rotation;

        if (!spawnFxAtHitPoint || playerTarget == null) return;

        // Ưu tiên collider nằm trên object gốc của Player (thân), tránh lấy nhầm collider của vũ khí
        Collider col = playerTarget.GetComponent<Collider>();
        if (col == null) col = playerTarget.GetComponentInChildren<Collider>();
        Vector3 hitPoint;

        if (col != null)
        {
            // bounds.ClosestPoint hoạt động với mọi loại collider (kể cả CharacterController)
            hitPoint = col.bounds.ClosestPoint(origin);

            // Origin nằm trong bounds (quá sát) -> dùng tâm Player
            if ((hitPoint - origin).sqrMagnitude < 0.0001f)
                hitPoint = col.bounds.center;
        }
        else
        {
            hitPoint = playerTarget.position + Vector3.up * hitOriginHeight;
        }

        // Pháp tuyến giả: hướng từ Player về phía quái
        Vector3 toAttacker = origin - hitPoint;
        if (toAttacker.sqrMagnitude > 0.0001f)
        {
            toAttacker.Normalize();
            pos = hitPoint + toAttacker * hitPointPullBack; // đẩy ra khỏi bề mặt một chút
            rot = Quaternion.LookRotation(toAttacker);      // FX hướng về phía quái (như máu bắn ra ngoài)
        }
        else
        {
            pos = hitPoint;
        }
    }

    private void PlayAttackAnimation()
    {
        if (animator == null) return;

        for (int i = 1; i < animator.layerCount; i++)
        {
            animator.SetLayerWeight(i, 1.0f);
        }

        if (HasParameter(SpeedHash)) animator.SetFloat(SpeedHash, 0f);

        int hash; int layer;
        if (CrossFadeToFirstExistingState(AttackStateNames, 0.1f, out hash, out layer))
        {
            // Đã vào thẳng state tấn công -> xóa trigger còn tồn đọng để không bắn thêm đòn thứ 2
            attackStateHash = hash;
            attackLayer = layer;
            if (HasParameter(AttackTriggerHash)) animator.ResetTrigger(AttackTriggerHash);
            if (HasParameter(MeleeTriggerHash)) animator.ResetTrigger(MeleeTriggerHash);
            if (HasParameter(BiteTriggerHash)) animator.ResetTrigger(BiteTriggerHash);
            return;
        }

        // Không tìm thấy state theo tên -> dùng trigger như cũ
        if (HasParameter(AttackTriggerHash)) animator.SetTrigger(AttackTriggerHash);
        if (HasParameter(MeleeTriggerHash)) animator.SetTrigger(MeleeTriggerHash);
        if (HasParameter(BiteTriggerHash)) animator.SetTrigger(BiteTriggerHash);
    }

    /// <summary>Animator đang chạy (hoặc đang chuyển vào) state tấn công?</summary>
    private bool IsPlayingAttackState()
    {
        if (animator == null || attackStateHash == 0) return false;
        if (animator.GetCurrentAnimatorStateInfo(attackLayer).shortNameHash == attackStateHash) return true;
        return animator.IsInTransition(attackLayer)
            && animator.GetNextAnimatorStateInfo(attackLayer).shortNameHash == attackStateHash;
    }

    /// <summary>CrossFade tới state đầu tiên tồn tại trong danh sách (duyệt từng layer). Trả về true nếu thành công.</summary>
    private bool CrossFadeToFirstExistingState(string[] stateNames, float fadeTime, out int stateHash, out int layerIndex)
    {
        for (int layer = 0; layer < animator.layerCount; layer++)
        {
            foreach (string stName in stateNames)
            {
                if (string.IsNullOrEmpty(stName)) continue;
                int hash = Animator.StringToHash(stName);
                if (animator.HasState(layer, hash))
                {
                    animator.CrossFadeInFixedTime(stName, fadeTime, layer);
                    stateHash = hash;
                    layerIndex = layer;
                    return true;
                }
            }
        }
        stateHash = 0;
        layerIndex = 0;
        return false;
    }

    // =====================================================================
    //  DEATH
    // =====================================================================
    /// <summary>
    /// Gọi hàm này khi quái hết máu: dừng AI, tắt collider + NavMeshAgent, chạy animation chết.
    /// </summary>
    public void Die()
    {
        if (IsDead) return;
        IsDead = true;
        currentState = AIState.Dead;
        attackStateHash = 0;
        touching.Clear();

        if (agent != null && agent.enabled)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
            agent.enabled = false;
        }

        // Tắt toàn bộ collider để xác không còn chặn / kích hoạt trigger
        foreach (Collider c in GetComponentsInChildren<Collider>())
        {
            c.enabled = false;
        }

        PlayDeathAnimation();

        if (corpseLifetime > 0f) Destroy(gameObject, corpseLifetime);
    }

    private void PlayDeathAnimation()
    {
        if (animator == null) return;

        if (HasParameter(SpeedHash)) animator.SetFloat(SpeedHash, 0f);

        if (dieTriggerHash != 0 && HasParameter(dieTriggerHash))
        {
            animator.SetTrigger(dieTriggerHash);
            return;
        }

        // Không có trigger -> CrossFade thẳng tới state chết
        int h; int l;
        CrossFadeToFirstExistingState(deathStateNames, 0.1f, out h, out l);
    }

    // =====================================================================
    //  ANIMATION (Blend Tree điều khiển bằng tham số "Speed")
    // =====================================================================
    /// <summary>
    /// Gửi tốc độ thực tế của quái vào tham số Speed (có làm mượt).
    /// Idle = 0, Walk ≈ patrolSpeed, Run ≈ chaseSpeed -> Blend Tree tự trộn Idle/Walk/Run.
    /// Khi đang Attack / Blocked thì Speed về 0.
    /// </summary>
    private void UpdateLocomotionAnimation()
    {
        if (animator == null || !HasParameter(SpeedHash)) return;

        float target = 0f;
        if (currentState == AIState.Patrol || currentState == AIState.Chase)
        {
            target = (agent != null && agent.enabled && agent.isOnNavMesh)
                ? agent.velocity.magnitude
                : manualSpeed;
        }

        animator.SetFloat(SpeedHash, target, speedDampTime, Time.deltaTime);
    }

    private bool HasParameter(int paramHash)
    {
        return animParams.Contains(paramHash);
    }

    // =====================================================================
    //  PLAYER FINDING & TRIGGERS
    // =====================================================================
    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj == null)
        {
            playerObj = GameObject.Find("Player") ?? GameObject.Find("PlayerCube") ?? GameObject.Find("Cube");
        }

        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
            if (!playerObj.CompareTag(playerTag))
            {
                try { playerObj.tag = playerTag; } catch { }
            }
        }
    }

    private void EnsureTriggerCollider()
    {
        Collider[] colliders = GetComponents<Collider>();
        foreach (Collider col in colliders)
        {
            if (col.isTrigger)
            {
                if (col is SphereCollider sc)
                    sc.radius = triggerRadius;
                else
                    Debug.LogWarning("[NavMeshEnemyAI] Trigger collider trên " + name + " không phải SphereCollider nên triggerRadius không được áp dụng.", this);
                return;
            }
        }

        SphereCollider triggerCol = gameObject.AddComponent<SphereCollider>();
        triggerCol.isTrigger = true;
        triggerCol.radius = triggerRadius;
        triggerCol.center = new Vector3(0, 1.0f, 0);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayerCollider(other)) touching.Add(other);
    }

    private void OnTriggerExit(Collider other)
    {
        touching.Remove(other);
    }

    private bool IsPlayerCollider(Collider col)
    {
        if (col == null) return false;

        if (playerTarget != null)
        {
            // Chỉ nhận collider trên object gốc của Player (thân), bỏ qua vũ khí / object con
            if (bodyColliderOnly) return col.gameObject == playerTarget.gameObject;
            return col.transform == playerTarget || col.transform.IsChildOf(playerTarget);
        }

        return col.CompareTag(playerTag);
    }

    // =====================================================================
    //  GIZMOS
    // =====================================================================
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, patrolDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseDistance);

        // Tầm đánh: hình cầu quanh điểm tấn công (mỏ/móng) - Player chạm hình cầu này là bị đánh
        Gizmos.color = Color.magenta;
        Vector3 ao = attackPoint != null ? attackPoint.position : transform.position + Vector3.up * hitOriginHeight;
        Gizmos.DrawWireSphere(ao, attackReach);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Application.isPlaying ? spawnPosition : transform.position, patrolRadius);

        // Đường đi đang truy đuổi (Xanh lơ = thông, Cam = bị chặn)
        if (debugCorners != null && debugCorners.Length >= 2)
        {
            Gizmos.color = pathBlocked ? new Color(1f, 0.5f, 0f) : Color.cyan;
            for (int i = 0; i < debugCorners.Length - 1; i++)
            {
                Gizmos.DrawLine(debugCorners[i] + Vector3.up * 0.2f, debugCorners[i + 1] + Vector3.up * 0.2f);
                Gizmos.DrawSphere(debugCorners[i + 1] + Vector3.up * 0.2f, 0.2f);
            }
        }
    }
}