using UnityEngine;

namespace DucThanh
{
    public enum ItemType
    {
        Weapon,
        Tool,
        Consumable,
        Resource
    }

    /// <summary>
    /// ScriptableObject chứa cấu hình cho một vật phẩm (Item / Weapon).
    /// Tương tự EnemyTypeConfigSO, cho phép tạo và tùy biến thông số item trực tiếp trong Editor.
    /// </summary>
    [CreateAssetMenu(fileName = "NewItemConfig", menuName = "DucThanh/Inventory/Item Config")]
    public class ItemConfigSO : ScriptableObject
    {
        [Header("Item Basic Info")]
        [Tooltip("Mã định danh duy nhất của item")]
        [SerializeField] private string itemId = "item_katana";

        [Tooltip("Tên hiển thị của item")]
        [SerializeField] private string itemName = "Katana";

        [Tooltip("Mô tả thông tin item")]
        [TextArea(2, 4)]
        [SerializeField] private string description = "Thanh kiếm sắc bén dùng để tự vệ trước quái vật.";

        [Tooltip("Biểu tượng hiển thị trên UI")]
        [SerializeField] private Sprite icon;

        [Tooltip("Loại vật phẩm")]
        [SerializeField] private ItemType itemType = ItemType.Weapon;

        [Header("Visual Prefabs")]
        [Tooltip("Prefab hiển thị trong tay player khi cầm")]
        [SerializeField] private GameObject heldPrefab;

        [Tooltip("Prefab hiển thị khi rơi dưới đất (nếu để trống sẽ dùng heldPrefab)")]
        [SerializeField] private GameObject dropPrefab;

        [Header("Hand Placement Settings (Vị trí cầm trên tay Player)")]
        [Tooltip("Tên Transform xương tay gắn vào (Mặc định 'Weapon (1)' hoặc 'RightHand')")]
        [SerializeField] private string handBoneName = "Weapon (1)";

        [Tooltip("Vị trí cục bộ trên tay (Local Position)")]
        [SerializeField] private Vector3 handLocalPosition = new Vector3(-0.03f, 0f, -0.24f);

        [Tooltip("Góc xoay cục bộ trên tay (Local Euler Angles)")]
        [SerializeField] private Vector3 handLocalEulerAngles = new Vector3(-190.923f, 103.418f, 0.39f);

        [Tooltip("Tỉ lệ cục bộ trên tay (Local Scale)")]
        [SerializeField] private Vector3 handLocalScale = new Vector3(2.6312f, 0.52624f, 2.6312f);

        [Header("Combat & Stats")]
        [Tooltip("Sát thương gây ra nếu là vũ khí")]
        [SerializeField] private float attackDamage = 25f;

        [Tooltip("Tầm đánh xa của vũ khí")]
        [SerializeField] private float attackRange = 2.2f;

        [Tooltip("Bán kính vùng chém trúng")]
        [SerializeField] private float attackRadius = 1.2f;

        [Header("Stack & Max Limit")]
        [Tooltip("Số lượng tối đa có thể xếp chồng trong 1 ô túi đồ (Vũ khí thường là 1)")]
        [SerializeField] private int maxStack = 1;

        // Getters & Setters
        public string ItemId => itemId;
        public string ItemName => itemName;
        public string Description => description;
        public Sprite Icon => icon;
        public ItemType Type => itemType;
        public GameObject HeldPrefab => heldPrefab;
        public GameObject DropPrefab => dropPrefab != null ? dropPrefab : heldPrefab;
        public string HandBoneName => handBoneName;
        public Vector3 HandLocalPosition { get => handLocalPosition; set => handLocalPosition = value; }
        public Vector3 HandLocalEulerAngles { get => handLocalEulerAngles; set => handLocalEulerAngles = value; }
        public Vector3 HandLocalScale { get => handLocalScale; set => handLocalScale = value; }
        public float AttackDamage => attackDamage;
        public float AttackRange => attackRange;
        public float AttackRadius => attackRadius;
        public int MaxStack => maxStack;

        public void SetHandOffset(Vector3 pos, Vector3 rotEuler, Vector3 scale)
        {
            handLocalPosition = pos;
            handLocalEulerAngles = rotEuler;
            handLocalScale = scale;
        }
    }
}
