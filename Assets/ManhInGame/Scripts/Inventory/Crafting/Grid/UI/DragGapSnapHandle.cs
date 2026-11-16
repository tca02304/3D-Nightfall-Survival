
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class DragGapSnapHandler : MonoBehaviour
{
    [Header("Snap Settings")]
    [SerializeField, Min(0f)]
    private float maxGapDistance = 20f;

    private RectTransform gridRect;

    // Cache the grid rectangle.
    private void Awake()
    {
        gridRect = GetComponent<RectTransform>();
    }

    // Forward a drop in a grid gap to the nearest valid slot.
    public bool TrySnapDrop(PointerEventData eventData)
    {
        if (gridRect == null || eventData == null)
            return false;

        if (!gridRect.gameObject.activeInHierarchy)
            return false;

        Camera camera = GetUICamera(gridRect);

        // Never attract drops from outside this grid.
        if (!RectTransformUtility.RectangleContainsScreenPoint(
                gridRect, eventData.position, camera))
            return false;

        GameObject nearestHandler = null;
        float nearestDistance = float.MaxValue;

        // Search direct slot children of the grid.
        for (int i = 0; i < gridRect.childCount; i++)
        {
            Transform child = gridRect.GetChild(i);

            if (!child.gameObject.activeInHierarchy)
                continue;

            RectTransform rect = child as RectTransform;

            if (rect == null)
                continue;

            GameObject handler =
                ExecuteEvents.GetEventHandler<IDropHandler>(
                    child.gameObject);

            if (handler == null)
                continue;

            // A direct hit must be handled by normal OnDrop.
            if (RectTransformUtility.RectangleContainsScreenPoint(
                    rect, eventData.position, camera))
                return false;

            float distance =
                GetScreenDistance(rect, eventData.position, camera);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestHandler = handler;
            }
        }

        if (nearestHandler == null ||
            nearestDistance > maxGapDistance)
            return false;

        ExecuteEvents.Execute<IDropHandler>(
            nearestHandler,
            eventData,
            ExecuteEvents.dropHandler);

        Debug.Log(
            $"[DragGapSnap] Drop forwarded to " +
            $"{nearestHandler.name}, " +
            $"distance={nearestDistance:F1}px",
            this);

        return true;
    }

    // Calculate the shortest screen-space distance to a slot rectangle.
    private static float GetScreenDistance(
        RectTransform rect,
        Vector2 position,
        Camera camera)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);

        Vector2 a = RectTransformUtility.WorldToScreenPoint(
            camera, corners[0]);

        Vector2 b = RectTransformUtility.WorldToScreenPoint(
            camera, corners[2]);

        float minX = Mathf.Min(a.x, b.x);
        float maxX = Mathf.Max(a.x, b.x);
        float minY = Mathf.Min(a.y, b.y);
        float maxY = Mathf.Max(a.y, b.y);

        float x = Mathf.Clamp(position.x, minX, maxX);
        float y = Mathf.Clamp(position.y, minY, maxY);

        return Vector2.Distance(position, new Vector2(x, y));
    }

    // Resolve the camera used by the grid's Canvas.
    private static Camera GetUICamera(RectTransform rect)
    {
        Canvas canvas = rect.GetComponentInParent<Canvas>();

        if (canvas == null ||
            canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }
}
