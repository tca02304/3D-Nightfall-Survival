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
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.digit1Key.wasPressedThisFrame) ToggleEquipSlot(0);
                else if (Keyboard.current.digit2Key.wasPressedThisFrame) ToggleEquipSlot(1);
                else if (Keyboard.current.digit3Key.wasPressedThisFrame) ToggleEquipSlot(2);
                else if (Keyboard.current.digit4Key.wasPressedThisFrame) ToggleEquipSlot(3);
                else if (Keyboard.current.digit5Key.wasPressedThisFrame) ToggleEquipSlot(4);
                else if (Keyboard.current.digit6Key.wasPressedThisFrame) ToggleEquipSlot(5);
                else if (Keyboard.current.digit7Key.wasPressedThisFrame) ToggleEquipSlot(6);
                else if (Keyboard.current.digit8Key.wasPressedThisFrame) ToggleEquipSlot(7);

                // Nhấn V để vứt vật phẩm đang cầm trên tay ra đất
                if (Keyboard.current.vKey.wasPressedThisFrame)
                {
                    DropCurrentEquippedItem();
                }
            }
#else
            if (Input.GetKeyDown(KeyCode.Alpha1)) ToggleEquipSlot(0);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) ToggleEquipSlot(1);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) ToggleEquipSlot(2);
            else if (Input.GetKeyDown(KeyCode.Alpha4)) ToggleEquipSlot(3);
            else if (Input.GetKeyDown(KeyCode.Alpha5)) ToggleEquipSlot(4);
            else if (Input.GetKeyDown(KeyCode.Alpha6)) ToggleEquipSlot(5);
            else if (Input.GetKeyDown(KeyCode.Alpha7)) ToggleEquipSlot(6);
            else if (Input.GetKeyDown(KeyCode.Alpha8)) ToggleEquipSlot(7);

            // Nhấn V để vứt vật phẩm đang cầm trên tay ra đất
            if (Input.GetKeyDown(KeyCode.V))
            {
                DropCurrentEquippedItem();
            }
#endif
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
            currentHeldObject.transform.localPosition = item.HandLocalPosition;
            currentHeldObject.transform.localRotation = Quaternion.Euler(item.HandLocalEulerAngles);
            currentHeldObject.transform.localScale = item.HandLocalScale;

            // Vô hiệu hóa script nhặt đồ hoặc collider trên held object để tránh trigger nhặt lại chính nó
            var pickups = currentHeldObject.GetComponentsInChildren<KatanaPickup>(true);
            foreach (var p in pickups)
            {
                Destroy(p);
            }

            var worldPickups = currentHeldObject.GetComponentsInChildren<ItemPickupWorld>(true);
            foreach (var wp in worldPickups)
            {
                Destroy(wp);
            }

            var colliders = currentHeldObject.GetComponentsInChildren<Collider>(true);
            foreach (var col in colliders)
            {
                col.enabled = false;
            }

            Debug.Log($"<color=cyan>[PlayerInventory] Đã cầm {item.ItemName} trên tay ({targetHand.name}) tại Pos={item.HandLocalPosition}</color>");
        }

        private Transform ResolveHandBone(string boneName)
        {
            if (string.IsNullOrEmpty(boneName)) return defaultHandTransform;

            Transform[] children = GetComponentsInChildren<Transform>(true);
            foreach (var child in children)
            {
                if (child.name.Equals(boneName, StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
            }

            return defaultHandTransform;
        }

        private void UpdateCombatStats(ItemConfigSO item)
        {
            if (cachedCombat == null) cachedCombat = GetComponent<PlayerCombat>();
            if (cachedCombat == null) return;

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

        private void ResetCombatStats()
        {
            if (cachedCombat == null) cachedCombat = GetComponent<PlayerCombat>();
            if (cachedCombat == null) return;

            cachedCombat.SetHasWeapon(false);
        }

        /// <summary>
        /// Vứt vật phẩm tại slot ra mặt đất
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

            // Spawn object rơi dưới đất phía trước mặt Player
            GameObject prefabToDrop = item.DropPrefab;
            if (prefabToDrop != null)
            {
                Vector3 dropPos = transform.position + transform.forward * 1.5f + Vector3.up * 0.4f;

                // Raycast tìm mặt đất để vật phẩm tiếp đất chuẩn đẹp
                if (Physics.Raycast(dropPos + Vector3.up * 1f, Vector3.down, out RaycastHit hit, 5f))
                {
                    dropPos.y = hit.point.y + 0.15f;
                }

                GameObject droppedObj = Instantiate(prefabToDrop, dropPos, Quaternion.identity);

                // Gắn ItemPickupWorld để có thể nhặt lại (pickupDelay = 1.5s để không bị tự hút lại ngay)
                var pickup = droppedObj.GetComponent<ItemPickupWorld>();
                if (pickup == null)
                {
                    pickup = droppedObj.AddComponent<ItemPickupWorld>();
                }
                pickup.Init(item, actualDrop, 1.5f);
            }

            Debug.Log($"<color=yellow>[PlayerInventory] Đã vứt: {item.ItemName} x{actualDrop} ra mặt đất.</color>");
            RemoveItemAt(slotIndex, actualDrop);
        }
    }
}
