using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Drop target on the PARENT Slot (its Image raycastTarget must be ON).</summary>
public sealed class GridCraftingIngredientSlot : MonoBehaviour, IDropHandler, IPointerClickHandler, IPointerDownHandler
{
    [SerializeField] private GridCraftingController controller;
    [SerializeField, Range(0, 8)] private int gridIndex;
    private bool suppressRightClick;

    public void Configure(GridCraftingController crafting, int index)
    {
        controller = crafting;
        gridIndex = index;
        Image background = GetComponent<Image>();
        if (background != null) background.raycastTarget = true;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (controller == null || eventData.pointerDrag == null) return;

        GridCraftingDragSource craftDrag =
            eventData.pointerDrag.GetComponent<GridCraftingDragSource>();
        if (craftDrag != null)
        {
            if (!craftDrag.IsResult)
                controller.TryMoveCell(craftDrag.CellIndex, gridIndex);
            return;
        }

        DraggableItem inventoryDrag =
            eventData.pointerDrag.GetComponent<DraggableItem>();
        if (inventoryDrag == null || inventoryDrag.sourceSlot == null) return;
        // A normal left drop transfers the source stack (up to grid maxStack).
        // If you already added RemainingDragAmount to DraggableItem,
        // prefer that over the source amount for mid-drag right-click placement.
        int sourceIndex = inventoryDrag.sourceSlot.SlotIndex;
        var inventory = InventoryManager.Instance;
        if (inventory == null || sourceIndex < 0 || sourceIndex >= inventory.Slots.Count) return;
        int requested = inventory.Slots[sourceIndex].Amount;
        int transferred = controller.TryMoveAmountFromInventory(sourceIndex, gridIndex, requested);
        Debug.Log($"[CraftSlot] Left drop: source={sourceIndex}, requested={requested}, " +
                  $"received={transferred}, craftCell={gridIndex}, newAmount={controller.GetCell(gridIndex).amount}");
        if (transferred <= 0)
            Debug.LogWarning($"[GridCrafting] Cannot place ingredient at {gridIndex}");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right) return;
        DraggableItem active = DraggableItem.ActiveDrag;
        if (active == null || controller == null) return;
        suppressRightClick = true;
        active.TryPlaceOneOnCraftCell(controller, gridIndex);
    }

    // Right click when not dragging returns ONE uncrafted ingredient.


    public void OnPointerClick(PointerEventData eventData)
    {
        if (controller == null)
            return;

        InventoryManager inventory = InventoryManager.Instance;

        if (inventory == null)
            return;

        bool isLeft =
            eventData.button == PointerEventData.InputButton.Left;

        bool isRight =
            eventData.button == PointerEventData.InputButton.Right;

        if (!isLeft && !isRight)
            return;

        // Truong hop dang keo Item bang chuot trai,
        // roi bam chuot phai de dat tung 1 Item.
        // OnPointerDown da xu ly, khong xu ly lan hai.
        if (isRight && suppressRightClick)
        {
            suppressRightClick = false;
            return;
        }

        // Khong xu ly click thong thuong
        // trong luc dang drag bang chuot trai.
        if (DraggableItem.ActiveDrag != null)
            return;

        // TRUONG HOP 1:
        // Cursor dang cam Item sau khi split.
        if (!inventory.CursorSlot.isEmpty())
        {
            if (isLeft)
            {
                // Chuot trai: dat toi da so luong dang cam.
                int requested = inventory.CursorSlot.Amount;

                controller.TryMoveAmountFromCursor(
                    gridIndex,
                    requested
                );
            }
            else if (isRight)
            {
                // Chuot phai: dat dung 1 Item.
                controller.TryMoveAmountFromCursor(
                    gridIndex,
                    1
                );
            }

            return;
        }

        if (isRight)
        {
            controller.TrySplitCellToCursor(gridIndex);
        }
    }

}
