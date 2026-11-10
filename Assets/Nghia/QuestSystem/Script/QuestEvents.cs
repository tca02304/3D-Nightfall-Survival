
using System;

public static class QuestEvents
{
    public static event Action<QuestObjectiveType, string, int> OnObjectiveProgress;

    public static void Raise(
        QuestObjectiveType objectiveType,
        string targetId,
        int amount = 1)
    {
        if (amount <= 0) return;

        OnObjectiveProgress?.Invoke(
            objectiveType,
            targetId,
            amount
        );
    }
}
