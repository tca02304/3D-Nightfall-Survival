using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private GameObject itemPrefab;

    [SerializeField] private Transform inventoryGrid;
    [SerializeField] private Transform hotbarGrid;

    [SerializeField] private int inventorySlotCount = 28;
    [SerializeField] private int hotbarSlotCount = 7;

    private List<Transform> inventorySlots = new();

    void Start()
    {
        CreateInventorySlots();
        CreateHotbarSlots();

        InventoryManager.Instance.OnInventoryChanged += RefreshInventory;

        RefreshInventory();
    }

    private void CreateInventorySlots()
    {
        for(int i = 0; i < inventorySlotCount; i++)
        {
            GameObject slot = Instantiate(slotPrefab, inventoryGrid);

            ItemSlot itemSlot = slot.GetComponent<ItemSlot>();
            itemSlot.SetIndex(i);

            inventorySlots.Add(slot.transform);
        }
    }

    private void CreateHotbarSlots()
    {
        for(int i = 0; i < hotbarSlotCount; i++)
        {
            GameObject slot = Instantiate(slotPrefab, hotbarGrid);

            ItemSlot itemSlot = slot.GetComponent<ItemSlot>();

            int slotIndex = inventorySlotCount + i;

            itemSlot.SetIndex(slotIndex);
        }
    }

    private void RefreshInventory()
    {
        for(int i = 0; i < inventorySlots.Count; i++)
        {
            InventorySlotData data = InventoryManager.Instance.Slots[i];
            Transform slotUI = inventorySlots[i];

            foreach(Transform child in slotUI)
            {
                if (child.GetComponent<DraggableItem>() != null)
                {
                    Destroy(child.gameObject);
                }
            }

            if (data.isEmpty())
                continue;

            GameObject item = Instantiate(itemPrefab, slotUI);

            RectTransform rect = item.GetComponent<RectTransform>();
            rect.anchoredPosition = Vector2.zero;

            ItemUI itemUI = item.GetComponent<ItemUI>();
            itemUI.SetAmount(data.Amount);
        }
    }

    private void OnDestroy()
    {
        if(InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += RefreshInventory;
        }
    }
}
