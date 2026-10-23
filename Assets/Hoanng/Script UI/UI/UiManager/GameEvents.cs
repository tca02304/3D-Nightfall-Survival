using System;
using System.Collections.Generic;
using UnityEngine;


public static class GameEvents
{
    // Kênh truyền số liệu sinh tồn (Máu, Đói, Thể lực)
    public static Action<float, float, float> OnPlayerStatusChanged;

    // Kênh truyền vị trí ô Hotbar đang được chọn (phím 1, 2, 3...)
    public static Action<int> OnHotbarSelected;

    // Kênh truyền thời gian (Ngày sinh tồn, Giờ)
    public static Action<int, string> OnTimeUpdated;
    // --- SỰ KIỆN MỚI CHO INVENTORY ---

    // Kênh INVENTORY (Sử dụng trực tiếp class InventorySlotData của đồng đội)
    public static Action<List<InventorySlotData>, List<InventorySlotData>> OnInventoryUpdated;

    // Kênh thông báo khi chế tạo thất bại (thiếu nguyên liệu)
    public static Action<string> OnCraftingFailed;
    // Kênh cập nhật các ô Trang bị (Equipment)
    public static Action<List<InventorySlotData>> OnEquipmentUpdated;

    // Kênh cập nhật bảng Chỉ số (HP, Shield, Armor, Speed, Attack)
    public static Action<int, int, int, int, int> OnPlayerStatsUpdated;

    // --- SỰ KIỆN CHO PAUSE & GAME OVER ---

    // Member 1 (Player) sẽ phát sóng sự kiện này khi máu <= 0
    public static Action OnPlayerDied;

    // UI của bạn sẽ phát các sự kiện này khi người chơi bấm nút, Member 1 sẽ lắng nghe
    public static Action OnResumeGame;
    public static Action OnRestartGame;
    public static Action OnReturnToMainMenu;

    // Kênh báo cho UI hiện/ẩn phím tương tác (Ví dụ: "Nhấn F để nhặt Gỗ")
    public static Action<bool, string> OnInteractionStateChanged;

    // Kênh cập nhật tiến độ nhiệm vụ (Ví dụ: "Tiêu diệt Slime", "2/5")
    public static Action<string, string> OnQuestProgressUpdated;

    // Kênh báo hoàn thành nhiệm vụ để hiện Popup chúc mừng
    public static Action<string> OnQuestCompleted;

}