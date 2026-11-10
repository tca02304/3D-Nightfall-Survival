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
    public Transform requirementsContainer; // Chứa text/icon nguyên liệu (danh sách cuộn hoặc cũ nếu có)
    public GameObject requirementTextPrefab;
    public Button craftButton;
    public TextMeshProUGUI craftMessageText; // Hiển thị lỗi (thiếu đồ, đầy túi)

    [Header("Fixed Ingredients Slots (2 Slots)")]
    public Image ingredientImage_1;
    public TextMeshProUGUI ingredientAmountText_1;
    public Image ingredientImage_2;
    public TextMeshProUGUI ingredientAmountText_2;

    [Header("Workbench 3x3 Grid & Result")]
    public Transform ingredientsGrid; // Transform của IngredientsGrid
    public Image[] ingredientSlots;   // 9 ô nguyên liệu (Slot_1 -> Slot_9)
    public Image resultSlot;          // Ô kết quả ResultSlot
    public Image resultProductIcon;   // Icon sản phẩm bên trong ResultSlot
    public TextMeshProUGUI resultAmountText; // Số lượng sản phẩm đầu ra

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

        if (craftButton != null)
            craftButton.onClick.AddListener(OnCraftButtonClicked);

        PopulateRecipeList();
    }

    private void OnDisable()
    {
        // 3. Unsubscribe (Hủy đăng ký)
        GameEvents.OnInventoryUpdated -= (list1, list2) => RefreshRecipeDetails();
        GameEvents.OnCraftResult -= HandleCraftResult;

        if (craftButton != null)
            craftButton.onClick.RemoveListener(OnCraftButtonClicked);
    }

    // --- HIỂN THỊ DANH SÁCH ---
    private void PopulateRecipeList()
    {
        if (recipeGrid == null || recipePrefab == null) return;

        // Xóa danh sách cũ
        foreach (Transform child in recipeGrid) Destroy(child.gameObject);

        // Tạo các ô Recipe mới
        if (availableRecipes != null)
        {
            foreach (var recipe in availableRecipes)
            {
                GameObject slotObj = Instantiate(recipePrefab, recipeGrid);
                UiRecipeSlot slotUI = slotObj.GetComponent<UiRecipeSlot>();

                // Khởi tạo slot và đăng ký callback khi slot được click
                if (slotUI != null)
                    slotUI.Initialize(recipe, SelectRecipe);
            }
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
            if (craftButton != null) craftButton.interactable = false;
            if (resultProductIcon != null) resultProductIcon.gameObject.SetActive(false);
            if (resultAmountText != null) resultAmountText.text = "";
            return;
        }

        if (productNameText != null)
            productNameText.text = currentSelectedRecipe.recipeName;
        if (craftMessageText != null)
            craftMessageText.text = "";

        // Cập nhật ô Kết Quả (ResultSlot)
        if (resultProductIcon != null)
        {
            resultProductIcon.gameObject.SetActive(true);
            resultProductIcon.sprite = currentSelectedRecipe.productIcon;
        }
        if (resultAmountText != null)
        {
            resultAmountText.text = currentSelectedRecipe.productAmount > 1 ? currentSelectedRecipe.productAmount.ToString() : "";
        }

        // Cập nhật 2 ô nguyên liệu cố định nếu có gán
        if (currentSelectedRecipe.requirements != null)
        {
            if (currentSelectedRecipe.requirements.Count > 0)
            {
                var req1 = currentSelectedRecipe.requirements[0];
                int count1 = GetItemAmountInInventory(req1.resourceID);
                if (ingredientAmountText_1 != null)
                {
                    ingredientAmountText_1.text = $"{count1}/{req1.requiredAmount}";
                    ingredientAmountText_1.color = count1 >= req1.requiredAmount ? Color.white : Color.red;
                }
            }
            if (currentSelectedRecipe.requirements.Count > 1)
            {
                var req2 = currentSelectedRecipe.requirements[1];
                int count2 = GetItemAmountInInventory(req2.resourceID);
                if (ingredientAmountText_2 != null)
                {
                    ingredientAmountText_2.text = $"{count2}/{req2.requiredAmount}";
                    ingredientAmountText_2.color = count2 >= req2.requiredAmount ? Color.white : Color.red;
                }
            }
        }

        // Cập nhật Lưới Nguyên Liệu 3x3 (nếu có các ô ingredientSlots)
        if (ingredientSlots != null && ingredientSlots.Length > 0)
        {
            // Reset các ô về trống trước
            for (int i = 0; i < ingredientSlots.Length; i++)
            {
                if (ingredientSlots[i] != null)
                {
                    Transform iconTrans = ingredientSlots[i].transform.Find("Icon");
                    if (iconTrans != null)
                        iconTrans.gameObject.SetActive(false);
                }
            }

            // Gán các nguyên liệu cần thiết vào các ô
            if (currentSelectedRecipe.requirements != null)
            {
                for (int i = 0; i < currentSelectedRecipe.requirements.Count && i < ingredientSlots.Length; i++)
                {
                    var req = currentSelectedRecipe.requirements[i];
                    if (ingredientSlots[i] != null)
                    {
                        Transform iconTrans = ingredientSlots[i].transform.Find("Icon");
                        if (iconTrans != null)
                        {
                            iconTrans.gameObject.SetActive(true);
                        }
                    }
                }
            }
        }

        // Xóa danh sách nguyên liệu dạng text cũ (nếu dùng requirementsContainer)
        if (requirementsContainer != null && requirementTextPrefab != null)
        {
            foreach (Transform child in requirementsContainer) Destroy(child.gameObject);
        }

        bool canCraft = true;

        if (currentSelectedRecipe.requirements != null)
        {
            foreach (var req in currentSelectedRecipe.requirements)
            {
                int currentAmount = GetItemAmountInInventory(req.resourceID);

                if (requirementsContainer != null && requirementTextPrefab != null)
                {
                    GameObject reqObj = Instantiate(requirementTextPrefab, requirementsContainer);
                    TextMeshProUGUI reqText = reqObj.GetComponent<TextMeshProUGUI>();

                    if (reqText != null)
                    {
                        reqText.text = $"{req.resourceName} - {req.requiredAmount} (Có: {currentAmount})";

                        if (currentAmount < req.requiredAmount)
                        {
                            reqText.color = Color.red;
                        }
                        else
                        {
                            reqText.color = Color.white;
                        }
                    }
                }

                if (currentAmount < req.requiredAmount)
                {
                    canCraft = false;
                }
            }
        }

        // Cập nhật trạng thái nút Craft
        if (craftButton != null)
            craftButton.interactable = canCraft && !isCrafting;
    }

    // --- XỬ LÝ NHẤN NÚT CRAFT ---
    private void OnCraftButtonClicked()
    {
        if (currentSelectedRecipe == null || isCrafting) return;

        isCrafting = true; // Khóa nút để chống spam click liên tục
        if (craftButton != null) craftButton.interactable = false;
        if (craftMessageText != null) craftMessageText.text = "Crafting...";

        // Gửi tín hiệu yêu cầu chế tạo cho hệ thống xử lý
        GameEvents.OnTryCraftItem?.Invoke(currentSelectedRecipe.recipeID);
    }

    // --- NHẬN KẾT QUẢ TỪ HỆ THỐNG ---
    private void HandleCraftResult(bool success, string message)
    {
        isCrafting = false; // Mở khóa

        if (craftMessageText != null)
        {
            if (success)
            {
                craftMessageText.color = Color.green;
                craftMessageText.text = "Crafted successfully!";
            }
            else
            {
                craftMessageText.color = Color.red;
                craftMessageText.text = message; // "Không đủ chỗ trống", "Thiếu nguyên liệu"...
                RefreshRecipeDetails(); // Mở lại nút nếu cần
            }
        }
    }

    // (Hàm giả lập) Liên kết với Backend Inventory để lấy số lượng
    private int GetItemAmountInInventory(string itemID)
    {
        // ... Code gọi backend team bạn (ví dụ: InventoryManager.Instance.GetItemCount(itemID)) ...
        return 10; // Trả về số lượng giả lập
    }
}
