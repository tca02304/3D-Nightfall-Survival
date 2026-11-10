using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class HotbarManager : MonoBehaviour
{
    public static HotbarManager Instance;

    public event Action OnHotbarChanged;
    public event Action<int> OnItemSelected;
    public event Action<ItemData> OnSelectedItemChanged;

    [Header("Hotbar Settings")]
    [SerializeField, Range(0, 6)]
    private int currentSlot = 0;

    private InventoryManager subscribedInventory;

    public int CurrentSlot => currentSlot;

    public int HotbarSize
    {
        get
        {
            if (InventoryManager.Instance == null)
                return 7;

            return InventoryManager.Instance.HotbarSize;
        }
    }

    public int SelectedInventoryIndex
    {
        get
        {
            InventoryManager inventory = InventoryManager.Instance;

            if (inventory == null)
                return -1;

            if (CurrentSlot < 0 || CurrentSlot >= inventory.HotbarSize)
                return -1;

            int index = inventory.InventorySize + currentSlot;

            if (index >= inventory.Slots.Count)
                return -1;

            return index;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        TrySubscribeInventory();

        NotifyHotbarChanged();
        NotifySelectedItemChanged();
    }

    private void Update()
    {
        if(subscribedInventory != InventoryManager.Instance)
        {
            TrySubscribeInventory();
        }

        HandleKeyboardInput();
    }

    private void HandleKeyboardInput()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            SelectSlot(0);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
            SelectSlot(1);
        else if (Keyboard.current.digit3Key.wasPressedThisFrame)
            SelectSlot(2);
        else if (Keyboard.current.digit4Key.wasPressedThisFrame)
            SelectSlot(3);
        else if (Keyboard.current.digit5Key.wasPressedThisFrame)
            SelectSlot(4);
        else if (Keyboard.current.digit6Key.wasPressedThisFrame)
            SelectSlot(5);
        else if (Keyboard.current.digit7Key.wasPressedThisFrame)
            SelectSlot(6);
    }

    private void TrySubscribeInventory()
    {
        InventoryManager inventory = InventoryManager.Instance;

        if (subscribedInventory == inventory)
            return;

        UnsubscribeInventory();

        if (inventory == null)
            return;

        subscribedInventory = inventory;

        subscribedInventory.OnInventoryChanged += HandleInventoryChanged;

        NotifyHotbarChanged();
        NotifySelectedItemChanged();
    }

    private void UnsubscribeInventory()
    {
        if(subscribedInventory != null)
        {
            subscribedInventory.OnInventoryChanged -= HandleInventoryChanged;
        }

        subscribedInventory = null;
    }

    private void OnDisable()
    {
        UnsubscribeInventory();
    }

    private void OnEnable()
    {
        if(Instance == this)
        {
            TrySubscribeInventory();
        }
    }

    private void OnDestroy()
    {
        UnsubscribeInventory();

        if (Instance == this)
            Instance = null;
    }

    private void HandleInventoryChanged()
    {
        NotifyHotbarChanged();
        NotifySelectedItemChanged();
    }

    public void NotifyHotbarChanged()
    {
        OnHotbarChanged?.Invoke();
    }

    private void NotifySelectedItemChanged()
    {
        OnSelectedItemChanged?.Invoke(GetSelectedItem());
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= HotbarSize)
            return;

        if (currentSlot == index)
            return;

        currentSlot = index;

        OnItemSelected?.Invoke(currentSlot);

        NotifySelectedItemChanged();
        NotifyHotbarChanged();

        Debug.Log($"Selected Hotbar Slot: {currentSlot + 1}");
    }

    public void SwitchSlot(int direction)
    {
        int size = HotbarSize;

        if (size <= 0)
            return;

        int newSlot = ((CurrentSlot + direction) % size + size) % size;

        SelectSlot(newSlot);
    }

    public InventorySlotData GetSelectedSlotData()
    {
        InventoryManager inventory = InventoryManager.Instance;

        int index = SelectedInventoryIndex;

        if (inventory == null || index < 0)
            return null;

        return inventory.Slots[index];
    }

    public ItemData GetSelectedItem()
    {
        InventorySlotData slot = GetSelectedSlotData();

        if (slot == null || slot.isEmpty())
            return null;

        if (ItemDatabase.Instance == null)
            return null;

        return ItemDatabase.Instance.GetItemById(slot.ItemId);
    }

    public int GetSelectedAmount()
    {
        InventorySlotData slot = GetSelectedSlotData();

        if (slot == null || slot.isEmpty())
            return 0;

        return slot.Amount;
    }

    public bool HasSelectedItem()
    {
        InventorySlotData slot = GetSelectedSlotData();

        return slot != null && !slot.isEmpty();
    }
}