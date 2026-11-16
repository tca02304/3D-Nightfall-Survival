using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemCatalog", menuName = "Nightfall/Inventory/Item Catalog")]
public sealed class ItemCatalog : ScriptableObject
{
    [SerializeField] private List<ItemDefinitionSO> items = new();

    private Dictionary<int, ItemDefinitionSO> byId;
    private Dictionary<string, ItemDefinitionSO> byCode;

    public IReadOnlyList<ItemDefinitionSO> Items => items;
    public bool IsValid { get; private set; }
    public int ValidCount => byId != null ? byId.Count : 0;

    // Build local lookup tables and reject invalid or duplicate item identities.
    public void Initialize()
    {
        byId = new Dictionary<int, ItemDefinitionSO>();
        byCode = new Dictionary<string, ItemDefinitionSO>(StringComparer.OrdinalIgnoreCase);
        IsValid = true;

        foreach (ItemDefinitionSO definition in items)
        {
            if (definition == null)
            {
                Debug.LogError("[ItemCatalog] Null ItemDefinition entry.", this);
                IsValid = false;
                continue;
            }

            string code = definition.ItemCode?.Trim();
            if (definition.ItemId <= 0 || string.IsNullOrEmpty(code))
            {
                Debug.LogError($"[ItemCatalog] Invalid item ID or code: {definition.name}.", definition);
                IsValid = false;
                continue;
            }

            if (byId.ContainsKey(definition.ItemId) || byCode.ContainsKey(code))
            {
                Debug.LogError($"[ItemCatalog] Duplicate ID={definition.ItemId} or code={code}: {definition.name}.", definition);
                IsValid = false;
                continue;
            }

            byId.Add(definition.ItemId, definition);
            byCode.Add(code, definition);
        }

        Debug.Log($"[ItemCatalog] Local definitions: {byId.Count}/{items.Count}; valid={IsValid}.", this);
    }

    // Resolve a definition using its persistent numeric item ID.
    public ItemDefinitionSO GetById(int itemId)
    {
        if (byId == null) Initialize();
        return byId.TryGetValue(itemId, out ItemDefinitionSO definition) ? definition : null;
    }

    // Resolve a definition using its stable item code.
    public ItemDefinitionSO GetByCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        if (byCode == null) Initialize();
        return byCode.TryGetValue(code.Trim(), out ItemDefinitionSO definition) ? definition : null;
    }

    // Check whether a persistent item ID exists in local definitions.
    public bool Contains(int itemId)
    {
        return GetById(itemId) != null;
    }

#if UNITY_EDITOR
    // Rebuild lookup tables after changes to the catalog asset.
    private void OnValidate()
    {
        byId = null;
        byCode = null;
    }
#endif
}
