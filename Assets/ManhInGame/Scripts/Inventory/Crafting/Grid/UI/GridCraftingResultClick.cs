using UnityEngine;
using UnityEngine.EventSystems;

public class GridCraftingResultClick : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GridCraftingController controller;

    public void Configure(GridCraftingController target)
    {
        controller = target;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right)
            return;

        if (controller == null)
            return;

        if (DraggableItem.ActiveDrag != null)
            return;

        bool access = controller.TryTakeOneResultToCursor(out string message);

        if (!access)
        {
            Debug.LogWarning("[GridCrafting] " + message);
        }
    }
}
