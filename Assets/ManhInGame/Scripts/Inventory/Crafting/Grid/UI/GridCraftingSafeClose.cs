
using UnityEngine;

public sealed class GridCraftingSafeClose : MonoBehaviour
{
    [SerializeField]
    private GridCraftingController controller;

    [SerializeField]
    private GameObject craftingPanel;

    public bool TryClose()
    {
        if (controller == null || craftingPanel == null)
        {
            Debug.LogError(
                "[Crafting] Missing controller or crafting panel");
            return false;
        }

        // Thu tra tat ca nguyen lieu ve Inventory.
        if (!controller.TryReturnAll(out string message))
        {
            Debug.LogWarning(
                "[Crafting] Cannot close: " + message);

            // UI van mo de nguoi choi giai quyet.
            return false;
        }

        // Chi dong UI sau khi tra nguyen lieu thanh cong.
        craftingPanel.SetActive(false);

        Debug.Log("[Crafting] Closed safely");
        return true;
    }
}
