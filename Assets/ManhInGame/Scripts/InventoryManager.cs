using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [SerializeField] private int inventorySize = 28;
    [SerializeField] private int hotbarSize = 7;

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
        int remainingAmount = amount;
        int originalAmount = amount;

        for(int i = 0; i < Slots.Count; i++)
        {
            InventorySlotData slot = Slots[i];

            if (!slot.isEmpty() &&
                slot.ItemId == item.id &&
                slot.Amount < slot.MaxStack)
            {
                int availableSpace = slot.MaxStack - slot.Amount;

                int amountToAdd = Mathf.Min(remainingAmount, availableSpace);

                slot.Amount += amountToAdd;
                remainingAmount -= amountToAdd;

                if (remainingAmount <= 0)
                {
                    OnItemAdded?.Invoke(item.id, originalAmount);
                    OnInventoryChanged?.Invoke();
                    return 0;
                }
            }
        }

        for(int i = 0; i < Slots.Count; i++)
        {
            if (Slots[i].isEmpty())
            {
                int amountToAdd = Mathf.Min(remainingAmount, item.maxStack);

                Slots[i].ItemId = item.id;
                Slots[i].ItemCode = item.itemCode;
                Slots[i].ItemName = item.name;
                Slots[i].Amount = amount;
                Slots[i].MaxStack = item.maxStack;

                remainingAmount -= amountToAdd;

                if (remainingAmount <= 0)
                {
                    OnItemAdded?.Invoke(item.id, originalAmount);
                    OnInventoryChanged?.Invoke();
                    return 0;
                }
            }
        }

        int addedAmount = originalAmount - remainingAmount;

        if (addedAmount > 0)
        {
            OnItemAdded?.Invoke(item.id, addedAmount);
            OnInventoryChanged?.Invoke();
        }

        return remainingAmount;
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
}