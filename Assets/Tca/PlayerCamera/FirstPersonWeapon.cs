
using System.Collections;
using UnityEngine;

public class FirstPersonWeapon : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform weaponModel;
    [SerializeField] private CharacterController playerController;

    [Header("Idle Position")]
    [SerializeField]
    private Vector3 idlePosition =
        new Vector3(0.35f, -0.30f, 0.55f);

    [SerializeField]
    private Vector3 idleRotation =
        new Vector3(0f, 0f, 0f);

    [Header("Movement Bob")]
    [SerializeField] private float bobFrequency = 8f;
    [SerializeField] private float bobAmount = 0.015f;
    [SerializeField] private float rotationBobAmount = 2f;

    [Header("Attack")]
    [SerializeField] private float attackDuration = 0.35f;

    // Góc vung kiếm. Có thể điều chỉnh trong Inspector.
    [SerializeField]
    private Vector3 attackRotation =
        new Vector3(-55f, 0f, -65f);

    [SerializeField]
    private Vector3 attackPosition =
        new Vector3(-0.12f, -0.08f, 0f);

    private bool isAttacking;
    private Coroutine attackCoroutine;
    private float bobTimer;

    private void LateUpdate()
    {
        if (isAttacking)
            return;

        bool isMoving = playerController != null &&
                        playerController.isGrounded &&
                        playerController.velocity.magnitude > 0.1f;

        float bobY = 0f;
        float bobX = 0f;
        float bobRotation = 0f;

        if (isMoving)
        {
            bobTimer += Time.deltaTime * bobFrequency;

            bobY = Mathf.Sin(bobTimer) * bobAmount;
            bobX = Mathf.Cos(bobTimer * 0.5f) * bobAmount;
            bobRotation = Mathf.Sin(bobTimer) * rotationBobAmount;
        }
        else
        {
            bobTimer = 0f;
        }

        transform.localPosition = idlePosition +
                                  new Vector3(bobX, bobY, 0f);

        transform.localRotation =
            Quaternion.Euler(idleRotation.x,
                             idleRotation.y,
                             idleRotation.z + bobRotation);
    }

    public void Attack()
    {
        if (isAttacking)
            return;

        if (attackCoroutine != null)
            StopCoroutine(attackCoroutine);

        attackCoroutine = StartCoroutine(PlayAttack());
    }

    private IEnumerator PlayAttack()
    {
        isAttacking = true;

        Vector3 startPosition = idlePosition;
        Quaternion startRotation = Quaternion.Euler(idleRotation);

        Vector3 endPosition = idlePosition + attackPosition;
        Quaternion endRotation =
            startRotation * Quaternion.Euler(attackRotation);

        // Pha 1: Vung kiếm.
        float timer = 0f;
        float swingTime = attackDuration * 0.45f;

        while (timer < swingTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / swingTime);
            t = Mathf.SmoothStep(0f, 1f, t);

            transform.localPosition =
                Vector3.Lerp(startPosition, endPosition, t);

            transform.localRotation =
                Quaternion.Slerp(startRotation, endRotation, t);

            yield return null;
        }

        // Pha 2: Đưa kiếm về tư thế Idle.
        timer = 0f;
        float returnTime = attackDuration * 0.55f;

        while (timer < returnTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / returnTime);
            t = Mathf.SmoothStep(0f, 1f, t);

            transform.localPosition =
                Vector3.Lerp(endPosition, startPosition, t);

            transform.localRotation =
                Quaternion.Slerp(endRotation, startRotation, t);

            yield return null;
        }

        transform.localPosition = idlePosition;
        transform.localRotation = startRotation;

        isAttacking = false;
        attackCoroutine = null;
    }
}