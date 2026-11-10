using UnityEngine;

public class RandomMapManager : MonoBehaviour
{
    [Header("Cài đặt Map")]
    public GameObject[] mapPrefabs;

    [Header("Cài đặt Player")]
    public GameObject playerPrefab; // Kéo Prefab nhân vật của team vào đây

    void Start()
    {
        SetupGame();
    }

    void SetupGame()
    {
        if (mapPrefabs.Length == 0) return;

        // 1. Sinh Map ngẫu nhiên
        int randomIndex = Random.Range(0, mapPrefabs.Length);
        GameObject spawnedMap = Instantiate(mapPrefabs[randomIndex], Vector3.zero, Quaternion.identity);

        // 2. Tìm điểm xuất phát của Player bên trong Map vừa sinh ra
        GameObject spawnPoint = GameObject.FindGameObjectWithTag("PlayerSpawnPoint");

        // 3. Sinh Player tại vị trí đã tìm thấy
        if (spawnPoint != null && playerPrefab != null)
        {
            Instantiate(playerPrefab, spawnPoint.transform.position, spawnPoint.transform.rotation);
            Debug.Log("Đã tạo Map và thả Player thành công!");
        }
        else
        {
            Debug.LogError("Lỗi: Quên gắn Tag PlayerSpawnPoint trong Map hoặc chưa kéo Player Prefab!");
        }
    }
}