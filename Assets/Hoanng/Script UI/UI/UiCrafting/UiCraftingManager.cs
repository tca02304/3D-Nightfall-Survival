using UnityEngine;
using UnityEngine.UI;

public class UiCraftingManager : UiBase
{
    [Header("Tab Group Buttons")]
    public Button inventoryTabButton; // Nút chuyển sang Túi đồ
    public Button recipeTabButton;  // Nút chuyển sang cong thuc Chế tạo
    public Button closeButton;        // Nút tắt (X)

    private void OnEnable()
    {
        // Đăng ký sự kiện click
        if (inventoryTabButton != null) inventoryTabButton.onClick.AddListener(RequestInventory);
        if (recipeTabButton != null) recipeTabButton.onClick.AddListener(RequestCrafting);
        if (closeButton != null) closeButton.onClick.AddListener(CloseUI);
    }

    private void OnDisable()
    {
        // Hủy đăng ký
        if (inventoryTabButton != null) inventoryTabButton.onClick.RemoveAllListeners();
        if (recipeTabButton != null) recipeTabButton.onClick.RemoveAllListeners();
        if (closeButton != null) closeButton.onClick.RemoveAllListeners();
    }

    // --- CÁC HÀM PHÁT TÍN HIỆU ---

    private void RequestInventory()
    {
        // Báo cho UIManager biết người chơi muốn mở lại Túi đồ
        GameEvents.OnRequestInventoryUI?.Invoke();
    }

    private void RequestCrafting()
    {
        // Báo cho UIManager mở bảng Chế tạo (có thể dùng để refresh lại bảng)
        GameEvents.OnRequestCraftingUI?.Invoke();
    }

    private void CloseUI()
    {
        // Tắt bảng hiện tại
        Hide();
    }
}