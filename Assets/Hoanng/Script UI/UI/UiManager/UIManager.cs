using UnityEngine;
using UnityEngine.SceneManagement;
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Panels")]
    public UiBase uiSetting;
    public UiBase uiInventory;
    
    public UiPauseGame uiPause;     // Kéo object UiPause vào đây
    public UiGameOver uiGameOver;   // Kéo object UiGameOver vào đây
    public UiBase uiCrafting;
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
        // Đăng ký thêm các sự kiện Game Over và Load Menu
        GameEvents.OnPlayerDied += ShowGameOver;
        GameEvents.OnReturnToMainMenu += LoadHomeScene;

        // Thêm sự kiện Pause/Resume
        GameEvents.OnPauseGame += ShowPauseMenu;
        GameEvents.OnResumeGame += HidePauseMenu;
        // ... (các đăng ký cũ)
        GameEvents.OnRequestCraftingUI += ShowCrafting; // Lắng nghe sự kiện
                                                        // Thêm dòng này để lắng nghe yêu cầu mở túi đồ
        GameEvents.OnRequestInventoryUI += ShowInventory;
        GameEvents.OnToggleInventoryUI += ToggleInventory;
    }

    private void OnDisable()
    {
        // 2. Hủy đăng ký (Unsubscribe): Tránh lỗi Memory Leak
        GameEvents.OnRequestSettingsUI -= ShowSetting;
        GameEvents.OnPlayerDied -= ShowGameOver;
        GameEvents.OnReturnToMainMenu -= LoadHomeScene;
        GameEvents.OnPauseGame -= ShowPauseMenu;
        GameEvents.OnResumeGame -= HidePauseMenu;
        // ... (các hủy đăng ký cũ)
        GameEvents.OnRequestCraftingUI -= ShowCrafting;
        // Hủy lắng nghe
        GameEvents.OnRequestInventoryUI -= ShowInventory;
        GameEvents.OnToggleInventoryUI -= ToggleInventory;
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            GameEvents.OnToggleInventoryUI?.Invoke();
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

        // Bắt sự kiện phím Tab
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            // Phát sự kiện yêu cầu bật/tắt túi đồ
            GameEvents.OnToggleInventoryUI?.Invoke();
        }
    }

    // 3. Xử lý (Notify): Đánh thức UiSetting dậy
    public void ShowSetting()
    {
        // Nếu UI Pause đang hiển thị, hãy tạm ẩn nó đi
        if (uiPause != null && uiPause.gameObject.activeInHierarchy)
        {
            uiPause.Hide();
        }
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
        // Nếu thời gian đang bị đóng băng (Time.timeScale == 0), tức là game đang Pause
        // thì hãy bật lại UI Pause sau khi đã đóng Setting.
        if (Time.timeScale == 0f && uiPause != null)
        {
            uiPause.gameObject.SetActive(true);
            uiPause.Show();
        }
    }

    private void ShowGameOver()
    {
        if (uiGameOver != null)
        {
            uiGameOver.gameObject.SetActive(true);
            uiGameOver.Show();
            Time.timeScale = 0f; // Đóng băng thời gian
        }
    }

    // Hàm thực hiện chuyển đổi
    private void ShowCrafting()
    {
        if (uiInventory != null) uiInventory.Hide(); // Tắt túi đồ

        if (uiCrafting != null)
        {
            uiCrafting.gameObject.SetActive(true);
            uiCrafting.Show(); // Bật chế tạo
        }
    }
    // Hàm chuyển về Inventory
    private void ShowInventory()
    {
        if (uiCrafting != null) uiCrafting.Hide(); // Tắt bảng chế tạo

        if (uiInventory != null)
        {
            uiInventory.gameObject.SetActive(true);
            uiInventory.Show(); // Bật lại túi đồ
        }
    }
    // Hàm xử lý việc chuyển đổi trạng thái (Toggle)
    private void ToggleInventory()
    {
        if (uiInventory != null)
        {
            // Quan trọng: Kiểm tra GameObject, KHÔNG phải check component
            if (uiInventory.gameObject.activeSelf)
            {
                uiInventory.Hide(); // Tắt
            }
            else
            {
                uiInventory.gameObject.SetActive(true);
                uiInventory.Show(); // Bật
            }
        }
    }
    private void LoadHomeScene()
    {
        Time.timeScale = 1f; // Bắt buộc mở khóa thời gian trước khi chuyển cảnh

        // Tắt bảng Game Over đi trước khi về Home
        if (uiGameOver != null) uiGameOver.Hide();
        if (uiPause != null) uiPause.Hide(); // Đảm bảo tắt Pause khi về Menu

        SceneManager.LoadScene("Home");
    }

    // --- Hàm xử lý Pause/Resume ---
    private void ShowPauseMenu()
    {
        if (uiPause != null)
        {
            uiPause.gameObject.SetActive(true);
            uiPause.Show();
            Time.timeScale = 0f; // Đóng băng thời gian
        }
    }

    private void HidePauseMenu()
    {
        if (uiPause != null)
        {
            uiPause.Hide();
            Time.timeScale = 1f; // Tiếp tục thời gian
        }
    }
}