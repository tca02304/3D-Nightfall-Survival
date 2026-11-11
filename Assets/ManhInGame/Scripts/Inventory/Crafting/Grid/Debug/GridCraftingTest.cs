using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Read-only matcher checks; does not modify player inventory or backend.</summary>
public sealed class GridCraftingTest : MonoBehaviour
{
    [SerializeField] private GridCraftingController crafting;

    [ContextMenu("TEST / Validate Grid Recipe Catalog")]
    public void Run()
    {
        if (crafting == null) crafting = GetComponent<GridCraftingController>();
        if (crafting == null) { Debug.LogError("[GridCraftingTest] Assign GridCraftingController."); return; }
        var recipes = crafting.Recipes;
        int passed = 0, total = 0;
        var shapeKeys = new HashSet<string>();
        foreach (var recipe in recipes)
        {
            total++;
            bool exact = GridRecipeCatalog.Matches(recipe.cells, recipe.cells);
            if (exact) passed++; else Debug.LogError("[GridCraftingTest] FAILED exact " + recipe.id);
            total++;
            GridIngredient[] empty = new GridIngredient[9];
            if (!GridRecipeCatalog.Matches(empty, recipe.cells)) passed++;
            else Debug.LogError("[GridCraftingTest] FAILED empty " + recipe.id);
            total++;
            bool unitOnly = true;
            foreach (GridIngredient ingredient in recipe.cells)
                if (ingredient.itemId > 0 && ingredient.amount != 1) unitOnly = false;
            if (unitOnly) passed++; else Debug.LogError("[GridCraftingTest] Non-unit ingredient " + recipe.id);
            // Distinguishable recipe signatures within same station.
            var signature = recipe.station + "|" + TightSignature(recipe.cells);
            total++;
            if (shapeKeys.Add(signature)) passed++;
            else Debug.LogError("[GridCraftingTest] Ambiguous shape for " + recipe.id);
        }
        Debug.Log($"[GridCraftingTest] {passed}/{total} structural checks passed; " +
            "this does NOT validate inventory capacity or network authority.");
    }

    private static string TightSignature(GridIngredient[] grid)
    {
        // Canonical by shifting to top left; includes per-cell amounts.
        int r0 = 3, c0 = 3, r1 = -1, c1 = -1;
        for (int i = 0; i < 9; i++)
        {
            if (grid[i].itemId <= 0) continue;
            r0 = Math.Min(r0, i / 3); c0 = Math.Min(c0, i % 3);
            r1 = Math.Max(r1, i / 3); c1 = Math.Max(c1, i % 3);
        }
        if (r1 < 0) return "EMPTY";
        var rows = new List<string>();
        for (int r = r0; r <= r1; r++)
        {
            var cols = new List<string>();
            for (int c = c0; c <= c1; c++)
            {
                var cell = grid[r * 3 + c];
                cols.Add(cell.itemId + ":" + cell.amount);
            }
            rows.Add(string.Join("/", cols));
        }
        return string.Join("|", rows);
    }
}
