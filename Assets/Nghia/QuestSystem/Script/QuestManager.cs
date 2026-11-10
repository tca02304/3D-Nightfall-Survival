
using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    public enum QuestStatus
    {
        NotStarted,
        Active,
        Completed
    }

    [Header("Quest Definitions")]
    [SerializeField] private List<QuestDefinition> questDefinitions = new();
    [Header("Quest Notification")]
    [SerializeField] private QuestNotification questNotification;
    [Header("Quest Guide")]
    [SerializeField] private QuestGuideController questGuideController;
    [Header("Quest UI")]
    [SerializeField] private GameObject questPanel;
    [SerializeField] private TMP_Text questNameText;
    [SerializeField] private TMP_Text questDescriptionText;
    [SerializeField] private TMP_Text questProgressText;

    private QuestDefinition currentQuest;
    private readonly List<QuestObjectiveData> runtimeObjectives = new();

    private QuestStatus currentStatus = QuestStatus.NotStarted;

    private bool quest2Unlocked;

    public QuestStatus CurrentStatus => currentStatus;
    public bool Quest2Unlocked => quest2Unlocked;

    public string CurrentQuestId =>
        currentQuest != null ? currentQuest.questId : string.Empty;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        QuestEvents.OnObjectiveProgress += HandleObjectiveProgress;
    }

    private void OnDisable()
    {
        QuestEvents.OnObjectiveProgress -= HandleObjectiveProgress;
    }

    private void Start()
    {
        if (questPanel != null)
            questPanel.SetActive(false);
    }

    // Giữ tương thích với GuideNPCDialogue cũ.
    public void StartQuest1()
    {
        StartQuest("quest_1");
    }

    public bool StartQuest(string questId)
    {
        if (currentStatus == QuestStatus.Active)
        {
            Debug.LogWarning("[Quest] Đang có quest hoạt động.");
            return false;
        }

        QuestDefinition definition = questDefinitions.Find(
            quest => quest != null && quest.questId == questId
        );

        if (definition == null)
        {
            Debug.LogError("[Quest] Không tìm thấy Quest: " + questId);
            return false;
        }

        if (questId == "quest_2" && !quest2Unlocked)
        {
            Debug.LogWarning("[Quest] Quest 2 chưa được mở khóa.");
            return false;
        }

        currentQuest = definition;
        runtimeObjectives.Clear();

        foreach (QuestObjectiveData objective in definition.objectives)
        {
            if (objective == null) continue;

            runtimeObjectives.Add(new QuestObjectiveData
            {
                objectiveType = objective.objectiveType,
                targetId = objective.targetId,
                description = objective.description,
                requiredAmount = objective.requiredAmount,
                currentAmount = 0
            });
        }

        currentStatus = QuestStatus.Active;

        if (questPanel != null)
            questPanel.SetActive(true);

        if (questNameText != null)
            questNameText.text = currentQuest.questName;

        if (questDescriptionText != null)
            questDescriptionText.text = currentQuest.description;

        UpdateQuestUI();
        // Hiển thị chỉ dẫn khi bắt đầu Quest 1.
        if (questId == "quest_1" && questGuideController != null)
        {
            questGuideController.ShowStoneGuide();
        }

        Debug.Log("[Quest] Started: " + currentQuest.questName);
        if (questNotification != null)
        {
            questNotification.ShowNotification(
                "Quest Started: " + currentQuest.questName
            );
        }

        return true;
    }

    private void HandleObjectiveProgress(
        QuestObjectiveType type,
        string targetId,
        int amount)
    {
        if (currentStatus != QuestStatus.Active)
            return;

        bool changed = false;

        foreach (QuestObjectiveData objective in runtimeObjectives)
        {
            if (objective.IsCompleted)
                continue;

            if (objective.objectiveType != type)
                continue;

            if (!string.Equals(
                objective.targetId,
                targetId,
                StringComparison.Ordinal))
                continue;

            objective.AddProgress(amount);
            changed = true;
        }

        if (!changed) return;

        UpdateQuestUI();

        if (AreAllObjectivesCompleted())
            CompleteCurrentQuest();
    }

    private bool AreAllObjectivesCompleted()
    {
        if (runtimeObjectives.Count == 0)
            return false;

        foreach (QuestObjectiveData objective in runtimeObjectives)
        {
            if (!objective.IsCompleted)
                return false;
        }

        return true;
    }



    private void UpdateQuestUI()
    {
        if (questProgressText == null)
            return;

        int completedCount = 0;
        string progress = "";

        foreach (QuestObjectiveData objective in runtimeObjectives)
        {
            bool completed = objective.IsCompleted;

            if (completed)
                completedCount++;

            string objectiveName = string.IsNullOrWhiteSpace(objective.description)
                ? "Unknown Objective"
                : objective.description;

            if (completed)
            {
                progress += "<color=#65D98A>[ DONE ] "
                    + objectiveName + " "
                    + objective.currentAmount + "/"
                    + objective.requiredAmount
                    + "</color>";
            }
            else
            {
                progress += "<color=#FFFFFF>[ ] "
                    + objectiveName + " "
                    + objective.currentAmount + "/"
                    + objective.requiredAmount
                    + "</color>";
            }

            progress += "\n";
        }

        questProgressText.richText = true;
        questProgressText.text = progress;
    }


    private void CompleteCurrentQuest()
    {
        if (currentStatus != QuestStatus.Active)
            return;

        currentStatus = QuestStatus.Completed;

        Debug.Log(
            "[Quest] Completed: " + currentQuest.questName
        );
        if (questNotification != null)
        {
            questNotification.ShowNotification(
                "Quest Completed: " + currentQuest.questName
            );
        }

        if (currentQuest.questId == "quest_1")
        {
            // Quest 1 reward: unlock Basic Workbench and Quest 2.
            quest2Unlocked = true;

            Debug.Log("[Quest] Quest 2 Unlocked: Stone of Sight");
            Debug.Log("[Quest] Reward: Unlock Basic Workbench");
        }

        if (questPanel != null)
            questPanel.SetActive(false);
    }

    // Giữ tương thích với GuideNPCDialogue cũ.
    public void UnlockQuest2()
    {
        if (currentStatus != QuestStatus.Completed ||
            currentQuest == null ||
            currentQuest.questId != "quest_1")
        {
            Debug.LogWarning("[Quest] Quest 1 chưa hoàn thành.");
            return;
        }

        if (quest2Unlocked)
        {
            Debug.Log("[Quest] Quest 2 đã được mở khóa.");
            return;
        }

        quest2Unlocked = true;

        Debug.Log("[Quest] Quest 2 Unlocked: Stone of Sight");
    }
}
