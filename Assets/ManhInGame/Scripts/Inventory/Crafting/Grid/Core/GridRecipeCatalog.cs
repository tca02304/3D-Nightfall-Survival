using System;
using System.Collections.Generic;
using UnityEngine;

// Pure runtime data. No ScriptableObject is created or required.
[Serializable]
public sealed class GridRecipe
{
    public string id;
    public string displayName;
    public string station;
    public int outputId;
    public string outputCode;
    public int outputAmount;
    public GridIngredient[] cells = new GridIngredient[9];
}

[Serializable]
public struct GridIngredient
{
    public int itemId;
    public int amount;
    public GridIngredient(int itemId, int amount)
    {
        this.itemId = itemId;
        this.amount = amount;
    }
}

public static class GridRecipeCatalog
{
    public static List<GridRecipe> Parse(TextAsset recipeCsv, TextAsset idsCsv)
    {
        if (recipeCsv == null || idsCsv == null)
            throw new InvalidOperationException("Assign both GridRecipes.csv and ItemCodeIds.csv.");

        var ids = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in Rows(idsCsv.text))
        {
            if (row.Length < 2 || !int.TryParse(row[1], out int id) || id <= 0)
                throw new FormatException("Invalid ItemCodeIds.csv row: " + string.Join(",", row));
            if (!ids.TryAdd(row[0], id))
                throw new FormatException("Duplicate ItemCode: " + row[0]);
        }

        var result = new List<GridRecipe>();
        var recipeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in Rows(recipeCsv.text))
        {
            if (row.Length < 6)
                throw new FormatException("GridRecipes.csv needs six columns.");
            if (!recipeIds.Add(row[0]))
                throw new FormatException("Duplicate recipeId: " + row[0]);
            if (!ids.TryGetValue(row[3], out int outputId))
                throw new FormatException("Unknown output itemCode: " + row[3]);
            if (!int.TryParse(row[4], out int outputAmount) || outputAmount < 1)
                throw new FormatException("Invalid outputAmount: " + row[0]);
            var cells = row[5].Split('|');
            if (cells.Length != 9)
                throw new FormatException("Recipe " + row[0] + " must contain exactly nine | separated cells.");

            var recipe = new GridRecipe
            {
                id = row[0], displayName = row[1], station = row[2],
                outputCode = row[3], outputId = outputId, outputAmount = outputAmount
            };
            int used = 0;
            for (int i = 0; i < 9; ++i)
            {
                string token = cells[i].Trim();
                if (token == "." || token.Length == 0) continue;
                int split = token.LastIndexOf(':');
                if (split < 1 || !int.TryParse(token.Substring(split + 1), out int amount) || amount != 1)
                    throw new FormatException("Grid cell must contain exactly 1 item: '" + token + "' in " + row[0]);
                string code = token.Substring(0, split);
                if (!ids.TryGetValue(code, out int itemId))
                    throw new FormatException("Unknown ingredient: " + code + " in " + row[0]);
                recipe.cells[i] = new GridIngredient(itemId, amount);
                used++;
            }
            if (used == 0) throw new FormatException("Empty recipe: " + row[0]);
            result.Add(recipe);
        }
        return result;
    }

    // 6 columns are unquoted plain CSV; the sixth column has no commas.
    private static IEnumerable<string[]> Rows(string csv)
    {
        var lines = csv.Replace("\r", "").Split('\n');
        foreach (string raw in lines)
        {
            string line = raw.Trim().TrimStart('\uFEFF');
            if (line.Length == 0 || line.StartsWith("#")) continue;
            string[] columns = line.Split(',');
            if (columns[0].Equals("recipeId", StringComparison.OrdinalIgnoreCase) ||
                columns[0].Equals("itemCode", StringComparison.OrdinalIgnoreCase)) continue;
            for (int i = 0; i < columns.Length; i++) columns[i] = columns[i].Trim();
            yield return columns;
        }
    }

    // Compare tight bounding boxes: a recipe can be shifted within a 3x3 grid.
    // Mirroring/rotating is deliberately NOT allowed (Minecraft-style shaped recipe).
    public static bool Matches(GridIngredient[] placed, GridIngredient[] template)
    {
        if (placed == null || template == null || placed.Length != 9 || template.Length != 9)
            return false;
        Bounds(placed, out int pr0, out int pc0, out int pr1, out int pc1);
        Bounds(template, out int tr0, out int tc0, out int tr1, out int tc1);
        if (pr1 < 0 || tr1 < 0 || pr1 - pr0 != tr1 - tr0 || pc1 - pc0 != tc1 - tc0)
            return false;
        for (int r = 0; r <= pr1 - pr0; r++)
        for (int c = 0; c <= pc1 - pc0; c++)
        {
            GridIngredient p = placed[(pr0 + r) * 3 + pc0 + c];
            GridIngredient t = template[(tr0 + r) * 3 + tc0 + c];
            if (p.itemId != t.itemId || p.amount != t.amount) return false;
        }
        return true;
    }

    private static void Bounds(GridIngredient[] grid, out int minRow, out int minCol,
        out int maxRow, out int maxCol)
    {
        minRow = minCol = 3;
        maxRow = maxCol = -1;
        for (int i = 0; i < 9; i++)
        {
            if (grid[i].itemId <= 0 || grid[i].amount <= 0) continue;
            minRow = Math.Min(minRow, i / 3); minCol = Math.Min(minCol, i % 3);
            maxRow = Math.Max(maxRow, i / 3); maxCol = Math.Max(maxCol, i % 3);
        }
    }
}
