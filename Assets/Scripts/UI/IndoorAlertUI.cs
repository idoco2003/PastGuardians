using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PastGuardians.AR;
using PastGuardians.Gameplay;
using System;

namespace PastGuardians.UI
{
    /// <summary>
    /// Alert UI shown when player is indoors without GPS signal
    /// Notifies about nearby intruders and guides them outside
    /// </summary>
    public class IndoorAlertUI : MonoBehaviour
    {
        public static IndoorAlertUI Instance { get; private set; }

        [Header("Main Panel")]
        [SerializeField] private GameObject alertPanel;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Alert Content")]
        [SerializeField] private TextMeshProUGUI alertTitleText;
        [SerializeField] private TextMeshProUGUI alertMessageText;
        [SerializeField] private TextMeshProUGUI directionText;
        [SerializeField] private TextMeshProUGUI distanceText;

        [Header("Direction Indicator")]
        [SerializeField] private RectTransform directionArrow;
        [SerializeField] private Image compassRing;

        [Header("Intruder Preview")]
        [SerializeField] private Image intruderIcon;
        [SerializeField] private TextMeshProUGUI intruderNameText;
        [SerializeField] private TextMeshProUGUI intruderCountText;

        [Header("Buttons")]
        [SerializeField] private Button goOutsideButton;
        [SerializeField] private Button dismissButton;

        [Header("Animation")]
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseAmount = 0.1f;

        [Header("Settings")]
        [SerializeField] private float checkInterval = 5f;
        [SerializeField] private float minAlertInterval = 30f;  // Don't spam alerts

        // State
        private bool isShowing;
        private float lastAlertTime;
        private float lastCheckTime;
        private Intruder nearestIntruder;
        private float intruderBearing;
        private float intruderDistance;

        // Cached last known location
        private double lastKnownLat;
        private double lastKnownLon;
        private bool hasLastKnownLocation;

        // Events
        public event Action OnGoOutsideClicked;
        public event Action OnAlertDismissed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (alertPanel != null)
            {
                alertPanel.SetActive(false);
            }
        }

        private void Start()
        {
            // Setup buttons
            if (goOutsideButton != null) goOutsideButton.onClick.AddListener(OnGoOutside);
            if (dismissButton != null) dismissButton.onClick.AddListener(Dismiss);

            // Subscribe to location events
            if (LocationManager.Instance != null)
            {
                LocationManager.Instance.OnLocationUpdated += HandleLocationUpdated;
                LocationManager.Instance.OnGPSSignalLost += HandleGPSSignalLost;
                LocationManager.Instance.OnGPSSignalRestored += HandleGPSSignalRestored;
            }
        }

        private void OnDestroy()
        {
            if (LocationManager.Instance != null)
            {
                LocationManager.Instance.OnLocationUpdated -= HandleLocationUpdated;
                LocationManager.Instance.OnGPSSignalLost -= HandleGPSSignalLost;
                LocationManager.Instance.OnGPSSignalRestored -= HandleGPSSignalRestored;
            }
        }

        private void Update()
        {
            // Check for nearby intruders periodically when indoors
            if (!LocationManager.Instance?.HasGPSSignal ?? false)
            {
                if (Time.time - lastCheckTime > checkInterval)
                {
                    lastCheckTime = Time.time;
                    CheckForNearbyIntruders();
                }
            }

            // Animate direction arrow
            if (isShowing && directionArrow != null)
            {
                UpdateDirectionArrow();
                AnimatePulse();
            }
        }

        /// <summary>
        /// Handle location update - cache last known position
        /// </summary>
        private void HandleLocationUpdated(double lat, double lon)
        {
            lastKnownLat = lat;
            lastKnownLon = lon;
            hasLastKnownLocation = true;
        }

        /// <summary>
        /// Handle GPS signal lost
        /// </summary>
        private void HandleGPSSignalLost()
        {
            Debug.Log("[IndoorAlert] GPS signal lost - player may be indoors");
            CheckForNearbyIntruders();
        }

        /// <summary>
        /// Handle GPS signal restored
        /// </summary>
        private void HandleGPSSignalRestored()
        {
            Debug.Log("[IndoorAlert] GPS signal restored");
            Hide();
        }

        /// <summary>
        /// Check for intruders near last known location
        /// </summary>
        private void CheckForNearbyIntruders()
        {
            if (!hasLastKnownLocation) return;
            if (IntruderSpawner.Instance == null) return;

            // Don't alert too frequently
            if (Time.time - lastAlertTime < minAlertInterval) return;

            // Find nearest intruder to last known location
            float nearestDistance = float.MaxValue;
            Intruder nearest = null;

            foreach (var intruder in IntruderSpawner.Instance.ActiveIntruders)
            {
                if (intruder == null) continue;

                float distance = LocationManager.CalculateHaversineDistance(
                    lastKnownLat, lastKnownLon,
                    intruder.Latitude, intruder.Longitude
                );

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = intruder;
                }
            }

            // If there's an intruder within reasonable range, show alert
            float alertRange = 100f;  // 100km range for alerts
            if (nearest != null && nearestDistance < alertRange)
            {
                float bearing = LocationManager.CalculateBearing(
                    lastKnownLat, lastKnownLon,
                    nearest.Latitude, nearest.Longitude
                );

                ShowAlert(nearest, nearestDistance, bearing);
            }
        }

        /// <summary>
        /// Show the indoor alert
        /// </summary>
        public void ShowAlert(Intruder intruder, float distanceKm, float bearing)
        {
            nearestIntruder = intruder;
            intruderDistance = distanceKm;
            intruderBearing = bearing;
            isShowing = true;
            lastAlertTime = Time.time;

            if (alertPanel != null)
            {
                alertPanel.SetActive(true);
            }

            UpdateAlertContent();
            Debug.Log($"[IndoorAlert] Showing alert for {intruder?.Data?.displayName} at {distanceKm:F1}km, bearing {bearing:F0}°");
        }

        /// <summary>
        /// Update alert content
        /// </summary>
        private void UpdateAlertContent()
        {
            // Title
            if (alertTitleText != null)
            {
                alertTitleText.text = "INTRUDER DETECTED!";
            }

            // Message
            if (alertMessageText != null)
            {
                alertMessageText.text = "GPS signal weak. Go outside to engage the intruder!";
            }

            // Direction
            if (directionText != null)
            {
                string direction = GetCardinalDirection(intruderBearing);
                directionText.text = $"Approximately {direction}";
            }

            // Distance
            if (distanceText != null)
            {
                if (intruderDistance < 1f)
                {
                    distanceText.text = $"~{intruderDistance * 1000:F0}m away";
                }
                else
                {
                    distanceText.text = $"~{intruderDistance:F1}km away";
                }
            }

            // Intruder info
            if (nearestIntruder?.Data != null)
            {
                if (intruderNameText != null)
                {
                    intruderNameText.text = nearestIntruder.Data.displayName;
                }

                if (intruderIcon != null)
                {
                    intruderIcon.color = nearestIntruder.Data.GetEraPortalColor();
                }
            }

            // Count of total intruders
            if (intruderCountText != null)
            {
                int count = IntruderSpawner.Instance?.ActiveIntruderCount ?? 0;
                if (count > 1)
                {
                    intruderCountText.text = $"+{count - 1} more nearby";
                }
                else
                {
                    intruderCountText.text = "";
                }
            }
        }

        /// <summary>
        /// Update direction arrow rotation
        /// </summary>
        private void UpdateDirectionArrow()
        {
            if (directionArrow == null) return;

            // Rotate arrow to point toward intruder
            // Adjust for device orientation if compass available
            float rotation = -intruderBearing;  // Negative because UI rotates opposite

            if (SkyCameraController.Instance != null)
            {
                float heading = SkyCameraController.Instance.CurrentCompassHeading;
                rotation = -(intruderBearing - heading);
            }

            directionArrow.rotation = Quaternion.Euler(0, 0, rotation);
        }

        /// <summary>
        /// Animate pulse effect
        /// </summary>
        private void AnimatePulse()
        {
            if (compassRing == null) return;

            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            compassRing.transform.localScale = Vector3.one * pulse;
        }

        /// <summary>
        /// Get cardinal direction from bearing
        /// </summary>
        private string GetCardinalDirection(float bearing)
        {
            // Normalize to 0-360
            bearing = (bearing % 360 + 360) % 360;

            if (bearing >= 337.5f || bearing < 22.5f) return "North";
            if (bearing >= 22.5f && bearing < 67.5f) return "Northeast";
            if (bearing >= 67.5f && bearing < 112.5f) return "East";
            if (bearing >= 112.5f && bearing < 157.5f) return "Southeast";
            if (bearing >= 157.5f && bearing < 202.5f) return "South";
            if (bearing >= 202.5f && bearing < 247.5f) return "Southwest";
            if (bearing >= 247.5f && bearing < 292.5f) return "West";
            if (bearing >= 292.5f && bearing < 337.5f) return "Northwest";

            return "Unknown";
        }

        /// <summary>
        /// Get approximate lat/lon description
        /// </summary>
        public string GetApproximateLocationDescription()
        {
            if (nearestIntruder == null) return "Unknown location";

            double lat = nearestIntruder.Latitude;
            double lon = nearestIntruder.Longitude;

            string latDir = lat >= 0 ? "N" : "S";
            string lonDir = lon >= 0 ? "E" : "W";

            // Round to 1 decimal for approximate location
            return $"{Mathf.Abs((float)lat):F1}°{latDir}, {Mathf.Abs((float)lon):F1}°{lonDir}";
        }

        /// <summary>
        /// Handle go outside button
        /// </summary>
        private void OnGoOutside()
        {
            OnGoOutsideClicked?.Invoke();

            // Keep alert visible but update message
            if (alertMessageText != null)
            {
                alertMessageText.text = "Head outside and look up at the sky!";
            }
        }

        /// <summary>
        /// Dismiss the alert
        /// </summary>
        public void Dismiss()
        {
            Hide();
            OnAlertDismissed?.Invoke();
        }

        /// <summary>
        /// Hide the alert panel
        /// </summary>
        public void Hide()
        {
            isShowing = false;

            if (alertPanel != null)
            {
                alertPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Check if alert is showing
        /// </summary>
        public bool IsShowing => isShowing;

        /// <summary>
        /// Force check for intruders (can be called externally)
        /// </summary>
        public void ForceCheck()
        {
            lastAlertTime = 0;  // Reset cooldown
            CheckForNearbyIntruders();
        }
    }
}
