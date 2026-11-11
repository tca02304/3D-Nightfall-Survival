using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class ItemDropper : MonoBehaviour
{
    [Header("World Item Registry")]
    [SerializeField] private WorldItemPrefabRegistry prefabRegistry;

    [Header("Drop Position")]
    [SerializeField, Min(0.1f)] private float dropDistance = 1.5f;

    [SerializeField] private float dropHeight = 0.5f;

    [Header("Multiple Item Drop")]
    [SerializeField, Min(0.05f)]
    private float itemSpacing = 0.5f;

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
            DropSelectedStack();
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

        if (prefabRegistry == null)
        {
            Debug.LogError(
                "[ItemDropper] WorldItemPrefabRegistry missing");
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

        // [CHANGED] Spawn one WorldItem per item instead of
        // a single WorldItem containing the entire stack.
        List<WorldItem> spawnedItems =
            new List<WorldItem>(amount);

        for (int i = 0; i < amount; i++)
        {
            WorldItem worldItem = SpawnWorldItem(
                itemId,
                1,
                i
            );

            // If any spawn fails, cancel the entire drop.
            // The Inventory has not been modified yet.
            if (worldItem == null)
            {
                foreach (WorldItem spawned in spawnedItems)
                {
                    if (spawned != null)
                    {
                        spawned.gameObject.SetActive(false);
                        Destroy(spawned.gameObject);
                    }
                }

                Debug.LogError(
                    "[ItemDropper] WorldItem spawn failed. " +
                    "Inventory remains unchanged.");

                return false;
            }

            spawnedItems.Add(worldItem);
        }

        // [KEEP] Inventory is reduced only after all spawns succeed.
        InventorySlotData dropped =
            inventory.DropItem(slotIndex, amount);

        if (dropped == null ||
            dropped.ItemId != itemId ||
            dropped.Amount != amount)
        {
            // Cancel spawned objects on inventory failure.
            foreach (WorldItem spawned in spawnedItems)
            {
                if (spawned != null)
                {
                    spawned.gameObject.SetActive(false);
                    Destroy(spawned.gameObject);
                }
            }

            Debug.LogError(
                "[ItemDropper] Inventory DropItem failed " +
                "or returned an unexpected amount.");

            return false;
        }

        Debug.Log(
            $"[ItemDropper] Spawned {spawnedItems.Count} WorldItems " +
            $"for ItemId={itemId}");

        return true;


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

    private WorldItem SpawnWorldItem(int itemId, int amount, int spawnIndex = 0)
    {
        Transform origin =
            dropOrigin != null ? dropOrigin : transform;


        // [CHANGED] Calculate the original drop position.
        Vector3 spawnPosition =
            origin.position +
            origin.forward * dropDistance +
            Vector3.up * dropHeight;

        // [ADD] Spread individual items in a small grid.
        // Use horizontal directions only to avoid stacking vertically.
        Vector3 right =
            Vector3.ProjectOnPlane(origin.right, Vector3.up).normalized;

        Vector3 forward =
            Vector3.ProjectOnPlane(origin.forward, Vector3.up).normalized;

        if (right.sqrMagnitude < 0.001f)
            right = Vector3.right;

        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;

        // Grid with 3 items per row.
        const int itemsPerRow = 3;

        int column = spawnIndex % itemsPerRow;
        int row = spawnIndex / itemsPerRow;

        Vector3 offset =
            right * ((column - 1) * itemSpacing) +
            forward * (row * itemSpacing);

        spawnPosition += offset;


        WorldItem worldItem;

        try
        {
            WorldItem prefab = prefabRegistry.GetPrefab(itemId);

            if(prefab == null)
            {
                Debug.LogWarning($"[ItemDropper] No WorldItem Prefab for ItemId={itemId}");
                return null;
            }

            worldItem = Instantiate(prefab, spawnPosition, Quaternion.identity);
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
