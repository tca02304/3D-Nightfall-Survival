
using UnityEngine;

namespace Nghia
{
    public class EnemyAttackAudioDetector : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NavMeshEnemyAI enemyAI;
        [SerializeField] private EnemyAttackAudioSubject subject;

        private float nextNotifyTime;

        private void Awake()
        {
            if (enemyAI == null)
                enemyAI = GetComponent<NavMeshEnemyAI>();

            if (subject == null)
                subject = GetComponent<EnemyAttackAudioSubject>();
        }

        private void Update()
        {
            if (enemyAI == null || subject == null)
                return;

            if (enemyAI.currentState != NavMeshEnemyAI.AIState.Attack)
                return;

            if (Time.time < nextNotifyTime)
                return;

            subject.NotifyAttack();

            nextNotifyTime = Time.time +
                Mathf.Max(0.1f, enemyAI.attackCooldown);
        }
    }
}
