
using UnityEngine;

public class QuestGuideController : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private Transform playerTarget;
    [SerializeField] private Transform stoneTarget;
    [SerializeField] private Transform woodTarget;

    [Header("Visuals")]
    [SerializeField] private StoneMarker stoneMarkerUI;
    [SerializeField] private DirectionIndicator directionIndicator;

    private ResourceType currentGuideType;
    private bool isGuiding;

    private void Awake()
    {
        // Ẩn toàn bộ hướng dẫn khi bắt đầu
        HideGuide();
    }

    private void Start()
    {
        // Đăng ký Observer
        if (ResourceInventory.Instance != null)
        {
            ResourceInventory.Instance.OnResourceChanged += HandleResourceChanged;
        }
        else
        {
            Debug.LogError(
                "[Quest Guide] Không tìm thấy ResourceInventory!"
            );
        }
    }

    private void OnDestroy()
    {
        // Hủy đăng ký Observer
        if (ResourceInventory.Instance != null)
        {
            ResourceInventory.Instance.OnResourceChanged -= HandleResourceChanged;
        }
    }

    // ==========================================
    // BẮT ĐẦU HƯỚNG DẪN
    // ==========================================

    public void ShowStoneGuide()
    {
        ShowResourceGuide(ResourceType.Stone, stoneTarget);
    }

    public void ShowWoodGuide()
    {
        ShowResourceGuide(ResourceType.Wood, woodTarget);
    }

    private void ShowResourceGuide(ResourceType type, Transform target)
    {
        // Kiểm tra mục tiêu
        if (target == null)
        {
            Debug.LogError(
                "[Quest Guide] Chưa gán mục tiêu " + type
            );
            return;
        }

        if (ResourceInventory.Instance == null)
        {
            Debug.LogError(
                "[Quest Guide] ResourceInventory chưa được khởi tạo!"
            );
            return;
        }

        // Không hướng dẫn nếu đã có tài nguyên
        if (ResourceInventory.Instance.GetAmount(type) > 0)
        {
            HideGuide();
            return;
        }

        currentGuideType = type;
        isGuiding = true;

        // Hiển thị vòng tròn chỉ đường
        if (directionIndicator != null)
        {
            directionIndicator.ShowGuide(target);
        }

        // Hiển thị Marker trên tài nguyên
        if (stoneMarkerUI != null)
        {
            string resourceName = type == ResourceType.Stone
                ? "STONE"
                : "WOOD";

            stoneMarkerUI.ShowMarker(target, resourceName);
        }

        Debug.Log(
            "[Quest Guide] Đang chỉ đường đến " + type
        );
    }

    // ==========================================
    // OBSERVER - THEO DÕI TÀI NGUYÊN
    // ==========================================

    private void HandleResourceChanged(
        ResourceType type,
        int totalAmount
    )
    {
        if (!isGuiding || totalAmount <= 0)
            return;

        // Người chơi nhặt Stone đầu tiên
        if (currentGuideType == ResourceType.Stone &&
            type == ResourceType.Stone)
        {
            Debug.Log(
                "[Quest Guide] Đã nhặt Stone đầu tiên!"
            );

            HideGuide();

            // Chuyển sang hướng dẫn Wood
            if (ResourceInventory.Instance.GetAmount(ResourceType.Wood) == 0)
            {
                ShowWoodGuide();
            }
            else
            {
                Debug.Log(
                    "[Quest Guide] Người chơi đã có Wood."
                );
            }
        }

        // Người chơi nhặt Wood đầu tiên
        else if (currentGuideType == ResourceType.Wood &&
                 type == ResourceType.Wood)
        {
            Debug.Log(
                "[Quest Guide] Đã nhặt Wood đầu tiên!"
            );

            HideGuide();

            Debug.Log(
                "[Quest Guide] Hoàn thành hướng dẫn tài nguyên!"
            );
        }
    }

    // ==========================================
    // ẨN HƯỚNG DẪN
    // ==========================================

    public void HideGuide()
    {
        isGuiding = false;

        if (directionIndicator != null)
        {
            directionIndicator.HideGuide();
        }

        if (stoneMarkerUI != null)
        {
            stoneMarkerUI.HideMarker();
        }
    }
}
