#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class UiIngredientsSetupTool
{
    [MenuItem("Tools/Setup Ingredients Container")]
    public static void SetupIngredientsContainer()
    {
        // 1. Tìm UiCrafting trong Scene
        GameObject uiCrafting = GameObject.Find("UiCrafting");
        if (uiCrafting == null)
        {
            UiCraftingManager existingManager = Object.FindFirstObjectByType<UiCraftingManager>(FindObjectsInactive.Include);
            if (existingManager != null)
            {
                uiCrafting = existingManager.gameObject;
            }
            else
            {
                Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
                foreach (var t in allTransforms)
                {
                    if (t.gameObject.name == "UiCrafting" && !EditorUtility.IsPersistent(t.gameObject) && t.gameObject.hideFlags == HideFlags.None)
                    {
                        uiCrafting = t.gameObject;
                        break;
                    }
                }
            }
        }

        if (uiCrafting == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy GameObject 'UiCrafting' trong Scene!", "OK");
            return;
        }

        // 2. Tìm IngredientsContainer bên trong UiCrafting (hoặc tạo mới nếu chưa có)
        Transform ingredientsTrans = FindChildRecursive(uiCrafting.transform, "IngredientsContainer");
        GameObject ingredientsObj;

        if (ingredientsTrans != null)
        {
            ingredientsObj = ingredientsTrans.gameObject;
        }
        else
        {
            // Nếu chưa có IngredientsContainer, thử kiểm tra RequirementsList xem có phải được đặt tên lại không
            Transform reqListTrans = FindChildRecursive(uiCrafting.transform, "RequirementsList");
            if (reqListTrans != null)
            {
                ingredientsObj = reqListTrans.gameObject;
                ingredientsObj.name = "IngredientsContainer";
            }
            else
            {
                // Tìm DetailPanel để làm cha, nếu không thì gắn trực tiếp vào UiCrafting
                Transform detailPanel = FindChildRecursive(uiCrafting.transform, "DetailPanel");
                Transform parent = detailPanel != null ? detailPanel : uiCrafting.transform;

                ingredientsObj = new GameObject("IngredientsContainer");
                ingredientsObj.transform.SetParent(parent, false);
                RectTransform rt = ingredientsObj.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.offsetMin = new Vector2(15f, 110f);
                rt.offsetMax = new Vector2(-15f, -65f);
            }
        }

        Undo.RegisterFullObjectHierarchyUndo(uiCrafting, "Setup Ingredients Container");

        // Yêu cầu 1: Gỡ bỏ Grid Layout Group nếu có
        GridLayoutGroup gridLayout = ingredientsObj.GetComponent<GridLayoutGroup>();
        if (gridLayout != null)
        {
            Object.DestroyImmediate(gridLayout);
        }

        // Nếu có VerticalLayoutGroup cũ (từ RequirementsList trước đây) cũng gỡ bỏ
        VerticalLayoutGroup vertLayout = ingredientsObj.GetComponent<VerticalLayoutGroup>();
        if (vertLayout != null)
        {
            Object.DestroyImmediate(vertLayout);
        }

        // Yêu cầu 2: Thiết lập Layout ngang (Horizontal Layout Group, Spacing 20, Middle Center)
        HorizontalLayoutGroup horizLayout = ingredientsObj.GetComponent<HorizontalLayoutGroup>();
        if (horizLayout == null)
        {
            horizLayout = ingredientsObj.AddComponent<HorizontalLayoutGroup>();
        }
        horizLayout.spacing = 20f;
        horizLayout.childAlignment = TextAnchor.MiddleCenter;
        horizLayout.childControlWidth = false;
        horizLayout.childControlHeight = false;
        horizLayout.childForceExpandWidth = false;
        horizLayout.childForceExpandHeight = false;

        // Yêu cầu 3: Tạo 2 ô nguyên liệu cố định IngredientSlot_1 và IngredientSlot_2
        // Xây dựng IngredientSlot_1
        var (slot1Img, slot1Amount) = SetupIngredientSlot(ingredientsObj.transform, "IngredientSlot_1");

        // Xây dựng IngredientSlot_2
        var (slot2Img, slot2Amount) = SetupIngredientSlot(ingredientsObj.transform, "IngredientSlot_2");

        // Yêu cầu 4: Cập nhật tham chiếu (References) trên UiCraftingManager
        UiCraftingManager manager = uiCrafting.GetComponent<UiCraftingManager>();
        if (manager == null)
        {
            manager = uiCrafting.AddComponent<UiCraftingManager>();
        }

        manager.ingredientImage_1 = slot1Img;
        manager.ingredientAmountText_1 = slot1Amount;
        manager.ingredientImage_2 = slot2Img;
        manager.ingredientAmountText_2 = slot2Amount;

        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(uiCrafting);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(uiCrafting.scene);

        EditorUtility.DisplayDialog("Thành công",
            "Đã cập nhật IngredientsContainer thành công:\n" +
            "- Gỡ GridLayoutGroup\n" +
            "- Gắn HorizontalLayoutGroup (Spacing 20, Child Alignment: Middle Center)\n" +
            "- Tạo IngredientSlot_1 & IngredientSlot_2 (100x100) kèm AmountText (neo Bottom Right)\n" +
            "- Đã gán đầy đủ tham chiếu vào UiCraftingManager!",
            "OK");
    }

    private static (Image, TextMeshProUGUI) SetupIngredientSlot(Transform parent, string slotName)
    {
        Transform slotTrans = parent.Find(slotName);
        GameObject slotObj = slotTrans != null ? slotTrans.gameObject : new GameObject(slotName);
        slotObj.transform.SetParent(parent, false);

        RectTransform slotRect = slotObj.GetComponent<RectTransform>();
        if (slotRect == null) slotRect = slotObj.AddComponent<RectTransform>();
        slotRect.sizeDelta = new Vector2(100f, 100f);

        Image slotImg = slotObj.GetComponent<Image>();
        if (slotImg == null) slotImg = slotObj.AddComponent<Image>();
        slotImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        // Tạo AmountText bên trong slot
        Transform amountTrans = slotObj.transform.Find("AmountText");
        GameObject amountObj = amountTrans != null ? amountTrans.gameObject : new GameObject("AmountText");
        amountObj.transform.SetParent(slotObj.transform, false);

        RectTransform amountRect = amountObj.GetComponent<RectTransform>();
        if (amountRect == null) amountRect = amountObj.AddComponent<RectTransform>();
        // Neo góc Bottom Right: anchorMin (1, 0), anchorMax (1, 0), pivot (1, 0)
        amountRect.anchorMin = new Vector2(1f, 0f);
        amountRect.anchorMax = new Vector2(1f, 0f);
        amountRect.pivot = new Vector2(1f, 0f);
        amountRect.anchoredPosition = new Vector2(-5f, 5f);
        amountRect.sizeDelta = new Vector2(60f, 30f);

        TextMeshProUGUI amountTMP = amountObj.GetComponent<TextMeshProUGUI>();
        if (amountTMP == null) amountTMP = amountObj.AddComponent<TextMeshProUGUI>();
        amountTMP.text = "0/0";
        amountTMP.fontSize = 20;
        amountTMP.fontStyle = FontStyles.Bold;
        amountTMP.alignment = TextAlignmentOptions.BottomRight;
        amountTMP.color = Color.white;

        return (slotImg, amountTMP);
    }

    private static Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindChildRecursive(parent.GetChild(i), name);
            if (result != null) return result;
        }
        return null;
    }
}
#endif
