using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Cấu hình Vùng Spawn Player")]
    public Terrain terrain;                  // Kéo Terrain của bạn vào đây
    public Transform centerPoint;            // Tâm khu vực spawn (Nếu trống sẽ lấy vị trí GameObject này)
    public float spawnRadius = 20f;          // Bán kính vùng xuất hiện quanh tâm
    public float spawnOffsetY = 1.0f;        // Nâng cao hơn mặt đất một chút để Player không bị kẹt

    void Start()
    {
        SpawnPlayer();
    }

    void SpawnPlayer()
    {
        // Tìm Player trong Scene (đảm bảo Player của bạn đã gắn Tag "Player")
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("Không tìm thấy GameObject có Tag là 'Player' trong Scene!");
            return;
        }

        if (terrain == null)
        {
            Debug.LogError("Chưa gán Terrain vào PlayerSpawner!");
            return;
        }

        // 1. Tạo tọa độ X, Z ngẫu nhiên hình tròn quanh tâm
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnCenter = centerPoint != null ? centerPoint.position : transform.position;
        float randomX = spawnCenter.x + randomCircle.x;
        float randomZ = spawnCenter.z + randomCircle.y;

        // 2. Bắn Raycast từ trên cao xuống để lấy đúng độ cao mặt đất của Terrain
        float rayOriginY = terrain.transform.position.y + terrain.terrainData.size.y + 100f;
        Vector3 rayOrigin = new Vector3(randomX, rayOriginY, randomZ);

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, Mathf.Infinity))
        {
            // Tính toán vị trí xuất hiện và cộng thêm độ cao bù trừ
            Vector3 spawnPosition = hit.point + new Vector3(0, spawnOffsetY, 0);

            // Tạm tắt CharacterController (nếu Player dùng nó) để dịch chuyển không bị lỗi
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            // Di chuyển Player đến vị trí mới
            player.transform.position = spawnPosition;

            // Bật lại CharacterController
            if (cc != null) cc.enabled = true;

            Debug.Log("Player đã được spawn thành công tại: " + spawnPosition);
        }
    }

    // Vẽ vòng tròn phạm vi spawn trong cửa sổ Scene để dễ quan sát
    private void OnDrawGizmosSelected()
    {
        Vector3 spawnCenter = centerPoint != null ? centerPoint.position : transform.position;
        Gizmos.color = Color.cyan; // Vòng tròn màu xanh ngọc biểu thị vùng spawn Player
        Gizmos.DrawWireSphere(spawnCenter, spawnRadius);
    }
}