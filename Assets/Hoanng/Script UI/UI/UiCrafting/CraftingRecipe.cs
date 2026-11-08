using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CraftingRecipe
{
    public string recipeID;
    public string recipeName;
    public Sprite productIcon;
    public int productAmount = 1;

    // Danh sách nguyên li?u c?n thi?t
    public List<ResourceRequirement> requirements;
}

[System.Serializable]
public class ResourceRequirement
{
    public string resourceID;
    public string resourceName;
    public int requiredAmount;
}