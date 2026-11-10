using UnityEngine;
using System.Collections.Generic;

public class VillageSpawner : MonoBehaviour
{
    [Header("Cấu hình Terrain & Tâm")]
    public Terrain terrain;
    public Transform centerPoint;
    public float spawnRadius = 200f; // Bán kính khu vực có thể xuất hiện làng trên map

    [Header("Danh sách Prefab Cụm Làng")]
    public GameObject[] villagePrefabs; // Kéo các Prefab cụm nhà vào đây
    public int totalVillagesToSpawn = 5; // Tổng số cụm làng muốn spawn trên map

    [Header("Điều kiện địa hình & khoảng cách")]
    public float minDistanceBetweenVillages = 50f; // Khoảng cách tối thiểu giữa 2 làng để không bị sát nhau
    public float maxTerrainSlope = 15f;          // Độ dốc tối đa cho phép (độ phẳng của đất)
    public float spawnOffsetY = 0.1f;            // Bù trừ chiều cao tránh lún đất

    // Lưu lại vị trí các làng đã spawn để kiểm tra khoảng cách
    private List<Vector3> spawnedVillagePositions = new List<Vector3>();

    void Start()
    {
        SpawnVillages();
    }

    void SpawnVillages()
    {
        if (terrain == null || villagePrefabs == null || villagePrefabs.Length == 0)
        {
            Debug.LogError("Chưa gán Terrain hoặc chưa thêm Prefab cụm làng vào VillageSpawner!");
            return;
        }

        Vector3 spawnCenter = centerPoint != null ? centerPoint.position : transform.position;
        int spawnedCount = 0;
        int maxAttempts = totalVillagesToSpawn * 20; // Giới hạn vòng lặp tránh bị đơ game
        int attempts = 0;

        while (spawnedCount < totalVillagesToSpawn && attempts < maxAttempts)
        {
            attempts++;

            // 1. Random tọa độ X, Z trong phạm vi bán kính
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            float randomX = spawnCenter.x + randomCircle.x;
            float randomZ = spawnCenter.z + randomCircle.y;

            // 2. Bắn Raycast từ trên cao xuống mặt đất
            float rayOriginY = terrain.transform.position.y + terrain.terrainData.size.y + 200f;
            Vector3 rayOrigin = new Vector3(randomX, rayOriginY, randomZ);

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, Mathf.Infinity))
            {
                // Kiểm tra xem mặt đất có đủ phẳng không (Góc nghiêng so với phương thẳng đứng nhỏ hơn giới hạn)
                float slope = Vector3.Angle(hit.normal, Vector3.up);
                if (slope <= maxTerrainSlope)
                {
                    Vector3 potentialPosition = hit.point + new Vector3(0, spawnOffsetY, 0);

                    // 3. Kiểm tra khoảng cách với các làng đã spawn trước đó
                    bool isTooClose = false;
                    foreach (Vector3 existingPos in spawnedVillagePositions)
                    {
                        if (Vector3.Distance(potentialPosition, existingPos) < minDistanceBetweenVillages)
                        {
                            isTooClose = true;
                            break;
                        }
                    }

                    // Nếu khoảng cách an toàn, tiến hành spawn
                    if (!isTooClose)
                    {
                        // Chọn ngẫu nhiên một mẫu làng trong danh sách Prefab
                        GameObject selectedPrefab = villagePrefabs[Random.Range(0, villagePrefabs.Length)];

                        // Xoay hướng ngẫu nhiên quanh trục Y cho tự nhiên
                        Quaternion randomRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

                        // Khởi tạo cụm làng và gán làm con của Spawner cho gọn Hierarchy
                        Instantiate(selectedPrefab, potentialPosition, randomRotation, transform);

                        // Lưu lại vị trí
                        spawnedVillagePositions.Add(potentialPosition);
                        spawnedCount++;
                    }
                }
            }
        }

        Debug.Log($"Đã spawn thành công {spawnedCount}/{totalVillagesToSpawn} cụm làng trên bản đồ!");
    }

    // Vẽ vùng giới hạn spawn làng màu xanh dương trên Scene View
    private void OnDrawGizmosSelected()
    {
        Vector3 spawnCenter = centerPoint != null ? centerPoint.position : transform.position;
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(spawnCenter, spawnRadius);
    }
}