using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Panels")]
    public UiBase uiSetting;
    public UiBase uiInventory;
    
    public UiPauseGame uiPause;     // Kéo object UiPause vào đây
    public UiGameOver uiGameOver;   // Kéo object UiGameOver vào đây

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // --- MẪU CHUẨN 3 BƯỚC CHO OBSERVER PATTERN ---

    private void OnEnable()
    {
        // 1. Đăng ký (Subscribe): Lắng nghe tín hiệu từ Scene Home Menu
        GameEvents.OnRequestSettingsUI += ShowSetting;
    }

    private void OnDisable()
    {
        // 2. Hủy đăng ký (Unsubscribe): Tránh lỗi Memory Leak
        GameEvents.OnRequestSettingsUI -= ShowSetting;
    }
    private void Update()
    {
        // Bật/tắt Inventory bằng phím I hoặc Tab
        if (Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Tab))
        {
            if (uiInventory != null)
            {
                if (uiInventory.gameObject.activeInHierarchy) uiInventory.Hide();
                else uiInventory.Show();
            }
        }

        // Bật tắt nút Tạm dừng (Pause) bằng phím ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Không cho bật Pause nếu đang ở màn hình Game Over
            if (uiGameOver != null && uiGameOver.gameObject.activeInHierarchy) return;

            if (uiPause.gameObject.activeInHierarchy)
            {
                // Nếu đang bật thì giả lập bấm nút Resume
                uiPause.resumeButton.onClick.Invoke();
            }
            else
            {
                uiPause.Show();
            }
        }
    }

    // 3. Xử lý (Notify): Đánh thức UiSetting dậy
    public void ShowSetting()
    {
        if (uiSetting != null)
        {
            // Bật GameObject chứa UiSetting lên (dù trước đó nó đang tắt dấu tích)
            uiSetting.gameObject.SetActive(true);
            uiSetting.Show();
        }
    }

    public void HideSetting()
    {
        if (uiSetting != null) uiSetting.Hide();
    }
}