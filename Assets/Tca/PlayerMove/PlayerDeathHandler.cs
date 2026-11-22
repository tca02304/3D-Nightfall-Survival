using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace DucThanh
{
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerDeathHandler : MonoBehaviour
    {
        [Header("Death Settings")]
        [Tooltip("Thời gian chạy animation Die trước khi đóng băng")]
        [SerializeField] private float dieAnimPlayDuration = 0.2f;

        [Tooltip("Tổng thời gian từ lúc chết tới lúc Destroy Player")]
        [SerializeField] private float destroyDelayAfterDeath = 2.0f;

        private PlayerStats playerStats;
        private PlayerController playerController;

        private void Awake()
        {
            playerStats = GetComponent<PlayerStats>();
            playerController = GetComponent<PlayerController>();
        }

        private void OnEnable()
        {
            if (playerStats != null)
                playerStats.OnPlayerDied += HandlePlayerDeath;
        }

        private void OnDisable()
        {
            if (playerStats != null)
                playerStats.OnPlayerDied -= HandlePlayerDeath;
        }

        private void HandlePlayerDeath()
        {
            GameObject rootObj = (transform.root != null) ? transform.root.gameObject : gameObject;

            // 1. Đổi tag và tên để AI quái mất dấu mục tiêu
            rootObj.tag = "Untagged";
            rootObj.name = "Player_Dead";

            // 2. Tắt điều khiển và va chạm
            if (playerController != null)
                playerController.enabled = false;

            var cc = rootObj.GetComponentInChildren<CharacterController>();
            if (cc != null) cc.enabled = false;

            foreach (var col in rootObj.GetComponentsInChildren<Collider>())
                col.enabled = false;

            // 3. Cho toàn bộ quái dừng đuổi và tấn công
            StopAllMonstersAttacking();

            // 4. Tách Camera nếu đang là con của Player để không bị Destroy theo
            var cam = rootObj.GetComponentInChildren<Camera>();
            if (cam != null)
                cam.transform.SetParent(null);

            // 5. Chạy animation Die và Destroy
            StartCoroutine(DeathSequenceRoutine(rootObj));
        }

        private void StopAllMonstersAttacking()
        {
            try
            {
                foreach (var enemy in Object.FindObjectsByType<NavMeshEnemyAI>(FindObjectsSortMode.None))
                {
                    if (enemy == null) continue;
                    enemy.playerTarget = null;
                    enemy.currentState = NavMeshEnemyAI.AIState.Patrol;
                    StopAgent(enemy.GetComponent<NavMeshAgent>());
                }

                foreach (var goblin in Object.FindObjectsByType<NavMeshGoblinAI>(FindObjectsSortMode.None))
                {
                    if (goblin == null) continue;
                    goblin.playerTarget = null;
                    goblin.currentState = NavMeshGoblinAI.AIState.Patrol;
                    StopAgent(goblin.GetComponent<NavMeshAgent>());
                }

                foreach (var wolf in Object.FindObjectsByType<WolfAI>(FindObjectsSortMode.None))
                {
                    if (wolf == null) continue;
                    wolf.playerTarget = null;
                    StopAgent(wolf.GetComponent<NavMeshAgent>());
                }

                foreach (var goblin in Object.FindObjectsByType<GoblinAI>(FindObjectsSortMode.None))
                {
                    if (goblin == null) continue;
                    goblin.playerTarget = null;
                    StopAgent(goblin.GetComponent<NavMeshAgent>());
                }

                // Quét thêm các script quái khác (Boss, Gronk...)
                foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                {
                    if (mb == null) continue;
                    var t = mb.GetType();
                    string typeName = t.Name;
                    if (typeName.Contains("Enemy") || typeName.Contains("Goblin") || typeName.Contains("Wolf") || typeName.Contains("Boss") || typeName.Contains("Gronk"))
                    {
                        var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

                        t.GetField("playerTarget", flags)?.SetValue(mb, null);

                        if (t.GetField("agent", flags)?.GetValue(mb) is NavMeshAgent ag)
                            StopAgent(ag);

                        if (t.GetField("animator", flags)?.GetValue(mb) is Animator a)
                        {
                            try { a.ResetTrigger("Attack"); } catch { }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[PlayerDeathHandler] Lỗi khi dừng quái: {ex.Message}");
            }
        }

        private void StopAgent(NavMeshAgent agent)
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
        }

        private IEnumerator DeathSequenceRoutine(GameObject rootObj)
        {
            Animator anim = rootObj.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                if (anim.layerCount > 1)
                    anim.SetLayerWeight(1, 0f);

                anim.speed = 1f;
                anim.Play("Die", 0, 0f);
                try { anim.SetTrigger("Die"); } catch { }
            }

            // Chạy animation Die trong 0.2s rồi đóng băng
            yield return new WaitForSeconds(dieAnimPlayDuration);
            if (anim != null)
                anim.speed = 0f;

            // Chờ đủ 2s rồi Destroy
            float remaining = Mathf.Max(0f, destroyDelayAfterDeath - dieAnimPlayDuration);
            yield return new WaitForSeconds(remaining);

            try { GameEvents.OnPlayerDied?.Invoke(); } catch { }

            Destroy(rootObj);
        }
    }
}