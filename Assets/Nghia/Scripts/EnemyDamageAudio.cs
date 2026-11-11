using UnityEngine;
using DucThanh;

namespace Nghia
{
    /// <summary>
    /// Phát âm thanh khi Enemy trúng/va chạm gây sát thương lên Player.
    /// Hỗ trợ cả EnemyDamageDealer và va chạm trực tiếp (OnTrigger/OnCollision).
    /// </summary>
    public class EnemyDamageAudio : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnemyDamageDealer damageDealer;
        [SerializeField] private AudioSource audioSource;

        [Header("Player Damage Sound (Muck style)")]
        [Tooltip("Clip âm thanh trúng đòn (Ví dụ: playerHit.ogg, Hit_Normal.ogg, Hit_crit.ogg)")]
        [SerializeField] private AudioClip[] damageClips;

        [Header("Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float volume = 0.85f;

        [Range(0.85f, 1.15f)]
        [SerializeField] private float minPitch = 0.92f;
        [Range(0.85f, 1.15f)]
        [SerializeField] private float maxPitch = 1.08f;

        [SerializeField] private float hitCooldown = 0.4f;
        private float lastHitTime = -10f;

        private void Awake()
        {
            if (damageDealer == null)
                damageDealer = GetComponent<EnemyDamageDealer>();

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                audioSource.loop = false;
            }
        }

        private void OnEnable()
        {
            if (damageDealer != null)
                damageDealer.OnDamageDealt += OnDamageDealt;
        }

        private void OnDisable()
        {
            if (damageDealer != null)
                damageDealer.OnDamageDealt -= OnDamageDealt;
        }

        private void OnDamageDealt(float damage)
        {
            PlayDamageSound();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || other.name.ToLower().Contains("player"))
            {
                if (damageDealer == null) // Nếu quái không có damageDealer mà chỉ dùng trigger
                {
                    PlayDamageSound();
                }
            }
        }

        public void PlayDamageSound()
        {
            if (Time.time - lastHitTime < hitCooldown) return;
            lastHitTime = Time.time;

            if (audioSource == null || damageClips == null || damageClips.Length == 0)
                return;

            AudioClip clip = damageClips[Random.Range(0, damageClips.Length)];
            if (clip == null) return;

            audioSource.pitch = Random.Range(minPitch, maxPitch);
            audioSource.PlayOneShot(clip, volume);
        }
    }
}
