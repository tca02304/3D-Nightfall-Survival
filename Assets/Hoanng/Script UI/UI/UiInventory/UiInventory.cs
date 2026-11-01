using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UiInventory : UiBase
{
    [Header("1. Lưới túi đồ chính")]
    public Transform inventoryGrid;
    private InventorySlotUI[] inventorySlots;

    [Header("2. Lưới 7 ô Hotbar")]
    public Transform hotbarGrid;
    private InventorySlotUI[] hotbarSlots;

    [Header("3. Lưới Trang Bị (Equipment)")]
    public Transform equipmentGrid; // Kéo object EquipmentSlots vào đây
    private InventorySlotUI[] equipmentSlots;

    [Header("4. Bảng Chỉ Số (Stats)")]
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI shieldText;
    public TextMeshProUGUI armorText;
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI attackText;

    [Header("5. Thông báo Chế tạo")]
    public TextMeshProUGUI warningText;
    public GameObject gob;
    private void Awake()
    {
        // Tự động tìm các ô (Slot) nằm bên trong các lưới
        if (inventoryGrid != null) inventorySlots = inventoryGrid.GetComponentsInChildren<InventorySlotUI>();
        if (hotbarGrid != null) hotbarSlots = hotbarGrid.GetComponentsInChildren<InventorySlotUI>();
        if (equipmentGrid != null) equipmentSlots = equipmentGrid.GetComponentsInChildren<InventorySlotUI>();
    }

    public override void Show()
    {
        base.Show();
        Debug.Log("Đã mở túi đồ!");
    }

    public override void Hide()
    {
        base.Hide();
        Debug.Log("Đã đóng túi đồ!");
    }

    // --- ĐĂNG KÝ OBSERVER PATTERN ---
    private void OnEnable()
    {
        GameEvents.OnInventoryUpdated += RefreshInventoryUI;
        GameEvents.OnCraftingFailed += ShowCraftingWarning;

        // Đăng ký nghe thêm Trang bị và Chỉ số
        GameEvents.OnEquipmentUpdated += RefreshEquipmentUI;
        GameEvents.OnPlayerStatsUpdated += UpdateStatsUI;
    }

    private void OnDisable()
    {
        GameEvents.OnInventoryUpdated -= RefreshInventoryUI;
        GameEvents.OnCraftingFailed -= ShowCraftingWarning;

        // Hủy nghe
        GameEvents.OnEquipmentUpdated -= RefreshEquipmentUI;
        GameEvents.OnPlayerStatsUpdated -= UpdateStatsUI;

        if (warningText != null) warningText.text = "";
    }

    // --- CÁC HÀM XỬ LÝ GIAO DIỆN ---

    // 1. Cập nhật Balo và Hotbar (Giữ nguyên)
    private void RefreshInventoryUI(List<InventorySlotData> inventoryData, List<InventorySlotData> hotbarData)
    {
        if (inventorySlots != null)
        {
            for (int i = 0; i < inventorySlots.Length; i++)
            {
                if (inventoryData != null && i < inventoryData.Count)
                    inventorySlots[i].UpdateSlot(inventoryData[i]);
                else
                    inventorySlots[i].UpdateSlot(new InventorySlotData());
            }
        }

        if (hotbarSlots != null)
        {
            for (int i = 0; i < hotbarSlots.Length; i++)
            {
                if (hotbarData != null && i < hotbarData.Count)
                    hotbarSlots[i].UpdateSlot(hotbarData[i]);
                else
                    hotbarSlots[i].UpdateSlot(new InventorySlotData());
            }
        }
    }

    // 2. Cập nhật các ô Trang bị (Vũ khí, Mũ, Áo...)
    private void RefreshEquipmentUI(List<InventorySlotData> equipmentData)
    {
        if (equipmentSlots != null)
        {
            for (int i = 0; i < equipmentSlots.Length; i++)
            {
                if (equipmentData != null && i < equipmentData.Count)
                    equipmentSlots[i].UpdateSlot(equipmentData[i]);
                else
                    equipmentSlots[i].UpdateSlot(new InventorySlotData());
            }
        }
    }

    // 3. Cập nhật các dòng Text Chỉ số (Stats)
    private void UpdateStatsUI(int hp, int shield, int armor, int speed, int attack)
    {
        if (hpText != null) hpText.text = hp.ToString();
        if (shieldText != null) shieldText.text = shield.ToString();
        if (armorText != null) armorText.text = armor.ToString();
        if (speedText != null) speedText.text = speed.ToString();
        if (attackText != null) attackText.text = attack.ToString();
    }

    private void ShowCraftingWarning(string message)
    {
        if (warningText != null)
        {
            warningText.text = message;
            warningText.color = Color.red;
        }
    }
}