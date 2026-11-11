
using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WorldItemPrefabRegistry : MonoBehaviour
{
    [Serializable]
    private class PrefabOverride
    {
        public string itemCode;
        public WorldItem prefab;
    }

    [Header("Optional Prefab Overrides")]
    [Tooltip("Only add items whose prefab name differs from ItemCode.")]
    [SerializeField]
    private List<PrefabOverride> overrides = new();

    [Header("Optional Fallback")]
    [Tooltip("Generic prefab for items without a dedicated model.")]
    [SerializeField]
    private WorldItem fallbackPrefab;

    // Cache prefabs already resolved.
    private readonly Dictionary<string, WorldItem> cache = new();

    // Override lookup.
    private readonly Dictionary<string, WorldItem> overrideLookup = new();

    private void Awake()
    {
        BuildOverrideLookup();
    }

    private void BuildOverrideLookup()
    {
        overrideLookup.Clear();
        cache.Clear();

        foreach (PrefabOverride entry in overrides)
        {
            if (entry == null ||
                string.IsNullOrWhiteSpace(entry.itemCode) ||
                entry.prefab == null)
            {
                continue;
            }

            string key = entry.itemCode.Trim();

            if (!overrideLookup.TryAdd(key, entry.prefab))
            {
                Debug.LogWarning(
                    $"[WorldItemRegistry] Duplicate override: {key}",
                    this);
            }
        }
    }

    /// <summary>
    /// Resolve world prefab using Inventory ItemId.
    /// </summary>
    public WorldItem GetPrefab(int itemId)
    {
        InventoryManager inventory = InventoryManager.Instance;

        if (inventory == null)
        {
            Debug.LogWarning(
                "[WorldItemRegistry] InventoryManager missing");
            return null;
        }

        ItemData item = inventory.GetItemData(itemId);

        if (item == null)
        {
            Debug.LogWarning(
                $"[WorldItemRegistry] Unknown ItemId={itemId}");
            return null;
        }

        return GetPrefabByCode(item.itemCode);
    }

    /// <summary>
    /// Resolve prefab from ItemCode.
    /// </summary>
    public WorldItem GetPrefabByCode(string itemCode)
    {
        if (string.IsNullOrWhiteSpace(itemCode))
            return null;

        string key = itemCode.Trim();

        // 1. Cached result.
        if (cache.TryGetValue(key, out WorldItem cached))
            return cached;

        // 2. Explicit override.
        if (overrideLookup.TryGetValue(key, out WorldItem overridden))
        {
            cache[key] = overridden;
            return overridden;
        }

        // 3. Convention-based Resources lookup.
        string resourcePath = "WorldItems/" + key;

        WorldItem prefab = Resources.Load<WorldItem>(
            resourcePath);

        if (prefab != null)
        {
            cache[key] = prefab;
            return prefab;
        }

        // 4. Optional generic fallback.
        if (fallbackPrefab != null)
        {
            Debug.LogWarning(
                $"[WorldItemRegistry] No prefab for '{key}'. " +
                "Using fallback prefab.");

            cache[key] = fallbackPrefab;
            return fallbackPrefab;
        }

        // Do not spawn or remove an item without a prefab.
        Debug.LogWarning(
            $"[WorldItemRegistry] Prefab not found: {resourcePath}");

        return null;
    }
}
