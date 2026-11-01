using UnityEngine;
using UnityEngine.AI;

public class GoblinAI : MonoBehaviour
{
    [Header("Detection & Distance Settings")]
    public float patrolDistance = 50.0f; // Tuần tra khi >= 50m
    public float chaseDistance = 49.0f;  // Truy đuổi khi <= 49m
    public float attackDistance = 2.2f;  // Khoảng cách tấn công áp sát (m)

    [Header("Patrol Settings (Circle)")]
    public float patrolRadius = 5.0f;     // Bán kính đường tròn tuần tra
    public float patrolSpeed = 2.5f;      // Tốc độ di chuyển khi tuần tra
    public float angularSpeed = 1.2f;     // Tốc độ góc xoay quanh tâm đường tròn

    [Header("Chase Settings")]
    public float chaseSpeed = 5.0f;       // Tốc độ chạy truy đuổi

    [Header("Attack Settings")]
    public float attackCooldown = 1.0f;
    private float nextAttackTime = 0f;

    [Header("References & Target")]
    public Transform playerTarget;
    public string playerTag = "Player";

    private NavMeshAgent agent;
    private Animator animator;
    private Vector3 spawnPosition;
    private float patrolAngle = 0f;
    private bool isPlayerInTrigger = false;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
    private static readonly int MeleeTriggerHash = Animator.StringToHash("Melee");

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.applyRootMotion = false;
            for (int i = 1; i < animator.layerCount; i++)
            {
                animator.SetLayerWeight(i, 1.0f);
            }
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        agent = GetComponent<NavMeshAgent>();
        if (agent == null) agent = gameObject.AddComponent<NavMeshAgent>();

        if (agent != null)
        {
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.stoppingDistance = 1.2f;
            agent.acceleration = 16.0f;
            agent.angularSpeed = 360.0f;
        }

        EnsureTriggerCollider();

        if (!CompareTag(playerTag) && !gameObject.name.ToLower().Contains("player"))
        {
            if (GetComponent<MobHealthBar>() == null) gameObject.AddComponent<MobHealthBar>();
        }
    }

    private void Start()
    {
        spawnPosition = transform.position;
        FindPlayer();
    }

    private void Update()
    {
        if (playerTarget == null) FindPlayer();

        float distanceToPlayer = (playerTarget != null) 
            ? Vector3.Distance(transform.position, playerTarget.position) 
            : float.MaxValue;

        bool inAttackRange = isPlayerInTrigger || (distanceToPlayer <= attackDistance);

        // 3. Trạng thái Tấn công
        if (inAttackRange && playerTarget != null)
        {
            AttackPlayer();
        }
        // 2. Trạng thái Truy đuổi
        else if (distanceToPlayer <= chaseDistance && playerTarget != null)
        {
            ChasePlayer();
        }
        // 1. Trạng thái Tuần tra
        else
        {
            PatrolInCircle();
        }
    }

    private void PatrolInCircle()
    {
        patrolAngle += angularSpeed * Time.deltaTime;
        if (patrolAngle >= Mathf.PI * 2f) patrolAngle -= Mathf.PI * 2f;

        Vector3 targetPos = spawnPosition + new Vector3(Mathf.Cos(patrolAngle) * patrolRadius, 0, Mathf.Sin(patrolAngle) * patrolRadius);

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

        if (animator != null)
        {
            if (HasParameter(IsRunningHash)) animator.SetBool(IsRunningHash, true);
            if (HasParameter(SpeedHash)) animator.SetFloat(SpeedHash, patrolSpeed);
        }
    }

    private void ChasePlayer()
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

        if (animator != null)
        {
            if (HasParameter(IsRunningHash)) animator.SetBool(IsRunningHash, true);
            if (HasParameter(SpeedHash)) animator.SetFloat(SpeedHash, chaseSpeed);
        }
    }

    private void AttackPlayer()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        if (playerTarget != null)
        {
            Vector3 direction = (playerTarget.position - transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 12f);
            }
        }

        if (Time.time >= nextAttackTime)
        {
            if (animator != null)
            {
                for (int i = 1; i < animator.layerCount; i++) animator.SetLayerWeight(i, 1.0f);
                if (HasParameter(IsRunningHash)) animator.SetBool(IsRunningHash, false);
                if (HasParameter(SpeedHash)) animator.SetFloat(SpeedHash, 0f);

                if (HasParameter(MeleeTriggerHash)) animator.SetTrigger(MeleeTriggerHash);
                if (HasParameter(AttackTriggerHash)) animator.SetTrigger(AttackTriggerHash);

                for (int layer = 0; layer < animator.layerCount; layer++)
                {
                    if (animator.HasState(layer, Animator.StringToHash("GoblinMelee")))
                    {
                        animator.CrossFadeInFixedTime("GoblinMelee", 0.1f, layer);
                        break;
                    }
                }
            }
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    private bool HasParameter(int hash)
    {
        if (animator == null) return false;
        foreach (var p in animator.parameters)
        {
            if (p.nameHash == hash) return true;
        }
        return false;
    }

    private void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag(playerTag) ?? GameObject.Find("Player") ?? GameObject.Find("PlayerCube");
        if (p != null) playerTarget = p.transform;
    }

    private void EnsureTriggerCollider()
    {
        foreach (var c in GetComponents<Collider>())
        {
            if (c.isTrigger) return;
        }
        SphereCollider sc = gameObject.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 2.5f;
        sc.center = new Vector3(0, 1.0f, 0);
    }

    private void OnTriggerEnter(Collider other) { if (IsPlayer(other)) isPlayerInTrigger = true; }
    private void OnTriggerStay(Collider other) { if (IsPlayer(other)) isPlayerInTrigger = true; }
    private void OnTriggerExit(Collider other) { if (IsPlayer(other)) isPlayerInTrigger = false; }
    private void OnCollisionEnter(Collision col) { if (IsPlayer(col.collider)) isPlayerInTrigger = true; }
    private void OnCollisionStay(Collision col) { if (IsPlayer(col.collider)) isPlayerInTrigger = true; }
    private void OnCollisionExit(Collision col) { if (IsPlayer(col.collider)) isPlayerInTrigger = false; }

    private bool IsPlayer(Collider other)
    {
        if (other == null) return false;
        if (other.CompareTag(playerTag)) return true;
        if (playerTarget != null && (other.transform == playerTarget || other.transform.IsChildOf(playerTarget))) return true;
        if (other.name.ToLower().Contains("player")) return true;
        return false;
    }

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
    }
}