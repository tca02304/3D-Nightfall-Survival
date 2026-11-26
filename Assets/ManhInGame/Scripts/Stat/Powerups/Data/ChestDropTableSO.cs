using System;
using UnityEngine;

namespace Nightfall.Stats
{
    [Serializable]
    public struct RarityWeight
    {
        public PowerupRarity rarity;
        [Min(0)] public float weight;
        // Create a non-negative weighting used by the chest data roller.
        public RarityWeight(PowerupRarity rarity, float weight) { this.rarity = rarity; this.weight = weight; }
    }

    [CreateAssetMenu(fileName = "ChestDrop_", menuName = "Nightfall/Stat/Chest Drop Table")]
    public sealed class ChestDropTableSO : ScriptableObject
    {
        [SerializeField] private ChestTier tier;
        [SerializeField, Min(0)] private int goldCost;
        [SerializeField] private PowerupCatalogSO powerupCatalog;
        [SerializeField] private RarityWeight[] rarityWeights = Array.Empty<RarityWeight>();
        public ChestTier Tier => tier;
        public int ChestNumber => tier == ChestTier.Basic ? 1 : (int)tier + 2;
        public int GoldCost => goldCost;
        public PowerupCatalogSO Catalog => powerupCatalog;
        public RarityWeight[] RarityWeights => rarityWeights;

        // Populate the chest asset with approved GDD rarity probabilities.
        public void Configure(ChestTier chestTier, int cost, PowerupCatalogSO catalog, RarityWeight[] weights)
        {
            tier = chestTier;
            goldCost = Mathf.Max(0, cost);
            powerupCatalog = catalog;
            rarityWeights = weights ?? Array.Empty<RarityWeight>();
        }
    }
}
