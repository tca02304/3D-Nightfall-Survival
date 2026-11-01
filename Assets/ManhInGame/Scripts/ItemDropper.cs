using UnityEngine;

public class ItemDropper : MonoBehaviour
{
    [SerializeField] private WorldItem worldItemPrefab;

    [SerializeField] private float dropDistance = 1.5f;
    [SerializeField] private float dropHeight = 0.5f;

    public bool DropFromSlot(int slotIndex, int amount = 1)
    {
        if (InventoryManager.Instance == null)
            return false;

        InventorySlotData droppedItem = InventoryManager.Instance.DropItem(slotIndex, amount);

        if (droppedItem == null)
            return false;

        SpawnWorldItem(droppedItem);
        return true;
    }

    private void SpawnWorldItem(InventorySlotData droppedItem)
    {
        Vector3 spawnPosition = transform.position + transform.forward * dropDistance + Vector3.up * dropHeight;

        WorldItem worldItem = Instantiate(worldItemPrefab, spawnPosition, Quaternion.identity);

        worldItem.Setup(droppedItem.ItemId, droppedItem.Amount);
    }
}
