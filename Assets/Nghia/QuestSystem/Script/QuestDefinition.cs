
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "NewQuest",
    menuName = "Nightfall/Quest Definition"
)]
public class QuestDefinition : ScriptableObject
{
    [Header("Quest Information")]
    public string questId;
    public string questName;

    [TextArea(2, 5)]
    public string description;

    [Header("Quest Objectives")]
    public List<QuestObjectiveData> objectives = new();

    [Header("Quest Rewards")]
    [TextArea(2, 5)]
    public string rewardDescription;
}
