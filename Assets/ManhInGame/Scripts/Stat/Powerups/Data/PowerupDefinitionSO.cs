
using System;
using UnityEngine;
using Nightfall.Stats;

namespace Nightfall.Powerups
{
    public enum PowerupRarity
    {
        Common,
        Rare,
        Legendary
    }

    public enum PowerupTrigger
    {
        Passive,
        OnHit,
        OnCriticalHit,
        OnDamageDealt,
        OnKill,
        OnHealthChanged,
        WhileFalling,
        WhileMoving
    }

    public enum PowerupStackFormula
    {
        Linear,
        Cumulative
    }

    [Serializable]
    public class PowerupEffect
    {
        public StatType statType;
        public StatModifierType modifierType;
        public PowerupTrigger trigger;
        public PowerupStackFormula stackFormula;

        public float baseValue;
        public float scaleSpeed;
        public float maxValue;

        public float duration;
        public float cooldown;
        public float healthThreshold;
    }

    [CreateAssetMenu(
        fileName = "NewPowerup",
        menuName = "Nightfall/Stats/Powerup")]
    public sealed class PowerupDefinitionSO :
        ScriptableObject
    {
        public string powerupId;
        public string displayName;

        [TextArea]
        public string description;

        public PowerupRarity rarity;
        public Sprite icon;

        [Min(1)]
        public int maxStacks = 999;

        public PowerupEffect[] effects;
    }
}
