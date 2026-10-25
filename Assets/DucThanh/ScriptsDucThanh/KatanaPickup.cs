using System.Collections;
using UnityEngine;

namespace DucThanh
{
    public class KatanaPickup : MonoBehaviour
    {
        [Header("Hand Setup (Đúng vị trí & góc độ đã setup)")]
        [Tooltip("Xương tay của Player sẽ cầm Katana. Nếu để trống sẽ tự động tìm 'Weapon (1)' trên Player")]
        [SerializeField] private Transform targetHand;

        [Tooltip("Vị trí cục bộ trên tay")]
        [SerializeField] private Vector3 equippedLocalPosition = new Vector3(-0.03f, 0f, -0.24f);

        [Tooltip("Góc xoay cục bộ trên tay (Quaternion chính xác)")]
        [SerializeField] private Quaternion equippedLocalRotation = new Quaternion(-0.6170957f, -0.072604194f, 0.7811037f, -0.061632864f);

        [Tooltip("Góc Euler cục bộ tương ứng")]
        [SerializeField] private Vector3 equippedLocalEulerAngles = new Vector3(-190.923f, 103.418f, 0.39f);

        [Tooltip("Tỉ lệ cục bộ trên tay")]
        [SerializeField] private Vector3 equippedLocalScale = new Vector3(2.6312f, 0.52624f, 2.6312f);

        [Header("Ground & Pickup Settings")]
        [Tooltip("Tự động đặt Katana xuống đất khi bắt đầu nếu Katana đang gắn trên tay")]
        [SerializeField] private bool placeOnGroundAtStart = true;

        [Tooltip("Khoảng cách tự động nhặt khi Player chạm/đến gần")]
        [SerializeField] private float pickupDistance = 1.6f;

        [Tooltip("Xoay nhẹ Katana khi đang ở dưới đất để dễ nhận diện")]
        [SerializeField] private bool rotateOnGround = true;

        [Tooltip("Tốc độ xoay dưới đất")]
        [SerializeField] private float rotationSpeed = 60f;

        private Collider pickupCollider;
        private bool isEquipped = false;
        private PlayerController cachedPlayer;

        public bool IsEquipped => isEquipped;

        private void Awake()
        {
            // Nếu ban đầu Katana đang là con của một Transform, lưu lại làm targetHand
            if (transform.parent != null)
            {
                targetHand = transform.parent;
            }

            SetupCollider();
        }

        private void Start()
        {
            FindPlayerAndHand();

            if (placeOnGroundAtStart && transform.parent != null)
            {
                PlaceOnGround();
            }
        }

        private void Update()
        {
            if (isEquipped) return;

            // Xoay nhẹ khi ở dưới đất
            if (rotateOnGround)
            {
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            }

            // Kiểm tra khoảng cách tới Player để nhặt khi chạm/đến gần
            CheckDistancePickup();
        }

        private void SetupCollider()
        {
            pickupCollider = GetComponent<Collider>();
            if (pickupCollider == null)
            {
                SphereCollider sc = gameObject.AddComponent<SphereCollider>();
                sc.isTrigger = true;
                sc.radius = 1.0f;
                pickupCollider = sc;
            }
            else
            {
                pickupCollider.isTrigger = true;
            }
        }

        private void FindPlayerAndHand()
        {
            if (cachedPlayer == null)
            {
                cachedPlayer = FindAnyObjectByType<PlayerController>();
            }

            if (targetHand == null && cachedPlayer != null)
            {
                targetHand = FindHandBone(cachedPlayer.transform);
            }
        }

        private Transform FindHandBone(Transform root)
        {
            // Tìm Transform xương tay Weapon (1) trên player
            Transform[] allChildren = root.GetComponentsInChildren<Transform>(true);
            foreach (var child in allChildren)
            {
                if (child.name == "Weapon (1)")
                {
                    return child;
                }
            }

            // Fallback tìm Transform có tên chứa Weapon hoặc Hand
            foreach (var child in allChildren)
            {
                if (child.name.Contains("Weapon") && child != transform)
                {
                    return child;
                }
            }

            return null;
        }

        /// <summary>
        /// Đặt Katana xuống mặt đất phía trước Player
        /// </summary>
        public void PlaceOnGround()
        {
            isEquipped = false;
            transform.SetParent(null);

            Vector3 spawnPos = Vector3.zero;
            if (cachedPlayer != null)
            {
                Transform playerT = cachedPlayer.transform;
                spawnPos = playerT.position + playerT.forward * 1.8f + playerT.right * 0.6f;
            }
            else
            {
                spawnPos = transform.position;
            }

            // Bắn Raycast xuống mặt đất để Katana nằm sát đất
            if (Physics.Raycast(spawnPos + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 10f))
            {
                spawnPos.y = hit.point.y + 0.15f;
            }
            else
            {
                spawnPos.y = Mathf.Max(0.5f, spawnPos.y);
            }

            transform.position = spawnPos;
            // Đặt nằm ngang trên mặt đất
            transform.rotation = Quaternion.Euler(0f, 45f, 90f);

            if (pickupCollider != null)
            {
                pickupCollider.enabled = true;
                pickupCollider.isTrigger = true;
            }

            if (cachedPlayer != null)
            {
                cachedPlayer.SetHasWeapon(false);
            }

            Debug.Log($"[Katana] Katana đã được đặt xuống đất tại vị trí: {transform.position}");
        }

        /// <summary>
        /// Khi Player chạm vào Trigger của Katana
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (isEquipped) return;

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                EquipToHand(player);
            }
        }

        private void CheckDistancePickup()
        {
            if (cachedPlayer == null)
            {
                FindPlayerAndHand();
                if (cachedPlayer == null) return;
            }

            float dist = Vector3.Distance(transform.position, cachedPlayer.transform.position);
            if (dist <= pickupDistance)
            {
                EquipToHand(cachedPlayer);
            }
        }

        /// <summary>
        /// Nhảy lên tay của Player đúng vị trí, góc độ và tỉ lệ đã setup sẵn
        /// </summary>
        public void EquipToHand(PlayerController player = null)
        {
            if (isEquipped) return;

            if (player != null)
            {
                cachedPlayer = player;
            }

            if (targetHand == null)
            {
                FindPlayerAndHand();
            }

            if (targetHand == null)
            {
                Debug.LogWarning("[Katana] Không tìm thấy xương tay 'Weapon (1)' để gắn Katana!");
                return;
            }

            isEquipped = true;

            // Gắn vào xương tay của Player
            transform.SetParent(targetHand);

            // Đặt lại đúng vị trí, góc xoay và scale đã setup sẵn trong Unity
            transform.localPosition = equippedLocalPosition;
            transform.localRotation = equippedLocalRotation;
            transform.localScale = equippedLocalScale;

            // Tắt trigger nhặt
            if (pickupCollider != null)
            {
                pickupCollider.enabled = false;
            }

            if (cachedPlayer != null)
            {
                cachedPlayer.SetHasWeapon(true);
            }

            Debug.Log("<color=green><b>[Katana] Player đã chạm vào Katana! Katana đã nhảy lên tay đúng vị trí & góc độ đã setup!</b></color>");
        }

        #region Context Menu Helpers for Unity Editor
        [ContextMenu("Capture Current Transform As Equipped")]
        private void CaptureCurrentTransform()
        {
            equippedLocalPosition = transform.localPosition;
            equippedLocalRotation = transform.localRotation;
            equippedLocalEulerAngles = transform.localEulerAngles;
            equippedLocalScale = transform.localScale;
            Debug.Log($"[Katana] Đã lưu thông số Transform: Pos={equippedLocalPosition}, Euler={equippedLocalEulerAngles}, Scale={equippedLocalScale}");
        }

        [ContextMenu("Equip To Hand (Editor Test)")]
        private void TestEquip()
        {
            FindPlayerAndHand();
            EquipToHand(cachedPlayer);
        }

        [ContextMenu("Place On Ground (Editor Test)")]
        private void TestPlaceOnGround()
        {
            FindPlayerAndHand();
            PlaceOnGround();
        }
        #endregion
    }
}
