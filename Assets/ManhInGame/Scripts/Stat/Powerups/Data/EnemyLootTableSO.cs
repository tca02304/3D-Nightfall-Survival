using System;
using UnityEngine;

namespace Nightfall.Stats
{
    [Serializable]
    public struct EnemyLootEntry
    {
        public string itemCode;
        [Range(0f,100f)] public float chancePercent;
        [Min(1)] public int minAmount;
        [Min(1)] public int maxAmount;
    }

    [CreateAssetMenu(fileName="EnemyLoot_",menuName="Nightfall/Stat/Enemy Loot Table")]
    public sealed class EnemyLootTableSO : ScriptableObject
    {
        [SerializeField] private string enemyCode;
        [SerializeField,Min(0)] private int minGold;
        [SerializeField,Min(0)] private int maxGold;
        [SerializeField] private EnemyLootEntry[] drops=Array.Empty<EnemyLootEntry>();
        [SerializeField] private ChestDropTableSO bossPowerupTable;
        public string EnemyCode => enemyCode;
        public int MinGold => minGold;
        public int MaxGold => maxGold;
        public EnemyLootEntry[] Drops => drops;
        public ChestDropTableSO BossPowerupTable => bossPowerupTable;
    }
}
