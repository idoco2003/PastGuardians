using UnityEngine;
using PastGuardians.Data;
using PastGuardians.Gameplay;
using PastGuardians.Core;
using PastGuardians.AR;
using System;
using System.Collections;
using System.Collections.Generic;

namespace PastGuardians.Network
{
    /// <summary>
    /// Connection state
    /// </summary>
    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        Reconnecting,
        Error
    }

    /// <summary>
    /// Singleton manager for all server communication
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private string serverUrl = "wss://api.pastguardians.game";
        [SerializeField] private float heartbeatInterval = 30f;
        [SerializeField] private float reconnectDelay = 5f;
        [SerializeField] private int maxReconnectAttempts = 5;

        [Header("Offline Mode")]
        [SerializeField] private bool offlineMode = true;  // Start in offline mode for development

        [Header("Update Intervals")]
        [SerializeField] private float intruderSyncInterval = 0.5f;
        [SerializeField] private float laserSyncInterval = 0.1f;

        // State
        private ConnectionState connectionState = ConnectionState.Disconnected;
        private int reconnectAttempts;
        private float lastHeartbeat;
        private string currentSubscribedIntruderId;

        // Events
        public event Action<List<IntruderNetworkData>> OnIntruderListReceived;
        public event Action<string, int, List<RemoteLaserData>> OnIntruderProgressUpdated;
        public event Action<IntruderReturnedMessage> OnIntruderReturned;
        public event Action<ConnectionState> OnConnectionStateChanged;

        // Properties
        public ConnectionState State => connectionState;
        public bool IsConnected => connectionState == ConnectionState.Connected || offlineMode;
        public bool IsOfflineMode => offlineMode;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (!offlineMode)
            {
                Connect();
            }
            else
            {
                Debug.Log("[NetworkManager] Running in offline mode");
                SetConnectionState(ConnectionState.Connected);
            }

            // Subscribe to tap events
            if (TapInputHandler.Instance != null)
            {
                TapInputHandler.Instance.OnTapRegistered += HandleLocalTap;
            }
        }

        private void OnDestroy()
        {
            if (TapInputHandler.Instance != null)
            {
                TapInputHandler.Instance.OnTapRegistered -= HandleLocalTap;
            }

            Disconnect();
        }

        private void Update()
        {
            // Heartbeat
            if (IsConnected && !offlineMode)
            {
                if (Time.time - lastHeartbeat > heartbeatInterval)
                {
                    SendHeartbeat();
                    lastHeartbeat = Time.time;
                }
            }
        }

        /// <summary>
        /// Connect to server
        /// </summary>
        public void Connect()
        {
            if (offlineMode)
            {
                SetConnectionState(ConnectionState.Connected);
                return;
            }

            if (connectionState == ConnectionState.Connected || connectionState == ConnectionState.Connecting)
                return;

            StartCoroutine(ConnectCoroutine());
        }

        /// <summary>
        /// Connection coroutine
        /// </summary>
        private IEnumerator ConnectCoroutine()
        {
            SetConnectionState(ConnectionState.Connecting);
            Debug.Log($"[NetworkManager] Connecting to {serverUrl}...");

            // In a real implementation, this would:
            // 1. Create WebSocket connection
            // 2. Authenticate with anonymous ID
            // 3. Subscribe to relevant channels

            // Simulated connection delay
            yield return new WaitForSeconds(1f);

            // For now, simulate successful connection
            SetConnectionState(ConnectionState.Connected);
            reconnectAttempts = 0;
            Debug.Log("[NetworkManager] Connected to server");
        }

        /// <summary>
        /// Disconnect from server
        /// </summary>
        public void Disconnect()
        {
            if (connectionState == ConnectionState.Disconnected)
                return;

            // In a real implementation, this would close the WebSocket

            SetConnectionState(ConnectionState.Disconnected);
            Debug.Log("[NetworkManager] Disconnected from server");
        }

        /// <summary>
        /// Send tap to server
        /// </summary>
        public void SendTap(string intruderId, string playerId, string city, string country)
        {
            if (offlineMode) return;

            if (!IsConnected)
            {
                Debug.LogWarning("[NetworkManager] Cannot send tap - not connected");
                return;
            }

            Color beamColor = PlayerDataManager.Instance?.Data?.selectedBeamColor ?? Color.cyan;

            TapMessage message = TapMessage.Create(playerId, intruderId, city, country, beamColor);

            // In a real implementation, this would send via WebSocket
            Debug.Log($"[NetworkManager] Sending tap for {intruderId}");
        }

        /// <summary>
        /// Request active intruders in area
        /// </summary>
        public void RequestActiveIntruders(double lat, double lon, float rangeKm)
        {
            if (offlineMode)
            {
                // In offline mode, intruders are spawned locally
                return;
            }

            if (!IsConnected)
            {
                Debug.LogWarning("[NetworkManager] Cannot request intruders - not connected");
                return;
            }

            IntruderListRequest request = new IntruderListRequest
            {
                latitude = lat,
                longitude = lon,
                rangeKm = rangeKm
            };

            // In a real implementation, this would send via WebSocket/REST
            Debug.Log($"[NetworkManager] Requesting intruders at ({lat:F4}, {lon:F4}) within {rangeKm}km");
        }

        /// <summary>
        /// Subscribe to updates for a specific intruder
        /// </summary>
        public void SubscribeToIntruderUpdates(string intruderId)
        {
            if (offlineMode) return;

            if (!IsConnected)
            {
                Debug.LogWarning("[NetworkManager] Cannot subscribe - not connected");
                return;
            }

            // Unsubscribe from previous
            if (!string.IsNullOrEmpty(currentSubscribedIntruderId) && currentSubscribedIntruderId != intruderId)
            {
                UnsubscribeFromIntruderUpdates(currentSubscribedIntruderId);
            }

            currentSubscribedIntruderId = intruderId;

            // In a real implementation, this would subscribe via WebSocket
            Debug.Log($"[NetworkManager] Subscribed to updates for {intruderId}");
        }

        /// <summary>
        /// Unsubscribe from intruder updates
        /// </summary>
        public void UnsubscribeFromIntruderUpdates(string intruderId)
        {
            if (offlineMode) return;
            if (!IsConnected) return;

            if (currentSubscribedIntruderId == intruderId)
            {
                currentSubscribedIntruderId = null;
            }

            // In a real implementation, this would unsubscribe via WebSocket
            Debug.Log($"[NetworkManager] Unsubscribed from updates for {intruderId}");
        }

        /// <summary>
        /// Handle local tap event
        /// </summary>
        private void HandleLocalTap(Intruder intruder, int tapCount)
        {
            if (intruder == null) return;

            string playerId = PlayerDataManager.Instance?.GetPlayerId() ?? "unknown";
            string city = LocationManager.Instance?.City ?? "Unknown";
            string country = LocationManager.Instance?.Country ?? "Unknown";

            SendTap(intruder.InstanceId, playerId, city, country);

            // Subscribe to this intruder for updates
            SubscribeToIntruderUpdates(intruder.InstanceId);
        }

        /// <summary>
        /// Send heartbeat
        /// </summary>
        private void SendHeartbeat()
        {
            if (offlineMode) return;

            // In a real implementation, this would send a ping via WebSocket
            Debug.Log("[NetworkManager] Heartbeat sent");
        }

        /// <summary>
        /// Handle received intruder list
        /// </summary>
        private void HandleIntruderListReceived(IntruderListResponse response)
        {
            if (response?.intruders == null) return;

            OnIntruderListReceived?.Invoke(response.intruders);

            // Sync with spawner
            foreach (var netData in response.intruders)
            {
                var existingIntruder = IntruderSpawner.Instance?.GetIntruder(netData.intruderId);
                if (existingIntruder != null)
                {
                    NetworkDataConverter.ApplyNetworkData(existingIntruder, netData);
                }
                // Could also spawn new intruders from network here
            }
        }

        /// <summary>
        /// Handle received progress update
        /// </summary>
        private void HandleProgressUpdateReceived(IntruderProgressUpdate update)
        {
            if (update == null) return;

            List<RemoteLaserData> lasers = NetworkDataConverter.ToRemoteLaserDataList(update.topContributors);

            OnIntruderProgressUpdated?.Invoke(update.intruderId, update.totalTaps, lasers);

            // Update TapProgressManager
            TapProgressManager.Instance?.ReceiveGlobalUpdate(update.intruderId, update.totalTaps, update.participantCount);

            // Update GlobalLaserDisplay
            GlobalLaserDisplay.Instance?.ReceiveLaserUpdate(lasers);

            // Handle return
            if (update.isReturned)
            {
                var intruder = IntruderSpawner.Instance?.GetIntruder(update.intruderId);
                if (intruder != null)
                {
                    intruder.StartReturnSequence();
                }
            }
        }

        /// <summary>
        /// Handle intruder returned message
        /// </summary>
        private void HandleIntruderReturnedReceived(IntruderReturnedMessage message)
        {
            if (message == null) return;

            OnIntruderReturned?.Invoke(message);
        }

        /// <summary>
        /// Handle connection error
        /// </summary>
        private void HandleConnectionError(string error)
        {
            Debug.LogError($"[NetworkManager] Connection error: {error}");
            SetConnectionState(ConnectionState.Error);

            // Attempt reconnect
            if (reconnectAttempts < maxReconnectAttempts)
            {
                reconnectAttempts++;
                StartCoroutine(ReconnectCoroutine());
            }
        }

        /// <summary>
        /// Reconnect coroutine
        /// </summary>
        private IEnumerator ReconnectCoroutine()
        {
            SetConnectionState(ConnectionState.Reconnecting);
            Debug.Log($"[NetworkManager] Reconnecting in {reconnectDelay}s... (attempt {reconnectAttempts}/{maxReconnectAttempts})");

            yield return new WaitForSeconds(reconnectDelay);

            Connect();
        }

        /// <summary>
        /// Set connection state and notify
        /// </summary>
        private void SetConnectionState(ConnectionState newState)
        {
            if (connectionState == newState) return;

            connectionState = newState;
            OnConnectionStateChanged?.Invoke(newState);
        }

        /// <summary>
        /// Enable/disable offline mode
        /// </summary>
        public void SetOfflineMode(bool offline)
        {
            if (offlineMode == offline) return;

            offlineMode = offline;

            if (offline)
            {
                Disconnect();
                SetConnectionState(ConnectionState.Connected);
            }
            else
            {
                Connect();
            }
        }

        /// <summary>
        /// Get debug info
        /// </summary>
        public string GetDebugInfo()
        {
            return $"State: {connectionState}\n" +
                   $"Offline Mode: {offlineMode}\n" +
                   $"Subscribed: {currentSubscribedIntruderId ?? "None"}\n" +
                   $"Reconnect Attempts: {reconnectAttempts}";
        }

        // Simulated server responses (for testing)
        #region Test Methods

        /// <summary>
        /// Simulate receiving intruder list (for testing)
        /// </summary>
        [ContextMenu("Test - Receive Intruder List")]
        public void TestReceiveIntruderList()
        {
            var response = new IntruderListResponse
            {
                intruders = new List<IntruderNetworkData>(),
                totalGlobalIntruders = 50,
                activePlayersCount = 1000
            };

            HandleIntruderListReceived(response);
        }

        /// <summary>
        /// Simulate receiving progress update (for testing)
        /// </summary>
        [ContextMenu("Test - Receive Progress Update")]
        public void TestReceiveProgressUpdate()
        {
            if (IntruderSpawner.Instance?.ActiveIntruders.Count == 0)
            {
                Debug.LogWarning("[NetworkManager] No active intruders for test");
                return;
            }

            var intruder = IntruderSpawner.Instance.ActiveIntruders[0];

            var update = new IntruderProgressUpdate
            {
                intruderId = intruder.InstanceId,
                totalTaps = intruder.TapProgress + 10,
                participantCount = 5,
                isReturned = false,
                topContributors = new List<ActiveTapperData>
                {
                    new ActiveTapperData
                    {
                        anonymousId = "test_player_1",
                        city = "Tokyo",
                        country = "Japan",
                        tapCount = 20,
                        lastTapTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        beamColor = Color.red
                    },
                    new ActiveTapperData
                    {
                        anonymousId = "test_player_2",
                        city = "London",
                        country = "UK",
                        tapCount = 15,
                        lastTapTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        beamColor = Color.green
                    }
                }
            };

            HandleProgressUpdateReceived(update);
        }

        #endregion
    }
}
