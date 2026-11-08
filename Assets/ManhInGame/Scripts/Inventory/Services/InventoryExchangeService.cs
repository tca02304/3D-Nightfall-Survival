using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class InventoryExchangeService : InventoryServiceBase
{
    private readonly InventoryItemService items;
    internal InventoryExchangeService(InventoryManager owner, InventoryItemService items) : base(owner)
    {
        this.items = items;
    }

    public bool CanExchangeItemsForItem(
        IReadOnlyDictionary<int, int> itemsToRemove,
        ItemData outputItem,
        int outputAmount)
    {
        if (!InventoryItemService.IsValidItem(outputItem) || outputAmount <= 0)
            return false;

        if (itemsToRemove == null || itemsToRemove.Count == 0)
            return false;

        foreach (KeyValuePair<int, int> requirement in itemsToRemove)
        {
            if (requirement.Key <= 0 || requirement.Value <= 0)
                return false;

            if (Owner.CountItem(requirement.Key) < requirement.Value)
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
        if (!CanExchangeItemsForItem(
            itemsToRemove,
            outputItem,
            outputAmount))
        {
            return false;
        }

        InventorySlotData[] snapshot = new InventorySlotData[Slots.Count];

        for (int i = 0; i < Slots.Count; i++)
        {
            snapshot[i] = CopySlot(Slots[i]);
        }

        foreach (KeyValuePair<int, int> requirement in itemsToRemove)
        {
            int remainingToRemove = requirement.Value;

            for (int i = 0; i < Slots.Count && remainingToRemove > 0; i++)
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

            if (remainingToRemove > 0)
            {
                RestoreSlots(snapshot);
                return false;
            }
        }

        int remainingOutput = items.AddItemInternal(outputItem, outputAmount);

        if (remainingOutput > 0)
        {
            RestoreSlots(snapshot);
            return false;
        }

        foreach (KeyValuePair<int, int> requirement in itemsToRemove)
        {
            Owner.NotifyRemoved(
                requirement.Key,
                requirement.Value
                );
        }

        Owner.NotifyAdded(outputItem.id, outputAmount);
        Owner.NotifyChanged();

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

        for (int i = 0; i < count; i++)
        {
            InventorySlotData slot = Slots[i];

            if (slot.isEmpty())
                continue;

            simulatedIds[i] = slot.ItemId;
            simulatedAmounts[i] = slot.Amount;
            simulatedMaxStacks[i] = slot.MaxStack;
        }

        //Mô phỏng trừ nguyên liệu, chưa sửa dữ liệu thật
        foreach (KeyValuePair<int, int> requirement in itemsToRemove)
        {
            int remainingToRemove = requirement.Value;

            for (int i = 0; i < count && remainingToRemove > 0; i++)
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
}
