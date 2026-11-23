using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nightfall.Stats
{
    [Serializable]
    public struct TimedBuffRecord
    {
        public string key;
        public StatModifier modifier;
        public float remainingSeconds;
    }

    public sealed class TimedBuffRuntime
    {
        private readonly Dictionary<string,TimedBuffRecord> active = new Dictionary<string,TimedBuffRecord>();
        public event Action Changed;

        // Add or refresh one named buff; reapplying the same key never doubles its modifier.
        public bool Apply(string key, StatModifier modifier, float duration)
        {
            if (string.IsNullOrWhiteSpace(key) || duration <= 0f || float.IsNaN(duration) || float.IsInfinity(duration) ||
                float.IsNaN(modifier.value) || float.IsInfinity(modifier.value)) return false;
            active[key] = new TimedBuffRecord { key = key, modifier = modifier, remainingSeconds = duration };
            Changed?.Invoke(); return true;
        }

        // Remove one explicitly cancelled buff.
        public bool Remove(string key)
        {
            if (key == null || !active.Remove(key)) return false;
            Changed?.Invoke(); return true;
        }

        // Advance scaled game time and publish an event only when a buff expires.
        public void Tick(float seconds)
        {
            if (active.Count == 0 || seconds <= 0f || float.IsInfinity(seconds) || float.IsNaN(seconds)) return;
            var keys = new List<string>(active.Keys);
            bool expired = false;
            foreach (string key in keys)
            {
                var b = active[key]; b.remainingSeconds -= seconds;
                if (b.remainingSeconds <= 0f) { active.Remove(key); expired = true; }
                else active[key] = b;
            }
            if (expired) Changed?.Invoke();
        }

        // Enumerate only currently active modifiers.
        public IEnumerable<StatModifier> GetModifiers()
        {
            foreach (var b in active.Values) yield return b.modifier;
        }

        // Capture remaining gameplay seconds; no wall-clock expiration during offline play.
        public List<TimedBuffRecord> Capture() => new List<TimedBuffRecord>(active.Values);

        // Restore a complete valid snapshot without partially changing active buffs.
        public bool TryRestore(IReadOnlyList<TimedBuffRecord> records)
        {
            if (records == null) return false;
            var next = new Dictionary<string,TimedBuffRecord>();
            foreach (var b in records)
            {
                if (string.IsNullOrWhiteSpace(b.key) || !(b.remainingSeconds > 0f) || float.IsInfinity(b.remainingSeconds) ||
                    float.IsNaN(b.modifier.value) || float.IsInfinity(b.modifier.value) || next.ContainsKey(b.key)) return false;
                next.Add(b.key,b);
            }
            active.Clear(); foreach (var b in next) active.Add(b.Key,b.Value);
            Changed?.Invoke(); return true;
        }
    }
}
