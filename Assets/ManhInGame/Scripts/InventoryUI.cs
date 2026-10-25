using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Transform inventoryGrid;

    private readonly List<ItemSlot> inventorySlots = new();

    private bool isSubscribed;

    private void Awake()
    {
        CacheInventorySlots();
    }

    void Start()
    {
        SubscribeInventoryEvent();
        RefreshInventory();
    }

    private void OnEnable()
    {
        SubscribeInventoryEvent();

        if(InventoryManager.Instance != null)
        {
            RefreshInventory();
        }
    }

    private void OnDisable()
    {
        UnsubscribeInventoryEvent();
    }

    private void CacheInventorySlots()
    {
        inventorySlots.Clear();

        for (int i = 0; i < inventoryGrid.childCount; i++)
        {
            Transform child = inventoryGrid.GetChild(i);

            ItemSlot itemSlot = child.GetComponent<ItemSlot>();

            if (itemSlot == null)
                continue;

            int slotIndex = inventorySlots.Count;

            itemSlot.SetIndex(slotIndex);

            inventorySlots.Add(itemSlot);
        }

        Debug.Log("Inventory UI found: " + inventorySlots.Count + "slot");
    }

    private void SubscribeInventoryEvent()
    {
        if (InventoryManager.Instance == null || isSubscribed)
            return;

        InventoryManager.Instance.OnInventoryChanged += RefreshInventory;

        isSubscribed = true;
    }

    private void UnsubscribeInventoryEvent()
    {
        if (InventoryManager.Instance == null || !isSubscribed)
            return;

        InventoryManager.Instance.OnInventoryChanged -= RefreshInventory;

        isSubscribed = false;
    }

    private void RefreshInventory()
    {
        if (InventoryManager.Instance == null)
            return; 

        for(int i = 0; i < inventorySlots.Count; i++)
        {
            ItemSlot slot = inventorySlots[i];

            ItemUI itemUI = slot.GetComponentInChildren<ItemUI>(true);
            
            if(itemUI == null)
            {
                Debug.LogWarning("Không tìm thấy ItemUI trong slot: " + slot.name);

                continue;
            }

            GameObject itemIcon = itemUI.gameObject;

            if (i >= InventoryManager.Instance.Slots.Count)
            {
                itemIcon.SetActive(false);
                continue;
            }

            InventorySlotData data = InventoryManager.Instance.Slots[i];

            if (data.isEmpty())
            {
                itemIcon.SetActive(false);
                continue;
            }

            itemIcon.SetActive(true);

            itemUI.SetAmount(data.Amount);
        }
    }
}
