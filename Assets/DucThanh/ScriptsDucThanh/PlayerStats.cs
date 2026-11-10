using UnityEngine;

namespace DucThanh
{
    public class PlayerStats : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;

        [Header("Hunger Settings")]
        [Tooltip("Tổng điểm đói tối đa")]
        [SerializeField] private float maxHunger = 100f;
        [SerializeField] private float currentHunger;

        [Tooltip("Thời gian để giảm 1 điểm đói (mặc định 10s)")]
        [SerializeField] private float hungerInterval = 10f;

        [Tooltip("Khi đói về 0 thì có trừ máu dần theo thời gian không")]
        [SerializeField] private bool takeDamageWhenStarving = true;

        [Tooltip("Số máu bị trừ / giây khi điểm đói cạn kiệt (về 0)")]
        [SerializeField] private float starvingDamageRate = 2f;

        [Header("Stamina Settings")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float currentStamina;
        [SerializeField] private float walkDrainRate = 1.5f;
        [SerializeField] private float sprintDrainRate = 15f;
        [SerializeField] private float staminaRegenRate = 10f;
        [SerializeField] private float regenDelay = 1.0f;

        private float hungerTimer = 0f;
        private float staminaCooldownTimer = 0f;

        private PlayerController playerController;
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

        public bool CanSprint => !isDead && currentStamina > 0.1f;
        public bool IsDead => isDead;

        // Events
        public event System.Action OnPlayerDied;
        public event System.Action OnStatsChanged;

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
            {
                playerController = GetComponentInParent<PlayerController>();
            }

            if (playerController == null)
            {
                Debug.LogError("<color=red>[PlayerStats] KHÔNG TÌM THẤY PlayerController trên GameObject này!</color>");
            }

            OnStatsChanged?.Invoke();
        }

        private void Update()
        {
            if (isDead) return;

            HandleHungerLogic();
            HandleStaminaLogic();
        }

        /// <summary>
        /// Logic tự động giảm điểm đói và mất máu khi chết đói
        /// </summary>
        private void HandleHungerLogic()
        {
            // 1. Trừ điểm đói theo khoảng thời gian hungerInterval
            if (currentHunger > 0f)
            {
                hungerTimer += Time.deltaTime;
                if (hungerTimer >= hungerInterval)
                {
                    hungerTimer = 0f;
                    currentHunger = Mathf.Clamp(currentHunger - 1f, 0f, maxHunger);
                    OnStatsChanged?.Invoke();
                }
            }
            // 2. Khi đói về 0, nếu bật takeDamageWhenStarving sẽ bị trừ máu từ từ theo thời gian
            else if (takeDamageWhenStarving && currentHealth > 0f)
            {
                ModifyHealth(-starvingDamageRate * Time.deltaTime);
            }
        }

        /// <summary>
        /// Logic tiêu hao và tự hồi thể lực (Stamina)
        /// </summary>
        private void HandleStaminaLogic()
        {
            if (playerController == null) return;

            // Kiểm tra PlayerController đang di chuyển VÀ đang bấm Sprint
            if (playerController.IsMoving && playerController.IsSprinting)
            {
                currentStamina = Mathf.Clamp(currentStamina - sprintDrainRate * Time.deltaTime, 0f, maxStamina);
                staminaCooldownTimer = regenDelay;
                OnStatsChanged?.Invoke();
            }
            else
            {
                // Tự hồi thể lực khi nhả phím chạy / đứng yên
                if (staminaCooldownTimer > 0)
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
            Debug.Log("<color=red><b>[Player] Player đã chết!</b></color>");
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