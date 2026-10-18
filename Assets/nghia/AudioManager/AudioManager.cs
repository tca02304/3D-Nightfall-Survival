using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    // =========================================================
    // MUSIC
    // =========================================================

    [Header("========== MUSIC ==========")]

    [SerializeField]
    private AudioSource musicSource;

    [Range(0f, 1f)]
    [SerializeField]
    private float musicVolume = 0.3f;

    [Space(5)]

    [SerializeField] private AudioClip dayAmbient;
    [SerializeField] private AudioClip exploration;
    [SerializeField] private AudioClip dusk;
    [SerializeField] private AudioClip nightCombat;
    [SerializeField] private AudioClip boss;
    [SerializeField] private AudioClip victory;


    // =========================================================
    // GLOBAL SFX
    // =========================================================

    [Header("========== GLOBAL SFX ==========")]

    [SerializeField]
    private AudioSource sfxSource;

    [Range(0f, 1f)]
    [SerializeField]
    private float sfxVolume = 1f;

    [Space(5)]

    [SerializeField] private AudioClip pickup;
    [SerializeField] private AudioClip crafting;
    [SerializeField] private AudioClip buffReveal;


    // =========================================================
    // 3D SFX
    // =========================================================

    [Header("========== 3D SFX ==========")]

    [SerializeField] private AudioClip mining;
    [SerializeField] private AudioClip chopping;
    [SerializeField] private AudioClip weaponHit;
    [SerializeField] private AudioClip criticalHit;
    [SerializeField] private AudioClip enemyAttack;
    [SerializeField] private AudioClip enemyDeath;
    [SerializeField] private AudioClip chestOpen;


    // =========================================================
    // SINGLETON
    // =========================================================

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        musicSource.volume = musicVolume;
        sfxSource.volume = sfxVolume;

        PlayDayAmbient();
    }


    // =========================================================
    // MUSIC
    // =========================================================

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null)
            return;

        if (musicSource.clip == clip && musicSource.isPlaying)
            return;

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }


    public void StopMusic()
    {
        musicSource.Stop();
    }


    public void PauseMusic()
    {
        musicSource.Pause();
    }


    public void ResumeMusic()
    {
        musicSource.UnPause();
    }


    // =========================================================
    // MUSIC PRESETS
    // =========================================================

    public void PlayDayAmbient()
    {
        PlayMusic(dayAmbient);
    }


    public void PlayExploration()
    {
        PlayMusic(exploration);
    }


    public void PlayDusk()
    {
        PlayMusic(dusk);
    }


    public void PlayNightCombat()
    {
        PlayMusic(nightCombat);
    }


    public void PlayBoss()
    {
        PlayMusic(boss);
    }


    public void PlayVictory()
    {
        PlayMusic(victory);
    }


    // =========================================================
    // GLOBAL SFX - 2D
    // =========================================================

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null)
            return;

        sfxSource.PlayOneShot(clip, sfxVolume);
    }


    public void PlayPickup()
    {
        PlaySFX(pickup);
    }


    public void PlayCrafting()
    {
        PlaySFX(crafting);
    }


    public void PlayBuffReveal()
    {
        PlaySFX(buffReveal);
    }


    // =========================================================
    // 3D SFX
    // =========================================================

    public void Play3DSFX(AudioClip clip, Vector3 position)
    {
        if (clip == null)
            return;

        AudioSource.PlayClipAtPoint(
            clip,
            position,
            sfxVolume
        );
    }


    // =========================================================
    // 3D SFX PRESETS
    // =========================================================

    public void PlayMining(Vector3 position)
    {
        Play3DSFX(mining, position);
    }


    public void PlayChopping(Vector3 position)
    {
        Play3DSFX(chopping, position);
    }


    public void PlayWeaponHit(Vector3 position)
    {
        Play3DSFX(weaponHit, position);
    }


    public void PlayCriticalHit(Vector3 position)
    {
        Play3DSFX(criticalHit, position);
    }


    public void PlayEnemyAttack(Vector3 position)
    {
        Play3DSFX(enemyAttack, position);
    }


    public void PlayEnemyDeath(Vector3 position)
    {
        Play3DSFX(enemyDeath, position);
    }


    public void PlayChestOpen(Vector3 position)
    {
        Play3DSFX(chestOpen, position);
    }
}