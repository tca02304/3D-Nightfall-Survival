using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private GameObject slotPrefab;

    [SerializeField] private Transform inventoryGrid;
    [SerializeField] private Transform hotbarGrid;

    [SerializeField] private int inventorySlotCount = 28;
    [SerializeField] private int hotbarSlotCount = 7;

    void Start()
    {
        CreateSlots(inventoryGrid, inventorySlotCount);
        CreateSlots(hotbarGrid, hotbarSlotCount);
    }

    private void CreateSlots(Transform parent, int count)
    {
        for(int i = 0; i < count; i++)
        {
            Instantiate(slotPrefab, parent);
        }
    }
}
