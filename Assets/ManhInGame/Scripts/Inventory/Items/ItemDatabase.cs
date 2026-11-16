using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance { get; private set; }

    [Header("Offline Item Definitions")]
    [SerializeField] private ItemCatalog catalog;

    private readonly Dictionary<int, ItemData> items = new();
    private readonly Dictionary<string, Sprite> icons = new(StringComparer.OrdinalIgnoreCase);

    public ItemCatalog Catalog => catalog;
    public bool IsReady { get; private set; }

    // Register the singleton and load item definitions entirely from local ScriptableObjects.
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        LoadCatalog();
    }

    // Recreate legacy runtime ItemData and icon lookups without using an API.
    public void LoadCatalog()
    {
        IsReady = false;
        items.Clear();
        icons.Clear();

        if (catalog == null)
        {
            Debug.LogError("[ItemDatabase] Assign ItemCatalog.asset on Managers.", this);
            return;
        }

        catalog.Initialize();
        if (!catalog.IsValid)
        {
            Debug.LogError("[ItemDatabase] Invalid ItemCatalog; fix duplicate or missing IDs before starting gameplay.", this);
            return;
        }

        foreach (ItemDefinitionSO definition in catalog.Items)
        {
            if (definition == null) continue;

            ItemData item = CreateItemData(definition);
            items.Add(item.id, item);
            if (definition.Icon != null)
                icons.Add(definition.ItemCode, definition.Icon);
        }

        IsReady = true;
        Debug.Log($"[ItemDatabase] OFFLINE READY: {items.Count} ItemDefinitions.", this);
    }

    // Adapt a ScriptableObject into the ItemData model used by existing gameplay systems.
    private static ItemData CreateItemData(ItemDefinitionSO definition)
    {
        return new ItemData
        {
            id = definition.ItemId,
            itemCode = definition.ItemCode,
            name = string.IsNullOrWhiteSpace(definition.DisplayName) ? definition.ItemCode : definition.DisplayName,
            description = definition.Description,
            type = definition.ItemType,
            maxStack = definition.MaxStack,
            buyPrice = definition.BuyPrice,
            sellPrice = definition.SellPrice,
            isActive = definition.IsActive,
            iconKey = definition.ItemCode,
            weaponStat = definition.WeaponStat,
            toolStat = definition.ToolStat,
            consumableStat = definition.ConsumableStat
        };
    }

    // Reject legacy network-fed definitions to keep the local catalog authoritative.
    [Obsolete("Item definitions are local. Remove callers of SetItems and use ItemCatalog assets.")]
    public void SetItems(ItemData[] remoteItems)
    {
        Debug.LogWarning("[ItemDatabase] SetItems ignored: item definitions are now local-only.", this);
    }

    // Resolve a compatibility ItemData object by persistent item ID.
    public ItemData GetItemById(int itemId)
    {
        return items.TryGetValue(itemId, out ItemData item) ? item : null;
    }

    // Check whether an Item can be sold according to its local ScriptableObject policy.
    public bool CanSell(int itemId)
    {
        ItemDefinitionSO definition = GetDefinitionById(itemId);
        return definition != null && definition.IsActive && definition.CanSell && definition.SellPrice > 0;
    }

    // Return the sale value of one Item, or zero if selling is not allowed.
    public int GetSellPrice(int itemId)
    {
        ItemDefinitionSO definition = GetDefinitionById(itemId);
        return CanSell(itemId) ? definition.SellPrice : 0;
    }

    // Resolve an icon from the item code used as the local icon key.
    public Sprite GetIconByKey(string iconKey)
    {
        if (string.IsNullOrWhiteSpace(iconKey)) return null;
        return icons.TryGetValue(iconKey, out Sprite icon) ? icon : null;
    }

    // Resolve the ScriptableObject definition for systems that need typed metadata.
    public ItemDefinitionSO GetDefinitionById(int itemId)
    {
        return catalog != null ? catalog.GetById(itemId) : null;
    }

    // Resolve a world prefab directly from the local item definition.
    public WorldItem GetWorldPrefab(int itemId)
    {
        ItemDefinitionSO definition = GetDefinitionById(itemId);
        return definition != null ? definition.WorldPrefab : null;
    }

    // Release singleton ownership when this component is destroyed.
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
