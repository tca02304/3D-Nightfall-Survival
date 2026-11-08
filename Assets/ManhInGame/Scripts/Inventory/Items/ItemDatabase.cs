using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ItemIconEntry
{
    public string iconKey;
    public Sprite icon;
}

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance;

    [Header("Item Data")]
    private Dictionary<int, ItemData> items = new();

    [Header("Item Icon")]
    [SerializeField] private List<ItemIconEntry> iconEntries = new();

    private Dictionary<string, Sprite> iconDictionary = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        LoadIcon();
    }

    private void LoadIcon()
    {
        iconDictionary.Clear();

        foreach(ItemIconEntry entry in iconEntries)
        {
            if (string.IsNullOrWhiteSpace(entry.iconKey) || entry.icon == null)
                continue;

            if(!iconDictionary.TryAdd(entry.iconKey, entry.icon))
            {
                Debug.LogWarning($"Dublicate IconKey: {entry.iconKey}");
            }
        }

        Debug.Log("Item Icons loaded: " + iconDictionary.Count);
    }

    public void SetItems(ItemData[] itemList)
    {
        items.Clear();

        foreach(ItemData item in itemList)
        {
            items[item.id] = item;
        }

        Debug.Log("ItemDatabase loaded: " + items.Count + "items");
    }

    public ItemData GetItemById(int itemId)
    {
        items.TryGetValue(itemId, out ItemData item);
        return item;
    }

    public Sprite GetIconByKey(string iconKey)
    {
        if (string.IsNullOrWhiteSpace(iconKey))
            return null;

        iconDictionary.TryGetValue(iconKey, out Sprite icon);
        return icon;
    }
}
