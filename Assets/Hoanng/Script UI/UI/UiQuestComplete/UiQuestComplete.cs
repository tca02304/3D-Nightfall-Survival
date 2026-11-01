using UnityEngine;
using TMPro;
using System.Collections;

public class UiQuestComplete : MonoBehaviour
{
    public GameObject popupPanel;
    public TextMeshProUGUI questNameText;
    public float displayTime = 3f; // Hiện trong 3 giây rồi tự tắt

    private void OnEnable()
    {
        GameEvents.OnQuestCompleted += ShowCompletionPopup;
    }

    private void OnDisable()
    {
        GameEvents.OnQuestCompleted -= ShowCompletionPopup;
    }

    private void ShowCompletionPopup(string questName)
    {
        popupPanel.SetActive(true);
        questNameText.text = questName;

        // Dừng các Coroutine cũ nếu người chơi hoàn thành 2 quest cùng lúc
        StopAllCoroutines();
        StartCoroutine(HidePopupRoutine());
    }

    private IEnumerator HidePopupRoutine()
    {
        yield return new WaitForSeconds(displayTime);
        popupPanel.SetActive(false);
    }
}