using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UiPauseGame : UiBase
{
    [Header("Buttons")]
    public Button resumeButton;
    public Button settingButton;
    public Button menuButton;

    [Header("Scene Settings")]
    [Tooltip("Tên scene Home/Menu để quay về khi nhấn Back To Game/Menu")]
    public string homeSceneName = "Home";

    private void Awake()
    {
        SetupButtonListeners();
    }

    private void OnEnable()
    {
        SetupButtonListeners();

        // Đóng băng thời gian và mở con trỏ chuột
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public override void Show()
    {
        base.Show();
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        // Khi bảng Pause đang hiển thị, luôn đảm bảo con trỏ chuột không bị bất kỳ script nào khác khóa lại
        if (Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.None;
        }
        if (!Cursor.visible)
        {
            Cursor.visible = true;
        }
    }

    private void SetupButtonListeners()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(ResumeGame);
            resumeButton.onClick.AddListener(ResumeGame);
        }

        if (settingButton != null)
        {
            settingButton.onClick.RemoveListener(OpenSettings);
            settingButton.onClick.AddListener(OpenSettings);
        }

        if (menuButton != null)
        {
            menuButton.onClick.RemoveListener(ReturnToMenu);
            menuButton.onClick.AddListener(ReturnToMenu);
        }
    }

    private void OnDisable()
    {
        if (resumeButton != null)
            resumeButton.onClick.RemoveListener(ResumeGame);

        if (settingButton != null)
            settingButton.onClick.RemoveListener(OpenSettings);

        if (menuButton != null)
            menuButton.onClick.RemoveListener(ReturnToMenu);
    }

    // --- Các hàm khi bấm nút ---

    /// <summary>
    /// Đóng UiPauseGame, mở lại thời gian và khóa chuột cho gameplay
    /// </summary>
    public void ResumeGame()
    {
        Hide();
        Time.timeScale = 1f;

        // Nếu đang trong scene gameplay thì khóa lại con trỏ chuột để người chơi điều khiển góc nhìn
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == "GamePlay")
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    /// <summary>
    /// Mở UiSetting và tạm ẩn UiPauseGame
    /// </summary>
    public void OpenSettings()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.previousSceneName = "GamePlay";
            UIManager.Instance.ShowSetting();
        }
        else
        {
            UiSetting settingUI = Object.FindFirstObjectByType<UiSetting>(FindObjectsInactive.Include);
            if (settingUI != null)
            {
                settingUI.gameObject.SetActive(true);
                settingUI.Show();
                Hide();
            }
        }
    }

    /// <summary>
    /// Khôi phục thời gian và quay trở lại Scene Home
    /// </summary>
    public void ReturnToMenu()
    {
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Hide();

        if (UIManager.Instance != null)
        {
            if (UIManager.Instance.uiGameOver != null) UIManager.Instance.uiGameOver.Hide();
            if (UIManager.Instance.uiSetting != null) UIManager.Instance.uiSetting.Hide();
        }

        SceneManager.LoadScene(string.IsNullOrEmpty(homeSceneName) ? "Home" : homeSceneName);
    }
}
