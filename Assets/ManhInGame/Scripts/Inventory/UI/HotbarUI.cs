using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class HotbarUI : MonoBehaviour
{
    [SerializeField] private Transform hotbarGrid;

    private readonly List<ItemSlot> hotbarSlots = new();
    private readonly List<GameObject> selectionHighlights = new();

    private InventoryManager subscribedInventory;
    private HotbarManager subscribedHotbar;
    private bool cacheReady;

    // Bind scene-local Hotbar UI to persistent gameplay managers whenever the panel opens.
    private void OnEnable()
    {
        CacheHotbarSlots();
        TryBindAndRefresh();
    }

    // Retry the initial binding after other Managers finish their initialization.
    private void Start()
    {
        TryBindAndRefresh();
    }

    // Recover subscriptions if a manager becomes available or is replaced after scene loading.
    private void LateUpdate()
    {
        if (subscribedInventory != InventoryManager.Instance ||
            subscribedHotbar != HotbarManager.Instance ||
            subscribedInventory == null || subscribedHotbar == null)
        {
            TryBindAndRefresh();
        }
    }

    // Disconnect inactive panels to avoid stale UI event handlers.
    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    // Disconnect event handlers before Unity destroys this scene-specific UI.
    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    // Collect all seven visual slots and assign their shared InventoryManager indices.
    private void CacheHotbarSlots()
    {
        hotbarSlots.Clear();
        selectionHighlights.Clear();
        cacheReady = false;

        InventoryManager inventory = InventoryManager.Instance;
        if (inventory == null || hotbarGrid == null)
            return;

        for (int i = 0; i < hotbarGrid.childCount && hotbarSlots.Count < inventory.HotbarSize; i++)
        {
            Transform child = hotbarGrid.GetChild(i);
            ItemSlot slot = child.GetComponent<ItemSlot>();
            if (slot == null)
                continue;

            slot.SetIndex(inventory.InventorySize + hotbarSlots.Count);
            hotbarSlots.Add(slot);

            Transform highlight = child.Find("SelectionHighlight");
            GameObject highlightObject = highlight != null ? highlight.gameObject : null;
            selectionHighlights.Add(highlightObject);

            if (highlightObject != null)
            {
                foreach (Graphic graphic in highlightObject.GetComponentsInChildren<Graphic>(true))
                    graphic.raycastTarget = false;
            }
        }

        cacheReady = hotbarSlots.Count == inventory.HotbarSize;
        if (!cacheReady)
            Debug.LogWarning($"[HotbarUI] Found {hotbarSlots.Count}/{inventory.HotbarSize} slots on {name}.", this);
    }

    // Subscribe to the current manager instances and force the panel to repaint immediately.
    private void TryBindAndRefresh()
    {
        InventoryManager inventory = InventoryManager.Instance;
        HotbarManager hotbar = HotbarManager.Instance;

        if (inventory == null || hotbar == null)
            return;

        if (!cacheReady)
            CacheHotbarSlots();

        if (subscribedInventory != inventory || subscribedHotbar != hotbar)
        {
            UnsubscribeEvents();
            subscribedInventory = inventory;
            subscribedHotbar = hotbar;
            subscribedInventory.OnInventoryChanged += RefreshHotbar;
            subscribedHotbar.OnItemSelected += RefreshSelection;
        }

        RefreshHotbar();
        RefreshSelection(hotbar.CurrentSlot);
    }

    // Unsubscribe from the same manager instances that supplied the event handlers.
    private void UnsubscribeEvents()
    {
        if (subscribedInventory != null)
            subscribedInventory.OnInventoryChanged -= RefreshHotbar;

        if (subscribedHotbar != null)
            subscribedHotbar.OnItemSelected -= RefreshSelection;

        subscribedInventory = null;
        subscribedHotbar = null;
    }

    // Repaint the visual Hotbar from the authoritative 7 slots in InventoryManager.
    private void RefreshHotbar()
    {
        InventoryManager inventory = InventoryManager.Instance;
        ItemDatabase database = ItemDatabase.Instance;
        if (inventory == null || database == null)
            return;

        for (int i = 0; i < hotbarSlots.Count; i++)
        {
            ItemSlot slot = hotbarSlots[i];
            if (slot == null)
                continue;

            int index = inventory.InventorySize + i;
            slot.SetIndex(index);

            ItemUI itemUI = slot.GetComponent<ItemUI>();
            if (itemUI == null)
                itemUI = slot.GetComponentInChildren<ItemUI>(true);

            if (itemUI == null)
            {
                Debug.LogWarning($"[HotbarUI] ItemUI missing: {slot.name}", slot);
                continue;
            }

            if (index >= inventory.Slots.Count)
            {
                itemUI.ClearItem();
                continue;
            }

            InventorySlotData data = inventory.Slots[index];
            if (data == null || data.isEmpty())
            {
                itemUI.ClearItem();
                continue;
            }

            ItemData item = database.GetItemById(data.ItemId);
            if (item == null)
            {
                Debug.LogWarning($"[HotbarUI] Unknown ItemId={data.ItemId} in slot {index}", slot);
                itemUI.ClearItem();
                continue;
            }

            itemUI.SetItem(item, data.Amount);
        }
    }

    // Show the selected Hotbar slot without blocking pointer events on item icons.
    private void RefreshSelection(int selectedIndex)
    {
        for (int i = 0; i < selectionHighlights.Count; i++)
        {
            GameObject highlight = selectionHighlights[i];
            if (highlight != null)
                highlight.SetActive(i == selectedIndex);
        }
    }
}
