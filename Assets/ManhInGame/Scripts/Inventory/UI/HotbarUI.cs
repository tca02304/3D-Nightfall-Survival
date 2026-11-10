using System.Collections.Generic;
using UnityEngine;

public class HotbarUI : MonoBehaviour
{
    [SerializeField] private Transform hotbarGrid;

    private readonly List<ItemSlot> hotbarSlots = new();
    private readonly List<GameObject> selectionHighlights = new();

    private bool isSubscibed;

    private void Start()
    {
        CacheHotbarSlots();

        SubscribeEvents();

        RefreshHotbar();

        if (HotbarManager.Instance != null)
        {
            RefreshSelection(HotbarManager.Instance.CurrentSlot);
        }
    }

    private void OnEnable()
    {
        
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void CacheHotbarSlots()
    {
        hotbarSlots.Clear();
        selectionHighlights.Clear();

        InventoryManager inventory = InventoryManager.Instance;

        if (inventory == null || hotbarGrid == null)
        {
            Debug.LogWarning("[HotbarUI] InventoryManager or HotbarGrid is missing.");
            return;
        }

        int inventorySize = inventory.InventorySize;
        int hotbarSize = inventory.HotbarSize;

        for (int i = 0; i < hotbarGrid.childCount; i++)
        {
            Transform child = hotbarGrid.GetChild(i);

            ItemSlot itemSlot = child.GetComponent<ItemSlot>();

            if (itemSlot == null)
                continue;

            if (hotbarSlots.Count >= hotbarSize)
                break;

            int dataIndex = inventorySize + hotbarSlots.Count;

            itemSlot.SetIndex(dataIndex);

            hotbarSlots.Add(itemSlot);

            Transform highlight = child.Find("SelectionHighlight");

            selectionHighlights.Add(
                highlight != null ? highlight.gameObject : null
            );

            Debug.Log(
                $"[HotbarUI] {child.name} assigned SlotIndex = {dataIndex}"
            );
        }

        Debug.Log($"[HotbarUI] Initialized {hotbarSlots.Count}/{hotbarSize} slots.");
    }

    private void SubscribeEvents()
    {
        if (isSubscibed)
            return;

        InventoryManager inventory = InventoryManager.Instance;
        HotbarManager hotbar = HotbarManager.Instance;

        if (inventory == null || hotbar == null)
        {
            Debug.LogWarning(
                "[HotbarUI] InventoryManager or HotbarManager not ready."
            );
            return;
        }

        inventory.OnInventoryChanged += RefreshHotbar;
        hotbar.OnItemSelected += RefreshSelection;

        isSubscibed = true;
    }

    private void UnsubscribeEvents()
    {
        if (!isSubscibed)
            return;

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= RefreshHotbar;
        }

        if (HotbarManager.Instance != null)
        {
            HotbarManager.Instance.OnItemSelected -= RefreshSelection;
        }

        isSubscibed = false;
    }

    private void RefreshHotbar()
    {
        InventoryManager inventory = InventoryManager.Instance;
        ItemDatabase database = ItemDatabase.Instance;

        if (inventory == null || database == null)
            return;

        for (int i = 0; i < hotbarSlots.Count; i++)
        {
            ItemSlot slot = hotbarSlots[i];

            int dataIndex = inventory.InventorySize + i;

            if (dataIndex >= inventory.Slots.Count)
                continue;

            InventorySlotData data = inventory.Slots[dataIndex];

            ItemUI itemUI = slot.GetComponentInChildren<ItemUI>(true);

            if (itemUI == null)
            {
                Debug.LogWarning(
                    $"[HotbarUI] Missing ItemUI at slot {dataIndex}"
                );
                continue;
            }

            if (data.isEmpty())
            {
                itemUI.ClearItem();
                continue;
            }

            ItemData item = database.GetItemById(data.ItemId);

            if (item == null)
            {
                Debug.LogWarning(
                    $"[HotbarUI] Item ID {data.ItemId} not found"
                );
                continue;
            }

            itemUI.SetItem(item, data.Amount);
        }
    }

    private void RefreshSelection(int selectedIndex)
    {
        for(int i = 0; i < selectionHighlights.Count; i++)
        {
            GameObject highlight = selectionHighlights[i];

            if (highlight == null)
                continue;

            highlight.SetActive(i == selectedIndex);
        }
    }
}