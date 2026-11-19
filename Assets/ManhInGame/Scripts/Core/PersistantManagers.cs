
using UnityEngine;

[DisallowMultipleComponent]
public class PersistentManagers : MonoBehaviour
{
    private static PersistentManagers instance;

    private void Awake()
    {
        // If another Managers already exists, remove this duplicate.
        if (instance != null && instance != this)
        {
            Debug.LogWarning(
                "[PersistentManagers] Duplicate Managers detected. " +
                "Destroying the new instance.",
                this
            );

            Destroy(gameObject);
            return;
        }

        instance = this;

        // DontDestroyOnLoad requires a root GameObject.
        transform.SetParent(null);

        DontDestroyOnLoad(gameObject);

        Debug.Log(
            "[PersistentManagers] Managers will persist between scenes.",
            this
        );
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
