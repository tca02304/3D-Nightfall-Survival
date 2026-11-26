using System;
using UnityEngine;

namespace Nightfall.Stats
{
    [Serializable]
    public struct PowerupEffectRule
    {
        public StatType stat;
        public StatModifierMode mode;
        public PowerupCondition condition;
        public PowerupGrowth growth;
        public float valuePerStack;
        [Min(0f)] public float limit;
        [Min(0f)] public float growthRate;
        [Min(0f)] public float duration;
        [Range(0f, 1f)] public float healthThreshold;

        // Calculate a bounded linear or saturating magnitude for the current number of stacks.
        public float Evaluate(int count)
        {
            if (count <= 0) return 0f;
            if (growth == PowerupGrowth.Saturating)
                return Mathf.Sign(valuePerStack) * limit * (1f - Mathf.Exp(-growthRate * count));
            float value = valuePerStack * count;
            return limit > 0f ? Mathf.Clamp(value, -limit, limit) : value;
        }
    }
}
