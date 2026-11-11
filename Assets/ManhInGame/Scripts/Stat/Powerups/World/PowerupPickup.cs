
using UnityEngine;
using Nightfall.Powerups;

[RequireComponent(typeof(Collider))]
public sealed class PowerupPickup : MonoBehaviour
{
    [SerializeField]
    private PowerupDefinitionSO definition;

    [SerializeField, Min(1)]
    private int amount = 1;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (definition == null)
            return;

        PowerupRuntime runtime =
            other.GetComponentInParent<PowerupRuntime>();

        if (runtime == null)
            runtime = other.GetComponentInChildren<PowerupRuntime>();

        if (runtime == null)
        {
            Debug.LogWarning(
                "[PowerupPickup] Player has no PowerupRuntime.");
            return;
        }

        if (!runtime.AddPowerup(definition.powerupId, amount))
            return;

        Destroy(gameObject);
    }
}
