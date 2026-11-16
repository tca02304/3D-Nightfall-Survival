using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class ItemDropper : MonoBehaviour
{
    [Header("Throw Position")]
    [SerializeField, Min(0.1f)] private float dropDistance = 0.8f;
    [SerializeField] private float dropHeight = 0.35f;

    [Header("Throw Physics")]
    [SerializeField, Min(0f)] private float forwardImpulse = 4.5f;
    [SerializeField, Min(0f)] private float upwardImpulse = 1.6f;

    [Header("Drop Input")]
    [SerializeField] private bool enableDropHotkey = true;

    [Header("Runtime Player Binding - Do Not Assign Across Scenes")]
    [SerializeField] private Transform playerRoot;
    [SerializeField] private Transform dropOrigin;

    // Bind the locally controlled Player and optional throw origin after spawning.
    public void BindPlayer(Transform newPlayerRoot, Transform newDropOrigin = null)
    {
        if (newPlayerRoot == null)
        {
            Debug.LogWarning("[ItemDropper] Cannot bind a null Player.", this);
            return;
        }

        playerRoot = newPlayerRoot;
        dropOrigin = newDropOrigin != null ? newDropOrigin : newPlayerRoot;
        Debug.Log($"[ItemDropper] Bound local Player: {playerRoot.name}", this);
    }

    // Clear references when the currently bound Player despawns.
    public void ClearPlayerBinding(Transform owner)
    {
        if (owner == null || playerRoot != owner)
            return;

        playerRoot = null;
        dropOrigin = null;
    }

    // Drop the selected Hotbar stack when the G key is pressed.
    private void Update()
    {
        if (enableDropHotkey && Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame)
            DropSelectedStack();
    }

    // Spawn the thrown stack first and remove inventory items only if spawning succeeds.
    public bool DropFromSlot(int slotIndex, int amount = 1)
    {
        InventoryManager inventory = InventoryManager.Instance;
        if (inventory == null || playerRoot == null)
        {
            Debug.LogWarning("[ItemDropper] Missing InventoryManager or local Player binding.", this);
            return false;
        }

        if (slotIndex < 0 || slotIndex >= inventory.Slots.Count || amount <= 0)
            return false;

        if (!inventory.CursorSlot.isEmpty())
            return false;

        InventorySlotData slot = inventory.Slots[slotIndex];
        if (slot == null || slot.isEmpty() || slot.Amount < amount)
            return false;

        int itemId = slot.ItemId;
        WorldItem spawned = SpawnWorldItem(itemId, amount);
        if (spawned == null)
            return false;

        InventorySlotData dropped = inventory.DropItem(slotIndex, amount);
        if (dropped == null || dropped.ItemId != itemId || dropped.Amount != amount)
        {
            spawned.gameObject.SetActive(false);
            Destroy(spawned.gameObject);
            Debug.LogError($"[ItemDropper] Inventory drop failed for ItemId={itemId}.", this);
            return false;
        }

        LaunchWorldItem(spawned);
        Debug.Log($"[ItemDropper] Threw stack: ID={itemId}, Amount={amount}.", this);
        return true;
    }

    // Drop every item from a specified Inventory or Hotbar slot.
    public bool DropFullSlot(int slotIndex)
    {
        InventoryManager inventory = InventoryManager.Instance;
        if (inventory == null || slotIndex < 0 || slotIndex >= inventory.Slots.Count)
            return false;

        InventorySlotData slot = inventory.Slots[slotIndex];
        return slot != null && !slot.isEmpty() && DropFromSlot(slotIndex, slot.Amount);
    }

    // Drop a chosen amount from the selected Hotbar slot.
    public bool DropSelectedItem(int amount = 1)
    {
        HotbarManager hotbar = HotbarManager.Instance;
        InventoryManager inventory = InventoryManager.Instance;
        return hotbar != null && inventory != null &&
            DropFromSlot(inventory.InventorySize + hotbar.CurrentSlot, amount);
    }

    // Drop the entire selected Hotbar stack.
    public bool DropSelectedStack()
    {
        HotbarManager hotbar = HotbarManager.Instance;
        InventorySlotData slot = hotbar != null ? hotbar.GetSelectedSlotData() : null;
        return slot != null && !slot.isEmpty() && DropSelectedItem(slot.Amount);
    }

    // Instantiate a single WorldItem representing the entire stack.
    private WorldItem SpawnWorldItem(int itemId, int amount)
    {
        Transform origin = dropOrigin != null ? dropOrigin : playerRoot;
        if (origin == null)
            return null;

        Vector3 direction = GetThrowDirection();
        Vector3 position = origin.position + direction * dropDistance + Vector3.up * dropHeight;

        WorldItem prefab = ItemDatabase.Instance != null
            ? ItemDatabase.Instance.GetWorldPrefab(itemId) : null;
        if (prefab == null)
        {
            Debug.LogWarning($"[ItemDropper] Missing WorldPrefab for ItemId={itemId}.", this);
            return null;
        }

        WorldItem item = null;
        try
        {
            item = Instantiate(prefab, position, Quaternion.identity);
            item.Setup(itemId, amount);
            item.SetDropOwner(playerRoot);

            if (!item.gameObject.activeSelf)
            {
                Destroy(item.gameObject);
                return null;
            }

            return item;
        }
        catch (Exception ex)
        {
            if (item != null)
            {
                item.gameObject.SetActive(false);
                Destroy(item.gameObject);
            }
            Debug.LogError($"[ItemDropper] Spawn failed: {ex.Message}", this);
            return null;
        }
    }

    // Read horizontal facing direction so a player looking down does not throw into the ground.
    private Vector3 GetThrowDirection()
    {
        Transform origin = dropOrigin != null ? dropOrigin : playerRoot;
        Vector3 forward = Vector3.ProjectOnPlane(origin.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.001f && playerRoot != null)
            forward = Vector3.ProjectOnPlane(playerRoot.forward, Vector3.up);

        return forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
    }

    // Apply forward and upward impulse after inventory removal succeeds.
    private void LaunchWorldItem(WorldItem worldItem)
    {
        Rigidbody rb = worldItem.GetComponent<Rigidbody>();
        if (rb == null || rb.isKinematic)
        {
            Debug.LogWarning($"[ItemDropper] Cannot throw {worldItem.name}: Rigidbody missing or kinematic.", worldItem);
            return;
        }

        Vector3 impulse = GetThrowDirection() * forwardImpulse + Vector3.up * upwardImpulse;
        rb.AddForce(impulse, ForceMode.Impulse);
    }
}
