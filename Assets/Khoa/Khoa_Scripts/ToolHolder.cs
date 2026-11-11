using UnityEngine;

/// Gắn trên WeaponHolder (con của Hand.R_end). Bật/tắt công cụ đang cầm ở tay phải.
public class ToolHolder : MonoBehaviour
{
    public enum Tool { None = 0, Sword = 1, Pickaxe = 2, Axe = 3 }

    public GameObject sword;
    public GameObject pickaxe;
    public GameObject axe;
    public Tool startingTool = Tool.None;

    public Tool Current { get; private set; }

    void Start() => Equip(startingTool);

    public void Equip(Tool tool)
    {
        Current = tool;
        if (sword)   sword.SetActive(tool == Tool.Sword);
        if (pickaxe) pickaxe.SetActive(tool == Tool.Pickaxe);
        if (axe)     axe.SetActive(tool == Tool.Axe);
    }

    // Gọi từ UI/hotbar bằng int: 0 = tay không, 1 = kiếm, 2 = cuốc, 3 = rìu
    public void Equip(int index) => Equip((Tool)index);

    /// Thay model của một slot, ví dụ khi nhặt item: holder.SetModel(Tool.Axe, axePrefab);
    public void SetModel(Tool tool, GameObject prefab)
    {
        Transform slot = tool == Tool.Sword ? sword.transform : tool == Tool.Pickaxe ? pickaxe.transform : axe.transform;
        foreach (Transform c in slot) Destroy(c.gameObject);
        Instantiate(prefab, slot);
    }
}
