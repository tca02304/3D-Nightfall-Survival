using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UiLoadGame : UiBase
{
    [Header("UI Components")]
    public Slider loadingBar;            // Kéo LoadingBar (Slider) vào đây
    public TextMeshProUGUI progressText; // Kéo Text % hoặc "Loading..." vào đây
    

    private void OnEnable()
    {
        // 1. Đăng ký (Subscribe) lắng nghe sự kiện tiến độ load từ GameManager (Member 1)
        GameEvents.OnLoadingProgressUpdated += UpdateLoadingProgress;
    }

    private void OnDisable()
    {
        // 3. Hủy đăng ký (Unsubscribe) khi ẩn màn hình Load để tránh rò rỉ bộ nhớ
        GameEvents.OnLoadingProgressUpdated -= UpdateLoadingProgress;
    }

    public override void Show()
    {
        base.Show(); // Bật Panel lên

        // Reset lại thanh load về 0 khi mới bắt đầu mở
        if (loadingBar != null) loadingBar.value = 0f;
        if (progressText != null) progressText.text = "0%";
    }

    // 2. Nhận thông tin (Notify): Hàm này tự chạy khi Member 1 phát tín hiệu tiến độ load (từ 0.0 đến 1.0)
    private void UpdateLoadingProgress(float progress)
    {
        if (loadingBar != null)
        {
            loadingBar.value = progress; // Slider thường chạy từ 0 đến 1
        }

        if (progressText != null)
        {
            // Đổi số thập phân (ví dụ 0.5) thành dạng phần trăm (50%)
            int percent = Mathf.RoundToInt(progress * 100);
            progressText.text = $"Loading... {percent}%";
        }

        // Tự động tắt màn hình Load khi tiến trình đạt 100%
        if (progress >= 1f)
        {
            // Bạn có thể hoãn tắt 0.5s để người chơi thấy chữ 100% (nếu cần), ở đây ta tắt luôn:
            Invoke(nameof(Hide), 0.5f);
        }
    }
}