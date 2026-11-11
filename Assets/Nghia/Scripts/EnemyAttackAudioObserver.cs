using UnityEngine;

namespace Nghia
{
    /// <summary>
    /// Phát âm thanh khi quái vật (Sói, Dinosaur/LilDave, Goblin, v.v.) tấn công hoặc cắn trúng người chơi.
    /// Thiết kế chuẩn Muck: Âm thanh 3D spatial sound chân thực + Random Pitch sống động.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class EnemyAttackAudioObserver : MonoBehaviour
    {
        [Header("References (Tự động tìm)")]
        [SerializeField] private EnemyAttackAudioSubject subject;
        [SerializeField] private AudioSource audioSource;

        [Header("Attack Sounds (Muck Style)")]
        [Tooltip("Các âm thanh tấn công/cắn: WolfBark cho Sói, lil' dave bite cho Dinosaur...")]
        [SerializeField] private AudioClip[] attackClips;

        [Header("Audio Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float volume = 0.85f;

        [Tooltip("Biến thiên cao độ pitch ngẫu nhiên (đặc trưng của game Muck giúp âm thanh đòn đánh sống động)")]
        [Range(0.85f, 1.15f)]
        [SerializeField] private float minPitch = 0.92f;
        [Range(0.85f, 1.15f)]
        [SerializeField] private float maxPitch = 1.08f;

        [Tooltip("Tránh phát trùng 1 clip 2 lần liên tiếp")]
        [SerializeField] private bool avoidRepeatingClip = true;

        private int lastPlayedIndex = -1;

        private void Awake()
        {
            if (subject == null)
            {
                subject = GetComponent<EnemyAttackAudioSubject>();
                if (subject == null)
                    subject = gameObject.AddComponent<EnemyAttackAudioSubject>();
            }

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                audioSource.loop = false;
                audioSource.spatialBlend = 1f; // 3D sound chuẩn Muck (nghe rõ hướng quái tấn công)
                audioSource.rolloffMode = AudioRolloffMode.Linear;
                audioSource.minDistance = 2f;
                audioSource.maxDistance = 35f;
            }
        }

        private void OnEnable()
        {
            if (subject != null)
            {
                subject.OnAttack += PlayAttackSound;
            }
        }

        private void OnDisable()
        {
            if (subject != null)
            {
                subject.OnAttack -= PlayAttackSound;
            }
        }

        public void PlayAttackSound()
        {
            if (audioSource == null) return;
            if (attackClips == null || attackClips.Length == 0) return;

            int index;
            if (attackClips.Length == 1)
            {
                index = 0;
            }
            else if (avoidRepeatingClip)
            {
                do
                {
                    index = Random.Range(0, attackClips.Length);
                } while (index == lastPlayedIndex);
            }
            else
            {
                index = Random.Range(0, attackClips.Length);
            }

            lastPlayedIndex = index;
            AudioClip clip = attackClips[index];
            if (clip == null) return;

            // Pitch random nhẹ theo chuẩn Dani/Muck
            audioSource.pitch = Random.Range(minPitch, maxPitch);
            audioSource.PlayOneShot(clip, volume);
        }
    }
}
