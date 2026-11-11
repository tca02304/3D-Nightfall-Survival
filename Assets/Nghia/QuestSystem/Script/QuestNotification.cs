
using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class QuestNotification : MonoBehaviour
{
    [SerializeField] private TMP_Text notificationText;
    [SerializeField] private float fadeDuration = 0.35f;
    [SerializeField] private float displayDuration = 2.5f;

    private CanvasGroup canvasGroup;
    private Coroutine notificationRoutine;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    public void ShowNotification(string message)
    {
        if (notificationRoutine != null)
            StopCoroutine(notificationRoutine);

        gameObject.SetActive(true);

        if (notificationText != null)
            notificationText.text = message;

        notificationRoutine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        yield return Fade(0f, 1f);

        yield return new WaitForSeconds(displayDuration);

        yield return Fade(1f, 0f);

        gameObject.SetActive(false);
        notificationRoutine = null;
    }

    private IEnumerator Fade(float start, float end)
    {
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            canvasGroup.alpha = Mathf.Lerp(
                start,
                end,
                timer / fadeDuration
            );

            yield return null;
        }

        canvasGroup.alpha = end;
    }
}
