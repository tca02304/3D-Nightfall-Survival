using UnityEngine;
using TMPro;

public class UiInteraction : MonoBehaviour
{
    public GameObject interactionPanel; // Kéo object chứa cả khung nền và chữ vào đây
    public TextMeshProUGUI interactionText; // Kéo Text vào đây

    private void OnEnable()
    {
        // Bước 1: Subscribe[cite: 4]
        GameEvents.OnInteractionStateChanged += UpdateInteractionUI;
    }

    private void OnDisable()
    {
        // Bước 3: Unsubscribe để tránh Memory Leak[cite: 4]
        GameEvents.OnInteractionStateChanged -= UpdateInteractionUI;
    }

    private void UpdateInteractionUI(bool isVisible, string message)
    {
        interactionPanel.SetActive(isVisible);
        if (isVisible && interactionText != null)
        {
            interactionText.text = message;
        }
    }
}