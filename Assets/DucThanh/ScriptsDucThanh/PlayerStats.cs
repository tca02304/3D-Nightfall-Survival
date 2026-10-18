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

        public bool CanSprint => !isDead;
        public bool IsDead => isDead;

        public event System.Action OnPlayerDied;

        private bool isDead = false;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();

            currentHealth = maxHealth;
            currentHunger = maxHunger;
            currentStamina = maxStamina;
            isDead = false;
        }

        private void Update()
        {
            HandleDebugInput();

            if (isDead) return;

            // Đã bỏ tính năng tự trừ thức ăn và trừ thể lực theo yêu cầu
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
            }
        }

        private void HandleHunger()
        {
            // Đã vô hiệu hóa tự giảm thức ăn
        }

        private void HandleStamina()
        {
            // Không tiêu hao thể lực khi chạy/di chuyển
        }

        public void ModifyHealth(float amount)
        {
            if (isDead && amount <= 0f) return;

            currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);

            // In rõ ràng ra Console mỗi khi Player mất/thay đổi máu
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

        public void ModifyHunger(float amount)
        {
            if (isDead) return;
            currentHunger = Mathf.Clamp(currentHunger + amount, 0f, maxHunger);
        }

        public void ModifyStamina(float amount)
        {
            if (isDead) return;
            currentStamina = Mathf.Clamp(currentStamina + amount, 0f, maxStamina);
        }

        /// <summary>
        /// Tiêu hao thể lực (stamina). Trả về true nếu đủ thể lực và tiêu hao thành công.
        /// </summary>
        public bool ConsumeStamina(float amount)
        {
            if (isDead) return false;
            if (amount <= 0f) return true;
            if (currentStamina < amount) return false;

            currentStamina = Mathf.Clamp(currentStamina - amount, 0f, maxStamina);
            staminaCooldownTimer = regenDelay;
            return true;
        }

        private void Die()
        {
            isDead = true;
            currentHealth = 0f;
            Debug.Log("<color=red><b>[Player] Player đã chết!</b></color>");
            OnPlayerDied?.Invoke();
        }

        public void Revive(float healthPercent = 1f)
        {
            isDead = false;
            currentHealth = Mathf.Clamp(maxHealth * healthPercent, 1f, maxHealth);
            Debug.Log($"[Player] Player đã hồi sinh với {currentHealth} máu!");
        }
    }
}
