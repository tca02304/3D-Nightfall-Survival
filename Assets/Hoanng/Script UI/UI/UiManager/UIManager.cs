using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Panels")]
    public UiBase uiSetting;
    public UiBase uiInventory;
    public HUDMenuPanel uiHud; // Kéo object HUD vào đây trong Inspector

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Khi game bắt đầu, luôn bật giao diện HUD lên
        if (uiHud != null)
        {
            uiHud.Show();
        }
    }

    private void Update()
    {
        // Bật/tắt Inventory bằng phím I hoặc Tab
        if (Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Tab))
        {
            if (uiInventory != null)
            {
                if (uiInventory.gameObject.activeInHierarchy) uiInventory.Hide();
                else uiInventory.Show();
            }
        }
    }

    public void ShowSetting()
    {
        if (uiSetting != null) uiSetting.Show();
    }

    public void HideSetting()
    {
        if (uiSetting != null) uiSetting.Hide();
    }
}