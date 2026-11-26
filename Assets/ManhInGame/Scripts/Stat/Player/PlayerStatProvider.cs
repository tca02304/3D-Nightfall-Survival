using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nightfall.Stats
{
    public struct AttackRoll
    {
        public float damage;
        public bool critical;
        public bool lightning;
        public bool chunky;
        public bool knockback;
    }

    [Serializable]
    public sealed class PlayerStatSnapshot
    {
        public List<PowerupStackRecord> powerups = new List<PowerupStackRecord>();
        public List<TimedBuffRecord> buffs = new List<TimedBuffRecord>();
        public float killHealthBonus;
    }

    [DisallowMultipleComponent]
    public sealed class PlayerStatProvider : MonoBehaviour
    {
        [SerializeField] private PowerupCatalogSO catalog;
        [SerializeField] private List<BaseStatEntry> baseStats = new List<BaseStatEntry>
        {
            new BaseStatEntry(StatType.MaxHealth,100f), new BaseStatEntry(StatType.MaxStamina,100f),
            new BaseStatEntry(StatType.MaxHunger,100f), new BaseStatEntry(StatType.Attack,10f),
            new BaseStatEntry(StatType.Defense,0f), new BaseStatEntry(StatType.MoveSpeed,1f),
            new BaseStatEntry(StatType.AttackSpeed,1f), new BaseStatEntry(StatType.CriticalChance,.05f),
            new BaseStatEntry(StatType.CriticalDamage,1.5f), new BaseStatEntry(StatType.LootMultiplier,1f)
        };
        private PowerupRuntime runtime;
        private readonly TimedBuffRuntime buffs = new TimedBuffRuntime();
        private readonly List<StatModifier> equipmentModifiers = new List<StatModifier>();
        private readonly System.Random rng = new System.Random(Guid.NewGuid().GetHashCode());
        private ItemData heldItem;
        private float health01 = 1f, movement01, killHealthBonus;
        private bool falling;
        public PowerupRuntime Powerups => runtime;
        public TimedBuffRuntime Buffs => buffs;
        public ItemData HeldItem => heldItem;
        public event Action StatsChanged;
        public event Action<float> HealRequested;

        // Initialize only data state; never replace the existing Player controller or health component.
        private void Awake()
        {
            if (catalog == null) Debug.LogError("[PlayerStatProvider] Assign the migrated PowerupCatalog.",this);
            else { runtime = new PowerupRuntime(catalog); runtime.Changed += NotifyChanged; }
            buffs.Changed += NotifyChanged;
        }

        // Expire temporary buffs in scaled gameplay time.
        private void Update() => buffs.Tick(Time.deltaTime);

        // Release subscriptions when the player is destroyed.
        private void OnDestroy()
        {
            if (runtime != null) runtime.Changed -= NotifyChanged;
            buffs.Changed -= NotifyChanged;
        }

        // Publish a single data change notification to gameplay consumers and HUD.
        private void NotifyChanged() => StatsChanged?.Invoke();

        // Return configured base values and supply defaults for newly introduced stats on old prefabs.
        public float GetBase(StatType stat)
        {
            foreach (var entry in baseStats) if (entry.stat == stat) return entry.value;
            switch (stat)
            {
                case StatType.MaxHealth: case StatType.MaxStamina: case StatType.MaxHunger: return 100f;
                case StatType.Attack: return 10f;
                case StatType.MoveSpeed: case StatType.AttackSpeed: case StatType.LootMultiplier:
                case StatType.JumpPower: case StatType.StaminaCostMultiplier: case StatType.HungerDrainMultiplier:
                case StatType.ResourceDamageMultiplier: return 1f;
                case StatType.CriticalChance: return .05f;
                case StatType.CriticalDamage: case StatType.AttackRange: return 1.5f;
                default: return 0f;
            }
        }

        // Replace the held-item reference instead of cumulatively applying its stats.
        public void SetHeldItem(ItemData item)
        {
            if (ReferenceEquals(heldItem,item)) return;
            heldItem = item;
            NotifyChanged();
        }

        // Replace external equipment modifiers atomically; armor controllers may call this later.
        public void SetEquipmentModifiers(IEnumerable<StatModifier> modifiers)
        {
            var next = modifiers == null ? new List<StatModifier>() : new List<StatModifier>(modifiers);
            equipmentModifiers.Clear(); equipmentModifiers.AddRange(next); NotifyChanged();
        }

        // Supply live HP ratio, normalized movement and falling state from the existing Player systems.
        public void SetCombatContext(float currentHealth, float maxHealth, float normalizedMovement, bool isFalling)
        {
            float h = maxHealth > 0f ? Mathf.Clamp01(currentHealth/maxHealth) : 1f;
            float m = Mathf.Clamp01(normalizedMovement);
            if (Mathf.Approximately(h,health01) && Mathf.Approximately(m,movement01) && falling==isFalling) return;
            health01=h; movement01=m; falling=isFalling; NotifyChanged();
        }

        // Derive held-item modifiers from valid catalog metadata, ignoring unused serialized stat objects.
        private IEnumerable<StatModifier> GetHeldModifiers()
        {
            if (ItemGameplayRules.GetKind(heldItem)==GameplayItemKind.Weapon && heldItem.weaponStat!=null)
            {
                var w=heldItem.weaponStat;
                yield return new StatModifier(StatType.Attack,StatModifierMode.Flat,Mathf.Max(0,w.damage));
                if (w.attackSpeed>0f) yield return new StatModifier(StatType.AttackSpeed,StatModifierMode.MultiplicativePercent,w.attackSpeed-1f);
                if (w.attackRange>0f) yield return new StatModifier(StatType.AttackRange,StatModifierMode.Flat,w.attackRange-GetBase(StatType.AttackRange));
                if (w.criticalChance>0f) yield return new StatModifier(StatType.CriticalChance,StatModifierMode.Flat,w.criticalChance-GetBase(StatType.CriticalChance));
            }
            else if (ItemGameplayRules.GetKind(heldItem)==GameplayItemKind.Tool && heldItem.toolStat!=null)
            {
                var t=heldItem.toolStat;
                yield return new StatModifier(StatType.MiningPower,StatModifierMode.Flat,Mathf.Max(0,t.toolPower));
                yield return new StatModifier(StatType.Attack,StatModifierMode.Flat,Mathf.Max(0,t.toolPower)*.1f);
                if (t.actionSpeed>0f) yield return new StatModifier(StatType.AttackSpeed,StatModifierMode.MultiplicativePercent,t.actionSpeed-1f);
            }
        }

        // Recalculate the final stat from base, held item, equipment, powerups and active timed buffs.
        public float GetStat(StatType stat, IEnumerable<StatModifier> equipment=null, IEnumerable<StatModifier> temporaryBuffs=null)
        {
            var modifiers=new List<StatModifier>(equipmentModifiers);
            modifiers.AddRange(GetHeldModifiers());
            if (equipment!=null) modifiers.AddRange(equipment);
            if (temporaryBuffs!=null) modifiers.AddRange(temporaryBuffs);
            if (runtime!=null) modifiers.AddRange(runtime.GetStatModifiers(health01,movement01,falling));
            modifiers.AddRange(buffs.GetModifiers());
            if (killHealthBonus>0f) modifiers.Add(new StatModifier(StatType.MaxHealth,StatModifierMode.Flat,killHealthBonus));
            return StatCalculator.Calculate(GetBase(stat),stat,modifiers);
        }

        // Grant a catalog powerup exactly once through the authoritative local runtime.
        public bool TryGrantPowerup(PowerupDefinitionSO definition,int amount=1) => definition!=null && runtime!=null && runtime.TryAdd(definition.Code,amount);

        // Guard the resource type and tier before applying any gathering modifiers.
        public float GetGatherDamage(string resourceCode)
        {
            float raw=ItemGameplayRules.GetBaseGatherDamage(heldItem,resourceCode);
            if (raw<=0f) return 0f;
            float power=heldItem.itemCode=="stone" ? raw : GetStat(StatType.MiningPower);
            return Mathf.Max(0f,power*GetStat(StatType.ResourceDamageMultiplier));
        }

        // Roll a resource hit only after tier validation; even rare powerup procs cannot bypass gating.
        public AttackRoll RollGatherHit(string resourceCode)
        {
            float damage=GetGatherDamage(resourceCode);
            return damage>0f ? RollDamage(damage,false) : default;
        }

        // Roll an outgoing attack once; callers must apply its final damage only once.
        public AttackRoll RollCombatHit() => RollDamage(GetStat(StatType.Attack),true);

        // Resolve common critical hits and special damage procs with a local random source.
        private AttackRoll RollDamage(float damage,bool allowCrit)
        {
            var result=new AttackRoll { damage=Mathf.Max(0f,damage) };
            if (damage<=0f) return result;
            result.critical=allowCrit && rng.NextDouble()<GetStat(StatType.CriticalChance);
            result.lightning=allowCrit && rng.NextDouble()<GetStat(StatType.LightningChance);
            result.chunky=rng.NextDouble()<GetStat(StatType.ChunkyChance);
            result.knockback=allowCrit && rng.NextDouble()<GetStat(StatType.KnockbackChance);
            if (result.critical) result.damage*=GetStat(StatType.CriticalDamage);
            if (result.lightning) result.damage+=damage*GetStat(StatType.LightningDamageMultiplier);
            if (result.chunky) result.damage+=damage*GetStat(StatType.ChunkyDamageMultiplier);
            return result;
        }

        // Apply lifesteal and on-crit timed effects after a real hit, not when an attack merely swings.
        public void NotifyHitApplied(AttackRoll roll,float actualDamage)
        {
            if (!(actualDamage>0f) || float.IsInfinity(actualDamage)) return;
            float heal=actualDamage*GetStat(StatType.LifeSteal);
            if (heal>0f) HealRequested?.Invoke(heal);
            if (!roll.critical || runtime==null) return;
            foreach(var effect in runtime.GetTriggeredEffects(PowerupCondition.OnCrit))
                buffs.Apply(effect.code+":"+effect.rule.stat,new StatModifier(effect.rule.stat,effect.rule.mode,effect.value),effect.rule.duration);
        }

        // Apply kill-triggered growth once; invoke only from the authoritative enemy death reward callback.
        public void NotifyEnemyKilled()
        {
            if (runtime==null) return;
            float growth=0f;
            foreach(var effect in runtime.GetTriggeredEffects(PowerupCondition.OnKill))
                if(effect.rule.stat==StatType.MaxHealth) growth+=Mathf.Max(0f,effect.value);
            if(growth>0f) { killHealthBonus+=growth; NotifyChanged(); }
        }

        // Calculate damage after defense using a configurable conventional diminishing-return armor formula.
        public float GetDamageAfterDefense(float incomingDamage)
        {
            if (!(incomingDamage > 0f) || float.IsInfinity(incomingDamage)) return 0f;
            return incomingDamage * (100f / (100f + GetStat(StatType.Defense)));
        }

        // Consume one selected food only after the caller has decided that consumption is valid.
        public bool TryConsumeSelected(out int heal,out int stamina)
        {
            heal=0; stamina=0;
            var hotbar=HotbarManager.Instance; var inv=InventoryManager.Instance;
            if(hotbar==null || inv==null) return false;
            var item=hotbar.GetSelectedItem();
            if(ItemGameplayRules.GetKind(item)!=GameplayItemKind.Consumable || item.consumableStat==null) return false;
            int h=Mathf.Max(0,item.consumableStat.healHP), st=Mathf.Max(0,item.consumableStat.restoreStamina);
            if(h==0 && st==0) return false;
            if(!inv.RemoveItemAt(hotbar.SelectedInventoryIndex,1)) return false;
            heal=h; stamina=st; return true;
        }

        // Export persistent player progress without storing derived stats or mutating the existing Inventory save.
        public PlayerStatSnapshot Capture() => new PlayerStatSnapshot { powerups=runtime!=null?runtime.Capture():new List<PowerupStackRecord>(), buffs=buffs.Capture(),killHealthBonus=killHealthBonus };

        // Validate a snapshot using temporary runtimes before applying the complete player data.
        public bool TryRestore(PlayerStatSnapshot record)
        {
            if(record==null || runtime==null || !(record.killHealthBonus>=0f) || float.IsInfinity(record.killHealthBonus)) return false;
            var testPowerups=new PowerupRuntime(runtime.Catalog); var testBuffs=new TimedBuffRuntime();
            if(!testPowerups.TryRestore(record.powerups) || !testBuffs.TryRestore(record.buffs)) return false;
            killHealthBonus=record.killHealthBonus;
            runtime.TryRestore(record.powerups); buffs.TryRestore(record.buffs); return true;
        }
    }
}
