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

    private GameObject dragGhost;
    private RectTransform dragGhostRect;

    private ItemSlot sourceSlot;

    public ItemSlot SourceSlot => sourceSlot;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        eventData.useDragThreshold = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        sourceSlot = GetComponentInParent<ItemSlot>();

        if (sourceSlot == null)
            return;

        CreateDragGhost();
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

        if(ghostDraggable != null)
        {
            ghostDraggable.enabled = false;
        }

        CanvasGroup ghostCanvasGroup = dragGhost.GetComponent<CanvasGroup>();

        if(ghostCanvasGroup != null)
        {
            ghostCanvasGroup.blocksRaycasts = false;
            ghostCanvasGroup.interactable = false;
        }

        Graphic[] graphics = dragGhost.GetComponentsInChildren<Graphic>(true);

        foreach(Graphic graphic in graphics)
        {
            graphic.raycastTarget = false;
        }
    }

    public void CompleteDrop(int targetSlotIndex)
    {
        if (sourceSlot == null)
            return;

        int sourceSlotIndex = sourceSlot.SlotIndex;

        CleanupDrag();

        InventoryManager.Instance.MoveSwapOrMergeItem(sourceSlotIndex, targetSlotIndex);

        sourceSlot = null;
    }

    private void CleanupDrag()
    {
        canvasGroup.blocksRaycasts = true;

        if(dragGhost != null)
        {
            Destroy(dragGhost);
        }

        dragGhost = null;
        dragGhostRect = null;
    }
}
