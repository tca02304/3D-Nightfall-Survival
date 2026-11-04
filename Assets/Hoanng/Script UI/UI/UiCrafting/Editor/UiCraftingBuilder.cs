#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class UiCraftingBuilder
{
    private const string PREFAB_DIR = "Assets/Hoanng/PrefabUI";

    [MenuItem("Tools/Build UI Crafting Setup")]
    public static void BuildSetup()
    {
        // 1. Kiểm tra hoặc tạo thư mục Prefab
        if (!AssetDatabase.IsValidFolder("Assets/Hoanng/PrefabUI"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Hoanng"))
                AssetDatabase.CreateFolder("Assets", "Hoanng");
            AssetDatabase.CreateFolder("Assets/Hoanng", "PrefabUI");
        }

        // 2. Tạo Prefabs nếu chưa có hoặc cập nhật
        GameObject recipePrefab = CreateOrUpdateRecipeSlotPrefab();
        GameObject reqTextPrefab = CreateOrUpdateRequirementTextPrefab();

        // 3. Tìm UiCrafting trong Scene đang mở
        GameObject uiCrafting = GameObject.Find("UiCrafting");
        if (uiCrafting == null)
        {
            // Thử tìm trong các đối tượng kể cả inactive
            UiCraftingManager existingManager = Object.FindFirstObjectByType<UiCraftingManager>(FindObjectsInactive.Include);
            if (existingManager != null)
            {
                uiCrafting = existingManager.gameObject;
            }
            else
            {
                // Tìm theo tên đối tượng không active
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

        Undo.RegisterFullObjectHierarchyUndo(uiCrafting, "Build UI Crafting");

        // 4. Xây dựng cấu trúc bên trong UiCrafting (ngang hàng nhau)
        // a. TabGroup
        Transform tabGroupTrans = uiCrafting.transform.Find("TabGroup");
        GameObject tabGroupObj = tabGroupTrans != null ? tabGroupTrans.gameObject : new GameObject("TabGroup");
        tabGroupObj.transform.SetParent(uiCrafting.transform, false);
        EnsureRectTransform(tabGroupObj);
        RectTransform tabRect = tabGroupObj.GetComponent<RectTransform>();
        // Neo Top Stretch: min (0, 1), max (1, 1), pivot (0.5, 1)
        tabRect.anchorMin = new Vector2(0f, 1f);
        tabRect.anchorMax = new Vector2(1f, 1f);
        tabRect.pivot = new Vector2(0.5f, 1f);
        tabRect.anchoredPosition = Vector2.zero;
        tabRect.sizeDelta = new Vector2(0f, 60f);

        Image tabImage = EnsureComponent<Image>(tabGroupObj);
        tabImage.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);

        HorizontalLayoutGroup tabLayout = EnsureComponent<HorizontalLayoutGroup>(tabGroupObj);
        tabLayout.childControlWidth = true;
        tabLayout.childControlHeight = true;
        tabLayout.childForceExpandWidth = true;
        tabLayout.childForceExpandHeight = true;
        tabLayout.spacing = 5f;
        tabLayout.padding = new RectOffset(5, 5, 5, 5);

        string[] tabNames = new string[] { "Button_Basic", "Button_Tools", "Button_Stations", "Button_Build" };
        string[] tabLabels = new string[] { "Basic", "Tools", "Stations", "Build" };
        for (int i = 0; i < tabNames.Length; i++)
        {
            CreateOrUpdateTabButton(tabGroupObj.transform, tabNames[i], tabLabels[i]);
        }

        // b. RecipeGridContainer
        Transform gridTrans = uiCrafting.transform.Find("RecipeGridContainer");
        GameObject gridObj = gridTrans != null ? gridTrans.gameObject : new GameObject("RecipeGridContainer");
        gridObj.transform.SetParent(uiCrafting.transform, false);
        EnsureRectTransform(gridObj);
        RectTransform gridRect = gridObj.GetComponent<RectTransform>();
        // Neo Stretch Stretch, Top 60:
        // Chiếm nửa trái hoặc vùng chính (ví dụ từ left 0 đến right 320 hoặc stretch với offset top 60)
        // Ở đây để stretch stretch: anchorMin(0, 0), anchorMax(0.7f, 1f) hoặc (0, 0) đến (1, 1) offset detail
        // Theo yêu cầu: "neo Stretch Stretch, Top 60 để không đè lên Tab"
        // Và DetailPanel "neo bên trái hoặc phải tùy ý" -> Đặt DetailPanel bên phải (Width 300), nên Grid từ AnchorMax.x = 1 với offsetRight = 320 hoặc Anchor (0,0) -> (0.65f, 1)
        // Thiết lập chuẩn: anchorMin (0, 0), anchorMax (0.68f, 1f), offsetTop 60:
        gridRect.anchorMin = new Vector2(0f, 0f);
        gridRect.anchorMax = new Vector2(0.68f, 1f);
        gridRect.pivot = new Vector2(0.5f, 0.5f);
        gridRect.offsetMin = new Vector2(10f, 10f); // Left, Bottom
        gridRect.offsetMax = new Vector2(-10f, -65f); // Right, -Top

        Image gridImage = EnsureComponent<Image>(gridObj);
        gridImage.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);

        GridLayoutGroup gridLayout = EnsureComponent<GridLayoutGroup>(gridObj);
        gridLayout.cellSize = new Vector2(80f, 80f);
        gridLayout.spacing = new Vector2(10f, 10f);
        gridLayout.childAlignment = TextAnchor.UpperCenter;
        gridLayout.padding = new RectOffset(10, 10, 10, 10);

        // c. DetailPanel
        Transform detailTrans = uiCrafting.transform.Find("DetailPanel");
        GameObject detailObj = detailTrans != null ? detailTrans.gameObject : new GameObject("DetailPanel");
        detailObj.transform.SetParent(uiCrafting.transform, false);
        EnsureRectTransform(detailObj);
        RectTransform detailRect = detailObj.GetComponent<RectTransform>();
        // Neo bên phải: anchorMin (0.68f, 0), anchorMax (1f, 1f), Top 60
        detailRect.anchorMin = new Vector2(0.68f, 0f);
        detailRect.anchorMax = new Vector2(1f, 1f);
        detailRect.pivot = new Vector2(1f, 0.5f);
        detailRect.offsetMin = new Vector2(10f, 10f);
        detailRect.offsetMax = new Vector2(-10f, -65f);

        Image detailImage = EnsureComponent<Image>(detailObj);
        detailImage.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);

        // Các thành phần trong DetailPanel:
        // - ProductNameText
        Transform prodTextTrans = detailObj.transform.Find("ProductNameText");
        GameObject prodTextObj = prodTextTrans != null ? prodTextTrans.gameObject : new GameObject("ProductNameText");
        prodTextObj.transform.SetParent(detailObj.transform, false);
        EnsureRectTransform(prodTextObj);
        RectTransform prodRect = prodTextObj.GetComponent<RectTransform>();
        prodRect.anchorMin = new Vector2(0f, 1f);
        prodRect.anchorMax = new Vector2(1f, 1f);
        prodRect.pivot = new Vector2(0.5f, 1f);
        prodRect.anchoredPosition = new Vector2(0f, -15f);
        prodRect.sizeDelta = new Vector2(-20f, 40f);
        TextMeshProUGUI prodTMP = EnsureComponent<TextMeshProUGUI>(prodTextObj);
        prodTMP.text = "Product Name";
        prodTMP.fontSize = 22;
        prodTMP.fontStyle = FontStyles.Bold;
        prodTMP.alignment = TextAlignmentOptions.Center;

        // - RequirementsList (Empty GameObject với Vertical Layout Group)
        Transform reqListTrans = detailObj.transform.Find("RequirementsList");
        GameObject reqListObj = reqListTrans != null ? reqListTrans.gameObject : new GameObject("RequirementsList");
        reqListObj.transform.SetParent(detailObj.transform, false);
        EnsureRectTransform(reqListObj);
        RectTransform reqListRect = reqListObj.GetComponent<RectTransform>();
        reqListRect.anchorMin = new Vector2(0f, 0f);
        reqListRect.anchorMax = new Vector2(1f, 1f);
        reqListRect.pivot = new Vector2(0.5f, 0.5f);
        reqListRect.offsetMin = new Vector2(15f, 110f); // Cách dưới 110px cho nút craft và message
        reqListRect.offsetMax = new Vector2(-15f, -65f); // Cách trên 65px dưới ProductNameText

        VerticalLayoutGroup reqLayout = EnsureComponent<VerticalLayoutGroup>(reqListObj);
        reqLayout.childControlWidth = true;
        reqLayout.childControlHeight = false;
        reqLayout.childForceExpandWidth = true;
        reqLayout.childForceExpandHeight = false;
        reqLayout.spacing = 6f;

        // - CraftButton
        Transform craftBtnTrans = detailObj.transform.Find("CraftButton");
        GameObject craftBtnObj = craftBtnTrans != null ? craftBtnTrans.gameObject : new GameObject("CraftButton");
        craftBtnObj.transform.SetParent(detailObj.transform, false);
        EnsureRectTransform(craftBtnObj);
        RectTransform craftBtnRect = craftBtnObj.GetComponent<RectTransform>();
        craftBtnRect.anchorMin = new Vector2(0.5f, 0f);
        craftBtnRect.anchorMax = new Vector2(0.5f, 0f);
        craftBtnRect.pivot = new Vector2(0.5f, 0f);
        craftBtnRect.anchoredPosition = new Vector2(0f, 50f);
        craftBtnRect.sizeDelta = new Vector2(160f, 45f);

        Image craftBtnImg = EnsureComponent<Image>(craftBtnObj);
        craftBtnImg.color = new Color(0.2f, 0.6f, 0.2f, 1f);
        Button craftBtn = EnsureComponent<Button>(craftBtnObj);

        Transform craftBtnTextTrans = craftBtnObj.transform.Find("Text (TMP)");
        GameObject craftBtnTextObj = craftBtnTextTrans != null ? craftBtnTextTrans.gameObject : new GameObject("Text (TMP)");
        craftBtnTextObj.transform.SetParent(craftBtnObj.transform, false);
        EnsureRectTransform(craftBtnTextObj);
        RectTransform craftBtnTextRect = craftBtnTextObj.GetComponent<RectTransform>();
        craftBtnTextRect.anchorMin = Vector2.zero;
        craftBtnTextRect.anchorMax = Vector2.one;
        craftBtnTextRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI craftBtnTMP = EnsureComponent<TextMeshProUGUI>(craftBtnTextObj);
        craftBtnTMP.text = "Craft";
        craftBtnTMP.fontSize = 20;
        craftBtnTMP.alignment = TextAlignmentOptions.Center;

        // - MessageText
        Transform msgTextTrans = detailObj.transform.Find("MessageText");
        GameObject msgTextObj = msgTextTrans != null ? msgTextTrans.gameObject : new GameObject("MessageText");
        msgTextObj.transform.SetParent(detailObj.transform, false);
        EnsureRectTransform(msgTextObj);
        RectTransform msgTextRect = msgTextObj.GetComponent<RectTransform>();
        msgTextRect.anchorMin = new Vector2(0f, 0f);
        msgTextRect.anchorMax = new Vector2(1f, 0f);
        msgTextRect.pivot = new Vector2(0.5f, 0f);
        msgTextRect.anchoredPosition = new Vector2(0f, 10f);
        msgTextRect.sizeDelta = new Vector2(-20f, 35f);
        TextMeshProUGUI msgTMP = EnsureComponent<TextMeshProUGUI>(msgTextObj);
        msgTMP.text = "";
        msgTMP.fontSize = 16;
        msgTMP.alignment = TextAlignmentOptions.Center;

        // 5. Gắn Script UiCraftingManager & Gán tham chiếu
        UiCraftingManager manager = EnsureComponent<UiCraftingManager>(uiCrafting);
        manager.recipePrefab = recipePrefab;
        manager.recipeGrid = gridObj.transform;
        manager.productNameText = prodTMP;
        manager.requirementsContainer = reqListObj.transform;
        manager.requirementTextPrefab = reqTextPrefab;
        manager.craftButton = craftBtn;
        manager.craftMessageText = msgTMP;

        EditorUtility.SetDirty(uiCrafting);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(uiCrafting.scene);

        EditorUtility.DisplayDialog("Thành công", "Đã khởi tạo xong cấu trúc UI Crafting, tạo Prefabs và gán toàn bộ tham chiếu!", "OK");
    }

    private static GameObject CreateOrUpdateRecipeSlotPrefab()
    {
        string path = PREFAB_DIR + "/RecipeSlot.prefab";
        GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existingPrefab != null) return existingPrefab;

        GameObject slot = new GameObject("RecipeSlot");
        RectTransform rect = slot.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(80f, 80f);

        Image bgImage = slot.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        Button btn = slot.AddComponent<Button>();

        // Icon Image con
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(slot.transform, false);
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.1f, 0.1f);
        iconRect.anchorMax = new Vector2(0.9f, 0.9f);
        iconRect.sizeDelta = Vector2.zero;
        Image iconImg = iconObj.AddComponent<Image>();
        iconImg.preserveAspect = true;

        // UiRecipeSlot component
        UiRecipeSlot slotScript = slot.AddComponent<UiRecipeSlot>();
        slotScript.slotButton = btn;
        slotScript.iconImage = iconImg;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(slot, path);
        Object.DestroyImmediate(slot);
        return prefab;
    }

    private static GameObject CreateOrUpdateRequirementTextPrefab()
    {
        string path = PREFAB_DIR + "/RequirementText.prefab";
        GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existingPrefab != null) return existingPrefab;

        GameObject textObj = new GameObject("RequirementText");
        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200f, 30f);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 16;
        tmp.text = "Material x1 (Have: 0)";
        tmp.alignment = TextAlignmentOptions.MidlineLeft;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(textObj, path);
        Object.DestroyImmediate(textObj);
        return prefab;
    }

    private static void CreateOrUpdateTabButton(Transform parent, string name, string label)
    {
        Transform btnTrans = parent.Find(name);
        GameObject btnObj = btnTrans != null ? btnTrans.gameObject : new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        EnsureRectTransform(btnObj);

        Image img = EnsureComponent<Image>(btnObj);
        img.color = new Color(0.25f, 0.25f, 0.25f, 1f);
        EnsureComponent<Button>(btnObj);

        Transform textTrans = btnObj.transform.Find("Text (TMP)");
        GameObject textObj = textTrans != null ? textTrans.gameObject : new GameObject("Text (TMP)");
        textObj.transform.SetParent(btnObj.transform, false);
        EnsureRectTransform(textObj);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = EnsureComponent<TextMeshProUGUI>(textObj);
        tmp.text = label;
        tmp.fontSize = 18;
        tmp.alignment = TextAlignmentOptions.Center;
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
