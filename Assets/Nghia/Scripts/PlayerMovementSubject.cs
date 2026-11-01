using System;
using UnityEngine;

namespace Nghia
{
    public class PlayerMovementSubject : MonoBehaviour
    {
        public event Action OnPlayerWalk;
        public event Action OnPlayerStop;

        [SerializeField] private float minMoveSpeed = 0.05f;

        private Vector3 lastPosition;
        private bool wasWalking;

        private void Start()
        {
            lastPosition = transform.position;
        }

        private void Update()
        {
            Vector3 movement = transform.position - lastPosition;
            movement.y = 0f;

            float speed = movement.magnitude / Time.deltaTime;

            bool isWalking = speed > minMoveSpeed;

            if (isWalking)
            {
                OnPlayerWalk?.Invoke();
            }
            else if (wasWalking)
            {
                OnPlayerStop?.Invoke();
            }

            wasWalking = isWalking;
            lastPosition = transform.position;
        }
    }
}