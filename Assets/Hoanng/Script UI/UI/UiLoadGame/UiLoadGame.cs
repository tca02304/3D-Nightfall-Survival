using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class UiLoadGame : UiBase
{
    [Header("UI Components")]
    public Slider loadingBar;
    public TextMeshProUGUI progressText;

    [Header("Scene Settings")]
    public string nextSceneName = "Home"; // Tên scene để chuyển tiếp

    private void Start()
    {
        // Khi đối tượng này (UI Load Game) được kích hoạt, 
        // Coroutine giả lập tiến trình load sẽ bắt đầu ngay lập tức
        StartCoroutine(FakeLoadingProcess());
    }

    public override void Show()
    {
        base.Show();

        // Reset hiển thị ban đầu
        if (loadingBar != null) loadingBar.value = 0f;
        if (progressText != null) progressText.text = "0%";
    }

    // Hàm cập nhật giao diện hiển thị
    private void UpdateLoadingProgress(float progress)
    {
        // 1. Ép giá trị tiến độ luôn nằm trong giới hạn từ 0.0 đến 1.0
        float clampedProgress = Mathf.Clamp01(progress);

        if (loadingBar != null)
        {
            loadingBar.value = clampedProgress;
        }

        if (progressText != null)
        {
            int percent = Mathf.RoundToInt(clampedProgress * 100);
            progressText.text = $"Loading... {percent}%";
        }

        // Tự động chuyển Scene khi tiến trình đạt 100%
        if (clampedProgress >= 1f)
        {
            // 2. TẮT giao diện Load trước khi sang cảnh mới để không bị đè
            Hide();

            SceneManager.LoadScene(nextSceneName);
        }
    }

    // --- HÀM GIẢ LẬP ĐỂ TEST ĐỘC LẬP TRÊN GIAO DIỆN ---
    private IEnumerator FakeLoadingProcess()
    {
        float fakeProgress = 0f;

        // Mô phỏng thanh load chạy từ 0 đến 1 trong vòng 3 giây
        while (fakeProgress < 1f)
        {
            fakeProgress += Time.deltaTime / 3f;

            // Cập nhật lên màn hình
            UpdateLoadingProgress(fakeProgress);

            yield return null; // Đợi khung hình (frame) tiếp theo
        }
    }
}