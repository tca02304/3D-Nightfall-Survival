using System;
using System.Collections.Generic;
using UnityEngine;

public enum CraftingStationType
{
    Hand = 0,
    BasicWorkbench = 1,
    AdvancedWorkbench = 2,
    Smelter = 3,
    AetherForge = 4,
    Campfire = 5,
    AetherAltar = 6
}

[Serializable]
public class RecipeIngredient
{
    [Min(1)]
    public int itemId;

    [Min(1)]
    public int amount = 1;
}

[CreateAssetMenu(
    fileName = "Recipe_New",
    menuName = "Nightfall/Crafting/Recipe"
)]
public class RecipeData : ScriptableObject
{
    [Header("Recipe Information")]
    public string recipeId;
    public string displayName;

    [TextArea(2, 4)]
    public string description;

    [Header("Crafting Requirements")]
    public CraftingStationType requiredStation = CraftingStationType.Hand;

    public List<RecipeIngredient> ingredients = new();

    [Header("Crafting Output")]
    [Min(1)]
    public int outputItemId;

    [Min(1)]
    public int outputAmount = 1;

    [Header("Presentation")]
    public Sprite recipeIcon;

    [Min(0f)]
    public float craftingTime = 0f;

    [Header("Status")]
    public bool isActive = true;
}