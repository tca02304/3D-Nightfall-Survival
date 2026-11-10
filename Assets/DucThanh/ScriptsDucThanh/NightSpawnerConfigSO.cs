using System;
using UnityEngine;

namespace DucThanh
{
    /// <summary>
    /// Cấu hình thông số sinh quái theo từng đêm, số lượng (budget) và phân bổ chủng loại quái.
    /// ScriptableObject này cho phép Game Designer dễ dàng cân bằng game trực tiếp trên Editor mà không cần sửa code.
    /// </summary>
    [CreateAssetMenu(fileName = "NightSpawnerConfig", menuName = "DucThanh/Night Spawner Config")]
    public class NightSpawnerConfigSO : ScriptableObject
    {
        [Header("Night Window Configuration (Khung giờ sinh quái)")]
        [Tooltip("Giờ bắt đầu sinh quái tối thiểu ban đêm (20.0 = 8h tối)")]
        [Range(18f, 24f)]
        [SerializeField] private float minStartHour = 20f;

        [Tooltip("Giờ bắt đầu sinh quái tối đa ban đêm (24.0 = 12h đêm)")]
        [Range(18f, 24f)]
        [SerializeField] private float maxStartHour = 24f;

        [Tooltip("Giờ kết thúc sinh quái tối thiểu sáng hôm sau (3.0 = 3h sáng)")]
        [Range(1f, 6f)]
        [SerializeField] private float minEndHour = 3f;

        [Tooltip("Giờ kết thúc sinh quái tối đa sáng hôm sau (5.0 = 5h sáng)")]
        [Range(1f, 6f)]
        [SerializeField] private float maxEndHour = 5f;

        [Header("Spawn Budget & Scaling (Số lượng quái sinh ra)")]
        [Tooltip("Tổng số quái sinh ra trong đêm 1 (Mặc định 10 con)")]
        [SerializeField] private int baseBudgetNight1 = 10;

        [Tooltip("Số quái tăng thêm mỗi đêm (Mặc định +2 con mỗi đêm: Đêm 1: 10, Đêm 2: 12, Đêm 3: 14...)")]
        [SerializeField] private float budgetGrowthPerNight = 2f;

        [Tooltip("Hệ số tăng theo cấp số nhân mỗi đêm (1.0 = chỉ tăng đều tuyến tính +2 con/đêm)")]
        [Range(1f, 1.5f)]
        [SerializeField] private float budgetMultiplierPerNight = 1.0f;

        [Tooltip("Giới hạn tổng số quái tối đa sinh ra trong 1 đêm (Mặc định tối đa 100 con)")]
        [SerializeField] private int maxNightSpawnBudgetCap = 100;

        [Tooltip("Giới hạn số lượng quái còn sống đồng thời trên bản đồ ở đêm 1 để tối ưu hiệu năng (FPS)")]
        [SerializeField] private int maxAliveMonstersLimit = 10;

        [Tooltip("Số quái giới hạn sống đồng thời cộng thêm mỗi đêm")]
        [SerializeField] private int maxAliveGrowthPerNight = 2;

        [Tooltip("Giới hạn cứng tối đa cho số quái sống đồng thời trên bản đồ")]
        [SerializeField] private int absoluteMaxAliveCap = 100;

        [Tooltip("Số lượng quái sinh ra tối đa trong MỘT LẦN spawn (Mặc định tối đa 3 con/lần)")]
        [Range(1, 10)]
        [SerializeField] private int maxEnemiesPerSingleSpawn = 3;

        [Header("Spawn Positioning (Khoảng cách xuất hiện quanh Player)")]
        [Tooltip("Bán kính tối thiểu so với Player (đảm bảo không xuất hiện đè lên mặt người chơi)")]
        [SerializeField] private float minSpawnRadius = 18f;

        [Tooltip("Bán kính tối đa so với Player")]
        [SerializeField] private float maxSpawnRadius = 38f;

        [Tooltip("Xóa sạch quái còn sót lại khi trời sáng (True) hoặc để chúng tự do (False)")]
        [SerializeField] private bool despawnRemainingAtDawn = false;

        [Header("Enemy Types & Selection Weight (Tùy biến các loại quái)")]
        [SerializeField] private EnemySpawnEntry[] enemyEntries = new EnemySpawnEntry[0];

        public float MinStartHour => minStartHour;
        public float MaxStartHour => maxStartHour;
        public float MinEndHour => minEndHour;
        public float MaxEndHour => maxEndHour;

        public int BaseBudgetNight1 => baseBudgetNight1;
        public float BudgetGrowthPerNight => budgetGrowthPerNight;
        public float BudgetMultiplierPerNight => budgetMultiplierPerNight;
        public int MaxNightSpawnBudgetCap => maxNightSpawnBudgetCap;
        public int MaxAliveMonstersLimit => maxAliveMonstersLimit;
        public int MaxAliveGrowthPerNight => maxAliveGrowthPerNight;
        public int AbsoluteMaxAliveCap => absoluteMaxAliveCap;
        public int MaxEnemiesPerSingleSpawn => maxEnemiesPerSingleSpawn;

        public float MinSpawnRadius => minSpawnRadius;
        public float MaxSpawnRadius => maxSpawnRadius;
        public bool DespawnRemainingAtDawn => despawnRemainingAtDawn;

        public EnemySpawnEntry[] EnemyEntries => enemyEntries;

        /// <summary>
        /// Tính giới hạn số quái sống đồng thời tối đa cho đêm thứ `night`
        /// </summary>
        public int CalculateMaxAliveForNight(int night)
        {
            int nightOffset = Mathf.Max(0, night - 1);
            int calculated = maxAliveMonstersLimit + (nightOffset * maxAliveGrowthPerNight);
            if (absoluteMaxAliveCap > 0)
            {
                calculated = Mathf.Min(calculated, absoluteMaxAliveCap);
            }
            return Mathf.Max(1, calculated);
        }

        /// <summary>
        /// Tính toán tổng số lượng/điểm quái được sinh ra cho đêm thứ `night`
        /// Sử dụng công thức tối ưu kết hợp Tuyến tính + Tăng trưởng bền vững có trần (Cap)
        /// </summary>
        public int CalculateBudgetForNight(int night)
        {
            int nightOffset = Mathf.Max(0, night - 1);
            float budget = (baseBudgetNight1 + (nightOffset * budgetGrowthPerNight)) * Mathf.Pow(budgetMultiplierPerNight, nightOffset);
            int finalBudget = Mathf.RoundToInt(budget);

            if (maxNightSpawnBudgetCap > 0)
            {
                finalBudget = Mathf.Min(finalBudget, maxNightSpawnBudgetCap);
            }

            return Mathf.Max(1, finalBudget);
        }
    }

    /// <summary>
    /// Đại diện cho 1 loại quái trong danh sách sinh sản
    /// </summary>
    [Serializable]
    public class EnemySpawnEntry
    {
        [Tooltip("Tên gợi nhớ")]
        public string label = "Normal Monster";

        [Tooltip("Prefab của quái sẽ được Instantiate hoặc lấy từ Object Pool")]
        public GameObject prefab;

        [Tooltip("Cấu hình chỉ số máu/sát thương mở rộng (EnemyTypeConfigSO)")]
        public EnemyTypeConfigSO typeConfig;

        [Tooltip("Đêm đầu tiên loại quái này bắt đầu xuất hiện (VD: Đêm 1 quái thường, Đêm 3 mới có quái mạnh)")]
        [Range(1, 100)]
        public int unlockNight = 1;

        [Tooltip("Trọng số xuất hiện cơ bản (càng cao tỉ lệ random ra quái này càng lớn)")]
        [Range(1, 1000)]
        public int baseWeight = 100;

        [Tooltip("Trọng số cộng thêm mỗi đêm tiếp theo")]
        public int weightGrowthPerNight = 0;

        [Tooltip("Chi phí điểm (Cost) cho mỗi con quái này khi sinh ra")]
        [Range(1, 50)]
        public int cost = 1;

        [Tooltip("Số lượng quái sinh ra trong 1 lần (Cluster / Pack spawn)")]
        [Range(1, 5)]
        public int countPerSpawn = 1;

        /// <summary>
        /// Tính trọng số tương đối của quái theo đêm hiện tại
        /// </summary>
        public int GetWeightForNight(int night)
        {
            int requiredNight = Mathf.Max(1, unlockNight);
            if (night < requiredNight) return 0;
            int nightOffset = Mathf.Max(0, night - requiredNight);
            int effectiveBaseWeight = Mathf.Max(1, baseWeight);
            int finalWeight = effectiveBaseWeight + (nightOffset * Mathf.Max(0, weightGrowthPerNight));
            return Mathf.Max(1, finalWeight);
        }
    }
}
