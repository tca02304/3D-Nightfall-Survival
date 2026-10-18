using System;

public static class GameEvents
{
    // Kênh truyền số liệu sinh tồn (Máu, Đói, Thể lực)
    public static Action<float, float, float> OnPlayerStatusChanged;

    // Kênh truyền vị trí ô Hotbar đang được chọn (phím 1, 2, 3...)
    public static Action<int> OnHotbarSelected;

    // Kênh truyền thời gian (Ngày sinh tồn, Giờ)
    public static Action<int, string> OnTimeUpdated;
}