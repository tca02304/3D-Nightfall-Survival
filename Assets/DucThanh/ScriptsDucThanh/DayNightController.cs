using System;
using UnityEngine;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Object = UnityEngine.Object;

namespace DucThanh
{
    /// <summary>
    /// Hệ thống quản lý chu kỳ ngày / đêm (Day-Night Cycle) và đồng hồ trong game.
    /// Quy định: 1 ngày 24 giờ = 12 phút thực tế (30 giây thực tế = 1 giờ trong game).
    /// Hỗ trợ điều khiển vị trí/cường độ/màu sắc của Mặt trời và Mặt trăng, Skybox/Bầu trời, đồng thời cập nhật UI đồng hồ.
    /// Tự động tìm kiếm UI nếu UI được sinh ra sau (Dynamic Spawning).
    /// </summary>
    public class DayNightController : MonoBehaviour
    {
        public static DayNightController Instance { get; private set; }

        [Header("Time Settings")]
        [Tooltip("Tỉ lệ: Số giây thực tế cho 1 giờ game. Mặc định 30s (1 ngày 24 giờ = 12 phút thực tế, 30 giây thực tế = 1 giờ game).")]
        [SerializeField] private float realSecondsPerInGameHour = 30f;

        [Tooltip("Giờ bắt đầu lúc vào game (từ 0.0h đến 24.0h). Mặc định: 6.0 = 6h sáng.")]
        [Range(0f, 24f)]
        [SerializeField] private float startHour = 6f;

        [Tooltip("Tạm dừng thời gian trôi (tiện khi pause game hoặc debug).")]
        [SerializeField] private bool pauseTime = false;

        [Header("Fast Forward (Tua nhanh thời gian)")]
        [Tooltip("Bật/tắt tính năng tua nhanh")]
        [SerializeField] private bool enableFastForward = true;

        [Tooltip("Phím kích hoạt tua nhanh thời gian (mặc định phím P)")]
        [SerializeField] private KeyCode fastForwardKey = KeyCode.P;

        [Tooltip("Hệ số tua nhanh khi ấn giữ hoặc bật toggle (ví dụ: 60x nghĩa là 1 giây trôi qua 1 giờ trong game)")]
        [SerializeField] private float fastForwardMultiplier = 60f;

        [Tooltip("Nếu tích chọn: Nhấn P một lần để bật/tắt (Toggle). Nếu bỏ tích: Giữ phím P để tua, nhả ra thì quay lại tốc độ bình thường.")]
        [SerializeField] private bool toggleFastForward = false;

        private bool isFastForwardActive = false;

        [Header("Debug & Test Keys (Phím tắt kiểm tra)")]
        [Tooltip("Bật phím tắt test")]
        [SerializeField] private bool enableDebugKeys = true;

        [Tooltip("Phím nhảy sang 6h00 sáng ngày tiếp theo (mặc định phím N)")]
        [SerializeField] private KeyCode skipToNextDayKey = KeyCode.N;

        [Header("Light References")]
        [Tooltip("Directional Light đại diện cho Mặt trời.")]
        [SerializeField] private Light sunLight;

        [Tooltip("Directional Light đại diện cho Mặt trăng (sẽ sáng vào ban đêm).")]
        [SerializeField] private Light moonLight;

        [Header("Visual Moon Mesh / Sphere (Hình cầu Mặt Trăng trên trời)")]
        [Tooltip("Tự động tạo khối cầu Mặt Trăng phát sáng trên bầu trời nếu chưa có")]
        [SerializeField] private bool autoCreateVisualMoon = true;
        [Tooltip("Khoảng cách từ tâm bầu trời đến mặt trăng")]
        [SerializeField] private float moonDistance = 250f;
        [Tooltip("Kích thước đĩa/khối cầu mặt trăng")]
        [SerializeField] private float moonSize = 25f;
        [Tooltip("Màu phát sáng của mặt trăng trên bầu trời")]
        [SerializeField] private Color moonVisualColor = new Color(0.9f, 0.95f, 1f, 1f);
        [SerializeField] private GameObject visualMoonObject;

        [Header("Starry Sky (Bầu trời đầy sao ban đêm)")]
        [Tooltip("Tự động tạo vòm sao lấp lánh trên bầu trời đêm")]
        [SerializeField] private bool autoCreateStars = true;
        [Tooltip("Số lượng ngôi sao trên bầu trời")]
        [SerializeField] private int starCount = 350;
        [Tooltip("Khoảng cách vòm sao")]
        [SerializeField] private float starDomeRadius = 260f;
        [Tooltip("Kích thước hạt sao")]
        [SerializeField] private float starSize = 1.2f;
        [SerializeField] private ParticleSystem starParticleSystem;

        [Header("Sun Settings")]
        [SerializeField] private float maxSunIntensity = 1.3f;
        [SerializeField] private Gradient sunColor;

        [Header("Moon Settings")]
        [Tooltip("Cường độ ánh sáng của Mặt trăng khi lên đỉnh ban đêm")]
        [SerializeField] private float maxMoonIntensity = 1.0f;
        [Tooltip("Độ đậm của bóng đổ ban đêm (0 = không có bóng, 1 = bóng đen rõ)")]
        [Range(0f, 1f)]
        [SerializeField] private float moonShadowStrength = 0.85f;
        [SerializeField] private Gradient moonColor;

        [Header("Ambient / Environment Settings")]
        [Tooltip("Tự động đổi màu/cường độ Ambient Light theo thời gian")]
        [SerializeField] private bool controlAmbientLight = true;
        [SerializeField] private Gradient ambientColor;

        [Header("Skybox Settings (Bầu trời)")]
        [Tooltip("Tự động điều chỉnh độ sáng/màu sắc của Skybox Material theo chu kỳ ngày đêm")]
        [SerializeField] private bool controlSkybox = true;
        [Tooltip("Dải màu sắc / độ phơi sáng của bầu trời theo thời gian (0h -> 24h).")]
        [SerializeField] private Gradient skyColor;
        [Tooltip("Độ phơi sáng tối đa ban ngày (_Exposure)")]
        [SerializeField] private float daySkyExposure = 1.0f;
        [Tooltip("Độ phơi sáng tối thiểu ban đêm (_Exposure)")]
        [SerializeField] private float nightSkyExposure = 0.15f;
        private Material runtimeSkyboxMat;

        [Header("UI Reference")]
        [Tooltip("Text hiển thị đồng hồ thời gian (TextMeshProUGUI). Code sẽ tự tìm nếu bỏ trống.")]
        [SerializeField] private TextMeshProUGUI clockText;
        [Tooltip("Text hiển thị ngày thứ mấy (Day 1, Day 2...). Code sẽ tự tìm nếu bỏ trống.")]
        [SerializeField] private TextMeshProUGUI dayText;
        [SerializeField] private string clockPrefix = "";
        [SerializeField] private bool showSeconds = false;

        // Runtime state
        private float currentInGameTime;
        private int currentDay = 1;

        public float CurrentHour => currentInGameTime;
        public int CurrentDay => currentDay;
        public bool IsDayTime => currentInGameTime >= 6f && currentInGameTime < 18f;
        public float RealSecondsPerInGameHour => realSecondsPerInGameHour;
        public bool IsFastForwardActive => isFastForwardActive;
        public float FastForwardMultiplier => fastForwardMultiplier;

        public event Action<int> OnDayChanged;
        public event Action<bool> OnDayNightStateChanged;
        private bool lastDayState;

        private void Reset()
        {
            SetupDefaultGradients();
        }

        private void Awake()
        {
            // Thiết lập Singleton để dễ truy cập từ các script khác
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            currentInGameTime = Mathf.Clamp(startHour, 0f, 24f);
            lastDayState = IsDayTime;

            if (sunColor == null || sunColor.colorKeys.Length == 0)
                SetupDefaultGradients();
            else if (skyColor == null || skyColor.colorKeys == null || skyColor.colorKeys.Length == 0)
                SetupDefaultSkyGradient();

            if (sunLight == null)
            {
                Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
                foreach (var l in lights)
                {
                    if (l.type == LightType.Directional) { sunLight = l; break; }
                }
            }

            if (moonLight == null)
            {
                Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
                foreach (var l in lights)
                {
                    if (l.type == LightType.Directional && l != sunLight) { moonLight = l; break; }
                }

                if (moonLight == null)
                {
                    GameObject moonObj = new GameObject("Moon Light");
                    moonObj.transform.SetParent(transform);
                    moonLight = moonObj.AddComponent<Light>();
                    moonLight.type = LightType.Directional;
                    moonLight.intensity = 0f;
                    moonLight.shadows = LightShadows.Soft;
                }
            }

            if (autoCreateVisualMoon && visualMoonObject == null) CreateVisualMoonMesh();
            if (autoCreateStars && starParticleSystem == null) CreateStarDome();

            if (controlSkybox && RenderSettings.skybox != null)
            {
                runtimeSkyboxMat = new Material(RenderSettings.skybox);
                RenderSettings.skybox = runtimeSkyboxMat;
            }
        }

        private void Start()
        {
            UpdateLighting();
            UpdateUI();
        }

        private void Update()
        {
            AutoFindUI(); // Tự động tìm UI nếu UI được spawn ra trễ
            HandleFastForwardInput();
            HandleDebugInput();

            if (!pauseTime && realSecondsPerInGameHour > 0f)
            {
                float speedMultiplier = isFastForwardActive ? Mathf.Max(1f, fastForwardMultiplier) : 1f;
                float hoursElapsed = (Time.deltaTime / realSecondsPerInGameHour) * speedMultiplier;
                currentInGameTime += hoursElapsed;

                if (currentInGameTime >= 24f)
                {
                    currentInGameTime -= 24f;
                    currentDay++;
                    OnDayChanged?.Invoke(currentDay);
                }

                bool currentDayState = IsDayTime;
                if (currentDayState != lastDayState)
                {
                    lastDayState = currentDayState;
                    OnDayNightStateChanged?.Invoke(currentDayState);
                }
            }

            UpdateLighting();
            UpdateUI();
        }

        // --- Logic Tự động tìm UI mà không cần kéo thả ---
        private void AutoFindUI()
        {
            if (clockText == null)
            {
                GameObject obj = GameObject.Find("Time Text");
                if (obj != null) clockText = obj.GetComponent<TextMeshProUGUI>();
            }

            if (dayText == null)
            {
                GameObject obj = GameObject.Find("Day Text");
                if (obj != null) dayText = obj.GetComponent<TextMeshProUGUI>();
            }
        }

        private void HandleFastForwardInput()
        {
            if (!enableFastForward)
            {
                isFastForwardActive = false;
                return;
            }

            bool isKeyPressed = false;
            bool isKeyDown = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                Key key = ConvertKeyCodeToInputSystemKey(fastForwardKey);
                if (key != Key.None)
                {
                    var keyControl = Keyboard.current[key];
                    if (keyControl != null)
                    {
                        isKeyPressed = keyControl.isPressed;
                        isKeyDown = keyControl.wasPressedThisFrame;
                    }
                }
            }
#else
            isKeyPressed = Input.GetKey(fastForwardKey);
            isKeyDown = Input.GetKeyDown(fastForwardKey);
#endif

            if (toggleFastForward)
            {
                if (isKeyDown) isFastForwardActive = !isFastForwardActive;
            }
            else
            {
                isFastForwardActive = isKeyPressed;
            }
        }

        private void HandleDebugInput()
        {
            if (!enableDebugKeys) return;
            bool isSkipPressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                Key key = ConvertKeyCodeToInputSystemKey(skipToNextDayKey);
                if (key != Key.None)
                {
                    var keyControl = Keyboard.current[key];
                    if (keyControl != null) isSkipPressed = keyControl.wasPressedThisFrame;
                }
            }
#else
            isSkipPressed = Input.GetKeyDown(skipToNextDayKey);
#endif

            if (isSkipPressed) SkipToNextDayAt6AM();
        }

#if ENABLE_INPUT_SYSTEM
        private Key ConvertKeyCodeToInputSystemKey(KeyCode code)
        {
            switch (code)
            {
                case KeyCode.P: return Key.P;
                case KeyCode.N: return Key.N;
                case KeyCode.Space: return Key.Space;
                case KeyCode.T: return Key.T;
                case KeyCode.F: return Key.F;
                default:
                    if (Enum.TryParse(code.ToString(), out Key parsedKey)) return parsedKey;
                    return Key.P;
            }
        }
#endif

        private void SetupDefaultGradients()
        {
            sunColor = new Gradient();
            GradientColorKey[] sKeys = new GradientColorKey[5];
            sKeys[0] = new GradientColorKey(new Color(0.2f, 0.2f, 0.35f), 0.0f);
            sKeys[1] = new GradientColorKey(new Color(1.0f, 0.65f, 0.4f), 0.25f);
            sKeys[2] = new GradientColorKey(new Color(1.0f, 0.98f, 0.92f), 0.5f);
            sKeys[3] = new GradientColorKey(new Color(1.0f, 0.5f, 0.25f), 0.75f);
            sKeys[4] = new GradientColorKey(new Color(0.2f, 0.2f, 0.35f), 1.0f);
            GradientAlphaKey[] sAlpha = new GradientAlphaKey[2] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) };
            sunColor.SetKeys(sKeys, sAlpha);

            moonColor = new Gradient();
            GradientColorKey[] mKeys = new GradientColorKey[5];
            mKeys[0] = new GradientColorKey(new Color(0.92f, 0.95f, 1.0f), 0.0f);
            mKeys[1] = new GradientColorKey(new Color(0.7f, 0.75f, 0.85f), 0.25f);
            mKeys[2] = new GradientColorKey(new Color(0.5f, 0.5f, 0.6f), 0.5f);
            mKeys[3] = new GradientColorKey(new Color(0.85f, 0.88f, 0.95f), 0.75f);
            mKeys[4] = new GradientColorKey(new Color(0.92f, 0.95f, 1.0f), 1.0f);
            moonColor.SetKeys(mKeys, sAlpha);

            ambientColor = new Gradient();
            GradientColorKey[] aKeys = new GradientColorKey[5];
            aKeys[0] = new GradientColorKey(new Color(0.12f, 0.15f, 0.22f), 0.0f);
            aKeys[1] = new GradientColorKey(new Color(0.28f, 0.25f, 0.3f), 0.25f);
            aKeys[2] = new GradientColorKey(new Color(0.5f, 0.52f, 0.55f), 0.5f);
            aKeys[3] = new GradientColorKey(new Color(0.3f, 0.24f, 0.24f), 0.75f);
            aKeys[4] = new GradientColorKey(new Color(0.12f, 0.15f, 0.22f), 1.0f);
            ambientColor.SetKeys(aKeys, sAlpha);

            SetupDefaultSkyGradient();
        }

        private void SetupDefaultSkyGradient()
        {
            skyColor = new Gradient();
            GradientColorKey[] skyKeys = new GradientColorKey[5];
            skyKeys[0] = new GradientColorKey(new Color(0.05f, 0.05f, 0.15f), 0.0f);
            skyKeys[1] = new GradientColorKey(new Color(0.9f, 0.6f, 0.45f), 0.25f);
            skyKeys[2] = new GradientColorKey(new Color(0.5f, 0.7f, 0.95f), 0.5f);
            skyKeys[3] = new GradientColorKey(new Color(0.85f, 0.45f, 0.25f), 0.75f);
            skyKeys[4] = new GradientColorKey(new Color(0.05f, 0.05f, 0.15f), 1.0f);
            GradientAlphaKey[] skyAlpha = new GradientAlphaKey[2] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) };
            skyColor.SetKeys(skyKeys, skyAlpha);
        }

        private void UpdateLighting()
        {
            float timePercent = currentInGameTime / 24f;
            float sunAngle = (timePercent * 360f) - 90f;
            float currentSunDot = 0f;

            if (sunLight != null)
            {
                sunLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);
                currentSunDot = Mathf.Clamp01(Vector3.Dot(sunLight.transform.forward, Vector3.down));
                sunLight.intensity = currentSunDot * maxSunIntensity;
                sunLight.color = sunColor.Evaluate(timePercent);
                sunLight.enabled = sunLight.intensity > 0.01f;
            }

            if (moonLight != null)
            {
                float moonAngle = sunAngle + 180f;
                moonLight.transform.rotation = Quaternion.Euler(moonAngle, 170f, 0f);
                float moonDot = Mathf.Clamp01(Vector3.Dot(moonLight.transform.forward, Vector3.down));
                moonLight.intensity = moonDot * maxMoonIntensity;
                moonLight.color = moonColor.Evaluate(timePercent);
                moonLight.enabled = moonLight.intensity > 0.01f;

                if (moonLight.shadows != LightShadows.Soft) moonLight.shadows = LightShadows.Soft;
                moonLight.shadowStrength = moonShadowStrength;

                Transform camTransform = Camera.main != null ? Camera.main.transform : transform;
                Vector3 centerPos = camTransform != null ? camTransform.position : Vector3.zero;

                if (visualMoonObject != null)
                {
                    Vector3 moonDirection = -moonLight.transform.forward;
                    visualMoonObject.transform.position = centerPos + (moonDirection * moonDistance);
                    visualMoonObject.transform.rotation = Quaternion.LookRotation(moonDirection);

                    bool isMoonAboveHorizon = moonDirection.y > -0.05f;
                    if (visualMoonObject.activeSelf != isMoonAboveHorizon) visualMoonObject.SetActive(isMoonAboveHorizon);
                }

                if (starParticleSystem != null)
                {
                    starParticleSystem.transform.position = centerPos;
                    float nightFactor = Mathf.Clamp01(1f - (currentSunDot * 3f));
                    var mainModule = starParticleSystem.main;
                    mainModule.startColor = new Color(1f, 1f, 1f, nightFactor);

                    bool shouldShowStars = nightFactor > 0.05f;
                    if (starParticleSystem.gameObject.activeSelf != shouldShowStars) starParticleSystem.gameObject.SetActive(shouldShowStars);
                }
            }

            if (controlAmbientLight && ambientColor != null)
            {
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = ambientColor.Evaluate(timePercent);
            }

            UpdateSkybox(timePercent, currentSunDot);
        }

        private void UpdateSkybox(float timePercent, float currentSunDot)
        {
            if (!controlSkybox) return;
            Material skyMat = runtimeSkyboxMat != null ? runtimeSkyboxMat : RenderSettings.skybox;
            if (skyMat == null) return;

            float dayFactor = Mathf.Clamp01(currentSunDot);
            float currentExposure = Mathf.Lerp(nightSkyExposure, daySkyExposure, dayFactor);

            if (skyMat.HasProperty("_Exposure")) skyMat.SetFloat("_Exposure", currentExposure);

            if (skyColor != null)
            {
                Color currentSkyCol = skyColor.Evaluate(timePercent);
                if (skyMat.HasProperty("_SkyTint")) skyMat.SetColor("_SkyTint", currentSkyCol);
                else if (skyMat.HasProperty("_Tint")) skyMat.SetColor("_Tint", currentSkyCol);
                else if (skyMat.HasProperty("_Color")) skyMat.SetColor("_Color", currentSkyCol);
                else if (skyMat.HasProperty("_BaseColor")) skyMat.SetColor("_BaseColor", currentSkyCol);
            }
        }

        // --- Cập nhật số liệu ra UI ---
        private void UpdateUI()
        {
            if (clockText != null)
            {
                int hours = Mathf.FloorToInt(currentInGameTime);
                float minuteRemainder = (currentInGameTime - hours) * 60f;
                int minutes = Mathf.FloorToInt(minuteRemainder);

                if (showSeconds)
                {
                    int seconds = Mathf.FloorToInt((minuteRemainder - minutes) * 60f);
                    clockText.text = $"{clockPrefix}{hours:D2}:{minutes:D2}:{seconds:D2}";
                }
                else
                {
                    clockText.text = $"{clockPrefix}{hours:D2}:{minutes:D2}";
                }
            }

            if (dayText != null)
            {
                dayText.text = $"Day {currentDay}";
            }
        }

        private void CreateVisualMoonMesh()
        {
            visualMoonObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visualMoonObject.name = "Visual_Moon";
            visualMoonObject.transform.SetParent(transform);
            visualMoonObject.transform.localScale = Vector3.one * moonSize;

            Collider col = visualMoonObject.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Renderer rend = visualMoonObject.GetComponent<Renderer>();
            if (rend != null)
            {
                Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
                Material moonMat = new Material(unlitShader);
                moonMat.name = "M_VisualMoon";

                if (moonMat.HasProperty("_BaseColor")) moonMat.SetColor("_BaseColor", moonVisualColor);
                else if (moonMat.HasProperty("_Color")) moonMat.SetColor("_Color", moonVisualColor);

                if (moonMat.HasProperty("_EmissionColor"))
                {
                    moonMat.EnableKeyword("_EMISSION");
                    moonMat.SetColor("_EmissionColor", moonVisualColor * 1.5f);
                }

                rend.material = moonMat;
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rend.receiveShadows = false;
            }
        }

        private void CreateStarDome()
        {
            GameObject starObj = new GameObject("Starry_Sky");
            starObj.transform.SetParent(transform);
            starParticleSystem = starObj.AddComponent<ParticleSystem>();

            var main = starParticleSystem.main;
            main.loop = true;
            main.playOnAwake = true;
            main.maxParticles = starCount;
            main.startSpeed = 0f;
            main.startLifetime = Mathf.Infinity;
            main.startSize = new ParticleSystem.MinMaxCurve(starSize * 0.7f, starSize * 1.5f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new Color(1f, 1f, 1f, 1f);

            var emission = starParticleSystem.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)starCount) });

            var shape = starParticleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = starDomeRadius;
            shape.radiusThickness = 0.1f;

            var sizeOverLifetime = starParticleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 0.8f); curve.AddKey(0.25f, 1.3f); curve.AddKey(0.5f, 0.7f); curve.AddKey(0.75f, 1.2f); curve.AddKey(1f, 0.8f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            var renderer = starObj.GetComponent<ParticleSystemRenderer>();
            Shader starShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Unlit/Color");
            Material starMat = new Material(starShader);
            starMat.name = "M_StarSky";
            if (starMat.HasProperty("_BaseColor")) starMat.SetColor("_BaseColor", Color.white);
            if (starMat.HasProperty("_Color")) starMat.SetColor("_Color", Color.white);

            renderer.material = starMat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            starParticleSystem.Play();
        }

        public void SetTime(float hour)
        {
            currentInGameTime = Mathf.Repeat(hour, 24f);
            UpdateLighting();
            UpdateUI();
        }

        public void SkipToNextDayAt6AM()
        {
            currentDay++;
            currentInGameTime = 6f;
            OnDayChanged?.Invoke(currentDay);

            bool currentDayState = IsDayTime;
            if (currentDayState != lastDayState)
            {
                lastDayState = currentDayState;
                OnDayNightStateChanged?.Invoke(currentDayState);
            }
            UpdateLighting();
            UpdateUI();
        }

        public void SkipToDay() => SetTime(8f);
        public void SkipToNight() => SetTime(20f);
    }
}