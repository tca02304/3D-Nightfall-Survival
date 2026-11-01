using UnityEngine;

public class TreeSpawner : MonoBehaviour
{
    [Header("Terrain & Prefabs")]
    public Terrain terrains;
    public GameObject[] treePrefabss; 
    [Header("Spawn Settings")]
    public int numberOfTreess = 100;    
    public LayerMask groundLayers;     

    void Start()
    {
        SpawnTreess();
    }

    void SpawnTreess()
    {
        if (terrains == null || treePrefabss.Length == 0) return;

        Vector3 terrainSize = terrains.terrainData.size;
        Vector3 terrainPos = terrains.transform.position;

        int spawnedCount = 0;
        int maxAttempts = numberOfTreess * 5; // Tránh vòng lặp vô tận
        int attempts = 0;

        while (spawnedCount < numberOfTreess && attempts < maxAttempts)
        {
            attempts++;

            // 1. Chọn vị trí X, Z ngẫu nhiên trong vùng Terrain
            float randomX = Random.Range(terrainPos.x, terrainPos.x + terrainSize.x);
            float randomZ = Random.Range(terrainPos.y, terrainPos.z + terrainSize.z);

            // 2. Bắn Raycast từ trên trời xuống để lấy chính xác độ cao Y của Terrain
            Vector3 rayOrigin = new Vector3(randomX, terrainPos.y + terrainSize.y + 100f, randomZ);

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, Mathf.Infinity, groundLayers))
            {
                // Kiểm tra xem góc dốc của mặt đất có quá dốc không (không spawn trên vách đá)
                if (Vector3.Angle(hit.normal, Vector3.up) < 30f)
                {
                    // 3. Chọn ngẫu nhiên 1 loại cây trong danh sách
                    GameObject selectedTree = treePrefabss[Random.Range(0, treePrefabss.Length)];

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