using UnityEngine;
using DucThanh;

namespace Nghia
{
    /// <summary>
    /// Tự động phát hiện khi quái vật (Sói, LilDave Dinosaur, Goblin, ...)
    /// vào trạng thái tấn công hoặc va chạm Player để kích hoạt âm thanh.
    /// Thiết kế theo phong cách Muck: chuẩn nhịp, tự động nhận diện collider và AI state.
    /// </summary>
    public class EnemyAttackAudioDetector : MonoBehaviour
    {
        [Header("References (Tự động tìm nếu để trống)")]
        [SerializeField] private NavMeshEnemyAI enemyAI;
        [SerializeField] private EnemyDamageDealer damageDealer;
        [SerializeField] private EnemyAttackAudioSubject subject;

        [Header("Detection Settings")]
        [Tooltip("Tag nhận diện Player")]
        [SerializeField] private string playerTag = "Player";

        [Tooltip("Khoảng cách tối thiểu giữa 2 lần phát âm thanh (tránh spam khi va chạm liên tục)")]
        [SerializeField] private float attackAudioCooldown = 0.8f;

        private float nextAllowedAudioTime = 0f;

        private void Awake()
        {
            if (enemyAI == null)
                enemyAI = GetComponent<NavMeshEnemyAI>();

            if (damageDealer == null)
                damageDealer = GetComponent<EnemyDamageDealer>();

            if (subject == null)
            {
                subject = GetComponent<EnemyAttackAudioSubject>();
                if (subject == null)
                    subject = gameObject.AddComponent<EnemyAttackAudioSubject>();
            }
        }

        private void OnEnable()
        {
            if (damageDealer != null)
            {
                damageDealer.OnDamageDealt += HandleDamageDealt;
            }
        }

        private void OnDisable()
        {
            if (damageDealer != null)
            {
                damageDealer.OnDamageDealt -= HandleDamageDealt;
            }
        }

        private void Update()
        {
            // Kiểm tra trạng thái tấn công từ NavMeshEnemyAI (nếu có)
            if (enemyAI != null && enemyAI.currentState == NavMeshEnemyAI.AIState.Attack)
            {
                TryTriggerAttackAudio();
            }
        }

        private void HandleDamageDealt(float damage)
        {
            // Khi EnemyDamageDealer gây dame lên Player thành công
            TryTriggerAttackAudio();
        }

        // Tự động bắt va chạm trực tiếp giữa quái và Player
        private void OnTriggerEnter(Collider other)
        {
            CheckAndTriggerCollision(other);
        }

        private void OnTriggerStay(Collider other)
        {
            CheckAndTriggerCollision(other);
        }

        private void OnCollisionEnter(Collision collision)
        {
            CheckAndTriggerCollision(collision.collider);
        }

        private void OnCollisionStay(Collision collision)
        {
            CheckAndTriggerCollision(collision.collider);
        }

        private void CheckAndTriggerCollision(Collider col)
        {
            if (col == null) return;
            if (col.CompareTag(playerTag) || col.name.ToLower().Contains("player"))
            {
                TryTriggerAttackAudio(col);
            }
        }

        public void TryTriggerAttackAudio(Collider playerCol = null)
        {
            if (Time.time < nextAllowedAudioTime) return;

            float cooldown = (enemyAI != null && enemyAI.attackCooldown > 0.1f)
                ? enemyAI.attackCooldown
                : attackAudioCooldown;

            nextAllowedAudioTime = Time.time + cooldown;

            if (subject != null)
            {
                if (playerCol != null)
                    subject.NotifyHitPlayer(playerCol);
                else
                    subject.NotifyAttack();
            }
        }
    }
}
