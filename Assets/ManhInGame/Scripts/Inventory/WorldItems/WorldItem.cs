
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class WorldItem : MonoBehaviour
{
    [Header("Item Data")]
    [SerializeField] private int itemId;
    [SerializeField, Min(1)] private int amount = 1;

    [Header("Pickup Detection")]
    [SerializeField, Min(0.1f)]
    private float pickupRadius = 1.2f;

    [SerializeField, Min(0f)]
    private float ownerPickupCooldown = 0.65f;

    [SerializeField, Min(0.05f)]
    private float retryInterval = 0.1f;

    private SphereCollider pickupCollider;
    private Rigidbody body;

    private Transform blockedOwner;
    private float ownerBlockUntil;
    private float nextPickupAttemptTime;

    private bool isBeingPickedUp;

    public int ItemId => itemId;
    public int Amount => amount;

    // Initialize Rigidbody and automatically configure the pickup trigger.
    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        pickupCollider = ResolvePickupCollider();

        if (pickupCollider == null)
        {
            Debug.LogError(
                $"[WorldItem] Pickup trigger unavailable: {name}",
                this);
        }

        body.isKinematic = false;
        body.useGravity = true;

        nextPickupAttemptTime = Time.time;
    }

    // Find or create the dedicated pickup trigger on a child object.
    private SphereCollider ResolvePickupCollider()
    {
        Transform child = transform.Find("PickupTrigger");

        if (child == null)
        {
            GameObject triggerObject =
                new GameObject("PickupTrigger");

            triggerObject.transform.SetParent(transform, false);
            child = triggerObject.transform;
        }

        SphereCollider sphere =
            child.GetComponent<SphereCollider>();

        if (sphere == null)
        {
            sphere = child.gameObject
                .AddComponent<SphereCollider>();
        }

        sphere.isTrigger = true;
        sphere.radius = pickupRadius;
        sphere.enabled = true;

        return sphere;
    }

    // Initialize the spawned world item with its ItemId and stack amount.
    public void Setup(int newItemId, int newAmount)
    {
        if (newItemId <= 0 || newAmount <= 0)
        {
            Debug.LogError(
                "[WorldItem] Invalid ItemId or Amount.",
                this);

            gameObject.SetActive(false);
            return;
        }

        itemId = newItemId;
        amount = newAmount;
    }

    // Temporarily block the thrower from immediately picking up the dropped item.
    public void SetDropOwner(Transform owner)
    {
        blockedOwner = owner;

        ownerBlockUntil = owner != null
            ? Time.time + ownerPickupCooldown
            : 0f;
    }

    // Check whether a collider belongs to a player.
    private bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player") ||
               other.transform.root.CompareTag("Player");
    }

    // Check whether the player is still within the temporary owner cooldown.
    private bool IsBlockedOwner(Collider other)
    {
        if (blockedOwner == null)
            return false;

        if (Time.time >= ownerBlockUntil)
        {
            blockedOwner = null;
            return false;
        }

        Transform target = other.transform;

        return target == blockedOwner ||
               target.IsChildOf(blockedOwner);
    }

    // Attempt pickup immediately when a player enters the pickup trigger.
    private void OnTriggerEnter(Collider other)
    {
        HandlePlayerContact(other);
    }

    // Retry pickup while a player remains inside the pickup trigger.
    private void OnTriggerStay(Collider other)
    {
        HandlePlayerContact(other);
    }

    // Validate the pickup conditions before transferring an item.
    private void HandlePlayerContact(Collider other)
    {
        if (amount <= 0 || isBeingPickedUp)
            return;

        if (!IsPlayer(other))
            return;

        if (IsBlockedOwner(other))
            return;

        if (Time.time < nextPickupAttemptTime)
            return;

        TryPickup();
    }

    // Transfer available items to inventory and preserve any remainder.
    private void TryPickup()
    {
        InventoryManager inventory =
            InventoryManager.Instance;

        ItemDatabase database =
            ItemDatabase.Instance;

        if (inventory == null || database == null)
        {
            nextPickupAttemptTime =
                Time.time + retryInterval;
            return;
        }

        ItemData item = database.GetItemById(itemId);

        if (item == null)
        {
            Debug.LogWarning(
                $"[WorldItem] Unknown ItemId={itemId}.",
                this);

            nextPickupAttemptTime =
                Time.time + retryInterval;
            return;
        }

        isBeingPickedUp = true;

        try
        {
            int previous = amount;

            int remaining =
                inventory.AddItem(item, previous);

            if (remaining < 0 || remaining > previous)
            {
                Debug.LogError(
                    "[WorldItem] Invalid AddItem result.",
                    this);

                nextPickupAttemptTime =
                    Time.time + retryInterval;
                return;
            }

            int picked = previous - remaining;
            amount = remaining;

            if (picked > 0)
            {
                Debug.Log(
                    $"[Pickup] +{picked} {item.name}",
                    this);
            }

            if (amount == 0)
            {
                Destroy(gameObject);
                return;
            }

            nextPickupAttemptTime =
                Time.time + retryInterval;
        }
        finally
        {
            isBeingPickedUp = false;
        }
    }
}
