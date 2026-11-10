using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class DraggableItem : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IInitializePotentialDragHandler
{
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private ItemUI itemUI;
    private GameObject dragGhost;
    private RectTransform dragGhostRect;

    public static DraggableItem ActiveDrag { get; private set; }

    public ItemSlot sourceSlot { get; private set; }
    private int dragAmount;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        itemUI = GetComponent<ItemUI>();
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        eventData.useDragThreshold = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (InventoryManager.Instance != null && !InventoryManager.Instance.CursorSlot.isEmpty())
            return;

        sourceSlot = GetComponentInParent<ItemSlot>();

        if (sourceSlot == null)
            return;

        InventorySlotData slotData = InventoryManager.Instance.Slots[sourceSlot.SlotIndex];

        if (slotData.isEmpty())
            return;

        dragAmount = slotData.Amount;
        canvasGroup.alpha = 0f;

        CreateDragGhost();
        ActiveDrag = this;

        ItemUI ghostUI = dragGhost.GetComponent<ItemUI>();

        if (ghostUI != null)
            ghostUI.SetAmount(dragAmount);

        UpdateGhostPosition(eventData);
        canvasGroup.blocksRaycasts = false; 
    }

    public void OnDrag(PointerEventData eventData)
    {
        UpdateGhostPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        CleanupDrag();
        sourceSlot = null;
    }

    private void UpdateGhostPosition(PointerEventData evnetData)
    {
        if (dragGhostRect == null || canvas == null)
            return;

        RectTransform canvasRect = canvas.transform as RectTransform;

        if(RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            evnetData.position,
            evnetData.pressEventCamera,
            out Vector2 localPoint))
        {
            dragGhostRect.localPosition = localPoint;
        }
    }

    private void CreateDragGhost()
    {
        dragGhost = Instantiate(gameObject, canvas.transform);

        dragGhost.name = "DragGhost";

        dragGhostRect = dragGhost.GetComponent<RectTransform>();

        dragGhost.transform.SetAsLastSibling();

        DraggableItem ghostDraggable = dragGhost.GetComponent<DraggableItem>();

        if (ghostDraggable != null)
            Destroy(ghostDraggable);

        CanvasGroup ghostCanvasGroup = dragGhost.GetComponent<CanvasGroup>();

        if(ghostCanvasGroup != null)
        {
            ghostCanvasGroup.blocksRaycasts = false;
            ghostCanvasGroup.alpha = 1f;
        }
    }

    public void CompleteDrop(int targetSlotIndex)
    {
        if (sourceSlot == null || dragAmount <= 0)
            return;

        int srcIndex = sourceSlot.SlotIndex;

        CleanupDrag();

        InventoryManager.Instance.MoveItemAmount(srcIndex, targetSlotIndex, dragAmount);

        sourceSlot = null;
    }


    public bool TryPlaceOneOnSlot(int targetSlotIndex)
    {
        InventoryManager inventory = InventoryManager.Instance;

        if (inventory == null || sourceSlot == null)
            return false;

        if (ActiveDrag != this || dragAmount <= 0)
            return false;

        if (!inventory.CursorSlot.isEmpty())
            return false;

        int sourceIndex = sourceSlot.SlotIndex;

        if (sourceIndex < 0 ||
            sourceIndex >= inventory.Slots.Count ||
            targetSlotIndex < 0 ||
            targetSlotIndex >= inventory.Slots.Count ||
            sourceIndex == targetSlotIndex)
            return false;

        InventorySlotData source = inventory.Slots[sourceIndex];
        InventorySlotData target = inventory.Slots[targetSlotIndex];

        if (source.isEmpty() || source.Amount <= 0)
            return false;

        // Chỉ cho đặt vào ô trống hoặc stack cùng loại.
        // Không swap Item khác loại khi thả từng 1.
        if (!target.isEmpty())
        {
            if (target.ItemId != source.ItemId)
                return false;

            if (target.Amount >= target.MaxStack)
                return false;
        }

        // Lưu số lượng trước khi chuyển.
        int remaining = source.Amount;

        bool success = inventory.MoveItemAmount(
            sourceIndex,
            targetSlotIndex,
            1
        );

        if (!success)
            return false;

        dragAmount = Mathf.Min(
            Mathf.Max(0, remaining - 1),
            inventory.Slots[sourceIndex].Amount
        );

        // Cập nhật số lượng còn lại trên DragGhost.
        if (dragGhost != null)
        {
            ItemUI ghostUI = dragGhost.GetComponent<ItemUI>();

            if (ghostUI != null)
                ghostUI.SetAmount(dragAmount);

            if (dragAmount <= 0)
                dragGhost.SetActive(false);
        }

        return true;
    }


    public bool TryPlaceOneOnCraftCell(
        GridCraftingController controller,
        int gridIndex)
    {
        InventoryManager inventory = InventoryManager.Instance;

        if (controller == null ||
            inventory == null ||
            sourceSlot == null ||
            ActiveDrag != this ||
            dragAmount <= 0)
        {
            return false;
        }

        if (!inventory.CursorSlot.isEmpty())
            return false;

        int sourceIndex = sourceSlot.SlotIndex;

        if (sourceIndex < 0 ||
            sourceIndex >= inventory.Slots.Count)
        {
            return false;
        }

        InventorySlotData source =
            inventory.Slots[sourceIndex];

        if (source.isEmpty())
            return false;

        // Chuyển đúng 1 Item từ Inventory vào Crafting.
        int transferred = controller.TryMoveAmountFromInventory(
            sourceIndex,
            gridIndex,
            1
        );

        if (transferred != 1)
            return false;

        // Cập nhật số lượng đang kéo.
        dragAmount = Mathf.Min(
            Mathf.Max(0, dragAmount - 1),
            inventory.Slots[sourceIndex].Amount
        );

        // Cập nhật số lượng trên DragGhost.
        if (dragGhost != null)
        {
            ItemUI ghostUI = dragGhost.GetComponent<ItemUI>();

            if (ghostUI != null)
                ghostUI.SetAmount(dragAmount);

            if (dragAmount <= 0)
                dragGhost.SetActive(false);
        }

        return true;
    }



    private void CleanupDrag()
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        if (ActiveDrag == this)
        {
            ActiveDrag = null;
        }

        if (dragGhost != null)
        {
            Destroy(dragGhost);
        }

        dragGhost = null;
    }

    private void OnDisable()
    {
        CleanupDrag();
    }
}
