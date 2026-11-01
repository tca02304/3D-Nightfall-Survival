using UnityEngine;
using UnityEngine.EventSystems;

public class ItemSlot : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    public int SlotIndex { get; private set; }

    public void SetIndex(int index)
    {
        SlotIndex = index;
    }

    public void OnDrop(PointerEventData eventData)
    {
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

        if(eventData.button == PointerEventData.InputButton.Right)
        {
            InventoryManager.Instance.HandleRightClick(SlotIndex);
        }
        else if(eventData.button == PointerEventData.InputButton.Left)
        {
            InventoryManager.Instance.HandleLeftClick(SlotIndex);
        }
    }
}
