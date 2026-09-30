using UnityEngine;

public class WolfAI : MonoBehaviour
{
    [Header("Detection & Combat Settings")]
    public float detectionRange = 29.0f; // Phạm vi chạy lại gần Player (< 29f)
    public float attackRange = 2.0f;     // Khoảng cách dừng di chuyển để thực hiện cắn
    public float moveSpeed = 5.0f;       // Tốc độ chạy của Sói
    public float rotationSpeed = 12.0f;  // Tốc độ xoay mặt

    [Header("Damage Settings")]
    public int attackDamage = 10;        // Sát thương mỗi lần cắn
    public float attackCooldown = 1.2f;  // Thời gian hồi chiêu giữa 2 lần cắn (giây)
    private float nextAttackTime = 0f;

    [Header("Target & References")]
    public Transform playerTarget;
    private Animator animator;

    // Animator Hashes
    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int BiteTriggerHash = Animator.StringToHash("Bite"); // Hoặc "Attack" tùy đặt tên trong Controller

    private void Start()
    {
        animator = (Animator)GetComponent(typeof(Animator));
        FindPlayer();
    }

    private void Update()
    {
        if (playerTarget == null)
        {
            FindPlayer();
            if (playerTarget == null)
            {
                SetIdle();
                return;
            }
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        // Trạng thái 1: Player xa hơn 30f (hoặc > 29f) -> Dừng lại và Idle
        if (distanceToPlayer > detectionRange)
        {
            SetIdle();
        }
        // Trạng thái 2: Player trong phạm vi <= 29f và chưa vào tầm đánh cận chiến -> Chạy lại gần
        else if (distanceToPlayer > attackRange)
        {
            ChasePlayer();
        }
        // Trạng thái 3: Áp sát Player (<= 2f) -> Thực hiện cắn
        else
        {
            PerformBiteAnimation();
        }
    }

    private void SetIdle()
    {
        if (animator != null)
        {
            animator.SetBool(IsRunningHash, false);
        }
    }

    private void ChasePlayer()
    {
        Vector3 direction = (playerTarget.position - transform.position).normalized;
        direction.y = 0; // Giữ phẳng trên mặt đất

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

    private void PerformBiteAnimation()
    {
        if (animator != null)
        {
            animator.SetBool(IsRunningHash, false);

            // Quay mặt về phía Player khi cắn
            Vector3 direction = (playerTarget.position - transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            // Kích hoạt animation cắn nếu hết Cooldown
            if (Time.time >= nextAttackTime)
            {
                animator.SetTrigger(BiteTriggerHash);
                nextAttackTime = Time.time + attackCooldown;
            }
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

    // --- XỬ LÝ SÁT THƯƠNG KHI VA CHẠM (COLLISION / TRIGGER) ---

    // Dành cho Collider dạng Trigger (Is Trigger = true)
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            TryDealDamage(other.gameObject);
        }
    }

    // Dành cho Collider thường (Is Trigger = false)
    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            TryDealDamage(collision.gameObject);
        }
    }

    private void TryDealDamage(GameObject target)
    {
        // Kiểm tra xem Player có script nhận sát thương không (Ví dụ: PlayerHealth)
        // Bạn có thể đổi 'PlayerHealth' thành tên script máu của bạn
        /*
        PlayerHealth health = target.GetComponent();
        if (health != null)
        {
            health.TakeDamage(attackDamage);
        }
        */

        Debug.Log($"[Wolf] Cắn trúng Player! Gây {attackDamage} sát thương.");
    }

    // Vẽ tầm nhận diện trong cửa sổ Scene
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange); // Tầm phát hiện 29f

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);    // Tầm đánh 2f
    }
}