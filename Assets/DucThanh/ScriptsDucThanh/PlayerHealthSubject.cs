using System;
using System.Collections.Generic;
using UnityEngine;

namespace DucThanh
{
    /// <summary>
    /// Giao diện Observer dành cho các bên khác (UI, VFX, Sound, Animation,...) 
    /// muốn đăng ký lắng nghe sự kiện thay đổi máu của Player.
    /// </summary>
    public interface IPlayerHealthObserver
    {
        void OnPlayerHealthChanged(float currentHealth, float maxHealth, float healthPercent);
    }

    /// <summary>
    /// Subject (Chủ thể) trong Observer Pattern đại diện cho Player.
    /// Quản lý danh sách các Observer và tự động phát thông báo khi máu Player thay đổi.
    /// Script độc lập, chỉ cần gắn lên GameObject của Player.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerHealthSubject : MonoBehaviour
    {
        [Header("Player Target")]
        [Tooltip("Tham chiếu tới PlayerStats để đọc máu (Tự động tìm trên chính Player nếu để trống)")]
        [SerializeField] private PlayerStats playerStats;

        // Danh sách các Observer đã đăng ký
        private readonly List<IPlayerHealthObserver> observers = new List<IPlayerHealthObserver>();

        // Event C# dự phòng cho người khác thích dùng Event thay vì Interface
        public event Action<float, float, float> OnHealthChanged;

        private float lastKnownHealth = -1f;
        private float lastKnownMaxHealth = -1f;

        public float CurrentHealth => playerStats != null ? playerStats.CurrentHealth : 0f;
        public float MaxHealth => playerStats != null ? playerStats.MaxHealth : 100f;
        public float HealthPercent => playerStats != null ? playerStats.HealthPercent : 0f;

        private void Awake()
        {
            if (playerStats == null)
            {
                playerStats = GetComponent<PlayerStats>();
            }

            if (playerStats != null)
            {
                lastKnownHealth = playerStats.CurrentHealth;
                lastKnownMaxHealth = playerStats.MaxHealth;
            }
        }

        private void Update()
        {
            if (playerStats == null)
            {
                playerStats = GetComponent<PlayerStats>();
                if (playerStats == null) return;
            }

            // Tự động phát hiện khi máu của Player có sự thay đổi và thông báo cho toàn bộ Observer
            if (!Mathf.Approximately(lastKnownHealth, playerStats.CurrentHealth) ||
                !Mathf.Approximately(lastKnownMaxHealth, playerStats.MaxHealth))
            {
                lastKnownHealth = playerStats.CurrentHealth;
                lastKnownMaxHealth = playerStats.MaxHealth;
                NotifyObservers();
            }
        }

        #region Observer Management (Subject Methods)
        /// <summary>
        /// Người khác gọi hàm này để đăng ký nhận thông báo máu Player
        /// </summary>
        public void Attach(IPlayerHealthObserver observer)
        {
            if (observer != null && !observers.Contains(observer))
            {
                observers.Add(observer);
                // Bắn thông báo ngay lần đầu đăng ký để bên Observer đồng bộ dữ liệu hiện tại
                observer.OnPlayerHealthChanged(CurrentHealth, MaxHealth, HealthPercent);
            }
        }

        /// <summary>
        /// Hủy đăng ký nhận thông báo
        /// </summary>
        public void Detach(IPlayerHealthObserver observer)
        {
            if (observer != null)
            {
                observers.Remove(observer);
            }
        }

        /// <summary>
        /// Phát thông báo tới toàn bộ Observers trong danh sách
        /// </summary>
        public void NotifyObservers()
        {
            float cur = CurrentHealth;
            float max = MaxHealth;
            float pct = HealthPercent;

            for (int i = observers.Count - 1; i >= 0; i--)
            {
                if (observers[i] != null)
                {
                    observers[i].OnPlayerHealthChanged(cur, max, pct);
                }
                else
                {
                    observers.RemoveAt(i);
                }
            }

            OnHealthChanged?.Invoke(cur, max, pct);
        }
        #endregion
    }
}
