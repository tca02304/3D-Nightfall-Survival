using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [SerializeField] private int inventorySize = 28;

    public List<InventorySlotData> Slots = new();

    public event Action OnInvenroryChanged;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        CreateInventory();
    }

    private void CreateInventory()
    {
        Slots.Clear();

        for(int i = 0; i< inventorySize; i++)
        {
            Slots.Add(new InventorySlotData());
        }
    }
}
