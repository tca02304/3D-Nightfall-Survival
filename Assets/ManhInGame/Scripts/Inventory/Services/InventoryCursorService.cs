using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class InventoryCursorService : InventoryServiceBase
{
    internal InventoryCursorService(InventoryManager owner) : base(owner) { }

    public void HandleRightClick(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= Slots.Count)
            return;

        InventorySlotData slot = Slots[slotIndex];

        if (CursorSlot.isEmpty())
        {
            if (slot.isEmpty()) return;

            if (slot.Amount == 1)
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

            Owner.NotifyChanged();
            return;
        }

        // Dang cam Item: khac ID thi khong swap, khong dat.
        if (!slot.isEmpty() && slot.ItemId != CursorSlot.ItemId)
            return;

        // Chuot phai chi dat mot Item.
        if (slot.isEmpty())
        {
            CopySlotData(CursorSlot, slot);
            slot.Amount = 1;
        }
        else
        {
            if (slot.Amount >= slot.MaxStack) return;
            slot.Amount++;
        }

        CursorSlot.Amount--;
        if (CursorSlot.Amount <= 0)
            CursorSlot.Clear();

        Owner.NotifyChanged();
    }

    public void HandleLeftClick(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= Slots.Count)
            return;

        if (CursorSlot.isEmpty())
            return;

        InventorySlotData slot = Slots[slotIndex];

        if (slot.isEmpty())
        {
            CopySlotData(CursorSlot, slot);
            CursorSlot.Clear();
            Owner.NotifyChanged();
            return;
        }

        // Khac ID thi khong swap voi CursorSlot.
        if (slot.ItemId != CursorSlot.ItemId)
            return;

        int availableSpace = slot.MaxStack - slot.Amount;
        if (availableSpace <= 0)
            return;

        int amountToMove = Mathf.Min(CursorSlot.Amount, availableSpace);
        slot.Amount += amountToMove;
        CursorSlot.Amount -= amountToMove;

        if (CursorSlot.Amount <= 0)
            CursorSlot.Clear();

        Owner.NotifyChanged();
    }
}