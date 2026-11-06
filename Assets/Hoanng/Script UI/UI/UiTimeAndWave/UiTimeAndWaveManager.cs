using System.Collections;
using UnityEngine;
using TMPro;

public class UiTimeAndWaveManager : MonoBehaviour
{
    [Header("Top UI - Day Tracking")]
    public TextMeshProUGUI dayText; // Hiển thị "Day: 1"

    [Header("Center UI - Announcements")]
    public GameObject announcementPanel; // Panel chứa thông báo giữa màn hình
    public TextMeshProUGUI announcementText; // Chữ thông báo (vd: "NIGHT FALLS", "WAVE 1 INCOMING")

    // Biến lưu trữ Coroutine hiện tại để chống lặp/đè thông báo
    private Coroutine currentAnnouncement;

    private void OnEnable()
    {
        // 1. Đăng ký lắng nghe sự kiện
        GameEvents.OnDayChanged += UpdateDayText;
        GameEvents.OnNightStarted += ShowNightWarning;
        GameEvents.OnWaveStarted += ShowWaveWarning;
        GameEvents.OnWaveEnded += ShowWaveCleared;
    }

    private void OnDisable()
    {
        // 2. Hủy đăng ký để tránh Memory Leak
        GameEvents.OnDayChanged -= UpdateDayText;
        GameEvents.OnNightStarted -= ShowNightWarning;
        GameEvents.OnWaveStarted -= ShowWaveWarning;
        GameEvents.OnWaveEnded -= ShowWaveCleared;
    }

    private void Start()
    {
        // Tắt thông báo lúc mới vào game
        if (announcementPanel != null)
            announcementPanel.SetActive(false);
    }

    // --- CÁC HÀM XỬ LÝ SỰ KIỆN ---

    private void UpdateDayText(int day)
    {
        if (dayText != null)
            dayText.text = $"Day {day}";
    }

    private void ShowNightWarning()
    {
        // Đổi màu đỏ thẫm/tím cho đáng sợ
        TriggerAnnouncement("NIGHT FALLS\nSurvive the darkness!", new Color(0.8f, 0.2f, 0.2f));
    }

    private void ShowWaveWarning(int waveIndex)
    {
        // Đổi màu đỏ tươi cảnh báo nguy hiểm
        TriggerAnnouncement($"WAVE {waveIndex} INCOMING\nDefend your camp!", Color.red);
    }

    private void ShowWaveCleared()
    {
        // Đổi màu xanh lam/vàng khi an toàn
        TriggerAnnouncement("WAVE CLEARED\nYou survived this time.", Color.yellow);
    }

    // --- LOGIC HIỂN THỊ & CHỐNG SPAM ---

    private void TriggerAnnouncement(string message, Color textColor)
    {
        // Nếu backend vô tình spam gửi sự kiện, ta dừng ngay thông báo cũ để chạy cái mới nhất
        if (currentAnnouncement != null)
        {
            StopCoroutine(currentAnnouncement);
        }

        currentAnnouncement = StartCoroutine(AnnouncementRoutine(message, textColor));
    }

    private IEnumerator AnnouncementRoutine(string message, Color textColor)
    {
        // Bật panel và set chữ
        announcementPanel.SetActive(true);
        announcementText.text = message;
        announcementText.color = textColor;

        // Giữ thông báo trên màn hình đúng 3.5 giây
        yield return new WaitForSeconds(3.5f);

        // Sau đó tự động tắt đi
        announcementPanel.SetActive(false);
        currentAnnouncement = null;
    }
}