#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class UiPauseGameFixTool
{
    [MenuItem("Tools/Fix UiPauseGame In Scene")]
    public static void FixUiPauseGame()
    {
        // 1. Tìm UiPauseGame trong Scene hiện tại
        UiPauseGame pauseComponent = Object.FindFirstObjectByType<UiPauseGame>(FindObjectsInactive.Include);
        GameObject pauseObj = null;

        if (pauseComponent != null)
        {
            pauseObj = pauseComponent.gameObject;
        }
        else
        {
            GameObject found = GameObject.Find("UiPauseGame");
            if (found != null)
            {
                pauseObj = found;
                pauseComponent = found.GetComponent<UiPauseGame>();
            }
        }

        if (pauseObj == null)
        {
            EditorUtility.DisplayDialog("Thông báo", "Không tìm thấy GameObject 'UiPauseGame' trong Scene hiện tại!\nHãy mở Scene chứa UiPauseGame (SPLASH hoặc GamePlay) rồi chạy lại Tool.", "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(pauseObj, "Fix UiPauseGame Canvas and Buttons");

        int fixedCount = 0;
        string details = "";

        // 2. Kiểm tra và gỡ bỏ Canvas thừa trên UiPauseGame nếu có
        Canvas redundantCanvas = pauseObj.GetComponent<Canvas>();
        if (redundantCanvas != null)
        {
            Object.DestroyImmediate(redundantCanvas);
            details += "- Đã gỡ bỏ component Canvas thừa (World Space) gây cản trở Raycast/sự kiện click.\n";
            fixedCount++;
        }

        // Gỡ GraphicRaycaster thừa trên chính UiPauseGame nếu Canvas cha đã có
        GraphicRaycaster redundantRaycaster = pauseObj.GetComponent<GraphicRaycaster>();
        if (redundantRaycaster != null)
        {
            Object.DestroyImmediate(redundantRaycaster);
            details += "- Đã gỡ bỏ GraphicRaycaster thừa trên UiPauseGame (sử dụng GraphicRaycaster của Canvas cha UIManager).\n";
            fixedCount++;
        }

        // 3. Tắt Raycast Target trên background Image của UiPauseGame nếu có (để không chặn raycast tới nút)
        Image bgImage = pauseObj.GetComponent<Image>();
        if (bgImage != null)
        {
            bgImage.raycastTarget = true;
        }

        // 4. Tìm các nút con Resume, Setting, Menu
        Button resumeBtn = FindButtonInChildren(pauseObj.transform, "ResumeButton", "Resume");
        Button settingBtn = FindButtonInChildren(pauseObj.transform, "SettingButton", "Setting");
        Button menuBtn = FindButtonInChildren(pauseObj.transform, "MenuButton", "BackGame", "BackToGame", "Menu");

        if (pauseComponent == null)
        {
            pauseComponent = pauseObj.AddComponent<UiPauseGame>();
        }

        if (resumeBtn != null)
        {
            pauseComponent.resumeButton = resumeBtn;
            details += $"- Đã gán resumeButton: {resumeBtn.gameObject.name}\n";
            fixedCount++;
        }

        if (settingBtn != null)
        {
            pauseComponent.settingButton = settingBtn;
            details += $"- Đã gán settingButton: {settingBtn.gameObject.name}\n";
            fixedCount++;
        }

        if (menuBtn != null)
        {
            pauseComponent.menuButton = menuBtn;
            details += $"- Đã gán menuButton: {menuBtn.gameObject.name}\n";
            fixedCount++;
        }

        // 5. Kiểm tra các Canvas con trên các UI khác thuộc UIManager (UiSetting, UiGameOver, UiInventory)
        Transform rootCanvas = pauseObj.transform.parent;
        if (rootCanvas != null)
        {
            Canvas[] allSubCanvases = rootCanvas.GetComponentsInChildren<Canvas>(true);
            foreach (var c in allSubCanvases)
            {
                if (c.gameObject != rootCanvas.gameObject && c.renderMode == RenderMode.WorldSpace && c.worldCamera == null)
                {
                    c.renderMode = RenderMode.ScreenSpaceOverlay;
                    details += $"- Đã sửa Render Mode của Canvas trên '{c.gameObject.name}' từ World Space sang Screen Space - Overlay.\n";
                    fixedCount++;
                }
            }
        }

        EditorUtility.SetDirty(pauseComponent);
        EditorUtility.SetDirty(pauseObj);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(pauseObj.scene);

        EditorUtility.DisplayDialog("Hoàn tất sửa lỗi UiPauseGame",
            $"Đã sửa xong UiPauseGame thành công ({fixedCount} mục):\n\n" + details +
            "\nBây giờ khi chạy game, các nút Resume, Setting, BackGame sẽ hoạt động bình thường!",
            "OK");
    }

    [MenuItem("Tools/Fix UiSetting In Scene")]
    public static void FixUiSetting()
    {
        UiSetting settingComponent = Object.FindFirstObjectByType<UiSetting>(FindObjectsInactive.Include);
        GameObject settingObj = null;

        if (settingComponent != null)
        {
            settingObj = settingComponent.gameObject;
        }
        else
        {
            GameObject found = GameObject.Find("UiSetting");
            if (found != null)
            {
                settingObj = found;
                settingComponent = found.GetComponent<UiSetting>();
            }
        }

        if (settingObj == null)
        {
            EditorUtility.DisplayDialog("Thông báo", "Không tìm thấy GameObject 'UiSetting' trong Scene hiện tại!\nHãy mở Scene chứa UiSetting (ví dụ SPLASH) rồi chạy lại Tool.", "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(settingObj, "Fix UiSetting Close Button");

        int fixedCount = 0;
        string details = "";

        // 1. Tìm nút Close
        Button closeBtn = FindButtonInChildren(settingObj.transform, "Close", "CloseButton", "Button_Close", "BtnClose");
        if (closeBtn != null)
        {
            settingComponent.closeButton = closeBtn;
            details += $"- Đã gán closeButton: {closeBtn.gameObject.name}\n";
            fixedCount++;
        }

        // 2. Kiểm tra nếu có Canvas trên UiSetting, chuyển sang ScreenSpaceOverlay nếu là WorldSpace
        Canvas subCanvas = settingObj.GetComponent<Canvas>();
        if (subCanvas != null)
        {
            if (subCanvas.renderMode == RenderMode.WorldSpace && subCanvas.worldCamera == null)
            {
                subCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                details += "- Đã chuyển RenderMode của Canvas UiSetting sang Screen Space - Overlay.\n";
                fixedCount++;
            }
        }

        EditorUtility.SetDirty(settingComponent);
        EditorUtility.SetDirty(settingObj);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(settingObj.scene);

        EditorUtility.DisplayDialog("Hoàn tất sửa lỗi UiSetting",
            $"Đã sửa xong UiSetting thành công ({fixedCount} mục):\n\n" + details +
            "\nNút Close trên UiSetting giờ đây sẽ tắt giao diện bình thường ở tất cả các Scene!",
            "OK");
    }

    private static Button FindButtonInChildren(Transform parent, params string[] possibleNames)
    {
        foreach (var name in possibleNames)
        {
            Transform t = FindChildRecursive(parent, name);
            if (t != null)
            {
                Button btn = t.GetComponent<Button>();
                if (btn != null) return btn;
            }
        }
        return null;
    }

    private static Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name.Equals(name, System.StringComparison.OrdinalIgnoreCase)) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindChildRecursive(parent.GetChild(i), name);
            if (result != null) return result;
        }
        return null;
    }
}
#endif
