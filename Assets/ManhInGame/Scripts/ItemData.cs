using System;

[Serializable]
public class ItemData
{
    public int id;
    public string itemCode;
    public string name;
    public string description;
    public string type;

    public int maxStack;
    public int price;
    public bool isActive;

    public string iconKey;

    public WeaponStatData weaponStat;
    public ToolStatData toolStat;
}
