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

    // --- Các hàm khi bấm nút ---

    private void ResumeGame()
    {
        // Phát sự kiện Resume ra ngoài
        GameEvents.OnResumeGame?.Invoke();
    }

    private void OpenSettings()
    {
        // Yêu cầu mở UI Setting
        GameEvents.OnRequestSettingsUI?.Invoke();
    }

    private void ReturnToMenu()
    {
        // Yêu cầu chuyển cảnh về Menu
        GameEvents.OnReturnToMainMenu?.Invoke();
    }
}