using UnityEngine;
using TMPro;

namespace DucThanh
{
    public class DayNightUI : MonoBehaviour
    {
        [Header("UI Text References")]
        [SerializeField] private TextMeshProUGUI dayText;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private string clockPrefix = "";

        private DayNightController dayNightController;

        private void Awake()
        {
            // Tự động tìm "Day Text" và "Time Text" trong các object con nếu bạn quên kéo thả
            if (dayText == null)
            {
                Transform dayObj = transform.Find("Day Text");
                if (dayObj != null) dayText = dayObj.GetComponent<TextMeshProUGUI>();
            }

            if (timeText == null)
            {
                Transform timeObj = transform.Find("Time Text");
                if (timeObj != null) timeText = timeObj.GetComponent<TextMeshProUGUI>();
            }
        }

        private void Update()
        {
            // 1. Tự động tìm DayNightController trong Scene (ngay cả khi nó sinh ra trễ)
            if (dayNightController == null)
            {
                dayNightController = FindAnyObjectByType<DayNightController>();
                return; // Đợi đến khi tìm thấy mới chạy tiếp
            }

            // 2. Nếu đã tìm thấy, liên tục cập nhật đồng hồ
            UpdateUI();
        }

        private void UpdateUI()
        {
            // Cập nhật Ngày
            if (dayText != null)
            {
                dayText.text = $"Day {dayNightController.CurrentDay}";
            }

            // Cập nhật Giờ (Định dạng HH:mm)
            if (timeText != null)
            {
                int hours = Mathf.FloorToInt(dayNightController.CurrentHour);
                int minutes = Mathf.FloorToInt((dayNightController.CurrentHour - hours) * 60f);

                timeText.text = $"{clockPrefix}{hours:D2}:{minutes:D2}";
            }
        }
    }
}