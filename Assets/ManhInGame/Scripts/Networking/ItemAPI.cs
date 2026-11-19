using UnityEngine;

// Legacy compatibility component; static ItemData is now loaded locally by ItemDatabase.
[DisallowMultipleComponent]
public sealed class ItemAPI : MonoBehaviour
{
    public bool IsLoaded => ItemDatabase.Instance != null && ItemDatabase.Instance.IsReady;

    // Confirm local ItemCatalog availability without issuing a network request.
    private void Start()
    {
        if (!IsLoaded)
            Debug.LogError("[ItemAPI] Local ItemCatalog is not ready; check ItemDatabase.Catalog.", this);
    }
}
