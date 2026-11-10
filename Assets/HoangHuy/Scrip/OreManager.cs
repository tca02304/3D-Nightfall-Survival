using UnityEngine;

[System.Serializable]
public class OreSpawnData
{
    public string oreName;          // Tên loại quặng (để dễ quản lý trong Inspector)
    public GameObject orePrefab;    // Prefab của quặng
    public int spawnCount = 30;     // Số lượng muốn spawn trên map
}

public class OreManager : MonoBehaviour
{
    [Header("Terrain & Cấu hình vùng")]
    public Terrain terrain;
    [Tooltip("Tâm khu vực spawn quặng (Nếu trống sẽ lấy vị trí của GameObject này)")]
    public Transform centerPoint;
    public float spawnRadius = 150f; // Bán kính khu vực quặng xuất hiện
    public LayerMask groundLayers;   // Layer của Terrain hoặc mặt đất

    [Header("Kiểm tra va chạm tránh Làng")]
    public LayerMask obstacleLayers; // Chọn Layer "Village" để quặng không mọc đè lên nhà cửa
    public float checkOverlapRadius = 1.5f;

    [Header("Độ cao bù trừ")]
    public float spawnOffsetY = 0.1f; // Độ cao bù trừ để quặng bám sát mặt đất

    [Header("Danh sách tất cả các loại Quặng")]
    public OreSpawnData[] oreList;   // Bạn có thể thêm/bớt và chỉnh số lượng thoải mái ở đây

    void Start()
    {
        SpawnAllOres();
    }

    void SpawnAllOres()
    {
        if (terrain == null)
        {
            Debug.LogError("Chưa gán Terrain vào OreManager!");
            return;
        }

        if (oreList == null || oreList.Length == 0)
        {
            Debug.LogWarning("Danh sách quặng (Ore List) đang trống!");
            return;
        }

        // Vòng lặp duyệt qua từng loại quặng trong danh sách để spawn
        foreach (var oreData in oreList)
        {
            if (oreData.orePrefab != null && oreData.spawnCount > 0)
            {
                SpawnOreType(oreData.orePrefab, oreData.spawnCount);
            }
        }
    }

    void SpawnOreType(GameObject prefab, int totalCount)
    {
        int spawned = 0;
        int maxAttempts = totalCount * 10;
        int attempts = 0;
        Vector3 spawnCenter = centerPoint != null ? centerPoint.position : transform.position;

        while (spawned < totalCount && attempts < maxAttempts)
        {
            attempts++;

            // 1. Random tọa độ X, Z hình tròn quanh tâm
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            float randomX = spawnCenter.x + randomCircle.x;
            float randomZ = spawnCenter.z + randomCircle.y;

            // 2. Bắn Raycast lấy độ cao mặt đất
            float rayOriginY = terrain.transform.position.y + terrain.terrainData.size.y + 100f;
            Vector3 rayOrigin = new Vector3(randomX, rayOriginY, randomZ);

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, Mathf.Infinity, groundLayers))
            {
                // Độ dốc dưới 25 độ
                if (Vector3.Angle(hit.normal, Vector3.up) < 25f)
                {
                    Vector3 spawnPosition = hit.point + new Vector3(0, spawnOffsetY, 0);

                    // 3. Né khu vực làng mạc
                    if (!Physics.CheckSphere(spawnPosition, checkOverlapRadius, obstacleLayers))
                    {
                        Quaternion randomRotation = Quaternion.Euler(Random.Range(-5f, 5f), Random.Range(0f, 360f), Random.Range(-5f, 5f));
                        Instantiate(prefab, spawnPosition, randomRotation, transform);
                        spawned++;
                    }
                }
            }
        }

        Debug.Log($"Đã sinh thành công {spawned}/{totalCount} quặng: {prefab.name}");
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 spawnCenter = centerPoint != null ? centerPoint.position : transform.position;
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(spawnCenter, spawnRadius);
    }
}