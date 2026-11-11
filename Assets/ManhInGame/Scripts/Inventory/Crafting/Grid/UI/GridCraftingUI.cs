using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class GridCraftingUI : MonoBehaviour
{
    [SerializeField] private GridCraftingController controller;
    [SerializeField] private Transform ingredientsGrid;
    [SerializeField] private Image resultProductIcon;
    [SerializeField] private TMP_Text resultAmountText;

    private readonly Image[] ingredientIcons = new Image[9];
    private readonly TMP_Text[] ingredientAmounts = new TMP_Text[9];
    private InventoryManager subscribedInventory;
    private bool initialized;
    private bool subscribedGrid;

    private void Start()
    {
        Initialize();
        Subscribe();
        Refresh();
    }

    private void OnEnable()
    {
        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        if (subscribedGrid && controller != null)
            controller.OnGridChanged -= Refresh;
        subscribedGrid = false;

        if (subscribedInventory != null)
            subscribedInventory.OnInventoryChanged -= Refresh;
        subscribedInventory = null;
    }

    private void Subscribe()
    {
        if (controller != null && !subscribedGrid)
        {
            controller.OnGridChanged += Refresh;
            subscribedGrid = true;
        }   

        var inventory = InventoryManager.Instance;
        if (inventory == subscribedInventory) return;

        if (subscribedInventory != null)
            subscribedInventory.OnInventoryChanged -= Refresh;
        subscribedInventory = inventory;
        if (subscribedInventory != null)
            subscribedInventory.OnInventoryChanged += Refresh;
    }

    private void Initialize()
    {
        initialized = false;
        if (controller == null || ingredientsGrid == null || resultProductIcon == null)
        {
            Debug.LogError("[GridCraftingUI] Assign Controller, IngredientsGrid and ResultProductIcon.", this);
            return;
        }
        if (ingredientsGrid.childCount != 9)
            Debug.LogWarning($"[GridCraftingUI] Expected 9 cells, found {ingredientsGrid.childCount}", this);
        for (int i = 0; i < 9; i++)
        {
            ingredientIcons[i] = null;
            if (i >= ingredientsGrid.childCount) continue;

            Transform slotTransform = ingredientsGrid.GetChild(i);
            var receiver = slotTransform.GetComponent<GridCraftingIngredientSlot>();
            if (receiver == null)
            {
                Debug.LogError($"[GridCraftingUI] {slotTransform.name} needs GridCraftingIngredientSlot on its root.", slotTransform);
                continue;
            }
            receiver.Configure(controller, i);

            // 'Icon' and 'IconImage' are the two naming conventions used by the UI team.
            // Do not accidentally draw the item onto the root slot's background Image.
            Transform iconTransform = FindChildRecursive(slotTransform, "IconImage") ??
                                      FindChildRecursive(slotTransform, "Icon");
            Image iconImage = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
            if (iconImage == null)
            {
                Debug.LogError($"[GridCraftingUI] No IconImage/Icon child with an Image in {slotTransform.name}.", slotTransform);
                continue;
            }

            ingredientIcons[i] = iconImage;
            Transform amountTransform = FindChildRecursive(slotTransform, "AmountText");
            if (amountTransform != null)
            {
                ingredientAmounts[i] = amountTransform.GetComponent<TMP_Text>();

                if (ingredientAmounts[i] != null)
                    ingredientAmounts[i].raycastTarget = false;
            }
            else
            {
                Debug.LogWarning($"[GridCraftingUI] {slotTransform.name} missing AmountText");
            }
            var drag = iconImage.GetComponent<GridCraftingDragSource>();
            if (drag == null) drag = iconImage.gameObject.AddComponent<GridCraftingDragSource>();
            drag.Configure(controller, i, false);

            // All decoration/text under the icon must let the Image receive pointer events.
            DisableChildRaycasts(iconImage.transform);
            Debug.Log($"[GridCraftingUI] Craft cell {i}: {slotTransform.name} -> {iconImage.name}", slotTransform);
        }

        var resultDrag = resultProductIcon.GetComponent<GridCraftingDragSource>();
        if (resultDrag == null)
            resultDrag = resultProductIcon.gameObject.AddComponent<GridCraftingDragSource>();
        resultDrag.Configure(controller, -1, true);
        GridCraftingResultClick resultClick =
    resultProductIcon.GetComponent<GridCraftingResultClick>();

        if (resultClick == null)
        {
            resultClick =
                resultProductIcon.gameObject
                    .AddComponent<GridCraftingResultClick>();
        }

        resultClick.Configure(controller);
        resultProductIcon.raycastTarget = true;
        DisableChildRaycasts(resultProductIcon.transform);

        initialized = true;
    }

    private static Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform nested = FindChildRecursive(child, name);
            if (nested != null) return nested;
        }
        return null;
    }

    private static void DisableChildRaycasts(Transform root)
    {
        foreach (Transform child in root)
        {
            foreach (Graphic graphic in child.GetComponents<Graphic>())
                graphic.raycastTarget = false;
            DisableChildRaycasts(child);
        }
    }


    public void Refresh()
    {
        if (!initialized || controller == null)
        {
            Debug.LogWarning(
                $"[GridCraftingUI] Refresh skipped. " +
                $"Initialized={initialized}, " +
                $"Controller={(controller != null)}"
            );
            return;
        }

        for (int i = 0; i < ingredientIcons.Length; i++)
        {
            GridIngredient cell = controller.GetCell(i);

            SetIcon(ingredientIcons[i], cell.itemId, $"Cell {i}");

            TMP_Text amountLabel = ingredientAmounts[i];

            if (amountLabel != null)
            {
                amountLabel.gameObject.SetActive(true);
                amountLabel.enabled = true;

                amountLabel.text = cell.itemId > 0 && cell.amount > 0
                    ? cell.amount.ToString()
                    : "";

                if (cell.itemId > 0)
                {
                    Debug.Log(
                        $"[GridCraftingUI] Cell={i}, " +
                        $"ItemId={cell.itemId}, " +
                        $"Amount={cell.amount}, " +
                        $"Text={amountLabel.text}, " +
                        $"Active={amountLabel.gameObject.activeInHierarchy}"
                    );
                }
            }
            else if (cell.itemId > 0)
            {
                Debug.LogWarning(
                    $"[GridCraftingUI] Cell {i}: AmountText reference is NULL"
                );
            }
        }


        GridRecipe recipe = controller.MatchedRecipe;

        long maxOutput = controller.GetMaxResultAmount();

        SetIcon(
            resultProductIcon,
            recipe != null && maxOutput > 0
                ? recipe.outputId
                : 0,
            "Result"
        );

        if (resultAmountText != null)
        {
            resultAmountText.text =
                maxOutput > 0
                    ? maxOutput.ToString()
                    : string.Empty;
        }

    }


    public void ClearCell(int index) => controller?.TryReturnCellAnywhere(index);


    private static void SetIcon(Image image, int itemId, string context)
    {
        if (image == null) return;
        var inventory = InventoryManager.Instance;
        var database = ItemDatabase.Instance;
        ItemData item = itemId > 0 && inventory != null ? inventory.GetItemData(itemId) : null;
        Sprite sprite = item != null && database != null ? database.GetIconByKey(item.iconKey) : null;

        if (!image.gameObject.activeSelf) image.gameObject.SetActive(true);
        image.sprite = sprite;
        image.color = Color.white;
        image.enabled = sprite != null;
        image.raycastTarget = sprite != null;

        if (itemId > 0 && sprite == null)
            Debug.LogWarning($"[GridCraftingUI] {context}: no Sprite for ItemId={itemId}, IconKey={item?.iconKey ?? "(missing ItemData)"}", image);
    }
}
