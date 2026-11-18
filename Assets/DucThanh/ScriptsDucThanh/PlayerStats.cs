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

        [Header("Death Settings (Cài đặt khi chết)")]
        [Tooltip("Thời gian chạy animation Die trước khi đóng băng (0.2s theo yêu cầu)")]
        [SerializeField] private float dieAnimPlayDuration = 0.2f;

        [Tooltip("Tổng thời gian từ khi chết tới lúc Destroy GameObject Player (2s theo yêu cầu)")]
        [SerializeField] private float destroyDelayAfterDeath = 2.0f;

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

            // Đảm bảo Player có PlayerInventory và PlayerCombat để nhặt và sử dụng vũ khí
            if (GetComponent<PlayerInventory>() == null)
            {
                gameObject.AddComponent<PlayerInventory>();
            }

            if (GetComponent<PlayerCombat>() == null)
            {
                gameObject.AddComponent<PlayerCombat>();
            }
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

            if (currentHealth <= 0f)
            {
                Die();
                return;
            }

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
            if (isDead) return;
            isDead = true;
            currentHealth = 0f;
            Debug.Log("<color=red><b>[Player] Player đã chết! Quái dừng tấn công, chạy animation Die (0.2s) và destroy sau 2s.</b></color>");

            GameObject rootObj = (playerController != null) ? playerController.gameObject : gameObject;
            if (transform.root != null)
            {
                rootObj = transform.root.gameObject;
            }

            // 1. Đổi tag và tên Player để toàn bộ AI quái mất dấu mục tiêu
            try
            {
                rootObj.tag = "Untagged";
                rootObj.name = "Player_Dead";
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[PlayerStats] Đổi tag/name: {ex.Message}");
            }

            // 2. Vô hiệu hóa điều khiển và va chạm của Player
            if (playerController != null)
            {
                playerController.enabled = false;
            }

            var combat = rootObj.GetComponentInChildren<PlayerCombat>();
            if (combat != null) combat.enabled = false;

            var inv = rootObj.GetComponentInChildren<PlayerInventory>();
            if (inv != null) inv.enabled = false;

            var limiter = rootObj.GetComponentInChildren<AttackRateLimiter>();
            if (limiter != null) limiter.enabled = false;

            var cc = rootObj.GetComponentInChildren<CharacterController>();
            if (cc != null) cc.enabled = false;

            var colliders = rootObj.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = false;
            }

            // 3. Ra lệnh cho toàn bộ quái trong màn chơi dừng tấn công ngay lập tức
            StopAllMonstersAttacking();

            // 4. Phát sự kiện thay đổi chỉ số và sự kiện chết cục bộ
            OnStatsChanged?.Invoke();
            OnPlayerDied?.Invoke();

            // 5. Nếu Main Camera gắn con vào Player, tách ra để không bị mất Camera khi Destroy Player
            var cam = rootObj.GetComponentInChildren<Camera>();
            if (cam != null)
            {
                cam.transform.SetParent(null);
            }

            // 6. Khởi chạy tiến trình animation Die (0.2s) và Destroy sau 2s
            StartCoroutine(DeathSequenceRoutine(rootObj));
        }

        private void StopAllMonstersAttacking()
        {
            try
            {
                // 1. Quái dùng NavMeshEnemyAI
                var navEnemies = Object.FindObjectsByType<NavMeshEnemyAI>(FindObjectsSortMode.None);
                foreach (var enemy in navEnemies)
                {
                    if (enemy == null) continue;
                    enemy.playerTarget = null;
                    enemy.currentState = NavMeshEnemyAI.AIState.Patrol;
                    var agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    if (agent != null && agent.isOnNavMesh)
                    {
                        agent.isStopped = true;
                        agent.velocity = Vector3.zero;
                    }
                }

                // 2. Quái dùng NavMeshGoblinAI
                var navGoblins = Object.FindObjectsByType<NavMeshGoblinAI>(FindObjectsSortMode.None);
                foreach (var goblin in navGoblins)
                {
                    if (goblin == null) continue;
                    goblin.playerTarget = null;
                    goblin.currentState = NavMeshGoblinAI.AIState.Patrol;
                    var agent = goblin.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    if (agent != null && agent.isOnNavMesh)
                    {
                        agent.isStopped = true;
                        agent.velocity = Vector3.zero;
                    }
                }

                // 3. Quái dùng WolfAI
                var wolves = Object.FindObjectsByType<WolfAI>(FindObjectsSortMode.None);
                foreach (var wolf in wolves)
                {
                    if (wolf == null) continue;
                    wolf.playerTarget = null;
                    var agent = wolf.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    if (agent != null && agent.isOnNavMesh)
                    {
                        agent.isStopped = true;
                        agent.velocity = Vector3.zero;
                    }
                }

                // 4. Quái dùng GoblinAI
                var goblins = Object.FindObjectsByType<GoblinAI>(FindObjectsSortMode.None);
                foreach (var goblin in goblins)
                {
                    if (goblin == null) continue;
                    goblin.playerTarget = null;
                    var agent = goblin.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    if (agent != null && agent.isOnNavMesh)
                    {
                        agent.isStopped = true;
                        agent.velocity = Vector3.zero;
                    }
                }

                // 5. Quét tổng quát mọi script quái khác (Boss, Gronk, etc.)
                var allMonos = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
                foreach (var mb in allMonos)
                {
                    if (mb == null) continue;
                    var t = mb.GetType();
                    string typeName = t.Name;
                    if (typeName.Contains("Enemy") || typeName.Contains("Goblin") || typeName.Contains("Wolf") || typeName.Contains("Boss") || typeName.Contains("Gronk"))
                    {
                        var targetField = t.GetField("playerTarget", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (targetField != null)
                        {
                            targetField.SetValue(mb, null);
                        }

                        var agentField = t.GetField("agent", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (agentField != null)
                        {
                            if (agentField.GetValue(mb) is UnityEngine.AI.NavMeshAgent ag && ag.isOnNavMesh)
                            {
                                ag.isStopped = true;
                                ag.velocity = Vector3.zero;
                            }
                        }

                        var animField = t.GetField("animator", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (animField != null)
                        {
                            if (animField.GetValue(mb) is Animator a)
                            {
                                try { a.ResetTrigger("Attack"); } catch { }
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[PlayerStats] Lỗi khi dừng quái: {ex.Message}");
            }
        }

        private System.Collections.IEnumerator DeathSequenceRoutine(GameObject rootObj)
        {
            Animator anim = (rootObj != null) ? rootObj.GetComponentInChildren<Animator>() : GetComponentInChildren<Animator>();
            if (anim != null)
            {
                // Tắt layer UpperBody để animation Die toàn thân hiển thị chuẩn xác
                if (anim.layerCount > 1)
                {
                    anim.SetLayerWeight(1, 0f);
                }

                anim.speed = 1f;
                anim.Play("Die", 0, 0f);
                try { anim.SetTrigger("Die"); } catch { }
            }

            // 1. Chạy animation Die trong 0.2s theo yêu cầu
            yield return new WaitForSeconds(dieAnimPlayDuration);

            // 2. Đóng băng animation Die ở mốc 0.2s (chỉ chạy 0.2s so với cả animation die)
            if (anim != null)
            {
                anim.speed = 0f;
            }

            // 3. Chờ phần thời gian còn lại sao cho tổng cộng sau 2s kể từ lúc chết thì Destroy Player
            float remaining = Mathf.Max(0f, destroyDelayAfterDeath - dieAnimPlayDuration);
            yield return new WaitForSeconds(remaining);

            Debug.Log("<color=red><b>[PlayerStats] Hết 2s sau khi chết. Destroy Player GameObject!</b></color>");

            // Kích hoạt Game Over lên UI hệ thống nếu có
            try
            {
                GameEvents.OnPlayerDied?.Invoke();
            }
            catch { }

            if (rootObj != null)
            {
                Destroy(rootObj);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void Revive(float healthPercent = 1f)
        {
            isDead = false;
            currentHealth = Mathf.Clamp(maxHealth * healthPercent, 1f, maxHealth);
            OnStatsChanged?.Invoke();
        }
    }
}