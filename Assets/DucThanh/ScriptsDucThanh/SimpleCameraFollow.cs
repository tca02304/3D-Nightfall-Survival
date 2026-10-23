using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DucThanh
{
    /// <summary>
    /// Camera theo sau nhân vật và hỗ trợ tự do xoay góc nhìn mọi hướng khi GIỮ CHUỘT PHẢI.
    /// Giúp người chơi dễ dàng ngước nhìn bầu trời, ngắm mặt trời, mặt trăng và quan sát không gian xung quanh.
    /// </summary>
    public class SimpleCameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Target player cần follow")]
        [SerializeField] private Transform target;

        [Header("Distance & Offset")]
        [Tooltip("Khoảng cách từ Camera tới Target")]
        [SerializeField] private float distance = 8f;

        [Tooltip("Điểm trọng tâm trên người Player (ví dụ cao 1.5m để ngang tầm ngực/đầu)")]
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.5f, 0f);

        [Tooltip("Tốc độ bám theo Player")]
        [SerializeField] private float followSmoothSpeed = 15f;

        [Header("Orbit / Free Look (Giữ chuột phải)")]
        [Tooltip("Độ nhạy khi xoay chuột ngang (X)")]
        [SerializeField] private float mouseSensitivityX = 3f;

        [Tooltip("Độ nhạy khi xoay chuột dọc (Y)")]
        [SerializeField] private float mouseSensitivityY = 3f;

        [Tooltip("Góc ngước lên trời tối đa (độ âm là nhìn lên trời)")]
        [SerializeField] private float minPitchAngle = -80f;

        [Tooltip("Góc cúi xuống đất tối đa")]
        [SerializeField] private float maxPitchAngle = 80f;

        [Tooltip("Khóa chuột và ẩn con trỏ chuột trong lúc giữ chuột phải")]
        [SerializeField] private bool lockCursorOnOrbit = true;

        [Tooltip("Tự động trả góc nhìn về góc mặc định khi thả chuột phải")]
        [SerializeField] private bool resetAngleOnRelease = false;

        // Góc xoay hiện tại
        private float currentYaw = 0f;    // Xoay ngang quanh player (trục Y)
        private float currentPitch = 25f; // Xoay dọc lên/xuống (trục X)
        private float defaultYaw = 0f;
        private float defaultPitch = 25f;

        private bool isOrbiting = false;

        private void Start()
        {
            currentYaw = transform.eulerAngles.y;
            currentPitch = transform.eulerAngles.x;

            defaultYaw = currentYaw;
            defaultPitch = currentPitch;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                var player = Object.FindFirstObjectByType<PlayerController>();
                if (player != null)
                {
                    target = player.transform;
                }
                else return;
            }

            HandleMouseOrbit();

            // Tính vị trí tâm điểm ngắm trên người Player
            Vector3 focusPoint = target.position + targetOffset;

            // Tính toán hướng nhìn từ Yaw và Pitch
            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);

            // Tính toạ độ camera lùi về sau theo hướng góc nhìn
            Vector3 desiredPosition = focusPoint - (rotation * Vector3.forward * distance);

            // Cập nhật vị trí mượt mà
            transform.position = Vector3.Lerp(transform.position, desiredPosition, followSmoothSpeed * Time.deltaTime);
            transform.rotation = rotation;
        }

        private void HandleMouseOrbit()
        {
            bool rightMouseDown = false;
            Vector2 mouseDelta = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                rightMouseDown = Mouse.current.rightButton.isPressed;
                mouseDelta = Mouse.current.delta.ReadValue() * 0.1f;
            }
#else
            rightMouseDown = Input.GetMouseButton(1);
            mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif

            if (rightMouseDown)
            {
                isOrbiting = true;
                if (lockCursorOnOrbit)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }

                // Cập nhật góc quay theo cử động chuột
                currentYaw += mouseDelta.x * mouseSensitivityX;
                currentPitch -= mouseDelta.y * mouseSensitivityY;
                currentPitch = Mathf.Clamp(currentPitch, minPitchAngle, maxPitchAngle);
            }
            else
            {
                if (isOrbiting)
                {
                    isOrbiting = false;
                    if (lockCursorOnOrbit)
                    {
                        Cursor.lockState = CursorLockMode.None;
                        Cursor.visible = true;
                    }

                    if (resetAngleOnRelease)
                    {
                        currentYaw = defaultYaw;
                        currentPitch = defaultPitch;
                    }
                }
            }
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}
