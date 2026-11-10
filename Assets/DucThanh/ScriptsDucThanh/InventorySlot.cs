using System;
using UnityEngine;

namespace DucThanh
{
    /// <summary>
    /// Đại diện cho 1 ô chứa trong túi đồ (Slot)
    /// </summary>
    [Serializable]
    public class InventorySlot
    {
        [SerializeField] private ItemConfigSO item;
        [SerializeField] private int quantity;

        public ItemConfigSO Item => item;
        public int Quantity => quantity;
        public bool IsEmpty => item == null || quantity <= 0;

        public InventorySlot()
        {
            item = null;
            quantity = 0;
        }

        public InventorySlot(ItemConfigSO item, int quantity)
        {
            this.item = item;
            this.quantity = Mathf.Max(0, quantity);
        }

        public void SetItem(ItemConfigSO newItem, int newQuantity)
        {
            item = newItem;
            quantity = Mathf.Max(0, newQuantity);
            if (quantity == 0)
            {
                item = null;
            }
        }

        public int AddQuantity(int amount)
        {
            if (item == null) return amount;
            int availableSpace = item.MaxStack - quantity;
            int added = Mathf.Min(availableSpace, amount);
            quantity += added;
            return amount - added; // Trả về số lượng còn dư không thể nhét thêm
        }

        public bool RemoveQuantity(int amount)
        {
            if (quantity < amount) return false;
            quantity -= amount;
            if (quantity <= 0)
            {
                Clear();
            }
            return true;
        }

        public void Clear()
        {
            item = null;
            quantity = 0;
        }
    }
}
