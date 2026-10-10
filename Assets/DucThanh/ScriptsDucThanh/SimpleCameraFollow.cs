using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DucThanh
{
    public class SimpleCameraFollow : MonoBehaviour
    {
        [Tooltip("Target player cần follow")]
        [SerializeField] private Transform target;

        [Tooltip("Khoảng cách bù so với Target")]
        [SerializeField] private Vector3 offset = new Vector3(0, 5, -8);

        [Tooltip("Tốc độ mượt mà khi follow")]
        [SerializeField] private float smoothSpeed = 10f;

        [Tooltip("Góc nhìn nghiêng xuống target")]
        [SerializeField] private float pitchAngle = 25f;

        private void LateUpdate()
        {
            if (target == null)
            {
                // Tự tìm player nếu chưa gán
                var player = Object.FindFirstObjectByType<PlayerController>();
                if (player != null)
                {
                    target = player.transform;
                }
                else return;
            }

            Vector3 desiredPosition = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(pitchAngle, 0, 0);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}
