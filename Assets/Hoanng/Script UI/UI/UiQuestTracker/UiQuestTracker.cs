using UnityEngine;
using TMPro;

public class UiQuestTracker : MonoBehaviour
{
    public GameObject trackerPanel;
    public TextMeshProUGUI questTitleText;
    public TextMeshProUGUI questProgressText;

    private void OnEnable()
    {
        GameEvents.OnQuestProgressUpdated += UpdateQuestUI;
    }

    private void OnDisable()
    {
        GameEvents.OnQuestProgressUpdated -= UpdateQuestUI;
    }

    private void UpdateQuestUI(string title, string progress)
    {
        // Nếu title trống tức là không có nhiệm vụ nào -> Ẩn bảng
        if (string.IsNullOrEmpty(title))
        {
            trackerPanel.SetActive(false);
            return;
        }

        trackerPanel.SetActive(true);
        questTitleText.text = title;
        questProgressText.text = progress;
    }
}