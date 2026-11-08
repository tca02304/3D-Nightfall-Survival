using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemUI : MonoBehaviour
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text amountText;

    public void SetItem(ItemData item, int amount)
    {
        if(item == null)
        {
            ClearItem();
            return;
        }

        Sprite icon = ItemDatabase.Instance.GetIconByKey(item.iconKey);

        if (icon == null)
        {
            Debug.LogWarning(
                $"[ItemUI] Missing icon: Item={item.itemCode}, IconKey={item.iconKey}"
            );
        }

        itemIcon.sprite = icon;
        itemIcon.color = Color.white;
        itemIcon.enabled = icon != null;

        SetAmount(amount);
    }

    public void SetAmount(int amount)
    {
        amountText.text = amount.ToString();
    }

    public void ClearItem()
    {
        itemIcon.sprite = null;
        itemIcon.enabled = false;
        amountText.text = "";
    }
}
