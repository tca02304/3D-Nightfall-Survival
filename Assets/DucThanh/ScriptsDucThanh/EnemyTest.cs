using System.Collections;
using UnityEngine;

namespace DucThanh
{
    public class EnemyTest : MonoBehaviour
    {
        [Header("Enemy Stats")]
        [Tooltip("Lượng máu tối đa")]
        [SerializeField] private float maxHealth = 100f;

        [Tooltip("Lượng máu hiện tại")]
        [SerializeField] private float currentHealth;

        [Tooltip("Thời gian chờ trước khi biến mất sau khi chết")]
        [SerializeField] private float destroyDelay = 0.3f;

        private bool isDead = false;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsDead => isDead;

        private void Awake()
        {
            currentHealth = maxHealth;
            isDead = false;
        }

        /// <summary>
        /// Nhận sát thương khi bị player chém trúng
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (isDead) return;

            currentHealth -= damage;
            if (currentHealth < 0f)
            {
                currentHealth = 0f;
            }

            Debug.Log($"Enemy có {currentHealth}hp");

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;

            Debug.Log("quái đã chết");
            Destroy(gameObject, destroyDelay);
        }
    }
}
