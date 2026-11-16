
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public sealed class GridGapDropHandler : MonoBehaviour, IDropHandler
{
    [Header("Gap Snap Settings")]
    [SerializeField, Min(0f)]
    private float maxSnapDistance = 24f;

    [SerializeField]
    private bool enableDebug = false;

    private RectTransform gridRect;

    // Cache the grid RectTransform and ensure it can receive UI drops.
    private void Awake()
    {
        gridRect = GetComponent<RectTransform>();

        Image background = GetComponent<Image>();

        if (background != null)
            background.raycastTarget = true;
    }

    // Redirect drops in the gaps to the nearest valid child slot.
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData == null || eventData.pointerDrag == null)
            return;

        if (!gameObject.activeInHierarchy)
            return;

        Camera uiCamera = GetUICamera(eventData);

        if (!RectTransformUtility.RectangleContainsScreenPoint(
                gridRect,
                eventData.position,
                uiCamera))
            return;

        GameObject nearestTarget = null;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < gridRect.childCount; i++)
        {
            Transform child = gridRect.GetChild(i);

            if (!child.gameObject.activeInHierarchy)
                continue;

            RectTransform slotRect = child as RectTransform;

            if (slotRect == null)
                continue;

            IDropHandler dropHandler =
                child.GetComponent<IDropHandler>();

            if (dropHandler == null)
                continue;

            float distance = GetDistanceToRect(
                slotRect,
                eventData.position,
                uiCamera);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestTarget = child.gameObject;
            }
        }

        if (nearestTarget == null ||
            nearestDistance > maxSnapDistance)
            return;

        if (enableDebug)
        {
            Debug.Log(
                $"[GridGapDrop] {name} -> " +
                $"{nearestTarget.name}, " +
                $"distance={nearestDistance:F1}px",
                this);
        }

        // Forward this drop once to the closest slot.
        ExecuteEvents.Execute<IDropHandler>(
            nearestTarget,
            eventData,
            ExecuteEvents.dropHandler);
    }

    // Resolve the event camera for the active Canvas.
    private Camera GetUICamera(PointerEventData eventData)
    {
        Canvas canvas = GetComponentInParent<Canvas>();

        if (canvas != null &&
            canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        if (eventData.enterEventCamera != null)
            return eventData.enterEventCamera;

        return canvas != null ? canvas.worldCamera : null;
    }

    // Calculate distance from the cursor to the slot's screen rectangle.
    private static float GetDistanceToRect(
        RectTransform rect,
        Vector2 screenPosition,
        Camera camera)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);

        Vector2 bottomLeft =
            RectTransformUtility.WorldToScreenPoint(
                camera, corners[0]);

        Vector2 topRight =
            RectTransformUtility.WorldToScreenPoint(
                camera, corners[2]);

        float minX = Mathf.Min(bottomLeft.x, topRight.x);
        float maxX = Mathf.Max(bottomLeft.x, topRight.x);
        float minY = Mathf.Min(bottomLeft.y, topRight.y);
        float maxY = Mathf.Max(bottomLeft.y, topRight.y);

        Vector2 closestPoint = new Vector2(
            Mathf.Clamp(screenPosition.x, minX, maxX),
            Mathf.Clamp(screenPosition.y, minY, maxY)
        );

        return Vector2.Distance(
            screenPosition,
            closestPoint);
    }
}
