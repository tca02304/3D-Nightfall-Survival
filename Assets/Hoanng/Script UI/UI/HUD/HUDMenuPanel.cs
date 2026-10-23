using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDMenuPanel : UiBase
{
    [Header("1. Status Bars")]
    public Slider healthBar;
    public Slider hungerBar;
    public Slider staminaBar;

    [Header("2. Hotbar")]
    public Transform hotbarContainer; // Kéo object ch?a các ô Hotbar vào ?ây
    private HotbarSlotUI[] hotbarSlots; // T? ??ng l?y danh sách các ô

    [Header("3. Time & Day")]
    public TextMeshProUGUI dayText;
    public TextMeshProUGUI timeText;

    [Header("4. Powerups")]
    public Transform powerupsGrid; // Kéo khung Grid Layout Group ch?a k? n?ng vào
    public GameObject powerupPrefab; // Kéo file PREFAB "PowerupSlot" vào ?ây

    private void Start()
    {
        // Khi game b?t ??u, t? ??ng tìm t?t c? các ô HotbarSlotUI ?ang n?m trong HotbarContainer
        if (hotbarContainer != null)
        {
            hotbarSlots = hotbarContainer.GetComponentsInChildren<HotbarSlotUI>();
        }
    }

    // -------------------------------------------------------------
    // ÁP D?NG OBSERVER PATTERN: ??ng ký nghe khi HUD b?t, h?y khi t?t
    // -------------------------------------------------------------
    private void OnEnable()
    {
        GameEvents.OnPlayerStatusChanged += UpdateStatusBars;
        GameEvents.OnHotbarSelected += SelectHotbarSlot;
        GameEvents.OnTimeUpdated += UpdateTimeUI;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerStatusChanged -= UpdateStatusBars;
        GameEvents.OnHotbarSelected -= SelectHotbarSlot;
        GameEvents.OnTimeUpdated -= UpdateTimeUI;
    }

    // -------------------------------------------------------------
    // CÁC HÀM X? LÝ T? ??NG CH?Y KHI CÓ SÓNG PHÁT ??N
    // -------------------------------------------------------------
    private void UpdateStatusBars(float currentHealth, float currentHunger, float currentStamina)
    {
        if (healthBar != null) healthBar.value = currentHealth;
        if (hungerBar != null) hungerBar.value = currentHunger;
        if (staminaBar != null) staminaBar.value = currentStamina;
    }

    private void SelectHotbarSlot(int slotIndex)
    {
        if (hotbarSlots == null || hotbarSlots.Length == 0) return;

        for (int i = 0; i < hotbarSlots.Length; i++)
        {
            // N?u i b?ng v? trí ?ang ch?n thì hàm SetSelected(true) -> vi?n vàng, ng??c l?i -> vi?n tr?ng
            hotbarSlots[i].SetSelected(i == slotIndex);
        }
    }

    private void UpdateTimeUI(int day, string timeString)
    {
        if (dayText != null) dayText.text = day.ToString();
        if (timeText != null) timeText.text = timeString;
    }

    // -------------------------------------------------------------
    // Hàm này dành riêng cho ch?c n?ng nh?t Powerup (Không dùng event vì sinh object liên t?c)
    // -------------------------------------------------------------
    public void AddPowerupUI(Sprite itemIcon, Color rarity, int stackAmount)
    {
        if (powerupPrefab == null || powerupsGrid == null) return;

        // ?úc ra m?t Prefab m?i và ném vào khung Grid
        GameObject newPowerup = Instantiate(powerupPrefab, powerupsGrid);

        // C?p nh?t d? li?u cho Prefab v?a ?úc
        PowerupSlotUI slotScript = newPowerup.GetComponent<PowerupSlotUI>();
        if (slotScript != null)
        {
            slotScript.SetupSlot(itemIcon, rarity, stackAmount);
        }
    }
}