using UnityEngine;
using System;
using System.Collections;
using UnityEngine.Networking;

namespace PastGuardians.AR
{
    /// <summary>
    /// Manages GPS location services and privacy-safe location display
    /// </summary>
    public class LocationManager : MonoBehaviour
    {
        public static LocationManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float updateIntervalSeconds = 30f;
        [SerializeField] private float desiredAccuracyMeters = 100f;
        [SerializeField] private float updateDistanceMeters = 50f;
        [SerializeField] private int locationTimeoutSeconds = 20;

        [Header("Fallback")]
        [SerializeField] private string defaultCity = "Unknown City";
        [SerializeField] private string defaultCountry = "Unknown";

        [Header("Debug")]
        [SerializeField] private bool useDebugLocation = false;
        [SerializeField] private double debugLatitude = 40.7128;   // New York
        [SerializeField] private double debugLongitude = -74.0060;
        [SerializeField] private bool debugSimulateIndoors = false;

        [Header("GPS Signal Detection")]
        [SerializeField] private float poorAccuracyThreshold = 100f;  // Meters - above this = poor signal
        [SerializeField] private float signalLostTimeout = 30f;       // Seconds without update = signal lost
        [SerializeField] private float signalCheckInterval = 5f;      // How often to check signal

        // Current location data
        private double currentLatitude;
        private double currentLongitude;
        private float currentAccuracy;
        private string currentCity;
        private string currentCountry;
        private bool locationAvailable;
        private bool locationPermissionGranted;
        private bool isUpdating;

        // GPS signal state
        private bool hasGPSSignal = true;
        private float lastLocationUpdateTime;
        private float lastSignalCheckTime;
        private bool wasIndoors = false;

        // Events
        public event Action<double, double> OnLocationUpdated;
        public event Action<string, string> OnCityCountryUpdated;
        public event Action<string> OnLocationError;
        public event Action OnLocationServicesStarted;
        public event Action OnGPSSignalLost;
        public event Action OnGPSSignalRestored;

        // Properties
        public double Latitude => currentLatitude;
        public double Longitude => currentLongitude;
        public float Accuracy => currentAccuracy;
        public string City => currentCity ?? defaultCity;
        public string Country => currentCountry ?? defaultCountry;
        public bool LocationAvailable => locationAvailable;
        public bool PermissionGranted => locationPermissionGranted;
        public bool HasGPSSignal => hasGPSSignal;
        public bool IsIndoors => !hasGPSSignal;
        public float TimeSinceLastUpdate => Time.time - lastLocationUpdateTime;

        private Coroutine updateCoroutine;

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
            StartCoroutine(InitializeLocationServices());
            lastLocationUpdateTime = Time.time;
        }

        private void Update()
        {
            // Periodically check GPS signal status
            if (Time.time - lastSignalCheckTime > signalCheckInterval)
            {
                lastSignalCheckTime = Time.time;
                CheckGPSSignalStatus();
            }
        }

        /// <summary>
        /// Check GPS signal status and fire events if changed
        /// </summary>
        private void CheckGPSSignalStatus()
        {
#if UNITY_EDITOR
            if (debugSimulateIndoors)
            {
                if (hasGPSSignal)
                {
                    hasGPSSignal = false;
                    wasIndoors = true;
                    OnGPSSignalLost?.Invoke();
                    Debug.Log("[LocationManager] DEBUG: Simulating indoor/no GPS");
                }
                return;
            }
#endif

            if (!locationAvailable) return;

            bool currentlyHasSignal = true;

            // Check 1: Time since last update
            float timeSinceUpdate = Time.time - lastLocationUpdateTime;
            if (timeSinceUpdate > signalLostTimeout)
            {
                currentlyHasSignal = false;
            }

            // Check 2: Accuracy is too poor (common indoors)
            if (currentAccuracy > poorAccuracyThreshold && currentAccuracy > 0)
            {
                currentlyHasSignal = false;
            }

            // Check 3: Location service status
            if (Input.location.status != LocationServiceStatus.Running)
            {
                currentlyHasSignal = false;
            }

            // Detect state changes
            if (hasGPSSignal && !currentlyHasSignal)
            {
                // Signal just lost
                hasGPSSignal = false;
                wasIndoors = true;
                OnGPSSignalLost?.Invoke();
                Debug.Log($"[LocationManager] GPS signal lost (accuracy: {currentAccuracy}m, time since update: {timeSinceUpdate}s)");
            }
            else if (!hasGPSSignal && currentlyHasSignal)
            {
                // Signal just restored
                hasGPSSignal = true;
                wasIndoors = false;
                OnGPSSignalRestored?.Invoke();
                Debug.Log("[LocationManager] GPS signal restored");
            }
        }

        private void OnDestroy()
        {
            StopLocationServices();
        }

        /// <summary>
        /// Initialize location services
        /// </summary>
        private IEnumerator InitializeLocationServices()
        {
#if UNITY_EDITOR
            if (useDebugLocation)
            {
                currentLatitude = debugLatitude;
                currentLongitude = debugLongitude;
                locationAvailable = true;
                locationPermissionGranted = true;
                OnLocationUpdated?.Invoke(currentLatitude, currentLongitude);

                // Get city/country for debug location
                StartCoroutine(ReverseGeocode(currentLatitude, currentLongitude));

                OnLocationServicesStarted?.Invoke();
                Debug.Log($"[LocationManager] Using debug location: {currentLatitude}, {currentLongitude}");
                yield break;
            }
#endif

            // Check if location services are enabled
            if (!Input.location.isEnabledByUser)
            {
                OnLocationError?.Invoke("Location services disabled by user");
                Debug.LogWarning("[LocationManager] Location services disabled by user");
                yield break;
            }

            // Request permission on Android
#if UNITY_ANDROID
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.FineLocation))
            {
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.FineLocation);
                yield return new WaitForSeconds(1f);

                if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.FineLocation))
                {
                    locationPermissionGranted = false;
                    OnLocationError?.Invoke("Location permission denied");
                    Debug.LogWarning("[LocationManager] Location permission denied");
                    yield break;
                }
            }
#endif

            locationPermissionGranted = true;

            // Start location service
            Input.location.Start(desiredAccuracyMeters, updateDistanceMeters);

            // Wait for initialization
            int timeout = locationTimeoutSeconds;
            while (Input.location.status == LocationServiceStatus.Initializing && timeout > 0)
            {
                yield return new WaitForSeconds(1);
                timeout--;
            }

            // Check if timed out
            if (timeout <= 0)
            {
                OnLocationError?.Invoke("Location service timed out");
                Debug.LogWarning("[LocationManager] Location service timed out");
                yield break;
            }

            // Check if failed
            if (Input.location.status == LocationServiceStatus.Failed)
            {
                OnLocationError?.Invoke("Unable to determine location");
                Debug.LogWarning("[LocationManager] Unable to determine location");
                yield break;
            }

            locationAvailable = true;
            OnLocationServicesStarted?.Invoke();
            Debug.Log("[LocationManager] Location services started");

            // Start periodic updates
            updateCoroutine = StartCoroutine(UpdateLocationPeriodically());
        }

        /// <summary>
        /// Periodically update location
        /// </summary>
        private IEnumerator UpdateLocationPeriodically()
        {
            while (true)
            {
                UpdateLocation();
                yield return new WaitForSeconds(updateIntervalSeconds);
            }
        }

        /// <summary>
        /// Update current location
        /// </summary>
        private void UpdateLocation()
        {
            if (!locationAvailable || isUpdating) return;

#if UNITY_EDITOR
            if (useDebugLocation)
            {
                return; // Use debug values
            }
#endif

            if (Input.location.status != LocationServiceStatus.Running)
            {
                Debug.LogWarning("[LocationManager] Location service not running");
                return;
            }

            var location = Input.location.lastData;

            // Always update accuracy and timestamp
            currentAccuracy = location.horizontalAccuracy;
            lastLocationUpdateTime = Time.time;

            // Check if location changed significantly
            double latDiff = Math.Abs(location.latitude - currentLatitude);
            double lonDiff = Math.Abs(location.longitude - currentLongitude);

            if (latDiff > 0.001 || lonDiff > 0.001) // About 100m
            {
                currentLatitude = location.latitude;
                currentLongitude = location.longitude;

                OnLocationUpdated?.Invoke(currentLatitude, currentLongitude);
                Debug.Log($"[LocationManager] Location updated: {currentLatitude:F4}, {currentLongitude:F4} (accuracy: {currentAccuracy}m)");

                // Update city/country
                StartCoroutine(ReverseGeocode(currentLatitude, currentLongitude));
            }
        }

        /// <summary>
        /// Reverse geocode coordinates to city/country
        /// Uses OpenStreetMap Nominatim API (free, no API key required)
        /// </summary>
        private IEnumerator ReverseGeocode(double lat, double lon)
        {
            isUpdating = true;

            string url = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={lat}&lon={lon}&zoom=10&addressdetails=1";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("User-Agent", "PastGuardians/1.0");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        NominatimResponse response = JsonUtility.FromJson<NominatimResponse>(request.downloadHandler.text);

                        if (response != null && response.address != null)
                        {
                            // Get city (try multiple fields as different regions use different ones)
                            currentCity = response.address.city;
                            if (string.IsNullOrEmpty(currentCity))
                                currentCity = response.address.town;
                            if (string.IsNullOrEmpty(currentCity))
                                currentCity = response.address.village;
                            if (string.IsNullOrEmpty(currentCity))
                                currentCity = response.address.municipality;
                            if (string.IsNullOrEmpty(currentCity))
                                currentCity = response.address.county;

                            currentCountry = response.address.country;

                            OnCityCountryUpdated?.Invoke(currentCity, currentCountry);
                            Debug.Log($"[LocationManager] Location: {currentCity}, {currentCountry}");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[LocationManager] Failed to parse geocode response: {e.Message}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[LocationManager] Geocoding failed: {request.error}");
                }
            }

            isUpdating = false;
        }

        /// <summary>
        /// Stop location services
        /// </summary>
        public void StopLocationServices()
        {
            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
                updateCoroutine = null;
            }

            Input.location.Stop();
            locationAvailable = false;
            Debug.Log("[LocationManager] Location services stopped");
        }

        /// <summary>
        /// Restart location services
        /// </summary>
        public void RestartLocationServices()
        {
            StopLocationServices();
            StartCoroutine(InitializeLocationServices());
        }

        /// <summary>
        /// Get city/country string
        /// </summary>
        public string GetCityCountryString()
        {
            string city = string.IsNullOrEmpty(currentCity) ? defaultCity : currentCity;
            string country = string.IsNullOrEmpty(currentCountry) ? defaultCountry : currentCountry;

            return $"{city}, {country}";
        }

        /// <summary>
        /// Calculate distance to another coordinate in kilometers
        /// </summary>
        public float DistanceTo(double lat, double lon)
        {
            return CalculateHaversineDistance(currentLatitude, currentLongitude, lat, lon);
        }

        /// <summary>
        /// Calculate distance between two coordinates using Haversine formula
        /// </summary>
        public static float CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Earth's radius in km

            double dLat = (lat2 - lat1) * Math.PI / 180;
            double dLon = (lon2 - lon1) * Math.PI / 180;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return (float)(R * c);
        }

        /// <summary>
        /// Calculate bearing from current location to target
        /// </summary>
        public float BearingTo(double lat, double lon)
        {
            return CalculateBearing(currentLatitude, currentLongitude, lat, lon);
        }

        /// <summary>
        /// Calculate bearing between two coordinates
        /// </summary>
        public static float CalculateBearing(double lat1, double lon1, double lat2, double lon2)
        {
            double dLon = (lon2 - lon1) * Math.PI / 180;
            double lat1Rad = lat1 * Math.PI / 180;
            double lat2Rad = lat2 * Math.PI / 180;

            double x = Math.Sin(dLon) * Math.Cos(lat2Rad);
            double y = Math.Cos(lat1Rad) * Math.Sin(lat2Rad) -
                       Math.Sin(lat1Rad) * Math.Cos(lat2Rad) * Math.Cos(dLon);

            double bearing = Math.Atan2(x, y) * 180 / Math.PI;

            return (float)((bearing + 360) % 360);
        }

        /// <summary>
        /// Set debug location (editor only)
        /// </summary>
        public void SetDebugLocation(double lat, double lon)
        {
#if UNITY_EDITOR
            debugLatitude = lat;
            debugLongitude = lon;
            currentLatitude = lat;
            currentLongitude = lon;
            OnLocationUpdated?.Invoke(currentLatitude, currentLongitude);
            StartCoroutine(ReverseGeocode(lat, lon));
#endif
        }

        /// <summary>
        /// Get debug info string
        /// </summary>
        public string GetDebugInfo()
        {
            return $"Lat: {currentLatitude:F4}\n" +
                   $"Lon: {currentLongitude:F4}\n" +
                   $"Accuracy: {currentAccuracy:F1}m\n" +
                   $"City: {City}\n" +
                   $"Country: {Country}\n" +
                   $"Available: {locationAvailable}\n" +
                   $"GPS Signal: {(hasGPSSignal ? "Good" : "LOST")}\n" +
                   $"Indoors: {IsIndoors}";
        }

        /// <summary>
        /// Get last known location (useful when GPS is lost)
        /// </summary>
        public (double lat, double lon) GetLastKnownLocation()
        {
            return (currentLatitude, currentLongitude);
        }

        /// <summary>
        /// Check if we have a valid last known location
        /// </summary>
        public bool HasLastKnownLocation()
        {
            return currentLatitude != 0 || currentLongitude != 0;
        }
    }

    /// <summary>
    /// Response structure for Nominatim geocoding API
    /// </summary>
    [Serializable]
    public class NominatimResponse
    {
        public NominatimAddress address;
    }

    [Serializable]
    public class NominatimAddress
    {
        public string city;
        public string town;
        public string village;
        public string municipality;
        public string county;
        public string country;
    }
}
