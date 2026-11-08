using UnityEngine;
using UnityEngine.InputSystem;

public class ItemDropper : MonoBehaviour
{
    [Header("World Item")]
    [SerializeField] private WorldItem worldItemPrefab;

    [Header("Drop Position")]
    [SerializeField, Min(0.1f)] private float dropDistance = 1.5f;

    [SerializeField] private float dropHeight = 0.5f;

    [Header("Drop Input")]
    [SerializeField] private bool enableDropHotkey = true;

    [SerializeField, Min(1)] private int dropAmount = 1;

    [Header("Drop Owner")]
    [SerializeField] private Transform playerRoot;

    [Header("Optional Spawn Origin")]
    [SerializeField] private Transform dropOrigin;

    private void Update()
    {
        if (!enableDropHotkey)
            return;

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        if (keyboard.gKey.wasPressedThisFrame)
        {
            DropSelectedItem(dropAmount);
        }
    }

    public bool DropFromSlot(int slotIndex, int amount = 1)
    {
        InventoryManager inventory = InventoryManager.Instance;

        if (inventory == null)
        {
            Debug.LogWarning(
                "[ItemDropper] InventoryManager not found");
            return false;
        }

        if (worldItemPrefab == null)
        {
            Debug.LogError(
                "[ItemDropper] WorldItem Prefab missing");
            return false;
        }

        if (slotIndex < 0 || slotIndex >= inventory.Slots.Count)
            return false;

        if (amount <= 0)
            return false;

        if (!inventory.CursorSlot.isEmpty())
        {
            Debug.LogWarning(
                "[ItemDropper] CursorSlot is not empty");
            return false;
        }

        InventorySlotData slot = inventory.Slots[slotIndex];

        if (slot == null || slot.isEmpty())
            return false;

        if (slot.Amount < amount)
        {
            Debug.LogWarning(
                "[ItemDropper] Not enough items");
            return false;
        }

        int itemId = slot.ItemId;

        WorldItem worldItem = SpawnWorldItem(itemId, amount);

        if (worldItem == null)
            return false;

        InventorySlotData dropped = inventory.DropItem(slotIndex, amount);

        if (dropped == null)
        {
            Destroy(worldItem.gameObject);

            Debug.LogError(
                "[ItemDropper] Inventory DropItem failed");

            return false;
        }

        Debug.Log(
            $"[Drop] ItemId: {dropped.ItemId} | " +
            $"Amount: {dropped.Amount}");

        return true;
    }

    public bool DropSelectedItem(int amount = 1)
    {
        HotbarManager hotbar = HotbarManager.Instance;
        InventoryManager inventory = InventoryManager.Instance;

        if (hotbar == null || inventory == null)
        {
            Debug.LogWarning(
                "[ItemDropper] Hotbar or Inventory missing");
            return false;
        }

        int slotIndex = inventory.InventorySize + hotbar.CurrentSlot;

        return DropFromSlot(slotIndex, amount);
    }

    public bool DropSelectedStack()
    {
        HotbarManager hotbar = HotbarManager.Instance;

        if (hotbar == null)
            return false;

        InventorySlotData slot = hotbar.GetSelectedSlotData();

        if (slot == null || slot.isEmpty())
            return false;

        return DropSelectedItem(slot.Amount);
    }

    private WorldItem SpawnWorldItem(int itemId, int amount)
    {
        Transform origin =
            dropOrigin != null ? dropOrigin : transform;

        Vector3 spawnPosition =
            origin.position +
            origin.forward * dropDistance +
            Vector3.up * dropHeight;

        WorldItem worldItem;

        try
        {
            worldItem = Instantiate(worldItemPrefab, spawnPosition, Quaternion.identity);
        }
        catch(System.Exception ex)
        {
            Debug.LogError(
                "[ItemDropper] Spawn failed: " +
                ex.Message);

            return null;
        }

        if (worldItem == null)
            return null;

        worldItem.Setup(itemId, amount);

        if (!worldItem.gameObject.activeSelf)
        {
            Destroy(worldItem.gameObject);
            return null;
        }

        Transform owner = playerRoot != null ? playerRoot : transform.root;

        worldItem.SetDropOwner(owner);

        return worldItem;
    }
}
