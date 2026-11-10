using System;
using System.Collections.Generic;
using UnityEngine;
using Nightfall.Stats;

namespace Nightfall.Powerups
{
    [Serializable]
    public class OwnedPowerup
    {
        public PowerupDefinitionSO definition;
        public int stacks;
    }

    public sealed class PowerupRuntime : MonoBehaviour
    {
        [Header("Available Powerups")]
        [SerializeField]
        private List<PowerupDefinitionSO> powerupCatalog = new();

        [Header("Runtime Powerups")]
        [SerializeField]
        private List<OwnedPowerup> ownedPowerups = new();

        private readonly Dictionary<StatType, float>
            baseStats = new();

        private readonly List<StatModifier>
            activeModifiers = new();

        public event Action OnPowerupsChanged;

        private void Awake()
        {
            InitializeBaseStats();
            RebuildModifiers();
        }

        private void InitializeBaseStats()
        {
            // Gia tri tam de test Stats Core.
            // Sau nay base stats do Player module cung cap.
            baseStats[StatType.MaxHealth] = 100f;
            baseStats[StatType.AttackDamage] = 25f;
            baseStats[StatType.MoveSpeed] = 5f;
            baseStats[StatType.AttackSpeed] = 1f;
            baseStats[StatType.HealthRegen] = 0f;
            baseStats[StatType.MaxStamina] = 100f;
        }


        [ContextMenu("Powerups/Validate Catalog")]
        public void ValidateCatalog()
        {
            HashSet<string> registeredIds =
                new HashSet<string>(StringComparer.Ordinal);

            int validCount = 0;
            int errorCount = 0;

            for (int i = 0; i < powerupCatalog.Count; i++)
            {
                PowerupDefinitionSO definition = powerupCatalog[i];

                if (definition == null)
                {
                    Debug.LogError(
                        $"[PowerupCatalog] Element {i} is NULL.",
                        this);

                    errorCount++;
                    continue;
                }

                string id = definition.powerupId;

                if (string.IsNullOrWhiteSpace(id))
                {
                    Debug.LogError(
                        $"[PowerupCatalog] {definition.name} has empty ID.",
                        definition);

                    errorCount++;
                    continue;
                }

                if (!registeredIds.Add(id))
                {
                    Debug.LogError(
                        $"[PowerupCatalog] Duplicate ID: {id}",
                        definition);

                    errorCount++;
                    continue;
                }

                if (definition.maxStacks < 1)
                {
                    Debug.LogError(
                        $"[PowerupCatalog] Invalid MaxStacks: {id}",
                        definition);

                    errorCount++;
                    continue;
                }

                if (definition.effects == null ||
                    definition.effects.Length == 0)
                {
                    Debug.LogWarning(
                        $"[PowerupCatalog] {id} has no effects.",
                        definition);
                }

                validCount++;
            }

            Debug.Log(
                $"[PowerupCatalog] Validation completed. " +
                $"Valid={validCount}, Errors={errorCount}, " +
                $"Total={powerupCatalog.Count}",
                this);
        }


        public bool AddPowerup(string powerupId, int amount = 1)
        {
            if (string.IsNullOrWhiteSpace(powerupId) ||
                amount <= 0)
                return false;

            PowerupDefinitionSO definition =
                powerupCatalog.Find(
                    p => p != null &&
                    p.powerupId == powerupId);

            if (definition == null)
            {
                Debug.LogWarning(
                    $"[PowerupRuntime] Unknown Powerup: {powerupId}");
                return false;
            }

            OwnedPowerup owned = ownedPowerups.Find(
                p => p != null && p.definition == definition);

            if (owned == null)
            {
                owned = new OwnedPowerup
                {
                    definition = definition,
                    stacks = 0
                };

                ownedPowerups.Add(owned);
            }

            int maxStacks = Mathf.Max(1, definition.maxStacks);
            int available = maxStacks - owned.stacks;

            if (available <= 0)
                return false;

            int added = Mathf.Min(amount, available);
            owned.stacks += added;

            RebuildModifiers();
            OnPowerupsChanged?.Invoke();

            Debug.Log(
                $"[PowerupRuntime] {definition.displayName} " +
                $"x{owned.stacks}");

            return true;
        }

        public int GetStacks(string powerupId)
        {
            OwnedPowerup owned = ownedPowerups.Find(
                p => p != null &&
                p.definition != null &&
                p.definition.powerupId == powerupId);

            return owned != null ? owned.stacks : 0;
        }

        public float GetStat(StatType stat)
        {
            float baseValue = baseStats.TryGetValue(
                stat, out float value) ? value : 0f;

            return StatCalculator.Calculate(
                baseValue,
                stat,
                activeModifiers);
        }

        public void SetBaseStat(StatType stat, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return;

            baseStats[stat] = value;
            OnPowerupsChanged?.Invoke();
        }

        private void RebuildModifiers()
        {
            activeModifiers.Clear();

            foreach (OwnedPowerup owned in ownedPowerups)
            {
                if (owned == null ||
                    owned.definition == null ||
                    owned.stacks <= 0)
                    continue;

                PowerupDefinitionSO definition =
                    owned.definition;

                if (definition.effects == null)
                    continue;

                foreach (PowerupEffect effect
                         in definition.effects)
                {
                    if (effect.trigger != PowerupTrigger.Passive)
                        continue;

                    float bonus =
                        CalculateEffectValue(effect, owned.stacks);

                    activeModifiers.Add(new StatModifier
                    {
                        statType = effect.statType,
                        modifierType = effect.modifierType,
                        value = bonus,
                        sourceId = definition.powerupId
                    });
                }
            }
        }

        private static float CalculateEffectValue(
            PowerupEffect effect,
            int stacks)
        {
            if (effect.stackFormula ==
                PowerupStackFormula.Linear)
            {
                return effect.baseValue * stacks;
            }

            return StatCalculator.Cumulative(
                stacks,
                effect.scaleSpeed,
                effect.maxValue);
        }
    }
}
