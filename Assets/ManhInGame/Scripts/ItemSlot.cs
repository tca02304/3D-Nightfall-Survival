using UnityEngine;
using UnityEngine.EventSystems;

public class ItemSlot : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        DraggableItem draggedItem =
            eventData.pointerDrag?.GetComponent<DraggableItem>();

        if (draggedItem == null)
            return;

        Transform oldParent = draggedItem.transform.parent;

        DraggableItem currentItem =
            GetComponentInChildren<DraggableItem>();

        if (currentItem != null)
        {
            currentItem.transform.SetParent(draggedItem.GetOldParent());
            currentItem.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            currentItem.SetNewParent(transform);
        }
    }
}
