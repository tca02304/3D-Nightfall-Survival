using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MobHealthBar : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    [Header("Position & Dimensions")]
    public float heightOffset = 2.4f;       // Độ cao lơ lửng trên đầu mob (m)
    public Vector2 barSize = new Vector2(1.2f, 0.15f); // Kích thước thanh máu (rộng x cao)

    [Header("Colors")]
    public Color fullHealthColor = new Color(0.18f, 0.8f, 0.44f);  // Xanh lá tươi
    public Color midHealthColor = new Color(0.95f, 0.77f, 0.06f);   // Vàng
    public Color lowHealthColor = new Color(0.91f, 0.3f, 0.24f);    // Đỏ nguy cấp
    public Color backgroundColor = new Color(0.1f, 0.1f, 0.12f, 0.85f); // Nền xám đen

    private Transform barTransform;
    private Image fillImage;
    private Text hpText;
    private Camera targetCamera;
    private float targetFill = 1f;
    private bool isDead = false;

    private void Awake()
    {
        // 1. Loại trừ Player: Không tạo thanh máu nếu là Player
        if (CompareTag("Player") || gameObject.name.ToLower().Contains("player"))
        {
            Destroy(this);
            return;
        }

        currentHealth = maxHealth;

        // 2. Tự động tính độ cao thanh máu theo chiều cao collider của Mob
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            heightOffset = col.bounds.size.y + 0.35f;
        }

        // 3. Khởi tạo UI thanh máu
        CreateHealthBarUI();
    }

    private void Start()
    {
        targetCamera = Camera.main;
        UpdateBarDisplay(currentHealth, maxHealth);
    }

    private void LateUpdate()
    {
        if (isDead || barTransform == null) return;

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null) return;
        }

        // 1. Cập nhật vị trí luôn nằm trên đỉnh đầu mob
        barTransform.position = transform.position + Vector3.up * heightOffset;

        // 2. BILLBOARD: Luôn luôn quay mặt đối diện chuẩn xác với Camera
        barTransform.rotation = targetCamera.transform.rotation;

        // 3. Hiệu ứng rút máu mượt mà
        if (fillImage != null)
        {
            fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, targetFill, Time.deltaTime * 10f);
            
            // Đổi màu theo lượng máu còn lại
            if (fillImage.fillAmount > 0.5f)
            {
                fillImage.color = Color.Lerp(midHealthColor, fullHealthColor, (fillImage.fillAmount - 0.5f) * 2f);
            }
            else
            {
                fillImage.color = Color.Lerp(lowHealthColor, midHealthColor, fillImage.fillAmount * 2f);
            }
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateBarDisplay(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (isDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateBarDisplay(currentHealth, maxHealth);
    }

    private void UpdateBarDisplay(float current, float max)
    {
        targetFill = (max > 0) ? Mathf.Clamp01(current / max) : 0f;

        if (hpText != null)
        {
            hpText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }
    }

    private void Die()
    {
        isDead = true;

        if (barTransform != null)
        {
            barTransform.gameObject.SetActive(false);
        }

        // Tắt AI và Animator
        var navAI = GetComponent<NavMeshEnemyAI>();
        if (navAI != null) navAI.enabled = false;

        var gAI = GetComponent<GoblinAI>();
        if (gAI != null) gAI.enabled = false;

        var wAI = GetComponent<WolfAI>();
        if (wAI != null) wAI.enabled = false;

        var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        var anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.Play("Die", 0, 0f);
        }

        Destroy(gameObject, 3.0f);
    }

    private void CreateHealthBarUI()
    {
        // 1. Tạo Root GameObject chứa World-Space Canvas
        GameObject canvasObj = new GameObject("Mob_HealthBar_Canvas");
        barTransform = canvasObj.transform;
        barTransform.position = transform.position + Vector3.up * heightOffset;

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100f;

        float scaleFactor = 0.01f;
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(barSize.x / scaleFactor, barSize.y / scaleFactor);
        canvasRect.localScale = Vector3.one * scaleFactor;

        Sprite whiteSprite = CreateWhiteSprite();

        // 2. Background Image (Nền tối)
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(barTransform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.sprite = whiteSprite;
        bgImg.color = backgroundColor;

        // 3. Foreground Fill Image (Thanh máu)
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(barTransform, false);
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0.03f, 0.15f);
        fillRect.anchorMax = new Vector2(0.97f, 0.85f);
        fillRect.sizeDelta = Vector2.zero;
        fillImage = fillObj.AddComponent<Image>();
        fillImage.sprite = whiteSprite;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.color = fullHealthColor;

        // 4. HP Text hiển thị chỉ số máu
        GameObject textObj = new GameObject("HP_Text");
        textObj.transform.SetParent(barTransform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        hpText = textObj.AddComponent<Text>();
        hpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        hpText.alignment = TextAnchor.MiddleCenter;
        hpText.color = Color.white;
        hpText.fontSize = 11;
        hpText.raycastTarget = false;

        Shadow shadow = textObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.9f);
        shadow.effectDistance = new Vector2(1, -1);
    }

    private void OnDestroy()
    {
        // Khi mob bị xóa, xóa luôn Canvas thanh máu đi kèm
        if (barTransform != null)
        {
            Destroy(barTransform.gameObject);
        }
    }

    private Sprite CreateWhiteSprite()
    {
        Texture2D tex = new Texture2D(2, 2);
        tex.SetPixels(new Color[] { Color.white, Color.white, Color.white, Color.white });
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
    }

    [ContextMenu("Test Take 20 Damage")]
    public void TestTakeDamage()
    {
        TakeDamage(20f);
    }
}
