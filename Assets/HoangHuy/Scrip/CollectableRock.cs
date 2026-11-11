using UnityEngine;

public class CollectableRock : MonoBehaviour
{
    [Header("Cấu hình tương tác")]
    public float interactDistance = 3f; // Khoảng cách tối đa để tương tác
    public string itemGivenName = "Stone"; // Tên vật phẩm nhận được khi đập/nhặt đá
    public int quantity = 1;            // Số lượng nhận được

    void Update()
    {
        // Khi người chơi ấn phím E
        if (Input.GetKeyDown(KeyCode.E))
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                // Kiểm tra khoảng cách giữa Player và cục đá
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance <= interactDistance)
                {
                    Collect();
                }
            }
        }
    }

    void Collect()
    {
        // 1. Logic cộng vật phẩm vào túi đồ của Player (Bạn có thể gọi hàm Inventory ở đây sau)
        Debug.Log($"Đã nhặt thành công: {quantity} {itemGivenName}!");

        // 2. Hủy/Xóa cục đá khỏi Scene (biến mất)
        Destroy(gameObject);
    }
}