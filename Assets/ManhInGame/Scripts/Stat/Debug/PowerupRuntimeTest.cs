
using UnityEngine;
using Nightfall.Stats;
using Nightfall.Powerups;

public sealed class PowerupRuntimeTest : MonoBehaviour
{
    [SerializeField]
    private PowerupRuntime runtime;

    [ContextMenu("Test/Add Red Pill")]
    public void AddRedPill()
    {
        if (runtime == null)
            return;

        runtime.AddPowerup("red_pill");

        Debug.Log(
            "[Stats Test] MaxHealth = " +
            runtime.GetStat(StatType.MaxHealth));
    }
}
