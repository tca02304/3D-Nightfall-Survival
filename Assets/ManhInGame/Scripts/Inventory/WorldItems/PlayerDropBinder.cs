using UnityEngine;

public sealed class PlayerDropBinder : MonoBehaviour
{
    [Header("Spawn Origin")]
    [SerializeField] private Transform dropPoint;

    [Header("Single-Player Only")]
    [SerializeField] private bool autoBindOnStart;

    private ItemDropper boundDropper;

    // Auto-bind only in single-player testing when this GameObject is the local Player.
    private void Start()
    {
        if (autoBindOnStart)
            BindForLocalPlayer();
    }

    // Bind the local Player to the persistent ItemDropper after spawn or Fusion authority checks.
    public void BindForLocalPlayer()
    {
        ItemDropper dropper = FindFirstObjectByType<ItemDropper>();
        if (dropper == null)
        {
            Debug.LogWarning("[PlayerDropBinder] ItemDropper not found; launch through bootstrap Scene.", this);
            return;
        }

        dropper.BindPlayer(transform, dropPoint != null ? dropPoint : transform);
        boundDropper = dropper;
    }

    // Unbind when this Player is disabled or despawned so Managers never stores a stale Transform.
    private void OnDisable()
    {
        if (boundDropper != null)
            boundDropper.ClearPlayerBinding(transform);

        boundDropper = null;
    }
}
