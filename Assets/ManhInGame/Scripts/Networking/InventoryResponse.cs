using System;

[Serializable]
public class InventoryResponse
{
    public bool success;
    public string message;
    public InventorySlotResponse[] data;
}

[Serializable]
public class InventorySlotResponse
{
    public int slotIndex;
    public int itemId;
    public int amount;
}
