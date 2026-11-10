using System;
using System.Collections.Generic;
using UnityEngine;

namespace DucThanh
{
    /// <summary>
    /// ScriptableObject cấu hình kho đồ / danh mục vật phẩm và thông số trang bị.
    /// Hoạt động tương tự như NightSpawnerConfigSO: Bạn chỉ cần add các ItemConfigSO vào đây
    /// và thiết lập các thông số mặc định mà không cần động đến code.
    /// </summary>
    [CreateAssetMenu(fileName = "InventoryDatabaseConfig", menuName = "DucThanh/Inventory/Inventory Database Config")]
    public class InventoryDatabaseConfigSO : ScriptableObject
    {
        [Header("Inventory Capacity")]
        [Tooltip("Số lượng ô chứa đồ tối đa trong túi")]
        [Range(1, 40)]
        [SerializeField] private int maxSlots = 8;

        [Header("Starting Items (Vật phẩm có sẵn khi bắt đầu game)")]
        [Tooltip("Danh sách các vật phẩm được thêm vào túi khi player bắt đầu chơi")]
        [SerializeField] private List<StartingItemEntry> startingItems = new List<StartingItemEntry>();

        [Header("Registered Item Database (Danh mục các vật phẩm trong game)")]
        [Tooltip("Toàn bộ các loại vật phẩm được đăng ký trong game để tìm kiếm hoặc spawn nhanh")]
        [SerializeField] private List<ItemConfigSO> registeredItems = new List<ItemConfigSO>();

        public int MaxSlots => maxSlots;
        public List<StartingItemEntry> StartingItems => startingItems;
        public List<ItemConfigSO> RegisteredItems => registeredItems;

        public ItemConfigSO GetItemById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return registeredItems.Find(item => item != null && item.ItemId.Equals(id, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Serializable]
    public class StartingItemEntry
    {
        [Tooltip("Cấu hình vật phẩm")]
        public ItemConfigSO itemConfig;

        [Tooltip("Số lượng nhận khi bắt đầu")]
        [Range(1, 99)]
        public int amount = 1;

        [Tooltip("Tự động cầm trang bị lên tay khi bắt đầu game")]
        public bool equipAtStart = false;
    }
}
