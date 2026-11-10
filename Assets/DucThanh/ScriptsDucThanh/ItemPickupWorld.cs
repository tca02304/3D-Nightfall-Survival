using UnityEngine;

namespace DucThanh
{
    /// <summary>
    /// Component gắn lên vật thể nằm trên mặt đất trong thế giới (World Item).
    /// Cho phép Player đi lại gần hoặc chạm vào để tự động nhặt vào túi đồ.
    /// Tương thích với mọi ItemConfigSO (Katana, Súng, Cung, Búa, Vật phẩm hồi phục...).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    public class ItemPickupWorld : MonoBehaviour
    {
        [Header("Item Config Reference")]
        [Tooltip("Cấu hình vật phẩm tương ứng")]
        [SerializeField] private ItemConfigSO itemConfig;

        [Tooltip("Số lượng vật phẩm khi nhặt")]
        [SerializeField] private int quantity = 1;

        [Header("Ground & Pickup Settings")]
        [Tooltip("Khoảng cách tự động nhặt khi Player đến gần")]
        [SerializeField] private float pickupDistance = 1.8f;

        [Tooltip("Thời gian chờ trước khi Player có thể nhặt lại (tránh nhặt ngay lập tức khi vừa vứt ra)")]
        [SerializeField] private float pickupDelay = 1.5f;

        [Tooltip("Tự động xoay nhẹ khi ở trên mặt đất")]
        [SerializeField] private bool rotateOnGround = true;

        [Tooltip("Tốc độ xoay")]
        [SerializeField] private float rotationSpeed = 60f;

        [Tooltip("Hiệu ứng nhấp nhô nhẹ (Bobbing)")]
        [SerializeField] private bool bobbingEffect = true;

        [Tooltip("Biên độ nhấp nhô")]
        [SerializeField] private float bobbingAmplitude = 0.08f;

        [Tooltip("Tần số nhấp nhô")]
        [SerializeField] private float bobbingFrequency = 2f;

        private Collider pickupCollider;
        private Vector3 startPosition;
        private PlayerInventory cachedInventory;
        private float spawnTime;

        public ItemConfigSO ItemConfig => itemConfig;
        public int Quantity => quantity;

        public void Init(ItemConfigSO config, int count, float customPickupDelay = 1.5f)
        {
            this.itemConfig = config;
            this.quantity = Mathf.Max(1, count);
            this.pickupDelay = customPickupDelay;
            this.spawnTime = Time.time;
        }

        private void Awake()
        {
            SetupCollider();
            startPosition = transform.position;
            spawnTime = Time.time;
        }

        private void SetupCollider()
        {
            pickupCollider = GetComponent<Collider>();
            if (pickupCollider == null)
            {
                // Tự động thêm SphereCollider nếu prefab chưa có collider
                SphereCollider sc = gameObject.AddComponent<SphereCollider>();
                sc.isTrigger = true;
                sc.radius = pickupDistance;
                pickupCollider = sc;
            }
            else
            {
                pickupCollider.isTrigger = true;
            }
        }

        private void Start()
        {
            FindPlayerInventory();
            AutoDetectItemConfigIfMissing();
        }

        private void AutoDetectItemConfigIfMissing()
        {
            if (itemConfig != null) return;

            // Nếu người dùng kéo prefab vào scene mà chưa gán itemConfig, tự động tìm theo tên GameObject
            string myName = gameObject.name.Replace("(Clone)", "").Trim();
#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:ItemConfigSO {myName}");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                itemConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemConfigSO>(path);
                return;
            }

            // Fallback tìm tất cả ItemConfigSO trong database
            string[] allGuids = UnityEditor.AssetDatabase.FindAssets("t:ItemConfigSO");
            foreach (var g in allGuids)
            {
                string p = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                var cfg = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemConfigSO>(p);
                if (cfg != null && (cfg.name.Equals(myName, System.StringComparison.OrdinalIgnoreCase) || 
                                    cfg.ItemName.Equals(myName, System.StringComparison.OrdinalIgnoreCase)))
                {
                    itemConfig = cfg;
                    return;
                }
            }
#endif
        }

        private void Update()
        {
            // Hiệu ứng xoay
            if (rotateOnGround)
            {
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            }

            // Hiệu ứng nhấp nhô
            if (bobbingEffect)
            {
                float newY = startPosition.y + Mathf.Sin(Time.time * bobbingFrequency) * bobbingAmplitude;
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            }

            // Kiểm tra khoảng cách
            CheckDistancePickup();
        }

        private void FindPlayerInventory()
        {
            if (cachedInventory == null)
            {
                cachedInventory = FindFirstObjectByType<PlayerInventory>();
            }
        }

        private void CheckDistancePickup()
        {
            if (Time.time < spawnTime + pickupDelay) return;

            if (cachedInventory == null)
            {
                FindPlayerInventory();
                if (cachedInventory == null) return;
            }

            float dist = Vector3.Distance(transform.position, cachedInventory.transform.position);
            if (dist <= pickupDistance)
            {
                TryCollect(cachedInventory);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (Time.time < spawnTime + pickupDelay) return;

            PlayerInventory inv = other.GetComponentInParent<PlayerInventory>();
            if (inv != null)
            {
                TryCollect(inv);
            }
        }

        private void TryCollect(PlayerInventory inventory)
        {
            if (itemConfig == null || inventory == null) return;

            int addedSlot = inventory.AddItem(itemConfig, quantity);
            if (addedSlot != -1)
            {
                // Nếu trên tay người chơi hiện đang chưa cầm gì, tự động trang bị vật phẩm vừa nhặt lên tay
                if (inventory.EquippedSlotIndex == -1)
                {
                    inventory.EquipSlot(addedSlot);
                }

                Debug.Log($"<color=green>[ItemPickup] Người chơi đã nhặt: {itemConfig.ItemName} x{quantity} (Slot {addedSlot + 1})</color>");
                Destroy(gameObject);
            }
        }
    }
}
