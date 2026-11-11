using UnityEngine;

public class CraftingInventoryTransferService : MonoBehaviour
{
    public static bool TryTakeAmount(int sourceIndex, int amount, out int itemId)
    {
        itemId = 0;
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null || !inv.CursorSlot.isEmpty() || amount <= 0 ||
            sourceIndex < 0 || sourceIndex >= inv.Slots.Count) return false;
        InventorySlotData slot = inv.Slots[sourceIndex];
        if (slot == null || slot.isEmpty() || slot.Amount < amount) return false;

        itemId = slot.ItemId;
        slot.Amount -= amount;
        if (slot.Amount == 0) slot.Clear();
        inv.NotifyChanged();
        inv.NotifyRemoved(itemId, amount);
        return true;
    }

    public static bool TryTakeOne(int sourceIndex, out int itemId) =>
        TryTakeAmount(sourceIndex, 1, out itemId);

    public static bool CanPlace(int targetIndex, int itemId, int amount)
    {
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null || !inv.CursorSlot.isEmpty() || amount <= 0 ||
            targetIndex < 0 || targetIndex >= inv.Slots.Count) return false;

        ItemData item = inv.GetItemData(itemId);
        if (item == null || item.maxStack < amount) return false;
        InventorySlotData slot = inv.Slots[targetIndex];
        if (slot == null) return false;
        if (slot.isEmpty()) return true;
        return slot.ItemId == itemId && slot.Amount <= item.maxStack - amount;
    }

    public static bool TryPlace(int targetIndex, int itemId, int amount)
    {
        if (!CanPlace(targetIndex, itemId, amount)) return false;

        InventoryManager inv = InventoryManager.Instance;
        ItemData item = inv.GetItemData(itemId);
        InventorySlotData slot = inv.Slots[targetIndex];

        if (slot.isEmpty())
        {
            slot.ItemId = item.id;
            slot.ItemCode = item.itemCode;
            slot.ItemName = item.name;
            slot.MaxStack = item.maxStack;
            slot.Amount = amount;
        }
        else slot.Amount += amount;

        inv.NotifyChanged();
        inv.NotifyAdded(itemId, amount);
        return true;
    }

    public static bool TryPlaceAnywhere(int itemId, int amount)
    {
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null || amount <= 0) return false;
        for (int i = 0; i < inv.Slots.Count; i++)
            if (!inv.Slots[i].isEmpty() && inv.Slots[i].ItemId == itemId &&
                CanPlace(i, itemId, amount)) return TryPlace(i, itemId, amount);
        for (int i = 0; i < inv.Slots.Count; i++)
            if (inv.Slots[i].isEmpty() && CanPlace(i, itemId, amount))
                return TryPlace(i, itemId, amount);
        return false;
    }
}
