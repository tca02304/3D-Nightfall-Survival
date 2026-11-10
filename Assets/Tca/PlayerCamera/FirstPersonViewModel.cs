
using UnityEngine;

public class FirstPersonViewModel : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField] private FirstPersonCamera firstPersonCamera;
    [SerializeField] private Animator weaponAnimator;

    // =========================================================
    // MOUSE SWAY
    // =========================================================

    [Header("Mouse Sway")]
    [SerializeField] private bool enableSway = true;
    [SerializeField] private float swayAmount = 2f;
    [SerializeField] private float swaySmooth = 8f;
    [SerializeField] private float maxSway = 5f;

    // =========================================================
    // INTERNAL STATE
    // =========================================================

    private Vector3 startPosition;
    private Quaternion startRotation;

    // =========================================================
    // INITIALIZATION
    // =========================================================

    private void Start()
    {
        startPosition = transform.localPosition;
        startRotation = transform.localRotation;
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void LateUpdate()
    {
        HandleSway();
    }

    // =========================================================
    // MOUSE SWAY
    // =========================================================

    private void HandleSway()
    {
        if (!enableSway || firstPersonCamera == null)
        {
            ResetRotation();
            return;
        }

        float mouseX = firstPersonCamera.MouseX;
        float mouseY = firstPersonCamera.MouseY;

        float targetX = Mathf.Clamp(
            -mouseY * swayAmount,
            -maxSway,
            maxSway
        );

        float targetY = Mathf.Clamp(
            mouseX * swayAmount,
            -maxSway,
            maxSway
        );

        Quaternion targetRotation =
            startRotation *
            Quaternion.Euler(
                targetX,
                targetY,
                -targetY
            );

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            targetRotation,
            1f - Mathf.Exp(-swaySmooth * Time.deltaTime)
        );
    }

    private void ResetRotation()
    {
        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            startRotation,
            1f - Mathf.Exp(-swaySmooth * Time.deltaTime)
        );
    }

    // =========================================================
    // WEAPON ANIMATION
    // =========================================================

    public void PlayAttack()
    {
        if (weaponAnimator != null)
            weaponAnimator.SetTrigger("Attack");
    }

    public void SetWeapon(Animator newAnimator)
    {
        weaponAnimator = newAnimator;
    }

    // =========================================================
    // SETTINGS
    // =========================================================

    public void SetSway(bool value)
    {
        enableSway = value;
    }

    public void SetSwayAmount(float value)
    {
        swayAmount = Mathf.Max(0f, value);
    }

    public void ResetViewModel()
    {
        transform.localPosition = startPosition;
        transform.localRotation = startRotation;
    }
}