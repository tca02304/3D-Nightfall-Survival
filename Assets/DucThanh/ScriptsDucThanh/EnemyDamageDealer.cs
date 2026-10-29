using System;
using UnityEngine;

namespace DucThanh
{
    /// <summary>
    /// Component độc lập chuyên tính toán và gây sát thương từ Enemy lên Player.
    /// - Sát thương cố định chuẩn xác theo ngày (đã bỏ tính năng random dame dao động).
    /// - Tự động đồng bộ với EnemyStats nếu có.
    /// - Khi Player hết máu (CurrentHealth <= 0 hoặc IsDead), Enemy lập tức ngừng đánh.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyDamageDealer : MonoBehaviour
    {
        [Header("Damage Settings (Chỉ số sát thương)")]
        [Tooltip("Lượng sát thương gây lên Player mỗi đòn đánh (sát thương chuẩn theo ngày)")]
        [SerializeField] private float baseDamage = 15f;

        [Header("Attack Timing (Nhịp đánh)")]
        [Tooltip("Thời gian giãn cách tối thiểu giữa 2 lần gây sát thương (giây)")]
        [SerializeField] private float attackInterval = 1.0f;

        [Tooltip("Độ trễ trước khi trừ máu để khớp chuyển động đánh của quái")]
        [SerializeField] private float hitDelay = 0.15f;

        [Header("Range & Target (Phạm vi & Mục tiêu)")]
        [Tooltip("Khoảng cách tấn công trúng Player (m)")]
        [SerializeField] private float maxAttackRange = 2.5f;

        [Tooltip("Tag của Player để nhận diện")]
        [SerializeField] private string playerTag = "Player";

        [Header("Auto Attack (Tự động gây sát thương khi chạm/trong tầm)")]
        [Tooltip("Tự động gây sát thương định kỳ khi Player ở trong tầm mà không cần sửa script AI")]
        [SerializeField] private bool autoDealDamageInRange = true;

        [Header("Optional Armor (Tùy chọn giáp)")]
        [Tooltip("Bật tính năng giảm sát thương qua giáp")]
        [SerializeField] private bool useArmorMitigation = false;
        [SerializeField] private float targetArmor = 0f;

        private Transform playerTransform;
        private PlayerStats playerStats;
        private EnemyStats enemyStats;
        private float nextDamageAllowedTime = 0f;
        private bool isPlayerInContact = false;

        public event Action<float> OnDamageDealt;

        public float BaseDamage => baseDamage;
        public float AttackInterval => attackInterval;
        public float MaxAttackRange => maxAttackRange;

        /// <summary>
        /// Kiểm tra nhanh Player còn sống và có máu > 0 không
        /// </summary>
        public bool IsPlayerAlive => playerStats != null && !playerStats.IsDead && playerStats.CurrentHealth > 0f;

        private void Awake()
        {
            enemyStats = GetComponent<EnemyStats>();
            if (enemyStats != null)
            {
                baseDamage = enemyStats.CurrentDamage;
            }
        }

        private void Start()
        {
            FindPlayerTarget();
        }

        private void Update()
        {
            if (!autoDealDamageInRange) return;

            if (playerTransform == null || playerStats == null)
            {
                FindPlayerTarget();
                if (playerTransform == null || playerStats == null) return;
            }

            // Nếu Player đã chết thì dừng đánh ngay
            if (!IsPlayerAlive) return;

            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            bool inRange = isPlayerInContact || (distanceToPlayer <= maxAttackRange);

            // Chỉ cần trúng Player (khoảng cách hoặc chạm Collider)
            if (inRange && Time.time >= nextDamageAllowedTime)
            {
                if (hitDelay > 0f)
                {
                    StartCoroutine(DelayedDamageRoutine(hitDelay));
                }
                else
                {
                    ExecuteDamage();
                }

                nextDamageAllowedTime = Time.time + attackInterval;
            }
        }

        private System.Collections.IEnumerator DelayedDamageRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (playerTransform != null && IsPlayerAlive)
            {
                float dist = Vector3.Distance(transform.position, playerTransform.position);
                if (dist <= maxAttackRange * 1.35f || isPlayerInContact)
                {
                    ExecuteDamage();
                }
            }
        }

        /// <summary>
        /// Cập nhật sát thương gốc (thường được gọi bởi EnemyStats khi qua ngày mới)
        /// </summary>
        public void SetBaseDamage(float newDamage)
        {
            baseDamage = Mathf.Max(1f, newDamage);
        }

        /// <summary>
        /// Hàm có thể gọi từ Animation Event hoặc trigger của quái
        /// </summary>
        public void DealDamageToPlayer()
        {
            if (!IsPlayerAlive) return;
            if (Time.time < nextDamageAllowedTime) return;

            ExecuteDamage();
            nextDamageAllowedTime = Time.time + attackInterval;
        }

        /// <summary>
        /// Thực hiện trừ máu Player với sát thương chuẩn xác (không random)
        /// </summary>
        private void ExecuteDamage()
        {
            if (playerStats == null || !IsPlayerAlive) return;

            float calculatedDamage = CalculateDamage(baseDamage);
            playerStats.ModifyHealth(-calculatedDamage);

            OnDamageDealt?.Invoke(calculatedDamage);
            Debug.Log($"<color=orange>[EnemyDamageDealer] {gameObject.name} đã gây <b>{calculatedDamage:F1}</b> sát thương lên Player! (Máu Player còn: {playerStats.CurrentHealth:F0}/{playerStats.MaxHealth:F0})</color>");
        }

        /// <summary>
        /// Tính toán sát thương cố định chuẩn xác theo ngày (không còn yếu tố ngẫu nhiên dao động)
        /// </summary>
        public float CalculateDamage(float rawDamage)
        {
            if (useArmorMitigation && targetArmor > 0f)
            {
                float mitigation = 100f / (100f + targetArmor);
                rawDamage *= mitigation;
            }

            return Mathf.Max(1f, Mathf.Round(rawDamage * 10f) / 10f);
        }

        private void FindPlayerTarget()
        {
            GameObject pObj = GameObject.FindGameObjectWithTag(playerTag);
            if (pObj == null)
            {
                pObj = GameObject.Find("Player") ?? GameObject.Find("PlayerCube");
            }

            if (pObj != null)
            {
                playerTransform = pObj.transform;
                playerStats = pObj.GetComponentInParent<PlayerStats>() ?? pObj.GetComponentInChildren<PlayerStats>();
            }
        }

        #region Trigger & Collision Handlers
        private void OnTriggerEnter(Collider other)
        {
            if (IsPlayer(other)) isPlayerInContact = true;
        }

        private void OnTriggerStay(Collider other)
        {
            if (IsPlayer(other) && IsPlayerAlive) isPlayerInContact = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (IsPlayer(other)) isPlayerInContact = false;
        }

        private void OnCollisionEnter(Collision col)
        {
            if (IsPlayer(col.collider)) isPlayerInContact = true;
        }

        private void OnCollisionStay(Collision col)
        {
            if (IsPlayer(col.collider) && IsPlayerAlive) isPlayerInContact = true;
        }

        private void OnCollisionExit(Collision col)
        {
            if (IsPlayer(col.collider)) isPlayerInContact = false;
        }

        private bool IsPlayer(Collider col)
        {
            if (col == null) return false;
            if (col.CompareTag(playerTag)) return true;
            if (playerTransform != null && (col.transform == playerTransform || col.transform.IsChildOf(playerTransform))) return true;
            return false;
        }
        #endregion

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsPlayerAlive ? Color.red : Color.gray;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 1f, maxAttackRange);
        }
    }
}
