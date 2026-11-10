using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DucThanh
{
    /// <summary>
    /// Component quản lý hiển thị các thanh chỉ số Health, Hunger, Stamina trên UI.
    /// </summary>
    public class PlayerUI : MonoBehaviour
    {
        [Header("Player Reference")]
        [SerializeField] private PlayerStats playerStats;

        [Header("Health UI")]
        [SerializeField] private Image healthFillImage;
        [SerializeField] private TextMeshProUGUI healthText;

        [Header("Hunger UI")]
        [SerializeField] private Image hungerFillImage;
        [SerializeField] private TextMeshProUGUI hungerText;

        [Header("Stamina UI")]
        [SerializeField] private Image staminaFillImage;
        [SerializeField] private TextMeshProUGUI staminaText;

        [Header("Settings")]
        [Tooltip("Tốc độ làm mượt thanh chỉ số (Lerp speed)")]
        [SerializeField] private float smoothSpeed = 10f;

        private void Start()
        {
            if (playerStats == null)
            {
                playerStats = FindAnyObjectByType<PlayerStats>();
            }

            // Đăng ký sự kiện cập nhật ngay lập tức khi thay đổi chỉ số
            if (playerStats != null)
            {
                playerStats.OnStatsChanged += UpdateUI;
                UpdateUI();
            }
        }

        private void OnDestroy()
        {
            if (playerStats != null)
            {
                playerStats.OnStatsChanged -= UpdateUI;
            }
        }

        private void Update()
        {
            if (playerStats == null) return;

            // Làm mượt fillAmount của thanh UI theo thời gian
            if (healthFillImage != null)
            {
                healthFillImage.fillAmount = Mathf.Lerp(healthFillImage.fillAmount, playerStats.HealthPercent, Time.deltaTime * smoothSpeed);
            }

            if (hungerFillImage != null)
            {
                hungerFillImage.fillAmount = Mathf.Lerp(hungerFillImage.fillAmount, playerStats.HungerPercent, Time.deltaTime * smoothSpeed);
            }

            if (staminaFillImage != null)
            {
                staminaFillImage.fillAmount = Mathf.Lerp(staminaFillImage.fillAmount, playerStats.StaminaPercent, Time.deltaTime * smoothSpeed);
            }
        }

        public void UpdateUI()
        {
            if (playerStats == null) return;

            // Cập nhật nội dung Text (100/100)
            if (healthText != null)
                healthText.text = $"{Mathf.CeilToInt(playerStats.CurrentHealth)}/{Mathf.CeilToInt(playerStats.MaxHealth)}";

            if (hungerText != null)
                hungerText.text = $"{Mathf.CeilToInt(playerStats.CurrentHunger)}/{Mathf.CeilToInt(playerStats.MaxHunger)}";

            if (staminaText != null)
                staminaText.text = $"{Mathf.CeilToInt(playerStats.CurrentStamina)}/{Mathf.CeilToInt(playerStats.MaxStamina)}";
        }
    }
}