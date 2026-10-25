using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class UiSetting : UiBase
{
    [Header("UI Elements")]
    public Slider sensitivitySlider;
    public Slider volumeSlider;
    public Toggle fullscreenToggle;

    private void Start()
    {
        // Khởi tạo các sự kiện lắng nghe (Listener) khi kéo thả slider hoặc bấm toggle
        // Viết ở đây giúp bạn không phải kéo thả thủ công từng sự kiện trong Inspector
        sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggled);
    }

    // Ghi đè hàm Show() của UiBase
    public override void Show()
    {
        base.Show(); // Bắt buộc gọi base.Show() để thực hiện SetActive(true) từ UiBase

        // Cập nhật lại UI mỗi khi Panel này được bật lên
        LoadCurrentSettings();
    }

    private void LoadCurrentSettings()
    {
        // Lấy dữ liệu đã lưu (dùng PlayerPrefs) hoặc gán giá trị mặc định
        sensitivitySlider.value = PlayerPrefs.GetFloat("Sensitivity", 0.5f);
        volumeSlider.value = PlayerPrefs.GetFloat("Volume", 1f);
        fullscreenToggle.isOn = Screen.fullScreen;
    }

    // --- Các hàm xử lý logic ---

    public void OnSensitivityChanged(float value)
    {
        Debug.Log("Độ nhạy chuột hiện tại: " + value);
        // Code điều chỉnh độ nhạy cho Camera/Player
    }

    public void OnVolumeChanged(float value)
    {
        Debug.Log("Âm lượng hiện tại: " + value);
        // Code điều chỉnh AudioMixer
    }

    public void OnFullscreenToggled(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    // Nút Save gọi hàm này
    public void SaveSettings()
    {
        // Lưu thông số vào hệ thống máy tính bằng PlayerPrefs
        PlayerPrefs.SetFloat("Sensitivity", sensitivitySlider.value);
        PlayerPrefs.SetFloat("Volume", volumeSlider.value);
        PlayerPrefs.Save();

        Debug.Log("Đã lưu cấu hình Settings!");
    }

    // Nút Close gọi hàm này
    public void CloseSettings()
    {
        // Cách 1: Gọi trực tiếp hàm Hide() được kế thừa từ UiBase (nó sẽ SetActive(false))
        Hide();

        // Cách 2: Báo cho UIManager ẩn đi (Khuyên dùng để hệ thống quản lý tập trung hơn)
        // UIManager.Instance.HideSetting();
    }
}
