using System;
using UnityEngine;

namespace Nghia
{
    /// <summary>
    /// Subject phát tín hiệu khi quái vật thực hiện hành vi tấn công hoặc chạm trúng Player.
    /// Hoạt động độc lập và tự động kết nối với NavMeshEnemyAI / EnemyDamageDealer.
    /// </summary>
    public class EnemyAttackAudioSubject : MonoBehaviour
    {
        public event Action OnAttack;
        public event Action<Collider> OnAttackHitPlayer;

        public void NotifyAttack()
        {
            OnAttack?.Invoke();
        }

        public void NotifyHitPlayer(Collider playerCol)
        {
            OnAttackHitPlayer?.Invoke(playerCol);
            OnAttack?.Invoke();
        }
    }
}
