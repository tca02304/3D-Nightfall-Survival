using System;
using System.Collections.Generic;

namespace Nightfall.Stats
{
    public static class ChestLootRoller
    {
        // Select a rarity from nonnegative relative weights; sum need not equal 100.
        public static PowerupRarity RollRarity(IReadOnlyList<RarityWeight> weights, Random rng)
        {
            if (weights == null || weights.Count == 0) throw new ArgumentException("Chest rarity table is empty.");
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            double total = 0d;
            foreach (RarityWeight entry in weights)
            {
                if (float.IsNaN(entry.weight) || float.IsInfinity(entry.weight) || entry.weight < 0f)
                    throw new ArgumentException("Invalid rarity weight.");
                total += entry.weight;
            }
            if (total <= 0d) throw new ArgumentException("Chest rarity total is zero.");
            double target = rng.NextDouble() * total;
            double sum = 0d;
            foreach (RarityWeight entry in weights)
            {
                if (entry.weight <= 0f) continue;
                sum += entry.weight;
                if (target < sum) return entry.rarity;
            }
            throw new InvalidOperationException("Rarity roll was not resolved.");
        }

        // Roll one actual powerup after excluding empty rarities and maxed-out stacks.
        public static PowerupDefinitionSO RollPowerup(ChestDropTableSO table, Random rng, PowerupRuntime runtime = null)
        {
            if (table == null || table.Catalog == null) throw new ArgumentException("Chest table/catalog missing.");
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            // Validate GDD tables before filtering capped or unavailable rewards.
            var seen = new HashSet<PowerupRarity>();
            double configuredTotal = 0d;
            if (table.RarityWeights == null || table.RarityWeights.Length != 6)
                throw new ArgumentException("Expected six rarity weights.");
            foreach (var entry in table.RarityWeights)
            {
                if (!Enum.IsDefined(typeof(PowerupRarity), entry.rarity) || !seen.Add(entry.rarity) ||
                    float.IsNaN(entry.weight) || float.IsInfinity(entry.weight) || entry.weight < 0f)
                    throw new ArgumentException("Invalid or duplicate chest rarity.");
                configuredTotal += entry.weight;
            }
            if (Math.Abs(configuredTotal - 100d) > .001d) throw new ArgumentException("Chest percentages must add to 100.");
            var eligible = new Dictionary<PowerupRarity, List<PowerupDefinitionSO>>();
            var seenCodes = new HashSet<string>(StringComparer.Ordinal);
            foreach (PowerupDefinitionSO definition in table.Catalog.Definitions)
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.Code) ||
                    definition.DropWeight <= 0f || float.IsNaN(definition.DropWeight) || float.IsInfinity(definition.DropWeight) ||
                    (runtime != null && (!runtime.CanAdd(definition.Code) || runtime.Catalog.Find(definition.Code) != definition))) continue;
                if (!seenCodes.Add(definition.Code)) throw new ArgumentException("Duplicate PowerupCode: " + definition.Code);
                if (!eligible.TryGetValue(definition.Rarity, out var list))
                {
                    list = new List<PowerupDefinitionSO>();
                    eligible.Add(definition.Rarity, list);
                }
                list.Add(definition);
            }
            var availableWeights = new List<RarityWeight>();
            foreach (RarityWeight entry in table.RarityWeights)
                if (entry.weight > 0f && eligible.ContainsKey(entry.rarity)) availableWeights.Add(entry);
            if (availableWeights.Count == 0) return null;
            PowerupRarity rarity = RollRarity(availableWeights, rng);
            List<PowerupDefinitionSO> pool = eligible[rarity];
            double total = 0;
            foreach (PowerupDefinitionSO item in pool) total += item.DropWeight;
            double roll = rng.NextDouble() * total;
            double cursor = 0;
            foreach (PowerupDefinitionSO item in pool)
            {
                cursor += item.DropWeight;
                if (roll < cursor) return item;
            }
            return pool[pool.Count - 1];
        }
    }
}
