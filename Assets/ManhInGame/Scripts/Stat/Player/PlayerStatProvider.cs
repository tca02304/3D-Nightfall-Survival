
using System;
using UnityEngine;
using Nightfall.Stats;
using Nightfall.Powerups;

namespace Nightfall.Stats
{
    /// <summary>
    /// Public API for Player Stats.
    /// Other gameplay systems should access stats through this class.
    ///
    /// Does not manage current HP, combat, or movement.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerStatProvider : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField]
        private PowerupRuntime powerupRuntime;

        public event Action OnStatsChanged;

        private void Awake()
        {
            if (powerupRuntime == null)
            {
                powerupRuntime =
                    GetComponent<PowerupRuntime>();
            }

            if (powerupRuntime == null)
            {
                Debug.LogError(
                    "[PlayerStatProvider] Missing PowerupRuntime.",
                    this);
            }
        }

        /// <summary>
        /// Get final value after all passive powerup modifiers.
        /// </summary>
        public float GetStat(StatType statType)
        {
            if (powerupRuntime == null)
                return 0f;

            return powerupRuntime.GetStat(statType);
        }

        /// <summary>
        /// Set a base stat provided by the Player system.
        /// </summary>
        public void SetBaseStat(
            StatType statType,
            float baseValue)
        {
            if (powerupRuntime == null)
                return;

            powerupRuntime.SetBaseStat(
                statType,
                baseValue);

            OnStatsChanged?.Invoke();
        }

        /// <summary>
        /// Get the number of owned stacks.
        /// </summary>
        public int GetPowerupStacks(string powerupId)
        {
            if (powerupRuntime == null)
                return 0;

            return powerupRuntime.GetStacks(powerupId);
        }

        /// <summary>
        /// Acquire a powerup.
        /// </summary>
        public bool AddPowerup(
            string powerupId,
            int amount = 1)
        {
            if (powerupRuntime == null)
                return false;

            bool success = powerupRuntime.AddPowerup(
                powerupId,
                amount);

            if (success)
                OnStatsChanged?.Invoke();

            return success;
        }
    }
}
