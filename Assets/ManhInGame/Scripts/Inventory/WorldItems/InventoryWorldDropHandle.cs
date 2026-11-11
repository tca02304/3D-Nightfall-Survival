
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
        {
            Debug.LogWarning(
                "[WorldDrop] Missing InventoryManager or ItemDropper");
            return false;
        }

        // Only allow dropping from the main Inventory.
        if (sourceSlotIndex < 0 ||
    sourceSlotIndex >= inventory.Slots.Count)
        {
            Debug.LogWarning(
                $"[WorldDrop] Invalid source slot: {sourceSlotIndex}");
            return false;
        }

        if (amount <= 0 || !inventory.CursorSlot.isEmpty())
            return false;

        if (protectedAreas == null || protectedAreas.Length == 0)
        {
            Debug.LogWarning(
                "[WorldDrop] Protected Areas not configured");
            return false;
        }

        // Check all protected UI areas.
        foreach (RectTransform area in protectedAreas)
        {
            if (area == null || !area.gameObject.activeInHierarchy)
                continue;

            bool inside =
                RectTransformUtility.RectangleContainsScreenPoint(
                    area,
                    screenPosition,
                    uiCamera);

            if (inside)
            {
                Debug.Log(
                    $"[WorldDrop] Mouse is inside: {area.name}");
                return false;
            }
        }

        if (sourceSlotIndex >= inventory.Slots.Count)
            return false;

        InventorySlotData slot = inventory.Slots[sourceSlotIndex];

        if (slot == null || slot.isEmpty())
            return false;

        int actualAmount = Mathf.Min(amount, slot.Amount);

        if (actualAmount <= 0)
            return false;

        Debug.Log(
            $"[WorldDrop] Dropping ItemId={slot.ItemId}, " +
            $"Amount={actualAmount}");

        return itemDropper.DropFromSlot(
            sourceSlotIndex,
            actualAmount);
    }

}
