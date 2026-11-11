
using System.Collections.Generic;
using UnityEngine;

namespace Nightfall.Stats
{
    public static class StatCalculator
    {
        /// <summary>
        /// Calculates the final stat value after applying modifiers.
        ///
        /// Order:
        /// 1. Base + Flat
        /// 2. Additive percentages
        /// 3. Multiplicative modifiers
        /// 4. Stat-specific limits
        /// </summary>
        public static float Calculate(
            float baseValue,
            StatType stat,
            IEnumerable<StatModifier> modifiers)
        {
            float flat = 0f;
            float additivePercent = 0f;
            float multiplier = 1f;

            if (modifiers != null)
            {
                foreach (StatModifier mod in modifiers)
                {
                    // Only apply modifiers targeting this stat.
                    if (mod.statType != stat)
                        continue;

                    // Ignore invalid numeric data.
                    if (float.IsNaN(mod.value) ||
                        float.IsInfinity(mod.value))
                    {
                        Debug.LogWarning(
                            $"[StatCalculator] Invalid modifier " +
                            $"for {stat}, source={mod.sourceId}");

                        continue;
                    }

                    switch (mod.modifierType)
                    {
                        case StatModifierType.Flat:
                            flat += mod.value;
                            break;

                        case StatModifierType.AdditivePercent:
                            additivePercent += mod.value;
                            break;

                        case StatModifierType.Multiplier:
                            multiplier *= mod.value;
                            break;
                    }
                }
            }

            // Step 1: Add flat bonuses.
            float result = baseValue + flat;

            // Step 2: Apply additive percent bonuses.
            result *= Mathf.Max(0f, 1f + additivePercent);

            // Step 3: Apply multiplicative bonuses.
            result *= Mathf.Max(0f, multiplier);

            // Step 4: Apply valid limits for this stat.
            return ClampStat(stat, result);
        }

        /// <summary>
        /// Diminishing-return stacking:
        ///
        /// C(n) = maxValue * (1 - exp(-n * scaleSpeed))
        ///
        /// Supports negative maxValue for reduction effects.
        /// </summary>
        public static float Cumulative(
            int stacks,
            float scaleSpeed,
            float maxValue)
        {
            if (stacks <= 0)
                return 0f;

            if (scaleSpeed <= 0f ||
                float.IsNaN(scaleSpeed) ||
                float.IsInfinity(scaleSpeed) ||
                float.IsNaN(maxValue) ||
                float.IsInfinity(maxValue))
            {
                return 0f;
            }

            return maxValue *
                   (1f - Mathf.Exp(-stacks * scaleSpeed));
        }

        /// <summary>
        /// Applies stat-specific bounds.
        /// </summary>
        private static float ClampStat(
            StatType stat,
            float value)
        {
            if (float.IsNaN(value))
                return 0f;

            if (float.IsPositiveInfinity(value))
                value = float.MaxValue;

            if (float.IsNegativeInfinity(value))
                return 0f;

            switch (stat)
            {
                // Probabilities must remain within [0, 1].
                case StatType.CriticalChance:
                case StatType.KnockbackChance:
                case StatType.LightningChance:
                case StatType.ChunkyChance:
                case StatType.DamageReduction:
                    return Mathf.Clamp01(value);

                // Other stats are non-negative, but do not
                // have a universal maximum of 100%.
                default:
                    return Mathf.Max(0f, value);
            }
        }
    }
}
