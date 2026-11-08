
using System;
using UnityEngine;

namespace Nghia
{
    public class EnemyAttackAudioSubject : MonoBehaviour
    {
        public event Action OnAttack;

        public void NotifyAttack()
        {
            OnAttack?.Invoke();
        }
    }
}
