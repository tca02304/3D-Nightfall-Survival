using UnityEngine;

public enum ItemRarity
{
    Common,   // Đồ phổ biến
    Rare,     // Đồ hiếm
    Epic      // Đồ cực hiếm
}

[System.Serializable]
public class RewardItem
{
    public string itemName;
    public GameObject itemPrefab;
    public ItemRarity rarity;

    [Range(0f, 100f)]
    public float dropRate = 50f;
}

public class ChestOpen : MonoBehaviour
{
    [Header("Cấu hình tương tác")]
    public float interactDistance = 3f;
    public bool isOpen = false;

    [Header("Animator của Rương")]
    private Animator anim;

    [Header("Điểm gắn vật phẩm (Nắp rương)")]
    [Tooltip("Kéo đối tượng nắp rương vào đây để vật phẩm bay lên cùng nắp")]
    public Transform lidTransform;

    [Header("Danh sách phần thưởng")]
    public RewardItem[] possibleRewards;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        if (anim == null)
        {
            anim = GetComponent<Animator>();
        }
    }

    void Update()
    {
        if (!isOpen && Input.GetKeyDown(KeyCode.E))
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null && Vector3.Distance(transform.position, player.transform.position) <= interactDistance)
            {
                OpenChest();
            }
        }
    }

    void OpenChest()
    {
        isOpen = true;
        Debug.Log("Rương đã mở bằng phím E!");

        // 1. Chạy Animator mở rương
        if (anim != null)
        {
            anim.SetTrigger("Use");
        }

        // 2. Random và sinh ra ĐÚNG 1 phần thưởng duy nhất
        GameObject rewardToSpawn = GetRandomReward();
        if (rewardToSpawn != null)
        {
            if (lidTransform != null)
            {
                // Sinh vật phẩm làm con của nắp rương ở chính giữa tâm
                GameObject spawnedItem = Instantiate(rewardToSpawn, lidTransform);
                spawnedItem.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                spawnedItem.transform.localRotation = Quaternion.identity;
            }
            else
            {
                // Sinh ra phía trên rương nếu không dùng nắp
                Vector3 spawnPos = transform.position + new Vector3(0f, 1.5f, 0f);
                Instantiate(rewardToSpawn, spawnPos, Quaternion.identity);
            }

            Debug.Log("✨ Đã rơi ra đúng 1 vật phẩm từ rương!");
        }
    }

    GameObject GetRandomReward()
    {
        if (possibleRewards == null || possibleRewards.Length == 0) return null;

        float totalWeight = 0f;
        foreach (var reward in possibleRewards)
        {
            totalWeight += reward.dropRate;
        }

        float randomValue = Random.Range(0f, totalWeight);
        float currentSum = 0f;

        foreach (var reward in possibleRewards)
        {
            currentSum += reward.dropRate;
            if (randomValue <= currentSum)
            {
                Debug.Log($"🎉 Random trúng đồ loại [{reward.rarity}]: {reward.itemName}");
                return reward.itemPrefab;
            }
        }

        return possibleRewards[0].itemPrefab;
    }
}