using UnityEngine;

namespace Nghia
{
    public class WalkAudio : MonoBehaviour
    {
        public enum FootstepMode
        {
            Automatic,         // Tự động nhận diện bước chân theo Muck
            AnimationEventOnly // Chỉ phát khi nhận Animation Event
        }

        [Header("Mode")]
        [Tooltip("Automatic: phát theo nhịp bước chân chuẩn Muck. AnimationEventOnly: gọi qua Animation Event.")]
        [SerializeField] private FootstepMode mode = FootstepMode.Automatic;

        [Header("References")]
        [SerializeField] private PlayerMovementSubject movementSubject;
        [SerializeField] private AudioSource audioSource;

        [Header("Audio Clips")]
        [Tooltip("Danh sách clip bước chân (Ví dụ: foot_grass, foot_wood, MediumStep...)")]
        [SerializeField] private AudioClip[] walkFootstepClips;

        [Header("Audio Settings (Muck style)")]
        [Range(0f, 1f)]
        [SerializeField] private float volume = 0.65f;

        [Tooltip("Muck biến thiên pitch ngẫu nhiên nhẹ để mỗi bước chân có cảm giác sống động, tự nhiên")]
        [Range(0.85f, 1.15f)]
        [SerializeField] private float minPitch = 0.92f;
        [Range(0.85f, 1.15f)]
        [SerializeField] private float maxPitch = 1.08f;

        [Tooltip("Tránh 2 clip giống hệt nhau phát liên tiếp")]
        [SerializeField] private bool avoidRepeatingClip = true;

        private int lastClipIndex = -1;

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

            movementSubject.OnPlayerStep += HandlePlayerStep;
        }

        private void OnDisable()
        {
            if (movementSubject == null)
                return;

            movementSubject.OnPlayerStep -= HandlePlayerStep;
        }

        private void HandlePlayerStep()
        {
            if (mode == FootstepMode.Automatic)
            {
                PlayFootstep();
            }
        }

        /// <summary>
        /// Có thể gọi từ Animation Event trên clip nếu muốn (ví dụ hàm LeftStep / RightStep).
        /// </summary>
        public void PlayFootstepEvent()
        {
            PlayFootstep();
        }

        public void PlayFootstep()
        {
            if (audioSource == null)
                return;

            if (walkFootstepClips == null || walkFootstepClips.Length == 0)
                return;

            int clipIndex;
            if (walkFootstepClips.Length == 1)
            {
                clipIndex = 0;
            }
            else if (avoidRepeatingClip)
            {
                do
                {
                    clipIndex = Random.Range(0, walkFootstepClips.Length);
                } while (clipIndex == lastClipIndex);
            }
            else
            {
                clipIndex = Random.Range(0, walkFootstepClips.Length);
            }

            lastClipIndex = clipIndex;
            AudioClip clip = walkFootstepClips[clipIndex];
            if (clip == null) return;

            // Pitch random theo phong cách Muck
            audioSource.pitch = Random.Range(minPitch, maxPitch);
            audioSource.PlayOneShot(clip, volume);
        }
    }
}
