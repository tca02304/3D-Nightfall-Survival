using System.ComponentModel;
using TMPro;
using UnityEngine;

public class ItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text amountText;

    public void SetAmount(int amount)
    {
        amountText.text = amount.ToString();
    }
}
