using UnityEngine;

public class UITester : MonoBehaviour
{
    [Header("1. Test Thanh Trạng Thái (Kéo trượt)")]
    [Range(0, 100)] public float testHealth = 100f;
    [Range(0, 100)] public float testHunger = 100f;
    [Range(0, 100)] public float testStamina = 100f;

    [Header("2. Test Powerup (Gắn 1 ảnh bất kỳ vào đây)")]
    public Sprite testPowerupIcon;

    void Update()
    {
        // 1. Phát sóng liên tục thông số sinh tồn dựa trên thanh trượt Inspector
        GameEvents.OnPlayerStatusChanged?.Invoke(testHealth, testHunger, testStamina);

        // 2. Giả lập bấm phím 1, 2, 3 để chọn Hotbar
        if (Input.GetKeyDown(KeyCode.Alpha1)) GameEvents.OnHotbarSelected?.Invoke(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) GameEvents.OnHotbarSelected?.Invoke(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) GameEvents.OnHotbarSelected?.Invoke(2);

        // 3. Giả lập nhặt được Powerup khi bấm phím P
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (UIManager.Instance != null && UIManager.Instance.uiHud != null)
            {
                UIManager.Instance.uiHud.AddPowerupUI(testPowerupIcon, Color.magenta, 5);
            }
        }
    }
}