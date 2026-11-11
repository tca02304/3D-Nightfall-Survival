using UnityEngine;
using UnityEngine.SceneManagement;

public class HomeMenuController : MonoBehaviour
{
    // Tên của Scene mà bạn muốn chuyển đến (ví dụ: SPLASH)
    public string targetSceneName = "SPLASH";

    // Hàm này sẽ được gọi khi người chơi bấm nút Settings
    public void OnSettingsButtonClicked()
    {
        // 1. Báo cho UIManager nhớ rằng ta đang ở Home
        // (Giả sử UIManager của bạn là một Singleton)
        if (UIManager.Instance != null)
        {
            UIManager.Instance.previousSceneName = "Home";
        }

        // 2. Kích hoạt giao diện Settings thông qua Event 
        // (Sự kiện này sẽ được UIManager bên SPLASH nhận)
        // 2. Gọi UIManager để bật Settings thay vì dùng Event
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowSetting();
        }

        // 3. Chuyển scene sang SPLASH 
        // LƯU Ý: Phải gọi LoadScene cuối cùng để Event kịp gửi đi
        SceneManager.LoadScene("SPLASH");
    }

    public void OnPlayButtonClicked()
    {
        // Nếu bạn chỉ muốn ẨN Menu đi để tái sử dụng sau:
        this.gameObject.SetActive(false);
        // Chuyển scene sang GamePlay
        SceneManager.LoadScene("GamePlay");
    }

    public void OnQuitButtonClicked()
    {
        Debug.Log("Quit game");
        Application.Quit();
    }
}
