using System;
using System.Collections.Generic;
using UnityEngine;

public class CraftingManager : MonoBehaviour
{
    public static CraftingManager Instance;

    [SerializeField]
    private List<RecipeData> recipes = new();

    public IReadOnlyList<RecipeData> Recipes => recipes;

    public event Action<RecipeData> OnCrafted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool CanCraft(RecipeData recipe, out string reason)
    {
        reason = string.Empty;

        if (recipe == null || !recipe.isActive)
        {
            reason = "Công thức không hợp lệ hoặc đã bị tắt.";
            return false;
        }

        InventoryManager inventory = InventoryManager.Instance;

        if (inventory == null || ItemDatabase.Instance == null)
        {
            reason = "Inventory hoặc Item Database chưa sẵn sàng.";
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

        TryBuildRequirements(
            recipe,
            out Dictionary<int, int> requirements,
            out message
        );

        ItemData outputItem = inventory.GetItemData(
            recipe.outputItemId
        );

        if (!inventory.TryExchangeItemsForItem(
            requirements,
            outputItem,
            recipe.outputAmount))
        {
            message = "Chế tạo thất bại. Dữ liệu Inventory chưa được thay đổi.";
            return false;
        }

        message =
            $"Chế tạo thành công: {outputItem.name} x{recipe.outputAmount}.";

        OnCrafted?.Invoke(recipe);

        return true;
    }

    private bool TryBuildRequirements(
        RecipeData recipe,
        out Dictionary<int, int> requirements,
        out string reason)
    {
        requirements = new Dictionary<int, int>();
        reason = string.Empty;

        if (recipe.ingredients == null || recipe.ingredients.Count == 0)
        {
            reason = "Công thức chưa có nguyên liệu.";
            return false;
        }

        foreach (RecipeIngredient ingredient in recipe.ingredients)
        {
            if (ingredient == null
                || ingredient.itemId <= 0
                || ingredient.amount <= 0)
            {
                reason = "Dữ liệu nguyên liệu không hợp lệ.";
                return false;
            }

            // Gộp nguyên liệu nếu cùng một ID xuất hiện nhiều lần.
            if (requirements.TryGetValue(
                ingredient.itemId,
                out int currentAmount))
            {
                requirements[ingredient.itemId] =
                    currentAmount + ingredient.amount;
            }
            else
            {
                requirements.Add(
                    ingredient.itemId,
                    ingredient.amount
                );
            }
        }

        return true;
    }
}