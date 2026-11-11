
using UnityEngine;
using UnityEngine.InputSystem;

public class GuideNPCInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
  

    [Header("UI References")]
    [SerializeField] private GameObject interactionPrompt;

    [Header("Dialogue Reference")]
    [SerializeField] private GuideNPCDialogue dialogue;

    private Transform player;
    private bool isPlayerNearby;

    private void Start()
    {
        Debug.Log("[NPC] Interaction Started");

        if (interactionPrompt == null)
        {
            Debug.LogError("[NPC] InteractionPrompt is NULL!");
            return;
        }

        interactionPrompt.SetActive(false);
    }


    private void Update()
    {
        if (!isPlayerNearby || player == null)
            return;

        // Nếu đang hội thoại thì ẩn Prompt
        if (dialogue != null && dialogue.IsOpen)
        {
            if (interactionPrompt != null)
                interactionPrompt.SetActive(false);

            if (Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                dialogue.CloseDialogue();
            }

            return;
        }

        // Chỉ hiện Prompt khi chưa hội thoại
        if (interactionPrompt != null &&
            !interactionPrompt.activeSelf)
        {
            interactionPrompt.SetActive(true);
        }

        if (dialogue == null)
            return;

        // Nhấn E để mở hội thoại
        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            interactionPrompt.SetActive(false);
            dialogue.StartDialogue();
        }
    }


    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        player = other.transform;
        isPlayerNearby = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        // Chỉ rời vùng khi Collider Player cuối cùng thoát ra
        if (other.transform != player)
            return;

        isPlayerNearby = false;
        player = null;

        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);

        if (dialogue != null && dialogue.IsOpen)
            dialogue.CloseDialogue();

        Debug.Log("[NPC] Player Left!");
    }
}
