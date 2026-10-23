using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUI : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI amountText;

    // Nhận dữ liệu từ class của đồng đội
    public void UpdateSlot(InventorySlotData slot)
    {
        // 1. Kiểm tra nếu ô không có dữ liệu hoặc trống rỗng
        if (slot == null || slot.isEmpty())
        {
            iconImage.enabled = false;
            amountText.gameObject.SetActive(false);
            iconImage.sprite = null;
        }
        else
        {
            // 2. Nếu có đồ -> Bật hiển thị
            iconImage.enabled = true;

            // ⚠️ LƯU Ý QUAN TRỌNG ĐỂ BÀN VỚI TEAM:
            // Class của đồng đội hiện tại CHƯA có biến Sprite. 
            // Tạm thời bạn hãy gán một ảnh mặc định, sau đó yêu cầu đồng đội:
            // 1 là thêm "public Sprite Icon;" vào InventorySlotData.
            // 2 là viết cho bạn một hàm để từ "slot.ItemCode" lấy ra được cái Sprite.
            // iconImage.sprite = slot.Icon; // (Mở comment dòng này khi team đã thêm biến Icon)

            // 3. Xử lý số lượng (lấy từ slot.Amount của đồng đội)
            if (slot.Amount > 1)
            {
                amountText.gameObject.SetActive(true);
                amountText.text = slot.Amount.ToString();
            }
            else
            {
                amountText.gameObject.SetActive(false);
            }
        }
    }
}