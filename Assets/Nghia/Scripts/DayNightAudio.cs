using UnityEngine;

namespace DucThanh
{
    /// <summary>
    /// Observer xử lý âm thanh theo chu kỳ ngày / đêm.
    /// Lắng nghe event từ DayNightController.
    /// </summary>
    public class DayNightAudio : MonoBehaviour
    {
        [Header("Day Night Controller")]
        [SerializeField] private DayNightController dayNightController;

        [Header("Music")]
        [SerializeField] private AudioSource musicSource;

        [SerializeField] private AudioClip dayAmbient;
        [SerializeField] private AudioClip nightAmbient;

        [Header("New Day")]
        [SerializeField] private AudioSource newDaySource;
        [SerializeField] private AudioClip newDayClip;

        [Header("Volume")]
        [Range(0f, 1f)]
        [SerializeField] private float musicVolume = 0.5f;

        [Range(0f, 1f)]
        [SerializeField] private float newDayVolume = 1f;

        private void Awake()
        {
            if (dayNightController == null)
            {
                dayNightController =
                    Object.FindFirstObjectByType<DayNightController>();
            }
        }

        private void OnEnable()
        {
            if (dayNightController == null) return;

            // Observer đăng ký lắng nghe event
            dayNightController.OnDayNightStateChanged += OnDayNightStateChanged;
            dayNightController.OnDayChanged += OnDayChanged;
        }

        private void Start()
        {
            if (musicSource == null)
            {
                Debug.LogError("DayNightAudio: Chưa gán Music Source!");
                return;
            }

            musicSource.loop = true;
            musicSource.volume = musicVolume;

            // Xác định nhạc ngay khi game bắt đầu
            if (dayNightController != null)
            {
                if (dayNightController.IsDayTime)
                {
                    PlayDayMusic();
                }
                else
                {
                    PlayNightMusic();
                }
            }

            if (newDaySource != null)
            {
                newDaySource.loop = false;
                newDaySource.volume = newDayVolume;
            }
        }

        /// <summary>
        /// Observer nhận event khi chuyển Day <-> Night.
        /// </summary>
        private void OnDayNightStateChanged(bool isDay)
        {
            if (isDay)
            {
                PlayDayMusic();
            }
            else
            {
                PlayNightMusic();
            }
        }

        /// <summary>
        /// Observer nhận event khi sang ngày mới.
        /// </summary>
        private void OnDayChanged(int newDay)
        {
            Debug.Log("New Day: " + newDay);

            if (newDayClip != null && newDaySource != null)
            {
                newDaySource.PlayOneShot(newDayClip, newDayVolume);
            }
        }

        private void PlayDayMusic()
        {
            if (musicSource == null || dayAmbient == null)
                return;

            if (musicSource.clip == dayAmbient && musicSource.isPlaying)
                return;

            musicSource.clip = dayAmbient;
            musicSource.loop = true;
            musicSource.volume = musicVolume;
            musicSource.Play();

            Debug.Log("Audio: Day Ambient");
        }

        private void PlayNightMusic()
        {
            if (musicSource == null || nightAmbient == null)
                return;

            if (musicSource.clip == nightAmbient && musicSource.isPlaying)
                return;

            musicSource.clip = nightAmbient;
            musicSource.loop = true;
            musicSource.volume = musicVolume;
            musicSource.Play();

            Debug.Log("Audio: Night Ambient");
        }

        private void OnDisable()
        {
            if (dayNightController == null) return;

            // Observer hủy đăng ký
            dayNightController.OnDayNightStateChanged -= OnDayNightStateChanged;
            dayNightController.OnDayChanged -= OnDayChanged;
        }
    }
}