using UnityEngine;

namespace Nghia
{
    public class WalkAudio : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerMovementSubject movementSubject;
        [SerializeField] private AudioSource audioSource;

        [Header("Walk Footstep")]
        [SerializeField] private AudioClip[] walkFootstepClips;

        [Header("Settings")]
        [SerializeField] private float stepInterval = 0.5f;

        [Range(0f, 1f)]
        [SerializeField] private float volume = 0.7f;

        private float stepTimer;
        private bool isWalking;

        private void Awake()
        {
            if (movementSubject == null)
                movementSubject = GetComponent<PlayerMovementSubject>();

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
            if (movementSubject == null)
                return;

            movementSubject.OnPlayerWalk += OnPlayerWalk;
            movementSubject.OnPlayerStop += OnPlayerStop;
        }

        private void OnDisable()
        {
            if (movementSubject == null)
                return;

            movementSubject.OnPlayerWalk -= OnPlayerWalk;
            movementSubject.OnPlayerStop -= OnPlayerStop;
        }

        private void Update()
        {
            if (!isWalking)
                return;

            stepTimer += Time.deltaTime;

            if (stepTimer >= stepInterval)
            {
                PlayFootstep();
                stepTimer = 0f;
            }
        }

        private void OnPlayerWalk()
        {
            isWalking = true;
        }

        private void OnPlayerStop()
        {
            isWalking = false;
            stepTimer = 0f;
        }

        private void PlayFootstep()
        {
            if (audioSource == null)
                return;

            if (walkFootstepClips == null ||
                walkFootstepClips.Length == 0)
                return;

            AudioClip clip =
                walkFootstepClips[
                    Random.Range(0, walkFootstepClips.Length)
                ];

            audioSource.PlayOneShot(clip, volume);
        }
    }
}