namespace Nightfall.Stats
{
    // Existing serialized enum values 0..14 are preserved. New values are append-only.
    public enum StatType
    {
        MaxHealth, MaxStamina, MaxHunger, Attack, Defense,
        MoveSpeed, AttackSpeed, CriticalChance, CriticalDamage,
        MiningPower, LootMultiplier, HealthRegen, StaminaRegen,
        LifeSteal, JumpPower, MaxShield, StaminaCostMultiplier,
        HungerDrainMultiplier, ExtraJumps, KnockbackChance,
        LightningChance, LightningDamageMultiplier, ChunkyChance,
        ChunkyDamageMultiplier, AttackRange, ResourceDamageMultiplier
    }
    public enum StatModifierMode { Flat, AdditivePercent, MultiplicativePercent }
    public enum PowerupRarity { Common, Uncommon, Rare, Epic, Legendary, Mythic }
    public enum PowerupEffectKind { StatModifier, LowHealthDamage, CriticalProc, LightningProc, KillHealthGrowth }
    // Basic is appended to preserve existing Wooden/Iron/Crystal/Aether assets.
    public enum ChestTier { Wooden, Iron, Crystal, Aether, Basic }
    public enum PowerupCondition { Passive, MissingHealth, LowHealth, Moving, Falling, OnCrit, OnKill }
    public enum PowerupGrowth { Linear, Saturating }
}
