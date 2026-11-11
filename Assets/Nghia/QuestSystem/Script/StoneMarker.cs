
using TMPro;
using UnityEngine;

public class StoneMarker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform target;
    [SerializeField] private Camera playerCamera;

    [Header("UI References")]
    [SerializeField] private Canvas markerCanvas;
    [SerializeField] private TMP_Text exclamationText;
    [SerializeField] private TMP_Text targetNameText;
    [SerializeField] private TMP_Text interactionText;

    [Header("Distance Settings")]
    [SerializeField] private float showDistance = 15f;
    [SerializeField] private float markerHeight = 2f;

    private bool isActive;

    private void Awake()
    {
        // Tự tìm Camera nếu chưa được gán
        if (playerCamera == null)
            playerCamera = Camera.main;

        // Kiểm tra References
        if (player == null)
            Debug.LogError("[StoneMarker] Chưa gán Player!");

        if (playerCamera == null)
            Debug.LogError("[StoneMarker] Không tìm thấy Main Camera!");

        if (markerCanvas == null)
            Debug.LogError("[StoneMarker] Chưa gán MarkerCanvas!");

        // Ẩn Canvas lúc bắt đầu
        if (markerCanvas != null)
            markerCanvas.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (!isActive || target == null)
            return;

        UpdateMarkerPosition();
        UpdateBillboard();
        UpdateVisibility();
    }

    // ==========================================
    // CẬP NHẬT VỊ TRÍ MARKER
    // ==========================================

    private void UpdateMarkerPosition()
    {
        transform.position =
            target.position + Vector3.up * markerHeight;
    }

    // ==========================================
    // BILLBOARD THEO FIRST PERSON CAMERA
    // ==========================================

    private void UpdateBillboard()
    {
        if (playerCamera == null)
            return;

        // Mặt trước của Marker hướng về Camera
        transform.rotation = Quaternion.LookRotation(
            playerCamera.transform.forward,
            playerCamera.transform.up
        );
    }

    // ==========================================
    // HIỆN / ẨN THÔNG TIN THEO KHOẢNG CÁCH
    // ==========================================

    private void UpdateVisibility()
    {
        if (player == null || target == null || markerCanvas == null)
            return;

        float distance = Vector3.Distance(
            player.position,
            target.position
        );

        bool shouldShow = distance <= showDistance;

        if (markerCanvas.gameObject.activeSelf != shouldShow)
        {
            markerCanvas.gameObject.SetActive(shouldShow);

            Debug.Log(
                "[StoneMarker] " +
                (shouldShow ? "HIỆN" : "ẨN") +
                " | Distance: " + distance.ToString("F2")
            );
        }
    }

    // ==========================================
    // HIỂN THỊ MARKER
    // ==========================================

    public void ShowMarker(Transform newTarget, string resourceName)
    {
        if (newTarget == null)
        {
            Debug.LogError("[StoneMarker] Target bị null!");
            return;
        }

        if (player == null)
        {
            Debug.LogError("[StoneMarker] Player chưa được gán!");
            return;
        }

        if (markerCanvas == null)
        {
            Debug.LogError("[StoneMarker] MarkerCanvas chưa được gán!");
            return;
        }

        target = newTarget;
        isActive = true;

        // Cập nhật nội dung
        if (exclamationText != null)
            exclamationText.text = "!";

        if (targetNameText != null)
            targetNameText.text = resourceName.ToUpper();

        if (interactionText != null)
            interactionText.text = "Press E to Pick Up";

        // Bật GameObject
        gameObject.SetActive(true);

        // Cập nhật ngay lập tức
        UpdateMarkerPosition();
        UpdateBillboard();
        UpdateVisibility();

        Debug.Log(
            "[StoneMarker] Hiển thị " + resourceName +
            " | Distance: " +
            Vector3.Distance(player.position, target.position).ToString("F2")
        );
    }

    // ==========================================
    // ẨN MARKER
    // ==========================================

    public void HideMarker()
    {
        isActive = false;

        if (markerCanvas != null)
            markerCanvas.gameObject.SetActive(false);

        gameObject.SetActive(false);
    }
}
