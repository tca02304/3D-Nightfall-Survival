using UnityEngine;

[CreateAssetMenu(fileName = "NewBuildableItem", menuName = "Building/Buildable Item")]
public class BuildableItemData : ScriptableObject
{
    public string itemName = "Vật phẩm xây dựng";

    [Tooltip("Model hiển thị trên tay Player khi đang cầm (có thể dùng bản thu nhỏ của công trình)")]
    public GameObject handPrefab;

    [Tooltip("Prefab công trình thật sẽ được đặt xuống đất")]
    public GameObject buildPrefab;

    [Tooltip("Bật nếu đặt xong sẽ cất/mất vật phẩm trên tay. Tắt nếu muốn đặt liên tiếp nhiều cái.")]
    public bool consumeOnPlace = true;
}