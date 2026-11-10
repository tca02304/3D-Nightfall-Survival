using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DucThanh
{
    /// <summary>
    /// Component quản lý hiển thị giao diện thanh Hotbar / Túi đồ của Player.
    /// Tự động cập nhật icon, số lượng và highlight ô đang được trang bị trên tay.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerInventoryUI : MonoBehaviour
    {
        [Header("Target Inventory")]
        [Tooltip("PlayerInventory cần hiển thị. Nếu để trống sẽ tự động tìm")]
        [SerializeField] private PlayerInventory playerInventory;

        [Header("Slot Containers")]
        [Tooltip("Prefab của từng ô Slot UI (Nếu muốn tự sinh dynamically)")]
        [SerializeField] private GameObject slotUIPrefab;

        [Tooltip("Transform chứa các ô Slot (VD: Horizontal Layout Group)")]
        [SerializeField] private Transform slotsParent;

        [Header("Manual Slot References (Tùy chọn gán sẵn danh sách Slot)")]
        [SerializeField] private List<InventorySlotUIElement> uiSlots = new List<InventorySlotUIElement>();

        private void Start()
        {
            if (playerInventory == null)
            {
                playerInventory = FindFirstObjectByType<PlayerInventory>();
            }

            if (playerInventory != null)
            {
                playerInventory.OnInventoryChanged += RefreshUI;
                playerInventory.OnItemEquipped += OnItemEquipped;
                playerInventory.OnItemUnequipped += OnItemUnequipped;
            }

            RefreshUI();
        }

        private void OnDestroy()
        {
            if (playerInventory != null)
            {
                playerInventory.OnInventoryChanged -= RefreshUI;
                playerInventory.OnItemEquipped -= OnItemEquipped;
                playerInventory.OnItemUnequipped -= OnItemUnequipped;
            }
        }

        private void OnItemEquipped(ItemConfigSO item)
        {
            RefreshUI();
        }

        private void OnItemUnequipped()
        {
            RefreshUI();
        }

        /// <summary>
        /// Làm mới toàn bộ UI túi đồ
        /// </summary>
        public void RefreshUI()
        {
            if (playerInventory == null) return;

            var inventorySlots = playerInventory.Slots;
            int count = inventorySlots.Count;

            // Nếu uiSlots chưa đủ và có slotsParent
            if (uiSlots.Count < count && slotsParent != null && slotUIPrefab != null)
            {
                int needed = count - uiSlots.Count;
                for (int i = 0; i < needed; i++)
                {
                    GameObject go = Instantiate(slotUIPrefab, slotsParent);
                    var el = go.GetComponent<InventorySlotUIElement>();
                    if (el != null)
                    {
                        uiSlots.Add(el);
                    }
                }
            }

            // Bind dữ liệu
            for (int i = 0; i < uiSlots.Count; i++)
            {
                if (i < count)
                {
                    bool isEquipped = (i == playerInventory.EquippedSlotIndex);
                    uiSlots[i].Bind(i, inventorySlots[i], isEquipped, this);
                    uiSlots[i].gameObject.SetActive(true);
                }
                else
                {
                    uiSlots[i].gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Được gọi khi click vào ô Slot UI
        /// </summary>
        public void OnSlotClicked(int index)
        {
            if (playerInventory != null)
            {
                playerInventory.ToggleEquipSlot(index);
            }
        }
    }

    /// <summary>
    /// Đại diện cho hiển thị của 1 ô Slot trên Canvas
    /// </summary>
    public class InventorySlotUIElement : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI quantityText;
        [SerializeField] private Image highlightBorder;
        [SerializeField] private Button clickButton;

        private int slotIndex;
        private PlayerInventoryUI parentUI;

        private void Awake()
        {
            if (clickButton == null)
            {
                clickButton = GetComponent<Button>();
            }

            if (clickButton != null)
            {
                clickButton.onClick.AddListener(OnClick);
            }
        }

        public void Bind(int index, InventorySlot slot, bool isEquipped, PlayerInventoryUI parent)
        {
            slotIndex = index;
            parentUI = parent;

            if (slot == null || slot.IsEmpty)
            {
                if (iconImage != null)
                {
                    iconImage.sprite = null;
                    iconImage.enabled = false;
                }
                if (quantityText != null)
                {
                    quantityText.text = "";
                }
            }
            else
            {
                if (iconImage != null)
                {
                    iconImage.sprite = slot.Item.Icon;
                    iconImage.enabled = slot.Item.Icon != null;
                }
                if (quantityText != null)
                {
                    quantityText.text = slot.Quantity > 1 ? slot.Quantity.ToString() : "";
                }
            }

            if (highlightBorder != null)
            {
                highlightBorder.enabled = isEquipped;
            }
        }

        private void OnClick()
        {
            if (parentUI != null)
            {
                parentUI.OnSlotClicked(slotIndex);
            }
        }
    }
}
