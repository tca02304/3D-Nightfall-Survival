using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DucThanh
{
    public class FirstPersonCamera : MonoBehaviour
    {
        [Header("Mouse Settings")]
        [Tooltip("Tốc độ xoay chuột")]
        public float mouseSensitivity = 50f;

        [Tooltip("Kéo thả Transform của Player (nhân vật chính) vào đây để xoay trái/phải")]
        public Transform playerBody;

        [Tooltip("Kéo thả Player (chứa PlayerStats) vào đây để dừng xoay camera khi chết")]
        public PlayerStats playerStats;

        private float xRotation = 0f;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Tự động lấy object cha làm playerBody nếu chưa được gán trong Inspector
            if (playerBody == null && transform.parent != null)
            {
                playerBody = transform.parent;
            }
        }

        private void Update()
        {
            if (playerStats != null && playerStats.IsDead)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            float mouseX = 0f;
            float mouseY = 0f;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                // New Input System trả về giá trị delta theo frame, không cần nhân Time.deltaTime
                Vector2 delta = Mouse.current.delta.ReadValue();
                mouseX = delta.x * mouseSensitivity * 0.02f;
                mouseY = delta.y * mouseSensitivity * 0.02f;
            }
#else
            mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
#endif

            if (mouseX == 0f && mouseY == 0f)
            {
                try
                {
                    mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
                    mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
                }
                catch { }
            }

            // Xoay Camera lên/xuống (Trục X)
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            // Xoay toàn bộ cơ thể nhân vật sang hai bên (Trục Y)
            if (playerBody != null)
            {
                playerBody.Rotate(Vector3.up * mouseX);
            }
        }
    }
}