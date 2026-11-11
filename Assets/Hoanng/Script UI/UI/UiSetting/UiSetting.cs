using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UiSetting : UiBase
{
    [Header("UI Elements")]
    public Slider sensitivitySlider;
    public Slider volumeSlider;
    public Toggle fullscreenToggle;

    [Header("Buttons")]
    public Button closeButton;

    private void Awake()
    {
        SetupCloseButton();
    }

    private void Start()
    {
        SetupCloseButton();

        // Khởi tạo các sự kiện lắng nghe (Listener) khi kéo thả slider hoặc bấm toggle
        if (sensitivitySlider != null)
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        if (volumeSlider != null)
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggled);
    }

    private void OnEnable()
    {
        SetupCloseButton();
    }

    private void SetupCloseButton()
    {
        if (closeButton == null)
        {
            Transform closeTrans = transform.Find("Close");
            if (closeTrans == null) closeTrans = transform.Find("CloseButton");
            if (closeTrans != null)
            {
                closeButton = closeTrans.GetComponent<Button>();
            }
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseSettings);
            closeButton.onClick.AddListener(CloseSettings);
        }
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
        if (sensitivitySlider != null)
            sensitivitySlider.value = PlayerPrefs.GetFloat("Sensitivity", 0.5f);
        if (volumeSlider != null)
            volumeSlider.value = PlayerPrefs.GetFloat("Volume", 1f);
        if (fullscreenToggle != null)
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
        if (sensitivitySlider != null)
            PlayerPrefs.SetFloat("Sensitivity", sensitivitySlider.value);
        if (volumeSlider != null)
            PlayerPrefs.SetFloat("Volume", volumeSlider.value);
        PlayerPrefs.Save();

        Debug.Log("Đã lưu cấu hình Settings!");
    }

    // Nút Close gọi hàm này
    public void CloseSettings()
    {
        // 1. Luôn luôn tắt giao diện Setting này ngay lập tức
        Hide();
        gameObject.SetActive(false);

        // 2. Thông báo cho UIManager xử lý điều hướng tiếp theo (về Home hoặc mở lại Pause)
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideSetting();
        }
    }
}
