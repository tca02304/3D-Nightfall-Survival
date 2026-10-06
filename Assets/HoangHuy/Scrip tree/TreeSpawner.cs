using UnityEngine;

public class TreeSpawner : MonoBehaviour
{
    [Header("Terrain & Prefabs")]
    public Terrain terrain;
    public GameObject[] treePrefabs; // Danh sách các loại cây (Phong, Birch, Oak...)

    [Header("Spawn Settings")]
    public int numberOfTrees = 100;    // Tổng số lượng cây muốn rải
    public LayerMask groundLayer;     // Layer của Terrain (Ví dụ: "Ground")

    void Start()
    {
        SpawnTrees();
    }

    void SpawnTrees()
    {
        if (terrain == null || treePrefabs.Length == 0) return;

        Vector3 terrainSize = terrain.terrainData.size;
        Vector3 terrainPos = terrain.transform.position;

        int spawnedCount = 0;
        int maxAttempts = numberOfTrees * 5; // Tránh vòng lặp vô tận
        int attempts = 0;

        while (spawnedCount < numberOfTrees && attempts < maxAttempts)
        {
            attempts++;

            // 1. Chọn vị trí X, Z ngẫu nhiên trong vùng Terrain
            float randomX = Random.Range(terrainPos.x, terrainPos.x + terrainSize.x);
            float randomZ = Random.Range(terrainPos.y, terrainPos.z + terrainSize.z);

            // 2. Bắn Raycast từ trên trời xuống để lấy chính xác độ cao Y của Terrain
            Vector3 rayOrigin = new Vector3(randomX, terrainPos.y + terrainSize.y + 100f, randomZ);

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, Mathf.Infinity, groundLayer))
            {
                // Kiểm tra xem góc dốc của mặt đất có quá dốc không (không spawn trên vách đá)
                if (Vector3.Angle(hit.normal, Vector3.up) < 30f)
                {
                    // 3. Chọn ngẫu nhiên 1 loại cây trong danh sách
                    GameObject selectedTree = treePrefabs[Random.Range(0, treePrefabs.Length)];

                    // 4. Xoay cây ngẫu nhiên quanh trục Y cho tự nhiên
                    Quaternion randomRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

                    // 5. Sinh cây ra Scene
                    Instantiate(selectedTree, hit.point, randomRotation, transform);

                    spawnedCount++;
                }
            }
        }
    }
}