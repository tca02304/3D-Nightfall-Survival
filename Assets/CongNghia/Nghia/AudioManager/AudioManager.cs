using UnityEngine;
using System;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("========== MUSIC ==========")]

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip dayAmbient;
    [SerializeField] private AudioClip exploration;
    [SerializeField] private AudioClip dusk;
    [SerializeField] private AudioClip nightCombat;
    [SerializeField] private AudioClip boss;
    [SerializeField] private AudioClip victory;


    [Header("========== SFX ==========")]

    [SerializeField] private AudioSource sfxSource;

    [SerializeField] private AudioClip mining;
    [SerializeField] private AudioClip chopping;
    [SerializeField] private AudioClip pickup;
    [SerializeField] private AudioClip crafting;
    [SerializeField] private AudioClip weaponHit;
    [SerializeField] private AudioClip criticalHit;
    [SerializeField] private AudioClip enemyAttack;
    [SerializeField] private AudioClip enemyDeath;
    [SerializeField] private AudioClip chestOpen;
    [SerializeField] private AudioClip buffReveal;


    [Header("========== VOLUME ==========")]

    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.3f;

    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 0.8f;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        musicSource.loop = true;
        musicSource.volume = musicVolume;

        sfxSource.volume = sfxVolume;
    }


    private void OnEnable()
    {
        AudioEventSystem.OnAudioEvent += HandleAudioEvent;
    }


    private void OnDisable()
    {
        AudioEventSystem.OnAudioEvent -= HandleAudioEvent;
    }


    private void HandleAudioEvent(AudioEvent audioEvent)
    {
        switch (audioEvent)
        {
            // MUSIC
            case AudioEvent.DayAmbient:
                PlayMusic(dayAmbient);
                break;

            case AudioEvent.Exploration:
                PlayMusic(exploration);
                break;

            case AudioEvent.Dusk:
                PlayMusic(dusk);
                break;

            case AudioEvent.NightCombat:
                PlayMusic(nightCombat);
                break;

            case AudioEvent.Boss:
                PlayMusic(boss);
                break;

            case AudioEvent.Victory:
                PlayMusic(victory, false);
                break;

            // SFX
            case AudioEvent.Mining:
                PlaySFX(mining);
                break;

            case AudioEvent.Chopping:
                PlaySFX(chopping);
                break;

            case AudioEvent.Pickup:
                PlaySFX(pickup);
                break;

            case AudioEvent.Crafting:
                PlaySFX(crafting);
                break;

            case AudioEvent.WeaponHit:
                PlaySFX(weaponHit);
                break;

            case AudioEvent.CriticalHit:
                PlaySFX(criticalHit);
                break;

            case AudioEvent.EnemyAttack:
                PlaySFX(enemyAttack);
                break;

            case AudioEvent.EnemyDeath:
                PlaySFX(enemyDeath);
                break;

            case AudioEvent.ChestOpen:
                PlaySFX(chestOpen);
                break;

            case AudioEvent.BuffReveal:
                PlaySFX(buffReveal);
                break;
        }
    }
    private void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null)
            return;

        if (musicSource.clip == clip)
            return;

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }


    private void PlaySFX(AudioClip clip)
    {
        if (clip == null)
            return;

        sfxSource.PlayOneShot(clip, sfxVolume);
    }
}