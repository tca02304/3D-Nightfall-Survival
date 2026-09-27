using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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
        [SerializeField] private float starvingDamageRate = 2f; // Trừ 2 máu / giây khi đói cạn kiệt

        [Header("Stamina Settings")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float currentStamina;
        [Tooltip("Tốc độ trừ thể lực khi đi bộ (/giây)")]
        [SerializeField] private float walkDrainRate = 1.5f;
        [Tooltip("Tốc độ trừ thể lực khi chạy sprint (/giây)")]
        [SerializeField] private float sprintDrainRate = 12f;
        [Tooltip("Tốc độ hồi thể lực khi đứng yên (/giây)")]
        [SerializeField] private float staminaRegenRate = 8f;
        [Tooltip("Thời gian chờ trước khi bắt đầu hồi thể lực sau khi dừng dùng")]
        [SerializeField] private float regenDelay = 1.5f;

        private PlayerController playerController;
        private float hungerTimer = 0f;
        private float staminaCooldownTimer = 0f;

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

        public bool CanSprint => currentStamina > 5f;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();

            currentHealth = maxHealth;
            currentHunger = maxHunger;
            currentStamina = maxStamina;
        }

        private void Update()
        {
            HandleDebugInput();
            HandleHunger();
            HandleStamina();
        }

        private void HandleDebugInput()
        {
            bool kPressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
            {
                kPressed = true;
            }
#endif
            if (!kPressed)
            {
                try
                {
                    if (Input.GetKeyDown(KeyCode.K))
                    {
                        kPressed = true;
                    }
                }
                catch
                {
                    // Tránh lỗi nếu Legacy Input bị tắt trong ProjectSettings
                }
            }

            if (kPressed)
            {
                ModifyHealth(-10f);
                Debug.Log($"[PlayerStats] Phím K được nhấn: -10 Máu (Còn lại: {currentHealth}/{maxHealth})");
            }
        }

        private void HandleHunger()
        {
            hungerTimer += Time.deltaTime;
            if (hungerTimer >= hungerInterval)
            {
                hungerTimer = 0f;
                ModifyHunger(-1f);
            }

            // Nếu đói bằng 0 thì bị trừ máu dần
            if (takeDamageWhenStarving && currentHunger <= 0f)
            {
                ModifyHealth(-starvingDamageRate * Time.deltaTime);
            }
        }

        private void HandleStamina()
        {
            bool isMoving = false;
            bool isSprinting = false;

            if (playerController != null)
            {
                isMoving = playerController.IsMoving;
                isSprinting = playerController.IsSprinting && isMoving;
            }

            if (isSprinting)
            {
                ModifyStamina(-sprintDrainRate * Time.deltaTime);
                staminaCooldownTimer = regenDelay;
            }
            else if (isMoving)
            {
                ModifyStamina(-walkDrainRate * Time.deltaTime);
                staminaCooldownTimer = regenDelay;
            }
            else
            {
                // Khi đứng yên thì hồi phục thể lực sau khoảng delay
                if (staminaCooldownTimer > 0f)
                {
                    staminaCooldownTimer -= Time.deltaTime;
                }
                else
                {
                    ModifyStamina(staminaRegenRate * Time.deltaTime);
                }
            }
        }

        public void ModifyHealth(float amount)
        {
            currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
            if (currentHealth <= 0f)
            {
                OnDeath();
            }
        }

        public void ModifyHunger(float amount)
        {
            currentHunger = Mathf.Clamp(currentHunger + amount, 0f, maxHunger);
        }

        public void ModifyStamina(float amount)
        {
            currentStamina = Mathf.Clamp(currentStamina + amount, 0f, maxStamina);
        }

        private void OnDeath()
        {
            Debug.Log("[PlayerStats] Player đã cạn máu!");
        }
    }
}
