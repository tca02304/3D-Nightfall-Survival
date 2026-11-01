using UnityEngine;

[CreateAssetMenu(fileName = "TreeData", menuName = "Scriptable Objects/TreeData")]
public class TreeData : ScriptableObject
{
    [Header("Thông tin chung")]
    public string treeName;          // Tên loại cây (Ví dụ: Birch, Oak)
    public float maxHealth = 100f;   // Lượng máu tối đa

    [Header("Cấu hình Rơi Vật Phẩm (Loot Drop)")]
    public GameObject woodPrefab;    // Prefab vật phẩm rơi ra (Gỗ, quả...)
    public int minWoodCount = 1;     // Số lượng tối thiểu
    public int maxWoodCount = 3;     // Số lượng tối đa

    [Header("Hiệu ứng rung khi bị chém")]
    public float shakeDuration = 0.15f;
    public float shakeMagnitude = 0.1f;
}