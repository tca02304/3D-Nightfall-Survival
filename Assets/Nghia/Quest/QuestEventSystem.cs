using System;

public static class QuestEventSystem
{
    public static event Action<string, int> OnProgress;

    public static void RaiseProgress(string targetId, int amount = 1)
    {
        OnProgress?.Invoke(targetId, amount);
    }
}