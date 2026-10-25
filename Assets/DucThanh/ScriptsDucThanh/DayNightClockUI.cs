using UnityEngine;
using TMPro;

namespace DucThanh
{
    /// <summary>
    /// Component UI đồng hồ hiển thị thời gian trong ngày.
    /// Tự động kết nối với DayNightController nếu chưa gán tham chiếu.
    /// </summary>
    public class DayNightClockUI : MonoBehaviour
    {
        [Header("Controller Reference")]
        [Tooltip("Nếu để trống, script sẽ tự tìm DayNightController trong Scene.")]
        [SerializeField] private DayNightController dayNightController;

        [Header("UI Elements")]
        [Tooltip("Text hiển thị giờ phút (ví dụ 14:05)")]
        [SerializeField] private TextMeshProUGUI timeText;

        [Tooltip("Text hiển thị ngày (ví dụ Day 1)")]
        [SerializeField] private TextMeshProUGUI dayText;

        [Tooltip("Text hoặc nhãn biểu tượng trạng thái (ví dụ Ngày / Đêm hoặc Sun / Moon)")]
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("Display Options")]
        [SerializeField] private string dayFormat = "Day {0}";
        [SerializeField] private string dayStatusLabel = "☀️ Day";
        [SerializeField] private string nightStatusLabel = "🌙 Night";

        private void Start()
        {
            if (dayNightController == null)
            {
                dayNightController = Object.FindFirstObjectByType<DayNightController>();
            }

            if (timeText == null)
            {
                timeText = GetComponent<TextMeshProUGUI>();
            }
        }

        private void Update()
        {
            if (dayNightController == null) return;

            float currentHour = dayNightController.CurrentHour;
            int hours = Mathf.FloorToInt(currentHour);
            int minutes = Mathf.FloorToInt((currentHour - hours) * 60f);

            if (timeText != null)
            {
                timeText.text = $"{hours:D2}:{minutes:D2}";
            }

            if (dayText != null)
            {
                dayText.text = string.Format(dayFormat, dayNightController.CurrentDay);
            }

            if (statusText != null)
            {
                statusText.text = dayNightController.IsDayTime ? dayStatusLabel : nightStatusLabel;
            }
        }
    }
}
