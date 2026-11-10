using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class InventoryItemService : InventoryServiceBase
{
    internal InventoryItemService(InventoryManager owner) : base(owner) { }

    public int AddItem(ItemData item, int amount = 1)
    {
        if (amount <= 0)
            return 0;

        if (!IsValidItem(item))
            return amount;

        int remainingAmount = AddItemInternal(item, amount);
        int addedAmount = amount - remainingAmount;

        if (addedAmount > 0)
        {
            Owner.NotifyAdded(item.id, addedAmount);
            Owner.NotifyChanged();
        }

        return remainingAmount;
    }

    internal static bool IsValidItem(ItemData item)
    {
        return item != null
            && item.id > 0
            && !string.IsNullOrEmpty(item.itemCode)
            && item.maxStack > 0;
    }

    internal int AddItemInternal(ItemData item, int amount)
    {
        int remainingAmount = amount;
        int limit = Mathf.Min(inventorySize, Slots.Count);

        for (int i = 0; i < limit; i++)
        {
            InventorySlotData slot = Slots[i];

            if (slot.isEmpty()
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

        for (int i = 0; i < limit; i++)
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

        Owner.NotifyRemoved(itemId, removedAmount);
        Owner.NotifyChanged();

        return true;
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

        if (slot.Amount <= 0)
        {
            slot.Clear();
        }

        Owner.NotifyRemoved(droppedItem.ItemId, amount);
        Owner.NotifyChanged();

        return droppedItem;
    }

    public int FindItem(int itemId)
    {
        for (int i = 0; i < Slots.Count; i++)
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

        for (int i = 0; i < Slots.Count; i++)
        {
            if (!Slots[i].isEmpty() && Slots[i].ItemId == itemId)
            {
                total += Slots[i].Amount;
            }
        }

        return total;
    }

    public ItemData GetItemData(int itemId)
    {
        if (ItemDatabase.Instance == null)
            return null;

        return ItemDatabase.Instance.GetItemById(itemId);
    }
}
