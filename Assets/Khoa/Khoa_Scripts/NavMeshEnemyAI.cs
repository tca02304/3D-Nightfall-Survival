using UnityEngine;
using UnityEngine.AI;

public class NavMeshEnemyAI : MonoBehaviour
{
    public enum AIState
    {
        Patrol, // 1. Tuần tra (Vòng tròn nhỏ khi cách player >= 50m)
        Chase,  // 2. Truy đuổi (Chạy về phía player khi cách <= 49m)
        Attack  // 3. Tấn công (Chạy animation attack khi chạm trigger collider / áp sát player)
    }

    [Header("Current State")]
    public AIState currentState = AIState.Patrol;

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

    [Header("Attack Settings")]
    public float attackCooldown = 1.0f;  // Thời gian giãn cách giữa các đòn đánh
    private float nextAttackTime = 0f;

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
                animator.SetLayerWeight(i, 1.0f); // Bật trọng số Layer trên cùng (Upper/Attack) lên 1.0
            }
        }

        // 2. Thêm Rigidbody Kinematic (BẮT BUỘC để Unity Physics kích hoạt sự kiện OnTriggerEnter/Stay)
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
            agent.stoppingDistance = 1.2f; // Dừng lại ở khoảng cách tấn công
            agent.acceleration = 16.0f;
            agent.angularSpeed = 360.0f;
            agent.speed = chaseSpeed;
        }

        // 4. Tự động thêm Trigger SphereCollider phát hiện va chạm với Player
        EnsureTriggerCollider();

        // 5. Gán thanh máu trên đầu cho mob (loại trừ player)
        if (!CompareTag(playerTag) && !gameObject.name.ToLower().Contains("player"))
        {
            MobHealthBar healthBar = GetComponent<MobHealthBar>();
            if (healthBar == null)
            {
                gameObject.AddComponent<MobHealthBar>();
            }
        }
    }

    private void Start()
    {
        spawnPosition = transform.position;
        FindPlayer();
    }

    private void Update()
    {
        if (playerTarget == null)
        {
            FindPlayer();
        }

        float distanceToPlayer = (playerTarget != null) 
            ? Vector3.Distance(transform.position, playerTarget.position) 
            : float.MaxValue;

        // Trạng thái 3: Tấn công khi Trigger Collider chạm Player HOẶC khoảng cách áp sát <= attackDistance
        bool inAttackRange = isPlayerInTrigger || (distanceToPlayer <= attackDistance);

        if (inAttackRange && playerTarget != null)
        {
            currentState = AIState.Attack;
            PerformAttack();
        }
        // Trạng thái 2: Player trong phạm vi <= 49m -> Truy đuổi
        else if (distanceToPlayer <= chaseDistance && playerTarget != null)
        {
            currentState = AIState.Chase;
            PerformChase();
        }
        // Trạng thái 1: Player xa >= 50m -> Tuần tra xoay tròn
        else
        {
            currentState = AIState.Patrol;
            PerformPatrol();
        }
    }

    private void PerformPatrol()
    {
        patrolAngle += angularSpeed * Time.deltaTime;
        if (patrolAngle >= Mathf.PI * 2f) patrolAngle -= Mathf.PI * 2f;

        Vector3 targetOffset = new Vector3(Mathf.Cos(patrolAngle) * patrolRadius, 0, Mathf.Sin(patrolAngle) * patrolRadius);
        Vector3 targetPos = spawnPosition + targetOffset;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = patrolSpeed;
            agent.SetDestination(targetPos);
        }
        else
        {
            Vector3 moveDir = (targetPos - transform.position).normalized;
            moveDir.y = 0;
            if (moveDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 10f);
                transform.position += moveDir * patrolSpeed * Time.deltaTime;
            }
        }

        UpdateLocomotionAnimation(patrolSpeed);
    }

    private void PerformChase()
    {
        if (playerTarget == null) return;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = chaseSpeed;
            agent.SetDestination(playerTarget.position);
        }
        else
        {
            Vector3 moveDir = (playerTarget.position - transform.position).normalized;
            moveDir.y = 0;
            if (moveDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 10f);
                transform.position += moveDir * chaseSpeed * Time.deltaTime;
            }
        }

        UpdateLocomotionAnimation(chaseSpeed);
    }

    private void PerformAttack()
    {
        // 1. Dừng di chuyển
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        // 2. Quay mặt về phía Player
        if (playerTarget != null)
        {
            Vector3 direction = (playerTarget.position - transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 12f);
            }
        }

        // 3. Kích hoạt animation attack khi hết Cooldown
        if (Time.time >= nextAttackTime)
        {
            PlayAttackAnimation();
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    private void PlayAttackAnimation()
    {
        if (animator == null) return;

        // Đảm bảo bật Layer Weight cho các layer trên cùng (Upper/Attack)
        for (int i = 1; i < animator.layerCount; i++)
        {
            animator.SetLayerWeight(i, 1.0f);
        }

        if (HasParameter(IsRunningHash)) animator.SetBool(IsRunningHash, false);
        if (HasParameter(SpeedHash)) animator.SetFloat(SpeedHash, 0f);

        // Kích hoạt triggers
        if (HasParameter(AttackTriggerHash)) animator.SetTrigger(AttackTriggerHash);
        if (HasParameter(MeleeTriggerHash)) animator.SetTrigger(MeleeTriggerHash);
        if (HasParameter(BiteTriggerHash)) animator.SetTrigger(BiteTriggerHash);

        // Danh sách các state tấn công trong AnimatorController của Goblin, Wolf, LilDave
        string[] attackStateNames = new string[] {
            "GoblinMelee",   // Goblin
            "WolfAttack",    // Wolf
            "AttackMelee",   // LilDave
            "Attack",        // Chung
            "AttackRange",   // LilDave ranged
            "Bite"           // Wolf
        };

        bool played = false;
        // Quét trên tất cả các layer để phát animation
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

        // Nếu chưa phát được, thử Play trực tiếp state name đầu tiên tìm thấy
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

    private void UpdateLocomotionAnimation(float currentSpeed)
    {
        if (animator != null)
        {
            if (HasParameter(IsRunningHash)) animator.SetBool(IsRunningHash, true);
            if (HasParameter(SpeedHash)) animator.SetFloat(SpeedHash, currentSpeed);
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
                try { playerObj.tag = playerTag; } catch {}
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

    // --- XỬ LÝ VA CHẠM TRIGGER VÀ COLLISION VỚI PLAYER ---
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

    private void OnDrawGizmosSelected()
    {
        // 1. Phạm vi Tuần tra (Vàng) >= 50m
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, patrolDistance);

        // 2. Phạm vi Truy đuổi (Đỏ) <= 49m
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseDistance);

        // 3. Phạm vi Tấn công (Tím) <= 2.2m
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackDistance);

        // 4. Đường tròn tuần tra (Xanh lá)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Application.isPlaying ? spawnPosition : transform.position, patrolRadius);
    }
}
