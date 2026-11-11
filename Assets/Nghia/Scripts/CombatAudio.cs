using UnityEngine;

namespace Nghia
{
    public class CombatAudio : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CombatAudioSubject combatSubject;
        [SerializeField] private AudioSource audioSource;

        [Header("Hit Sound")]
        [SerializeField] private AudioClip[] hitClips;

        [Header("Take Damage Sound")]
        [SerializeField] private AudioClip[] damageClips;

        [Header("Volume")]
        [Range(0f, 1f)]
        [SerializeField] private float hitVolume = 0.8f;

        [Range(0f, 1f)]
        [SerializeField] private float damageVolume = 0.8f;

        private void Awake()
        {
            if (combatSubject == null)
                combatSubject = GetComponent<CombatAudioSubject>();

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
            if (combatSubject == null)
                return;

            combatSubject.OnHit += PlayHitSound;
            combatSubject.OnTakeDamage += PlayDamageSound;
        }

        private void OnDisable()
        {
            if (combatSubject == null)
                return;

            combatSubject.OnHit -= PlayHitSound;
            combatSubject.OnTakeDamage -= PlayDamageSound;
        }

        private void PlayHitSound()
        {
            PlayRandomClip(hitClips, hitVolume);
        }

        private void PlayDamageSound()
        {
            PlayRandomClip(damageClips, damageVolume);
        }

        private void PlayRandomClip(AudioClip[] clips, float volume)
        {
            if (audioSource == null)
                return;

            if (clips == null || clips.Length == 0)
                return;

            AudioClip clip = clips[Random.Range(0, clips.Length)];

            audioSource.PlayOneShot(clip, volume);
        }
    }
}