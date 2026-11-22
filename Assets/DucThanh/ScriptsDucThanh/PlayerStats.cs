using System;
using UnityEngine;

namespace DucThanh
{
    public class PlayerStats : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;

        [Header("Hunger")]
        [SerializeField] private float maxHunger = 100f;
        [SerializeField] private float currentHunger;
        [Tooltip("Số phút trong game để giảm 1 điểm đói")]
        [SerializeField] private float hungerInGameMinutesInterval = 30f;
        [SerializeField] private bool takeDamageWhenStarving = true;
        [SerializeField] private float starvingDamageRate = 2f;

        [Header("Stamina")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float currentStamina;
        [SerializeField] private float sprintDrainRate = 15f;
        [SerializeField] private float staminaRegenRate = 10f;
        [SerializeField] private float regenDelay = 1.0f;

        private PlayerController playerController;
        private float hungerTimer;
        private double lastTotalInGameHours = -1.0;
        private float staminaCooldownTimer;
        private bool isDead;

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

        public bool CanSprint => !isDead && currentStamina > 0.1f;
        public bool IsDead => isDead;

        // Events
        public event Action OnPlayerDied;
        public event Action OnStatsChanged;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();

            currentHealth = maxHealth;
            currentHunger = maxHunger;
            currentStamina = maxStamina;
            isDead = false;
        }

        private void Start()
        {
            if (playerController == null)
                playerController = GetComponentInParent<PlayerController>();

            OnStatsChanged?.Invoke();
        }

        private void Update()
        {
            if (isDead) return;

            if (currentHealth <= 0f)
            {
                Die();
                return;
            }

            HandleHunger();
            HandleStamina();
        }

        private void HandleHunger()
        {
            var dayNight = DayNightController.Instance;
            if (dayNight == null) return;

            double currentTotalInGameHours = dayNight.CurrentDay * 24.0 + dayNight.CurrentHour;

            if (lastTotalInGameHours < 0.0)
            {
                lastTotalInGameHours = currentTotalInGameHours;
                return;
            }

            double deltaInGameHours = currentTotalInGameHours - lastTotalInGameHours;
            lastTotalInGameHours = currentTotalInGameHours;

            if (deltaInGameHours <= 0.0) return;

            float deltaInGameMinutes = (float)(deltaInGameHours * 60.0);

            if (currentHunger > 0f && hungerInGameMinutesInterval > 0f)
            {
                hungerTimer += deltaInGameMinutes;
                if (hungerTimer >= hungerInGameMinutesInterval)
                {
                    int hungerToLose = Mathf.FloorToInt(hungerTimer / hungerInGameMinutesInterval);
                    hungerTimer -= hungerToLose * hungerInGameMinutesInterval;

                    currentHunger = Mathf.Clamp(currentHunger - hungerToLose, 0f, maxHunger);
                    OnStatsChanged?.Invoke();
                }
            }
            else if (currentHunger <= 0f && takeDamageWhenStarving && currentHealth > 0f)
            {
                hungerTimer = 0f;
                ModifyHealth(-starvingDamageRate * Time.deltaTime);
            }
        }

        private void HandleStamina()
        {
            if (playerController == null) return;

            if (playerController.IsMoving && playerController.IsSprinting)
            {
                currentStamina = Mathf.Clamp(currentStamina - sprintDrainRate * Time.deltaTime, 0f, maxStamina);
                staminaCooldownTimer = regenDelay;
                OnStatsChanged?.Invoke();
            }
            else
            {
                if (staminaCooldownTimer > 0f)
                {
                    staminaCooldownTimer -= Time.deltaTime;
                }
                else if (currentStamina < maxStamina)
                {
                    currentStamina = Mathf.Clamp(currentStamina + staminaRegenRate * Time.deltaTime, 0f, maxStamina);
                    OnStatsChanged?.Invoke();
                }
            }
        }

        public void ModifyHealth(float amount)
        {
            if (isDead && amount <= 0f) return;

            currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
            OnStatsChanged?.Invoke();

            if (!isDead && currentHealth <= 0f)
            {
                Die();
            }
            else if (isDead && currentHealth > 0f)
            {
                Revive();
            }
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
            if (isDead) return;

            isDead = true;
            currentHealth = 0f;

            OnStatsChanged?.Invoke();
            OnPlayerDied?.Invoke();
        }

        public void Revive(float healthPercent = 1f)
        {
            isDead = false;
            currentHealth = Mathf.Clamp(maxHealth * healthPercent, 1f, maxHealth);
            OnStatsChanged?.Invoke();
        }
    }
}