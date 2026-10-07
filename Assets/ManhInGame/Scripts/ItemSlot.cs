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

        Transform oldSlot = draggedItem.GetOldParent();

        DraggableItem currentItem =
            GetComponentInChildren<DraggableItem>();

        if (currentItem != null)
        {
            currentItem.transform.SetParent(oldSlot);
            currentItem.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            currentItem.SetNewParent(oldSlot);
        }

        draggedItem.transform.SetParent(transform);
        draggedItem.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        draggedItem.SetNewParent(transform);
    }
}
