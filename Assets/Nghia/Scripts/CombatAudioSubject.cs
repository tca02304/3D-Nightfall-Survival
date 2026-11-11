using System;
using UnityEngine;

namespace Nghia
{
    public class CombatAudioSubject : MonoBehaviour
    {
        public event Action OnHit;
        public event Action OnTakeDamage;

        public void Hit()
        {
            OnHit?.Invoke();
        }

        public void TakeDamage()
        {
            OnTakeDamage?.Invoke();
        }
    }
}