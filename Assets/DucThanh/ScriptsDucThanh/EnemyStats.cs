using System;
using UnityEngine;

namespace DucThanh
{
    /// <summary>
    /// Component quản lý chỉ số máu, sát thương và cơ chế tăng sức mạnh theo từng ngày cho quái.
    /// - Hỗ trợ gán EnemyTypeConfigSO để tái sử dụng và quản lý nhiều loại quái tối ưu nhất.
    /// - Tự động cập nhật chỉ số khi bước sang ngày mới qua DayNightController.
    /// - Tương thích ngược: nếu không gắn SO, vẫn có thể cấu hình trực tiếp trên Inspector.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyStats : MonoBehaviour
    {
        [Header("Enemy Type Configuration (Tối ưu cho nhiều loại quái)")]
        [Tooltip("ScriptableObject cấu hình loại quái. Nếu để trống sẽ dùng các thông số mặc định bên dưới.")]
        [SerializeField] private EnemyTypeConfigSO typeConfig;

        [Header("Standalone Stats (Dùng khi không có ScriptableObject)")]
        [SerializeField] private float baseMaxHealth = 100f;
        [SerializeField] private float baseDamage = 15f;
        [SerializeField] private EnemyGrowthType fallbackGrowthType = EnemyGrowthType.Linear;
        [Range(0f, 2f)]
        [SerializeField] private float fallbackHealthGrowthRate = 0.15f;
        [Range(0f, 2f)]
        [SerializeField] private float fallbackDamageGrowthRate = 0.10f;

        [Header("Runtime State (Hiển thị runtime)")]
        [SerializeField] private float currentMaxHealth;
        [SerializeField] private float currentHealth;
        [SerializeField] private float currentDamage;
        [SerializeField] private int registeredDay = 1;
        [SerializeField] private float destroyDelay = 0.3f;

        private bool isDead = false;
        private DayNightController dayNightController;

        public float MaxHealth => currentMaxHealth;
        public float CurrentHealth => currentHealth;
        public float CurrentDamage => currentDamage;
        public bool IsDead => isDead;
        public EnemyTypeConfigSO TypeConfig => typeConfig;

        public event Action<float, float> OnHealthChanged; // (current, max)
        public event Action<float> OnDamageUpdated;         // (currentDamage)
        public event Action OnDeath;

        private void Awake()
        {
            dayNightController = UnityEngine.Object.FindFirstObjectByType<DayNightController>();
            int startingDay = dayNightController != null ? dayNightController.CurrentDay : 1;
            ApplyStatsForDay(startingDay, isInitialSetup: true);
        }

        private void OnEnable()
        {
            if (dayNightController != null)
            {
                dayNightController.OnDayChanged += HandleDayChanged;
            }
        }

        private void OnDisable()
        {
            if (dayNightController != null)
            {
                dayNightController.OnDayChanged -= HandleDayChanged;
            }
        }

        /// <summary>
        /// Xử lý khi bước sang ngày mới: quái tự động tăng tiến sức mạnh
        /// </summary>
        private void HandleDayChanged(int newDay)
        {
            if (isDead) return;
            ApplyStatsForDay(newDay, isInitialSetup: false);
        }

        /// <summary>
        /// Cập nhật máu và dame tương ứng với ngày hiện tại
        /// </summary>
        public void ApplyStatsForDay(int day, bool isInitialSetup = false)
        {
            registeredDay = Mathf.Max(1, day);
            float newMaxHealth;
            float newDamage;

            if (typeConfig != null)
            {
                newMaxHealth = typeConfig.CalculateMaxHealthForDay(registeredDay);
                newDamage = typeConfig.CalculateDamageForDay(registeredDay);
            }
            else
            {
                int dayOffset = registeredDay - 1;
                if (fallbackGrowthType == EnemyGrowthType.Linear)
                {
                    newMaxHealth = baseMaxHealth * (1f + (dayOffset * fallbackHealthGrowthRate));
                    newDamage = baseDamage * (1f + (dayOffset * fallbackDamageGrowthRate));
                }
                else
                {
                    newMaxHealth = baseMaxHealth * Mathf.Pow(1f + fallbackHealthGrowthRate, dayOffset);
                    newDamage = baseDamage * Mathf.Pow(1f + fallbackDamageGrowthRate, dayOffset);
                }
                newMaxHealth = Mathf.Max(1f, Mathf.Round(newMaxHealth));
                newDamage = Mathf.Max(1f, Mathf.Round(newDamage * 10f) / 10f);
            }

            if (isInitialSetup)
            {
                currentMaxHealth = newMaxHealth;
                currentHealth = currentMaxHealth;
            }
            else
            {
                // Khi qua ngày mới: Tăng max health và hồi lượng máu gia tăng (hoặc giữ tỉ lệ máu)
                float healthRatio = currentMaxHealth > 0 ? (currentHealth / currentMaxHealth) : 1f;
                currentMaxHealth = newMaxHealth;
                currentHealth = Mathf.Clamp(currentMaxHealth * healthRatio, 1f, currentMaxHealth);
            }

            currentDamage = newDamage;

            // Đồng bộ sang EnemyDamageDealer nếu có gắn cùng GameObject hoặc component con
            EnemyDamageDealer damageDealer = GetComponent<EnemyDamageDealer>();
            if (damageDealer != null)
            {
                damageDealer.SetBaseDamage(currentDamage);
            }

            OnHealthChanged?.Invoke(currentHealth, currentMaxHealth);
            OnDamageUpdated?.Invoke(currentDamage);
        }

        /// <summary>
        /// Gán cấu hình loại quái động (hữu ích khi spawner sinh quái)
        /// </summary>
        public void SetTypeConfig(EnemyTypeConfigSO newConfig, int currentDay = 1)
        {
            typeConfig = newConfig;
            ApplyStatsForDay(currentDay, isInitialSetup: true);
        }

        /// <summary>
        /// Nhận sát thương khi bị tấn công
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (isDead) return;

            currentHealth -= damage;
            if (currentHealth < 0f)
            {
                currentHealth = 0f;
            }

            OnHealthChanged?.Invoke(currentHealth, currentMaxHealth);
            Debug.Log($"<color=red>[EnemyStats] {gameObject.name} nhận {damage} dame -> Máu còn: {currentHealth}/{currentMaxHealth} (Day {registeredDay})</color>");

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;

            OnDeath?.Invoke();
            Debug.Log($"<color=gray>[EnemyStats] {gameObject.name} đã chết!</color>");
            Destroy(gameObject, destroyDelay);
        }
    }
}
