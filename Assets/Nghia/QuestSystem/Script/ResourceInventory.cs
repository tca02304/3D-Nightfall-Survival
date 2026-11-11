using System;
using UnityEngine;

public class ResourceInventory : MonoBehaviour
{
    public static ResourceInventory Instance { get; private set; }

    public event Action<ResourceType, int> OnResourceChanged;

    [SerializeField] private int stone;
    [SerializeField] private int wood;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void AddResource(ResourceType type, int amount)
    {
        if (amount <= 0) return;

        switch (type)
        {
            case ResourceType.Stone:
                stone += amount;
                break;

            case ResourceType.Wood:
                wood += amount;
                break;
        }

        OnResourceChanged?.Invoke(type, GetAmount(type));

        Debug.Log($"Collected {type}: {GetAmount(type)}");
    }

    public int GetAmount(ResourceType type)
    {
        switch (type)
        {
            case ResourceType.Stone:
                return stone;

            case ResourceType.Wood:
                return wood;

            default:
                return 0;
        }
    }
}