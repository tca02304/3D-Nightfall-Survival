
using UnityEngine;

namespace Nghia
{
    public class EnemyWeaponHitAudio : MonoBehaviour
    {
        [Header("Audio Settings")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip[] hitClips;

        [Range(0f, 1f)]
        [SerializeField] private float volume = 1f;

        [Header("Delay Settings")]
        public float delay = 0.2f;

        [Header("Target Settings")]
        [SerializeField] private string playerTag = "Player";

        private bool isTouchingPlayer;
        private bool canPlay = true;
        private float timer;

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                audioSource.loop = false;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(playerTag))
                isTouchingPlayer = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(playerTag))
                isTouchingPlayer = false;
        }

        private void Update()
        {
            if (!isTouchingPlayer)
                return;

            if (!canPlay)
            {
                timer -= Time.deltaTime;

                if (timer <= 0f)
                    canPlay = true;

                return;
            }

            PlayHitSound();

            canPlay = false;
            timer = delay;
        }

        private void PlayHitSound()
        {
            if (audioSource == null ||
                hitClips == null ||
                hitClips.Length == 0)
                return;

            AudioClip clip =
                hitClips[Random.Range(0, hitClips.Length)];

            if (clip != null)
                audioSource.PlayOneShot(clip, volume);
        }
    }
}
