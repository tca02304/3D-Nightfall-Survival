
using UnityEngine;

namespace Nghia
{
    [RequireComponent(typeof(AudioSource))]
    public class EnemyAttackAudioObserver : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnemyAttackAudioSubject subject;
        [SerializeField] private AudioSource audioSource;

        [Header("Attack Sounds")]
        [SerializeField] private AudioClip[] attackClips;

        [Range(0f, 1f)]
        [SerializeField] private float volume = 0.8f;

        private void Awake()
        {
            if (subject == null)
                subject = GetComponent<EnemyAttackAudioSubject>();

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 1f;
        }

        private void OnEnable()
        {
            if (subject != null)
                subject.OnAttack += PlayAttackSound;
        }

        private void OnDisable()
        {
            if (subject != null)
                subject.OnAttack -= PlayAttackSound;
        }

        private void PlayAttackSound()
        {
            if (audioSource == null) return;
            if (attackClips == null || attackClips.Length == 0) return;

            AudioClip clip = attackClips[
                Random.Range(0, attackClips.Length)
            ];

            if (clip != null)
                audioSource.PlayOneShot(clip, volume);
        }
    }
}
