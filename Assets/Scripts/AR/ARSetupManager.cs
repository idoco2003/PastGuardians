using UnityEngine;
using UnityEngine.XR.ARFoundation;
using System;
using System.Collections;

namespace PastGuardians.AR
{
    /// <summary>
    /// Manages AR Foundation initialization and session state
    /// </summary>
    public class ARSetupManager : MonoBehaviour
    {
        public static ARSetupManager Instance { get; private set; }

        [Header("AR Components")]
        [SerializeField] private ARSession arSession;
        [SerializeField] private ARCameraManager arCameraManager;

        [Header("Error UI")]
        [SerializeField] private GameObject arNotSupportedPanel;
        [SerializeField] private GameObject permissionDeniedPanel;
        [SerializeField] private GameObject initializingPanel;

        [Header("Settings")]
        [SerializeField] private bool autoInitialize = true;
        [SerializeField] private float initializationTimeout = 10f;

        // State
        private ARSessionState currentSessionState = ARSessionState.None;
        private bool isInitialized;
        private bool permissionsGranted;

        // Events
        public event Action OnARReady;
        public event Action<string> OnARError;
        public event Action<ARSessionState> OnSessionStateChanged;

        public bool IsARReady => isInitialized && currentSessionState == ARSessionState.SessionTracking;
        public bool IsARSupported { get; private set; } = true;
        public ARSessionState SessionState => currentSessionState;

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
            // Subscribe to AR session state changes
            ARSession.stateChanged += OnARSessionStateChanged;

            if (autoInitialize)
            {
                StartCoroutine(InitializeAR());
            }
        }

        private void OnDestroy()
        {
            ARSession.stateChanged -= OnARSessionStateChanged;
        }

        /// <summary>
        /// Initialize AR session
        /// </summary>
        public IEnumerator InitializeAR()
        {
            ShowPanel(initializingPanel);

            Debug.Log("[ARSetupManager] Starting AR initialization...");

            // Check AR availability
            if (ARSession.state == ARSessionState.None ||
                ARSession.state == ARSessionState.CheckingAvailability)
            {
                yield return ARSession.CheckAvailability();
            }

            // Handle unsupported devices
            if (ARSession.state == ARSessionState.Unsupported)
            {
                IsARSupported = false;
                ShowPanel(arNotSupportedPanel);
                OnARError?.Invoke("AR is not supported on this device");
                Debug.LogWarning("[ARSetupManager] AR not supported on this device");
                yield break;
            }

            // Request permissions if needed
            if (ARSession.state == ARSessionState.NeedsInstall)
            {
                Debug.Log("[ARSetupManager] AR software needs installation");
                yield return ARSession.Install();
            }

            // Wait for session to initialize
            float timeout = initializationTimeout;
            while (ARSession.state != ARSessionState.SessionTracking && timeout > 0)
            {
                yield return new WaitForSeconds(0.5f);
                timeout -= 0.5f;
            }

            if (ARSession.state == ARSessionState.SessionTracking)
            {
                isInitialized = true;
                HideAllPanels();
                OnARReady?.Invoke();
                Debug.Log("[ARSetupManager] AR initialized successfully");
            }
            else
            {
                ShowPanel(arNotSupportedPanel);
                OnARError?.Invoke($"AR initialization failed. State: {ARSession.state}");
                Debug.LogError($"[ARSetupManager] AR initialization failed. State: {ARSession.state}");
            }
        }

        /// <summary>
        /// Handle AR session state changes
        /// </summary>
        private void OnARSessionStateChanged(ARSessionStateChangedEventArgs args)
        {
            currentSessionState = args.state;
            OnSessionStateChanged?.Invoke(args.state);

            Debug.Log($"[ARSetupManager] AR Session state changed: {args.state}");

            switch (args.state)
            {
                case ARSessionState.SessionTracking:
                    if (!isInitialized)
                    {
                        isInitialized = true;
                        HideAllPanels();
                        OnARReady?.Invoke();
                    }
                    break;

                case ARSessionState.SessionInitializing:
                    ShowPanel(initializingPanel);
                    break;

                case ARSessionState.Unsupported:
                    IsARSupported = false;
                    ShowPanel(arNotSupportedPanel);
                    break;
            }
        }

        /// <summary>
        /// Request camera permission
        /// </summary>
        public IEnumerator RequestCameraPermission()
        {
#if UNITY_ANDROID
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            {
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
                yield return new WaitForSeconds(0.5f);

                permissionsGranted = UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera);
            }
            else
            {
                permissionsGranted = true;
            }
#elif UNITY_IOS
            // iOS handles permissions through Info.plist
            permissionsGranted = true;
            yield return null;
#else
            permissionsGranted = true;
            yield return null;
#endif

            if (!permissionsGranted)
            {
                ShowPanel(permissionDeniedPanel);
                OnARError?.Invoke("Camera permission denied");
            }
        }

        /// <summary>
        /// Reset AR session
        /// </summary>
        public void ResetSession()
        {
            if (arSession != null)
            {
                arSession.Reset();
                Debug.Log("[ARSetupManager] AR session reset");
            }
        }

        /// <summary>
        /// Pause AR session
        /// </summary>
        public void PauseSession()
        {
            if (arSession != null)
            {
                arSession.enabled = false;
                Debug.Log("[ARSetupManager] AR session paused");
            }
        }

        /// <summary>
        /// Resume AR session
        /// </summary>
        public void ResumeSession()
        {
            if (arSession != null)
            {
                arSession.enabled = true;
                Debug.Log("[ARSetupManager] AR session resumed");
            }
        }

        private void ShowPanel(GameObject panel)
        {
            HideAllPanels();
            if (panel != null)
            {
                panel.SetActive(true);
            }
        }

        private void HideAllPanels()
        {
            if (arNotSupportedPanel != null) arNotSupportedPanel.SetActive(false);
            if (permissionDeniedPanel != null) permissionDeniedPanel.SetActive(false);
            if (initializingPanel != null) initializingPanel.SetActive(false);
        }

        /// <summary>
        /// Get AR session info for debugging
        /// </summary>
        public string GetDebugInfo()
        {
            return $"AR State: {currentSessionState}\n" +
                   $"Initialized: {isInitialized}\n" +
                   $"Supported: {IsARSupported}\n" +
                   $"Ready: {IsARReady}";
        }
    }
}
