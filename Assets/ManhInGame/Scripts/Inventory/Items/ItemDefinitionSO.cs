using UnityEngine;

[CreateAssetMenu(fileName = "ItemDefinition", menuName = "Nightfall/Inventory/Item Definition")]
public sealed class ItemDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField, Min(1)] private int itemId;
    [SerializeField] private string itemCode;

    [Header("Display")]
    [SerializeField] private string displayName;
    [SerializeField, TextArea(2, 4)] private string description;
    [SerializeField] private Sprite icon;

    [Header("Inventory Rules")]
    [SerializeField, Min(1)] private int maxStack = 1;
    [SerializeField] private string itemType;
    [SerializeField, Min(0)] private int buyPrice;
    [SerializeField, Min(0)] private int sellPrice;
    [SerializeField] private bool canSell;
    [SerializeField] private bool isActive = true;

    [Header("Optional Gameplay Stats")]
    [SerializeField] private WeaponStatData weaponStat;
    [SerializeField] private ToolStatData toolStat;
    [SerializeField] private ConsumableStatData consumableStat;

    [Header("World Item")]
    [SerializeField] private WorldItem worldPrefab;

    public int ItemId => itemId;
    public string ItemCode => itemCode;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public int MaxStack => maxStack;
    public string ItemType => itemType;
    public int BuyPrice => buyPrice;
    public int SellPrice => canSell ? sellPrice : 0;
    public bool CanSell => canSell;
    public bool IsActive => isActive;
    public WeaponStatData WeaponStat => weaponStat;
    public ToolStatData ToolStat => toolStat;
    public ConsumableStatData ConsumableStat => consumableStat;
    public WorldItem WorldPrefab => worldPrefab;

#if UNITY_EDITOR
    // Normalize identifiers and prevent invalid stack sizes in the Inspector.
    private void OnValidate()
    {
        itemId = Mathf.Max(1, itemId);
        maxStack = Mathf.Max(1, maxStack);
        itemCode = itemCode == null ? string.Empty : itemCode.Trim();
    }
#endif
}
