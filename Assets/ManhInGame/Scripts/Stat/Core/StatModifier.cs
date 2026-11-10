
using System;

namespace Nightfall.Stats
{
    public enum StatModifierType
    {
        Flat,           // +10 HP
        AdditivePercent,// +10% gia tri goc
        Multiplier      // x1.5
    }

    [Serializable]
    public struct StatModifier
    {
        public StatType statType;
        public StatModifierType modifierType;
        public float value;
        public string sourceId;
    }
}
