
using System.Collections.Generic;
using UnityEngine;

namespace Nightfall.Stats
{
    public static class StatCalculator
    {
        public static float Calculate(
            float baseValue,
            StatType stat,
            IEnumerable<StatModifier> modifiers)
        {
            float flat = 0f;
            float additivePercent = 0f;
            float multiplier = 1f;

            foreach (StatModifier mod in modifiers)
            {
                if (mod.statType != stat)
                    continue;

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

            float result =
                (baseValue + flat) *
                (1f + additivePercent) *
                multiplier;

            return Mathf.Max(0f, result);
        }

        public static float Cumulative(
            int stacks,
            float scaleSpeed,
            float maxValue)
        {
            if (stacks <= 0)
                return 0f;

            return
                (1f - Mathf.Exp(-stacks * scaleSpeed)) *
                maxValue;
        }
    }
}
