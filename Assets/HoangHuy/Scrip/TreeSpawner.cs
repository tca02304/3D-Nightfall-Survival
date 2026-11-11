using UnityEngine;

public class TreeSpawner : MonoBehaviour
{
    [Header("Terrain & Prefabs")]
    public Terrain terrain;
    public GameObject[] treePrefabs;

    [Header("Spawn Area Settings")]
    [Tooltip("Tâm khu vực spawn. Nếu để trống, sẽ lấy vị trí của chính GameObject chứa script này.")]
    public Transform centerPoint;
    public float spawnRadius = 50f; // Bán kính khu vực muốn spawn

    [Header("Spawn Settings")]
    public int numberOfTrees = 100;
    public LayerMask groundLayers;

    private void OnDrawGizmosSelected()
    {
        // Xác định tâm (nếu có centerPoint thì lấy, không thì lấy vị trí hiện tại)
        Vector3 spawnCenter = centerPoint != null ? centerPoint.position : transform.position;

        // Đặt màu cho khung vẽ (Ví dụ: Màu xanh lá)
        Gizmos.color = Color.green;

        // Vẽ một hình cầu dạng lưới (WireSphere) thể hiện bán kính spawn
        Gizmos.DrawWireSphere(spawnCenter, spawnRadius);

        // Hoặc nếu là dạng hình hộp (Box), bạn có thể dùng:
        // Gizmos.DrawWireCube(spawnCenter, new Vector3(width, height, length));
    }

    void Start()
    {
        SpawnTrees();
    }

    void SpawnTrees()
    {
        if (terrain == null || treePrefabs.Length == 0) return;

        // Xác định tâm điểm spawn
        Vector3 spawnCenter = centerPoint != null ? centerPoint.position : transform.position;

        int spawnedCount = 0;
        int maxAttempts = numberOfTrees * 5; // Tránh vòng lặp vô tận
        int attempts = 0;

        while (spawnedCount < numberOfTrees && attempts < maxAttempts)
        {
            attempts++;

            // 1. Tạo vị trí ngẫu nhiên hình tròn quanh tâm với bán kính spawnRadius
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            float randomX = spawnCenter.x + randomCircle.x;
            float randomZ = spawnCenter.z + randomCircle.y;

            // 2. Bắn Raycast từ trên cao xuống để lấy độ cao chính xác của mặt đất/terrain
            float rayOriginY = terrain.transform.position.y + terrain.terrainData.size.y + 100f;
            Vector3 rayOrigin = new Vector3(randomX, rayOriginY, randomZ);

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, Mathf.Infinity, groundLayers))
            {
                // Kiểm tra xem góc dốc của mặt đất có quá dốc không (tránh mọc trên vách đá đứng)
                if (Vector3.Angle(hit.normal, Vector3.up) < 30f)
                {
                    // 3. Chọn ngẫu nhiên 1 loại cây/đá trong danh sách
                    GameObject selectedTree = treePrefabs[Random.Range(0, treePrefabs.Length)];

                    // 4. Xoay ngẫu nhiên quanh trục Y cho tự nhiên
                    Quaternion randomRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

                    // 5. Sinh ra trong Scene và đặt làm con của Spawner cho gọn Hierarchy
                    Instantiate(selectedTree, hit.point, randomRotation, transform);

                    spawnedCount++;
                }
            }
        }
    }
}