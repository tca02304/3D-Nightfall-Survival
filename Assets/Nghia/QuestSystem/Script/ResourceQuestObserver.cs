
using UnityEngine;

public class ResourceQuestObserver : MonoBehaviour
{
    private ResourceInventory inventory;

    private int previousWood;
    private int previousStone;

    private void Start()
    {
        inventory = ResourceInventory.Instance;

        if (inventory == null)
        {
            Debug.LogError(
                "[Quest] Không tìm thấy ResourceInventory!"
            );
            return;
        }

        // Lấy số lượng hiện tại làm mốc ban đầu.
        previousWood = inventory.GetAmount(ResourceType.Wood);
        previousStone = inventory.GetAmount(ResourceType.Stone);

        // Đăng ký lắng nghe sự kiện.
        inventory.OnResourceChanged += HandleResourceChanged;

        Debug.Log("[Quest] Resource Observer đã đăng ký.");
    }

    private void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.OnResourceChanged -= HandleResourceChanged;
        }
    }

    private void HandleResourceChanged(
        ResourceType type,
        int currentAmount)
    {
        int amountCollected = 0;

        switch (type)
        {
            case ResourceType.Wood:
                amountCollected = currentAmount - previousWood;
                previousWood = currentAmount;
                break;

            case ResourceType.Stone:
                amountCollected = currentAmount - previousStone;
                previousStone = currentAmount;
                break;
        }

        if (amountCollected <= 0)
            return;

        string targetId = type == ResourceType.Wood
            ? "1"
            : "6";

        QuestEvents.Raise(
            QuestObjectiveType.CollectResource,
            targetId,
            amountCollected
        );

        Debug.Log(
            $"[Quest] Collected {type}: +{amountCollected}"
        );
    }
}
