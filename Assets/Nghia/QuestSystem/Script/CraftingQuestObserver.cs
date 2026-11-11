
using UnityEngine;

public class CraftingQuestObserver : MonoBehaviour
{
    private void Start()
    {
        if (CraftingManager.Instance == null)
        {
            Debug.LogError("[Quest] Không tìm thấy CraftingManager!");
            return;
        }

        CraftingManager.Instance.OnCrafted += HandleCrafted;

        Debug.Log("[Quest] Crafting Observer đã đăng ký.");
    }

    private void OnDestroy()
    {
        if (CraftingManager.Instance != null)
        {
            CraftingManager.Instance.OnCrafted -= HandleCrafted;
        }
    }

    private void HandleCrafted(RecipeData recipe)
    {
        if (recipe == null) return;

        QuestEvents.Raise(
            QuestObjectiveType.CraftItem,
            recipe.outputItemId.ToString(),
            recipe.outputAmount
        );

        Debug.Log(
            $"[Quest] Crafted Item ID: {recipe.outputItemId}, Amount: {recipe.outputAmount}"
        );
    }
}
