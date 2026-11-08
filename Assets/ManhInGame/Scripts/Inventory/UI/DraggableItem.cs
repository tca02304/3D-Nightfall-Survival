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
        if (sourceSlot == null)
            return;

        int srcIndex = sourceSlot.SlotIndex;

        CleanupDrag();

        InventoryManager.Instance.MoveItemAmount(srcIndex, targetSlotIndex, dragAmount);

        sourceSlot = null;
    }

    private void CleanupDrag()
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        if(dragGhost != null)
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
