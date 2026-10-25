using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HotbarSlotUI : MonoBehaviour
{
    public Image backgroundImage; // Kéo ảnh nền ô gỗ vào đây
    public Image iconImage;       // Kéo ảnh icon vật phẩm bên trong vào đây
    public TextMeshProUGUI amountText; // Kéo Text chứa số lượng vào đây

    public Color selectedColor = Color.yellow; // Màu viền khi đang cầm món này (sáng lên)
    public Color normalColor = Color.white;    // Màu bình thường (không chọn)

    // Hàm dùng để hiện vật phẩm lên ô
    public void SetItem(Sprite icon, int amount)
    {
        iconImage.sprite = icon;
        iconImage.enabled = true; // Bật hiển thị ảnh

        amountText.text = amount.ToString();
        amountText.gameObject.SetActive(true); // Bật số lượng
    }

    // Hàm làm trống ô khi dùng hết đồ
    public void ClearSlot()
    {
        iconImage.enabled = false;
        amountText.text = "";
        amountText.gameObject.SetActive(false);
    }

    // Hàm tự động đổi màu khi người chơi cuộn chuột / ấn phím số
    public void SetSelected(bool isSelected)
    {
        backgroundImage.color = isSelected ? selectedColor : normalColor;
    }
}