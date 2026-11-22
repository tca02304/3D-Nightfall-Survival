using UnityEngine;

public class PlayerBuilding : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [Tooltip("Điểm trống (Empty Object) đặt ở bàn tay Player hoặc trước Camera để gắn vật phẩm đang cầm")]
    [SerializeField] private Transform handHoldPoint;

    [Header("Quick Equip (Phím 1, 2, 3... để cầm đồ)")]
    [SerializeField] private BuildableItemData[] buildableItems;

    [Header("Build Settings")]
    [Tooltip("Khoảng cách xây tối đa tính từ chân Player")]
    [SerializeField] private float maxBuildDistance = 5f;
    [SerializeField] private float rotationStep = 45f;
    [Tooltip("Layer mặt đất (ví dụ: Group / Ground)")]
    [SerializeField] private LayerMask groundLayer;
    [Tooltip("Layer vật cản (Obstacle, Player...) - KHÔNG tích Layer mặt đất vào đây")]
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Preview Materials")]
    [SerializeField] private Material validMaterial;   // Màu xanh trong suốt
    [SerializeField] private Material invalidMaterial; // Màu đỏ trong suốt

    private BuildableItemData currentItem;
    private GameObject currentHandModel;
    private GameObject currentPreview;
    private Renderer[] previewRenderers;
    private Bounds previewLocalBounds;

    private float currentRotationY;
    private bool canPlace;

    public bool IsBuilding => currentItem != null && currentPreview != null;

    private void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Update()
    {
        HandleQuickEquipInput();

        if (!IsBuilding) return;

        HandleRotationInput();
        UpdatePreviewPosition();

        // Chuột trái để đặt công trình
        if (Input.GetMouseButtonDown(0) && canPlace)
        {
            PlaceBuilding();
        }

        // Chuột phải hoặc ESC để cất vật phẩm và hủy xây dựng
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            UnequipItem();
        }
    }

    private void HandleQuickEquipInput()
    {
        if (buildableItems == null) return;

        for (int i = 0; i < buildableItems.Length && i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                if (currentItem == buildableItems[i])
                    UnequipItem();
                else
                    EquipItem(buildableItems[i]);
            }
        }
    }

    /// <summary>
    /// Gọi hàm này khi Player cầm một vật phẩm xây dựng lên tay
    /// </summary>
    public void EquipItem(BuildableItemData itemData)
    {
        UnequipItem();

        if (itemData == null || itemData.buildPrefab == null) return;

        currentItem = itemData;
        currentRotationY = 0f;

        // 1. Hiện model vật phẩm trên tay Player
        if (itemData.handPrefab != null && handHoldPoint != null)
        {
            currentHandModel = Instantiate(itemData.handPrefab, handHoldPoint);
            currentHandModel.transform.localPosition = Vector3.zero;
            currentHandModel.transform.localRotation = Quaternion.identity;

            foreach (var col in currentHandModel.GetComponentsInChildren<Collider>())
                col.enabled = false;

            foreach (var rb in currentHandModel.GetComponentsInChildren<Rigidbody>())
                rb.isKinematic = true;
        }

        // 2. Tạo bản mô phỏng (Ghost) từ Prefab thật
        currentPreview = Instantiate(itemData.buildPrefab);
        currentPreview.name = itemData.buildPrefab.name + "_Preview";

        foreach (var col in currentPreview.GetComponentsInChildren<Collider>())
            col.enabled = false;

        foreach (var script in currentPreview.GetComponentsInChildren<MonoBehaviour>())
            script.enabled = false;

        previewRenderers = currentPreview.GetComponentsInChildren<Renderer>();
        CalculateLocalBounds();
    }

    /// <summary>
    /// Gọi hàm này để cất vật phẩm khỏi tay và tắt bản mô phỏng
    /// </summary>
    public void UnequipItem()
    {
        if (currentHandModel != null)
        {
            Destroy(currentHandModel);
            currentHandModel = null;
        }

        if (currentPreview != null)
        {
            Destroy(currentPreview);
            currentPreview = null;
        }

        currentItem = null;
        previewRenderers = null;
        canPlace = false;
    }

    private void HandleRotationInput()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            currentRotationY += rotationStep;
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            currentRotationY += scroll > 0 ? rotationStep : -rotationStep;
        }
    }

    private void UpdatePreviewPosition()
    {
        if (playerCamera == null) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 desiredPos;

        // 1. Tìm điểm tâm màn hình đang nhìn trên mặt đất
        if (Physics.Raycast(ray, out RaycastHit hit, 50f, groundLayer))
        {
            desiredPos = hit.point;
        }
        else
        {
            // Nếu nhìn thẳng hoặc ngước lên trời, lấy hướng phía trước Camera
            Vector3 flatForward = playerCamera.transform.forward;
            flatForward.y = 0f;
            desiredPos = transform.position + flatForward.normalized * maxBuildDistance;
        }

        // 2. Giới hạn khoảng cách xây tối đa tính từ vị trí Player
        Vector3 offset = desiredPos - transform.position;
        offset.y = 0f;

        if (offset.magnitude > maxBuildDistance)
        {
            offset = offset.normalized * maxBuildDistance;
        }

        Vector3 clampedPos = transform.position + offset;

        // 3. Chiếu thẳng từ trên xuống để bám đúng độ cao mặt đất (Terrain)
        if (Physics.Raycast(clampedPos + Vector3.up * 10f, Vector3.down, out RaycastHit groundHit, 30f, groundLayer))
        {
            currentPreview.SetActive(true);
            currentPreview.transform.position = groundHit.point;
            currentPreview.transform.rotation = Quaternion.Euler(0f, currentRotationY, 0f);

            Vector3 checkCenter = currentPreview.transform.TransformPoint(previewLocalBounds.center);
            Vector3 halfExtents = previewLocalBounds.extents * 0.9f;

            bool isBlocked = Physics.CheckBox(checkCenter, halfExtents, currentPreview.transform.rotation, obstacleLayer);
            canPlace = !isBlocked;

            SetPreviewMaterial(canPlace ? validMaterial : invalidMaterial);
        }
        else
        {
            currentPreview.SetActive(false);
            canPlace = false;
        }
    }

    private void PlaceBuilding()
    {
        if (currentItem == null || currentPreview == null) return;

        GameObject placedObj = Instantiate(
            currentItem.buildPrefab,
            currentPreview.transform.position,
            currentPreview.transform.rotation
        );

        // Tự động chuyển công trình vừa xây sang Layer "Obstacle" để chặn xây chồng lên nhau
        int obstacleLayerIndex = LayerMask.NameToLayer("Obstacle");
        if (obstacleLayerIndex != -1)
        {
            SetLayerRecursively(placedObj, obstacleLayerIndex);
        }

        if (currentItem.consumeOnPlace)
        {
            UnequipItem();
        }
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    private void CalculateLocalBounds()
    {
        if (previewRenderers == null || previewRenderers.Length == 0)
        {
            previewLocalBounds = new Bounds(Vector3.zero, Vector3.one);
            return;
        }

        Bounds worldBounds = previewRenderers[0].bounds;
        for (int i = 1; i < previewRenderers.Length; i++)
            worldBounds.Encapsulate(previewRenderers[i].bounds);

        Vector3 localCenter = currentPreview.transform.InverseTransformPoint(worldBounds.center);
        previewLocalBounds = new Bounds(localCenter, worldBounds.size);
    }

    private void SetPreviewMaterial(Material mat)
    {
        if (mat == null || previewRenderers == null) return;

        foreach (var rend in previewRenderers)
        {
            Material[] mats = new Material[rend.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++)
                mats[i] = mat;

            rend.sharedMaterials = mats;
        }
    }
}