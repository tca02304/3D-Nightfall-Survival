using System.Collections.Generic;
using UnityEngine;

internal abstract class InventoryServiceBase
{
    protected readonly InventoryManager Owner;
    protected List<InventorySlotData> Slots => Owner.Slots;
    protected InventorySlotData CursorSlot => Owner.CursorSlot;
    protected int inventorySize => Owner.InventorySize;

    protected InventoryServiceBase(InventoryManager owner) { Owner = owner; }

    protected static void CopySlotData(InventorySlotData source, InventorySlotData target)
    {
        target.ItemId = source.ItemId;
        target.ItemCode = source.ItemCode;
        target.ItemName = source.ItemName;
        target.MaxStack = source.MaxStack;
        target.Amount = source.Amount;
    }

    protected static InventorySlotData CopySlot(InventorySlotData source)
    {
        var result = new InventorySlotData();
        CopySlotData(source, result);
        return result;
    }

    protected void RestoreSlots(InventorySlotData[] snapshot)
    {
        for (int i = 0; i < Slots.Count; i++) CopySlotData(snapshot[i], Slots[i]);
    }
}
