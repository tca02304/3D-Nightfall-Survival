using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A 3x3 VIRTUAL crafting plan: items stay in Inventory until a successful Craft.
/// No item duplication or loss from entering/leaving the grid; no ScriptableObjects.
/// Local/demo only: move TryCraft authority to Fusion State Authority in multiplayer.
/// </summary>
public sealed class GridCraftingController : MonoBehaviour
{
    [SerializeField] private TextAsset gridRecipesCsv;
    [SerializeField] private TextAsset itemCodeIdsCsv;
    [SerializeField] private string currentStation = "BasicWorkbench";
    [SerializeField] private bool requireStation = true;

    private readonly GridIngredient[] grid = new GridIngredient[9];
    private readonly List<GridRecipe> recipes = new List<GridRecipe>();
    private bool loaded;

    public event Action OnGridChanged;
    public GridRecipe MatchedRecipe { get; private set; }
    public IReadOnlyList<GridRecipe> Recipes => recipes;
    public string CurrentStation => currentStation;
    public GridIngredient GetCell(int index) => index >= 0 && index < 9 ? grid[index] : default;

    private void Awake() => ReloadCatalog();

    [ContextMenu("Crafting / Reload CSV")]
    public void ReloadCatalog()
    {
        loaded = false;
        recipes.Clear();
        try
        {
            recipes.AddRange(GridRecipeCatalog.Parse(gridRecipesCsv, itemCodeIdsCsv));
            loaded = true;
            Refresh();
            Debug.Log($"[GridCrafting] Loaded {recipes.Count} data-only shaped recipes.", this);
        }
        catch (Exception e) { Debug.LogError("[GridCrafting] " + e.Message, this); }
    }

    public void SetStation(string station)
    {
        currentStation = station;
        Refresh();
    }

    // Inventory slot is NOT consumed here; player only previews an arrangement.
    public bool SetCellFromInventory(int gridIndex, int inventorySlotIndex, int amount)
    {
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null || inventorySlotIndex < 0 || inventorySlotIndex >= inv.Slots.Count) return false;
        var slot = inv.Slots[inventorySlotIndex];
        if (slot == null || slot.isEmpty()) return false;
        return SetCell(gridIndex, slot.ItemId, amount);
    }

    public bool SetCell(int index, int itemId, int amount)
    {
        if (index < 0 || index >= grid.Length || itemId <= 0 || amount != 1) return false;
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null || inv.GetItemData(itemId) == null) return false;
        grid[index] = new GridIngredient(itemId, amount);
        Refresh();
        return true;
    }

    public void ClearCell(int index)
    {
        if (index < 0 || index >= grid.Length) return;
        grid[index] = default;
        Refresh();
    }

    public void ClearGrid()
    {
        Array.Clear(grid, 0, grid.Length);
        Refresh();
    }

    public bool CanCraft(out string reason)
    {
        if (!loaded) { reason = "Recipe catalog not loaded"; return false; }
        if (MatchedRecipe == null) { reason = "No recipe matches this 3x3 grid"; return false; }
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null) { reason = "InventoryManager missing"; return false; }
        if (!inv.CursorSlot.isEmpty()) { reason = "Place cursor-held item back first"; return false; }
        ItemData output = inv.GetItemData(MatchedRecipe.outputId);
        if (output == null) { reason = "Output item not loaded from backend"; return false; }
        var needs = Requirements(MatchedRecipe);
        foreach (var need in needs)
        {
            if (inv.CountItem(need.Key) < need.Value)
            {
                reason = $"Insufficient item {need.Key}: {inv.CountItem(need.Key)}/{need.Value}";
                return false;
            }
        }
        if (!inv.CanExchangeItemsForItem(needs, output, MatchedRecipe.outputAmount))
        {
            reason = "No room for output after consuming ingredients";
            return false;
        }
        reason = "Ready";
        return true;
    }

    public bool TryCraft(out string message)
    {
        if (!CanCraft(out message)) return false;
        var recipe = MatchedRecipe;
        InventoryManager inv = InventoryManager.Instance;
        ItemData output = inv.GetItemData(recipe.outputId);
        if (!inv.TryExchangeItemsForItem(Requirements(recipe), output, recipe.outputAmount))
        {
            message = "Inventory changed; crafting rejected without consuming items";
            return false;
        }
        // This grid represents a reusable plan, NOT physically transferred stacks.
        // Keep it in place so player can craft again while materials last.
        message = $"Crafted {recipe.displayName} x{recipe.outputAmount}";
        OnGridChanged?.Invoke();
        return true;
    }

    public bool TryTakeResult(out string message)
    {
        if(MatchedRecipe == null)
        {
            message = "No matching recipt";
            return false;
        }

        return TryCraft(out message);
    }

    private static Dictionary<int, int> Requirements(GridRecipe recipe)
    {
        var result = new Dictionary<int, int>();
        foreach (var ingredient in recipe.cells)
        {
            if (ingredient.itemId <= 0 || ingredient.amount <= 0) continue;
            if (!result.ContainsKey(ingredient.itemId)) result.Add(ingredient.itemId, 0);
            checked { result[ingredient.itemId] += ingredient.amount; }
        }
        return result;
    }

    private void Refresh()
    {
        MatchedRecipe = null;
        if (loaded)
            foreach (GridRecipe recipe in recipes)
            {
                if (requireStation && !string.Equals(recipe.station, currentStation,
                        StringComparison.OrdinalIgnoreCase)) continue;
                if (!GridRecipeCatalog.Matches(grid, recipe.cells)) continue;
                MatchedRecipe = recipe;
                break;
            }
        OnGridChanged?.Invoke();
    }
}
