using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DucThanh
{
    public class PlayerStatsUI : MonoBehaviour
    {
        [Header("Target Player")]
        [Tooltip("Tham chiếu đến PlayerStats. Nếu để trống sẽ tự tìm")]
        [SerializeField] private PlayerStats playerStats;

        [Header("Health UI")]
        [SerializeField] private Image healthBarFill;
        [SerializeField] private Slider healthSlider;
        [SerializeField] private TextMeshProUGUI healthText;

        [Header("Hunger UI")]
        [SerializeField] private Image hungerBarFill;
        [SerializeField] private Slider hungerSlider;
        [SerializeField] private TextMeshProUGUI hungerText;

        [Header("Stamina UI")]
        [SerializeField] private Image staminaBarFill;
        [SerializeField] private Slider staminaSlider;
        [SerializeField] private TextMeshProUGUI staminaText;

        [Header("Optional Smooth Animation")]
        [SerializeField] private bool smoothTransition = true;
        [SerializeField] private float smoothSpeed = 10f;

        private float currentHealthRatio = 1f;
        private float currentHungerRatio = 1f;
        private float currentStaminaRatio = 1f;

        private void Start()
        {
            if (playerStats == null)
            {
                playerStats = Object.FindFirstObjectByType<PlayerStats>();
            }

            if (playerStats != null)
            {
                currentHealthRatio = playerStats.HealthPercent;
                currentHungerRatio = playerStats.HungerPercent;
                currentStaminaRatio = playerStats.StaminaPercent;
                UpdateUI(true);
            }
        }

        private void Update()
        {
            if (playerStats == null)
            {
                playerStats = Object.FindFirstObjectByType<PlayerStats>();
                if (playerStats == null) return;
            }

            UpdateUI(!smoothTransition);
        }

        private void UpdateUI(bool instant)
        {
            float targetHealth = playerStats.HealthPercent;
            float targetHunger = playerStats.HungerPercent;
            float targetStamina = playerStats.StaminaPercent;

            if (instant)
            {
                currentHealthRatio = targetHealth;
                currentHungerRatio = targetHunger;
                currentStaminaRatio = targetStamina;
            }
            else
            {
                currentHealthRatio = Mathf.Lerp(currentHealthRatio, targetHealth, smoothSpeed * Time.deltaTime);
                currentHungerRatio = Mathf.Lerp(currentHungerRatio, targetHunger, smoothSpeed * Time.deltaTime);
                currentStaminaRatio = Mathf.Lerp(currentStaminaRatio, targetStamina, smoothSpeed * Time.deltaTime);
            }

            // Cập nhật thanh Máu (Health)
            if (healthBarFill != null) healthBarFill.fillAmount = currentHealthRatio;
            if (healthSlider != null) healthSlider.value = currentHealthRatio;
            if (healthText != null) healthText.text = $"{Mathf.CeilToInt(playerStats.CurrentHealth)} / {Mathf.CeilToInt(playerStats.MaxHealth)}";

            // Cập nhật thanh Đói (Hunger)
            if (hungerBarFill != null) hungerBarFill.fillAmount = currentHungerRatio;
            if (hungerSlider != null) hungerSlider.value = currentHungerRatio;
            if (hungerText != null) hungerText.text = $"{Mathf.CeilToInt(playerStats.CurrentHunger)} / {Mathf.CeilToInt(playerStats.MaxHunger)}";

            // Cập nhật thanh Thể lực (Stamina)
            if (staminaBarFill != null) staminaBarFill.fillAmount = currentStaminaRatio;
            if (staminaSlider != null) staminaSlider.value = currentStaminaRatio;
            if (staminaText != null) staminaText.text = $"{Mathf.CeilToInt(playerStats.CurrentStamina)} / {Mathf.CeilToInt(playerStats.MaxStamina)}";
        }

        public void SetPlayerStats(PlayerStats stats)
        {
            playerStats = stats;
        }
    }
}
