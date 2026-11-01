using UnityEngine;
using UnityEngine.UI;

public class UiPauseGame : UiBase
{
    public Button resumeButton;
    public Button settingButton;
    public Button menuButton;

    private void OnEnable()
    {
        resumeButton.onClick.AddListener(ResumeGame);
        settingButton.onClick.AddListener(OpenSettings);
        menuButton.onClick.AddListener(ReturnToMenu);
    }

    private void OnDisable()
    {
        resumeButton.onClick.RemoveAllListeners();
        settingButton.onClick.RemoveAllListeners();
        menuButton.onClick.RemoveAllListeners();
    }

    // Khi UIManager gọi hàm Show() để bật bảng Pause, ta phải dừng thời gian
    public override void Show()
    {
        base.Show();
        Time.timeScale = 0f; // Dừng game
    }

    // Bấm nút Resume
    private void ResumeGame()
    {
        Time.timeScale = 1f; // Tiếp tục game
        Hide(); // Tắt bảng Pause

        // Phát sóng cho GameManager biết game đã tiếp tục (để mở khóa chuột/camera)
        GameEvents.OnResumeGame?.Invoke();
    }

    private void OpenSettings()
    {
        // Gọi UIManager bật Settings (nằm đè lên Pause)
        UIManager.Instance.ShowSetting();
    }

    private void ReturnToMenu()
    {
        Time.timeScale = 1f; // Luôn nhớ trả lại timeScale = 1 trước khi chuyển scene
        GameEvents.OnReturnToMainMenu?.Invoke();
    }
}
