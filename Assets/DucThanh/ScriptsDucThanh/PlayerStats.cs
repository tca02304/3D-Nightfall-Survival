using UnityEngine;

namespace DucThanh
{
    public class PlayerStats : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;
        [Tooltip("Thời gian chờ trước khi Destroy Player sau khi hết máu")]
        [SerializeField] private float destroyDelayOnDeath = 2.0f;

#pragma warning disable 0414, 0169
        [Header("Hunger Settings")]
        [SerializeField] private float maxHunger = 100f;
        [SerializeField] private float currentHunger;
        [SerializeField] private float hungerInterval = 10f;
        [SerializeField] private bool takeDamageWhenStarving = true;
        [SerializeField] private float starvingDamageRate = 2f;

        [Header("Stamina Settings")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float currentStamina;
        [SerializeField] private float walkDrainRate = 1.5f;
        [SerializeField] private float sprintDrainRate = 12f;
        [SerializeField] private float staminaRegenRate = 8f;
        [SerializeField] private float regenDelay = 1.5f;

        private float hungerTimer = 0f;
        private float staminaCooldownTimer = 0f;
#pragma warning restore 0414, 0169

        private PlayerMove playerController;
        private bool isDead = false;

        // Getters
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float HealthPercent => Mathf.Clamp01(currentHealth / maxHealth);

        public float MaxHunger => maxHunger;
        public float CurrentHunger => currentHunger;
        public float HungerPercent => Mathf.Clamp01(currentHunger / maxHunger);

        public float MaxStamina => maxStamina;
        public float CurrentStamina => currentStamina;
        public float StaminaPercent => Mathf.Clamp01(currentStamina / maxStamina);

        public bool CanSprint => !isDead;
        public bool IsDead => isDead;

        // Events
        public event System.Action OnPlayerDied;
        public event System.Action OnStatsChanged;

        private void Awake()
        {
            playerController = GetComponent<PlayerMove>();

            currentHealth = maxHealth;
            currentHunger = maxHunger;
            currentStamina = maxStamina;
            isDead = false;
        }

        private void Start()
        {
            OnStatsChanged?.Invoke();
        }

        public void ModifyHealth(float amount)
        {
            if (isDead && amount <= 0f) return;

            currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
            OnStatsChanged?.Invoke();

            if (amount < 0f)
            {
                Debug.Log($"<color=red><b>[Player Health]</b> Player bị đánh trúng! Mất {-amount:F1} máu. <b>Máu hiện tại: {currentHealth:F0} / {maxHealth:F0} HP</b></color>");
            }
            else if (amount > 0f)
            {
                Debug.Log($"<color=green><b>[Player Health]</b> Player được hồi {amount:F1} máu. Máu hiện tại: {currentHealth:F0} / {maxHealth:F0} HP</color>");
            }

            if (!isDead && currentHealth <= 0f)
            {
                Die();
            }
            else if (isDead && currentHealth > 0f)
            {
                Revive();
            }
        }

        public void ModifyHealths(float amount)
        {
            ModifyHealth(amount);
        }

        public void ModifyHunger(float amount)
        {
            if (isDead) return;
            currentHunger = Mathf.Clamp(currentHunger + amount, 0f, maxHunger);
            OnStatsChanged?.Invoke();
        }

        public void ModifyStamina(float amount)
        {
            if (isDead) return;
            currentStamina = Mathf.Clamp(currentStamina + amount, 0f, maxStamina);
            OnStatsChanged?.Invoke();
        }

        public bool ConsumeStamina(float amount)
        {
            if (isDead) return false;
            if (amount <= 0f) return true;
            if (currentStamina < amount) return false;

            currentStamina = Mathf.Clamp(currentStamina - amount, 0f, maxStamina);
            staminaCooldownTimer = regenDelay;
            OnStatsChanged?.Invoke();
            return true;
        }

        private void Die()
        {
            isDead = true;
            currentHealth = 0f;
            Debug.Log("<color=red><b>[Player] Player đã chết! Quái ngừng tấn công, hủy Player sau 2s.</b></color>");
            OnPlayerDied?.Invoke();

            // Destroy Player sau 2 giây
            Destroy(gameObject, destroyDelayOnDeath);
        }

        public void Revive(float healthPercent = 1f)
        {
            isDead = false;
            currentHealth = Mathf.Clamp(maxHealth * healthPercent, 1f, maxHealth);
            Debug.Log($"[Player] Player đã hồi sinh với {currentHealth} máu!");
            OnStatsChanged?.Invoke();
        }
    }
}