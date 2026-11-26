using UnityEngine;

namespace Nightfall.Stats
{
    /// <summary>Purely visual first-person held-item swing. Does not apply damage.</summary>
    [DisallowMultipleComponent]
    public sealed class HeldItemSwing : MonoBehaviour
    {
        [Header("Target (leave empty for HeldItemSocket under this Camera)")]
        [SerializeField] private Transform heldItemSocket;

        [Header("Input")]
        [SerializeField] private bool requireLockedCursor = true;
        [SerializeField] private bool autoSwingWhileHeld = true;

        [Header("Swing Motion")]
        [SerializeField, Min(0.05f)] private float swingDuration = 0.28f;
        [SerializeField] private Vector3 swingEuler = new Vector3(55f, -15f, 25f);
        [SerializeField] private Vector3 swingPosition = new Vector3(-0.10f, -0.10f, 0.06f);

        private Transform target;
        private Vector3 restingPosition;
        private Quaternion restingRotation;
        private float elapsed;
        private bool swinging;

        // Capture the socket's untouched pose after it becomes available.
        private void BindTarget()
        {
            Transform candidate = heldItemSocket != null
                ? heldItemSocket
                : transform.Find("HeldItemSocket");

            if (candidate == target)
                return;

            RestorePose();
            target = candidate;
            if (target == null)
                return;

            restingPosition = target.localPosition;
            restingRotation = target.localRotation;
            swinging = false;
            elapsed = 0f;
        }

        // Read mouse input without altering combat or gathering logic.
        private void Update()
        {
            BindTarget();
            if (target == null)
                return;

            if (requireLockedCursor && Cursor.lockState != CursorLockMode.Locked)
                return;

            bool pressed = Input.GetMouseButtonDown(0);
            bool held = autoSwingWhileHeld && Input.GetMouseButton(0);
            if (!swinging && (pressed || held) && HasVisibleItem())
            {
                swinging = true;
                elapsed = 0f;
            }
        }

        // Animate after gameplay logic, always relative to the original local pose.
        private void LateUpdate()
        {
            if (!swinging || target == null)
                return;

            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / swingDuration);
            float arc = Mathf.Sin(progress * Mathf.PI);

            target.localPosition = restingPosition + swingPosition * arc;
            target.localRotation = restingRotation * Quaternion.Euler(swingEuler * arc);

            if (progress >= 1f)
            {
                swinging = false;
                RestorePose();
            }
        }

        // Ignore an empty SwingPivot/Socket hierarchy; require a visible model renderer.
        private bool HasVisibleItem()
        {
            foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.enabled && renderer.gameObject.activeInHierarchy)
                    return true;
            }
            return false;
        }

        // Undo all visual offsets when disabled or when the target changes.
        private void RestorePose()
        {
            if (target == null)
                return;

            target.localPosition = restingPosition;
            target.localRotation = restingRotation;
        }

        // Avoid leaving the held socket rotated when the component is disabled.
        private void OnDisable()
        {
            RestorePose();
            swinging = false;
            elapsed = 0f;
        }
    }
}
