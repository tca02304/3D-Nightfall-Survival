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
        if (cursorObj == null || !cursorObj.activeInHierarchy)
            return;

        if (cursorRect == null)
            return;

        RectTransform parentRect =
            cursorRect.parent as RectTransform;

        if (parentRect == null)
            return;

        Canvas rootCanvas = cursorObj.GetComponentInParent<Canvas>();

        if (rootCanvas == null)
            return;

        Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : rootCanvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            Input.mousePosition,
            uiCamera,
            out Vector2 localPoint))
        {
            cursorRect.localPosition = localPoint;
        }
    }


    private void RefreshInventory()
    {
        InventoryManager inventory = InventoryManager.Instance;

        if (inventory == null)
            return;

        for (int i = 0; i < inventorySlots.Count; i++)
        {
            ItemSlot slot = inventorySlots[i];

            if (slot == null)
                continue;

            // [FIX] ItemUI is on InvSlot.
            ItemUI itemUI =
                slot.GetComponent<ItemUI>();

            if (itemUI == null)
            {
                Debug.LogWarning(
                    $"[InventoryUI] Missing ItemUI: {slot.name}",
                    slot);
                continue;
            }

            // [FIX] Never disable InvSlot itself.
            if (i >= inventory.InventorySize ||
                i >= inventory.Slots.Count)
            {
                itemUI.ClearItem();
                continue;
            }

            InventorySlotData data = inventory.Slots[i];

            if (data == null || data.isEmpty())
            {
                // ClearItem hides the icon, not the slot.
                itemUI.ClearItem();
                continue;
            }

            ItemData itemData =
                inventory.GetItemData(data.ItemId);

            if (itemData == null)
            {
                Debug.LogWarning(
                    $"[InventoryUI] ItemId={data.ItemId} not found",
                    slot);

                itemUI.ClearItem();
                continue;
            }

            // [FIX] Show sprite and amount.
            itemUI.SetItem(itemData, data.Amount);
        }

        // [KEEP] Existing shared CursorSlot UI logic.
        if (cursorObj != null && cursorUI != null)
        {
            InventorySlotData cursorData =
                inventory.CursorSlot;

            if (cursorData == null || cursorData.isEmpty())
            {
                cursorUI.ClearItem();
                cursorObj.SetActive(false);
            }
            else
            {
                ItemData itemData =
                    inventory.GetItemData(cursorData.ItemId);

                if (itemData != null)
                {
                    cursorObj.SetActive(true);
                    cursorUI.SetItem(
                        itemData,
                        cursorData.Amount);
                }
                else
                {
                    cursorUI.ClearItem();
                    cursorObj.SetActive(false);
                }
            }
        }
    }

}
