using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class GridCraftingController : MonoBehaviour
{
    [SerializeField] private TextAsset gridRecipesCsv;
    [SerializeField] private TextAsset itemCodeIdsCsv;

    private readonly GridIngredient[] grid = new GridIngredient[9];
    private readonly List<GridRecipe> recipes = new List<GridRecipe>();
    private bool loaded;

    public event Action OnGridChanged;
    public GridRecipe MatchedRecipe { get; private set; }
    public IReadOnlyList<GridRecipe> Recipes => recipes;
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
            Debug.Log($"[GridCrafting] Loaded {recipes.Count} data-only shaped recipes.", this);
        }
        catch (Exception e) { Debug.LogError("[GridCrafting] " + e.Message, this); }
        Refresh();
    }

    public int TryMoveAmountFromInventory(int sourceInventoryIndex, int targetCell, int requestedAmount)
    {
        if (targetCell < 0 || targetCell >= 9 || requestedAmount <= 0) return 0;
        var inv = InventoryManager.Instance;
        if (inv == null || !inv.CursorSlot.isEmpty() ||
            sourceInventoryIndex < 0 || sourceInventoryIndex >= inv.Slots.Count) return 0;

        var source = inv.Slots[sourceInventoryIndex];
        if (source == null || source.isEmpty()) return 0;
        ItemData item = inv.GetItemData(source.ItemId);
        if (item == null || item.maxStack <= 0) return 0;
        GridIngredient target = grid[targetCell];
        if (target.itemId > 0 && target.itemId != source.ItemId) return 0;

        int capacity = item.maxStack - target.amount;
        int transfer = Mathf.Min(requestedAmount, source.Amount, capacity);
        Debug.Log($"[GridCrafting] LeftDrop source={sourceInventoryIndex}, cell={targetCell}, " +
                  $"item={source.ItemId}, sourceAmount={source.Amount}, requested={requestedAmount}, " +
                  $"cellAmount={target.amount}, maxStack={item.maxStack}, transfer={transfer}");
        if (transfer <= 0) return 0;

        if (!CraftingInventoryTransferService.TryTakeAmount(sourceInventoryIndex, transfer, out int id))
            return 0;

        grid[targetCell] = new GridIngredient(id, target.amount + transfer);
        Refresh();
        return transfer;
    }

    public bool TryMoveOneFromInventory(int sourceInventoryIndex, int targetCell) =>
        TryMoveAmountFromInventory(sourceInventoryIndex, targetCell, 1) == 1;

    public bool TryMoveOneFromCursor(int targetCell)
    {
        return TryMoveAmountFromCursor(targetCell, 1) == 1;
    }

    public bool TryMoveCell(int sourceCell, int targetCell)
    {
        if (sourceCell < 0 || sourceCell >= 9 || targetCell < 0 || targetCell >= 9 ||
            sourceCell == targetCell || grid[sourceCell].itemId <= 0) return false;

        // Same kind: merge as far as target stack capacity allows.
        GridIngredient from = grid[sourceCell];
        GridIngredient to = grid[targetCell];
        if (from.itemId == to.itemId)
        {
            var inv = InventoryManager.Instance;
            ItemData item = inv != null ? inv.GetItemData(from.itemId) : null;
            if (item == null) return false;
            int count = Mathf.Min(from.amount, item.maxStack - to.amount);
            if (count <= 0) return false;
            to.amount += count;
            from.amount -= count;
            grid[targetCell] = to;
            grid[sourceCell] = from.amount > 0 ? from : default;
        }
        else
        {
            grid[sourceCell] = to;
            grid[targetCell] = from;
        }
        Refresh();
        return true;
    }


    public bool TryReturnCellToInventory(
        int sourceCell,
        int targetInventoryIndex)
    {
        if (sourceCell < 0 || sourceCell >= grid.Length)
            return false;

        GridIngredient cell = grid[sourceCell];

        if (cell.itemId <= 0 || cell.amount <= 0)
            return false;

        // Tra toan bo stack neu o dich du suc chua.
        if (!CraftingInventoryTransferService.TryPlace(
            targetInventoryIndex,
            cell.itemId,
            cell.amount))
        {
            return false;
        }

        grid[sourceCell] = default;

        Refresh();
        return true;
    }



    public bool TryReturnOneCellAnywhere(int sourceCell)
    {
        if (sourceCell < 0 || sourceCell >= grid.Length)
            return false;

        GridIngredient cell = grid[sourceCell];

        if (cell.itemId <= 0 || cell.amount <= 0)
            return false;

        // Chi tra 1 Item ve Inventory.
        if (!CraftingInventoryTransferService.TryPlaceAnywhere(
            cell.itemId,
            1))
        {
            return false;
        }

        // Giam 1 Item, khong xoa ca stack.
        cell.amount--;

        grid[sourceCell] =
            cell.amount > 0 ? cell : default;

        Refresh();
        return true;
    }


    public bool TryReturnCellAnywhere(int sourceCell) => TryReturnOneCellAnywhere(sourceCell);


    public bool TryReturnAll()
    {
        return TryReturnAll(out _);
    }

    public bool TryReturnAll(out string message)
    {
        InventoryManager inv = InventoryManager.Instance;

        if (inv == null)
        {
            message = "InventoryManager not found";
            return false;
        }

        // Khong dong Crafting khi nguoi cho dang cam Item.
        if (!inv.CursorSlot.isEmpty())
        {
            message = "Put the cursor-held item away first";
            return false;
        }

        int slotCount = inv.Slots.Count;

        // Trang thai mo phong: chua sua Inventory that.
        int[] plannedItemIds = new int[slotCount];
        int[] plannedAmounts = new int[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            InventorySlotData slot = inv.Slots[i];

            if (slot == null)
            {
                message = $"Inventory slot {i} is null";
                return false;
            }

            plannedItemIds[i] = slot.isEmpty() ? 0 : slot.ItemId;
            plannedAmounts[i] = slot.isEmpty() ? 0 : slot.Amount;
        }

        // GIAI DOAN 1: Mo phong viec tra toan bo 9 o.
        for (int cellIndex = 0; cellIndex < grid.Length; cellIndex++)
        {
            GridIngredient cell = grid[cellIndex];

            if (cell.itemId <= 0 || cell.amount <= 0)
                continue;

            ItemData item = inv.GetItemData(cell.itemId);

            if (item == null || item.maxStack <= 0)
            {
                message = $"Invalid item in crafting cell {cellIndex}";
                return false;
            }

            int remaining = cell.amount;

            // Uu tien gop vao nhung stack cung ItemId.
            for (int i = 0; i < slotCount && remaining > 0; i++)
            {
                if (plannedItemIds[i] != cell.itemId)
                    continue;

                int capacity = item.maxStack - plannedAmounts[i];

                if (capacity <= 0)
                    continue;

                int moved = Mathf.Min(remaining, capacity);

                plannedAmounts[i] += moved;
                remaining -= moved;
            }

            // Sau do phan bo vao cac o trong.
            for (int i = 0; i < slotCount && remaining > 0; i++)
            {
                if (plannedItemIds[i] != 0)
                    continue;

                int moved = Mathf.Min(remaining, item.maxStack);

                plannedItemIds[i] = cell.itemId;
                plannedAmounts[i] = moved;
                remaining -= moved;
            }

            if (remaining > 0)
            {
                message =
                    $"Not enough inventory space. " +
                    $"Cannot return item {cell.itemId} x{remaining}";

                // Inventory va Crafting Grid van nguyen ven.
                return false;
            }
        }

        // GIAI DOAN 2: Tat ca da du cho.
        // Bat dau ghi du lieu that vao Inventory.
        for (int i = 0; i < slotCount; i++)
        {
            InventorySlotData slot = inv.Slots[i];

            if (plannedItemIds[i] <= 0)
                continue;

            ItemData item = inv.GetItemData(plannedItemIds[i]);

            // Da kiem tra item trong buoc mo phong.
            slot.ItemId = item.id;
            slot.ItemCode = item.itemCode;
            slot.ItemName = item.name;
            slot.MaxStack = item.maxStack;
            slot.Amount = plannedAmounts[i];
        }

        // Chi lam trong Grid SAU KHI ghi Inventory thanh cong.
        for (int i = 0; i < grid.Length; i++)
            grid[i] = default;

        // Day la chuyen vi tri Item, khong phai tao Item moi.
        // Khong goi NotifyAdded/NotifyRemoved.
        inv.NotifyChanged();
        Refresh();

        message = "All crafting ingredients returned";
        return true;
    }



    public bool TryTakeOneResultToCursor(out string message)
    {
        InventoryManager inv = InventoryManager.Instance;
        GridRecipe recipe = MatchedRecipe;

        if (inv == null || recipe == null ||
            GetMaxCraftCount() < 1)
        {
            message = "No craftable recipe";
            return false;
        }

        ItemData output = inv.GetItemData(recipe.outputId);

        if (output == null || recipe.outputAmount <= 0)
        {
            message = "Invalid output item";
            return false;
        }

        InventorySlotData cursor = inv.CursorSlot;

        // Cursor chỉ nhận Item cùng loại.
        if (!cursor.isEmpty() &&
            cursor.ItemId != output.id)
        {
            message = "Cursor contains another item";
            return false;
        }

        int currentAmount =
            cursor.isEmpty() ? 0 : cursor.Amount;

        // Phải đủ chỗ cho trọn một lượt Craft.
        if (recipe.outputAmount >
            output.maxStack - currentAmount)
        {
            message = "Not enough cursor space";
            return false;
        }

        // Kiểm tra toàn bộ nguyên liệu trước khi thay đổi.
        for (int i = 0; i < 9; i++)
        {
            GridIngredient required = recipe.cells[i];

            if (required.itemId <= 0)
                continue;

            if (grid[i].itemId != required.itemId ||
                grid[i].amount < Mathf.Max(1, required.amount))
            {
                message = "Insufficient ingredients";
                return false;
            }
        }

        // Tất cả điều kiện đã hợp lệ.
        if (cursor.isEmpty())
        {
            cursor.ItemId = output.id;
            cursor.ItemCode = output.itemCode;
            cursor.ItemName = output.name;
            cursor.MaxStack = output.maxStack;
            cursor.Amount = recipe.outputAmount;
        }
        else
        {
            cursor.Amount += recipe.outputAmount;
        }

        // Tiêu hao đúng một lượt công thức.
        for (int i = 0; i < 9; i++)
        {
            GridIngredient required = recipe.cells[i];

            if (required.itemId <= 0)
                continue;

            GridIngredient cell = grid[i];
            cell.amount -= Mathf.Max(1, required.amount);

            grid[i] = cell.amount > 0
                ? cell
                : default;
        }

        Refresh();
        inv.NotifyChanged();

        message = $"Crafted {output.name} x{recipe.outputAmount}";
        return true;
    }


    public bool CanCraft(out string reason)
    {
        if (!loaded) { reason = "CSV recipes not loaded"; return false; }
        if (MatchedRecipe == null) { reason = "Grid does not match a recipe"; return false; }
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null) { reason = "InventoryManager missing"; return false; }
        if (!inv.CursorSlot.isEmpty()) { reason = "Put cursor-held item away"; return false; }
        if (inv.GetItemData(MatchedRecipe.outputId) == null)
        { reason = "Output item not loaded"; return false; }
        reason = "Ready";
        return true;
    }


    public int GetMaxCraftCount()
    {
        GridRecipe recipe = MatchedRecipe;

        if (!loaded || recipe == null || recipe.cells == null ||
            recipe.cells.Length != 9)
            return 0;

        int maxCrafts = int.MaxValue;
        bool hasIngredient = false;

        for (int i = 0; i < 9; i++)
        {
            GridIngredient required = recipe.cells[i];

            if (required.itemId <= 0)
                continue;

            hasIngredient = true;

            GridIngredient available = grid[i];

            if (available.itemId != required.itemId ||
                available.amount <= 0)
                return 0;

            int needed = Mathf.Max(1, required.amount);
            int crafts = available.amount / needed;

            maxCrafts = Mathf.Min(maxCrafts, crafts);
        }

        return hasIngredient ? maxCrafts : 0;
    }

    public long GetMaxResultAmount()
    {
        if (MatchedRecipe == null)
            return 0;

        return (long)GetMaxCraftCount() *
               MatchedRecipe.outputAmount;
    }



    public bool TryTakeResultToInventory(
        int targetIndex,
        out string message)
    {
        InventoryManager inv = InventoryManager.Instance;
        GridRecipe recipe = MatchedRecipe;

        if (inv == null || recipe == null ||
            !inv.CursorSlot.isEmpty())
        {
            message = "Inventory unavailable or cursor not empty";
            return false;
        }

        if (targetIndex < 0 || targetIndex >= inv.Slots.Count)
        {
            message = "Invalid target slot";
            return false;
        }

        int craftCount = GetMaxCraftCount();

        if (craftCount <= 0)
        {
            message = "Not enough ingredients";
            return false;
        }

        ItemData output = inv.GetItemData(recipe.outputId);

        if (output == null || output.maxStack <= 0 ||
            recipe.outputAmount <= 0)
        {
            message = "Invalid output item";
            return false;
        }

        long totalLong =
            (long)craftCount * recipe.outputAmount;

        if (totalLong > int.MaxValue)
        {
            message = "Output quantity exceeds supported limit";
            return false;
        }

        int total = (int)totalLong;
        int remaining = total;

        // Lập kế hoạch phân phối trước khi sửa dữ liệu.
        var allocations =
            new List<(int index, int amount)>();

        void AllocateTo(int index)
        {
            if (remaining <= 0)
                return;

            InventorySlotData slot = inv.Slots[index];

            if (!slot.isEmpty() &&
                slot.ItemId != output.id)
                return;

            int capacity = slot.isEmpty()
                ? output.maxStack
                : output.maxStack - slot.Amount;

            int amount = Mathf.Min(remaining, capacity);

            if (amount <= 0)
                return;

            allocations.Add((index, amount));
            remaining -= amount;
        }

        // Ô người chơi thả vào phải nhận được sản phẩm.
        AllocateTo(targetIndex);

        if (allocations.Count == 0)
        {
            message = "Target slot cannot receive output";
            return false;
        }

        // Ưu tiên gộp vào các stack cùng loại.
        for (int i = 0; i < inv.Slots.Count && remaining > 0; i++)
        {
            if (i == targetIndex)
                continue;

            InventorySlotData slot = inv.Slots[i];

            if (!slot.isEmpty() && slot.ItemId == output.id)
                AllocateTo(i);
        }

        // Sau đó sử dụng các ô còn trống.
        for (int i = 0; i < inv.Slots.Count && remaining > 0; i++)
        {
            if (i == targetIndex)
                continue;

            if (inv.Slots[i].isEmpty())
                AllocateTo(i);
        }

        // Không đủ chỗ thì không chế tạo.
        if (remaining > 0)
        {
            message = $"Inventory needs space for {remaining} more items";
            return false;
        }

        // Bảo đảm nguyên liệu vẫn đủ trước khi commit.
        for (int i = 0; i < 9; i++)
        {
            GridIngredient required = recipe.cells[i];

            if (required.itemId <= 0)
                continue;

            int consumed = checked(
                Mathf.Max(1, required.amount) * craftCount
            );

            if (grid[i].itemId != required.itemId ||
                grid[i].amount < consumed)
            {
                message = "Ingredients changed";
                return false;
            }
        }

        // Commit: thêm sản phẩm vào Inventory.
        foreach (var allocation in allocations)
        {
            InventorySlotData slot = inv.Slots[allocation.index];

            if (slot.isEmpty())
            {
                slot.ItemId = output.id;
                slot.ItemCode = output.itemCode;
                slot.ItemName = output.name;
                slot.MaxStack = output.maxStack;
                slot.Amount = allocation.amount;
            }
            else
            {
                slot.Amount += allocation.amount;
            }
        }

        // Commit: tiêu hao nguyên liệu tương ứng.
        for (int i = 0; i < 9; i++)
        {
            GridIngredient required = recipe.cells[i];

            if (required.itemId <= 0)
                continue;

            GridIngredient cell = grid[i];

            cell.amount -=
                Mathf.Max(1, required.amount) * craftCount;

            grid[i] = cell.amount > 0 ? cell : default;
        }

        Refresh();
        inv.NotifyChanged();
        inv.NotifyAdded(output.id, total);

        message = $"Crafted {output.name} x{total}";
        return true;
    }



    public int TryMoveAmountFromCursor(
        int targetCell,
        int requestedAmount)
    {
        InventoryManager inv = InventoryManager.Instance;

        if (inv == null ||
            targetCell < 0 ||
            targetCell >= grid.Length ||
            requestedAmount <= 0)
        {
            return 0;
        }

        InventorySlotData held = inv.CursorSlot;

        if (held == null ||
            held.isEmpty() ||
            held.Amount <= 0)
        {
            return 0;
        }

        ItemData item = inv.GetItemData(held.ItemId);

        if (item == null || item.maxStack <= 0)
            return 0;

        GridIngredient target = grid[targetCell];

        // Khac ItemId thi khong swap.
        if (target.itemId > 0 &&
            target.itemId != held.ItemId)
        {
            return 0;
        }

        // Chi chuyen trong gioi han maxStack.
        int capacity = item.maxStack - target.amount;

        int transfer = Mathf.Min(
            requestedAmount,
            held.Amount,
            capacity
        );

        if (transfer <= 0)
            return 0;

        int itemId = held.ItemId;

        // Cap nhat du lieu cua Crafting Grid.
        grid[targetCell] = new GridIngredient(
            itemId,
            target.amount + transfer
        );

        // Cap nhat CursorSlot.
        held.Amount -= transfer;

        if (held.Amount <= 0)
            held.Clear();

        // Thong bao InventoryUI cap nhat Cursor.
        inv.NotifyChanged();

        // Kiem tra lai cong thuc CSV.
        Refresh();

        Debug.Log(
            $"[GridCrafting] Cursor -> Cell {targetCell}, " +
            $"ItemId={itemId}, transferred={transfer}, " +
            $"cellAmount={grid[targetCell].amount}"
        );

        return transfer;
    }


    public bool TrySplitCellToCursor(int sourceCell)
    {
        InventoryManager inventory = InventoryManager.Instance;

        if (inventory == null ||
            sourceCell < 0 ||
            sourceCell >= grid.Length)
            return false;

        InventorySlotData cursor = inventory.CursorSlot;

        // Chi split khi Cursor dang trong.
        if (!cursor.isEmpty())
            return false;

        GridIngredient cell = grid[sourceCell];

        if (cell.itemId <= 0 || cell.amount <= 0)
            return false;

        ItemData item = inventory.GetItemData(cell.itemId);

        if (item == null)
            return false;

        // Giong InventoryCursorService:
        // stack = 1 -> lay 1
        // stack > 1 -> lay floor(amount / 2)
        int splitAmount =
            cell.amount == 1 ? 1 : cell.amount / 2;

        // Gan du thong tin Item cho Cursor.
        cursor.ItemId = item.id;
        cursor.ItemCode = item.itemCode;
        cursor.ItemName = item.name;
        cursor.MaxStack = item.maxStack;
        cursor.Amount = splitAmount;

        // Tru so luong da lay khoi Crafting.
        cell.amount -= splitAmount;

        grid[sourceCell] =
            cell.amount > 0 ? cell : default;

        // Cap nhat Cursor UI va Crafting UI.
        inventory.NotifyChanged();
        Refresh();

        Debug.Log(
            $"[GridCrafting] Split Cell={sourceCell}, " +
            $"ItemId={item.id}, " +
            $"CursorAmount={splitAmount}, " +
            $"Remaining={grid[sourceCell].amount}"
        );

        return true;
    }


    public GridIngredient[] CreateSnapshot()
    {
        GridIngredient[] snapshot =
            new GridIngredient[grid.Length];

        for (int i = 0; i < grid.Length; i++)
            snapshot[i] = grid[i];

        return snapshot;
    }


    public bool TryLoadGridFromResponse(
        InventorySlotResponse[] records)
    {
        if (records == null)
            return false;

        InventoryManager inventory = InventoryManager.Instance;

        if (inventory == null)
            return false;

        // Tao mang tam, khong sua Grid hien tai ngay.
        GridIngredient[] restored =
            new GridIngredient[grid.Length];

        HashSet<int> used = new HashSet<int>();

        // Kiem tra du lieu tu Backend.
        foreach (InventorySlotResponse record in records)
        {
            if (record == null ||
                record.slotIndex < 0 ||
                record.slotIndex >= grid.Length ||
                !used.Add(record.slotIndex))
            {
                Debug.LogError(
                    "[GridCrafting] Invalid or duplicate grid index.");
                return false;
            }

            ItemData item =
                inventory.GetItemData(record.itemId);

            if (item == null ||
                record.amount <= 0 ||
                record.amount > item.maxStack)
            {
                Debug.LogError(
                    $"[GridCrafting] Invalid saved item: " +
                    $"ItemId={record.itemId}, Amount={record.amount}");
                return false;
            }

            restored[record.slotIndex] =
                new GridIngredient(
                    record.itemId,
                    record.amount);
        }

        // Chi cap nhat khi tat ca du lieu hop le.
        Array.Copy(restored, grid, grid.Length);

        // Kiem tra lai CSV recipe va cap nhat UI.
        Refresh();

        Debug.Log(
            $"[GridCrafting] Restored {records.Length} crafting slots.");

        return true;
    }


    public bool TryLoadGrid(
        IReadOnlyList<InventorySlotRecord> records)
    {
        if (records == null)
            return false;

        InventoryManager inv = InventoryManager.Instance;

        if (inv == null)
            return false;

        GridIngredient[] restored =
            new GridIngredient[grid.Length];

        HashSet<int> used = new HashSet<int>();

        // Kiểm tra toàn bộ dữ liệu trước khi thay đổi Grid.
        foreach (InventorySlotRecord record in records)
        {
            if (record.SlotIndex < 0 ||
                record.SlotIndex >= grid.Length ||
                !used.Add(record.SlotIndex))
            {
                return false;
            }

            ItemData item = inv.GetItemData(record.ItemId);

            if (item == null ||
                record.Amount <= 0 ||
                record.Amount > item.maxStack)
            {
                return false;
            }

            restored[record.SlotIndex] =
                new GridIngredient(
                    record.ItemId,
                    record.Amount
                );
        }

        // Dữ liệu hợp lệ: khôi phục đủ 9 ô.
        for (int i = 0; i < grid.Length; i++)
            grid[i] = restored[i];

        Refresh();
        return true;
    }



    [Obsolete("Use TryMoveAmountFromInventory; virtual cell assignment is disabled.")]
    public bool SetCell(int index, int itemId, int amount) => false;
    [Obsolete("Use TryMoveAmountFromInventory.")]
    public bool SetCellFromInventory(int cell, int index, int amount) =>
        TryMoveAmountFromInventory(index, cell, amount) == amount;
    [Obsolete("Use TryReturnCellAnywhere.")]
    public void ClearCell(int index) => TryReturnCellAnywhere(index);
    [Obsolete("Use TryReturnAll.")]
    public void ClearGrid() => TryReturnAll();
    [Obsolete("Use TryTakeResultToInventory.")]
    public bool TryCraft(out string message)
    {
        message = "Drop the result onto an Inventory slot";
        return false;
    }
    public string CurrentStation => string.Empty;
    [Obsolete("No station required.")]
    public void SetStation(string station) { }

    private void Refresh()
    {
        MatchedRecipe = null;
        if (loaded)
        {
            // CSV matcher expects amount == 1 per occupied cell.
            // For stacking, normalize amounts ONLY for shape matching.
            GridIngredient[] shape = new GridIngredient[9];
            for (int i = 0; i < 9; i++)
                if (grid[i].itemId > 0 && grid[i].amount > 0)
                    shape[i] = new GridIngredient(grid[i].itemId, 1);
            foreach (GridRecipe recipe in recipes)
                if (GridRecipeCatalog.Matches(shape, recipe.cells))
                {
                    MatchedRecipe = recipe;
                    break;
                }
        }
        OnGridChanged?.Invoke();
    }
}
