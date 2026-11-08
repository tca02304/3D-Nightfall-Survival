#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class UiWorkbenchBuilder
{
    [MenuItem("Tools/Build Crafting Workbench UI")]
    public static void BuildWorkbench()
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

        Undo.RegisterFullObjectHierarchyUndo(uiCrafting, "Build Workbench UI");

        // Tìm vị trí thích hợp để đặt CraftingWorkbenchPanel:
        // Nếu có DetailPanel, đặt bên trong DetailPanel (thay thế vùng trống) hoặc đặt trực tiếp vào UiCrafting
        Transform detailPanel = uiCrafting.transform.Find("DetailPanel");
        Transform parentTransform = detailPanel != null ? detailPanel : uiCrafting.transform;

        // 2. Tạo CraftingWorkbenchPanel (Neo Center)
        Transform wbTrans = parentTransform.Find("CraftingWorkbenchPanel");
        if (wbTrans == null && parentTransform != uiCrafting.transform)
        {
            wbTrans = uiCrafting.transform.Find("CraftingWorkbenchPanel");
        }

        GameObject wbObj = wbTrans != null ? wbTrans.gameObject : new GameObject("CraftingWorkbenchPanel");
        wbObj.transform.SetParent(parentTransform, false);

        RectTransform wbRect = EnsureRectTransform(wbObj);
        // Neo Center
        wbRect.anchorMin = new Vector2(0.5f, 0.5f);
        wbRect.anchorMax = new Vector2(0.5f, 0.5f);
        wbRect.pivot = new Vector2(0.5f, 0.5f);
        wbRect.anchoredPosition = Vector2.zero;
        wbRect.sizeDelta = new Vector2(480f, 280f);

        Image wbImage = EnsureComponent<Image>(wbObj);
        wbImage.color = new Color(0.12f, 0.12f, 0.12f, 0.7f);

        // Horizontal Layout Group: Spacing 30, Middle Center, Control Child Size (Width & Height)
        HorizontalLayoutGroup wbLayout = EnsureComponent<HorizontalLayoutGroup>(wbObj);
        wbLayout.spacing = 30f;
        wbLayout.childAlignment = TextAnchor.MiddleCenter;
        wbLayout.childControlWidth = true;
        wbLayout.childControlHeight = true;
        wbLayout.childForceExpandWidth = false;
        wbLayout.childForceExpandHeight = false;
        wbLayout.padding = new RectOffset(15, 15, 15, 15);

        // ContentSizeFitter để panel tự co giãn vừa vặn
        ContentSizeFitter wbFitter = EnsureComponent<ContentSizeFitter>(wbObj);
        wbFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        wbFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 3. Tạo IngredientsGrid (Bên trái): 3x3 Grid Layout Group
        Transform gridTrans = wbObj.transform.Find("IngredientsGrid");
        GameObject gridObj = gridTrans != null ? gridTrans.gameObject : new GameObject("IngredientsGrid");
        gridObj.transform.SetParent(wbObj.transform, false);

        RectTransform gridRect = EnsureRectTransform(gridObj);
        // Kích thước lưới 3x3: (80*3 + 5*2) = 250
        gridRect.sizeDelta = new Vector2(250f, 250f);
        LayoutElement gridLayoutElement = EnsureComponent<LayoutElement>(gridObj);
        gridLayoutElement.preferredWidth = 250f;
        gridLayoutElement.preferredHeight = 250f;

        GridLayoutGroup gridLayout = EnsureComponent<GridLayoutGroup>(gridObj);
        gridLayout.cellSize = new Vector2(80f, 80f);
        gridLayout.spacing = new Vector2(5f, 5f);
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.childAlignment = TextAnchor.MiddleCenter;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 3;

        // Tạo 9 ô Slot_1 -> Slot_9
        Image[] slots = new Image[9];
        for (int i = 1; i <= 9; i++)
        {
            string slotName = $"Slot_{i}";
            Transform slotTrans = gridObj.transform.Find(slotName);
            GameObject slotObj = slotTrans != null ? slotTrans.gameObject : new GameObject(slotName);
            slotObj.transform.SetParent(gridObj.transform, false);

            EnsureRectTransform(slotObj);
            Image slotImg = EnsureComponent<Image>(slotObj);
            slotImg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            slots[i - 1] = slotImg;

            // Tạo icon con rỗng bên trong mỗi slot
            Transform iconTrans = slotObj.transform.Find("Icon");
            GameObject iconObj = iconTrans != null ? iconTrans.gameObject : new GameObject("Icon");
            iconObj.transform.SetParent(slotObj.transform, false);
            RectTransform iconRect = EnsureRectTransform(iconObj);
            iconRect.anchorMin = new Vector2(0.1f, 0.1f);
            iconRect.anchorMax = new Vector2(0.9f, 0.9f);
            iconRect.sizeDelta = Vector2.zero;
            Image iconImg = EnsureComponent<Image>(iconObj);
            iconImg.preserveAspect = true;
            iconObj.SetActive(false); // Ẩn mặc định
        }

        // 4. Tạo ArrowIcon (Ở giữa)
        Transform arrowTrans = wbObj.transform.Find("ArrowIcon");
        GameObject arrowObj = arrowTrans != null ? arrowTrans.gameObject : new GameObject("ArrowIcon");
        arrowObj.transform.SetParent(wbObj.transform, false);

        EnsureRectTransform(arrowObj);
        LayoutElement arrowLayout = EnsureComponent<LayoutElement>(arrowObj);
        arrowLayout.preferredWidth = 50f;
        arrowLayout.preferredHeight = 50f;

        Image arrowImg = EnsureComponent<Image>(arrowObj);
        arrowImg.preserveAspect = true;
        arrowImg.color = Color.white;

        // Tìm sprite mũi tên nếu có trong project
        Sprite arrowSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Asset3D/Texture2D/TutorialArrow.png");
        if (arrowSprite != null)
        {
            arrowImg.sprite = arrowSprite;
        }

        // 5. Tạo ResultSlot (Bên phải): kích thước 110x110
        Transform resultTrans = wbObj.transform.Find("ResultSlot");
        GameObject resultObj = resultTrans != null ? resultTrans.gameObject : new GameObject("ResultSlot");
        resultObj.transform.SetParent(wbObj.transform, false);

        EnsureRectTransform(resultObj);
        LayoutElement resultLayout = EnsureComponent<LayoutElement>(resultObj);
        resultLayout.preferredWidth = 110f;
        resultLayout.preferredHeight = 110f;

        Image resultImg = EnsureComponent<Image>(resultObj);
        resultImg.color = new Color(0.25f, 0.25f, 0.25f, 0.95f);

        // Icon sản phẩm con bên trong ResultSlot
        Transform prodIconTrans = resultObj.transform.Find("ProductIcon");
        GameObject prodIconObj = prodIconTrans != null ? prodIconTrans.gameObject : new GameObject("ProductIcon");
        prodIconObj.transform.SetParent(resultObj.transform, false);
        RectTransform prodIconRect = EnsureRectTransform(prodIconObj);
        prodIconRect.anchorMin = new Vector2(0.1f, 0.1f);
        prodIconRect.anchorMax = new Vector2(0.9f, 0.9f);
        prodIconRect.sizeDelta = Vector2.zero;
        Image prodIconImg = EnsureComponent<Image>(prodIconObj);
        prodIconImg.preserveAspect = true;

        // AmountText bên trong ResultSlot
        Transform amountTrans = resultObj.transform.Find("AmountText");
        GameObject amountObj = amountTrans != null ? amountTrans.gameObject : new GameObject("AmountText");
        amountObj.transform.SetParent(resultObj.transform, false);
        RectTransform amountRect = EnsureRectTransform(amountObj);
        amountRect.anchorMin = new Vector2(1f, 0f);
        amountRect.anchorMax = new Vector2(1f, 0f);
        amountRect.pivot = new Vector2(1f, 0f);
        amountRect.anchoredPosition = new Vector2(-5f, 5f);
        amountRect.sizeDelta = new Vector2(50f, 30f);

        TextMeshProUGUI amountTMP = EnsureComponent<TextMeshProUGUI>(amountObj);
        amountTMP.fontSize = 20;
        amountTMP.fontStyle = FontStyles.Bold;
        amountTMP.alignment = TextAlignmentOptions.BottomRight;
        amountTMP.text = "";

        // 6. Cập nhật tham chiếu cho UiCraftingManager
        UiCraftingManager manager = uiCrafting.GetComponent<UiCraftingManager>();
        if (manager == null) manager = uiCrafting.AddComponent<UiCraftingManager>();

        manager.ingredientsGrid = gridObj.transform;
        manager.ingredientSlots = slots;
        manager.resultSlot = resultImg;
        manager.resultProductIcon = prodIconImg;
        manager.resultAmountText = amountTMP;

        // Đảm bảo thứ tự con trong CraftingWorkbenchPanel đúng: IngredientsGrid -> ArrowIcon -> ResultSlot
        gridObj.transform.SetSiblingIndex(0);
        arrowObj.transform.SetSiblingIndex(1);
        resultObj.transform.SetSiblingIndex(2);

        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(uiCrafting);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(uiCrafting.scene);

        EditorUtility.DisplayDialog("Thành công",
            "Đã tạo hoàn chỉnh cấu trúc Bàn Chế Tạo (CraftingWorkbenchPanel):\n" +
            "- IngredientsGrid (Lưới 3x3 gồm Slot_1 -> Slot_9, Cell Size 80x80, Spacing 5x5)\n" +
            "- ArrowIcon (Mũi tên chỉ hướng ở giữa)\n" +
            "- ResultSlot (Ô kết quả kèm ProductIcon & AmountText)\n" +
            "- Đã gán đầy đủ tham chiếu vào UiCraftingManager!",
            "OK");
    }

    private static RectTransform EnsureRectTransform(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        return rt;
    }

    private static T EnsureComponent<T>(GameObject go) where T : Component
    {
        T comp = go.GetComponent<T>();
        if (comp == null) comp = go.AddComponent<T>();
        return comp;
    }
}
#endif
