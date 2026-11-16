#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class ItemDefinitionImporter
{
    private const string JsonPath = "Assets/ManhInGame/Data/ItemSpecs/Nightfall_52_ItemSpecs.json";

    [Serializable]
    private sealed class Root { public Record[] items; }

    [Serializable]
    private sealed class WeaponRecord
    {
        public float damage;
        public float attackSpeed;
        public float durability;
        public float criticalChance;
        public float attackRange;
    }

    [Serializable]
    private sealed class ToolRecord
    {
        public float chopPower;
        public float miningPower;
        public float durability;
        public float actionSpeed;
    }

    [Serializable]
    private sealed class Record
    {
        public string itemCode;
        public string displayName;
        public string description;
        public string itemType;
        public int maxStack;
        public bool canSellRecommended;
        public int sellPriceRecommended;
        public int healHP;
        public int restoreStamina;
        public bool isActive;
        public WeaponRecord weaponStat;
        public ToolRecord toolStat;
    }

    // Preview all changes without modifying Unity assets.
    [MenuItem("Tools/Nightfall/Items/Preview 52 Item Import")]
    public static void Preview() => Run(false);

    // Import the validated JSON data into existing ItemDefinitionSO assets.
    [MenuItem("Tools/Nightfall/Items/Import 52 Item Definitions")]
    public static void Import() => Run(true);

    // Validate IDs and unique codes, then optionally update matching assets.
    private static void Run(bool apply)
    {
        string absolute = Path.GetFullPath(JsonPath);
        if (!File.Exists(absolute))
        {
            Debug.LogError($"[ItemImport] Missing JSON: {absolute}");
            return;
        }

        Root root;
        try { root = JsonUtility.FromJson<Root>(File.ReadAllText(absolute)); }
        catch (Exception ex) { Debug.LogError($"[ItemImport] Invalid JSON: {ex.Message}"); return; }
        if (root?.items == null || root.items.Length != 52)
        {
            Debug.LogError("[ItemImport] Expected exactly 52 items.");
            return;
        }

        var assets = new Dictionary<string, ItemDefinitionSO>(StringComparer.OrdinalIgnoreCase);
        var duplicates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string guid in AssetDatabase.FindAssets("t:ItemDefinitionSO"))
        {
            ItemDefinitionSO asset = AssetDatabase.LoadAssetAtPath<ItemDefinitionSO>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset == null || string.IsNullOrWhiteSpace(asset.ItemCode)) continue;
            if (!assets.TryAdd(asset.ItemCode.Trim(), asset)) duplicates.Add(asset.ItemCode.Trim());
        }

        var mappings = new List<(Record record, ItemDefinitionSO asset)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var errors = new List<string>();
        var ids = new HashSet<int>();
        foreach (Record r in root.items)
        {
            if (r == null || string.IsNullOrWhiteSpace(r.itemCode)) { errors.Add("Empty ItemCode"); continue; }
            string code = r.itemCode.Trim();
            if (!seen.Add(code)) { errors.Add($"Duplicate JSON ItemCode: {code}"); continue; }
            if (duplicates.Contains(code)) { errors.Add($"Duplicate Unity ItemCode: {code}"); continue; }
            if (!assets.TryGetValue(code, out ItemDefinitionSO asset)) { errors.Add($"Missing Unity asset: {code}"); continue; }
            if (asset.ItemId <= 0 || !ids.Add(asset.ItemId)) { errors.Add($"Invalid/duplicate Unity ItemId: {code}, {asset.ItemId}"); continue; }
            if (r.maxStack != (r.itemType == "Weapon" || r.itemType == "Tool" ? 1 : 64))
                errors.Add($"Invalid stack limit for {code}: {r.maxStack}");
            mappings.Add((r, asset));
        }
        foreach (string code in assets.Keys)
            if (!seen.Contains(code)) Debug.LogWarning($"[ItemImport] Asset not listed in JSON: {code}");
        foreach (string error in errors) Debug.LogError("[ItemImport] " + error);
        if (errors.Count > 0 || mappings.Count != 52)
        {
            EditorUtility.DisplayDialog("Import blocked", $"Matched {mappings.Count}/52; errors={errors.Count}. See Console.", "OK");
            return;
        }

        if (!apply)
        {
            foreach (var pair in mappings)
                Debug.Log($"[ItemImport] MATCH {pair.record.itemCode}: type={pair.record.itemType}, stack={pair.record.maxStack}, sell={pair.record.canSellRecommended}; ItemId preserved={pair.asset.ItemId}");
            EditorUtility.DisplayDialog("Preview complete", "Matched 52/52. No assets changed. See Console.", "OK");
            return;
        }
        if (!EditorUtility.DisplayDialog("Import 52 Item definitions?",
            "Update metadata, economy and compatible gameplay stat fields? Existing ItemId, ItemCode, Icon and WorldPrefab will be preserved. Commit to Git first.",
            "Import", "Cancel")) return;

        int statSuccess = 0;
        var warnings = new StringBuilder();
        foreach (var pair in mappings)
        {
            Record r = pair.record;
            ItemDefinitionSO asset = pair.asset;
            Undo.RecordObject(asset, "Import Nightfall item balance");
            SerializedObject so = new SerializedObject(asset);
            Set(so, "displayName", r.displayName);
            Set(so, "description", r.description);
            Set(so, "itemType", r.itemType);
            Set(so, "maxStack", r.maxStack);
            Set(so, "isActive", r.isActive);
            Set(so, "buyPrice", 0);
            Set(so, "canSell", r.canSellRecommended);
            Set(so, "sellPrice", r.canSellRecommended ? r.sellPriceRecommended : 0);
            so.ApplyModifiedProperties();

            if (r.weaponStat != null)
            {
                int updated = SetStat(asset, "weaponStat", new (string[], float)[] {
                    (new[]{"damage", "baseDamage", "attackDamage"}, r.weaponStat.damage),
                    (new[]{"attackSpeed", "speed"}, r.weaponStat.attackSpeed),
                    (new[]{"durability", "maxDurability"}, r.weaponStat.durability),
                    (new[]{"criticalChance", "critChance"}, r.weaponStat.criticalChance),
                    (new[]{"attackRange", "range"}, r.weaponStat.attackRange)
                });
                if (updated == 0) warnings.AppendLine($"WeaponStat not mapped: {r.itemCode}"); else statSuccess++;
            }
            if (r.toolStat != null)
            {
                int updated = SetStat(asset, "toolStat", new (string[], float)[] {
                    (new[]{"chopPower", "choppingPower", "woodPower"}, r.toolStat.chopPower),
                    (new[]{"miningPower", "minePower"}, r.toolStat.miningPower),
                    (new[]{"durability", "maxDurability"}, r.toolStat.durability),
                    (new[]{"actionSpeed", "gatherSpeed", "speed"}, r.toolStat.actionSpeed)
                });
                if (updated == 0) warnings.AppendLine($"ToolStat not mapped: {r.itemCode}"); else statSuccess++;
            }
            if (r.itemType == "Consumable")
            {
                int updated = SetStat(asset, "consumableStat", new (string[], float)[] {
                    (new[]{"healHP", "healHp", "healAmount", "healthRestore"}, r.healHP),
                    (new[]{"restoreStamina", "staminaRestore"}, r.restoreStamina)
                });
                if (updated == 0) warnings.AppendLine($"ConsumableStat not mapped: {r.itemCode}"); else statSuccess++;
            }
            EditorUtility.SetDirty(asset);
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[ItemImport] Updated 52 assets; stat objects matched={statSuccess}/27.\n{warnings}");
        EditorUtility.DisplayDialog("Import complete", $"52 Item assets updated. Stat objects matched: {statSuccess}/27. Check Console for fields needing mapping.", "OK");
    }

    // Assign a serialized property if the target asset exposes the field.
    private static void Set(SerializedObject obj, string name, object value)
    {
        SerializedProperty prop = obj.FindProperty(name);
        if (prop == null) { Debug.LogWarning($"[ItemImport] Property not found: {name}"); return; }
        if (value is string s) prop.stringValue = s;
        else if (value is bool b) prop.boolValue = b;
        else if (value is int i) prop.intValue = i;
    }

    // Update recognized numeric stat members without assuming the full gameplay model.
    private static int SetStat(ItemDefinitionSO asset, string fieldName, (string[] names, float value)[] values)
    {
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        FieldInfo hostField = typeof(ItemDefinitionSO).GetField(fieldName, flags);
        if (hostField == null || !hostField.FieldType.IsSerializable || typeof(UnityEngine.Object).IsAssignableFrom(hostField.FieldType))
        {
            Debug.LogWarning($"[ItemImport] {asset.ItemCode}: {fieldName} missing or is not a serializable plain class.");
            return 0;
        }
        object stat = hostField.GetValue(asset);
        if (stat == null)
        {
            try { stat = Activator.CreateInstance(hostField.FieldType); }
            catch (Exception ex) { Debug.LogWarning($"[ItemImport] {asset.ItemCode} {fieldName}: {ex.Message}"); return 0; }
        }
        int updated = 0;
        foreach (var pair in values)
        {
            bool assigned = false;
            foreach (string name in pair.names)
            {
                FieldInfo f = stat.GetType().GetField(name, flags | BindingFlags.IgnoreCase);
                if (f == null) continue;
                if (f.FieldType == typeof(float)) f.SetValue(stat, pair.value);
                else if (f.FieldType == typeof(int)) f.SetValue(stat, Mathf.RoundToInt(pair.value));
                else continue;
                assigned = true; updated++; break;
            }
            if (!assigned) Debug.LogWarning($"[ItemImport] {asset.ItemCode}: {fieldName}.{pair.names[0]} not mapped; inspect your StatData class.");
        }
        if (updated > 0) hostField.SetValue(asset, stat);
        return updated;
    }
}
#endif
