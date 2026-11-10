using UnityEngine;
using UnityEngine.SceneManagement;

public class HomeMenuController : MonoBehaviour
{
    // Tên của Scene mà bạn muốn chuyển đến (ví dụ: SPLASH)
    public string targetSceneName = "SPLASH";

    // Hàm này sẽ được gọi khi người chơi bấm nút Settings
    public void OnSettingsButtonClicked()
    {
        // 1. Chuyển scene sang SPLASH
        SceneManager.LoadScene(targetSceneName);

        // 2. Kích hoạt giao diện Settings thông qua Event
        // Giả sử trong hệ thống GameEvents của bạn đã có: public static Action OnRequestSettingsUI;
        GameEvents.OnRequestSettingsUI?.Invoke();
    }

    public void OnPlayButtonClicked()
    {
        // Hàm cho nút Play (bạn có thể điền sau)
        Debug.Log("Play button clicked");
    }

    public void OnQuitButtonClicked()
    {
        Debug.Log("Quit game");
        Application.Quit();
    }
}
