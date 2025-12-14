using UnityEngine;
using PastGuardians.AR;
using PastGuardians.Data;
using System;

namespace PastGuardians.Gameplay
{
    /// <summary>
    /// Converts world coordinates to screen positions for AR display
    /// </summary>
    public class IntruderScreenPositioner : MonoBehaviour
    {
        public static IntruderScreenPositioner Instance { get; private set; }

        [Header("References")]
        [SerializeField] private Camera arCamera;
        [SerializeField] private GameConfig gameConfig;

        [Header("Field of View")]
        [SerializeField] private float horizontalFOV = 60f;
        [SerializeField] private float verticalFOV = 90f;

        [Header("Display Settings")]
        [SerializeField] private float minScale = 0.6f;
        [SerializeField] private float maxScale = 12f;
        [SerializeField] private float screenPadding = 0.05f;  // 5% padding from edges

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
            if (arCamera == null)
            {
                arCamera = Camera.main;
            }
        }

        private void LateUpdate()
        {
            UpdateAllIntruderPositions();
        }

        /// <summary>
        /// Update screen positions for all active intruders
        /// </summary>
        private void UpdateAllIntruderPositions()
        {
            if (IntruderSpawner.Instance == null) return;

            foreach (var intruder in IntruderSpawner.Instance.ActiveIntruders)
            {
                if (intruder == null) continue;

                UpdateIntruderPosition(intruder);
            }
        }

        /// <summary>
        /// Update screen position for a single intruder
        /// </summary>
        public void UpdateIntruderPosition(Intruder intruder)
        {
            if (intruder == null || LocationManager.Instance == null || SkyCameraController.Instance == null)
                return;

            // Calculate distance and bearing from player
            float distance = LocationManager.Instance.DistanceTo(intruder.Latitude, intruder.Longitude);
            float bearing = LocationManager.Instance.BearingTo(intruder.Latitude, intruder.Longitude);

            // Get relative angle from current heading
            float relativeAngle = SkyCameraController.Instance.GetRelativeAngle(bearing);

            // Calculate elevation angle based on altitude and distance
            float elevationAngle = CalculateElevationAngle(intruder.Altitude, distance);

            // Get current camera pitch
            float cameraPitch = SkyCameraController.Instance.CurrentPitch;

            // Check if intruder is visible
            bool isVisible = IsIntruderVisible(relativeAngle, elevationAngle, cameraPitch);

            // Convert to screen position
            Vector2 screenPos = AnglesToScreenPosition(relativeAngle, elevationAngle, cameraPitch);

            // Calculate scale based on distance and altitude
            float scale = CalculateScale(distance, intruder.Altitude, intruder.Data);

            // Update the intruder
            intruder.UpdateScreenPosition(screenPos, scale, isVisible, distance);
        }

        /// <summary>
        /// Calculate elevation angle to intruder
        /// </summary>
        private float CalculateElevationAngle(float altitude, float distanceKm)
        {
            // Convert distance to meters
            float distanceM = distanceKm * 1000f;

            // Calculate angle (accounting for Earth's curvature at large distances is ignored for simplicity)
            float angle = Mathf.Atan2(altitude, distanceM) * Mathf.Rad2Deg;

            return angle;
        }

        /// <summary>
        /// Check if intruder is within visible FOV
        /// </summary>
        private bool IsIntruderVisible(float relativeAngle, float elevationAngle, float cameraPitch)
        {
            // Check horizontal bounds
            if (Mathf.Abs(relativeAngle) > horizontalFOV / 2f)
                return false;

            // Check if looking at sky
            if (!SkyCameraController.Instance.IsLookingAtSky)
                return false;

            // Calculate apparent elevation relative to camera pitch
            float apparentElevation = elevationAngle - (90f - cameraPitch);

            // Check vertical bounds (more permissive when looking up)
            if (apparentElevation < -verticalFOV / 2f || apparentElevation > verticalFOV / 2f)
                return false;

            return true;
        }

        /// <summary>
        /// Convert angles to screen position
        /// </summary>
        private Vector2 AnglesToScreenPosition(float relativeAngle, float elevationAngle, float cameraPitch)
        {
            // Normalize relative angle to -1 to 1
            float normalizedX = relativeAngle / (horizontalFOV / 2f);

            // Calculate apparent elevation relative to camera center
            float apparentElevation = elevationAngle - (90f - cameraPitch);
            float normalizedY = apparentElevation / (verticalFOV / 2f);

            // Apply padding
            float paddedWidth = 1f - (2f * screenPadding);
            float paddedHeight = 1f - (2f * screenPadding);

            // Convert to screen coordinates (0-1)
            float screenX = 0.5f + (normalizedX * paddedWidth * 0.5f);
            float screenY = 0.5f + (normalizedY * paddedHeight * 0.5f);

            // Clamp to screen
            screenX = Mathf.Clamp01(screenX);
            screenY = Mathf.Clamp01(screenY);

            // Convert to pixel coordinates
            return new Vector2(screenX * Screen.width, screenY * Screen.height);
        }

        /// <summary>
        /// Calculate visual scale for intruder
        /// </summary>
        private float CalculateScale(float distanceKm, float altitude, IntruderData data)
        {
            if (gameConfig == null) return 1f;

            // Get base scale from distance tier
            float distanceScale = gameConfig.GetSizeMultiplierForDistance(distanceKm);

            // Adjust for altitude (higher = smaller, but inversely - closer = bigger)
            // At 1000m (close): scale = 2.0, at 10000m (far): scale = 0.5
            float altitudeScale = Mathf.Lerp(2f, 0.5f, Mathf.Clamp01((altitude - 1000f) / 9000f));

            // Get size class multiplier - doubled for better visibility
            float sizeMultiplier = data?.sizeClass switch
            {
                IntruderSize.Tiny => 2f,
                IntruderSize.Small => 3f,
                IntruderSize.Medium => 4f,
                IntruderSize.Large => 6f,
                IntruderSize.Boss => 8f,
                _ => 4f
            };

            float finalScale = distanceScale * altitudeScale * sizeMultiplier;
            return Mathf.Clamp(finalScale, minScale, maxScale);
        }

        /// <summary>
        /// Convert world position to screen position
        /// </summary>
        public Vector2 WorldToScreen(double lat, double lon, float altitude)
        {
            if (LocationManager.Instance == null || SkyCameraController.Instance == null)
                return Vector2.zero;

            float distance = LocationManager.Instance.DistanceTo(lat, lon);
            float bearing = LocationManager.Instance.BearingTo(lat, lon);
            float relativeAngle = SkyCameraController.Instance.GetRelativeAngle(bearing);
            float elevationAngle = CalculateElevationAngle(altitude, distance);
            float cameraPitch = SkyCameraController.Instance.CurrentPitch;

            return AnglesToScreenPosition(relativeAngle, elevationAngle, cameraPitch);
        }

        /// <summary>
        /// Calculate bearing between two coordinates
        /// </summary>
        public static float CalculateBearing(double lat1, double lon1, double lat2, double lon2)
        {
            return LocationManager.CalculateBearing(lat1, lon1, lat2, lon2);
        }

        /// <summary>
        /// Calculate distance between two coordinates (Haversine)
        /// </summary>
        public static float CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            return LocationManager.CalculateHaversineDistance(lat1, lon1, lat2, lon2);
        }

        /// <summary>
        /// Get scale for given distance
        /// </summary>
        public float GetScaleForDistance(float distanceKm, float altitude)
        {
            return CalculateScale(distanceKm, altitude, null);
        }

        /// <summary>
        /// Check if a coordinate is in view
        /// </summary>
        public bool IsCoordinateInView(double lat, double lon, float altitude)
        {
            if (LocationManager.Instance == null || SkyCameraController.Instance == null)
                return false;

            float distance = LocationManager.Instance.DistanceTo(lat, lon);
            float bearing = LocationManager.Instance.BearingTo(lat, lon);
            float relativeAngle = SkyCameraController.Instance.GetRelativeAngle(bearing);
            float elevationAngle = CalculateElevationAngle(altitude, distance);
            float cameraPitch = SkyCameraController.Instance.CurrentPitch;

            return IsIntruderVisible(relativeAngle, elevationAngle, cameraPitch);
        }

        /// <summary>
        /// Get debug info
        /// </summary>
        public string GetDebugInfo()
        {
            return $"H-FOV: {horizontalFOV}°\n" +
                   $"V-FOV: {verticalFOV}°\n" +
                   $"Screen: {Screen.width}x{Screen.height}";
        }
    }
}
