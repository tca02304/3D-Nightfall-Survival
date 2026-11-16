using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

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
    private InventoryWorldDropHandler worldDropHandler;

    public static DraggableItem ActiveDrag { get; private set; }
    public ItemSlot sourceSlot { get; private set; }
    private int dragAmount;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        itemUI = GetComponentInParent<ItemUI>();
        worldDropHandler = GetComponentInParent<InventoryWorldDropHandler>();
    }
    
    // Start dragging immediately without Unity's default movement threshold
    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        eventData.useDragThreshold = false;
    }

    // Begin a left-button drag only when the source Slot contains an Item
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        InventoryManager inventory = InventoryManager.Instance;
        if (inventory == null || !inventory.CursorSlot.isEmpty() || ActiveDrag != null)
            return;

        ItemSlot candidate = GetComponentInParent<ItemSlot>();
        if (candidate == null || candidate.SlotIndex < 0 || candidate.SlotIndex >= inventory.Slots.Count)
            return;

        InventorySlotData data = inventory.Slots[candidate.SlotIndex];
        if (data == null || data.isEmpty() || data.Amount <= 0 || canvas == null)
            return;

        sourceSlot = candidate;
        dragAmount = data.Amount;
        worldDropHandler = GetComponentInParent<InventoryWorldDropHandler>();

        CreateDragGhost();
        if(dragGhost == null)
        {
            sourceSlot = null;
            dragAmount = 0;
            return;
        }

        ActiveDrag = this;
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        UpdateGhostAmount();
        UpdateGhostPosition(eventData);
    }

    // Follow the pointer while an active drag is in progress
    public void OnDrag(PointerEventData eventData)
    {
        if(ActiveDrag == this)
        {
            UpdateGhostPosition(eventData);
        }
    }

    // Try a world drop only if no Slot or Crafting handler already completed the drag
    public void OnEndDrag(PointerEventData eventData)
    {
        // Try snapping a drop to a nearby slot inside a UI grid.
        if (ActiveDrag == this && sourceSlot != null)
        {
            DragGapSnapHandler[] handlers =
                FindObjectsByType<DragGapSnapHandler>(
                    FindObjectsSortMode.None);

            foreach (DragGapSnapHandler handler in handlers)
            {
                if (handler.TrySnapDrop(eventData))
                    break;
            }
        }

        if (ActiveDrag == this && sourceSlot != null && dragAmount > 0 &&
            eventData.button == PointerEventData.InputButton.Left && worldDropHandler != null)
        {
            worldDropHandler.TryDropOutsideUI(
                sourceSlot.SlotIndex,
                dragAmount,
                eventData.position,
                eventData.pressEventCamera);
        }

        CleanupDrag();
        sourceSlot = null;
        dragAmount = 0;
    }

    // Move the ghost using screen coordinates converted into the Canvas coordinate system
    private void UpdateGhostPosition(PointerEventData eventData)
    {
        if (dragGhostRect == null || canvas == null)
            return;

        RectTransform canvasRect = canvas.transform as RectTransform;
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera != null ? canvas.worldCamera : eventData.pressEventCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, eventData.position, camera, out Vector2 localPoint))
        {
            dragGhostRect.localPosition = localPoint;
        }
    }

    // Clone only ItemIcon as a non-raycasting visual drag ghost
    private void CreateDragGhost()
    {
        dragGhost = Instantiate(gameObject, canvas.transform);
        dragGhost.name = "DragGhost";
        dragGhostRect = dragGhost.GetComponent<RectTransform>();
        dragGhost.transform.SetAsLastSibling();

        DraggableItem ghostDraggable = dragGhost.GetComponent<DraggableItem>();
        if (ghostDraggable != null)
        {
            ghostDraggable.enabled = false;
            Destroy(ghostDraggable);
        }

        CanvasGroup ghostGroup = dragGhost.GetComponent<CanvasGroup>();
        if(ghostGroup != null)
        {
            ghostGroup.alpha = 1f;
            ghostGroup.interactable = false;
            ghostGroup.blocksRaycasts = false;
        }

        // The ghost contains ItemIcon/AmountText but no ItemUI component on its parent
        foreach (UnityEngine.UI.Graphic graphic in dragGhost.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
            graphic.raycastTarget = false;
    }

    // Refresh the AmountText that lives inside the cloned ItemIcon instead of searching for ItemUI
    public void UpdateGhostAmount()
    {
        if (dragGhost == null)
            return;

        TMP_Text amountLabel = dragGhost.GetComponentInChildren<TMP_Text>(true);
        if (amountLabel != null)
            amountLabel.text = dragAmount > 1 ? dragAmount.ToString() : string.Empty;

        if (dragAmount <= 0)
            dragGhost.SetActive(false);
    }

    // Complete the existing full-stack Slot transfer without changing Inventory transfer rules
    public void CompleteDrop(int targetSlotIndex)
    {
        if (ActiveDrag != this || sourceSlot == null || dragAmount <= 0)
            return;

        int sourceIndex = sourceSlot.SlotIndex;
        int amount = dragAmount;
        CleanupDrag();
        sourceSlot = null;
        dragAmount = 0;

        InventoryManager inventory = InventoryManager.Instance;
        if (inventory != null)
            inventory.MoveItemAmount(sourceIndex, targetSlotIndex, amount);
    }

    // Transfer exactly one Item into an empty Slot or a compatible stack
    public bool TryPlaceOneOnSlot(int targetSlotIndex)
    {
        InventoryManager inventory = InventoryManager.Instance;
        if (inventory == null || sourceSlot == null || ActiveDrag != this ||
            dragAmount <= 0 || !inventory.CursorSlot.isEmpty())
            return false;

        int sourceIndex = sourceSlot.SlotIndex;
        if (sourceIndex < 0 || sourceIndex >= inventory.Slots.Count ||
            targetSlotIndex < 0 || targetSlotIndex >= inventory.Slots.Count ||
            sourceIndex == targetSlotIndex)
            return false;

        InventorySlotData source = inventory.Slots[sourceIndex];
        InventorySlotData target = inventory.Slots[targetSlotIndex];
        if (source == null || target == null || source.isEmpty() || source.Amount <= 0)
            return false;

        if (!target.isEmpty() &&
            (target.ItemId != source.ItemId || target.Amount >= target.MaxStack))
            return false;

        if (!inventory.MoveItemAmount(sourceIndex, targetSlotIndex, 1))
            return false;

        dragAmount = Mathf.Min(Mathf.Max(0, dragAmount - 1), inventory.Slots[sourceIndex].Amount);
        UpdateGhostAmount();
        return true;
    }

    // Transfer exactly one Item from Inventory or Hotbar to the selected Crafting Cell
    public bool TryPlaceOneOnCraftCell(GridCraftingController controller, int gridIndex)
    {
        InventoryManager inventory = InventoryManager.Instance;
        if (controller == null || inventory == null || sourceSlot == null ||
            ActiveDrag != this || dragAmount <= 0 || !inventory.CursorSlot.isEmpty())
            return false;

        int sourceIndex = sourceSlot.SlotIndex;
        if (sourceIndex < 0 || sourceIndex >= inventory.Slots.Count)
            return false;

        InventorySlotData source = inventory.Slots[sourceIndex];
        if (source == null || source.isEmpty())
            return false;

        int transferred = controller.TryMoveAmountFromInventory(sourceIndex, gridIndex, 1);
        if (transferred != 1)
            return false;

        dragAmount = Mathf.Min(Mathf.Max(0, dragAmount - 1), inventory.Slots[sourceIndex].Amount);
        UpdateGhostAmount();
        return true;
    }

    // Restore the original Icon and remove its ghost without affecting Inventory data
    private void CleanupDrag()
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }

        if (ActiveDrag == this)
            ActiveDrag = null;

        if (dragGhost != null)
            Destroy(dragGhost);

        dragGhost = null;
        dragGhostRect = null;
    }

    private void OnDisable()
    {
        CleanupDrag();
    }
}
