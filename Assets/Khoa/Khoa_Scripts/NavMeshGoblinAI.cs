using UnityEngine;
using UnityEngine.AI;

public class NavMeshGoblinAI : MonoBehaviour
{
    public enum AIState
    {
        Spawn,   // 0. Chui từ dưới đất lên (chạy trước tiên, chưa di chuyển/tấn công được)
        Patrol,  // 1. Tuần tra (Vòng tròn nhỏ khi cách player >= 50m)
        Chase,   // 2. Truy đuổi (Chạy về phía player khi cách <= 49m)
        Attack,  // 3. Tấn công (Chạy animation attack khi chạm trigger collider / áp sát player)
        Blocked  // 4. Bị chặn (Dốc quá cao / không có đường tới player -> đứng ở điểm gần nhất)
    }

    [Header("Current State")]
    public AIState currentState = AIState.Spawn;

    [Header("Detection & Distance Settings")]
    public float patrolDistance = 50.0f; // Vùng tuần tra (>= 50m)
    public float chaseDistance = 49.0f;  // Vùng truy đuổi (<= 49m)
    public float attackDistance = 2.2f;  // Khoảng cách tấn công áp sát (m)

    [Header("Patrol Circle Settings")]
    public float patrolRadius = 5.0f;    // Bán kính đường tròn tuần tra
    public float patrolSpeed = 2.5f;     // Tốc độ tuần tra
    public float angularSpeed = 1.2f;    // Tốc độ quay quanh tâm tuần tra

    [Header("Chase Settings")]
    public float chaseSpeed = 5.0f;      // Tốc độ chạy truy đuổi

    [Header("Terrain & Slope Settings")]
    [Tooltip("Góc dốc tối đa quái có thể leo (độ). Dốc hơn mức này sẽ bị block.")]
    [Range(1f, 89f)] public float maxSlopeAngle = 35f;
    [Tooltip("Layer của Terrain / mặt đất (dùng cho Raycast kiểm tra độ dốc).")]
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

    [Header("Spawn Settings (chui từ dưới đất lên)")]
    [Tooltip("Bật = chạy animation spawn ngay khi xuất hiện, xong mới bắt đầu Patrol/Chase/Attack")]
    public bool playSpawnOnStart = true;

    [Tooltip("Tên state animation spawn trong Animator (chui từ đất lên)")]
    public string spawnStateName = "Spawn";

    [Tooltip("Tên Trigger spawn trong Animator (chỉ dùng khi không tìm thấy state theo tên)")]
    public string spawnTriggerName = "Spawn";

    [Tooltip("Thời gian tối đa chờ animation spawn (giây). Cũng là thời gian chờ dự phòng nếu không tìm thấy state")]
    public float spawnMaxDuration = 4f;

    /// <summary>Bắn ra khi animation spawn kết thúc và goblin bắt đầu hoạt động.</summary>
    public event System.Action OnSpawnFinished;

    /// <summary>Script máu/damage nên kiểm tra thuộc tính này để bỏ qua sát thương khi đang chui lên (nếu muốn).</summary>
    public bool IsSpawning => currentState == AIState.Spawn;

    [Header("Target & Tag")]
    public Transform playerTarget;
    public string playerTag = "Player";

    [Header("Trigger Collider Settings")]
    public float triggerRadius = 2.5f;   // Bán kính Trigger Collider tấn công

    private NavMeshAgent agent;
    private Animator animator;
    private Vector3 spawnPosition;
    private float patrolAngle = 0f;
    private bool isPlayerInTrigger = false;

    // --- Path finding ---
    private NavMeshPath navPath;
    private float nextRepathTime = 0f;
    private bool pathBlocked = false;
    private Vector3[] debugCorners;

    // Animator Hash Parameters
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
    private static readonly int MeleeTriggerHash = Animator.StringToHash("Melee");
    private static readonly int BiteTriggerHash = Animator.StringToHash("Bite");

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
        }

        // 2. Rigidbody Kinematic (BẮT BUỘC để Unity Physics kích hoạt OnTriggerEnter/Stay)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;

        // 3. Cấu hình NavMeshAgent
        // Tắt agent ngay trong Awake (trước OnEnable) để không báo lỗi
        // "Failed to create agent because there is no valid NavMesh".
        // Agent sẽ được bật lại trong TryActivateAgent() khi xác nhận có NavMesh.
        agent = GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        navPath = new NavMeshPath();

        // 4. Tự động thêm Trigger SphereCollider phát hiện va chạm với Player
        EnsureTriggerCollider();

        // 5. Thanh máu sẽ được gắn sau khi chui lên xong (xem FinishSpawn)
    }

    private void Start()
    {
        spawnPosition = transform.position;
        FindPlayer();
        TryActivateAgent();

        if (playSpawnOnStart) StartSpawn();
        else FinishSpawn();
    }

    /// <summary>
    /// Chỉ bật NavMeshAgent khi có NavMesh gần vị trí quái. Nếu không có,
    /// quái dùng chế độ di chuyển thủ công bám Terrain (vẫn chặn dốc).
    /// </summary>
    private void TryActivateAgent()
    {
        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            Debug.LogWarning($"[{name}] Không có NavMesh gần vị trí spawn -> dùng di chuyển thủ công. " +
                             "Hãy Bake NavMesh (hoặc thêm NavMesh Surface) cho Terrain và đặt quái trên vùng xanh.", this);
            return;
        }

        // Đặt quái sát mặt NavMesh rồi mới tạo/bật agent
        transform.position = hit.position;
        spawnPosition = hit.position;

        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
        }

        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.stoppingDistance = 1.2f;
        agent.acceleration = 16.0f;
        agent.angularSpeed = 360.0f;
        agent.speed = chaseSpeed;
        agent.autoRepath = true;
        agent.areaMask = NavMesh.AllAreas;

        agent.enabled = true;
        agent.Warp(hit.position);
    }

    // =====================================================================
    //  SPAWN (chui từ dưới đất lên) - chạy trước mọi state khác
    // =====================================================================
    /// <summary>Chạy animation spawn. Có thể gọi lại từ Spawner / object pool khi tái sử dụng goblin.</summary>
    public void StartSpawn()
    {
        StopAllCoroutines();
        StartCoroutine(SpawnRoutine());
    }

    private System.Collections.IEnumerator SpawnRoutine()
    {
        currentState = AIState.Spawn;
        isPlayerInTrigger = false;

        // Đứng yên, không di chuyển / không tấn công trong lúc chui lên
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
        SetIdleAnimation();

        int stateHash;
        int stateLayer;
        bool found = PlaySpawnAnimation(out stateHash, out stateLayer);

        if (!found || animator == null)
        {
            // Không tìm thấy state -> chờ thời gian dự phòng
            yield return new WaitForSeconds(spawnMaxDuration);
        }
        else
        {
            float elapsed = 0f;
            bool reached = false;

            while (elapsed < spawnMaxDuration)
            {
                elapsed += Time.deltaTime;
                yield return null;

                AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(stateLayer);
                bool inSpawnState = info.shortNameHash == stateHash;

                if (inSpawnState)
                {
                    reached = true;
                    if (info.normalizedTime >= 1f) break;     // chạy xong
                    if (animator.IsInTransition(stateLayer) && info.normalizedTime > 0.5f) break; // đang chuyển sang state khác
                }
                else if (reached)
                {
                    break; // đã rời state spawn
                }
            }
        }

        FinishSpawn();
    }

    private bool PlaySpawnAnimation(out int stateHash, out int stateLayer)
    {
        stateHash = 0;
        stateLayer = 0;
        if (animator == null) return false;

        for (int i = 1; i < animator.layerCount; i++) animator.SetLayerWeight(i, 1.0f);

        string[] candidates = new string[] {
            spawnStateName, "Spawn", "Emerge", "SpawnFromGround", "Rise", "Appear", "GoblinSpawn"
        };

        // Ưu tiên chạy thẳng state theo tên (chắc chắn nhất, không phụ thuộc transition)
        for (int layer = 0; layer < animator.layerCount; layer++)
        {
            foreach (string stName in candidates)
            {
                if (string.IsNullOrEmpty(stName)) continue;
                int hash = Animator.StringToHash(stName);
                if (animator.HasState(layer, hash))
                {
                    animator.CrossFadeInFixedTime(hash, 0.05f, layer);
                    stateHash = hash;
                    stateLayer = layer;

                    // Tránh trigger "Spawn" còn sót làm animation chạy lại lần nữa
                    int trig = Animator.StringToHash(spawnTriggerName);
                    if (!string.IsNullOrEmpty(spawnTriggerName) && HasParameter(trig)) animator.ResetTrigger(trig);
                    return true;
                }
            }
        }

        // Không có state theo tên -> thử dùng Trigger
        if (!string.IsNullOrEmpty(spawnTriggerName))
        {
            int trig = Animator.StringToHash(spawnTriggerName);
            if (HasParameter(trig))
            {
                animator.SetTrigger(trig);
                return false; // không biết state nào -> chờ theo thời gian dự phòng
            }
        }

        Debug.LogWarning($"[{name}] Không tìm thấy state/trigger spawn trong Animator. " +
                         $"Hãy đặt 'Spawn State Name' đúng tên state chui từ đất lên.", this);
        return false;
    }

    private void FinishSpawn()
    {
        EnsureHealthBar();

        nextRepathTime = 0f;
        if (agent != null && agent.isOnNavMesh) agent.isStopped = false;

        currentState = AIState.Patrol;
        OnSpawnFinished?.Invoke();
    }

    private void EnsureHealthBar()
    {
        if (CompareTag(playerTag) || gameObject.name.ToLower().Contains("player")) return;
        if (GetComponent<MobHealthBar>() == null) gameObject.AddComponent<MobHealthBar>();
    }

    private void Update()
    {
        // Đang chui từ đất lên -> chưa làm gì cả
        if (currentState == AIState.Spawn) return;

        if (playerTarget == null)
        {
            FindPlayer();
        }

        float distanceToPlayer = (playerTarget != null)
            ? Vector3.Distance(transform.position, playerTarget.position)
            : float.MaxValue;

        bool inAttackRange = isPlayerInTrigger || (distanceToPlayer <= attackDistance);

        if (inAttackRange && playerTarget != null)
        {
            currentState = AIState.Attack;
            PerformAttack();
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
            ManualMoveOnTerrain(targetPos, patrolSpeed);
        }

        UpdateLocomotionAnimation(patrolSpeed);
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
                    SetIdleAnimation();
                    return;
                }
            }

            currentState = AIState.Chase;
            UpdateLocomotionAnimation(chaseSpeed);
        }
        else
        {
            // Không có NavMesh -> di chuyển thủ công bám Terrain + chặn dốc
            bool moved = ManualMoveOnTerrain(playerTarget.position, chaseSpeed);
            currentState = moved ? AIState.Chase : AIState.Blocked;
            if (moved) UpdateLocomotionAnimation(chaseSpeed);
            else SetIdleAnimation();
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

        // Không có Terrain -> Raycast xuống mặt đất (bỏ qua collider của chính quái)
        RaycastHit[] hits = Physics.RaycastAll(p + Vector3.up * 2f, Vector3.down, 6f, groundMask, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        bool found = false;
        normal = Vector3.up;
        groundY = p.y;

        foreach (RaycastHit h in hits)
        {
            if (h.transform == transform || h.transform.IsChildOf(transform)) continue;
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
    private void PerformAttack()
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

        if (Time.time >= nextAttackTime)
        {
            PlayAttackAnimation();

            // Mỗi lần tấn công -> chạy FX (trừ khi FX do Animation Event đảm nhận)
            if (!useAnimationEvent) SpawnFx();

            nextAttackTime = Time.time + attackCooldown;
        }
    }

    /// <summary>
    /// Sinh FX tấn công. Được gọi tự động mỗi lần đánh,
    /// hoặc từ Animation Event tên "SpawnFx" nếu bật useAnimationEvent.
    /// </summary>
    public void SpawnFx()
    {
        if (fxPrefab == null) return;

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
        // Đang chui từ đất lên: FX nổ tại chân goblin (không phải điểm trúng đòn trên Player)
        if (currentState == AIState.Spawn)
        {
            pos = transform.position;
            rot = transform.rotation;
            return;
        }

        // Điểm xuất phát dò: fxPoint (miệng/móng) nếu có, không thì ngang ngực quái
        Vector3 origin = fxPoint != null
            ? fxPoint.position
            : transform.position + Vector3.up * hitOriginHeight;

        pos = origin;
        rot = fxPoint != null ? fxPoint.rotation : transform.rotation;

        if (!spawnFxAtHitPoint || playerTarget == null) return;

        Collider col = playerTarget.GetComponentInChildren<Collider>();
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

        if (HasParameter(IsRunningHash)) animator.SetBool(IsRunningHash, false);
        if (HasParameter(SpeedHash)) animator.SetFloat(SpeedHash, 0f);

        if (HasParameter(AttackTriggerHash)) animator.SetTrigger(AttackTriggerHash);
        if (HasParameter(MeleeTriggerHash)) animator.SetTrigger(MeleeTriggerHash);
        if (HasParameter(BiteTriggerHash)) animator.SetTrigger(BiteTriggerHash);

        string[] attackStateNames = new string[] {
            "GoblinMelee",   // Goblin
            "WolfAttack",    // Wolf
            "AttackMelee",   // LilDave
            "Attack",        // Chung
            "AttackRange",   // LilDave ranged
            "Bite"           // Wolf
        };

        bool played = false;
        for (int layer = 0; layer < animator.layerCount; layer++)
        {
            foreach (string stName in attackStateNames)
            {
                int hash = Animator.StringToHash(stName);
                if (animator.HasState(layer, hash))
                {
                    animator.CrossFadeInFixedTime(stName, 0.1f, layer);
                    played = true;
                    break;
                }
            }
            if (played) break;
        }

        if (!played)
        {
            foreach (string stName in attackStateNames)
            {
                try
                {
                    animator.Play(stName);
                    break;
                }
                catch { }
            }
        }
    }

    // =====================================================================
    //  ANIMATION
    // =====================================================================
    private void UpdateLocomotionAnimation(float currentSpeed)
    {
        if (animator != null)
        {
            if (HasParameter(IsRunningHash)) animator.SetBool(IsRunningHash, true);
            if (HasParameter(SpeedHash)) animator.SetFloat(SpeedHash, currentSpeed);
        }
    }

    private void SetIdleAnimation()
    {
        if (animator != null)
        {
            if (HasParameter(IsRunningHash)) animator.SetBool(IsRunningHash, false);
            if (HasParameter(SpeedHash)) animator.SetFloat(SpeedHash, 0f);
        }
    }

    private bool HasParameter(int paramHash)
    {
        if (animator == null) return false;
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.nameHash == paramHash) return true;
        }
        return false;
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
                if (col is SphereCollider sc) sc.radius = triggerRadius;
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
        if (IsPlayerCollider(other)) isPlayerInTrigger = true;
    }

    private void OnTriggerStay(Collider other)
    {
        if (IsPlayerCollider(other)) isPlayerInTrigger = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayerCollider(other)) isPlayerInTrigger = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (IsPlayerCollider(collision.collider)) isPlayerInTrigger = true;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (IsPlayerCollider(collision.collider)) isPlayerInTrigger = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        if (IsPlayerCollider(collision.collider)) isPlayerInTrigger = false;
    }

    private bool IsPlayerCollider(Collider col)
    {
        if (col == null) return false;
        if (col.CompareTag(playerTag)) return true;
        if (playerTarget != null && (col.transform == playerTarget || col.transform.IsChildOf(playerTarget) || col.gameObject == playerTarget.gameObject)) return true;
        if (col.name.ToLower().Contains("player")) return true;
        return false;
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

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackDistance);

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
