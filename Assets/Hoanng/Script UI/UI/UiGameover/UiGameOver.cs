using UnityEngine;
using UnityEngine.UI;

// Kế thừa UiBase để dùng sẵn hàm Show(), Hide()
public class UiGameOver : UiBase
{
    [Header("Nút bấm")]
    public Button restartButton;
    public Button menuButton;

    private void OnEnable()
    {
        // 1. Subscribe: Lắng nghe sự kiện Player chết[cite: 4]
        GameEvents.OnPlayerDied += HandlePlayerDied;

        // Đăng ký sự kiện khi click nút
        restartButton.onClick.AddListener(RestartGame);
        menuButton.onClick.AddListener(ReturnToMenu);
    }

    private void OnDisable()
    {
        // 3. Unsubscribe: Hủy lắng nghe để tránh lỗi Memory Leak[cite: 4]
        GameEvents.OnPlayerDied -= HandlePlayerDied;

        restartButton.onClick.RemoveAllListeners();
        menuButton.onClick.RemoveAllListeners();
    }

    // Hàm này sẽ tự động chạy khi Player phát sóng OnPlayerDied
    private void HandlePlayerDied()
    {
        Show(); // Bật giao diện Game Over lên
        Time.timeScale = 0f; // Đóng băng thời gian trong game
    }

    private void RestartGame()
    {
        Time.timeScale = 1f; // Bắt buộc mở khóa thời gian trước khi load lại
        GameEvents.OnRestartGame?.Invoke(); // Báo cho hệ thống Load lại Scene
    }

    private void ReturnToMenu()
    {
        Time.timeScale = 1f; // Bắt buộc mở khóa thời gian
        GameEvents.OnReturnToMainMenu?.Invoke(); // Báo cho hệ thống quay về Menu
    }
}