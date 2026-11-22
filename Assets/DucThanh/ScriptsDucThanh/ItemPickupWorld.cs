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
        [SerializeField] private float pickupDistance = 2.2f;

        [Tooltip("Thời gian chờ trước khi Player có thể nhặt lại (tránh nhặt ngay lập tức khi vừa vứt ra)")]
        [SerializeField] private float pickupDelay = 1.5f;

        private Collider pickupCollider;
        private PlayerInventory cachedInventory;
        private PlayerController cachedPlayer;
        private float spawnTime;

        public ItemConfigSO ItemConfig => itemConfig;
        public int Quantity => quantity;

        public void Init(ItemConfigSO config, int count, float customPickupDelay = 1.2f)
        {
            this.itemConfig = config;
            this.quantity = Mathf.Max(1, count);
            this.pickupDelay = customPickupDelay;
            this.spawnTime = Time.time;

            SetupPhysicsAndColliders();
        }

        private void Awake()
        {
            SetupPhysicsAndColliders();
            // Vật phẩm có sẵn trong Scene có thể nhặt ngay lập tức không bị delay
            spawnTime = -999f;
        }

        private void SetupPhysicsAndColliders()
        {
            // 1. Cấu hình Rigidbody mô phỏng vật lý chân thực (rơi xuống đất, theo dõi biến động địa hình)
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }

            rb.isKinematic = false;
            rb.useGravity = true;
            rb.mass = 1.0f;
            rb.linearDamping = 0.8f;
            rb.angularDamping = 1.5f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            // 2. Tìm và phân loại tất cả Colliders trên vật phẩm
            Collider[] allColliders = GetComponentsInChildren<Collider>(true);
            bool hasSolidCollider = false;

            foreach (var col in allColliders)
            {
                // Nếu là SphereCollider có bán kính nhặt lớn, đặt làm trigger pickup
                if (col == pickupCollider || (col is SphereCollider sc && sc.radius >= pickupDistance * 0.75f))
                {
                    col.isTrigger = true;
                    if (col is SphereCollider sphere)
                    {
                        sphere.radius = pickupDistance;
                        pickupCollider = sphere;
                    }
                }
                else
                {
                    // Các collider hình thể (BoxCollider, MeshCollider, CapsuleCollider, Sphere nhỏ) giữ làm solid để va chạm sàn
                    col.isTrigger = false;
                    hasSolidCollider = true;
                }
            }

            // Nếu chưa có solid collider nào để tiếp xúc mặt đất, tự động tạo BoxCollider solid vừa vặn
            if (!hasSolidCollider)
            {
                BoxCollider solidCol = gameObject.AddComponent<BoxCollider>();
                solidCol.isTrigger = false;
                solidCol.size = new Vector3(0.35f, 0.35f, 0.35f);
                hasSolidCollider = true;
            }

            // Nếu chưa có trigger collider để quét bán kính nhặt, tự động tạo mới SphereCollider Trigger
            if (pickupCollider == null)
            {
                SphereCollider sc = gameObject.AddComponent<SphereCollider>();
                sc.isTrigger = true;
                sc.radius = pickupDistance;
                pickupCollider = sc;
            }

            // 3. Vô hiệu hóa va chạm giữa item và Player (để không cản trở di chuyển hay xô đẩy người chơi)
            IgnoreCollisionWithPlayer();

            // 4. Vô hiệu hóa script WorldItem cũ (nếu có) để tránh xung đột hệ thống nhặt
            var legacyWorldItems = GetComponentsInChildren<WorldItem>(true);
            foreach (var wi in legacyWorldItems)
            {
                wi.enabled = false;
            }
        }

        private void IgnoreCollisionWithPlayer()
        {
            if (cachedPlayer == null)
            {
                cachedPlayer = FindFirstObjectByType<PlayerController>();
            }

            if (cachedPlayer != null)
            {
                Collider[] playerColliders = cachedPlayer.GetComponentsInChildren<Collider>(true);
                Collider[] itemColliders = GetComponentsInChildren<Collider>(true);
                foreach (var pCol in playerColliders)
                {
                    foreach (var iCol in itemColliders)
                    {
                        if (pCol != null && iCol != null && !iCol.isTrigger)
                        {
                            Physics.IgnoreCollision(pCol, iCol, true);
                        }
                    }
                }
            }
        }

        private void Start()
        {
            FindPlayerInventory();
            AutoDetectItemConfigIfMissing();
            IgnoreCollisionWithPlayer();
        }

        private void AutoDetectItemConfigIfMissing()
        {
            if (itemConfig != null) return;

            // Nếu người dùng kéo prefab vào scene mà chưa gán itemConfig, tự động tìm theo tên GameObject
            string myName = gameObject.name.Replace("(Clone)", "").Trim();

            // 1. Tìm trong DatabaseConfig của PlayerInventory nếu có sẵn
            if (cachedInventory == null) FindPlayerInventory();
            if (cachedInventory != null && cachedInventory.Config != null && cachedInventory.Config.RegisteredItems != null)
            {
                foreach (var reg in cachedInventory.Config.RegisteredItems)
                {
                    if (reg != null && (reg.name.Equals(myName, System.StringComparison.OrdinalIgnoreCase) ||
                                        reg.ItemName.Equals(myName, System.StringComparison.OrdinalIgnoreCase) ||
                                        (reg.HeldPrefab != null && reg.HeldPrefab.name.Equals(myName, System.StringComparison.OrdinalIgnoreCase))))
                    {
                        itemConfig = reg;
                        return;
                    }
                }
            }
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
            // Bảo vệ an toàn: nếu vật phẩm rơi ra khỏi map (dưới đáy vực sâu), tự hủy để tránh leak
            if (transform.position.y < -100f)
            {
                Destroy(gameObject);
                return;
            }

            // Kiểm tra khoảng cách
            CheckDistancePickup();
        }

        private void FindPlayerInventory()
        {
            if (cachedInventory != null) return;

            cachedInventory = FindFirstObjectByType<PlayerInventory>();
            if (cachedInventory != null) return;

            // Tự động tìm Player qua PlayerController hoặc Tag và gắn PlayerInventory nếu Player chưa có
            if (cachedPlayer == null)
            {
                cachedPlayer = FindFirstObjectByType<PlayerController>();
            }

            if (cachedPlayer != null)
            {
                cachedInventory = cachedPlayer.GetComponent<PlayerInventory>();
                if (cachedInventory == null)
                {
                    cachedInventory = cachedPlayer.gameObject.AddComponent<PlayerInventory>();
                }
                return;
            }

            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                cachedInventory = playerObj.GetComponent<PlayerInventory>();
                if (cachedInventory == null)
                {
                    cachedInventory = playerObj.AddComponent<PlayerInventory>();
                }
            }
        }

        private void CheckDistancePickup()
        {
            if (Time.time < spawnTime + pickupDelay) return;

            if (cachedInventory == null)
            {
                FindPlayerInventory();
                if (cachedInventory == null)
                {
                    // Fallback kiểm tra khoảng cách với PlayerController nếu chưa có inventory
                    if (cachedPlayer == null) cachedPlayer = FindFirstObjectByType<PlayerController>();
                    if (cachedPlayer != null)
                    {
                        Vector3 pPos = cachedPlayer.transform.position;
                        float hDist = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(pPos.x, pPos.z));
                        float vDist = Mathf.Abs(transform.position.y - pPos.y);
                        if (hDist <= pickupDistance && vDist <= 3.0f)
                        {
                            TryCollect(null);
                        }
                    }
                    return;
                }
            }

            Vector3 playerPos = cachedInventory.transform.position;
            // Tính khoảng cách 2D ngang XZ và kiểm tra độ chênh lệch chiều cao Y để nhặt mượt mà
            float horizontalDist = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(playerPos.x, playerPos.z));
            float verticalDist = Mathf.Abs(transform.position.y - playerPos.y);
            float distance3D = Vector3.Distance(transform.position, playerPos);

            if ((horizontalDist <= pickupDistance && verticalDist <= 3.0f) || distance3D <= pickupDistance)
            {
                TryCollect(cachedInventory);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (Time.time < spawnTime + pickupDelay) return;

            PlayerInventory inv = other.GetComponentInParent<PlayerInventory>();
            if (inv == null)
            {
                var player = other.GetComponentInParent<PlayerController>();
                if (player != null)
                {
                    inv = player.GetComponent<PlayerInventory>();
                    if (inv == null)
                    {
                        inv = player.gameObject.AddComponent<PlayerInventory>();
                    }
                }
                else if (other.CompareTag("Player"))
                {
                    inv = other.GetComponent<PlayerInventory>();
                    if (inv == null)
                    {
                        inv = other.gameObject.AddComponent<PlayerInventory>();
                    }
                }
            }

            if (inv != null)
            {
                TryCollect(inv);
            }
            else if (other.GetComponentInParent<PlayerController>() != null || other.CompareTag("Player"))
            {
                TryCollect(null);
            }
        }

        private void TryCollect(PlayerInventory inventory)
        {
            if (itemConfig == null)
            {
                AutoDetectItemConfigIfMissing();
            }

            if (inventory != null && itemConfig != null)
            {
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
                    return;
                }
                else
                {
                    // Túi đồ đã đầy
                    return;
                }
            }

            // Fallback an toàn: Trang bị trực tiếp lên tay Player nếu không có inventory
            if (inventory == null)
            {
                EquipDirectlyToHandFallback();
            }
        }

        private void EquipDirectlyToHandFallback()
        {
            if (cachedPlayer == null)
            {
                cachedPlayer = FindFirstObjectByType<PlayerController>();
            }
            if (cachedPlayer == null) return;

            Transform targetHand = null;
            string handBone = itemConfig != null ? itemConfig.HandBoneName : "Weapon (1)";
            Transform[] children = cachedPlayer.GetComponentsInChildren<Transform>(true);
            if (!string.IsNullOrEmpty(handBone))
            {
                foreach (var c in children)
                {
                    if (c.name.Equals(handBone, System.StringComparison.OrdinalIgnoreCase))
                    {
                        targetHand = c;
                        break;
                    }
                }
            }

            if (targetHand == null)
            {
                string[] commonBones = { "Weapon (1)", "Hand.R", "RightHand", "Hand_R", "weapon" };
                foreach (var b in commonBones)
                {
                    foreach (var c in children)
                    {
                        if (c.name.Equals(b, System.StringComparison.OrdinalIgnoreCase))
                        {
                            targetHand = c;
                            break;
                        }
                    }
                    if (targetHand != null) break;
                }
            }

            if (targetHand == null) targetHand = cachedPlayer.transform;

            transform.SetParent(targetHand);

            // 1. Vô hiệu hóa và hủy Rigidbody để không bị trọng lực vật lý kéo rơi xuyên map
            var rigidbodies = GetComponentsInChildren<Rigidbody>(true);
            foreach (var rb in rigidbodies)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.detectCollisions = false;
                Destroy(rb);
            }

            // 2. Vô hiệu hóa toàn bộ Collider
            var allCols = GetComponentsInChildren<Collider>(true);
            foreach (var c in allCols)
            {
                c.enabled = false;
            }

            // 3. Vô hiệu hóa script WorldItem cũ
            var legacyWorldItems = GetComponentsInChildren<WorldItem>(true);
            foreach (var wi in legacyWorldItems)
            {
                wi.enabled = false;
                Destroy(wi);
            }

            if (itemConfig != null)
            {
                transform.localPosition = itemConfig.HandLocalPosition;
                transform.localRotation = Quaternion.Euler(itemConfig.HandLocalEulerAngles);
                transform.localScale = itemConfig.HandLocalScale;
            }

            cachedPlayer.SetHasWeapon(true);

            var combat = cachedPlayer.GetComponent<PlayerCombat>();
            if (combat != null)
            {
                combat.SetHasWeapon(true);
                if (itemConfig != null)
                {
                    combat.SetAttackDamage(itemConfig.AttackDamage);
                }
            }

            this.enabled = false;
            Debug.Log($"<color=green>[ItemPickup] Đã trang bị trực tiếp {(itemConfig != null ? itemConfig.ItemName : gameObject.name)} lên tay Player!</color>");
        }
    }
}
