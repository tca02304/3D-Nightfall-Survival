using System;
using UnityEngine;

namespace DucThanh
{
    /// <summary>
    /// Component quản lý máu của Enemy, tích hợp với EnemyStats để nhận máu và tăng chỉ số theo ngày.
    /// Giữ nguyên các hàm và thuộc tính tương thích cho AttackRateLimiter & PlayerCombat.
    /// </summary>
    [RequireComponent(typeof(EnemyStats))]
    public class EnemyTest : MonoBehaviour
    {
        [Tooltip("Tham chiếu tới EnemyStats. Nếu để trống sẽ tự động lấy trên cùng GameObject.")]
        [SerializeField] private EnemyStats enemyStats;

        public float MaxHealth => enemyStats != null ? enemyStats.MaxHealth : 100f;
        public float CurrentHealth => enemyStats != null ? enemyStats.CurrentHealth : 100f;
        public bool IsDead => enemyStats != null ? enemyStats.IsDead : false;

        private void Awake()
        {
            if (enemyStats == null)
            {
                enemyStats = GetComponent<EnemyStats>();
            }
        }

        /// <summary>
        /// Nhận sát thương khi bị player chém trúng (chuyển tiếp sang EnemyStats)
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (enemyStats != null)
            {
                enemyStats.TakeDamage(damage);
            }
            else
            {
                Debug.LogWarning($"[EnemyTest] Chưa có EnemyStats trên {gameObject.name}");
            }
        }
    }
}
