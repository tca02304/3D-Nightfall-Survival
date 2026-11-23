using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Editor-only importer for the actual WeaponStatData/ToolStatData/ConsumableStatData fields.
public static class NightfallStatSync
{
    private const string JsonPath = "Assets/ManhInGame/Data/ItemSpecs/Nightfall_52_ItemSpecs.json";

    [Serializable] private sealed class Root { public Spec[] items; }
    [Serializable]
    private sealed class Spec
    {
        public string itemCode;
        public string itemType;
        public int maxStack;
        public WeaponSpec weaponStat;
        public ToolSpec toolStat;
        public int healHP;
        public int restoreStamina;
    }
    [Serializable]
    private sealed class WeaponSpec
    {
        public int damage;
        public float attackSpeed;
        public float attackRange;
        public int durability;
        public float criticalChance;
    }
    [Serializable]
    private sealed class ToolSpec
    {
        public int chopPower;
        public int miningPower;
        public int durability;
        public float actionSpeed;
    }

    // Report missing, duplicated, and mismatched ScriptableObject definitions without editing assets.
    [MenuItem("Tools/Nightfall/Items/Preview Gameplay Stat Sync")]
    public static void Preview() { Run(false); }

    // Apply supported stats to the 52 existing ScriptableObject assets, preserving IDs and references.
    [MenuItem("Tools/Nightfall/Items/Apply Gameplay Stat Sync")]
    public static void Apply() { Run(true); }

    // Read the local specs file, locate assets by ItemCode, and perform preview or update.
    private static void Run(bool apply)
    {
        if (!File.Exists(JsonPath))
        {
            Debug.LogError("[StatSync] Missing JSON: " + JsonPath);
            return;
        }
        Root root;
        try { root = JsonUtility.FromJson<Root>(File.ReadAllText(JsonPath)); }
        catch (Exception e) { Debug.LogError("[StatSync] Invalid JSON: " + e.Message); return; }
        if (root == null || root.items == null || root.items.Length != 52)
        {
            Debug.LogError("[StatSync] Expected 52 items in JSON.");
            return;
        }
        var assets = new Dictionary<string, ItemDefinitionSO>(StringComparer.OrdinalIgnoreCase);
        foreach (string guid in AssetDatabase.FindAssets("t:ItemDefinitionSO"))
        {
            var asset = AssetDatabase.LoadAssetAtPath<ItemDefinitionSO>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset == null || string.IsNullOrWhiteSpace(asset.ItemCode)) continue;
            if (!assets.TryAdd(asset.ItemCode.Trim(), asset))
            {
                Debug.LogError("[StatSync] Duplicate ItemCode: " + asset.ItemCode);
                return;
            }
        }
        var expected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        bool valid = true;
        foreach (Spec spec in root.items)
        {
            if (spec == null || string.IsNullOrWhiteSpace(spec.itemCode) || !expected.Add(spec.itemCode))
            {
                Debug.LogError("[StatSync] Duplicate or empty ItemCode in JSON."); valid = false; continue;
            }
            if (!assets.TryGetValue(spec.itemCode, out ItemDefinitionSO asset))
            {
                Debug.LogError("[StatSync] Missing asset: " + spec.itemCode); valid = false; continue;
            }
            if (!string.Equals(asset.ItemType, spec.itemType, StringComparison.OrdinalIgnoreCase) || asset.MaxStack != spec.maxStack)
            {
                Debug.LogError($"[StatSync] Metadata mismatch {spec.itemCode}: assetType={asset.ItemType}, JSONType={spec.itemType}, assetStack={asset.MaxStack}, JSONStack={spec.maxStack}");
                valid = false;
            }
        }
        if (!valid || assets.Count != 52)
        {
            Debug.LogError($"[StatSync] Aborted: {assets.Count} assets found, metadata needs correction.");
            return;
        }
        if (!apply)
        {
            Debug.Log("[StatSync] PREVIEW PASS: 52/52 assets, type/stack aligned. No files changed.");
            return;
        }
        if (!EditorUtility.DisplayDialog("Nightfall Stat Sync",
                "Apply gameplay stats to the 52 existing ItemDefinitionSO assets? Commit Git first.", "Apply", "Cancel")) return;
        foreach (Spec spec in root.items)
        {
            ItemDefinitionSO asset = assets[spec.itemCode];
            var obj = new SerializedObject(asset);
            if (spec.weaponStat != null)
            {
                SerializedProperty p = obj.FindProperty("weaponStat");
                SetInt(p, "itemId", asset.ItemId);
                SetInt(p, "damage", spec.weaponStat.damage);
                SetFloat(p, "attackSpeed", spec.weaponStat.attackSpeed);
                SetFloat(p, "attackRange", spec.weaponStat.attackRange);
                SetInt(p, "durability", spec.weaponStat.durability);
                SetFloat(p, "criticalChance", spec.weaponStat.criticalChance);
            }
            if (spec.toolStat != null)
            {
                SerializedProperty p = obj.FindProperty("toolStat");
                SetInt(p, "itemId", asset.ItemId);
                SetString(p, "toolType", spec.itemCode.EndsWith("_axe", StringComparison.OrdinalIgnoreCase) ? "Axe" : "Pickaxe");
                SetInt(p, "toolPower", spec.itemCode.EndsWith("_axe", StringComparison.OrdinalIgnoreCase)
                    ? spec.toolStat.chopPower : spec.toolStat.miningPower);
                string[] tiers = { "wood", "steel", "gold", "mithril", "adamantite" };
                int tier = Array.IndexOf(tiers, spec.itemCode.Split('_')[0]) + 1;
                SetInt(p, "toolTier", tier);
                SetInt(p, "durability", spec.toolStat.durability);
                SetFloat(p, "actionSpeed", spec.toolStat.actionSpeed);
            }
            if (string.Equals(spec.itemType, "Consumable", StringComparison.OrdinalIgnoreCase))
            {
                SerializedProperty p = obj.FindProperty("consumableStat");
                SetInt(p, "itemId", asset.ItemId);
                SetInt(p, "healHP", spec.healHP);
                SetInt(p, "restoreStamina", spec.restoreStamina);
            }
            obj.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[StatSync] Applied stats including ToolTier, ToolPower, action speed, durability and weapon critical chance.");
    }

    // Update a matching serialized integer without changing ScriptableObject identity.
    private static void SetInt(SerializedProperty parent, string field, int value)
    {
        SerializedProperty property = parent?.FindPropertyRelative(field);
        if (property != null) property.intValue = value;
    }

    // Update a matching serialized float if this gameplay stat is supported.
    private static void SetFloat(SerializedProperty parent, string field, float value)
    {
        SerializedProperty property = parent?.FindPropertyRelative(field);
        if (property != null) property.floatValue = value;
    }

    // Update a matching serialized text field if available.
    private static void SetString(SerializedProperty parent, string field, string value)
    {
        SerializedProperty property = parent?.FindPropertyRelative(field);
        if (property != null) property.stringValue = value;
    }
}
