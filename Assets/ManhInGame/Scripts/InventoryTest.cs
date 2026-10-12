using System.Collections;
using UnityEngine;

public class InventoryTest : MonoBehaviour
{
    private IEnumerator Start()
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

        ItemData sword = null;

        while(sword == null)
        {
            sword = ItemDatabase.Instance.GetItemById(5);
            yield return null;
        }

        InventoryManager.Instance.AddItem(wood, 5);
        InventoryManager.Instance.AddItem(stone, 3);
        InventoryManager.Instance.AddItem(sword, 1);

        InventorySlotData slot =
            InventoryManager.Instance.Slots[2];

        ItemData itemData =
            InventoryManager.Instance.GetItemData(slot.ItemId);

        Debug.Log("Slot contains: " + itemData.name);

        Debug.Log("Damage: " + itemData.weaponStat.damage);
        Debug.Log("Range: " + itemData.weaponStat.attackRange);
    }
}
