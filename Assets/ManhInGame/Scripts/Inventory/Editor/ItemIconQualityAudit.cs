#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ItemIconQualityAudit
{
    // Inspect the import settings of all distinct icon textures referenced by item assets.
    [MenuItem("Tools/Nightfall/Items/Audit Icon Quality")]
    public static void Audit()
    {
        string[] ids = AssetDatabase.FindAssets("t:ItemDefinitionSO");
        var checkedPaths = new HashSet<string>();
        int found = 0;
        int warnings = 0;

        foreach (string guid in ids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            ItemDefinitionSO definition = AssetDatabase.LoadAssetAtPath<ItemDefinitionSO>(assetPath);
            if (definition == null || definition.Icon == null)
                continue;

            string texturePath = AssetDatabase.GetAssetPath(definition.Icon);
            if (!checkedPaths.Add(texturePath))
                continue;

            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer == null)
                continue;

            found++;
            bool lowQuality = importer.textureCompression != TextureImporterCompression.Uncompressed ||
                              importer.mipmapEnabled || importer.maxTextureSize < 512;

            if (lowQuality)
            {
                warnings++;
                Debug.LogWarning($"[IconAudit] Check {texturePath}: " +
                    $"max={importer.maxTextureSize}, compression={importer.textureCompression}, " +
                    $"mipmaps={importer.mipmapEnabled}, filter={importer.filterMode}.", definition);
            }
        }

        Debug.Log($"[IconAudit] Checked {found} icon textures; {warnings} require import-setting review. " +
                  "Audit does not modify any assets.");
    }
}
#endif
