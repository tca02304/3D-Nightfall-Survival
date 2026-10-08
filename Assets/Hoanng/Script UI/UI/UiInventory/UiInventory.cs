using UnityEngine;

public class UiInventory : UiBase
{
    public override void Show()
    {
        base.Show(); // Bật gameObject (SetActive(true))

        // Sau này bạn có thể thêm code cập nhật danh sách item ở đây
        Debug.Log("Đã mở túi đồ!");
    }

    public override void Hide()
    {
        base.Hide(); // Tắt gameObject (SetActive(false))
        Debug.Log("Đã đóng túi đồ!");
    }
}
