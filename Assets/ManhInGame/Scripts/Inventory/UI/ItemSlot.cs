using UnityEngine;
using UnityEngine.EventSystems;

public class ItemSlot : MonoBehaviour, IDropHandler, IPointerClickHandler, IPointerDownHandler
{
    private bool suppressRightClick;
    public int SlotIndex { get; private set; }

    public void SetIndex(int index)
    {
        SlotIndex = index;
    }

    public void OnDrop(PointerEventData eventData)
    {
        GridCraftingDragSource craftingDrag =
    eventData.pointerDrag?.GetComponent<GridCraftingDragSource>();

        if (craftingDrag != null)
        {
            craftingDrag.DropOnInventory(SlotIndex);
            return;
        }

        DraggableItem draggedItem = eventData.pointerDrag?.GetComponent<DraggableItem>();
        if(draggedItem != null & draggedItem.sourceSlot != null)
        {
            draggedItem.CompleteDrop(SlotIndex);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (InventoryManager.Instance == null)
            return;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (suppressRightClick)
            {
                suppressRightClick = false;
                return;
            }

            if (DraggableItem.ActiveDrag != null)
                return;
        }

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            InventoryManager.Instance.HandleRightClick(SlotIndex);
        }
        else if(eventData.button == PointerEventData.InputButton.Left)
        {
            InventoryManager.Instance.HandleLeftClick(SlotIndex);
        }
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right)
            return;

        DraggableItem drag = DraggableItem.ActiveDrag;

        if (drag == null)
            return;

        // Không thực hiện HandleRightClick trong lúc đang kéo.
        suppressRightClick = true;

        bool success = drag.TryPlaceOneOnSlot(SlotIndex);

        if (success)
        {
            Debug.Log(
                $"[Inventory] Placed one item into slot {SlotIndex}"
            );
        }
    }

}
