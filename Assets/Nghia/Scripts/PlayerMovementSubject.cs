using System;
using UnityEngine;

namespace Nghia
{
    public class PlayerMovementSubject : MonoBehaviour
    {
        public event Action OnPlayerStep;
        public event Action<float> OnPlayerMove; // Truyền tốc độ thực tế (m/s)
        public event Action OnPlayerStop;

        [Header("Movement Detection")]
        [Tooltip("Ngưỡng tốc độ tối thiểu (m/s) để tính là đang di chuyển")]
        [SerializeField] private float minMoveSpeed = 0.5f;

        [Header("Muck Step Timing (Thời gian giữa 2 bước chân)")]
        [Tooltip("Khoảng thời gian (giây) giữa 2 bước khi đi bộ bình thường (Muck: ~0.45s - 0.55s)")]
        [SerializeField] private float walkStepInterval = 0.5f;

        [Tooltip("Khoảng thời gian (giây) giữa 2 bước khi chạy nhanh (Muck: ~0.33s - 0.38s)")]
        [SerializeField] private float sprintStepInterval = 0.36f;

        [Tooltip("Tốc độ (m/s) bắt đầu tính là chạy Sprint (PlayerController walkSpeed = 5, sprintSpeed = 8)")]
        [SerializeField] private float sprintThresholdSpeed = 6.0f;

        [Header("Step Timing Safety")]
        [Tooltip("Khoảng cách tối thiểu giữa 2 âm bước chân liên tiếp để tuyệt đối không bị dồn dập (spam)")]
        [SerializeField] private float minStepCooldown = 0.28f;

        private CharacterController characterController;
        private Vector3 lastPosition;
        private float stepTimer;
        private float lastStepRealTime;
        private bool wasMoving;

        public bool IsGrounded => characterController == null || characterController.isGrounded;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        private void Start()
        {
            lastPosition = transform.position;
            lastStepRealTime = -10f;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            Vector3 currentPos = transform.position;
            Vector3 horizontalDelta = currentPos - lastPosition;
            horizontalDelta.y = 0f;

            float currentSpeed = horizontalDelta.magnitude / dt;
            bool isGrounded = IsGrounded;
            bool isMoving = isGrounded && (currentSpeed >= minMoveSpeed);

            if (isMoving)
            {
                OnPlayerMove?.Invoke(currentSpeed);

                // Tính thời gian chu kỳ bước theo tốc độ di chuyển
                float targetInterval = (currentSpeed >= sprintThresholdSpeed)
                    ? sprintStepInterval
                    : walkStepInterval;

                // Khi vừa bắt đầu bước sau khi đứng yên
                if (!wasMoving)
                {
                    // Tránh trigger dồn nếu vừa mới dừng và ấn đi lại ngay lập tức
                    if (Time.time - lastStepRealTime >= minStepCooldown)
                    {
                        TriggerStep();
                    }
                    stepTimer = 0f;
                }
                else
                {
                    stepTimer += dt;
                    if (stepTimer >= targetInterval)
                    {
                        if (Time.time - lastStepRealTime >= minStepCooldown)
                        {
                            TriggerStep();
                        }
                        stepTimer = 0f;
                    }
                }
            }
            else
            {
                if (wasMoving)
                {
                    OnPlayerStop?.Invoke();
                    stepTimer = 0f;
                }
            }

            wasMoving = isMoving;
            lastPosition = currentPos;
        }

        private void TriggerStep()
        {
            lastStepRealTime = Time.time;
            OnPlayerStep?.Invoke();
        }
    }
}
