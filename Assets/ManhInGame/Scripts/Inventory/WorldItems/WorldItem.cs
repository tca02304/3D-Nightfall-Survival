using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class WorldItem : MonoBehaviour
{
    [Header("Item Data")]
    [SerializeField] private int itemId;
    [SerializeField, Min(1)] private int amount = 1;

    [Header("Pickup Settings")]
    [SerializeField, Min(0f)]
    private float pickupDelay = 0.5f;

    [SerializeField, Min(0.1f)]
    private float retryInterval = 0.35f;

    private float pickupEnableTime;
    private float nextPickupAttemptTime;

    private bool isBeingPickedUp;

    private Transform blockedOwner;

    public int ItemId => itemId;
    public int Amount => amount;

    private void Awake()
    {
        SphereCollider sphere = GetComponent<SphereCollider>();
        Rigidbody rb = GetComponent<Rigidbody>();

        sphere.isTrigger = true;

        rb.isKinematic = false;
        rb.useGravity = true;

        pickupEnableTime = Time.time + pickupDelay;
        nextPickupAttemptTime = pickupEnableTime;
    }

    public void Setup(int newItemId, int newAmount)
    {
        if(newItemId <= 0 || newAmount <= 0)
        {
            Debug.LogError("[WorldItem] Ivalid ItemId or Amount");
            gameObject.SetActive(false);
            return;
        }

        itemId = newItemId;
        amount = newAmount;

        ResetPickupTimer();
    }

    public void SetDropOwner(Transform owner)
    {
        blockedOwner = owner != null ? owner.root : null;

        ResetPickupTimer();
    }

    private void ResetPickupTimer()
    {
        pickupEnableTime = Time.time + pickupDelay;
        nextPickupAttemptTime = pickupEnableTime;
    }

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player") ||
            other.transform.root.CompareTag("Player");
    }

    private bool IsBlockedOwner(Collider other)
    {
        return blockedOwner != null &&
            other.transform.root == blockedOwner;
    }

    private void OnTriggerEnter(Collider other)
    {
        HandlePlayerContact(other);
    }

    private void OnTriggerStay(Collider other)
    {
        HandlePlayerContact(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
            return;

        if (IsBlockedOwner(other))
        {
            blockedOwner = null;
        }
    }

    private void HandlePlayerContact(Collider other)
    {
        if (!IsPlayer(other))
            return;

        if (IsBlockedOwner(other))
            return;

        if (Time.time < pickupEnableTime)
            return;

        if (Time.time < nextPickupAttemptTime)
            return;

        if (isBeingPickedUp || amount <= 0)
            return;

        TryPickup();
    }

    private void TryPickup()
    {
        InventoryManager inventory = InventoryManager.Instance;
        ItemDatabase database = ItemDatabase.Instance;

        if(inventory == null || database == null)
        {
            nextPickupAttemptTime = Time.time + retryInterval;
            return;
        }

        ItemData item = database.GetItemById(itemId);

        if(item == null)
        {
            Debug.LogWarning($"[WorldItem] Item Id {itemId} not found");

            nextPickupAttemptTime = Time.time + retryInterval;
            return;
        }

        isBeingPickedUp = true;

        try
        {
            int previousAmount = amount;

            int remaining = inventory.AddItem(item, previousAmount);

            if (remaining < 0 || remaining > previousAmount)
            {
                Debug.LogError("[WorldItem] Invalid AddItem result");
                return;
            }

            int pickedAmount = previousAmount - remaining;

            amount = remaining;

            if (pickedAmount > 0)
            {
                Debug.Log($"[Pickup] +{pickedAmount} {item.name}");
            }

            if(amount <= 0)
            {
                Destroy(gameObject);
                return;
            }

            nextPickupAttemptTime = Time.time + retryInterval;
        }
        finally
        {
            isBeingPickedUp = false;
        }
    }
}
