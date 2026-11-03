using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Unity-facing facade. Gameplay scripts depend on this API; services own business rules.
/// Only this class is a MonoBehaviour and is added to the Managers GameObject.
/// </summary>
public sealed class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Inventory Layout (keep aligned with UI and backend)")]
    [SerializeField, Min(1)] private int inventorySize = 32;
    [SerializeField, Min(0)] private int hotbarSize = 7;

    public int InventorySize => inventorySize;
    public int HotbarSize => hotbarSize;

    // Compatibility: existing code reads Slots[index] and CursorSlot directly.
    // Avoid mutating them outside the inventory service layer in new code.
    public List<InventorySlotData> Slots { get; } = new List<InventorySlotData>();
    public InventorySlotData CursorSlot { get; } = new InventorySlotData();

    public event Action<int, int> OnItemAdded;
    public event Action<int, int> OnItemRemoved;
    public event Action OnInventoryChanged;

    private InventoryItemService items;
    private InventoryTransferService transfers;
    private InventoryCursorService cursor;
    private InventoryExchangeService exchange;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        int total = inventorySize + hotbarSize;
        for (int i = 0; i < total; i++) Slots.Add(new InventorySlotData());

        items = new InventoryItemService(this);
        transfers = new InventoryTransferService(this);
        cursor = new InventoryCursorService(this);
        exchange = new InventoryExchangeService(this, items);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    internal void NotifyChanged() => OnInventoryChanged?.Invoke();
    internal void NotifyAdded(int id, int amount) => OnItemAdded?.Invoke(id, amount);
    internal void NotifyRemoved(int id, int amount) => OnItemRemoved?.Invoke(id, amount);

    public int AddItem(ItemData item, int amount = 1) => items.AddItem(item, amount);
    public bool RemoveItemAt(int slotIndex, int amount = 1) => items.RemoveItemAt(slotIndex, amount);
    public InventorySlotData DropItem(int slotIndex, int amount = 1) => items.DropItem(slotIndex, amount);
    public int FindItem(int itemId) => items.FindItem(itemId);
    public bool CheckItem(int itemId) => items.CheckItem(itemId);
    public int CountItem(int itemId) => items.CountItem(itemId);
    public ItemData GetItemData(int itemId) => items.GetItemData(itemId);

    public bool MoveItemAmount(int fromIndex, int toIndex, int amount) => transfers.MoveItemAmount(fromIndex, toIndex, amount);
    public bool MoveSwapOrMergeItem(int fromIndex, int toIndex) => transfers.MoveSwapOrMergeItem(fromIndex, toIndex);
    public bool SplitStack(int slotIndex, int splitAmount) => transfers.SplitStack(slotIndex, splitAmount);
    public bool SplitStackHalf(int slotIndex) => transfers.SplitStackHalf(slotIndex);

    public void HandleRightClick(int slotIndex) => cursor.HandleRightClick(slotIndex);
    public void HandleLeftClick(int slotIndex) => cursor.HandleLeftClick(slotIndex);

    public bool CanExchangeItemsForItem(IReadOnlyDictionary<int, int> itemsToRemove, ItemData outputItem, int outputAmount)
        => exchange.CanExchangeItemsForItem(itemsToRemove, outputItem, outputAmount);

    public bool TryExchangeItemsForItem(IReadOnlyDictionary<int, int> itemsToRemove, ItemData outputItem, int outputAmount)
        => exchange.TryExchangeItemsForItem(itemsToRemove, outputItem, outputAmount);

    /// <summary>Atomically replaces local slots after validation. Call only after item definitions are loaded.</summary>
    public bool TryLoadSlots(IReadOnlyList<InventorySlotRecord> records)
    {
        if (records == null || !CursorSlot.isEmpty()) return false;
        var used = new HashSet<int>();
        foreach (var record in records)
        {
            if (record.SlotIndex < 0 || record.SlotIndex >= Slots.Count || !used.Add(record.SlotIndex)) return false;
            ItemData item = GetItemData(record.ItemId);
            if (item == null || record.Amount <= 0 || record.Amount > item.maxStack) return false;
        }

        foreach (var slot in Slots) slot.Clear();
        foreach (var record in records)
        {
            var item = GetItemData(record.ItemId);
            var slot = Slots[record.SlotIndex];
            slot.ItemId = item.id;
            slot.ItemCode = item.itemCode;
            slot.ItemName = item.name;
            slot.MaxStack = item.maxStack;
            slot.Amount = record.Amount;
        }
        NotifyChanged();
        return true;
    }
}