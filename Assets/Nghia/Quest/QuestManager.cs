using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    [Header("Quest Objectives")]
    [SerializeField]
    private QuestObjective[] objectives;

    private int currentObjectiveIndex;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        QuestEventSystem.OnProgress += UpdateProgress;
    }

    private void OnDisable()
    {
        QuestEventSystem.OnProgress -= UpdateProgress;
    }

    private void Start()
    {
        StartQuest();
    }

    private void StartQuest()
    {
        if (objectives == null || objectives.Length == 0)
        {
            Debug.LogWarning("No quest objectives assigned!");
            return;
        }

        currentObjectiveIndex = 0;

        for (int i = 0; i < objectives.Length; i++)
        {
            objectives[i].isActive = false;
            objectives[i].currentAmount = 0;
        }

        objectives[0].isActive = true;

        ShowCurrentObjective();
    }

    private void UpdateProgress(string targetId, int amount)
    {
        if (currentObjectiveIndex >= objectives.Length)
            return;

        QuestObjective currentObjective = objectives[currentObjectiveIndex];

        if (!currentObjective.isActive)
            return;

        if (currentObjective.targetId != targetId)
            return;

        currentObjective.AddProgress(amount);

        Debug.Log(
            currentObjective.targetId +
            ": " +
            currentObjective.currentAmount +
            "/" +
            currentObjective.requiredAmount
        );

        if (currentObjective.IsCompleted)
        {
            CompleteCurrentObjective();
        }
    }

    private void CompleteCurrentObjective()
    {
        QuestObjective completedObjective =
            objectives[currentObjectiveIndex];

        Debug.Log(
            "Objective Complete: " +
            completedObjective.targetId
        );

        completedObjective.isActive = false;

        currentObjectiveIndex++;

        if (currentObjectiveIndex >= objectives.Length)
        {
            CompleteQuest();
            return;
        }

        objectives[currentObjectiveIndex].isActive = true;

        ShowCurrentObjective();
    }

    private void ShowCurrentObjective()
    {
        QuestObjective objective =
            objectives[currentObjectiveIndex];

        Debug.Log(
            "Current Objective: " +
            objective.targetId +
            " " +
            objective.currentAmount +
            "/" +
            objective.requiredAmount
        );
    }

    private void CompleteQuest()
    {
        Debug.Log("QUEST COMPLETE!");
    }
}