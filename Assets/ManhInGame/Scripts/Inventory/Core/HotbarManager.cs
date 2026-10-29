using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class HotbarManager : MonoBehaviour
{
    public static HotbarManager Instance;

    public event Action OnHotbarChanged;
    public event Action<int> OnItemSelected;
    public event Action<ItemData> OnSelectedItemChanged;

    private int currentSlot = 0;

    public int CurrentSlot => currentSlot;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            SelectSlot(0);
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            SelectSlot(1);
        if (Keyboard.current.digit3Key.wasPressedThisFrame)
            SelectSlot(2);
        if (Keyboard.current.digit4Key.wasPressedThisFrame)
            SelectSlot(3);
        if (Keyboard.current.digit5Key.wasPressedThisFrame)
            SelectSlot(4);
        if (Keyboard.current.digit6Key.wasPressedThisFrame)
            SelectSlot(5);
        if (Keyboard.current.digit7Key.wasPressedThisFrame)
            SelectSlot(6);
        if (Keyboard.current.digit8Key.wasPressedThisFrame)
            SelectSlot(7);
    }

    private void OnEnable()
    {
        if(InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += HandleInventoryChanged;
        }
    }

    private void OnDisable()
    {
        if(InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= HandleInventoryChanged;
        }
    }

    private void HandleInventoryChanged()
    {
        NotifySelectedItemChanged();
    }

    public void NotifyHotbarChanged()
    {
        OnHotbarChanged?.Invoke();
    }

    private void NotifySelectedItemChanged()
    {
        ItemData selectedItem = GetSelectedItem();

        OnSelectedItemChanged?.Invoke(selectedItem);
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index > 6)
            return;

        currentSlot = index;

        OnItemSelected?.Invoke(currentSlot);

        NotifySelectedItemChanged();

        Debug.Log("Selected Hotbar Slot: " + (currentSlot + 1));
    }

    public void SwitchSlot(int direction)
    {
        int newSlot = currentSlot + direction;

        if (newSlot < 0)
        {
            newSlot = 7;
        }
        else if (newSlot > 7)
        {
            newSlot = 0;
        }

        SelectSlot(newSlot);
    }

    public InventorySlotData GetSelectedSlotData()
    {
        if (InventoryManager.Instance == null)
            return null;

        int dataIndex = InventoryManager.Instance.InventorySize + currentSlot;

        if (dataIndex < 0 || dataIndex >= InventoryManager.Instance.Slots.Count)
        {
            return null;
        }

        return InventoryManager.Instance.Slots[dataIndex];
    }

    public ItemData GetSelectedItem()
    {
        InventorySlotData slotData = GetSelectedSlotData();

        if (slotData == null || slotData.isEmpty())
            return null;

        if (ItemDatabase.Instance == null)
            return null;

        return ItemDatabase.Instance.GetItemById(slotData.ItemId);
    }
}