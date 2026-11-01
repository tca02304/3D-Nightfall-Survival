using System;

[Serializable]
public class InventorySlotData
{
    public int ItemId;
    public string ItemCode;
    public string ItemName;

    public int Amount;
    public int MaxStack;

    public bool isEmpty()
    {
        return Amount <= 0 || string.IsNullOrEmpty(ItemCode);
    }

    public void Clear()
    {
        ItemId = 0;
        ItemCode = null;
        ItemName = null;
        Amount = 0;
        MaxStack = 0;
    }
}
