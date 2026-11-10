using UnityEngine;

[System.Serializable]
public class RewardItem
{
    public string itemName;      // Tên vật phẩm/buff
    public GameObject itemPrefab; // Prefab của vật phẩm rơi ra
    [Range(0f, 100f)]
    public float dropRate = 50f;  // Tỉ lệ xuất hiện
}

public class ChestOpen : MonoBehaviour
{
    [Header("Cấu hình tương tác")]
    public float interactDistance = 3f; // Khoảng cách tối đa để mở rương
    public bool isOpen = false;

    [Header("Danh sách phần thưởng có thể nhận (Random theo tỉ lệ)")]
    public RewardItem[] possibleRewards; // Kéo thả các item và chỉnh % tỉ lệ vào đây

    void Update()
    {
        // Đã đổi lại thành KeyCode.E để mở rương
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

        // Random phần thưởng dựa trên tỉ lệ dropRate
        GameObject rewardToSpawn = GetRandomReward();
        if (rewardToSpawn != null)
        {
            // Sinh ra vật phẩm lơ lửng trên đầu rương (cao hơn 1 đơn vị)
            Vector3 spawnPos = transform.position + new Vector3(0, 1f, 0);
            Instantiate(rewardToSpawn, spawnPos, Quaternion.identity);
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
                return reward.itemPrefab;
            }
        }

        return possibleRewards[0].itemPrefab;
    }
}