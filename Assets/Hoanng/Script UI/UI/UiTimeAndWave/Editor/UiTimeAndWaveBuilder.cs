#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class UiTimeAndWaveBuilder
{
    [MenuItem("Tools/Build UI Time And Wave Setup")]
    public static void BuildSetup()
    {
        // 1. Tìm Canvas hoặc UIManager / HUD làm cha
        Transform parentTransform = null;

        // Thử tìm theo thứ tự ưu tiên: HUD -> UIManager -> Canvas
        GameObject targetParentObj = GameObject.Find("HUD");
        if (targetParentObj == null) targetParentObj = GameObject.Find("UIManager");
        if (targetParentObj == null)
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas != null)
            {
                targetParentObj = canvas.gameObject;
            }
        }

        if (targetParentObj != null)
        {
            parentTransform = targetParentObj.transform;
        }

        // 2. Tìm hoặc tạo UiTimeAndWave
        GameObject uiTimeAndWave = GameObject.Find("UiTimeAndWave");
        if (uiTimeAndWave == null)
        {
            // Tìm cả trong inactive
            Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
            foreach (var t in allTransforms)
            {
                if (t.gameObject.name == "UiTimeAndWave" && !EditorUtility.IsPersistent(t.gameObject) && t.gameObject.hideFlags == HideFlags.None)
                {
                    uiTimeAndWave = t.gameObject;
                    break;
                }
            }
        }

        if (uiTimeAndWave == null)
        {
            uiTimeAndWave = new GameObject("UiTimeAndWave");
            if (parentTransform != null)
            {
                uiTimeAndWave.transform.SetParent(parentTransform, false);
            }
        }

        Undo.RegisterFullObjectHierarchyUndo(uiTimeAndWave, "Build UI Time And Wave");

        RectTransform mainRect = EnsureRectTransform(uiTimeAndWave);
        // Neo Full Stretch theo Canvas / Parent để các con căn vị trí chuẩn
        mainRect.anchorMin = Vector2.zero;
        mainRect.anchorMax = Vector2.one;
        mainRect.sizeDelta = Vector2.zero;
        mainRect.anchoredPosition = Vector2.zero;

        // 3. Tạo DayText (Neo Top Center, Font Size 36, Color White, text "Day 1")
        Transform dayTextTrans = uiTimeAndWave.transform.Find("DayText");
        GameObject dayTextObj = dayTextTrans != null ? dayTextTrans.gameObject : new GameObject("DayText");
        dayTextObj.transform.SetParent(uiTimeAndWave.transform, false);
        RectTransform dayRect = EnsureRectTransform(dayTextObj);
        // Neo Top Center: min (0.5, 1), max (0.5, 1), pivot (0.5, 1)
        dayRect.anchorMin = new Vector2(0.5f, 1f);
        dayRect.anchorMax = new Vector2(0.5f, 1f);
        dayRect.pivot = new Vector2(0.5f, 1f);
        dayRect.anchoredPosition = new Vector2(0f, -20f);
        dayRect.sizeDelta = new Vector2(300f, 60f);

        TextMeshProUGUI dayTMP = EnsureComponent<TextMeshProUGUI>(dayTextObj);
        dayTMP.text = "Day 1";
        dayTMP.fontSize = 36;
        dayTMP.color = Color.white;
        dayTMP.alignment = TextAlignmentOptions.Center;
        dayTMP.fontStyle = FontStyles.Bold;

        // 4. Tạo AnnouncementPanel (Neo Center, Image trong suốt hoặc đen mờ Alpha 150)
        Transform panelTrans = uiTimeAndWave.transform.Find("AnnouncementPanel");
        GameObject panelObj = panelTrans != null ? panelTrans.gameObject : new GameObject("AnnouncementPanel");
        panelObj.transform.SetParent(uiTimeAndWave.transform, false);
        panelObj.SetActive(true); // Bật tạm để chỉnh component/con trước khi disable
        RectTransform panelRect = EnsureRectTransform(panelObj);
        // Neo Center: min (0.5, 0.5), max (0.5, 0.5), pivot (0.5, 0.5)
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(800f, 200f);

        Image panelImg = EnsureComponent<Image>(panelObj);
        // Màu đen mờ alpha 150 (~0.588f)
        panelImg.color = new Color(0f, 0f, 0f, 150f / 255f);

        // 5. Tạo AnnouncementText bên trong AnnouncementPanel
        Transform textTrans = panelObj.transform.Find("AnnouncementText");
        GameObject textObj = textTrans != null ? textTrans.gameObject : new GameObject("AnnouncementText");
        textObj.transform.SetParent(panelObj.transform, false);
        RectTransform textRect = EnsureRectTransform(textObj);
        // Neo Center: min (0.5, 0.5), max (0.5, 0.5), pivot (0.5, 0.5) hoặc Stretch
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(760f, 160f);

        TextMeshProUGUI announceTMP = EnsureComponent<TextMeshProUGUI>(textObj);
        announceTMP.text = "WARNING";
        announceTMP.fontSize = 60;
        announceTMP.alignment = TextAlignmentOptions.Center;
        announceTMP.fontStyle = FontStyles.Bold;
        announceTMP.color = Color.red;

        // 6. Gắn script UiTimeAndWaveManager vào UiTimeAndWave và gán tham chiếu
        UiTimeAndWaveManager manager = EnsureComponent<UiTimeAndWaveManager>(uiTimeAndWave);
        manager.dayText = dayTMP;
        manager.announcementPanel = panelObj;
        manager.announcementText = announceTMP;

        // 7. Disable (Tắt) AnnouncementPanel theo mặc định
        panelObj.SetActive(false);

        // Lưu trạng thái dirty
        EditorUtility.SetDirty(uiTimeAndWave);
        EditorUtility.SetDirty(manager);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(uiTimeAndWave.scene);

        EditorUtility.DisplayDialog("Thành công", "Đã tạo cấu trúc UiTimeAndWave, thiết lập Text/Panel và gán đầy đủ tham chiếu vào UiTimeAndWaveManager!", "OK");
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
