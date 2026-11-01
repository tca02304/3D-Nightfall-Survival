using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUI : MonoBehaviour
{
    [Header("1. Thành phần Dữ liệu (Team)")]
    [SerializeField]private Image iconImage;
    public TextMeshProUGUI amountText;

    [Header("2. Thành phần Tương tác (Đổi màu)")]
    public Image backgroundImage;     // Kéo ảnh nền của ô vào đây (nếu dùng)
    [SerializeField]private Button slotButton;         // Kéo component Button vào đây
    public Color normalColor = Color.white;
    public Color selectedColor = new Color(0.8f, 0.8f, 0.8f, 0.5f); // Màu mờ khi được chọn

    private int slotIndex;

    // --- PHẦN 1: TƯƠNG TÁC CLICK & ĐỔI MÀU ---

    // Được gọi bởi UiInventory lúc bắt đầu game để đánh số thứ tự ô
    public void InitializeSlot(int index)
    {
        slotIndex = index;
        if (backgroundImage != null)
            backgroundImage.color = normalColor;
    }

    private void OnEnable()
    {
        // Lắng nghe sự kiện click chuột[cite: 5]
        if (slotButton != null)
            slotButton.onClick.AddListener(OnSlotClicked);
    }

    private void OnDisable()
    {
        // Hủy lắng nghe để tránh lỗi Memory Leak[cite: 5]
        if (slotButton != null)
            slotButton.onClick.RemoveListener(OnSlotClicked);
    }

    private void OnSlotClicked()
    {
        // Phát sự kiện ra ngoài khi ô bị click
        GameEvents.OnHotbarSlotSelected?.Invoke(slotIndex);
    }

    // Quản lý sẽ gọi hàm này để đổi màu trực tiếp
    public void SetHighlight(bool isSelected)
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = isSelected ? selectedColor : normalColor;
        }
    }

    // --- PHẦN 2: XỬ LÝ DỮ LIỆU TỪ ĐỒNG ĐỘI ---

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

            // Yêu cầu team thêm "public Sprite Icon;" vào InventorySlotData
            // iconImage.sprite = slot.Icon; 

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