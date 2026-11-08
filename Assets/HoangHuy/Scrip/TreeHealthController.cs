using UnityEngine;
using UnityEngine.UI;

public class TreeHealthController : MonoBehaviour
{
    [Header("Cấu hình loại cây")]
    public float maxHealth = 100f; // Các loại cây khác nhau có thể chỉnh số này (Cây to = máu nhiều, cây nhỏ = máu ít)
    private float currentHealth;

    [Header("UI References")]
    public GameObject healthCanvasObject; // Kéo Canvas chứa Slider vào đây
    public Slider healthSlider;

    [Header("Thời gian tự ẩn")]
    public float hideDelay = 3f; // Sau 3 giây không tương tác sẽ tự ẩn
    private float hideTimer;
    private bool isInteractActive = false;

    void Start()
    {
        currentHealth = maxHealth;
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        // Ban đầu ẩn thanh máu đi
        if (healthCanvasObject != null)
            healthCanvasObject.SetActive(false);
    }

    void Update()
    {
        // Đếm ngược thời gian để ẩn thanh máu
        if (isInteractActive)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0)
            {
                HideHealthBar();
            }
        }
    }

    // Hàm gọi khi MC tương tác/chặt vào cây
    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }

        // Hiện thanh máu và reset thời gian đếm ngược
        ShowHealthBar();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void ShowHealthBar()
    {
        isInteractActive = true;
        hideTimer = hideDelay;
        if (healthCanvasObject != null)
            healthCanvasObject.SetActive(true);
    }

    public void HideHealthBar()
    {
        isInteractActive = false;
        if (healthCanvasObject != null)
            healthCanvasObject.SetActive(false);
    }

    void Die()
    {
        // Xử lý khi cây hết máu (rơi gỗ, biến mất,...)
        Destroy(gameObject);
    }
}