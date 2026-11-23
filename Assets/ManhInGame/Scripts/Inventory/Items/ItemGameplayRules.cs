using System;
using System.Collections.Generic;

public enum GameplayItemKind { Unknown, Material, Weapon, Tool, Consumable }
public enum GatheringKind { None, Axe, Pickaxe }

public static class ItemGameplayRules
{
    private static readonly Dictionary<string,int> OreRanks = new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase)
    { {"stone",0},{"coal",0},{"iron_ore",0},{"gold_ore",1},{"mithril_ore",2},{"adamantite_ore",3},{"obamium_ore",4} };
    private static readonly Dictionary<string,int> WoodRanks = new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase)
    { {"wood",0},{"wood_birch",0},{"wood_fir",1},{"wood_oak",2},{"wood_dark_oak",4} };
    private static readonly string[] TierPrefixes = { "wood", "steel", "gold", "mithril", "adamantite" };

    // Map the existing string-based catalog type to a strict gameplay enum.
    public static GameplayItemKind GetKind(ItemData item)
    {
        if (item == null || !Enum.TryParse(item.type, true, out GameplayItemKind kind) || !Enum.IsDefined(typeof(GameplayItemKind),kind)) return GameplayItemKind.Unknown;
        return kind;
    }

    // Identify the matching tool category without accidentally treating a weapon as a tool.
    public static GatheringKind GetToolKind(ItemData item)
    {
        if (GetKind(item) != GameplayItemKind.Tool || item.toolStat == null) return GatheringKind.None;
        if (string.Equals(item.toolStat.toolType,"Axe",StringComparison.OrdinalIgnoreCase)) return GatheringKind.Axe;
        if (string.Equals(item.toolStat.toolType,"Pickaxe",StringComparison.OrdinalIgnoreCase)) return GatheringKind.Pickaxe;
        return GatheringKind.None;
    }

    // Read the catalog tier, with a stable ItemCode fallback for old assets.
    public static int GetToolTier(ItemData item)
    {
        if (GetToolKind(item) == GatheringKind.None) return 0;
        if (item.toolStat.toolTier > 0) return item.toolStat.toolTier;
        for (int i=0;i<TierPrefixes.Length;i++)
            if (item.itemCode != null && item.itemCode.StartsWith(TierPrefixes[i]+"_",StringComparison.OrdinalIgnoreCase)) return i+1;
        return 0;
    }

    // Resolve the resource family and zero-based rank; Chunkium is a boss reward, not a mineable ore.
    public static bool TryGetResource(string code, out GatheringKind kind, out int rank)
    {
        kind=GatheringKind.None; rank=-1;
        if (string.IsNullOrWhiteSpace(code)) return false;
        if (OreRanks.TryGetValue(code,out rank)) { kind=GatheringKind.Pickaxe; return true; }
        if (WoodRanks.TryGetValue(code,out rank)) { kind=GatheringKind.Axe; return true; }
        rank=-1; return false;
    }

    // Reject incorrect tool category or resource rank above tool tier minus one before any damage/proc.
    public static bool CanGather(ItemData held, string resourceCode)
    {
        if (!TryGetResource(resourceCode,out var kind,out int rank)) return false;
        // Muck starts with a rock that can break basic trees; this avoids a crafting deadlock.
        if (held != null && held.itemCode == "stone") return resourceCode == "wood";
        return GetToolKind(held)==kind && GetToolTier(held)>0 && rank<=GetToolTier(held)-1;
    }

    // Return raw gathering power; invalid tools always yield exactly zero.
    public static float GetBaseGatherDamage(ItemData held, string resourceCode)
    {
        if (!CanGather(held,resourceCode)) return 0f;
        return held.itemCode == "stone" ? 1f : Math.Max(0,held.toolStat.toolPower);
    }
}
