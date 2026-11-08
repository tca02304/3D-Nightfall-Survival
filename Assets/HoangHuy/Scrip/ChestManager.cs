using UnityEngine;

public class ChestManager : MonoBehaviour
{
    [Header("Terrain & Cấu hình vùng")]
    public Terrain terrain;
    [Tooltip("Tâm khu vực spawn rương (Nếu trống sẽ lấy vị trí của GameObject này)")]
    public Transform centerPoint;
    public float spawnRadius = 100f; // Bán kính khu vực rương xuất hiện
    public LayerMask groundLayers;   // Layer của Terrain hoặc mặt đất

    [Header("Độ cao bù trừ để rương không bị chìm")]
    public float spawnOffsetY = 0.3f; // Tăng giảm giá trị này nếu rương vẫn hơi lún hoặc lơ lửng

    [Header("Danh sách Prefab Rương & Số lượng")]
    public GameObject chestBeginningPrefab;
    public int countBeginning = 50;

    public GameObject chestFreePrefab;
    public int countFree = 50;

    public GameObject chestGoldPrefab;
    public int countGold = 20;

    public GameObject chestMediumPrefab;
    public int countMedium = 35;

    void Start()
    {
        SpawnAllChests();
    }

    void SpawnAllChests()
    {
        if (terrain == null)
        {
            Debug.LogError("Chưa gán Terrain vào ChestManager!");
            return;
        }

        // Tiến hành spawn từng loại rương theo đúng số lượng yêu cầu
        SpawnChestType(chestBeginningPrefab, countBeginning);
        SpawnChestType(chestFreePrefab, countFree);
        SpawnChestType(chestGoldPrefab, countGold);
        SpawnChestType(chestMediumPrefab, countMedium);
    }

    void SpawnChestType(GameObject prefab, int totalCount)
    {
        if (prefab == null || totalCount <= 0) return;

        Vector3 spawnCenter = centerPoint != null ? centerPoint.position : transform.position;
        int spawned = 0;
        int maxAttempts = totalCount * 10; // Giới hạn vòng lặp tránh bị treo game
        int attempts = 0;

        while (spawned < totalCount && attempts < maxAttempts)
        {
            attempts++;

            // 1. Tạo tọa độ X, Z ngẫu nhiên hình tròn quanh tâm
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            float randomX = spawnCenter.x + randomCircle.x;
            float randomZ = spawnCenter.z + randomCircle.y;

            // 2. Bắn Raycast từ trên cao xuống để bắt đúng độ cao mặt đất của Terrain
            float rayOriginY = terrain.transform.position.y + terrain.terrainData.size.y + 100f;
            Vector3 rayOrigin = new Vector3(randomX, rayOriginY, randomZ);

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, Mathf.Infinity, groundLayers))
            {
                // Chỉ spawn nếu mặt đất không quá dốc (góc nhỏ hơn 30 độ)
                if (Vector3.Angle(hit.normal, Vector3.up) < 30f)
                {
                    // Xoay ngẫu nhiên hướng của rương quanh trục Y cho tự nhiên
                    Quaternion randomRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

                    // 3. Tính toán vị trí spawn và cộng thêm khoảng bù trừ chiều cao (spawnOffsetY)
                    Vector3 spawnPosition = hit.point + new Vector3(0, spawnOffsetY, 0);

                    // Khởi tạo rương và đặt làm con của Manager này cho gọn Hierarchy
                    Instantiate(prefab, spawnPosition, randomRotation, transform);

                    spawned++;
                }
            }
        }

        Debug.Log($"Đã sinh thành công {spawned}/{totalCount} rương loại: {prefab.name}");
    }

    // Vẽ vòng tròn khu vực spawn trên Scene View để dễ nhận biết
    private void OnDrawGizmosSelected()
    {
        Vector3 spawnCenter = centerPoint != null ? centerPoint.position : transform.position;
        Gizmos.color = Color.yellow; // Vòng tròn màu vàng biểu thị vùng rương xuất hiện
        Gizmos.DrawWireSphere(spawnCenter, spawnRadius);
    }
}