using UnityEngine;
using UnityEngine.EventSystems;

public class ItemSlot : MonoBehaviour, IDropHandler
{
    public int SlotIndex { get; private set; }

    public void SetIndex(int index)
    {
        SlotIndex = index;
    }

    public void OnDrop(PointerEventData eventData)
    {
        DraggableItem draggedItem =
            eventData.pointerDrag?.GetComponent<DraggableItem>();

        if (draggedItem == null)
            return;

        ItemSlot sourceSlot = draggedItem.SourceSlot;

        if (sourceSlot == null)
            return;

        draggedItem.CompleteDrop(SlotIndex);
    }
}
