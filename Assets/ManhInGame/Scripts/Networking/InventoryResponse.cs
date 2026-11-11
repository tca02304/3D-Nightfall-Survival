
using System;

[Serializable]
public class InventoryResponse
{
    public bool success;
    public string message;

    public InventorySlotResponse[] data;
    public InventorySlotResponse[] craftingSlots;
    public InventorySlotResponse cursorSlot;
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
    public SaveInventorySlot[] craftingSlots;
    public SaveInventorySlot cursorSlot;
}

[Serializable]
public class SaveInventorySlot
{
    public int slotIndex;
    public int itemId;
    public int amount;
}


[Serializable]
public class SaveInventoryRequestWithoutCursor
{
    public SaveInventorySlot[] slots;
    public SaveInventorySlot[] craftingSlots;
}
