using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace DucThanh
{
    /// <summary>
    /// Hệ thống sinh quái vật ban đêm (Night Monster Spawner) cho DucThanh.
    /// Tính năng nổi bật:
    /// 1. Tự động random khung giờ bắt đầu mỗi đêm (trong khoảng 8h tối - 12h đêm, tức 20h00 - 24h00).
    /// 2. Tự động random khung giờ kết thúc sinh quái (trong khoảng 3h sáng - 5h sáng hôm sau).
    /// 3. Tùy biến phân bổ quái bằng thuật toán Roulette-Wheel Selection (Weighted Random Selection) dựa trên NightSpawnerConfigSO.
    /// 4. Quản lý lượng quái theo Ngân sách đêm (Night Budget System) và kiểm soát số lượng quái sống tối đa (Cap) tránh tụt FPS.
    /// 5. Tính toán vị trí sinh quái xung quanh người chơi trên NavMesh hoặc Raycast mặt đất có kiểm tra tầm nhìn để không spawn lộ liễu.
    /// 6. Tích hợp sẵn với EnemyStats, DayNightController và EnemyPoolManager.
    /// </summary>
    [DisallowMultipleComponent]
    public class NightMonsterSpawner : MonoBehaviour
    {
        [Header("Configuration Reference")]
        [Tooltip("ScriptableObject chứa toàn bộ thông số cân bằng sinh quái")]
        [SerializeField] private NightSpawnerConfigSO config;

        [Header("Scene Dependencies")]
        [Tooltip("DayNightController để đồng bộ đồng hồ và ngày trong game. Nếu để trống sẽ tự động tìm.")]
        [SerializeField] private DayNightController dayNightController;

        [Tooltip("Transform của Player để tính tâm sinh quái. Nếu để trống sẽ tự động tìm Player.")]
        [SerializeField] private Transform playerTransform;

        [Header("Spawn Position Settings")]
        [Tooltip("Tầng lớp Layer của mặt đất (Terrain / Ground) để thả quái xuống đúng độ cao nếu không dùng NavMesh")]
        [SerializeField] private LayerMask groundLayer = ~0;

        [Tooltip("Ưu tiên lấy vị trí trên NavMesh nếu có")]
        [SerializeField] private bool useNavMeshIfAvailable = true;

        [Tooltip("Số lần thử tìm vị trí hợp lệ tối đa mỗi lần spawn")]
        [SerializeField] private int maxPlacementAttempts = 10;

        [Header("Debug & Monitor (Runtime State - Chỉ hiển thị trạng thái)")]
        [SerializeField] private bool isNightSpawningActive = false;
        [SerializeField] private int currentNight = 1;
        [SerializeField] private float scheduledStartHour = 21f;
        [SerializeField] private float scheduledEndHour = 4f;
        [SerializeField] private int currentNightTotalBudget = 0;
        [SerializeField] private int remainingBudget = 0;
        [SerializeField] private int currentAliveEnemies = 0;
        [SerializeField] private int currentNightMaxAliveAllowed = 10;

        private float nextSpawnTimeReal = 0f;
        private int lastEvaluatedNight = -1;
        private readonly List<GameObject> activeEnemiesList = new List<GameObject>();

        public bool IsNightSpawningActive => isNightSpawningActive;
        public int CurrentNight => currentNight;
        public float ScheduledStartHour => scheduledStartHour;
        public float ScheduledEndHour => scheduledEndHour;
        public int RemainingBudget => remainingBudget;
        public int CurrentAliveEnemies => currentAliveEnemies;
        public int CurrentNightMaxAliveAllowed => currentNightMaxAliveAllowed;
        public int EffectiveMaxAliveMonsters => config != null ? config.CalculateMaxAliveForNight(currentNight) : 10;

        private void Awake()
        {
            if (dayNightController == null)
            {
                dayNightController = FindFirstObjectByType<DayNightController>();
            }

            FindPlayerIfMissing();
        }

        private void Start()
        {
            if (dayNightController != null)
            {
                currentNight = dayNightController.CurrentDay;
                ScheduleNightHoursForDay(currentNight);
            }
            else
            {
                Debug.LogWarning("[NightMonsterSpawner] Không tìm thấy DayNightController trong Scene!");
            }
        }

        private void Update()
        {
            if (dayNightController == null || config == null) return;

            CleanDeadEnemiesFromList();

            float currentHour = dayNightController.CurrentHour;
            int day = dayNightController.CurrentDay;

            // Khi bước sang ngày mới -> lập lịch lại khung giờ ngẫu nhiên cho đêm này
            if (day != lastEvaluatedNight)
            {
                currentNight = day;
                ScheduleNightHoursForDay(currentNight);
                lastEvaluatedNight = day;
            }

            // Kiểm tra trạng thái hiện tại có đang nằm trong khung giờ sinh quái không
            // Quy tắc nghiêm ngặt: KHÔNG ĐƯỢC sinh quái sau thời gian lập lịch (sau scheduledEndHour)
            bool isTimeInWindow = CheckIfWithinSpawnWindow(currentHour);

            // Chỉ sinh quái khi đang trong khung giờ và vẫn còn ngân sách
            bool shouldSpawn = isTimeInWindow && remainingBudget > 0;

            if (shouldSpawn && !isNightSpawningActive)
            {
                // Bắt đầu đêm sinh quái
                isNightSpawningActive = true;
                Debug.Log($"<color=orange>[NightMonsterSpawner] BẮT ĐẦU ĐÊM {currentNight} SINH QUÁI! (Giờ game: {currentHour:F1}h | Bắt đầu lúc: {scheduledStartHour:F1}h | Hết lúc: {scheduledEndHour:F1}h | Tổng ngân sách: {currentNightTotalBudget})</color>");
            }
            else if (!shouldSpawn && isNightSpawningActive)
            {
                // Kết thúc đêm sinh quái khi đã hết ngân sách hoặc đồng hồ chạm mốc kết thúc lập lịch
                isNightSpawningActive = false;
                Debug.Log($"<color=yellow>[NightMonsterSpawner] KẾT THÚC ĐÊM {currentNight} SINH QUÁI! (Đã sinh: {currentNightTotalBudget - remainingBudget}/{currentNightTotalBudget} quái | Giờ game: {currentHour:F1}h | Hạn chót kết thúc: {scheduledEndHour:F1}h)</color>");

                if (config.DespawnRemainingAtDawn)
                {
                    ClearRemainingEnemies();
                }
            }

            // Quá trình sinh quái định kỳ
            if (isNightSpawningActive)
            {
                HandleNightSpawning(currentHour);
            }
            else
            {
                // Cảnh báo nếu chưa đến giờ để dev dễ theo dõi
                // (Chỉ log mỗi khi cần debug hoặc nhìn qua Inspector)
            }
        }

        /// <summary>
        /// Tạo ngẫu nhiên giờ bắt đầu (8h - 12h tối) và giờ kết thúc (3h - 5h sáng) cho đêm chỉ định
        /// </summary>
        public void ScheduleNightHoursForDay(int night)
        {
            if (config == null) return;

            // Random từ 20h00 đến 24h00 (8h - 12h tối)
            scheduledStartHour = UnityEngine.Random.Range(config.MinStartHour, config.MaxStartHour);

            // Random từ 3h00 đến 5h00 sáng hôm sau
            scheduledEndHour = UnityEngine.Random.Range(config.MinEndHour, config.MaxEndHour);

            // Tính toán ngân sách quái cho đêm này
            currentNightTotalBudget = config.CalculateBudgetForNight(night);
            remainingBudget = currentNightTotalBudget;
            currentNightMaxAliveAllowed = EffectiveMaxAliveMonsters;

            Debug.Log($"<color=cyan>[NightMonsterSpawner] Lập lịch đêm {night}: Spawn từ {FormatHour(scheduledStartHour)} -> {FormatHour(scheduledEndHour)} (Ngân sách: {currentNightTotalBudget} | Giới hạn sống tối đa đêm nay: {currentNightMaxAliveAllowed})</color>");
        }

        /// <summary>
        /// Kiểm tra giờ hiện tại có nằm trong khoảng thời gian [scheduledStartHour -> 24h] hoặc [0h -> scheduledEndHour]
        /// </summary>
        private bool CheckIfWithinSpawnWindow(float currentHour)
        {
            // Khoảng thời gian span qua nửa đêm (20h-24h và 0h-5h)
            if (currentHour >= scheduledStartHour || currentHour < scheduledEndHour)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Xử lý việc sinh quái theo nhịp điệu và thuật toán phân bổ tối ưu
        /// </summary>
        private void HandleNightSpawning(float currentHour)
        {
            // 1. Kiểm tra ngân sách quái còn lại
            if (remainingBudget <= 0) return;

            // 2. Kiểm tra giới hạn số lượng quái còn sống trên sàn đấu (tránh giật lag)
            int maxAllowed = EffectiveMaxAliveMonsters;
            if (activeEnemiesList.Count >= maxAllowed) return;

            // 3. Tính toán chính xác thời gian game còn lại từ giờ hiện tại đến scheduledEndHour
            float remainingGameHours = 0f;
            if (currentHour >= scheduledStartHour)
            {
                remainingGameHours = (24f - currentHour) + scheduledEndHour;
            }
            else if (currentHour < scheduledEndHour)
            {
                remainingGameHours = scheduledEndHour - currentHour;
            }

            // Nếu bằng cách nào đó đã vượt mốc kết thúc, dừng sinh quái ngay lập tức
            if (remainingGameHours <= 0.01f)
            {
                return;
            }

            // Tính số giây thực tế còn lại trước khi hết đêm
            float realSecondsPerHour = dayNightController != null ? dayNightController.RealSecondsPerInGameHour : 60f;
            float speedMult = (dayNightController != null && dayNightController.IsFastForwardActive) ? dayNightController.FastForwardMultiplier : 1f;
            float remainingRealSeconds = (remainingGameHours * realSecondsPerHour) / Mathf.Max(1f, speedMult);

            // Kiểm tra nhịp thời gian giữa các lần spawn
            if (Time.time < nextSpawnTimeReal) return;

            // Thuật toán Adaptive Auto-Pacing:
            // Tự động tính toán số đợt spawn và số quái cần sinh sao cho dùng HẾT SẠCH ngân sách trước khi remainingRealSeconds về 0
            EnemySpawnEntry selectedEntry = SelectEnemyByWeightedRoulette(currentNight, remainingBudget);
            if (selectedEntry == null || selectedEntry.prefab == null) return;

            int entryCost = Mathf.Max(1, selectedEntry.cost);
            int enemiesRemainingInBudget = remainingBudget / entryCost;

            // Xác định thời gian giãn cách lý tưởng để xả hết số quái trong quỹ thời gian còn lại
            // Để an toàn, chúng ta nhắm mục tiêu hoàn thành trước mốc kết thúc 10% (buffer)
            float targetDuration = Mathf.Max(1f, remainingRealSeconds * 0.85f);
            float targetInterval = targetDuration / Mathf.Max(1, enemiesRemainingInBudget);

            // Nếu thời gian còn lại quá gấp hoặc ngân sách còn nhiều, cho phép sinh theo bầy nhưng KHÔNG ĐƯỢC VƯỢT QUÁ GIỚI HẠN (mặc định tối đa 1 con/lần)
            int maxAllowedPerBatch = config != null ? Mathf.Max(1, config.MaxEnemiesPerSingleSpawn) : 1;
            int countToSpawn = Mathf.Clamp(selectedEntry.countPerSpawn, 1, maxAllowedPerBatch);

            if (targetInterval < 1.0f && enemiesRemainingInBudget > 1)
            {
                // Khi cần đẩy nhanh tiến độ, tăng dần theo bầy nhưng luôn bị chặn ở maxAllowedPerBatch (tối đa 1 con)
                int boostCluster = Mathf.CeilToInt(1.0f / Mathf.Max(0.1f, targetInterval));
                countToSpawn = Mathf.Clamp(Mathf.Max(countToSpawn, boostCluster), 1, maxAllowedPerBatch);
                targetInterval = 0.5f;
            }

            // Giới hạn trong khoảng an toàn (0.3s - 5.0s)
            targetInterval = Mathf.Clamp(targetInterval, 0.3f, 5.0f);

            int actuallySpawnedCount = 0;
            for (int i = 0; i < countToSpawn; i++)
            {
                if (remainingBudget < entryCost) break;
                if (activeEnemiesList.Count >= maxAllowed) break;

                if (TryGetValidSpawnPosition(out Vector3 spawnPos))
                {
                    SpawnSingleEnemy(selectedEntry, spawnPos);
                    remainingBudget -= entryCost;
                    actuallySpawnedCount++;
                }
            }

            // Lập lịch lần spawn kế tiếp
            nextSpawnTimeReal = Time.time + targetInterval;
        }

        /// <summary>
        /// Thuật toán Roulette-Wheel Selection (chọn theo xác suất trọng số)
        /// Phân bổ chính xác tỷ lệ các loại quái xuất hiện tùy theo cấp độ đêm hiện tại.
        /// </summary>
        private EnemySpawnEntry SelectEnemyByWeightedRoulette(int night, int currentBudget)
        {
            if (config.EnemyEntries == null || config.EnemyEntries.Length == 0) return null;

            int totalWeight = 0;
            List<KeyValuePair<EnemySpawnEntry, int>> eligibleEntries = new List<KeyValuePair<EnemySpawnEntry, int>>();

            foreach (var entry in config.EnemyEntries)
            {
                if (entry == null || entry.prefab == null) continue;
                if (entry.unlockNight > night) continue;
                if (entry.cost > currentBudget) continue;

                int weight = entry.GetWeightForNight(night);
                if (weight > 0)
                {
                    totalWeight += weight;
                    eligibleEntries.Add(new KeyValuePair<EnemySpawnEntry, int>(entry, weight));
                }
            }

            if (eligibleEntries.Count == 0 || totalWeight <= 0) return null;

            int randomValue = UnityEngine.Random.Range(0, totalWeight);
            int cumulative = 0;

            foreach (var pair in eligibleEntries)
            {
                cumulative += pair.Value;
                if (randomValue < cumulative)
                {
                    return pair.Key;
                }
            }

            return eligibleEntries[eligibleEntries.Count - 1].Key;
        }

        /// <summary>
        /// Tạo một cá thể quái và thiết lập cấu hình chỉ số cho nó
        /// </summary>
        private void SpawnSingleEnemy(EnemySpawnEntry entry, Vector3 position)
        {
            Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
            GameObject enemyObj = null;

            // Sử dụng Pool nếu có, ngược lại Instantiate thông thường
            if (EnemyPoolManager.Instance != null)
            {
                enemyObj = EnemyPoolManager.Instance.Spawn(entry.prefab, position, rotation);
            }
            else
            {
                enemyObj = Instantiate(entry.prefab, position, rotation);
            }

            if (enemyObj == null) return;

            // Thiết lập hoặc cập nhật chỉ số theo loại quái & đêm hiện tại
            EnemyStats stats = enemyObj.GetComponent<EnemyStats>();
            if (stats != null)
            {
                EnemyTypeConfigSO targetConfig = entry.typeConfig != null ? entry.typeConfig : stats.TypeConfig;
                if (targetConfig != null)
                {
                    stats.SetTypeConfig(targetConfig, currentNight);
                }
                else
                {
                    stats.ApplyStatsForDay(currentNight, isInitialSetup: true);
                }
            }

            activeEnemiesList.Add(enemyObj);
            currentAliveEnemies = activeEnemiesList.Count;
        }

        /// <summary>
        /// Tìm vị trí ngẫu nhiên xung quanh Player, kiểm tra NavMesh và độ cao địa hình
        /// </summary>
        private bool TryGetValidSpawnPosition(out Vector3 result)
        {
            result = Vector3.zero;
            FindPlayerIfMissing();

            Vector3 center = playerTransform != null ? playerTransform.position : transform.position;

            for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
            {
                // Chọn một góc ngẫu nhiên và bán kính ngẫu nhiên trong vành đai [minRadius, maxRadius]
                float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                float distance = UnityEngine.Random.Range(config.MinSpawnRadius, config.MaxSpawnRadius);

                Vector3 candidate = center + new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);

                // 1. Ưu tiên chiếu vị trí lên NavMesh nếu có dữ liệu NavMesh
                if (useNavMeshIfAvailable && NavMesh.SamplePosition(candidate, out NavMeshHit hit, 8f, NavMesh.AllAreas))
                {
                    result = hit.position;
                    return true;
                }

                // 2. Chiếu Raycast từ trên cao xuống để tìm mặt đất (Terrain / Mesh Collider)
                float rayOriginY = center.y + 40f;
                Ray ray = new Ray(new Vector3(candidate.x, rayOriginY, candidate.z), Vector3.down);
                if (Physics.Raycast(ray, out RaycastHit groundHit, 100f, groundLayer))
                {
                    result = groundHit.point;
                    return true;
                }

                // 3. Fallback dự phòng: Dùng ngay độ cao của người chơi
                result = new Vector3(candidate.x, center.y, candidate.z);
                return true;
            }

            return false;
        }

        private void CleanDeadEnemiesFromList()
        {
            for (int i = activeEnemiesList.Count - 1; i >= 0; i--)
            {
                GameObject enemy = activeEnemiesList[i];
                if (enemy == null || !enemy.activeInHierarchy)
                {
                    activeEnemiesList.RemoveAt(i);
                }
            }
            currentAliveEnemies = activeEnemiesList.Count;
        }

        private void ClearRemainingEnemies()
        {
            foreach (var enemy in activeEnemiesList)
            {
                if (enemy == null) continue;

                if (EnemyPoolManager.Instance != null)
                {
                    EnemyPoolManager.Instance.Despawn(enemy);
                }
                else
                {
                    Destroy(enemy);
                }
            }
            activeEnemiesList.Clear();
            currentAliveEnemies = 0;
        }

        private void FindPlayerIfMissing()
        {
            if (playerTransform == null)
            {
                PlayerController pc = FindFirstObjectByType<PlayerController>();
                if (pc != null)
                {
                    playerTransform = pc.transform;
                }
            }
        }

        private string FormatHour(float hour)
        {
            int h = Mathf.FloorToInt(hour) % 24;
            int m = Mathf.FloorToInt((hour - Mathf.Floor(hour)) * 60f);
            return $"{h:D2}:{m:D2}";
        }

        private void OnDrawGizmosSelected()
        {
            if (config == null) return;
            Vector3 center = playerTransform != null ? playerTransform.position : transform.position;

            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(center, config.MinSpawnRadius);

            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(center, config.MaxSpawnRadius);
        }
    }
}
