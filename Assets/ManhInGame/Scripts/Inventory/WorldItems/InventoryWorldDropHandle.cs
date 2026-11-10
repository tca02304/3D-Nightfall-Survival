
using UnityEngine;

public sealed class InventoryWorldDropHandler : MonoBehaviour
{
    [Header("Drop System")]
    [SerializeField] private ItemDropper itemDropper;

    [Header("UI Areas Where World Drop Is Forbidden")]
    [Tooltip("Main Inventory, Hotbar, Equipment, Crafting, etc.")]
    [SerializeField] private RectTransform[] protectedAreas;

    /// <summary>
    /// Try dropping an item when the mouse is released
    /// outside all protected UI areas.
    /// </summary>
    public bool TryDropOutsideUI(
        int sourceSlotIndex,
        int amount,
        Vector2 screenPosition,
        Camera uiCamera)
    {
        InventoryManager inventory = InventoryManager.Instance;

        if (inventory == null || itemDropper == null)
            return false;

        if (amount <= 0)
            return false;

        // Only drag items from the main inventory.
        // Hotbar dropping is handled separately by G.
        if (sourceSlotIndex < 0 ||
            sourceSlotIndex >= inventory.InventorySize)
        {
            return false;
        }

        // Avoid dropping items while CursorSlot is occupied.
        if (!inventory.CursorSlot.isEmpty())
            return false;

        // Safety: an unconfigured UI must not drop items.
        if (protectedAreas == null ||
            protectedAreas.Length == 0)
        {
            Debug.LogWarning(
                "[InventoryWorldDropHandler] " +
                "Protected UI areas are not configured.");
            return false;
        }

        // Do not drop if the cursor is inside a protected area.
        foreach (RectTransform area in protectedAreas)
        {
            if (area == null)
                continue;

            bool isInside =
                RectTransformUtility.RectangleContainsScreenPoint(
                    area,
                    screenPosition,
                    uiCamera);

            if (isInside)
                return false;
        }

        // Validate the original inventory slot.
        if (sourceSlotIndex >= inventory.Slots.Count)
            return false;

        InventorySlotData slot =
            inventory.Slots[sourceSlotIndex];

        if (slot == null || slot.isEmpty())
            return false;

        int actualAmount = Mathf.Min(amount, slot.Amount);

        if (actualAmount <= 0)
            return false;

        // ItemDropper handles spawning and inventory removal.
        return itemDropper.DropFromSlot(
            sourceSlotIndex,
            actualAmount);
    }
}
