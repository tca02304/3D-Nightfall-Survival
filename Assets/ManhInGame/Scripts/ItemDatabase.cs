using System.Collections.Generic;
using UnityEngine;

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance;

    private Dictionary<int, ItemData> items = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
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
        if(items.TryGetValue(itemId, out ItemData item))
        {
            return item;
        }

        return null;
    }
}
