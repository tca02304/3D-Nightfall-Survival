using System;
using UnityEngine;
using UnityEngine.UI;

public class UiRecipeSlot : MonoBehaviour
{
    public Image iconImage;
    public Button slotButton;

    private CraftingRecipe recipeData;
    private Action<CraftingRecipe> onSelectedCallback;

    public void Initialize(CraftingRecipe data, Action<CraftingRecipe> onClick)
    {
        recipeData = data;
        onSelectedCallback = onClick;

        // (Nhớ đổi Texture Type sang Sprite (2D and UI) cho ảnh icon![cite: 14])
        if (iconImage != null && data.productIcon != null)
            iconImage.sprite = data.productIcon;
    }

    private void OnEnable()
    {
        slotButton.onClick.AddListener(OnSlotClicked);
    }

    private void OnDisable()
    {
        slotButton.onClick.RemoveListener(OnSlotClicked);
    }

    private void OnSlotClicked()
    {
        onSelectedCallback?.Invoke(recipeData);
    }
}