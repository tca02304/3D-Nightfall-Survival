using UnityEngine;
using UnityEngine.UI;

public class TargetHPFollower : MonoBehaviour
{
    [Header("UI Component")]
    public Slider hpSlider;

    [Header("Cấu hình lệch độ cao trên đầu cây")]
    public Vector3 worldOffset = new Vector3(0, 3.5f, 0); // Chỉnh độ cao thanh HP so với gốc cây

    [Header("Thời gian tự ẩn")]
    public float hideDelay = 3f;

    private Transform targetTreeTransform;
    private Camera mainCamera;
    private float hideTimer;
    private bool isActive = false;

    void Awake()
    {
        mainCamera = Camera.main;
        if (hpSlider == null) hpSlider = GetComponent<Slider>();
        gameObject.SetActive(false); // Ban đầu ẩn đi
    }

    void LateUpdate()
    {
        if (!isActive || targetTreeTransform == null) return;

        // 1. Cập nhật vị trí UI 2D theo vị trí 3D của cây
        Vector3 screenPos = mainCamera.WorldToScreenPoint(targetTreeTransform.position + worldOffset);

        // Kiểm tra xem cây có nằm phía trước Camera không
        if (screenPos.z > 0)
        {
            transform.position = screenPos;
        }

        // 2. Đếm ngược thời gian tự ẩn
        hideTimer -= Time.deltaTime;
        if (hideTimer <= 0)
        {
            Hide();
        }
    }

    // Hàm gọi khi MC chặt/tương tác với cây
    public void ShowHP(Transform treeTransform, float currentHP, float maxHP)
    {
        targetTreeTransform = treeTransform;

        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHP;
            hpSlider.value = currentHP;
        }

        hideTimer = hideDelay;
        isActive = true;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        isActive = false;
        gameObject.SetActive(false);
    }
}