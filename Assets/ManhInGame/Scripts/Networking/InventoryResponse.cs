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

[Serializable]
public class SaveInventoryRequest
{
    public SaveInventorySlot[] slots;
}

[Serializable]
public class SaveInventorySlot
{
    public int slotIndex;
    public int itemId;
    public int amount;
}