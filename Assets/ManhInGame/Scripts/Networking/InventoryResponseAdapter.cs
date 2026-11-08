using System.Collections.Generic;

/// <summary>Adapter for the existing Unity InventorySlotResponse DTO.</summary>
public static class InventoryResponseAdapter
{
    public static bool Apply(InventoryManager manager, InventorySlotResponse[] response)
    {
        if (manager == null || response == null) return false;
        var records = new List<InventorySlotRecord>(response.Length);
        foreach (var slot in response)
            records.Add(new InventorySlotRecord(slot.slotIndex, slot.itemId, slot.amount));
        return manager.TryLoadSlots(records);
    }
}
