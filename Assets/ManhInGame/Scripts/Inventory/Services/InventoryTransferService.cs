using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class InventoryTransferService : InventoryServiceBase
{
    internal InventoryTransferService(InventoryManager owner) : base(owner) { }

    public bool MoveItemAmount(int fromIndex, int toIndex, int amount)
    {
        if (fromIndex < 0 || fromIndex >= Slots.Count || toIndex < 0 || toIndex >= Slots.Count || fromIndex == toIndex)
            return false;

        InventorySlotData source = Slots[fromIndex];
        InventorySlotData target = Slots[toIndex];

        if (amount <= 0 || source.isEmpty() || source.Amount < amount)
            return false;

        if (target.isEmpty())
        {
            CopySlotData(source, target);
            target.Amount = amount;
            source.Amount -= amount;

            if (source.Amount <= 0)
                source.Clear();

            Owner.NotifyChanged();
            return true;
        }

        if (source.ItemId == target.ItemId)
        {
            int availableSpace = target.MaxStack - target.Amount;

            if (availableSpace > 0)
            {
                int amountToAdd = Mathf.Min(availableSpace, amount);
                target.Amount += amountToAdd;
                source.Amount -= amountToAdd;

                if (source.Amount <= 0)
                    source.Clear();

                Owner.NotifyChanged();
                return true;
            }

        }

        if (amount == source.Amount && target.ItemId != source.ItemId)
        {
            InventorySlotData temp = new InventorySlotData();
            CopySlotData(source, temp);
            CopySlotData(target, source);
            CopySlotData(temp, target);
            Owner.NotifyChanged();
            return true;
        }

        return false;
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

            Owner.NotifyChanged();
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

            Owner.NotifyChanged();
            return true;
        }

        Slots[fromIndex] = target;
        Slots[toIndex] = source;

        Owner.NotifyChanged();
        return true;
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

        for (int i = 0; i < Slots.Count; i++)
        {
            if (Slots[i].isEmpty())
            {
                Slots[i].ItemId = sourceSlot.ItemId;
                Slots[i].ItemCode = sourceSlot.ItemCode;
                Slots[i].ItemName = sourceSlot.ItemName;
                Slots[i].Amount = splitAmount;
                Slots[i].MaxStack = sourceSlot.MaxStack;

                sourceSlot.Amount -= splitAmount;

                Owner.NotifyChanged();

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
}
