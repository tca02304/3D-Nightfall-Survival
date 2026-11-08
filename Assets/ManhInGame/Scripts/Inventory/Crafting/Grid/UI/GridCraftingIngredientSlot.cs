using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class GridCraftingIngredientSlot :
    MonoBehaviour,
    IDropHandler,
    IPointerClickHandler
{
    [Header("Crafting References")]
    [SerializeField] private GridCraftingController controller;

    [SerializeField, Range(0, 8)] private int gridIndex;

    public void Configure(GridCraftingController crafting, int index)
    {
        controller = crafting;
        gridIndex = index;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if(controller == null)
        {
            Debug.LogWarning("[CraftingSlot] Controller missing");
            return;
        }

        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        DraggableItem dragged = eventData.pointerDrag?.GetComponent<DraggableItem>();

        if (dragged == null || dragged.sourceSlot == null)
            return;

        InventoryManager inventory = InventoryManager.Instance;

        if (inventory == null)
            return;

        int sourceIndex = dragged.sourceSlot.SlotIndex;

        if (sourceIndex < 0 || sourceIndex >= inventory.Slots.Count)
            return;

        if (!inventory.CursorSlot.isEmpty())
            return;

        InventorySlotData source = inventory.Slots[sourceIndex];

        if (source.isEmpty())
            return;

        int itemId = source.ItemId;

        bool success = controller.SetCellFromInventory(gridIndex, sourceIndex, 1);

        if (success)
        {
            Debug.Log($"[CraftingSlot] Cell {gridIndex}" +
                $"received ItemId={itemId}");
        }
        else
        {
            Debug.LogWarning($"[CraftingSlot] Cannot set cell {gridIndex}");
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (controller == null)
            return;

        if(eventData.button == PointerEventData.InputButton.Right)
        {
            controller.ClearCell(gridIndex);
        }
    }
}
