
using UnityEngine;
using TMPro;

public class GuideNPCDialogue : MonoBehaviour
{
    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text npcNameText;
    [SerializeField] private TMP_Text dialogueText;

    [Header("NPC Information")]
    [SerializeField] private string npcName = "The Survivor";

    [Header("First Encounter")]
    [TextArea(2, 5)]
    [SerializeField]
    private string[] firstDialogueLines =
    {
        "You're finally awake.",
        "This place is dangerous. You need to learn how to survive.",
        "First, collect some stones. You will need them to craft your tools.",
        "Now find a tree and collect some wood.",
        "Good luck, survivor. The night is coming."
    };

    [Header("Quest In Progress")]
    [TextArea(2, 5)]
    [SerializeField]
    private string[] reminderDialogueLines =
    {
        "You still have work to do.",
        "Collect 5 Stone and 5 Wood before returning to me."
    };

    [Header("After Quest 1")]
    [TextArea(2, 5)]
    [SerializeField]
    private string[] secondDialogueLines =
    {
        "Well done, survivor!",
        "You have proven that you can survive.",
        "But your journey has only just begun.",
        "There are more dangerous things waiting beyond these lands."
    };

    private int currentLine;
    private string[] currentDialogueLines;

    private enum DialogueStage
    {
        FirstEncounter,
        QuestReminder,
        SecondEncounter
    }

    private DialogueStage currentStage;

    public bool IsOpen { get; private set; }

    private void Start()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    public void StartDialogue()
    {
        if (IsOpen)
            return;

        if (QuestManager.Instance == null)
        {
            Debug.LogError("QuestManager not found!");
            return;
        }

        switch (QuestManager.Instance.CurrentStatus)
        {
            case QuestManager.QuestStatus.NotStarted:

                currentStage = DialogueStage.FirstEncounter;
                currentDialogueLines = firstDialogueLines;
                break;

            case QuestManager.QuestStatus.Active:

                currentStage = DialogueStage.QuestReminder;
                currentDialogueLines = reminderDialogueLines;
                break;

            case QuestManager.QuestStatus.Completed:

                currentStage = DialogueStage.SecondEncounter;
                currentDialogueLines = secondDialogueLines;
                break;
        }

        if (currentDialogueLines == null ||
            currentDialogueLines.Length == 0)
        {
            Debug.LogWarning("Dialogue lines are empty!");
            return;
        }

        currentLine = 0;
        IsOpen = true;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        if (npcNameText != null)
            npcNameText.text = npcName;

        ShowCurrentLine();
    }

    public void NextLine()
    {
        if (!IsOpen)
            return;

        currentLine++;

        if (currentLine >= currentDialogueLines.Length)
        {
            FinishDialogue();
            return;
        }

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        if (dialogueText != null)
            dialogueText.text = currentDialogueLines[currentLine];
    }

    private void FinishDialogue()
    {
        // Chỉ kích hoạt Quest 1 khi kết thúc hội thoại đầu tiên
        if (currentStage == DialogueStage.FirstEncounter)
        {
            QuestManager.Instance.StartQuest1();
        }

        CloseDialogue();
    }

    public void CloseDialogue()
    {
        IsOpen = false;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }
}
