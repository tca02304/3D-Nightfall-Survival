using UnityEngine;

namespace DucThanh
{
    public enum EnemyGrowthType
    {
        Linear,      // Tăng đều tuyến tính mỗi ngày: Base * (1 + (Day - 1) * Rate)
        Percentage   // Tăng theo cấp số nhân % lũy tiến: Base * (1 + Rate)^(Day - 1)
    }

    /// <summary>
    /// ScriptableObject định nghĩa cấu hình chỉ số cơ bản và tốc độ tăng trưởng theo ngày cho một loại quái.
    /// Giúp quản lý tập trung nhiều loại quái (Zombie, Skeleton, Boss, Mutated...) một cách tối ưu,
    /// không cần sửa code mỗi khi thêm hay cân bằng quái mới.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyTypeConfig", menuName = "DucThanh/Enemy Type Config")]
    public class EnemyTypeConfigSO : ScriptableObject
    {
        [Header("General Info")]
        [Tooltip("Tên định danh loại quái")]
        [SerializeField] private string enemyTypeName = "Normal Monster";

        [Header("Base Stats (Chỉ số gốc ngày 1)")]
        [Tooltip("Lượng máu tối đa ở ngày 1")]
        [SerializeField] private float baseMaxHealth = 100f;

        [Tooltip("Lượng sát thương cơ bản ở ngày 1")]
        [SerializeField] private float baseDamage = 15f;

        [Header("Growth Scaling (Cơ chế quái mạnh lên theo ngày)")]
        [Tooltip("Công thức tăng tiến chỉ số (Tuyến tính hoặc Tăng % lũy tiến)")]
        [SerializeField] private EnemyGrowthType growthType = EnemyGrowthType.Linear;

        [Tooltip("Tỉ lệ tăng máu mỗi ngày (VD: 0.15 = +15% máu cơ bản mỗi ngày)")]
        [Range(0f, 2f)]
        [SerializeField] private float healthGrowthRatePerDay = 0.15f;

        [Tooltip("Tỉ lệ tăng sát thương mỗi ngày (VD: 0.10 = +10% dame cơ bản mỗi ngày)")]
        [Range(0f, 2f)]
        [SerializeField] private float damageGrowthRatePerDay = 0.10f;

        [Tooltip("Hệ số sát thương tối đa quái có thể đạt được (ngăn việc dame quá vô lý ở ngày cao). Đặt <= 0 nếu không giới hạn.")]
        [SerializeField] private float maxDamageCap = 0f;

        [Tooltip("Hệ số máu tối đa có thể đạt được. Đặt <= 0 nếu không giới hạn.")]
        [SerializeField] private float maxHealthCap = 0f;

        public string EnemyTypeName => enemyTypeName;
        public float BaseMaxHealth => baseMaxHealth;
        public float BaseDamage => baseDamage;
        public EnemyGrowthType GrowthType => growthType;
        public float HealthGrowthRatePerDay => healthGrowthRatePerDay;
        public float DamageGrowthRatePerDay => damageGrowthRatePerDay;

        /// <summary>
        /// Tính toán lượng máu tối đa cho một ngày cụ thể
        /// </summary>
        public float CalculateMaxHealthForDay(int day)
        {
            int dayOffset = Mathf.Max(0, day - 1);
            float calculatedHealth = baseMaxHealth;

            switch (growthType)
            {
                case EnemyGrowthType.Linear:
                    calculatedHealth = baseMaxHealth * (1f + (dayOffset * healthGrowthRatePerDay));
                    break;
                case EnemyGrowthType.Percentage:
                    calculatedHealth = baseMaxHealth * Mathf.Pow(1f + healthGrowthRatePerDay, dayOffset);
                    break;
            }

            if (maxHealthCap > 0f)
            {
                calculatedHealth = Mathf.Min(calculatedHealth, maxHealthCap);
            }

            return Mathf.Max(1f, Mathf.Round(calculatedHealth));
        }

        /// <summary>
        /// Tính toán sát thương cố định cho một ngày cụ thể
        /// </summary>
        public float CalculateDamageForDay(int day)
        {
            int dayOffset = Mathf.Max(0, day - 1);
            float calculatedDamage = baseDamage;

            switch (growthType)
            {
                case EnemyGrowthType.Linear:
                    calculatedDamage = baseDamage * (1f + (dayOffset * damageGrowthRatePerDay));
                    break;
                case EnemyGrowthType.Percentage:
                    calculatedDamage = baseDamage * Mathf.Pow(1f + damageGrowthRatePerDay, dayOffset);
                    break;
            }

            if (maxDamageCap > 0f)
            {
                calculatedDamage = Mathf.Min(calculatedDamage, maxDamageCap);
            }

            return Mathf.Max(1f, Mathf.Round(calculatedDamage * 10f) / 10f);
        }
    }
}
