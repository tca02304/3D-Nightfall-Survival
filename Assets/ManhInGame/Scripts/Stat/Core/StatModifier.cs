using System;
using UnityEngine;

namespace Nightfall.Stats
{
    [Serializable]
    public struct StatModifier
    {
        public StatType stat;
        public StatModifierMode mode;
        [Tooltip("Flat=absolute; AdditivePercent=0.1 means +10%; MultiplicativePercent=0.1 means x1.1")]
        public float value;

        // Create one data-only modifier without altering a player component.
        public StatModifier(StatType stat, StatModifierMode mode, float value)
        {
            this.stat = stat;
            this.mode = mode;
            this.value = value;
        }
    }

    [Serializable]
    public struct BaseStatEntry
    {
        public StatType stat;
        public float value;

        // Create a base-stat entry for the calculator.
        public BaseStatEntry(StatType stat, float value)
        {
            this.stat = stat;
            this.value = value;
        }
    }
}
