
using UnityEngine;

public class DirectionIndicator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform target;
    [SerializeField] private Transform greenCircle;

    [Header("Distance")]
    [SerializeField] private float farDistance = 30f;
    [SerializeField] private float nearDistance = 3f;
    [SerializeField] private float transitionDistance = 8f;

    [Header("Scale")]
    [SerializeField] private float farSize = 8f;
    [SerializeField] private float nearSize = 2f;
    [SerializeField] private float scaleSmooth = 5f;

    [Header("Height")]
    [SerializeField] private float verticalHeight = 2f;
    [SerializeField] private float groundOffset = 0.08f;

    [Header("Rotation")]
    [SerializeField] private float rotationSmooth = 5f;

    private bool isActive;



    private void Update()
    {
        if (!isActive || player == null ||
            target == null || greenCircle == null)
            return;

        UpdatePosition();
        UpdateScale();
        UpdateRotation();
     
    }

    private float GetDistance()
    {
        return Vector3.Distance(player.position, target.position);
    }

    private float GetTransition()
    {
        return Mathf.InverseLerp(
            nearDistance,
            transitionDistance,
            GetDistance()
        );
    }

    private void UpdatePosition()
    {
        float t = GetTransition();

        float height = Mathf.Lerp(
            groundOffset,
            verticalHeight,
            t
        );

        transform.position = new Vector3(
            target.position.x,
            target.position.y + height,
            target.position.z
        );
    }

    private void UpdateScale()
    {
        float t = Mathf.InverseLerp(
            nearDistance,
            farDistance,
            GetDistance()
        );

        float size = Mathf.Lerp(nearSize, farSize, t);

        transform.localScale = Vector3.Lerp(
            transform.localScale,
            Vector3.one * size,
            scaleSmooth * Time.deltaTime
        );
    }

    private void UpdateRotation()
    {
        float t = GetTransition();

        Vector3 direction = player.position - target.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion verticalRotation =
            Quaternion.LookRotation(direction.normalized, Vector3.up);

        Quaternion horizontalRotation =
            Quaternion.Euler(90f, 0f, 0f);

        Quaternion desiredRotation = Quaternion.Slerp(
            horizontalRotation,
            verticalRotation,
            t
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desiredRotation,
            rotationSmooth * Time.deltaTime
        );
    }

    

    public void ShowGuide(Transform newTarget)
    {
        if (newTarget == null)
            return;

        target = newTarget;
        isActive = true;

        gameObject.SetActive(true);
    }

    public void HideGuide()
    {
        isActive = false;


        gameObject.SetActive(false);
    }
}
