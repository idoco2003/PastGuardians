using UnityEngine;
using System;

namespace PastGuardians.AR
{
    /// <summary>
    /// Handles camera orientation detection for sky-based AR gameplay
    /// </summary>
    public class SkyCameraController : MonoBehaviour
    {
        public static SkyCameraController Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float skyViewThreshold = 30f;  // Degrees above horizon
        [SerializeField] private float compassSmoothTime = 0.2f;
        [SerializeField] private float pitchSmoothTime = 0.1f;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;

        // Current state
        private float currentPitch;
        private float currentCompassHeading;
        private float smoothedPitch;
        private float smoothedCompassHeading;
        private bool isLookingAtSky;
        private float pitchVelocity;
        private float compassVelocity;

        // Sensor availability
        private bool gyroAvailable;
        private bool compassAvailable;

        // Events
        public event Action OnStartedLookingAtSky;
        public event Action OnStoppedLookingAtSky;
        public event Action<float> OnPitchChanged;
        public event Action<float> OnHeadingChanged;

        // Properties
        public float CurrentPitch => smoothedPitch;
        public float CurrentCompassHeading => smoothedCompassHeading;
        public bool IsLookingAtSky => isLookingAtSky;
        public float SkyViewThreshold => skyViewThreshold;
        public bool GyroAvailable => gyroAvailable;
        public bool CompassAvailable => compassAvailable;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            InitializeSensors();
        }

        private void Update()
        {
            UpdateSensorReadings();
            UpdateSkyLookingState();
        }

        /// <summary>
        /// Initialize device sensors
        /// </summary>
        private void InitializeSensors()
        {
            // Enable gyroscope
            if (SystemInfo.supportsGyroscope)
            {
                Input.gyro.enabled = true;
                gyroAvailable = true;
                Debug.Log("[SkyCameraController] Gyroscope enabled");
            }
            else
            {
                gyroAvailable = false;
                Debug.LogWarning("[SkyCameraController] Gyroscope not available");
            }

            // Enable compass
            if (Input.compass.enabled || SystemInfo.supportsLocationService)
            {
                Input.compass.enabled = true;
                compassAvailable = true;
                Debug.Log("[SkyCameraController] Compass enabled");
            }
            else
            {
                compassAvailable = false;
                Debug.LogWarning("[SkyCameraController] Compass not available");
            }
        }

        /// <summary>
        /// Update sensor readings
        /// </summary>
        private void UpdateSensorReadings()
        {
            // Update pitch (device tilt)
            UpdatePitch();

            // Update compass heading
            UpdateCompassHeading();
        }

        /// <summary>
        /// Calculate device pitch angle (0 = horizontal, 90 = pointing up)
        /// </summary>
        private void UpdatePitch()
        {
            if (gyroAvailable)
            {
                // Use gyroscope attitude
                Quaternion attitude = Input.gyro.attitude;

                // Convert gyro attitude to Unity coordinate system
                Quaternion rotFix = new Quaternion(attitude.x, attitude.y, -attitude.z, -attitude.w);

                // Get forward vector and calculate pitch
                Vector3 forward = rotFix * Vector3.forward;
                currentPitch = Mathf.Asin(forward.y) * Mathf.Rad2Deg;
            }
            else
            {
                // Fallback to accelerometer
                Vector3 acceleration = Input.acceleration;
                currentPitch = Mathf.Asin(Mathf.Clamp(acceleration.z, -1f, 1f)) * Mathf.Rad2Deg;

                // Invert because accelerometer reports opposite when tilting up
                currentPitch = -currentPitch;
            }

            // Clamp pitch to valid range
            currentPitch = Mathf.Clamp(currentPitch, -90f, 90f);

            // Smooth the pitch
            smoothedPitch = Mathf.SmoothDamp(smoothedPitch, currentPitch, ref pitchVelocity, pitchSmoothTime);

            OnPitchChanged?.Invoke(smoothedPitch);
        }

        /// <summary>
        /// Update compass heading (0-360, 0 = North)
        /// </summary>
        private void UpdateCompassHeading()
        {
            if (compassAvailable && Input.compass.enabled)
            {
                currentCompassHeading = Input.compass.trueHeading;

                // Handle wrap-around smoothing (359 -> 0)
                float delta = currentCompassHeading - smoothedCompassHeading;
                if (delta > 180f) delta -= 360f;
                if (delta < -180f) delta += 360f;

                float targetHeading = smoothedCompassHeading + delta;
                smoothedCompassHeading = Mathf.SmoothDamp(smoothedCompassHeading, targetHeading, ref compassVelocity, compassSmoothTime);

                // Normalize to 0-360
                if (smoothedCompassHeading < 0f) smoothedCompassHeading += 360f;
                if (smoothedCompassHeading >= 360f) smoothedCompassHeading -= 360f;

                OnHeadingChanged?.Invoke(smoothedCompassHeading);
            }

#if UNITY_EDITOR
            // Editor simulation: use mouse for compass
            if (Input.GetKey(KeyCode.LeftShift))
            {
                currentCompassHeading += Input.GetAxis("Mouse X") * 50f;
                if (currentCompassHeading < 0f) currentCompassHeading += 360f;
                if (currentCompassHeading >= 360f) currentCompassHeading -= 360f;
                smoothedCompassHeading = currentCompassHeading;
            }

            // Editor simulation: use mouse Y for pitch
            if (Input.GetKey(KeyCode.LeftControl))
            {
                currentPitch += Input.GetAxis("Mouse Y") * 30f;
                currentPitch = Mathf.Clamp(currentPitch, -90f, 90f);
                smoothedPitch = currentPitch;
            }
#endif
        }

        /// <summary>
        /// Update sky looking state
        /// </summary>
        private void UpdateSkyLookingState()
        {
            bool wasLookingAtSky = isLookingAtSky;
            isLookingAtSky = smoothedPitch > skyViewThreshold;

            if (isLookingAtSky && !wasLookingAtSky)
            {
                OnStartedLookingAtSky?.Invoke();
                Debug.Log("[SkyCameraController] Started looking at sky");
            }
            else if (!isLookingAtSky && wasLookingAtSky)
            {
                OnStoppedLookingAtSky?.Invoke();
                Debug.Log("[SkyCameraController] Stopped looking at sky");
            }
        }

        /// <summary>
        /// Get direction vector for current heading
        /// </summary>
        public Vector3 GetHeadingDirection()
        {
            float rad = smoothedCompassHeading * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
        }

        /// <summary>
        /// Get relative angle from current heading to a target bearing
        /// </summary>
        public float GetRelativeAngle(float targetBearing)
        {
            float relative = targetBearing - smoothedCompassHeading;
            if (relative > 180f) relative -= 360f;
            if (relative < -180f) relative += 360f;
            return relative;
        }

        /// <summary>
        /// Check if a bearing is within the current field of view
        /// </summary>
        public bool IsBearingInView(float bearing, float fovAngle = 60f)
        {
            float relative = Mathf.Abs(GetRelativeAngle(bearing));
            return relative <= fovAngle / 2f;
        }

        /// <summary>
        /// Set sky view threshold
        /// </summary>
        public void SetSkyViewThreshold(float threshold)
        {
            skyViewThreshold = Mathf.Clamp(threshold, 10f, 80f);
        }

        /// <summary>
        /// Get compass direction string (N, NE, E, etc.)
        /// </summary>
        public string GetCompassDirectionString()
        {
            float heading = smoothedCompassHeading;

            if (heading >= 337.5f || heading < 22.5f) return "N";
            if (heading >= 22.5f && heading < 67.5f) return "NE";
            if (heading >= 67.5f && heading < 112.5f) return "E";
            if (heading >= 112.5f && heading < 157.5f) return "SE";
            if (heading >= 157.5f && heading < 202.5f) return "S";
            if (heading >= 202.5f && heading < 247.5f) return "SW";
            if (heading >= 247.5f && heading < 292.5f) return "W";
            if (heading >= 292.5f && heading < 337.5f) return "NW";

            return "N";
        }

        /// <summary>
        /// Get debug info string
        /// </summary>
        public string GetDebugInfo()
        {
            return $"Pitch: {smoothedPitch:F1}°\n" +
                   $"Heading: {smoothedCompassHeading:F1}° ({GetCompassDirectionString()})\n" +
                   $"Looking at Sky: {isLookingAtSky}\n" +
                   $"Gyro: {(gyroAvailable ? "Available" : "N/A")}\n" +
                   $"Compass: {(compassAvailable ? "Available" : "N/A")}";
        }

        private void OnGUI()
        {
            if (!showDebugInfo) return;

            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                normal = { textColor = Color.white }
            };

            GUI.Label(new Rect(10, 10, 400, 200), GetDebugInfo(), style);
        }
    }
}
