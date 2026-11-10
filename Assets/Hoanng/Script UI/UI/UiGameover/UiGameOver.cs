using UnityEngine;
using UnityEngine.UI;

public class UiGameOver : UiBase
{
    [Header("Nút bấm")]
    public Button restartButton;
    public Button menuButton;

    private void OnEnable()
    {
        // Chỉ đăng ký nút bấm
        restartButton.onClick.AddListener(RestartGame);
        menuButton.onClick.AddListener(ReturnToMenu);
    }

    private void OnDisable()
    {
        // Hủy đăng ký nút bấm
        restartButton.onClick.RemoveAllListeners();
        menuButton.onClick.RemoveAllListeners();
    }

    private void RestartGame()
    {
        GameEvents.OnRestartGame?.Invoke(); // Báo cho hệ thống Load lại Scene
    }

    private void ReturnToMenu()
    {
        // Phát sự kiện về Menu, UIManager sẽ nghe thấy và load scene
        GameEvents.OnReturnToMainMenu?.Invoke();
    }
}