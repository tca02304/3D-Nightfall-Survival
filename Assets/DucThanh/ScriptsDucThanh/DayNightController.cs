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
    /// Quy định: 1 phút thực tế = 1 giờ trong game (1 ngày 24 giờ = 24 phút thực tế).
    /// Hỗ trợ điều khiển vị trí/cường độ/màu sắc của Mặt trời và Mặt trăng, đồng thời cập nhật UI đồng hồ.
    /// </summary>
    public class DayNightController : MonoBehaviour
    {
        [Header("Time Settings")]
        [Tooltip("Tỉ lệ: Số giây thực tế cho 1 giờ game. Mặc định 60s (1 phút thực tế = 1 giờ game).")]
        [SerializeField] private float realSecondsPerInGameHour = 60f;

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

        [Header("UI Reference")]
        [Tooltip("Text hiển thị đồng hồ thời gian (TextMeshProUGUI). Nếu có sẽ tự động format HH:mm hoặc Day/Night.")]
        [SerializeField] private TextMeshProUGUI clockText;
        [Tooltip("Text hiển thị ngày thứ mấy (Day 1, Day 2...). Có thể bỏ trống.")]
        [SerializeField] private TextMeshProUGUI dayText;
        [SerializeField] private string clockPrefix = "";
        [SerializeField] private bool showSeconds = false;

        // Runtime state
        private float currentInGameTime; // Đơn vị: giờ (0 -> 24)
        private int currentDay = 1;

        public float CurrentHour => currentInGameTime;
        public int CurrentDay => currentDay;
        public bool IsDayTime => currentInGameTime >= 6f && currentInGameTime < 18f;

        // Events để các script khác (ví dụ AI quái vật, spawning, stats...) có thể lắng nghe
        public event Action<int> OnDayChanged;
        public event Action<bool> OnDayNightStateChanged; // true: Ban ngày, false: Ban đêm
        private bool lastDayState;

        private void Reset()
        {
            SetupDefaultGradients();
        }

        private void Awake()
        {
            currentInGameTime = Mathf.Clamp(startHour, 0f, 24f);
            lastDayState = IsDayTime;

            if (sunColor == null || sunColor.colorKeys.Length == 0)
            {
                SetupDefaultGradients();
            }

            // Tự động tìm sunLight nếu chưa gán
            if (sunLight == null)
            {
                Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
                foreach (var l in lights)
                {
                    if (l.type == LightType.Directional)
                    {
                        sunLight = l;
                        break;
                    }
                }
            }

            // Tự động tìm hoặc tạo moonLight nếu chưa gán
            if (moonLight == null)
            {
                Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
                foreach (var l in lights)
                {
                    if (l.type == LightType.Directional && l != sunLight)
                    {
                        moonLight = l;
                        break;
                    }
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

            // Tự động tạo khối cầu Mặt Trăng hiển thị trên bầu trời
            if (autoCreateVisualMoon && visualMoonObject == null)
            {
                CreateVisualMoonMesh();
            }

            // Tự động tạo vòm sao lấp lánh ban đêm
            if (autoCreateStars && starParticleSystem == null)
            {
                CreateStarDome();
            }
        }

        private void Start()
        {
            UpdateLighting();
            UpdateUI();
        }

        private void Update()
        {
            HandleFastForwardInput();
            HandleDebugInput();

            if (!pauseTime && realSecondsPerInGameHour > 0f)
            {
                // Tính toán thời gian trôi:
                // realSecondsPerInGameHour giây thực tế = 1 giờ in-game => tốc độ = 1f / realSecondsPerInGameHour (giờ/giây)
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
                if (isKeyDown)
                {
                    isFastForwardActive = !isFastForwardActive;
                }
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
                    if (keyControl != null)
                    {
                        isSkipPressed = keyControl.wasPressedThisFrame;
                    }
                }
            }
#else
            isSkipPressed = Input.GetKeyDown(skipToNextDayKey);
#endif

            if (isSkipPressed)
            {
                SkipToNextDayAt6AM();
            }
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
                    if (Enum.TryParse(code.ToString(), out Key parsedKey))
                        return parsedKey;
                    return Key.P;
            }
        }
#endif

        /// <summary>
        /// Khởi tạo dải màu mặc định phù hợp với chu kỳ tự nhiên: Bình minh -> Giữa trưa -> Hoàng hôn -> Đêm.
        /// </summary>
        private void SetupDefaultGradients()
        {
            // Sun Color Gradient
            sunColor = new Gradient();
            GradientColorKey[] sKeys = new GradientColorKey[5];
            sKeys[0] = new GradientColorKey(new Color(0.2f, 0.2f, 0.35f), 0.0f);     // 0h: Đêm (tối/xanh thẫm)
            sKeys[1] = new GradientColorKey(new Color(1.0f, 0.65f, 0.4f), 0.25f);    // 6h: Bình minh vàng cam
            sKeys[2] = new GradientColorKey(new Color(1.0f, 0.98f, 0.92f), 0.5f);    // 12h: Trưa nắng ấm trắng
            sKeys[3] = new GradientColorKey(new Color(1.0f, 0.5f, 0.25f), 0.75f);    // 18h: Hoàng hôn rực rỡ
            sKeys[4] = new GradientColorKey(new Color(0.2f, 0.2f, 0.35f), 1.0f);     // 24h: Đêm
            GradientAlphaKey[] sAlpha = new GradientAlphaKey[2] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) };
            sunColor.SetKeys(sKeys, sAlpha);

            // Moon Color Gradient (Mặt trăng ánh sáng trắng ngà pha chút xanh dịu)
            moonColor = new Gradient();
            GradientColorKey[] mKeys = new GradientColorKey[5];
            mKeys[0] = new GradientColorKey(new Color(0.92f, 0.95f, 1.0f), 0.0f);     // 0h: Đỉnh đêm - ánh sáng trắng tinh khôi
            mKeys[1] = new GradientColorKey(new Color(0.7f, 0.75f, 0.85f), 0.25f);    // 6h: Mờ dần
            mKeys[2] = new GradientColorKey(new Color(0.5f, 0.5f, 0.6f), 0.5f);       // 12h: Trưa
            mKeys[3] = new GradientColorKey(new Color(0.85f, 0.88f, 0.95f), 0.75f);   // 18h: Bắt đầu mọc
            mKeys[4] = new GradientColorKey(new Color(0.92f, 0.95f, 1.0f), 1.0f);     // 24h: Đỉnh đêm
            moonColor.SetKeys(mKeys, sAlpha);

            // Ambient Color Gradient (Ánh sáng môi trường ban đêm vẫn giữ độ sáng dịu mắt để thấy rõ vật thể)
            ambientColor = new Gradient();
            GradientColorKey[] aKeys = new GradientColorKey[5];
            aKeys[0] = new GradientColorKey(new Color(0.12f, 0.15f, 0.22f), 0.0f);   // 0h: Đêm sáng trắng xanh dịu
            aKeys[1] = new GradientColorKey(new Color(0.28f, 0.25f, 0.3f), 0.25f);   // 6h: Mờ sáng
            aKeys[2] = new GradientColorKey(new Color(0.5f, 0.52f, 0.55f), 0.5f);    // 12h: Đủ sáng rõ
            aKeys[3] = new GradientColorKey(new Color(0.3f, 0.24f, 0.24f), 0.75f);   // 18h: Chiều tối
            aKeys[4] = new GradientColorKey(new Color(0.12f, 0.15f, 0.22f), 1.0f);   // 24h: Đêm
            ambientColor.SetKeys(aKeys, sAlpha);
        }

        private void UpdateLighting()
        {
            // Chu kỳ 0.0 -> 1.0 tương ứng 0h -> 24h
            float timePercent = currentInGameTime / 24f;

            // Góc quay: 6h sáng = 0 độ (mọc), 12h trưa = 90 độ (đỉnh đầu), 18h chiều = 180 độ (lặn), 24h/0h = 270 độ
            float sunAngle = (timePercent * 360f) - 90f;

            // Xoay Mặt trời
            float currentSunDot = 0f;
            if (sunLight != null)
            {
                sunLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);

                // Cường độ sáng của Mặt trời: đạt đỉnh khi ở trên chân trời (sin(sunAngle) > 0)
                currentSunDot = Mathf.Clamp01(Vector3.Dot(sunLight.transform.forward, Vector3.down));
                sunLight.intensity = currentSunDot * maxSunIntensity;
                sunLight.color = sunColor.Evaluate(timePercent);
                sunLight.enabled = sunLight.intensity > 0.01f;
            }

            // Mặt trăng đối diện Mặt trời (lệch 180 độ)
            if (moonLight != null)
            {
                float moonAngle = sunAngle + 180f;
                moonLight.transform.rotation = Quaternion.Euler(moonAngle, 170f, 0f);

                float moonDot = Mathf.Clamp01(Vector3.Dot(moonLight.transform.forward, Vector3.down));
                moonLight.intensity = moonDot * maxMoonIntensity;
                moonLight.color = moonColor.Evaluate(timePercent);
                moonLight.enabled = moonLight.intensity > 0.01f;

                // Đảm bảo Mặt trăng có bóng đổ tương tự Mặt trời
                if (moonLight.shadows != LightShadows.Soft)
                {
                    moonLight.shadows = LightShadows.Soft;
                }
                moonLight.shadowStrength = moonShadowStrength;

                // Cập nhật vị trí của hình cầu Mặt Trăng trên bầu trời
                Transform camTransform = Camera.main != null ? Camera.main.transform : transform;
                Vector3 centerPos = camTransform != null ? camTransform.position : Vector3.zero;

                if (visualMoonObject != null)
                {
                    // Mặt trăng nằm ở hướng ngược lại với hướng chiếu của moonLight
                    Vector3 moonDirection = -moonLight.transform.forward;
                    visualMoonObject.transform.position = centerPos + (moonDirection * moonDistance);
                    visualMoonObject.transform.rotation = Quaternion.LookRotation(moonDirection);

                    // Bật hình cầu khi ở trên chân trời, ẩn khi dưới lòng đất
                    bool isMoonAboveHorizon = moonDirection.y > -0.05f;
                    if (visualMoonObject.activeSelf != isMoonAboveHorizon)
                    {
                        visualMoonObject.SetActive(isMoonAboveHorizon);
                    }
                }

                // Cập nhật vòm sao (bám theo Camera, chỉ sáng lấp lánh vào ban đêm)
                if (starParticleSystem != null)
                {
                    starParticleSystem.transform.position = centerPos;

                    // Độ mờ/rõ của sao: 0 khi ban ngày, 1 khi ban đêm (đạt đỉnh từ 20h đến 4h sáng)
                    float nightFactor = Mathf.Clamp01(1f - (currentSunDot * 3f));
                    var mainModule = starParticleSystem.main;
                    Color baseStarColor = new Color(1f, 1f, 1f, nightFactor);
                    mainModule.startColor = baseStarColor;

                    bool shouldShowStars = nightFactor > 0.05f;
                    if (starParticleSystem.gameObject.activeSelf != shouldShowStars)
                    {
                        starParticleSystem.gameObject.SetActive(shouldShowStars);
                    }
                }
            }

            // Điều khiển ánh sáng môi trường Ambient Light
            if (controlAmbientLight && ambientColor != null)
            {
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = ambientColor.Evaluate(timePercent);
            }
        }

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

        /// <summary>
        /// Tạo tự động một khối cầu Mặt Trăng phát sáng 3D trên bầu trời
        /// </summary>
        private void CreateVisualMoonMesh()
        {
            visualMoonObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visualMoonObject.name = "Visual_Moon";
            visualMoonObject.transform.SetParent(transform);
            visualMoonObject.transform.localScale = Vector3.one * moonSize;

            // Xóa collider để không cản trở raycast hay va chạm với nhân vật
            Collider col = visualMoonObject.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col);
            }

            // Tạo Material Unlit / Emission cho mặt trăng
            Renderer rend = visualMoonObject.GetComponent<Renderer>();
            if (rend != null)
            {
                // Ưu tiên shader URP Unlit hoặc Particles/Unlit
                Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
                if (unlitShader == null) unlitShader = Shader.Find("Unlit/Color");
                if (unlitShader == null) unlitShader = Shader.Find("Standard");

                Material moonMat = new Material(unlitShader);
                moonMat.name = "M_VisualMoon";

                if (moonMat.HasProperty("_BaseColor"))
                {
                    moonMat.SetColor("_BaseColor", moonVisualColor);
                }
                else if (moonMat.HasProperty("_Color"))
                {
                    moonMat.SetColor("_Color", moonVisualColor);
                }

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

        /// <summary>
        /// Tạo tự động vòm sao lấp lánh (Star Dome) trên nền trời ban đêm
        /// </summary>
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
            shape.radiusThickness = 0.1f; // Chỉ nằm ở vỏ ngoài của quả cầu

            // Tạo hiệu ứng lấp lánh (Twinkle / Size over Lifetime)
            var sizeOverLifetime = starParticleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 0.8f);
            curve.AddKey(0.25f, 1.3f);
            curve.AddKey(0.5f, 0.7f);
            curve.AddKey(0.75f, 1.2f);
            curve.AddKey(1f, 0.8f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            var renderer = starObj.GetComponent<ParticleSystemRenderer>();
            Shader starShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (starShader == null) starShader = Shader.Find("Particles/Standard Unlit");
            if (starShader == null) starShader = Shader.Find("Unlit/Color");

            Material starMat = new Material(starShader);
            starMat.name = "M_StarSky";
            if (starMat.HasProperty("_BaseColor")) starMat.SetColor("_BaseColor", Color.white);
            if (starMat.HasProperty("_Color")) starMat.SetColor("_Color", Color.white);

            renderer.material = starMat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            // Khởi chạy hệ thống hạt sao
            starParticleSystem.Play();
        }

        /// <summary>
        /// Cho phép thiết lập giờ tùy ý qua code hoặc Event (ví dụ: nhảy tới 12h trưa, 20h tối).
        /// </summary>
        public void SetTime(float hour)
        {
            currentInGameTime = Mathf.Repeat(hour, 24f);
            UpdateLighting();
            UpdateUI();
        }

        /// <summary>
        /// Nhảy sang 06:00 sáng của ngày tiếp theo (phục vụ test).
        /// Kích hoạt event OnDayChanged để các hệ thống (quái, UI...) tự động cập nhật sức mạnh.
        /// </summary>
        public void SkipToNextDayAt6AM()
        {
            currentDay++;
            currentInGameTime = 6f;

            Debug.Log($"<color=cyan>[DayNightController] Chuyển sang 06:00 sáng ngày tiếp theo -> <b>Day {currentDay}</b></color>");

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

        /// <summary>
        /// Nhảy sang thời điểm ban ngày (08:00).
        /// </summary>
        public void SkipToDay()
        {
            SetTime(8f);
        }

        /// <summary>
        /// Nhảy sang thời điểm ban đêm (20:00).
        /// </summary>
        public void SkipToNight()
        {
            SetTime(20f);
        }
    }
}
