using System.Collections;
using UnityEngine;

public class TreeHealths : MonoBehaviour
{
    [Header("Dữ liệu cấu hình cây (ScriptableObject)")]
    public TreeData treeData; // Kéo file asset TreeData vào đây

    [Header("Chỉ số hiện tại")]
    public float currentHealth;

    [Header("Tham chiếu UI HUD")]
    public TargetHPFollower hpUI;
    public Transform dropSpawnPoint; // Điểm văng vật phẩm (tùy chọn)

    private Vector3 originalPosition;
    private bool isShaking = false;

    void Start()
    {
        // Khởi tạo máu dựa vào ScriptableObject
        if (treeData != null)
        {
            currentHealth = treeData.maxHealth;
        }
        else
        {
            currentHealth = 100f; // Giá trị mặc định phòng hờ
        }

        // Tự tìm UI nếu chưa gán
        if (hpUI == null)
        {
            hpUI = FindFirstObjectByType<TargetHPFollower>(FindObjectsInactive.Include);
        }
    }

    void Update()
    {
        // Phím test nhanh (Bấm Space để trừ máu cây)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TakeDamage(20f);
        }
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        float maxHp = treeData != null ? treeData.maxHealth : 100f;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHp);

        // Kích hoạt hiệu ứng rung theo thông số trong ScriptableObject
        if (!isShaking && treeData != null)
        {
            StartCoroutine(ShakeTree());
        }

        // Cập nhật giao diện thanh máu
        if (hpUI != null)
        {
            hpUI.ShowHP(transform, currentHealth, maxHp);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    IEnumerator ShakeTree()
    {
        isShaking = true;
        originalPosition = transform.localPosition;
        float elapsed = 0.0f;

        float duration = treeData != null ? treeData.shakeDuration : 0.15f;
        float magnitude = treeData != null ? treeData.shakeMagnitude : 0.1f;

        while (elapsed < duration)
        {
            float offsetX = Random.Range(-1f, 1f) * magnitude;
            float offsetZ = Random.Range(-1f, 1f) * magnitude;

            transform.localPosition = new Vector3(originalPosition.x + offsetX, originalPosition.y, originalPosition.z + offsetZ);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPosition;
        isShaking = false;
    }

    void Die()
    {
        if (hpUI != null) hpUI.Hide();
        SpawnLoot();
        Destroy(gameObject);
    }

    void SpawnLoot()
    {
        if (treeData != null && treeData.woodPrefab != null)
        {
            int dropCount = Random.Range(treeData.minWoodCount, treeData.maxWoodCount + 1);
            Vector3 spawnPos = dropSpawnPoint != null ? dropSpawnPoint.position : transform.position + Vector3.up * 2f;

            for (int i = 0; i < dropCount; i++)
            {
                Vector3 randomOffset = new Vector3(Random.Range(-1f, 1f), 0.5f, Random.Range(-1f, 1f));
                GameObject droppedItem = Instantiate(treeData.woodPrefab, spawnPos + randomOffset, Quaternion.identity);

                Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 randomForce = new Vector3(Random.Range(-2f, 2f), Random.Range(3f, 5f), Random.Range(-2f, 2f));
                    rb.AddForce(randomForce, ForceMode.Impulse);
                }
            }
        }
    }
}