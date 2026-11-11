
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text amountText;

    private void Awake()
    {
        // [ADD] Recover references after prefab changes.
        if (itemIcon == null)
        {
            Transform iconTransform = transform.Find("ItemIcon");

            if (iconTransform != null)
                itemIcon = iconTransform.GetComponent<Image>();
        }

        if (amountText == null && itemIcon != null)
        {
            amountText =
                itemIcon.GetComponentInChildren<TMP_Text>(true);
        }

        if (itemIcon == null || amountText == null)
        {
            Debug.LogError(
                $"[ItemUI] Missing UI references on {gameObject.name}",
                this);
        }
    }

    public void SetItem(ItemData item, int amount)
    {
        if (item == null || amount <= 0)
        {
            ClearItem();
            return;
        }

        if (itemIcon == null)
        {
            Debug.LogError("[ItemUI] ItemIcon not assigned.", this);
            return;
        }

        ItemDatabase database = ItemDatabase.Instance;

        if (database == null)
        {
            Debug.LogWarning("[ItemUI] ItemDatabase not ready.", this);
            return;
        }

        Sprite icon = database.GetIconByKey(item.iconKey);

        if (icon == null)
        {
            Debug.LogWarning(
                $"[ItemUI] Missing icon: " +
                $"Item={item.itemCode}, IconKey={item.iconKey}",
                this);
        }

        itemIcon.sprite = icon;
        itemIcon.color = Color.white;
        itemIcon.enabled = icon != null;

        SetAmount(amount);
    }

    public void SetAmount(int amount)
    {
        if (amountText == null)
            return;

        // Show stack count only when greater than 1.
        amountText.text = amount > 1
            ? amount.ToString()
            : string.Empty;
    }

    public void ClearItem()
    {
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }

        if (amountText != null)
            amountText.text = string.Empty;
    }
}
