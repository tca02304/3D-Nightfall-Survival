using UnityEngine;
using DucThanh;

namespace Nghia
{
    public class EnemyDamageAudio : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnemyDamageDealer damageDealer;
        [SerializeField] private AudioSource audioSource;

        [Header("Player Damage Sound")]
        [SerializeField] private AudioClip[] damageClips;

        [Header("Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float volume = 1f;

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
            if (damageDealer == null)
                return;

            damageDealer.OnDamageDealt += OnDamageDealt;
        }

        private void OnDisable()
        {
            if (damageDealer == null)
                return;

            damageDealer.OnDamageDealt -= OnDamageDealt;
        }

        private void OnDamageDealt(float damage)
        {
            PlayDamageSound();

            Debug.Log($"Audio: Enemy hit Player - Damage {damage:F1}");
        }

        private void PlayDamageSound()
        {
            if (audioSource == null)
            {
                Debug.LogWarning("EnemyDamageAudio: Chưa có AudioSource!");
                return;
            }

            if (damageClips == null || damageClips.Length == 0)
            {
                Debug.LogWarning("EnemyDamageAudio: Chưa gán Damage Clips!");
                return;
            }

            AudioClip clip =
                damageClips[Random.Range(0, damageClips.Length)];

            audioSource.PlayOneShot(clip, volume);
        }
    }
}