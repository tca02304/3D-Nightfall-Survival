
using UnityEngine;

public class ResourcePickup : MonoBehaviour
{
    [Header("Resource Settings")]
    [SerializeField] private ResourceType resourceType;
    [SerializeField] private int amount = 1;

    private bool collected;

    private void OnTriggerEnter(Collider other)
    {
        if (collected)
            return;

        // Kiểm tra Player
        if (!other.CompareTag("Player") &&
            !other.transform.root.CompareTag("Player"))
            return;

        // Kiểm tra hệ thống Inventory
        if (ResourceInventory.Instance == null)
        {
            Debug.LogError("ResourceInventory not found!");
            return;
        }

        collected = true;

        // Thêm tài nguyên vào Inventory
        ResourceInventory.Instance.AddResource(resourceType, amount);

        // Xóa vật phẩm khỏi Scene
        Destroy(gameObject);
    }
}
