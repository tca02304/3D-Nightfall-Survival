using UnityEngine;

public class InventoryTest : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private int testItemId = 1;
    [SerializeField] private int testAmount = 10;

    [ContextMenu("Test/Add Item To Inventory")]
    public void TestAddItem()
    {
        if (ItemDatabase.Instance == null)
        {
            Debug.LogError("ItemDatabase chưa được khởi tạo!");
            return;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogError("InventoryManager chưa được khởi tạo!");
            return;
        }

        ItemData item = ItemDatabase.Instance.GetItemById(testItemId);

        if (item == null)
        {
            Debug.LogError(
                $"Không tìm thấy Item ID: {testItemId}. " +
                "Kiểm tra API đã tải dữ liệu chưa!"
            );
            return;
        }

        Sprite icon = ItemDatabase.Instance.GetIconByKey(item.iconKey);

        if (icon == null)
        {
            Debug.LogWarning(
                $"Không tìm thấy Sprite cho IconKey: {item.iconKey}"
            );
        }

        int remaining = InventoryManager.Instance.AddItem(
            item,
            testAmount
        );

        Debug.Log(
            $"Test Item: {item.name} | " +
            $"IconKey: {item.iconKey} | " +
            $"Amount: {testAmount} | " +
            $"Remaining: {remaining}"
        );
    }


}
