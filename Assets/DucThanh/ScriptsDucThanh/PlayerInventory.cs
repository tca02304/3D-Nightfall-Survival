using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DucThanh
{
    /// <summary>
    /// Component quản lý túi đồ và trang bị cầm trên tay của người chơi.
    /// Tích hợp cấu hình từ InventoryDatabaseConfigSO giống hệ thống NightMonsterSpawner.
    /// Bạn chỉ cần gắn script lên Player, gán config và thiết lập các Item trên Editor.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerInventory : MonoBehaviour
    {
        [Header("Configuration Reference")]
        [Tooltip("Cấu hình danh mục túi đồ & vật phẩm bắt đầu (Tương tự NightSpawnerConfigSO)")]
        [SerializeField] private InventoryDatabaseConfigSO config;

        [Header("Hand Equipment Setup")]
        [Tooltip("Xương tay cầm vật phẩm. Nếu để trống sẽ tự động tìm xương theo tên chỉ định trong ItemConfig")]
        [SerializeField] private Transform defaultHandTransform;

        [Header("Runtime State")]
        [SerializeField] private int equippedSlotIndex = -1;
        [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>();

        private GameObject currentHeldObject;
        private PlayerCombat cachedCombat;

        public event Action OnInventoryChanged;
        public event Action<ItemConfigSO> OnItemEquipped;
        public event Action OnItemUnequipped;

        public int MaxSlots => config != null ? config.MaxSlots : 8;
        public InventoryDatabaseConfigSO Config => config;
        public List<InventorySlot> Slots => slots;
        public int EquippedSlotIndex => equippedSlotIndex;
        public ItemConfigSO CurrentEquippedItem => (equippedSlotIndex >= 0 && equippedSlotIndex < slots.Count) ? slots[equippedSlotIndex].Item : null;
        public GameObject CurrentHeldObject => currentHeldObject;

        private void Awake()
        {
            cachedCombat = GetComponent<PlayerCombat>();
            EnsureDatabaseConfigLoaded();
            InitializeSlots();
        }

        private void EnsureDatabaseConfigLoaded()
        {
            if (config == null)
            {
                // Tự động tìm config trong thư mục Assets/DucThanh
#if UNITY_EDITOR
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:InventoryDatabaseConfigSO");
                if (guids.Length > 0)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    config = UnityEditor.AssetDatabase.LoadAssetAtPath<InventoryDatabaseConfigSO>(path);
                }
#endif
                if (config == null)
                {
                    config = Resources.Load<InventoryDatabaseConfigSO>("InventoryDatabaseConfig");
                }
            }
        }

        private void Start()
        {
            ApplyStartingItems();
            FindDefaultHandIfMissing();
        }

        private void Update()
        {
            HandleQuickSlotInput();
        }

        private void InitializeSlots()
        {
            int capacity = MaxSlots;
            if (slots.Count != capacity)
            {
                slots.Clear();
                for (int i = 0; i < capacity; i++)
                {
                    slots.Add(new InventorySlot());
                }
            }
        }

        private void ApplyStartingItems()
        {
            if (config == null || config.StartingItems == null) return;

            int slotToEquip = -1;
            for (int i = 0; i < config.StartingItems.Count; i++)
            {
                var entry = config.StartingItems[i];
                if (entry != null && entry.itemConfig != null)
                {
                    int addedIndex = AddItem(entry.itemConfig, entry.amount);
                    if (entry.equipAtStart && addedIndex != -1 && slotToEquip == -1)
                    {
                        slotToEquip = addedIndex;
                    }
                }
            }

            if (slotToEquip != -1)
            {
                EquipSlot(slotToEquip);
            }
        }

        private void FindDefaultHandIfMissing()
        {
            if (defaultHandTransform != null) return;

            Transform[] children = GetComponentsInChildren<Transform>(true);
            foreach (var child in children)
            {
                if (child.name == "Weapon (1)")
                {
                    defaultHandTransform = child;
                    return;
                }
            }

            foreach (var child in children)
            {
                if (child.name.Contains("RightHand") || child.name.Contains("Hand_R") || child.name.Contains("Weapon"))
                {
                    defaultHandTransform = child;
                    return;
                }
            }
        }

        /// <summary>
        /// Phím tắt nhanh 1-8 để đổi vũ khí/vật phẩm trên tay và phím V để vứt vật phẩm trên tay ra đất
        /// </summary>
        private void HandleQuickSlotInput()
        {
            bool vPressed = false;
            int numberSlot = -1;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.digit1Key.wasPressedThisFrame) numberSlot = 0;
                else if (Keyboard.current.digit2Key.wasPressedThisFrame) numberSlot = 1;
                else if (Keyboard.current.digit3Key.wasPressedThisFrame) numberSlot = 2;
                else if (Keyboard.current.digit4Key.wasPressedThisFrame) numberSlot = 3;
                else if (Keyboard.current.digit5Key.wasPressedThisFrame) numberSlot = 4;
                else if (Keyboard.current.digit6Key.wasPressedThisFrame) numberSlot = 5;
                else if (Keyboard.current.digit7Key.wasPressedThisFrame) numberSlot = 6;
                else if (Keyboard.current.digit8Key.wasPressedThisFrame) numberSlot = 7;

                if (Keyboard.current.vKey.wasPressedThisFrame)
                {
                    vPressed = true;
                }
            }
#endif
            if (!vPressed && numberSlot == -1)
            {
                try
                {
                    if (Input.GetKeyDown(KeyCode.Alpha1)) numberSlot = 0;
                    else if (Input.GetKeyDown(KeyCode.Alpha2)) numberSlot = 1;
                    else if (Input.GetKeyDown(KeyCode.Alpha3)) numberSlot = 2;
                    else if (Input.GetKeyDown(KeyCode.Alpha4)) numberSlot = 3;
                    else if (Input.GetKeyDown(KeyCode.Alpha5)) numberSlot = 4;
                    else if (Input.GetKeyDown(KeyCode.Alpha6)) numberSlot = 5;
                    else if (Input.GetKeyDown(KeyCode.Alpha7)) numberSlot = 6;
                    else if (Input.GetKeyDown(KeyCode.Alpha8)) numberSlot = 7;

                    if (Input.GetKeyDown(KeyCode.V))
                    {
                        vPressed = true;
                    }
                }
                catch
                {
                    // Tránh lỗi nếu chỉ bật New Input System
                }
            }

            if (numberSlot >= 0)
            {
                ToggleEquipSlot(numberSlot);
            }

            if (vPressed)
            {
                DropCurrentEquippedItem();
            }
        }

        /// <summary>
        /// Vứt vật phẩm đang cầm trên tay ra mặt đất phía trước Player
        /// </summary>
        public void DropCurrentEquippedItem(int amount = 1)
        {
            if (equippedSlotIndex < 0 || equippedSlotIndex >= slots.Count)
            {
                return;
            }

            int slotToDrop = equippedSlotIndex;
            DropItemAt(slotToDrop, amount);
        }

        public void ToggleEquipSlot(int slotIndex)
        {
            if (equippedSlotIndex == slotIndex)
            {
                UnequipCurrent();
            }
            else
            {
                EquipSlot(slotIndex);
            }
        }

        /// <summary>
        /// Thêm vật phẩm vào túi đồ. Trả về index ô được thêm hoặc -1 nếu túi đã đầy.
        /// </summary>
        public int AddItem(ItemConfigSO item, int amount = 1)
        {
            if (item == null || amount <= 0) return -1;

            // 1. Tìm ô đã có item cùng loại để xếp chồng (nếu maxStack > 1)
            if (item.MaxStack > 1)
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    if (!slots[i].IsEmpty && slots[i].Item.ItemId == item.ItemId && slots[i].Quantity < item.MaxStack)
                    {
                        amount = slots[i].AddQuantity(amount);
                        OnInventoryChanged?.Invoke();
                        if (amount <= 0) return i;
                    }
                }
            }

            // 2. Tìm ô trống đầu tiên
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].IsEmpty)
                {
                    int take = Mathf.Min(item.MaxStack, amount);
                    slots[i].SetItem(item, take);
                    amount -= take;
                    OnInventoryChanged?.Invoke();

                    if (amount > 0)
                    {
                        // Thử thêm số dư vào các ô trống tiếp theo
                        AddItem(item, amount);
                    }
                    return i;
                }
            }

            Debug.LogWarning($"[PlayerInventory] Túi đồ đã đầy, không thể thêm {item.ItemName}!");
            return -1;
        }

        /// <summary>
        /// Loại bỏ số lượng vật phẩm khỏi túi đồ theo slot index
        /// </summary>
        public bool RemoveItemAt(int slotIndex, int amount = 1)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count || slots[slotIndex].IsEmpty) return false;

            bool success = slots[slotIndex].RemoveQuantity(amount);
            if (success)
            {
                if (slots[slotIndex].IsEmpty && equippedSlotIndex == slotIndex)
                {
                    UnequipCurrent();
                }
                OnInventoryChanged?.Invoke();
            }
            return success;
        }

        /// <summary>
        /// Cầm vật phẩm tại slotIndex lên tay
        /// </summary>
        public bool EquipSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count) return false;
            if (slots[slotIndex].IsEmpty)
            {
                UnequipCurrent();
                return false;
            }

            UnequipCurrent();

            equippedSlotIndex = slotIndex;
            ItemConfigSO item = slots[slotIndex].Item;

            SpawnHeldItemVisual(item);
            UpdateCombatStats(item);

            OnItemEquipped?.Invoke(item);
            OnInventoryChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Hạ vật phẩm trên tay xuống
        /// </summary>
        public void UnequipCurrent()
        {
            if (currentHeldObject != null)
            {
                Destroy(currentHeldObject);
                currentHeldObject = null;
            }

            equippedSlotIndex = -1;
            ResetCombatStats();

            OnItemUnequipped?.Invoke();
            OnInventoryChanged?.Invoke();
        }

        private void SpawnHeldItemVisual(ItemConfigSO item)
        {
            if (item == null || item.HeldPrefab == null)
            {
                Debug.LogWarning($"[PlayerInventory] Không thể hiển thị trang bị: {(item == null ? "Item null" : item.ItemName + " chưa gán HeldPrefab!")}");
                return;
            }

            Transform targetHand = ResolveHandBone(item.HandBoneName);
            if (targetHand == null)
            {
                targetHand = defaultHandTransform != null ? defaultHandTransform : transform;
            }

            currentHeldObject = Instantiate(item.HeldPrefab, targetHand);
            currentHeldObject.name = $"{item.ItemName}_Held";

            // 1. Vô hiệu hóa và xóa toàn bộ Rigidbody để PhysX không tác động trọng lực kéo vật phẩm rơi xuyên map
            var rigidbodies = currentHeldObject.GetComponentsInChildren<Rigidbody>(true);
            foreach (var rb in rigidbodies)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.detectCollisions = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                Destroy(rb);
            }

            // 2. Vô hiệu hóa toàn bộ Collider trên vật phẩm cầm tay
            var colliders = currentHeldObject.GetComponentsInChildren<Collider>(true);
            foreach (var col in colliders)
            {
                col.enabled = false;
            }

            // 3. Vô hiệu hóa script nhặt đồ hoặc script logic rơi (ItemPickupWorld, WorldItem...)
            var worldPickups = currentHeldObject.GetComponentsInChildren<ItemPickupWorld>(true);
            foreach (var wp in worldPickups)
            {
                wp.enabled = false;
                Destroy(wp);
            }

            var legacyWorldItems = currentHeldObject.GetComponentsInChildren<WorldItem>(true);
            foreach (var wi in legacyWorldItems)
            {
                wi.enabled = false;
                Destroy(wi);
            }

            // 4. Snap vị trí, góc xoay và tỉ lệ bám chắc vào tay cầm theo thiết lập ItemConfigSO
            currentHeldObject.transform.localPosition = item.HandLocalPosition;
            currentHeldObject.transform.localRotation = Quaternion.Euler(item.HandLocalEulerAngles);
            currentHeldObject.transform.localScale = item.HandLocalScale;

            Debug.Log($"<color=cyan>[PlayerInventory] Đã cầm {item.ItemName} trên tay ({targetHand.name}) tại Pos={item.HandLocalPosition}</color>");
        }

        private Transform ResolveHandBone(string boneName)
        {
            if (!string.IsNullOrEmpty(boneName))
            {
                Transform[] children = GetComponentsInChildren<Transform>(true);
                foreach (var child in children)
                {
                    if (child.name.Equals(boneName, StringComparison.OrdinalIgnoreCase))
                    {
                        return child;
                    }
                }
            }

            if (defaultHandTransform != null) return defaultHandTransform;

            // Fallback tự động tìm các tên bone tay thông dụng
            string[] commonBones = { "Weapon (1)", "Hand.R", "RightHand", "Hand_R", "weapon" };
            Transform[] allChildren = GetComponentsInChildren<Transform>(true);
            foreach (var b in commonBones)
            {
                foreach (var child in allChildren)
                {
                    if (child.name.Equals(b, StringComparison.OrdinalIgnoreCase))
                    {
                        defaultHandTransform = child;
                        return child;
                    }
                }
            }

            return transform;
        }

        private void UpdateCombatStats(ItemConfigSO item)
        {
            if (cachedCombat == null) cachedCombat = GetComponent<PlayerCombat>();
            if (cachedCombat != null)
            {
                if (item.Type == ItemType.Weapon)
                {
                    cachedCombat.SetHasWeapon(true);
                    cachedCombat.SetAttackDamage(item.AttackDamage);
                }
                else
                {
                    cachedCombat.SetHasWeapon(false);
                }
            }

            var controller = GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.SetHasWeapon(item.Type == ItemType.Weapon);
            }
        }

        private void ResetCombatStats()
        {
            if (cachedCombat == null) cachedCombat = GetComponent<PlayerCombat>();
            if (cachedCombat != null)
            {
                cachedCombat.SetHasWeapon(false);
            }

            var controller = GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.SetHasWeapon(false);
            }
        }

        /// <summary>
        /// Vứt/ném vật phẩm tại slot ra thế giới phía trước Player
        /// </summary>
        public void DropItemAt(int slotIndex, int amount = 1)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count || slots[slotIndex].IsEmpty) return;

            ItemConfigSO item = slots[slotIndex].Item;
            int actualDrop = Mathf.Min(amount, slots[slotIndex].Quantity);

            // Nếu slot này đang được cầm trên tay, hạ/hủy vật thể trên tay ngay lập tức
            if (equippedSlotIndex == slotIndex)
            {
                UnequipCurrent();
            }

            // Spawn object ở phía trước Player (tầm ngực/tay) để item bay lên một chút rồi rơi xuống
            GameObject prefabToDrop = item.DropPrefab;
            if (prefabToDrop != null)
            {
                Vector3 spawnPos = transform.position + Vector3.up * 1.0f;
                Vector3 throwDir = transform.forward;

                // Kiểm tra va chạm phía trước để không spawn xuyên tường
                if (Physics.Raycast(spawnPos, throwDir, out RaycastHit wallHit, 0.7f, ~0, QueryTriggerInteraction.Ignore))
                {
                    spawnPos += throwDir * Mathf.Max(0.2f, wallHit.distance - 0.2f);
                }
                else
                {
                    spawnPos += throwDir * 0.7f;
                }

                GameObject droppedObj = Instantiate(prefabToDrop, spawnPos, Quaternion.identity);

                // Gắn ItemPickupWorld để quản lý nhặt và va chạm vật lý
                var pickup = droppedObj.GetComponent<ItemPickupWorld>();
                if (pickup == null)
                {
                    pickup = droppedObj.AddComponent<ItemPickupWorld>();
                }
                pickup.Init(item, actualDrop, 1.2f);

                // Kích hoạt lực ném: bay lên một chút và hướng nhẹ tới trước (tạo vòng cung đẹp mắt)
                Rigidbody rb = droppedObj.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = droppedObj.AddComponent<Rigidbody>();
                }

                rb.isKinematic = false;
                rb.useGravity = true;
                rb.mass = 1.0f;
                rb.linearDamping = 0.8f;
                rb.angularDamping = 1.5f;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rb.interpolation = RigidbodyInterpolation.Interpolate;

                // Vận tốc ném: hướng trước + vút lên trên một chút
                Vector3 throwVelocity = throwDir * 2.8f + Vector3.up * 3.8f;
                rb.linearVelocity = throwVelocity;
                rb.angularVelocity = UnityEngine.Random.insideUnitSphere * 3.0f;
            }

            Debug.Log($"<color=yellow>[PlayerInventory] Đã ném: {item.ItemName} x{actualDrop} ra thế giới.</color>");
            RemoveItemAt(slotIndex, actualDrop);
        }
    }
}
