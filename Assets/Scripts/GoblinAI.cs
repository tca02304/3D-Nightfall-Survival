using UnityEngine;

public class GoblinAI : MonoBehaviour
{
    [Header("Detection & Combat Settings")]
    public float detectionRange = 50.0f; // Tầm nhìn phát hiện Player
    public float attackRange = 2.0f;      // Tầm đánh cận chiến
    public float moveSpeed = 3.5f;        // Tốc độ di chuyển
    public float rotationSpeed = 10.0f;   // Tốc độ xoay mặt

    [Header("Patrol Settings (Circle)")]
    public float patrolRadius = 5.0f;     // Bán kính đường tròn tuần tra
    public float patrolSpeed = 2.0f;      // Tốc độ di chuyển khi tuần tra

    [Header("References")]
    public Transform playerTarget;
    private Animator animator;
    private Vector3 spawnPosition;
    private float patrolAngle = 0f;

    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int MeleeTriggerHash = Animator.StringToHash("Melee");

    private void Start()
    {
        animator = (Animator)GetComponent(typeof(Animator));
        spawnPosition = transform.position; // Lưu lại vị trí xuất phát để làm tâm đường tròn tuần tra
        FindPlayer();
    }

    private void Update()
    {
        if (playerTarget == null)
        {
            FindPlayer();
        }

        float distanceToPlayer = playerTarget != null 
            ? Vector3.Distance(transform.position, playerTarget.position) 
            : float.MaxValue;

        // Trạng thái 1: Player ngoài tầm nhìn (> 50m) -> Tuần tra xoay tròn
        if (distanceToPlayer > detectionRange)
        {
            PatrolInCircle();
        }
        // Trạng thái 2: Player trong tầm nhìn nhưng chưa tới tầm đánh -> Chạy lại gần Player
        else if (distanceToPlayer > attackRange)
        {
            ChasePlayer();
        }
        // Trạng thái 3: Đã áp sát Player (<= 2m) -> Tấn công cận chiến
        else
        {
            AttackPlayer();
        }
    }

    private void PatrolInCircle()
    {
        // Tính vị trí tiếp theo trên đường tròn
        patrolAngle += (patrolSpeed / patrolRadius) * Time.deltaTime;
        if (patrolAngle >= Mathf.PI * 2) patrolAngle -= Mathf.PI * 2;

        Vector3 targetPatrolPos = spawnPosition + new Vector3(Mathf.Cos(patrolAngle) * patrolRadius, 0, Mathf.Sin(patrolAngle) * patrolRadius);
        Vector3 moveDir = (targetPatrolPos - transform.position).normalized;
        moveDir.y = 0;

        if (moveDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            transform.position += moveDir * patrolSpeed * Time.deltaTime;
        }

        if (animator != null)
        {
            animator.SetBool(IsRunningHash, true); // Đang tuần tra di chuyển
        }
    }

    private void ChasePlayer()
    {
        Vector3 direction = (playerTarget.position - transform.position).normalized;
        direction.y = 0; // Giữ thăng bằng mặt đất

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }

        transform.position += direction * moveSpeed * Time.deltaTime;

        if (animator != null)
        {
            animator.SetBool(IsRunningHash, true);
        }
    }

    private void AttackPlayer()
    {
        if (animator != null)
        {
            animator.SetBool(IsRunningHash, false); // Dừng di chuyển để đánh

            // Luôn hướng mặt về phía Player khi đánh
            Vector3 direction = (playerTarget.position - transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            // Trigger animation Melee
            animator.SetTrigger(MeleeTriggerHash);
        }
    }

    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
        }
    }

    // Vẽ bán kính tuần tra và tầm nhìn trực quan trong cửa sổ Scene
    private void OnDrawGizmosSelected()
    {
        // Vòng tròn tầm nhìn 50m (Màu vàng)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Vòng tròn tầm đánh 2m (Màu đỏ)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Vòng tròn đường đi tuần tra (Màu xanh lá)
        Gizmos.color = Color.green;
        Vector3 center = Application.isPlaying ? spawnPosition : transform.position;
        Gizmos.DrawWireSphere(center, patrolRadius);
    }
}