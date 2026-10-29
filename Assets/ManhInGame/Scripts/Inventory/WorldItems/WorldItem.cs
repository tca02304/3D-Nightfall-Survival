using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class WorldItem : MonoBehaviour
{
    [SerializeField] private int itemId;
    [SerializeField] private int amount = 1;

    [SerializeField] private float pickupDelay = 0.5f;

    private float pickupEnableTime;

    private bool isBeingPickedUp;

    public int ItemId => itemId;
    public int Amount => amount;

    private void Awake()
    {
        SphereCollider sphereCollider = GetComponent<SphereCollider>();
        Rigidbody rb = GetComponent<Rigidbody>();

        sphereCollider.isTrigger = true;

        rb.isKinematic = true;
        rb.useGravity = false;

        pickupEnableTime = Time.time + pickupDelay;
    }

    public void Setup(int newItemId, int newAmount)
    {
        itemId = newItemId;
        amount = Mathf.Max(1, newAmount);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isPlayer(other))
            return;

        TryPickup();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!isPlayer(other))
            return;

        isBeingPickedUp = false;
    }

    private bool isPlayer(Collider other)
    {
        return other.CompareTag("Player") ||
            other.transform.root.CompareTag("Player");
    }

    private void TryPickup()
    {
        if (Time.time < pickupEnableTime)
            return;

        if (isBeingPickedUp)
            return;

        if (amount <= 0)
            return;

        if(InventoryManager.Instance == null)
        {
            Debug.LogWarning("InventoryManager chưa tồn tại");
            return;
        }

        if(ItemDatabase.Instance == null)
        {
            Debug.LogWarning("ItemDatabase chưa tồn tại");
            return;
        }

        ItemData item = ItemDatabase.Instance.GetItemById(itemId);

        if(item == null)
        {
            Debug.LogWarning("Không tìm thấy ItemData với ID: " + ItemId);
            return;
        }

        isBeingPickedUp = true;

        int oldAmount = amount;

        int remainingAmount = InventoryManager.Instance.AddItem(item, amount);

        amount = remainingAmount;

        int pickedAmount = oldAmount - remainingAmount;

        if(pickedAmount > 0)
        {
            Debug.Log("Picked up " + pickedAmount + " " + item.name);
        }

        if (amount <= 0)
        {
            Destroy(gameObject);
            return;
        }

        Debug.Log(item.name + " remaining in world: " + amount);
    }
}
