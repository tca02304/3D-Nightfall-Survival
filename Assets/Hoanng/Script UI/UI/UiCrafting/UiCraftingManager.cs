using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UiCraftingManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject recipePrefab; // Prefab của một ô Recipe
    public Transform recipeGrid;    // Lưới chứa các Recipe

    [Header("Tooltip / Detail Panel")]
    public TextMeshProUGUI productNameText;
    public Transform requirementsContainer; // Chứa text/icon nguyên liệu
    public GameObject requirementTextPrefab;
    public Button craftButton;
    public TextMeshProUGUI craftMessageText; // Hiển thị lỗi (thiếu đồ, đầy túi)

    private CraftingRecipe currentSelectedRecipe;

    // Biến chặn thao tác spam click (debounce)
    private bool isCrafting = false;

    // Danh sách công thức (Giả lập, thực tế lấy từ hệ thống/database)
    public List<CraftingRecipe> availableRecipes;

    private void OnEnable()
    {
        // 1. Subscribe (Đăng ký)
        GameEvents.OnInventoryUpdated += (list1, list2) => RefreshRecipeDetails();
        GameEvents.OnCraftResult += HandleCraftResult;

        craftButton.onClick.AddListener(OnCraftButtonClicked);

        PopulateRecipeList();
    }

    private void OnDisable()
    {
        // 3. Unsubscribe (Hủy đăng ký)[cite: 15]
        GameEvents.OnInventoryUpdated -= (list1, list2) => RefreshRecipeDetails();
        GameEvents.OnCraftResult -= HandleCraftResult;

        craftButton.onClick.RemoveListener(OnCraftButtonClicked);
    }

    // --- HIỂN THỊ DANH SÁCH ---
    private void PopulateRecipeList()
    {
        // Xóa danh sách cũ
        foreach (Transform child in recipeGrid) Destroy(child.gameObject);

        // Tạo các ô Recipe mới
        foreach (var recipe in availableRecipes)
        {
            GameObject slotObj = Instantiate(recipePrefab, recipeGrid);
            UiRecipeSlot slotUI = slotObj.GetComponent<UiRecipeSlot>();

            // Khởi tạo slot và đăng ký callback khi slot được click
            slotUI.Initialize(recipe, SelectRecipe);
        }
    }

    // --- CHỌN CÔNG THỨC & HIỂN THỊ CHI TIẾT ---
    public void SelectRecipe(CraftingRecipe recipe)
    {
        currentSelectedRecipe = recipe;
        RefreshRecipeDetails();
    }

    private void RefreshRecipeDetails()
    {
        if (currentSelectedRecipe == null)
        {
            craftButton.interactable = false;
            return;
        }

        productNameText.text = currentSelectedRecipe.recipeName;
        craftMessageText.text = "";

        // Xóa danh sách nguyên liệu cũ
        foreach (Transform child in requirementsContainer) Destroy(child.gameObject);

        bool canCraft = true;

        // Hiển thị từng nguyên liệu yêu cầu
        foreach (var req in currentSelectedRecipe.requirements)
        {
            GameObject reqObj = Instantiate(requirementTextPrefab, requirementsContainer);
            TextMeshProUGUI reqText = reqObj.GetComponent<TextMeshProUGUI>();

            // Giả sử có hàm kiểm tra số lượng item trong inventory từ backend
            int currentAmount = GetItemAmountInInventory(req.resourceID);

            reqText.text = $"{req.resourceName} - {req.requiredAmount} (Có: {currentAmount})";

            // Phân biệt trạng thái đủ/thiếu nguyên liệu bằng màu sắc
            if (currentAmount < req.requiredAmount)
            {
                reqText.color = Color.red; // Thiếu màu đỏ
                canCraft = false;
            }
            else
            {
                reqText.color = Color.white; // Đủ màu trắng
            }
        }

        // Cập nhật trạng thái nút Craft
        craftButton.interactable = canCraft && !isCrafting;
    }

    // --- XỬ LÝ NHẤN NÚT CRAFT ---
    private void OnCraftButtonClicked()
    {
        if (currentSelectedRecipe == null || isCrafting) return;

        isCrafting = true; // Khóa nút để chống spam click liên tục
        craftButton.interactable = false;
        craftMessageText.text = "Crafting...";

        // Gửi tín hiệu yêu cầu chế tạo cho hệ thống xử lý[cite: 15]
        GameEvents.OnTryCraftItem?.Invoke(currentSelectedRecipe.recipeID);
    }

    // --- NHẬN KẾT QUẢ TỪ HỆ THỐNG ---
    private void HandleCraftResult(bool success, string message)
    {
        isCrafting = false; // Mở khóa

        if (success)
        {
            craftMessageText.color = Color.green;
            craftMessageText.text = "Crafted successfully!";
            // Note: OnInventoryUpdated sẽ tự động chạy (do hệ thống Inventory phát) và làm mới lại UI.
        }
        else
        {
            craftMessageText.color = Color.red;
            craftMessageText.text = message; // "Không đủ chỗ trống", "Thiếu nguyên liệu"...
            RefreshRecipeDetails(); // Mở lại nút nếu cần
        }
    }

    // (Hàm giả lập) Liên kết với Backend Inventory để lấy số lượng
    private int GetItemAmountInInventory(string itemID)
    {
        // ... Code gọi backend team bạn (ví dụ: InventoryManager.Instance.GetItemCount(itemID)) ...
        return 10; // Trả về số lượng giả lập
    }
}