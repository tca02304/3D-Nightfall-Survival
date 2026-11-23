using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nightfall.Stats
{
    [Serializable]
    public struct PowerupStackRecord
    {
        public string code;
        public int stacks;
        // Construct a stable save record.
        public PowerupStackRecord(string code, int stacks) { this.code = code; this.stacks = stacks; }
    }

    public sealed class PowerupRuntime
    {
        private readonly PowerupCatalogSO catalog;
        private readonly Dictionary<string, int> stacks = new Dictionary<string, int>();
        public event Action Changed;
        public PowerupCatalogSO Catalog => catalog;

        // Initialize one independent player runtime from shared read-only definitions.
        public PowerupRuntime(PowerupCatalogSO catalog) { this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog)); }

        // Return the number of stacks for the given persistent code.
        public int GetStacks(string code) => !string.IsNullOrEmpty(code) && stacks.TryGetValue(code, out int n) ? n : 0;

        // Reject a grant that would exceed the stack limit rather than silently discarding stacks.
        public bool CanAdd(string code, int count = 1)
        {
            var d = catalog.Find(code);
            return count > 0 && d != null && (long)GetStacks(code) + count <= d.MaxStacks;
        }

        // Apply a complete validated grant and publish one change notification.
        public bool TryAdd(string code, int count = 1)
        {
            if (!CanAdd(code, count)) return false;
            stacks[code] = GetStacks(code) + count;
            Changed?.Invoke();
            return true;
        }

        // Remove a validated number of stacks without changing definition assets.
        public bool TryRemove(string code, int count = 1)
        {
            if (count <= 0 || GetStacks(code) < count) return false;
            int next = GetStacks(code) - count;
            if (next == 0) stacks.Remove(code); else stacks[code] = next;
            Changed?.Invoke(); return true;
        }

        // Return passive modifiers for backward-compatible callers and tests.
        public IEnumerable<StatModifier> GetStatModifiers() => GetStatModifiers(1f, 0f, false);

        // Evaluate passive and conditional powerups from the current gameplay context.
        public IEnumerable<StatModifier> GetStatModifiers(float health01, float movement01, bool falling)
        {
            foreach (var entry in stacks)
            {
                var d = catalog.Find(entry.Key);
                if (d == null) continue;
                if (d.EffectKind == PowerupEffectKind.StatModifier)
                    foreach (var m in d.ModifiersPerStack)
                    {
                        var scaled = m;
                        scaled.value = m.mode == StatModifierMode.MultiplicativePercent
                            ? (float)(Math.Pow(1d + m.value, entry.Value) - 1d) : m.value * entry.Value;
                        yield return scaled;
                    }
                foreach (var rule in d.RuntimeEffects)
                {
                    float factor;
                    switch (rule.condition)
                    {
                        case PowerupCondition.Passive: factor = 1f; break;
                        case PowerupCondition.MissingHealth: factor = 1f - Mathf.Clamp01(health01); break;
                        case PowerupCondition.LowHealth: factor = health01 <= rule.healthThreshold ? 1f : 0f; break;
                        case PowerupCondition.Moving: factor = Mathf.Clamp01(movement01); break;
                        case PowerupCondition.Falling: factor = falling ? 1f : 0f; break;
                        default: continue;
                    }
                    if (factor > 0f) yield return new StatModifier(rule.stat, rule.mode, rule.Evaluate(entry.Value) * factor);
                }
            }
        }

        // Return event-triggered effect magnitudes without mutating runtime stacks.
        public IEnumerable<(string code, PowerupEffectRule rule, float value)> GetTriggeredEffects(PowerupCondition trigger)
        {
            foreach (var entry in stacks)
            {
                var d = catalog.Find(entry.Key);
                if (d == null) continue;
                foreach (var rule in d.RuntimeEffects)
                    if (rule.condition == trigger) yield return (entry.Key, rule, rule.Evaluate(entry.Value));
            }
        }

        // Export all stacks by code for offline save integration.
        public List<PowerupStackRecord> Capture()
        {
            var result = new List<PowerupStackRecord>();
            foreach (var e in stacks) result.Add(new PowerupStackRecord(e.Key, e.Value));
            result.Sort((a,b) => string.CompareOrdinal(a.code,b.code));
            return result;
        }

        // Validate the entire snapshot before replacing the live state.
        public bool TryRestore(IReadOnlyList<PowerupStackRecord> records)
        {
            if (records == null) return false;
            var next = new Dictionary<string,int>();
            foreach (var record in records)
            {
                var d = catalog.Find(record.code);
                if (d == null || record.stacks <= 0 || record.stacks > d.MaxStacks || next.ContainsKey(record.code)) return false;
                next.Add(record.code, record.stacks);
            }
            stacks.Clear(); foreach (var e in next) stacks.Add(e.Key,e.Value);
            Changed?.Invoke(); return true;
        }
    }
}
