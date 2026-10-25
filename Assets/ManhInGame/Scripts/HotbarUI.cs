using System.Collections.Generic;
using UnityEngine;

public class HotbarUI : MonoBehaviour
{
    [SerializeField] private Transform hotbarGrid;

    private readonly List<ItemSlot> hotbarSlots = new();
    private readonly List<GameObject> selectionHighlights = new();

    private bool isSubscibed;

    private void Awake()
    {
        CacheHotbarSlots();
    }

    private void OnEnable()
    {
        SubscribeEvents();

        if (InventoryManager.Instance != null)
        {
            RefreshHotbar();
        }

        if(HotbarManager.Instance != null)
        {
            RefreshSelection(HotbarManager.Instance.CurrentSlot);
        }
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void CacheHotbarSlots()
    {
        hotbarSlots.Clear();
        selectionHighlights.Clear();

        if (InventoryManager.Instance == null)
            return;

        int inventorySize = InventoryManager.Instance.InventorySize;

        for (int i = 0; i < hotbarGrid.childCount; i++)
        {
            Transform child = hotbarGrid.GetChild(i);

            ItemSlot itemSlot = child.GetComponent<ItemSlot>();

            if (itemSlot == null)
                continue;

            int dataIndex = inventorySize + hotbarSlots.Count;

            itemSlot.SetIndex(dataIndex);

            hotbarSlots.Add(itemSlot);

            Transform highlight = child.Find("SelectionHighlight");

            if(highlight != null)
            {
                selectionHighlights.Add(highlight.gameObject);
            }
            else
            {
                selectionHighlights.Add(null);
            }
        }

        Debug.Log("Hotbar UI found: " + hotbarSlots.Count + " slots");
    }

    private void SubscribeEvents()
    {
        if (isSubscibed)
            return;

        if(InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += RefreshHotbar;
        }

        if(HotbarManager.Instance != null)
        {
            HotbarManager.Instance.OnItemSelected += RefreshSelection;
        }

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
        if (InventoryManager.Instance == null)
            return;

        for(int i = 0; i < hotbarSlots.Count; i++)
        {
            ItemSlot slot = hotbarSlots[i];

            ItemUI itemUI = slot.GetComponentInChildren<ItemUI>(true);

            if (itemUI == null)
                continue;

            GameObject itemIcon = itemUI.gameObject;

            int dataIndex = InventoryManager.Instance.InventorySize + i;

            InventorySlotData data = InventoryManager.Instance.Slots[dataIndex];

            if (data.isEmpty())
            {
                itemIcon.SetActive(false);
                continue;
            }

            itemIcon.SetActive(true);

            itemUI.SetAmount(data.Amount);
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