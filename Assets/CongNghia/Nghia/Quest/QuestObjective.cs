using System;

[Serializable]
public class QuestObjective
{
    public QuestObjectiveType type;

    public string targetId;

    public int requiredAmount;

    public int currentAmount;

    public bool isActive;

    public bool IsCompleted
    {
        get
        {
            return currentAmount >= requiredAmount;
        }
    }

    public void AddProgress(int amount)
    {
        if (!isActive || IsCompleted)
            return;

        currentAmount += amount;

        if (currentAmount > requiredAmount)
            currentAmount = requiredAmount;
    }
}