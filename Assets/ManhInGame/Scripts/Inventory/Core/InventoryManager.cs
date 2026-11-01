using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [SerializeField] private int inventorySize = 32;
    [SerializeField] private int hotbarSize = 7;

    public int InventorySize => inventorySize;
    public int HotbarSize => hotbarSize;

    public List<InventorySlotData> Slots = new();

    public event Action<int, int> OnItemAdded;
    public event Action<int, int> OnItemRemoved;
    public event Action OnInventoryChanged;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        CreateInventory();
    }

    // Biến lưu trữ dữ liệu của vật phẩm đang "dính" trên chuột
    public InventorySlotData CursorSlot = new InventorySlotData();

    public bool MoveItemAmount(int fromIndex, int toIndex, int amount)
    {
        if (fromIndex < 0 || fromIndex >= Slots.Count || toIndex < 0 || toIndex >= Slots.Count || fromIndex == toIndex)
            return false;

        InventorySlotData source = Slots[fromIndex];
        InventorySlotData target = Slots[toIndex];

        if (source.isEmpty() || source.Amount < amount)
            return false;

        if (target.isEmpty())
        {
            CopySlotData(source, target);
            target.Amount = amount;
            source.Amount -= amount;

            if (source.Amount <= 0)
                source.Clear();

            OnInventoryChanged?.Invoke();
            return true;
        }

        if(source.ItemId == target.ItemId)
        {
            int availableSpace = target.MaxStack - target.Amount;

            if (availableSpace > 0)
            {
                int amountToAdd = Mathf.Min(availableSpace, amount);
                target.Amount += amountToAdd;
                source.Amount -= amountToAdd;

                if (source.Amount <= 0)
                    source.Clear();

                OnInventoryChanged?.Invoke();
                return true;
            }

        }

        if(amount == source.Amount)
        {
            InventorySlotData temp = new InventorySlotData();
            CopySlotData(source, temp);
            CopySlotData(target, source);
            CopySlotData(temp, target);
            OnInventoryChanged?.Invoke();
            return true;
        }

        return false;
    }

    public void HandleRightClick(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= Slots.Count)
            return;

        InventorySlotData slot = Slots[slotIndex];

        // 1. Chuột trống
        if (CursorSlot.isEmpty())
        {
            if (slot.isEmpty())
                return;

            if(slot.Amount == 1)
            {   
                CopySlotData(slot, CursorSlot);
                slot.Clear();
            }
            else
            {
                int half = slot.Amount / 2;

                CopySlotData(slot, CursorSlot);
                CursorSlot.Amount = half;

                slot.Amount -= half;
            }

            OnInventoryChanged?.Invoke();
            return;
        }

        // 2. Chuột đang cầm Item
        if (!slot.isEmpty() && slot.ItemId != CursorSlot.ItemId)
            return;

        if (slot.isEmpty())
        {
            CopySlotData(CursorSlot, slot);
            slot.Amount = 1;
        }
        else
        {
            if (slot.Amount >= slot.MaxStack)
                return;

            slot.Amount += 1;
        }

        CursorSlot.Amount -= 1;

        if (CursorSlot.Amount <= 0)
            CursorSlot.Clear();

        OnInventoryChanged?.Invoke();
    }

    public void HandleLeftClick(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= Slots.Count)
            return;

        if (CursorSlot.isEmpty())
            return;

        InventorySlotData slot = Slots[slotIndex];

        // 1. Ô trống
        if (slot.isEmpty())
        {
            CopySlotData(CursorSlot, slot);
            CursorSlot.Clear();

            OnInventoryChanged?.Invoke();
            return;
        }

        // 2. Ô khác ID
        if (slot.ItemId != CursorSlot.ItemId)
            return;

        // 2. Ô cùng ID
        int availableSpace = slot.MaxStack - slot.Amount;

        if (availableSpace <= 0)
            return;

        int amountToAdd = Mathf.Min(CursorSlot.Amount, availableSpace);

        slot.Amount += amountToAdd;
        CursorSlot.Amount -= amountToAdd;

        if (CursorSlot.Amount <= 0)
            CursorSlot.Clear();

        OnInventoryChanged?.Invoke();
    }

    private void CopySlotData(InventorySlotData source, InventorySlotData target)
    {
        target.ItemId = source.ItemId;
        target.ItemCode = source.ItemCode;
        target.ItemName = source.ItemName;
        target.MaxStack = source.MaxStack;
        target.Amount = source.Amount;
    }

    // Hàm tiện ích để copy dữ liệu

    private void CreateInventory()
    {
        Slots.Clear();

        int totalSize = inventorySize + hotbarSize;

        for (int i = 0; i < totalSize; i++)
        {
            Slots.Add(new InventorySlotData());
        }
    }

    public int AddItem(ItemData item, int amount = 1)
    {
        if (amount <= 0)
            return 0;

        if (!IsValidItem(item))
            return amount;

        int remainingAmount = AddItemInternal(item, amount);
        int addedAmount = amount - remainingAmount;

        if(addedAmount > 0)
        {
            OnItemAdded?.Invoke(item.id, addedAmount);
            OnInventoryChanged?.Invoke();
        }

        return remainingAmount;
    }

    private bool IsValidItem(ItemData item)
    {
        return item != null
            && item.id > 0
            && !string.IsNullOrEmpty(item.itemCode)
            && item.maxStack > 0;
    }

    private int AddItemInternal(ItemData item, int amount)
    {
        int remainingAmount = amount;
        int limit = Mathf.Min(inventorySize, Slots.Count);

        for (int i = 0; i < limit; i++)
        {
            InventorySlotData slot = Slots[i];

            if(slot.isEmpty()
                || slot.ItemId != item.id
                || slot.Amount >= slot.MaxStack)
            {
                continue;
            }

            int availableSpace = slot.MaxStack - slot.Amount;
            int amountToAdd = Mathf.Min(remainingAmount, availableSpace);

            slot.Amount += amountToAdd;
            remainingAmount -= amountToAdd;

            if (remainingAmount <= 0)
                return 0;
        }

        for(int i = 0; i < limit; i++)
        {
            InventorySlotData slot = Slots[i];

            if (!slot.isEmpty())
                continue;

            int amountToAdd = Mathf.Min(remainingAmount, item.maxStack);

            slot.ItemId = item.id;
            slot.ItemCode = item.itemCode;
            slot.ItemName = item.name;
            slot.Amount = amountToAdd;
            slot.MaxStack = item.maxStack;

            remainingAmount -= amountToAdd;

            if (remainingAmount <= 0)
                return 0;
        }

        return remainingAmount;
    }

    public bool MoveSwapOrMergeItem(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= Slots.Count ||
            toIndex < 0 || toIndex >= Slots.Count)
        {
            return false;
        }

        if (fromIndex == toIndex)
            return false;

        InventorySlotData source = Slots[fromIndex];
        InventorySlotData target = Slots[toIndex];

        if (source.isEmpty())
            return false;

        if (target.isEmpty())
        {
            Slots[toIndex] = source;
            Slots[fromIndex] = new InventorySlotData();

            OnInventoryChanged?.Invoke();
            return true;
        }

        if (source.ItemId == target.ItemId)
        {
            if (target.Amount >= target.MaxStack)
                return false;

            int availableSpace = target.MaxStack - target.Amount;

            int amountToMove = Mathf.Min(source.Amount, availableSpace);

            target.Amount += amountToMove;
            source.Amount -= amountToMove;

            if (source.Amount <= 0)
            {
                source.Clear();
            }

            OnInventoryChanged?.Invoke();
            return true;
        }

        Slots[fromIndex] = target;
        Slots[toIndex] = source;

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool RemoveItemAt(int slotIndex, int amount = 1)
    {
        if (slotIndex < 0 || slotIndex >= Slots.Count)
            return false;

        if (amount <= 0)
            return false;

        InventorySlotData slot = Slots[slotIndex];

        if (slot.isEmpty())
            return false;

        int itemId = slot.ItemId;

        int removedAmount = Mathf.Min(amount, slot.Amount);

        slot.Amount -= removedAmount;

        if (slot.Amount <= 0)
        {
            slot.Clear();
        }

        OnItemRemoved?.Invoke(itemId, removedAmount);
        OnInventoryChanged?.Invoke();

        return true;
    }

    public int FindItem(int itemId)
    {
        for(int i = 0; i < Slots.Count; i++)
        {
            if (!Slots[i].isEmpty() && Slots[i].ItemId == itemId)
            {
                return i;
            }
        }

        return -1;
    }

    public bool CheckItem(int itemId)
    {
        return FindItem(itemId) != -1;
    }

    public int CountItem(int itemId)
    {
        int total = 0;

        for(int i = 0; i < Slots.Count; i++)
        {
            if (!Slots[i].isEmpty() && Slots[i].ItemId == itemId)
            {
                total += Slots[i].Amount;
            }
        }

        return total;
    }

    public bool SplitStack(int slotIndex, int splitAmount)
    {
        if (slotIndex < 0 || slotIndex >= Slots.Count)
            return false;

        InventorySlotData sourceSlot = Slots[slotIndex];

        if (sourceSlot.isEmpty())
            return false;

        if (sourceSlot.MaxStack <= 1)
            return false;

        if (splitAmount <= 0 || splitAmount >= sourceSlot.Amount)
            return false;

        for(int i = 0; i < Slots.Count; i++)
        {
            if (Slots[i].isEmpty())
            {
                Slots[i].ItemId = sourceSlot.ItemId;
                Slots[i].ItemCode = sourceSlot.ItemCode;
                Slots[i].ItemName = sourceSlot.ItemName;
                Slots[i].Amount = splitAmount;
                Slots[i].MaxStack = sourceSlot.MaxStack;

                sourceSlot.Amount -= splitAmount;

                OnInventoryChanged?.Invoke();

                return true;
            }
        }

        return false;
    }

    public bool SplitStackHalf(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= Slots.Count)
            return false;

        InventorySlotData sourceSlot = Slots[slotIndex];

        if (sourceSlot.isEmpty() || sourceSlot.Amount <= 1)
            return false;

        int splitAmount = sourceSlot.Amount / 2;

        return SplitStack(slotIndex, splitAmount);
    }

    public InventorySlotData DropItem(int slotIndex, int amount = 1)
    {
        if (slotIndex < 0 || slotIndex >= Slots.Count)
            return null;

        InventorySlotData slot = Slots[slotIndex];

        if (slot.isEmpty())
            return null;

        if (amount <= 0 || amount > slot.Amount)
            return null;

        InventorySlotData droppedItem = new InventorySlotData
        {
            ItemId = slot.ItemId,
            ItemCode = slot.ItemCode,
            ItemName = slot.ItemName,
            Amount = amount,
            MaxStack = slot.MaxStack
        };

        slot.Amount -= amount;

        if(slot.Amount <= 0)
        {
            slot.Clear();
        }

        OnItemRemoved?.Invoke(droppedItem.ItemId, amount);
        OnInventoryChanged?.Invoke();

        return droppedItem;
    }

    public bool CanExchangeItemsForItem(
        IReadOnlyDictionary<int, int> itemsToRemove,
        ItemData outputItem,
        int outputAmount)
    {
        if (!IsValidItem(outputItem) || outputAmount <= 0)
            return false;

        if (itemsToRemove == null || itemsToRemove.Count == 0)
            return false;

        foreach(KeyValuePair<int, int> requirement in itemsToRemove)
        {
            if (requirement.Key <= 0 || requirement.Value <= 0)
                return false;

            if (CountItem(requirement.Key) < requirement.Value)
                return false;
        }

        return CanAddItemAfterRemoving(
            itemsToRemove,
            outputItem,
            outputAmount
            );
    }

    public bool TryExchangeItemsForItem(
        IReadOnlyDictionary<int, int> itemsToRemove,
        ItemData outputItem,
        int outputAmount)
    {
        if(!CanExchangeItemsForItem(
            itemsToRemove,
            outputItem,
            outputAmount))
        {
            return false;
        }

        InventorySlotData[] snapshot = new InventorySlotData[Slots.Count];

        for(int i = 0; i < Slots.Count; i++)
        {
            snapshot[i] = CopySlot(Slots[i]);
        }

        foreach(KeyValuePair<int, int> requirement in itemsToRemove)
        {
            int remainingToRemove = requirement.Value;

            for(int i = 0; i < Slots.Count && remainingToRemove > 0; i++)
            {
                InventorySlotData slot = Slots[i];

                if (slot.isEmpty() || slot.ItemId != requirement.Key)
                    continue;

                int amountToRemove = Mathf.Min(remainingToRemove, slot.Amount);

                slot.Amount -= amountToRemove;
                remainingToRemove -= amountToRemove;

                if (slot.Amount <= 0)
                    slot.Clear();
            }

            if(remainingToRemove > 0)
            {
                RestoreSlots(snapshot);
                return false;
            }
        }

        int remainingOutput = AddItemInternal(outputItem, outputAmount);

        if(remainingOutput > 0)
        {
            RestoreSlots(snapshot);
            return false;
        }
        
        foreach(KeyValuePair<int, int> requirement in itemsToRemove)
        {
            OnItemRemoved?.Invoke(
                requirement.Key,
                requirement.Value
                );
        }

        OnItemAdded?.Invoke(outputItem.id, outputAmount);
        OnInventoryChanged?.Invoke();

        return true;
    }

    private bool CanAddItemAfterRemoving(
        IReadOnlyDictionary<int, int> itemsToRemove,
        ItemData outputItem,
        int outputAmount)
    {
        int count = Slots.Count;

        int[] simulatedIds = new int[count];
        int[] simulatedAmounts = new int[count];
        int[] simulatedMaxStacks = new int[count];

        for(int i = 0; i < count; i++)
        {
            InventorySlotData slot = Slots[i];

            if (slot.isEmpty())
                continue;

            simulatedIds[i] = slot.ItemId;
            simulatedAmounts[i] = slot.Amount;
            simulatedMaxStacks[i] = slot.MaxStack;
        }

        //Mô phỏng trừ nguyên liệu, chưa sửa dữ liệu thật
        foreach(KeyValuePair<int, int> requirement in itemsToRemove)
        {
            int remainingToRemove = requirement.Value;

            for(int i = 0; i < count && remainingToRemove > 0; i++)
            {
                if (simulatedAmounts[i] <= 0
                    || simulatedIds[i] != requirement.Key)
                {
                    continue;
                }

                int amountToRemove = Mathf.Min(remainingToRemove, simulatedAmounts[i]);

                simulatedAmounts[i] -= amountToRemove;
                remainingToRemove -= amountToRemove;

                if (simulatedAmounts[i] <= 0)
                {
                    simulatedIds[i] = 0;
                    simulatedMaxStacks[i] = 0;
                }
            }

            if (remainingToRemove > 0)
                return false;
        }

        int remainingOutput = outputAmount;
        int inventoryLimit = Mathf.Min(inventorySize, count);

        // Mô phỏng thêm vào stack cùng loại
        for (int i = 0; i < inventoryLimit; i++)
        {
            if (simulatedAmounts[i] <= 0
                || simulatedIds[i] != outputItem.id
                || simulatedAmounts[i] >= simulatedMaxStacks[i])
            {
                continue;
            }

            int availableSpace =
                simulatedMaxStacks[i] - simulatedAmounts[i];

            int amountToAdd = Mathf.Min(
                remainingOutput,
                availableSpace
            );

            remainingOutput -= amountToAdd;

            if (remainingOutput <= 0)
                return true;
        }

        //Mô phỏng thêm vào ô trống
        for (int i = 0; i < inventoryLimit; i++)
        {
            if (simulatedAmounts[i] > 0)
                continue;

            int amountToAdd = Mathf.Min(
                remainingOutput,
                outputItem.maxStack
            );

            remainingOutput -= amountToAdd;

            if (remainingOutput <= 0)
                return true;
        }

        return false;
    }

    private InventorySlotData CopySlot(InventorySlotData source)
    {
        return new InventorySlotData
        {
            ItemId = source.ItemId,
            ItemCode = source.ItemCode,
            ItemName = source.ItemName,
            Amount = source.Amount,
            MaxStack = source.MaxStack
        };
    }

    private void RestoreSlots(InventorySlotData[] snapshot)
    {
        for(int i = 0; i < Slots.Count; i++)
        {
            InventorySlotData source = snapshot[i];
            InventorySlotData target = Slots[i];

            target.ItemId = source.ItemId;
            target.ItemCode = source.ItemCode;
            target.ItemName = source.ItemName;
            target.Amount = source.Amount;
            target.MaxStack = source.MaxStack;
        }
    }

    public ItemData GetItemData(int itemId)
    {
        if (ItemDatabase.Instance == null)
            return null;

        return ItemDatabase.Instance.GetItemById(itemId);
    }

    public bool LoadInventory(InventorySlotResponse[] serverSlots)
    {
        if(serverSlots == null)
        {
            Debug.LogError("[Inventory] Server data is null");
            return false;
        }

        if (!CursorSlot.isEmpty())
        {
            Debug.LogWarning(
                "[Inventory] Cannot load while holding items");
            return false;
        }

        HashSet<int> usedIndices = new HashSet<int>();

        foreach(InventorySlotResponse serverSlot in serverSlots)
        {
            if(serverSlot.slotIndex < 0 ||
                serverSlot.slotIndex >= Slots.Count)
            {
                Debug.LogError("[Inventory] Invalid slot index");
                return false;
            }

            if (!usedIndices.Add(serverSlot.slotIndex))
            {
                Debug.LogError("[Inventory] Duplicate slot index");
                return false;
            }

            ItemData item = GetItemData(serverSlot.itemId);

            if(item == null ||
                serverSlot.amount <= 0 ||
                serverSlot.amount > item.maxStack)
            {
                Debug.LogError(
                    "[Inventory] Invalid item or amount");
                return false;
            }
        }

        foreach(InventorySlotData slot in Slots)
        {
            slot.Clear();
        }

        foreach(InventorySlotResponse serverSlot in serverSlots)
        {
            ItemData item = GetItemData(serverSlot.itemId);

            InventorySlotData slot = Slots[serverSlot.slotIndex];

            slot.ItemId = item.id;
            slot.ItemCode = item.itemCode;
            slot.ItemName = item.name;
            slot.Amount = serverSlot.amount;
            slot.MaxStack = item.maxStack;
        }

        OnInventoryChanged?.Invoke();

        Debug.Log("[Inventory] Loaded inventory into slots");

        return true;
    }
}