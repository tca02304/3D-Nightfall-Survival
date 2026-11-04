using System;
using System.Collections.Generic;
using UnityEngine;

public class CraftingManager : MonoBehaviour
{
    public static CraftingManager Instance;

    [Header("Recipes")]
    [SerializeField] private List<RecipeData> recipes = new();

    [Header("Crafting Station")]
    [SerializeField] private CraftingStationType activeStation = CraftingStationType.Hand;

    [SerializeField] private bool enforceCraftingStation = false;

    public IReadOnlyList<RecipeData> Recipes => recipes;

    public CraftingStationType ActiveStation => activeStation;

    public event Action<RecipeData> OnCrafted;
    public event Action OnRecipesChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        ValidateRecipeList();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void SetActiveStation(CraftingStationType station)
    {
        if (activeStation == station)
            return;

        activeStation = station;
        OnRecipesChanged?.Invoke();
    }

    public void SetStationRequirementEnable(bool enable)
    {
        enforceCraftingStation = enable;
        OnRecipesChanged?.Invoke();
    }

    public RecipeData GetRecipeById(string recipeId)
    {
        if (string.IsNullOrWhiteSpace(recipeId))
            return null;

        foreach(RecipeData recipe in recipes)
        {
            if(recipe != null &&
                string.Equals(recipe.recipeId, recipeId, StringComparison.Ordinal))
            {
                return recipe;
            }
        }

        return null;
    }

    public bool CanCraft(RecipeData recipe, out string reason)
    {
        reason = string.Empty;

        if (recipe == null || !recipe.isActive)
        {
            reason = "Công thức không hợp lệ hoặc đã bị tắt.";
            return false;
        }

        if(enforceCraftingStation &&
            recipe.requiredStation != activeStation)
        {
            reason = $"Cần sử dụng trạm: {recipe.requiredStation}";
            return false;
        }

        InventoryManager inventory = InventoryManager.Instance;
        ItemDatabase database = ItemDatabase.Instance;

        if (inventory == null || database == null)
        {
            reason = "Inventory hoặc Item Database chưa sẵn sàng.";
            return false;
        }

        if(inventory.CursorSlot == null ||
            !inventory.CursorSlot.isEmpty())
        {
            reason = "Hãy đặt Item trên chuột xuống trước khi Craft";
            return false;
        }

        if (recipe.outputItemId <= 0 || recipe.outputAmount <= 0)
        {
            reason = "Dữ liệu sản phẩm đầu ra không hợp lệ.";
            return false;
        }

        ItemData outputItem = inventory.GetItemData(
            recipe.outputItemId
        );

        if (outputItem == null || !outputItem.isActive)
        {
            reason = "Không tìm thấy sản phẩm trong Item Database.";
            return false;
        }

        if (!TryBuildRequirements(
            recipe,
            out Dictionary<int, int> requirements,
            out reason))
        {
            return false;
        }

        // Kiểm tra từng loại nguyên liệu.
        foreach (KeyValuePair<int, int> requirement in requirements)
        {
            ItemData ingredientItem = inventory.GetItemData(
                requirement.Key
            );

            if (ingredientItem == null || !ingredientItem.isActive)
            {
                reason = $"Nguyên liệu ID {requirement.Key} không hợp lệ.";
                return false;
            }

            int currentAmount = inventory.CountItem(
                requirement.Key
            );

            if (currentAmount < requirement.Value)
            {
                reason =
                    $"Thiếu {ingredientItem.name}: " +
                    $"cần {requirement.Value}, " +
                    $"hiện có {currentAmount}.";

                return false;
            }
        }

        // Kiểm tra chỗ chứa sau khi trừ nguyên liệu.
        if (!inventory.CanExchangeItemsForItem(
            requirements,
            outputItem,
            recipe.outputAmount))
        {
            reason = "Inventory không đủ chỗ chứa sản phẩm.";
            return false;
        }

        return true;
    }

    public bool TryCraft(RecipeData recipe, out string message)
    {
        if (!CanCraft(recipe, out message))
            return false;

        InventoryManager inventory = InventoryManager.Instance;

        if (!TryBuildRequirements(
            recipe,
            out Dictionary<int, int> requirements,
            out message))
        {
            return false;
        }

        ItemData outputItem = inventory.GetItemData(
            recipe.outputItemId);

        // Inventory xử lý trừ nguyên liệu,
        // thêm sản phẩm và rollback nếu thất bại.
        bool success = inventory.TryExchangeItemsForItem(
            requirements,
            outputItem,
            recipe.outputAmount);

        if (!success)
        {
            message =
                "Chế tạo thất bại. Inventory không được thay đổi.";
            return false;
        }

        message =
            $"Chế tạo thành công: " +
            $"{outputItem.name} x{recipe.outputAmount}.";

        OnCrafted?.Invoke(recipe);

        Debug.Log("[Crafting] " + message);

        return true;
    }

    public bool TryCraftById(string recipeId, out string message)
    {
        RecipeData recipe = GetRecipeById(recipeId);

        if (recipe == null)
        {
            message = $"Không tìm thấy Recipe: {recipeId}";
            return false;
        }

        return TryCraft(recipe, out message);
    }

    private bool TryBuildRequirements(
        RecipeData recipe,
        out Dictionary<int, int> requirements,
        out string reason)
    {
        requirements = new Dictionary<int, int>();
        reason = string.Empty;

        if (recipe.ingredients == null ||
            recipe.ingredients.Count == 0)
        {
            reason = "Công thức chưa có nguyên liệu.";
            return false;
        }

        foreach (RecipeIngredient ingredient in recipe.ingredients)
        {
            if (ingredient == null ||
                ingredient.itemId <= 0 ||
                ingredient.amount <= 0)
            {
                reason = "Dữ liệu nguyên liệu không hợp lệ.";
                return false;
            }

            int current = 0;
            requirements.TryGetValue(ingredient.itemId, out current);

            // Tránh tràn int khi gộp các nguyên liệu trùng ID.
            if (current > int.MaxValue - ingredient.amount)
            {
                reason = "Số lượng nguyên liệu vượt giới hạn.";
                return false;
            }

            requirements[ingredient.itemId] =
                current + ingredient.amount;
        }

        return true;
    }

    private void ValidateRecipeList()
    {
        HashSet<string> recipeIds = new();

        foreach (RecipeData recipe in recipes)
        {
            if (recipe == null)
            {
                Debug.LogWarning(
                    "[Crafting] Có Recipe Asset bị thiếu.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(recipe.recipeId))
            {
                Debug.LogWarning(
                    $"[Crafting] Recipe {recipe.name} thiếu recipeId.");
                continue;
            }

            if (!recipeIds.Add(recipe.recipeId))
            {
                Debug.LogWarning(
                    $"[Crafting] Trùng recipeId: {recipe.recipeId}");
            }
        }
    }
}