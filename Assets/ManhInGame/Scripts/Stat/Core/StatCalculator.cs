using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nightfall.Stats
{
    public static class StatCalculator
    {
        // Recalculate one stat from its immutable base and the current modifier set.
        public static float Calculate(float baseValue, StatType stat, IEnumerable<StatModifier> modifiers)
        {
            float flat = 0f;
            float additive = 0f;
            double multiplicative = 1d;
            if (modifiers != null)
            {
                foreach (StatModifier m in modifiers)
                {
                    if (m.stat != stat || float.IsNaN(m.value) || float.IsInfinity(m.value)) continue;
                    switch (m.mode)
                    {
                        case StatModifierMode.Flat: flat += m.value; break;
                        case StatModifierMode.AdditivePercent: additive += m.value; break;
                        case StatModifierMode.MultiplicativePercent: multiplicative *= 1d + m.value; break;
                    }
                }
            }
            double result = (baseValue + flat) * (1d + additive) * multiplicative;
            if (double.IsNaN(result) || double.IsInfinity(result)) return baseValue;
            return ClampByStat(stat, (float)Math.Max(-float.MaxValue, Math.Min(float.MaxValue, result)));
        }

        // Apply minimum and maximum restrictions to bounded player statistics.
        public static float ClampByStat(StatType stat, float value)
        {
            switch (stat)
            {
                case StatType.CriticalChance:
                case StatType.KnockbackChance:
                case StatType.LightningChance:
                case StatType.ChunkyChance: return Mathf.Clamp01(value);
                case StatType.LifeSteal: return Mathf.Clamp(value, 0f, .5f);
                case StatType.StaminaCostMultiplier:
                case StatType.HungerDrainMultiplier: return Mathf.Max(.1f, value);
                case StatType.ExtraJumps: return Mathf.Max(0f, Mathf.Floor(value));
                case StatType.LootMultiplier:
                case StatType.MoveSpeed:
                case StatType.AttackSpeed:
                case StatType.JumpPower:
                case StatType.CriticalDamage: return Mathf.Max(0f, value);
                default: return Mathf.Max(0f, value);
            }
        }
    }
}
