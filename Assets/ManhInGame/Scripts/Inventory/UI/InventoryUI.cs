using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Transform inventoryGrid;

    [Header("Cursor UI (Item dính theo chuột)")]
    [SerializeField] private GameObject cursorObj;
    [SerializeField] private ItemUI cursorUI;

    private RectTransform cursorRect;
    private Canvas canvas;
    private readonly List<ItemSlot> inventorySlots = new();
    private bool isSubscribed;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        if (cursorObj != null)
        {
            cursorRect = cursorObj.GetComponent<RectTransform>();

            // 1. Tự động thêm CanvasGroup và khóa Raycast
            CanvasGroup cg = cursorObj.GetComponent<CanvasGroup>();
            if (cg == null) cg = cursorObj.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;

            // 2. Dùng code quét tất cả Text/Image bên trong con trỏ và TẮT CHẶN CLICK
            UnityEngine.UI.Graphic[] graphics = cursorObj.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
            foreach (var g in graphics)
            {
                g.raycastTarget = false;
            }
        }
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

    private void Update()
    {
        if (cursorObj != null && cursorObj.activeSelf && canvas != null)
        {
            // 3. Ép ảnh con trỏ luôn nằm TRÊN CÙNG, không bị các ô đồ che khuất
            cursorObj.transform.SetAsLastSibling();

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                Input.mousePosition,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out Vector2 localPoint);

            cursorRect.localPosition = localPoint;
        }
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
                itemUI.ClearItem();
                itemIcon.SetActive(false);
                continue;
            }

            InventorySlotData data = InventoryManager.Instance.Slots[i];

            if (data.isEmpty())
            {
                itemUI.ClearItem();
                itemIcon.SetActive(false);
                continue;
            }

            ItemData itemData = InventoryManager.Instance.GetItemData(data.ItemId);

            if(itemData == null)
            {
                Debug.LogWarning("Không tìm thấy ItemData với ID: " + data.ItemId);
                itemUI.ClearItem();
                itemIcon.SetActive(false);
                continue;
            }

            itemIcon.SetActive(true);

            itemUI.SetItem(itemData, data.Amount);
        }

        if(cursorObj != null && cursorUI != null)
        {
            InventorySlotData cursorData = InventoryManager.Instance.CursorSlot;

            if (cursorData.isEmpty())
            {
                cursorObj.SetActive(false);
                cursorUI.ClearItem();
            }
            else
            {
                cursorObj.SetActive(true);
                ItemData itemData = InventoryManager.Instance.GetItemData(cursorData.ItemId);
                if (itemData != null)
                    cursorUI.SetItem(itemData, cursorData.Amount);
            }
        }
    }
}
