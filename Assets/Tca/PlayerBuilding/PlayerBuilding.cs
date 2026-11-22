using UnityEngine;

public class PlayerBuilding : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [Tooltip("Prefab mẫu để test nhanh bằng phím B")]
    [SerializeField] private GameObject testBuildPrefab;

    [Header("Build Settings")]
    [SerializeField] private float maxBuildDistance = 8f;
    [SerializeField] private float rotationStep = 45f;
    [Tooltip("Layer mặt đất hoặc sàn có thể đặt công trình lên")]
    [SerializeField] private LayerMask groundLayer;
    [Tooltip("Layer vật cản (cây, đá, nhà khác, quái...) không được đặt đè lên")]
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Preview Materials")]
    [SerializeField] private Material validMaterial;   // Màu xanh trong suốt
    [SerializeField] private Material invalidMaterial; // Màu đỏ trong suốt

    private GameObject currentRealPrefab;
    private GameObject currentPreview;
    private Renderer[] previewRenderers;
    private Bounds previewLocalBounds;

    private float currentRotationY;
    private bool canPlace;

    public bool IsBuilding => currentPreview != null;

    private void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Update()
    {
        // Phím B để bật/tắt chế độ test xây dựng nhanh
        if (Input.GetKeyDown(KeyCode.B) && testBuildPrefab != null)
        {
            if (IsBuilding) CancelBuilding();
            else StartBuilding(testBuildPrefab);
        }

        if (!IsBuilding) return;

        HandleRotationInput();
        UpdatePreviewPosition();

        // Chuột trái để đặt công trình
        if (Input.GetMouseButtonDown(0) && canPlace)
        {
            PlaceBuilding();
        }

        // Chuột phải hoặc ESC để hủy xây dựng
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelBuilding();
        }
    }

    /// <summary>
    /// Gọi hàm này khi Player cầm vật dụng xây dựng trên tay
    /// </summary>
    public void StartBuilding(GameObject prefabToBuild)
    {
        CancelBuilding();

        if (prefabToBuild == null) return;

        currentRealPrefab = prefabToBuild;
        currentRotationY = 0f;

        // Tạo bản mô phỏng từ chính Prefab thật
        currentPreview = Instantiate(prefabToBuild);
        currentPreview.name = prefabToBuild.name + "_Preview";

        // Tắt toàn bộ Collider và Script trên bản mô phỏng để không va chạm hay chạy logic
        foreach (var col in currentPreview.GetComponentsInChildren<Collider>())
            col.enabled = false;

        foreach (var script in currentPreview.GetComponentsInChildren<MonoBehaviour>())
            script.enabled = false;

        // Lưu lại Renderer và kích thước để đổi màu & kiểm tra vướng vật cản
        previewRenderers = currentPreview.GetComponentsInChildren<Renderer>();
        CalculateLocalBounds();
    }

    /// <summary>
    /// Gọi hàm này khi Player cất vật dụng xây dựng đi hoặc hủy đặt
    /// </summary>
    public void CancelBuilding()
    {
        if (currentPreview != null)
        {
            Destroy(currentPreview);
            currentPreview = null;
        }

        currentRealPrefab = null;
        previewRenderers = null;
        canPlace = false;
    }

    private void HandleRotationInput()
    {
        // Bấm R để xoay theo góc cố định (45 độ)
        if (Input.GetKeyDown(KeyCode.R))
        {
            currentRotationY += rotationStep;
        }

        // Hoặc lăn chuột giữa để xoay
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            currentRotationY += scroll > 0 ? rotationStep : -rotationStep;
        }
    }

    private void UpdatePreviewPosition()
    {
        if (playerCamera == null) return;

        // Bắn tia từ tâm màn hình ra phía trước
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, maxBuildDistance, groundLayer))
        {
            currentPreview.SetActive(true);
            currentPreview.transform.position = hit.point;
            currentPreview.transform.rotation = Quaternion.Euler(0f, currentRotationY, 0f);

            // Kiểm tra xem vị trí này có bị đè lên vật cản không
            Vector3 checkCenter = currentPreview.transform.TransformPoint(previewLocalBounds.center);
            Vector3 halfExtents = previewLocalBounds.extents * 0.9f;

            bool isBlocked = Physics.CheckBox(checkCenter, halfExtents, currentPreview.transform.rotation, obstacleLayer);
            canPlace = !isBlocked;

            SetPreviewMaterial(canPlace ? validMaterial : invalidMaterial);
        }
        else
        {
            // Nếu nhìn lên trời hoặc quá tầm xây dựng thì ẩn bản mô phỏng
            currentPreview.SetActive(false);
            canPlace = false;
        }
    }

    private void PlaceBuilding()
    {
        if (currentRealPrefab == null || currentPreview == null) return;

        Instantiate(
            currentRealPrefab,
            currentPreview.transform.position,
            currentPreview.transform.rotation
        );

        // Nếu muốn đặt xong 1 cái thì thoát chế độ xây luôn thì bỏ comment dòng dưới:
        // CancelBuilding();
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