using UnityEngine;

public class InventoryTest : MonoBehaviour
{
    private void Start()
    {
        ItemData wood = new ItemData
        {
            id = 1,
            itemCode = "wood",
            name = "Wood",
            maxStack = 64
        };

        ItemData stone = new ItemData
        {
            id = 2,
            itemCode = "stone",
            name = "Stone",
            maxStack = 64
        };

        InventoryManager.Instance.AddItem(wood, 5);
        InventoryManager.Instance.AddItem(stone, 3);
    }
}
