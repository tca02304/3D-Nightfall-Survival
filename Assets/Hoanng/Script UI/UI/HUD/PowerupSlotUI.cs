using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PowerupSlotUI : MonoBehaviour
{
    public Image outlineImage; // Kéo viền ảnh (hiển thị độ hiếm: Trắng, Xanh, Tím, Vàng)
    public Image iconImage;    // Kéo ảnh hình kỹ năng/thuốc vào đây
    public TextMeshProUGUI stackText; // Kéo chữ hiển thị số cộng dồn vào (x2, x3)

    public void SetupSlot(Sprite icon, Color rarityColor, int amount)
    {
        iconImage.sprite = icon;
        outlineImage.color = rarityColor;
        UpdateAmount(amount);
    }

    public void UpdateAmount(int currentAmount)
    {
        // Nếu nhặt được 1 cái thì không hiện số, từ 2 cái trở lên mới hiện x2, x3...
        if (currentAmount <= 1)
        {
            stackText.gameObject.SetActive(false);
        }
        else
        {
            stackText.gameObject.SetActive(true);
            stackText.text = "x" + currentAmount.ToString();
        }
    }
}