
using System;
using UnityEngine;

[Serializable]
public class QuestObjectiveData
{
    [Header("Objective Information")]
    public QuestObjectiveType objectiveType;

    [Tooltip("ID của Item, Boss, Biome hoặc Building")]
    public string targetId;

    [TextArea]
    public string description;

    [Header("Progress")]
    [Min(1)]
    public int requiredAmount = 1;

    [HideInInspector]
    public int currentAmount;

    public bool IsCompleted =>
        currentAmount >= requiredAmount;

    public void AddProgress(int amount)
    {
        if (IsCompleted || amount <= 0)
            return;

        currentAmount = Mathf.Min(
            currentAmount + amount,
            requiredAmount
        );
    }

    public void ResetProgress()
    {
        currentAmount = 0;
    }
}
