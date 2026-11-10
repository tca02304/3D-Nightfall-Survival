/// <summary>Transport-neutral snapshot from a persistence adapter.</summary>
public readonly struct InventorySlotRecord
{
    public int SlotIndex { get; }
    public int ItemId { get; }
    public int Amount { get; }

    public InventorySlotRecord(int slotIndex, int itemId, int amount)
    {
        SlotIndex = slotIndex;
        ItemId = itemId;
        Amount = amount;
    }
}
