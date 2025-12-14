#if DEVELOPMENT_BUILD || UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PastGuardians.Data;
using PastGuardians.Gameplay;
using PastGuardians.AR;
using PastGuardians.Core;
using PastGuardians.Network;
using System.Collections.Generic;

namespace PastGuardians.DevTools
{
    /// <summary>
    /// Development debug panel for testing
    /// </summary>
    public class DebugPanel : MonoBehaviour
    {
        public static DebugPanel Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject debugPanelRoot;
        [SerializeField] private TextMeshProUGUI debugInfoText;
        [SerializeField] private Button togglePanelButton;

        [Header("Spawn Controls")]
        [SerializeField] private TMP_Dropdown intruderTypeDropdown;
        [SerializeField] private Button spawnRandomButton;
        [SerializeField] private Button spawnAboveButton;
        [SerializeField] private Slider altitudeSlider;
        [SerializeField] private TextMeshProUGUI altitudeText;

        [Header("Simulation Controls")]
        [SerializeField] private Toggle simulatePlayersToggle;
        [SerializeField] private Slider simTapRateSlider;
        [SerializeField] private TextMeshProUGUI simTapRateText;
        [SerializeField] private Button instantCompleteButton;

        [Header("Data")]
        [SerializeField] private List<IntruderData> availableIntruderTypes;

        [Header("Settings")]
        [SerializeField] private int fingerCountToToggle = 3;
        [SerializeField] private float holdDurationToToggle = 1f;

        // State
        private bool isPanelVisible;
        private float multiTouchStartTime;
        private bool isMultiTouchActive;

        // Update info
        private float infoUpdateInterval = 0.2f;
        private float lastInfoUpdate;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (debugPanelRoot != null)
            {
                debugPanelRoot.SetActive(false);
            }
        }

        private void Start()
        {
            SetupUI();
            PopulateIntruderDropdown();
        }

        private void Update()
        {
            // Check for 3-finger tap to toggle panel
            CheckMultiFingerToggle();

            // Update debug info
            if (isPanelVisible && Time.time - lastInfoUpdate > infoUpdateInterval)
            {
                lastInfoUpdate = Time.time;
                UpdateDebugInfo();
            }

            // Keyboard shortcuts in editor
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.F1))
            {
                TogglePanel();
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                SpawnRandomIntruder();
            }

            if (Input.GetKeyDown(KeyCode.F3))
            {
                SpawnAbovePlayer();
            }

            if (Input.GetKeyDown(KeyCode.F4))
            {
                InstantCompleteTarget();
            }
#endif
        }

        /// <summary>
        /// Setup UI elements
        /// </summary>
        private void SetupUI()
        {
            if (togglePanelButton != null)
            {
                togglePanelButton.onClick.AddListener(TogglePanel);
            }

            if (spawnRandomButton != null)
            {
                spawnRandomButton.onClick.AddListener(SpawnRandomIntruder);
            }

            if (spawnAboveButton != null)
            {
                spawnAboveButton.onClick.AddListener(SpawnAbovePlayer);
            }

            if (instantCompleteButton != null)
            {
                instantCompleteButton.onClick.AddListener(InstantCompleteTarget);
            }

            if (altitudeSlider != null)
            {
                altitudeSlider.onValueChanged.AddListener(OnAltitudeSliderChanged);
                altitudeSlider.minValue = 50f;
                altitudeSlider.maxValue = 1500f;
                altitudeSlider.value = 300f;
            }

            if (simTapRateSlider != null)
            {
                simTapRateSlider.onValueChanged.AddListener(OnSimTapRateChanged);
                simTapRateSlider.minValue = 0f;
                simTapRateSlider.maxValue = 20f;
                simTapRateSlider.value = 5f;
            }

            if (simulatePlayersToggle != null)
            {
                simulatePlayersToggle.onValueChanged.AddListener(OnSimulatePlayersToggled);
            }
        }

        /// <summary>
        /// Populate intruder type dropdown
        /// </summary>
        private void PopulateIntruderDropdown()
        {
            if (intruderTypeDropdown == null) return;

            intruderTypeDropdown.ClearOptions();

            List<string> options = new List<string>();

            // Get from spawner if available
            if (IntruderSpawner.Instance != null)
            {
                // Use reflection or add public accessor
            }

            // Use local list
            foreach (var type in availableIntruderTypes)
            {
                options.Add(type.displayName);
            }

            if (options.Count == 0)
            {
                options.Add("No intruder types loaded");
            }

            intruderTypeDropdown.AddOptions(options);
        }

        /// <summary>
        /// Check for multi-finger toggle
        /// </summary>
        private void CheckMultiFingerToggle()
        {
            if (Input.touchCount >= fingerCountToToggle)
            {
                if (!isMultiTouchActive)
                {
                    isMultiTouchActive = true;
                    multiTouchStartTime = Time.time;
                }
                else if (Time.time - multiTouchStartTime >= holdDurationToToggle)
                {
                    TogglePanel();
                    isMultiTouchActive = false;
                    multiTouchStartTime = 0f;
                }
            }
            else
            {
                isMultiTouchActive = false;
            }
        }

        /// <summary>
        /// Toggle debug panel visibility
        /// </summary>
        public void TogglePanel()
        {
            isPanelVisible = !isPanelVisible;

            if (debugPanelRoot != null)
            {
                debugPanelRoot.SetActive(isPanelVisible);
            }

            if (isPanelVisible)
            {
                UpdateDebugInfo();
            }
        }

        /// <summary>
        /// Update debug info display
        /// </summary>
        private void UpdateDebugInfo()
        {
            if (debugInfoText == null) return;

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            // FPS
            sb.AppendLine($"<b>FPS:</b> {1f / Time.deltaTime:F1}");
            sb.AppendLine();

            // Location
            if (LocationManager.Instance != null)
            {
                sb.AppendLine("<b>=== Location ===</b>");
                sb.AppendLine($"Lat: {LocationManager.Instance.Latitude:F4}");
                sb.AppendLine($"Lon: {LocationManager.Instance.Longitude:F4}");
                sb.AppendLine($"City: {LocationManager.Instance.City}");
                sb.AppendLine($"Country: {LocationManager.Instance.Country}");
                sb.AppendLine();
            }

            // Camera
            if (SkyCameraController.Instance != null)
            {
                sb.AppendLine("<b>=== Camera ===</b>");
                sb.AppendLine($"Pitch: {SkyCameraController.Instance.CurrentPitch:F1}°");
                sb.AppendLine($"Heading: {SkyCameraController.Instance.CurrentCompassHeading:F1}°");
                sb.AppendLine($"Looking at Sky: {SkyCameraController.Instance.IsLookingAtSky}");
                sb.AppendLine();
            }

            // Intruders
            if (IntruderSpawner.Instance != null)
            {
                sb.AppendLine("<b>=== Intruders ===</b>");
                sb.AppendLine($"Active: {IntruderSpawner.Instance.ActiveIntruderCount}");

                foreach (var intruder in IntruderSpawner.Instance.ActiveIntruders)
                {
                    if (intruder == null) continue;
                    sb.AppendLine($"  - {intruder.Data?.displayName ?? "?"}: {intruder.ProgressPercent:P0}");
                }
                sb.AppendLine();
            }

            // Tap Input
            if (TapInputHandler.Instance != null)
            {
                sb.AppendLine("<b>=== Input ===</b>");
                sb.AppendLine($"Target: {TapInputHandler.Instance.CurrentTarget?.Data?.displayName ?? "None"}");
                sb.AppendLine($"Tap Rate: {TapInputHandler.Instance.TapRate:F1}/s");
                sb.AppendLine($"Is Tapping: {TapInputHandler.Instance.IsTapping}");
                sb.AppendLine();
            }

            // Network
            if (NetworkManager.Instance != null)
            {
                sb.AppendLine("<b>=== Network ===</b>");
                sb.AppendLine($"State: {NetworkManager.Instance.State}");
                sb.AppendLine($"Offline Mode: {NetworkManager.Instance.IsOfflineMode}");
                sb.AppendLine();
            }

            // Player
            if (PlayerDataManager.Instance?.Data != null)
            {
                var data = PlayerDataManager.Instance.Data;
                sb.AppendLine("<b>=== Player ===</b>");
                sb.AppendLine($"Rank: {PlayerRank.GetRankTitle(data.currentRank)}");
                sb.AppendLine($"XP: {data.currentXP:N0}");
                sb.AppendLine($"Total Returns: {data.totalReturns}");
                sb.AppendLine();
            }

            // Mock Server
            if (MockServer.Instance != null)
            {
                sb.AppendLine("<b>=== Mock Server ===</b>");
                sb.AppendLine($"Running: {MockServer.Instance.IsRunning}");
                sb.AppendLine($"Sim Players: {MockServer.Instance.SimulatedPlayerCount}");
            }

            debugInfoText.text = sb.ToString();
        }

        /// <summary>
        /// Spawn random intruder
        /// </summary>
        public void SpawnRandomIntruder()
        {
            if (IntruderSpawner.Instance != null)
            {
                IntruderSpawner.Instance.SpawnRandomIntruder();
            }
            else
            {
                UnityEngine.Debug.LogWarning("[DebugPanel] IntruderSpawner not found");
            }
        }

        /// <summary>
        /// Spawn intruder directly above player
        /// </summary>
        public void SpawnAbovePlayer()
        {
            if (IntruderSpawner.Instance == null || LocationManager.Instance == null)
            {
                UnityEngine.Debug.LogWarning("[DebugPanel] Required managers not found");
                return;
            }

            IntruderData type = GetSelectedIntruderType();
            if (type == null)
            {
                UnityEngine.Debug.LogWarning("[DebugPanel] No intruder type selected");
                return;
            }

            float altitude = altitudeSlider?.value ?? 300f;
            double lat = LocationManager.Instance.Latitude;
            double lon = LocationManager.Instance.Longitude;

            IntruderSpawner.Instance.SpawnIntruder(type, lat, lon, altitude);
        }

        /// <summary>
        /// Get selected intruder type from dropdown
        /// </summary>
        private IntruderData GetSelectedIntruderType()
        {
            if (intruderTypeDropdown == null || availableIntruderTypes.Count == 0)
                return null;

            int index = intruderTypeDropdown.value;
            if (index >= 0 && index < availableIntruderTypes.Count)
            {
                return availableIntruderTypes[index];
            }

            return null;
        }

        /// <summary>
        /// Instantly complete current target
        /// </summary>
        public void InstantCompleteTarget()
        {
            if (TapInputHandler.Instance?.CurrentTarget != null)
            {
                var target = TapInputHandler.Instance.CurrentTarget;
                int remaining = target.TotalTapsRequired - target.TapProgress;
                target.RegisterTap(remaining);
            }
            else if (IntruderSpawner.Instance?.ActiveIntruderCount > 0)
            {
                var intruder = IntruderSpawner.Instance.ActiveIntruders[0];
                int remaining = intruder.TotalTapsRequired - intruder.TapProgress;
                intruder.RegisterTap(remaining);
            }
        }

        /// <summary>
        /// Handle altitude slider change
        /// </summary>
        private void OnAltitudeSliderChanged(float value)
        {
            if (altitudeText != null)
            {
                altitudeText.text = $"Altitude: {value:F0}m";
            }
        }

        /// <summary>
        /// Handle sim tap rate change
        /// </summary>
        private void OnSimTapRateChanged(float value)
        {
            if (simTapRateText != null)
            {
                simTapRateText.text = $"Tap Rate: {value:F1}/s";
            }

            if (MockServer.Instance != null)
            {
                MockServer.Instance.SetSimulatedTapRate(value);
            }
        }

        /// <summary>
        /// Handle simulate players toggle
        /// </summary>
        private void OnSimulatePlayersToggled(bool enabled)
        {
            if (MockServer.Instance != null)
            {
                if (enabled)
                {
                    MockServer.Instance.StartSimulation();
                }
                else
                {
                    MockServer.Instance.StopSimulation();
                }
            }
        }

        /// <summary>
        /// Add XP for testing
        /// </summary>
        public void AddTestXP(int amount = 100)
        {
            PlayerDataManager.Instance?.AddXP(amount, "Debug");
        }

        /// <summary>
        /// Reset player data
        /// </summary>
        public void ResetPlayerData()
        {
            PlayerDataManager.Instance?.ResetPlayerData();
        }

        /// <summary>
        /// Show hitboxes on intruders
        /// </summary>
        public void ToggleHitboxes(bool show)
        {
            // Could implement hitbox visualization here
        }
    }
}
#endif
