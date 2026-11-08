#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generates RecipeData assets from a reviewed, static catalog.
/// IDs are sourced from ItemCodeIds.csv exported/reviewed against YOUR database.
/// Does not touch backend, ItemDatabase, existing player inventory, or RecipeData class.
/// </summary>
public static class NightfallRecipeImporter
{
    private const string CatalogRoot = "Assets/ManhInGame/Data/Crafting";
    private const string RecipeCsv = CatalogRoot + "/RecipeCatalog.csv";
    private const string IdCsv = CatalogRoot + "/ItemCodeIds.csv";
    private const string Output = CatalogRoot + "/Generated";

    [MenuItem("Nightfall/Crafting/Generate Recipe Assets")]
    public static void Generate()
    {
        if (!File.Exists(RecipeCsv) || !File.Exists(IdCsv))
        {
            Debug.LogError($"[Recipes] Missing CSV. Expected: {RecipeCsv} and {IdCsv}");
            return;
        }

        // ItemIds must be refreshed if database was reseeded or IDs changed.
        var ids = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (string line in File.ReadAllLines(IdCsv).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            string[] cells = line.Trim().Split(',');
            if (cells.Length != 2 || !int.TryParse(cells[1], out int id) || id <= 0)
                throw new FormatException("Invalid ItemCodeIds.csv row: " + line);
            if (!ids.TryAdd(cells[0].Trim(), id))
                throw new InvalidOperationException("Duplicate ItemCode in CSV: " + cells[0]);
        }
        if (!AssetDatabase.IsValidFolder(Output))
        {
            string parent = CatalogRoot;
            AssetDatabase.CreateFolder(parent, "Generated");
        }

        int created = 0, updated = 0, skipped = 0;
        var recipeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string raw in File.ReadAllLines(RecipeCsv).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            // CSV uses no quoted/comma-containing values. Ingredients are semicolon-separated.
            string[] columns = raw.Trim().Split(',');
            if (columns.Length != 7)
            {
                Debug.LogError("[Recipes] Invalid recipe row: " + raw);
                skipped++;
                continue;
            }
            string recipeId = columns[0].Trim();
            string displayName = columns[1].Trim();
            string station = columns[2].Trim();
            string outputCode = columns[3].Trim();
            string source = columns[6].Trim();
            if (!recipeIds.Add(recipeId))
            {
                Debug.LogError("[Recipes] Duplicate RecipeId: " + recipeId);
                skipped++;
                continue;
            }
            if (!int.TryParse(columns[4], out int outputAmount) || outputAmount <= 0 ||
                !Enum.TryParse(station, true, out CraftingStationType stationType))
            {
                Debug.LogError("[Recipes] Invalid amount/station: " + recipeId);
                skipped++;
                continue;
            }
            var missing = new List<string>();
            if (!ids.TryGetValue(outputCode, out int outputId)) missing.Add(outputCode);
            var ingredients = new List<RecipeIngredient>();
            foreach (string part in columns[5].Split(';'))
            {
                string[] piece = part.Split(':');
                if (piece.Length != 2 || !int.TryParse(piece[1], out int qty) || qty <= 0)
                {
                    missing.Add("invalid ingredient " + part);
                    continue;
                }
                if (!ids.TryGetValue(piece[0], out int ingredientId))
                {
                    missing.Add(piece[0]);
                    continue;
                }
                ingredients.Add(new RecipeIngredient { itemId = ingredientId, amount = qty });
            }
            if (missing.Count > 0)
            {
                Debug.LogWarning($"[Recipes] SKIP {recipeId}: missing ItemCodes: {string.Join(", ", missing)}");
                skipped++;
                continue;
            }

            string path = Output + "/Recipe_" + recipeId + ".asset";
            RecipeData recipe = AssetDatabase.LoadAssetAtPath<RecipeData>(path);
            bool isNew = recipe == null;
            if (isNew) recipe = ScriptableObject.CreateInstance<RecipeData>();
            recipe.recipeId = recipeId;
            recipe.displayName = displayName;
            recipe.description = "Imported from Muck reference; " + source + ". Station mapped to Nightfall enum.";
            recipe.requiredStation = stationType;
            recipe.ingredients = ingredients;
            recipe.outputItemId = outputId;
            recipe.outputAmount = outputAmount;
            recipe.isActive = true;
            // Preserve recipeIcon and craftingTime for existing assets.
            if (isNew)
            {
                AssetDatabase.CreateAsset(recipe, path);
                created++;
            }
            else
            {
                EditorUtility.SetDirty(recipe);
                updated++;
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Recipes] Created {created}, updated {updated}, skipped {skipped}. " +
                  "Skipped recipes need missing ItemCodes added to DB and refreshed in ItemCodeIds.csv.");
    }

    [MenuItem("Nightfall/Crafting/Assign Generated Recipes To Selected CraftingManager")]
    public static void AssignSelected()
    {
        if (Selection.activeGameObject == null ||
            !Selection.activeGameObject.TryGetComponent(out CraftingManager manager))
        {
            Debug.LogError("[Recipes] Select Managers GameObject with CraftingManager component.");
            return;
        }
        string[] guids = AssetDatabase.FindAssets("t:RecipeData", new[] { Output });
        RecipeData[] recipes = guids
            .Select(guid => AssetDatabase.LoadAssetAtPath<RecipeData>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(recipe => recipe != null)
            .OrderBy(recipe => recipe.recipeId, StringComparer.Ordinal)
            .ToArray();
        SerializedObject target = new SerializedObject(manager);
        SerializedProperty list = target.FindProperty("recipes");
        if (list == null)
        {
            Debug.LogError("[Recipes] CraftingManager does not have serialized 'recipes' field.");
            return;
        }
        Undo.RecordObject(manager, "Assign generated recipes");
        list.arraySize = recipes.Length;
        for (int i = 0; i < recipes.Length; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = recipes[i];
        target.ApplyModifiedProperties();
        EditorUtility.SetDirty(manager);
        Debug.Log($"[Recipes] Assigned {recipes.Length} generated recipes to {manager.name}. " +
                  "Existing hand-made recipes in the list were replaced; undo if necessary.");
    }
}
#endif
