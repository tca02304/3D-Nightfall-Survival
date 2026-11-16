#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class ItemCatalogEditorTools
{
    // Validate every asset and generate a readable item definition report in the Console.
    [MenuItem("Tools/Nightfall/Items/Validate Item Catalog")]
    public static void ValidateCatalog()
    {
        ItemCatalog catalog = FindCatalog();
        if (catalog == null) return;

        catalog.Initialize();
        int errors = catalog.IsValid ? 0 : 1;
        int warnings = 0;
        foreach (ItemDefinitionSO definition in catalog.Items)
        {
            if (definition == null) continue;

            if (definition.Icon == null)
            {
                Debug.LogWarning($"[CatalogAudit] Missing icon: {definition.ItemCode} (ID {definition.ItemId}).", definition);
                warnings++;
            }

            if (definition.WorldPrefab == null)
            {
                Debug.LogWarning($"[CatalogAudit] No WorldPrefab: {definition.ItemCode}; this item cannot be world-dropped.", definition);
                warnings++;
            }
            if (definition.MaxStack < 1)
            {
                Debug.LogError($"[CatalogAudit] MaxStack invalid: {definition.ItemCode}.", definition);
                errors++;
            }
            string type = (definition.ItemType ?? "").Trim().ToLowerInvariant();
            if (type.Contains("weapon") && definition.WeaponStat == null ||
                type.Contains("tool") && definition.ToolStat == null ||
                (type.Contains("consumable") || type.Contains("food") || type.Contains("potion")) && definition.ConsumableStat == null)
            {
                Debug.LogWarning($"[CatalogAudit] Missing expected gameplay stats for {definition.ItemCode} (type={definition.ItemType}).", definition);
                warnings++;
            }
        }
        Debug.Log($"[CatalogAudit] Definitions={catalog.ValidCount}; errors={errors}; warnings={warnings}. Catalog check completed.", catalog);
    }

    // Import existing metadata and stat objects once from a saved backend ItemResponse JSON file.
    [MenuItem("Tools/Nightfall/Items/Import Missing Metadata From JSON")]
    public static void ImportMetadataFromJson()
    {
        ItemCatalog catalog = FindCatalog();
        if (catalog == null) return;
        string path = EditorUtility.OpenFilePanel("Select exported ItemResponse JSON", "", "json");
        if (string.IsNullOrEmpty(path)) return;

        ItemResponse response;
        try
        {
            response = JsonUtility.FromJson<ItemResponse>(File.ReadAllText(path));
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CatalogImport] JSON read failed: {ex.Message}");
            return;
        }

        if (response == null || response.data == null)
        {
            Debug.LogError("[CatalogImport] Expected JSON format: {success:true,data:[...]}");
            return;
        }

        catalog.Initialize();
        int updated = 0;
        int skipped = 0;
        foreach (ItemData remote in response.data)
        {
            if (remote == null) continue;
            ItemDefinitionSO definition = catalog.GetById(remote.id);
            if (definition == null || !string.Equals(definition.ItemCode, remote.itemCode, StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning($"[CatalogImport] ID/Code mismatch or missing local asset: {remote.id}/{remote.itemCode}.");
                skipped++;
                continue;
            }

            Undo.RecordObject(definition, "Import Item Metadata");
            SerializedObject serialized = new SerializedObject(definition);
            serialized.FindProperty("itemType").stringValue = remote.type ?? "";
            serialized.FindProperty("buyPrice").intValue = Mathf.Max(0, remote.buyPrice);
            serialized.FindProperty("sellPrice").intValue = Mathf.Max(0, remote.sellPrice);
            serialized.FindProperty("isActive").boolValue = remote.isActive;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Copy nested stat models when they are serializable classes in this Unity project.
            SetStat(definition, "weaponStat", remote.weaponStat);
            SetStat(definition, "toolStat", remote.toolStat);
            SetStat(definition, "consumableStat", remote.consumableStat);
            EditorUtility.SetDirty(definition);
            updated++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[CatalogImport] Updated {updated} assets, skipped {skipped}. Existing ID, Code, Icon, MaxStack, WorldPrefab were preserved.", catalog);
    }

    // Assign an imported stat object only when its data type can be serialized by Unity.
    private static void SetStat<T>(ItemDefinitionSO definition, string fieldName, T stat) where T : class
    {
        if (stat == null) return;
        Type type = typeof(T);
        if (!type.IsSerializable && !typeof(UnityEngine.Object).IsAssignableFrom(type))
        {
            Debug.LogWarning($"[CatalogImport] {type.Name} is not Unity-serializable; cannot persist {fieldName} on {definition.ItemCode}.", definition);
            return;
        }
        FieldInfo field = typeof(ItemDefinitionSO).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null) field.SetValue(definition, stat);
    }

    // Locate the single ItemCatalog asset used by the current project.
    private static ItemCatalog FindCatalog()
    {
        string[] guids = AssetDatabase.FindAssets("t:ItemCatalog");
        if (guids.Length != 1)
        {
            Debug.LogError($"[CatalogTools] Expected one ItemCatalog asset; found {guids.Length}. Keep one or run the tool on a single catalog.");
            return null;
        }
        string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<ItemCatalog>(assetPath);
    }
}
#endif
