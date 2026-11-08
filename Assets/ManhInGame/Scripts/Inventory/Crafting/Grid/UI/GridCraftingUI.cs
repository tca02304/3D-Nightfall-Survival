using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class GridCraftingUI : MonoBehaviour
{
    [Header("Crafting Core")]
    [SerializeField] private GridCraftingController controller;

    [Header("Crafting Grid 3x3")]
    [SerializeField] private Transform ingredientsGrid;
    
    [Header("Crafting Result")]
    [SerializeField] private Image resultProductIcon;

    [SerializeField] private TMP_Text resultAmountText;

    private readonly Image[] ingredientIcons = new Image[9];

    private InventoryManager subscribedInventory;

    private void Start()
    {
        InitializeIngredientSlots();
        SubscribeInventory();
        Refresh();
    }

    private void OnEnable()
    {
        if (controller != null) controller.OnGridChanged += Refresh;
        SubscribeInventory();
        Refresh();
    }

    private void OnDisable()
    {
        if (controller != null) controller.OnGridChanged -= Refresh;
        if (subscribedInventory != null)
        {
            subscribedInventory.OnInventoryChanged -= Refresh;
            subscribedInventory = null;
        }
    }

    private void SubscribeInventory()
    {
        InventoryManager inventory = InventoryManager.Instance;

        if (subscribedInventory == inventory)
            return;

        if (subscribedInventory != null)
            subscribedInventory.OnInventoryChanged -= Refresh;

        subscribedInventory = inventory;

        if (subscribedInventory != null)
            subscribedInventory.OnInventoryChanged += Refresh;
    }

    private void InitializeIngredientSlots()
    {
        if (controller == null || ingredientsGrid == null)
        {
            Debug.LogError("[GridCraftingUI] Missing references.");
            return;
        }

        if(ingredientsGrid.childCount != 9)
        {
            Debug.LogWarning($"[GridCraftingUI] Expected 9 slots, " +
                $"found {ingredientsGrid.childCount}");
        }

        for (int i = 0; i < ingredientIcons.Length; i++)
        {
            ingredientIcons[i] = null;

            if (i >= ingredientsGrid.childCount)
                break;

            Transform child = ingredientsGrid.GetChild(i);

            GridCraftingIngredientSlot slot = child.GetComponent<GridCraftingIngredientSlot>();

            if (slot == null)
                continue;

            slot.Configure(controller, i);

            Transform iconTransform = child.Find("Icon");

            if (iconTransform == null)
                continue;

            Image icon = iconTransform.GetComponent<Image>();

            if(icon == null)
            {
                Debug.LogWarning($"[GridCraftingUI] Missing Image on {iconTransform.name}");
                continue;
            }

            ingredientIcons[i] = icon;
            icon.raycastTarget = false;

            Debug.Log($"[GridCraftingUI] {child.name} -> GridIdex={i}");
        }
    }

    public void Refresh()
    {
        if (controller == null)
            return;

        RefreshIngredientIcons();
        RefreshResultIcon();
    }

    private void RefreshIngredientIcons()
    {
        InventoryManager inventory = InventoryManager.Instance;
        ItemDatabase database = ItemDatabase.Instance;

        if (inventory == null || database == null)
            return;

        for (int i = 0; i < ingredientIcons.Length; i++)
        {
            Image iconImage = ingredientIcons[i];

            if (iconImage == null)
                continue;

            GridIngredient cell = controller.GetCell(i);

            if (cell.itemId <= 0)
            {
                ClearImage(iconImage);
                continue;
            }

            ItemData item = inventory.GetItemData(cell.itemId);

            if (item == null)
            {
                ClearImage(iconImage);
                continue;
            }

            Sprite sprite =
                database.GetIconByKey(item.iconKey);

            iconImage.sprite = sprite;
            iconImage.color = Color.white;
            iconImage.enabled = sprite != null;
        }
    }


    private void RefreshResultIcon()
    {
        if (resultProductIcon == null)
            return;

        InventoryManager inventory = InventoryManager.Instance;
        ItemDatabase database = ItemDatabase.Instance;

        GridRecipe recipe = controller.MatchedRecipe;

        if (recipe == null ||
            inventory == null ||
            database == null)
        {
            ClearResult();
            return;
        }

        ItemData output =
            inventory.GetItemData(recipe.outputId);

        if (output == null)
        {
            ClearResult();
            return;
        }

        Sprite sprite =
            database.GetIconByKey(output.iconKey);

        resultProductIcon.sprite = sprite;
        resultProductIcon.color = Color.white;
        resultProductIcon.enabled = sprite != null;

        if (resultAmountText != null)
        {
            resultAmountText.text =
                recipe.outputAmount > 1
                    ? recipe.outputAmount.ToString()
                    : "";
        }
    }

    private void ClearResult()
    {
        ClearImage(resultProductIcon);

        if (resultAmountText != null)
            resultAmountText.text = "";
    }

    private static void ClearImage(Image image)
    {
        if (image == null)
            return;

        image.sprite = null;
        image.enabled = false;
    }
}
