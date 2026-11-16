using UnityEngine;

public sealed class InventoryWorldDropHandler : MonoBehaviour
{
    [Header("Drop System")]
    [SerializeField] private ItemDropper itemDropper;

    [Header("UI Areas Where World Drop Is Forbidden")]
    [Tooltip("Inventory, Hotbar, Equipment, Crafting, and other protected panels.")]
    [SerializeField] private RectTransform[] protectedAreas;

    private void OnEnable()
    {
        ResolveDropper();
    }

    // Find the persistent ItemDropper without requiring a cross-Scene Inspector reference.
    private bool ResolveDropper()
    {
        if (itemDropper == null)
            itemDropper = FindFirstObjectByType<ItemDropper>();

        return itemDropper != null;
    }

    // Drop only when the pointer is outside all protected UI areas.
    public bool TryDropOutsideUI(int sourceSlotIndex, int amount, Vector2 screenPosition, Camera uiCamera)
    {
        InventoryManager inventory = InventoryManager.Instance;
        if (inventory == null || !ResolveDropper())
        {
            Debug.LogWarning("[WorldDrop] Missing InventoryManager or ItemDropper.", this);
            return false;
        }

        if (sourceSlotIndex < 0 || sourceSlotIndex >= inventory.Slots.Count || amount <= 0)
            return false;

        if (!inventory.CursorSlot.isEmpty())
            return false;

        if (protectedAreas == null || protectedAreas.Length == 0)
        {
            Debug.LogWarning("[WorldDrop] Protected Areas not configured.", this);
            return false;
        }

        foreach (RectTransform area in protectedAreas)
        {
            if (area == null || !area.gameObject.activeInHierarchy)
                continue;

            Camera camera = GetAreaCamera(area, uiCamera);
            if (RectTransformUtility.RectangleContainsScreenPoint(area, screenPosition, camera))
                return false;
        }

        InventorySlotData slot = inventory.Slots[sourceSlotIndex];
        if (slot == null || slot.isEmpty())
            return false;

        return itemDropper.DropFullSlot(sourceSlotIndex);
    }

    // Use the correct camera for each protected Canvas, including Screen Space Overlay.
    private Camera GetAreaCamera(RectTransform area, Camera fallback)
    {
        Canvas areaCanvas = area.GetComponentInParent<Canvas>();
        if (areaCanvas == null)
            return fallback;

        Canvas rootCanvas = areaCanvas.rootCanvas;
        return rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : rootCanvas.worldCamera != null ? rootCanvas.worldCamera : fallback;
    }
}
