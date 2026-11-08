using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class CraftingTest : MonoBehaviour
{
    [SerializeField] private CraftingManager craftingManager;
    [SerializeField] private string recipeId = "wood_axe";
    [SerializeField] private int woodId = 1;
    [SerializeField] private int barkId = 34;
    [SerializeField] private int stoneId = 6;
    [SerializeField] private int woodAxeId = 36;

    [ContextMenu("TEST / Run 4 Crafting Cases")]
    public void RunTests()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[CraftingTest] Enter Play Mode first."); return; }
        var inv = InventoryManager.Instance;
        if (inv == null || craftingManager == null) { Debug.LogError("[CraftingTest] Missing manager references."); return; }

        var recipe = craftingManager.GetRecipeById(recipeId);
        var wood = inv.GetItemData(woodId);
        var bark = inv.GetItemData(barkId);
        var stone = inv.GetItemData(stoneId);
        var axe = inv.GetItemData(woodAxeId);
        if (recipe == null || wood == null || bark == null || stone == null || axe == null)
        { Debug.LogError("[CraftingTest] ItemAPI not ready or recipe/IDs invalid."); return; }
        if (inv.InventorySize < 3 || inv.Slots.Count < inv.InventorySize || wood.maxStack < 6 || bark.maxStack < 6)
        { Debug.LogError("[CraftingTest] InventorySize / stack settings invalid."); return; }

        // Only run against exactly Wood x5 + Bark x5 => Wood Axe x1.
        if (recipe.outputItemId != woodAxeId || recipe.outputAmount != 1 || recipe.ingredients == null)
        { Debug.LogError("[CraftingTest] Wrong Wood Axe recipe."); return; }
        var needs = new Dictionary<int, int>();
        foreach (var ing in recipe.ingredients)
        {
            if (!needs.ContainsKey(ing.itemId)) needs.Add(ing.itemId, 0);
            needs[ing.itemId] += ing.amount;
        }
        if (needs.Count != 2 || !needs.TryGetValue(woodId, out int w) || w != 5 ||
            !needs.TryGetValue(barkId, out int b) || b != 5)
        { Debug.LogError("[CraftingTest] Expected ingredients Wood x5 / Bark x5."); return; }

        var saved = Snapshot(inv.Slots);
        var savedCursor = Clone(inv.CursorSlot);
        int passed = 0;
        try
        {
            SetScenario(inv, wood, 5, bark, 5, stone, false);
            bool can = craftingManager.CanCraft(recipe, out _);
            bool crafted = craftingManager.TryCraft(recipe, out string reason);
            Check("1: enough ingredients", can && crafted && Counts(inv, 0, 0, 1), reason, ref passed);

            SetScenario(inv, wood, 5, bark, 4, stone, false);
            can = craftingManager.CanCraft(recipe, out _);
            crafted = craftingManager.TryCraft(recipe, out reason);
            Check("2: missing bark, unchanged", !can && !crafted && Counts(inv, 5, 4, 0), reason, ref passed);

            SetScenario(inv, wood, 6, bark, 6, stone, true);
            can = craftingManager.CanCraft(recipe, out _);
            crafted = craftingManager.TryCraft(recipe, out reason);
            Check("3: full and no slot freed", !can && !crafted && Counts(inv, 6, 6, 0), reason, ref passed);

            SetScenario(inv, wood, 5, bark, 5, stone, true);
            can = craftingManager.CanCraft(recipe, out _);
            crafted = craftingManager.TryCraft(recipe, out reason);
            Check("4: full but slots freed", can && crafted && Counts(inv, 0, 0, 1), reason, ref passed);
        }
        catch (Exception ex) { Debug.LogException(ex); }
        finally
        {
            for (int i = 0; i < saved.Count; i++) CopyInto(saved[i], inv.Slots[i]);
            CopyInto(savedCursor, inv.CursorSlot);
        }
        Debug.Log($"[CraftingTest] RESULT {passed}/4 PASS. Stop Play Mode after test.");
    }

    private bool Counts(InventoryManager inv, int wood, int bark, int axe) =>
        inv.CountItem(woodId) == wood && inv.CountItem(barkId) == bark && inv.CountItem(woodAxeId) == axe;

    private static void SetScenario(InventoryManager inv, ItemData wood, int wc,
        ItemData bark, int bc, ItemData filler, bool full)
    {
        foreach (var slot in inv.Slots) slot.Clear();
        inv.CursorSlot.Clear();
        WriteItem(inv.Slots[0], wood, wc);
        WriteItem(inv.Slots[1], bark, bc);
        if (full)
            for (int i = 2; i < inv.InventorySize; i++) WriteItem(inv.Slots[i], filler, filler.maxStack);
    }

    private static void WriteItem(InventorySlotData slot, ItemData item, int amount)
    {
        slot.ItemId = item.id;
        slot.ItemCode = item.itemCode;
        slot.ItemName = item.name;
        slot.Amount = amount;
        slot.MaxStack = item.maxStack;
    }

    private static InventorySlotData Clone(InventorySlotData slot) => new InventorySlotData
    {
        ItemId = slot.ItemId,
        ItemCode = slot.ItemCode,
        ItemName = slot.ItemName,
        Amount = slot.Amount,
        MaxStack = slot.MaxStack
    };

    private static List<InventorySlotData> Snapshot(List<InventorySlotData> slots)
    {
        var result = new List<InventorySlotData>();
        foreach (var slot in slots) result.Add(Clone(slot));
        return result;
    }

    private static void CopyInto(InventorySlotData from, InventorySlotData to)
    {
        to.ItemId = from.ItemId;
        to.ItemCode = from.ItemCode;
        to.ItemName = from.ItemName;
        to.Amount = from.Amount;
        to.MaxStack = from.MaxStack;
    }

    private static void Check(string test, bool ok, string reason,ref int passed)
    {
        if (ok)
        {
            passed++;
            Debug.Log($"[CraftingTest] PASS: {test}");
        }
        else
        {
            Debug.LogError($"[CraftingTest] FAIL: {test} | Reason: {reason}");
        }
    }
}