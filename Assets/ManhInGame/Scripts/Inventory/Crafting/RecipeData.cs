using System;
using System.Collections.Generic;
using UnityEngine;

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
    public string recipeId;
    public string displayName;

    [TextArea]
    public string description;

    public List<RecipeIngredient> ingredients = new();

    [Min(1)]
    public int outputItemId;

    [Min(1)]
    public int outputAmount = 1;

    public bool isActive = true;
}