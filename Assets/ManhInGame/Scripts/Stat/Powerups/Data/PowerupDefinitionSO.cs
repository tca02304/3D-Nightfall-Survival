using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Nightfall.Stats
{
    [CreateAssetMenu(fileName = "Powerup_", menuName = "Nightfall/Stat/Powerup Definition")]
    public sealed class PowerupDefinitionSO : ScriptableObject
    {
        [FormerlySerializedAs("powerupId")]
        [SerializeField] private string powerupCode;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField] private PowerupRarity rarity;
        [SerializeField, Min(0f)] private float dropWeight = 100f;
        [SerializeField, Min(1)] private int maxStacks = 20;
        [SerializeField] private PowerupEffectKind effectKind = PowerupEffectKind.StatModifier;
        [SerializeField] private StatModifier[] modifiersPerStack = Array.Empty<StatModifier>();
        [SerializeField, TextArea] private string description;
        [SerializeField] private PowerupEffectRule[] runtimeEffects = Array.Empty<PowerupEffectRule>();

        public string Code => powerupCode;
        public string PowerupId => powerupCode;
        public PowerupEffectRule[] RuntimeEffects => runtimeEffects ?? Array.Empty<PowerupEffectRule>();
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public PowerupRarity Rarity => rarity;
        public float DropWeight => dropWeight;
        public int MaxStacks => maxStacks;
        public PowerupEffectKind EffectKind => effectKind;
        public StatModifier[] ModifiersPerStack => modifiersPerStack ?? Array.Empty<StatModifier>();
        public string Description => description;

        // Populate a new definition from a tooling or balance-data import pipeline.
        public void Configure(string code, string title, PowerupRarity rarityValue, float weight,
            int stacks, PowerupEffectKind kind, StatModifier[] modifiers, string details)
        {
            powerupCode = code;
            displayName = title;
            rarity = rarityValue;
            dropWeight = Mathf.Max(0f, weight);
            maxStacks = Mathf.Max(1, stacks);
            effectKind = kind;
            modifiersPerStack = modifiers ?? Array.Empty<StatModifier>();
            description = details;
            runtimeEffects = Array.Empty<PowerupEffectRule>();
        }
        // Configure the complete data-driven effects without replacing this asset or its icon reference.
        public void ConfigureRuntimeEffects(PowerupEffectRule[] rules, Sprite newIcon = null)
        {
            runtimeEffects = rules ?? Array.Empty<PowerupEffectRule>();
            if (newIcon != null) icon = newIcon;
        }
    }
}
