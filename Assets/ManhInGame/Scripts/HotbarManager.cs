using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class HotbarManager : MonoBehaviour
{
    public static HotbarManager Instance;

    public event Action OnHotbarChanged;
    public event Action<int> OnItemSelected;

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
    }

    public void NotifyHotbarChanged()
    {
        OnHotbarChanged?.Invoke();
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index > 6)
            return;

        currentSlot = index;

        OnItemSelected?.Invoke(currentSlot);

        Debug.Log("Selected Hotbar Slot: " + (currentSlot + 1));
    }

    public void SwitchSlot(int direction)
    {
        int newSlot = currentSlot + direction;

        if (newSlot < 0)
        {
            newSlot = 6;
        }
        else if (newSlot > 6)
        {
            newSlot = 0;
        }

        SelectSlot(newSlot);
    }
}
