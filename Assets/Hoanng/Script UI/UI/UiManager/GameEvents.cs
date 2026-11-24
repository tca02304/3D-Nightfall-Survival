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
    //public static Action OnResumeGame;
    public static Action OnRestartGame;
    public static Action OnReturnToMainMenu;

    // Kênh báo cho UI hiện/ẩn phím tương tác (Ví dụ: "Nhấn F để nhặt Gỗ")
    public static Action<bool, string> OnInteractionStateChanged;

    

    
    // Khai báo kênh sự kiện cho Hotbar: Truyền đi 1 số nguyên (int) là vị trí ô được click
    public static Action<int> OnHotbarSlotSelected;
    // Sự kiện truyền đi tiến độ tải (float từ 0.0f đến 1.0f)
    public static System.Action<float> OnLoadingProgressUpdated;
    public static Action<string> OnTryCraftItem;
    public static Action<bool, string> OnCraftResult;
    //public static Action OnInventoryUpdated;
    // --- DAY/NIGHT & WAVE EVENTS ---

    // Cập nhật ngày sinh tồn (truyền vào số ngày)
    public static Action<int> OnDayChanged;

    // Cảnh báo khi màn đêm buông xuống
    public static Action OnNightStarted;

    // Cảnh báo khi Wave bắt đầu (truyền vào số thứ tự Wave)
    public static Action<int> OnWaveStarted;

    // Thông báo khi Wave kết thúc (dọn sạch quái)
    public static Action OnWaveEnded;

    // Yêu cầu bật màn hình Settings (dùng khi chuyển từ Home sang SPLASH)
    public static System.Action OnRequestSettingsUI;

    // Yêu cầu tạm dừng/tiếp tục game
    public static System.Action OnPauseGame;
    public static System.Action OnResumeGame;

    // Thêm dòng này vào trong class GameEvents
    public static System.Action OnRequestCraftingUI;
    // Thêm vào file GameEvents.cs
    public static System.Action OnRequestInventoryUI;
    public static System.Action OnToggleInventoryUI;
}