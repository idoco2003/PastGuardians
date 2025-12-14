using UnityEngine;
using PastGuardians.Data;
using PastGuardians.AR;
using PastGuardians.Gameplay;
using PastGuardians.Network;
using PastGuardians.UI;
using System.Collections;

namespace PastGuardians.Core
{
    /// <summary>
    /// Main game manager - coordinates all game systems
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private GameConfig gameConfig;

        [Header("Managers")]
        [SerializeField] private PlayerDataManager playerDataManager;
        [SerializeField] private ARSetupManager arSetupManager;
        [SerializeField] private SkyCameraController skyCameraController;
        [SerializeField] private LocationManager locationManager;
        [SerializeField] private IntruderSpawner intruderSpawner;
        [SerializeField] private IntruderScreenPositioner screenPositioner;
        [SerializeField] private TapInputHandler tapInputHandler;
        [SerializeField] private TapProgressManager tapProgressManager;
        [SerializeField] private ReturnAnimationController returnAnimationController;
        [SerializeField] private NetworkManager networkManager;

        [Header("Visual Systems")]
        [SerializeField] private PlayerLaserRenderer playerLaserRenderer;
        [SerializeField] private GlobalLaserDisplay globalLaserDisplay;

        [Header("UI")]
        [SerializeField] private MainGameHUD mainHUD;
        [SerializeField] private CelebrationOverlay celebrationOverlay;
        [SerializeField] private CodexUI codexUI;

        [Header("Settings")]
        [SerializeField] private bool autoInitialize = true;
        [SerializeField] private bool offlineMode = true;

        // State
        private bool isInitialized;
        private GameState currentState = GameState.Initializing;

        // Properties
        public GameConfig Config => gameConfig;
        public GameState State => currentState;
        public bool IsInitialized => isInitialized;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Set target frame rate
            Application.targetFrameRate = 60;
        }

        private void Start()
        {
            if (autoInitialize)
            {
                StartCoroutine(InitializeGame());
            }
        }

        /// <summary>
        /// Initialize all game systems
        /// </summary>
        public IEnumerator InitializeGame()
        {
            currentState = GameState.Initializing;
            Debug.Log("[GameManager] Initializing game...");

            // Wait for AR to be ready
            if (arSetupManager != null)
            {
                float timeout = 10f;
                while (!arSetupManager.IsARReady && timeout > 0)
                {
                    yield return new WaitForSeconds(0.5f);
                    timeout -= 0.5f;
                }

                if (!arSetupManager.IsARReady)
                {
                    Debug.LogWarning("[GameManager] AR not ready, continuing anyway");
                }
            }

            // Wait for location
            if (locationManager != null)
            {
                float timeout = 10f;
                while (!locationManager.LocationAvailable && timeout > 0)
                {
                    yield return new WaitForSeconds(0.5f);
                    timeout -= 0.5f;
                }

                if (!locationManager.LocationAvailable)
                {
                    Debug.LogWarning("[GameManager] Location not available, using defaults");
                }
            }

            // Configure network mode
            if (networkManager != null)
            {
                networkManager.SetOfflineMode(offlineMode);
            }

            isInitialized = true;
            currentState = GameState.Playing;

            Debug.Log("[GameManager] Game initialized successfully");

            // Start spawning if in offline mode
            if (offlineMode && intruderSpawner != null)
            {
                // Spawn initial test intruder
                yield return new WaitForSeconds(2f);
                intruderSpawner.SpawnTestIntruder();
            }
        }

        /// <summary>
        /// Pause the game
        /// </summary>
        public void Pause()
        {
            currentState = GameState.Paused;
            Time.timeScale = 0f;
        }

        /// <summary>
        /// Resume the game
        /// </summary>
        public void Resume()
        {
            currentState = GameState.Playing;
            Time.timeScale = 1f;
        }

        /// <summary>
        /// Open codex
        /// </summary>
        public void OpenCodex()
        {
            codexUI?.Open();
        }

        /// <summary>
        /// Close codex
        /// </summary>
        public void CloseCodex()
        {
            codexUI?.Close();
        }

        /// <summary>
        /// Handle application pause (mobile background)
        /// </summary>
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // Going to background
                Pause();
            }
            else
            {
                // Resuming
                Resume();
            }
        }

        /// <summary>
        /// Handle application focus
        /// </summary>
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                // Lost focus
            }
            else
            {
                // Gained focus
            }
        }

        /// <summary>
        /// Get a formatted stats string
        /// </summary>
        public string GetGameStats()
        {
            if (playerDataManager?.Data == null) return "No data";

            var data = playerDataManager.Data;
            return $"Rank: {PlayerRank.GetRankTitle(data.currentRank)}\n" +
                   $"XP: {data.currentXP:N0}\n" +
                   $"Total Returns: {data.totalReturns:N0}\n" +
                   $"Boss Returns: {data.bossReturns:N0}";
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-find managers in editor
            if (playerDataManager == null) playerDataManager = FindObjectOfType<PlayerDataManager>();
            if (arSetupManager == null) arSetupManager = FindObjectOfType<ARSetupManager>();
            if (skyCameraController == null) skyCameraController = FindObjectOfType<SkyCameraController>();
            if (locationManager == null) locationManager = FindObjectOfType<LocationManager>();
            if (intruderSpawner == null) intruderSpawner = FindObjectOfType<IntruderSpawner>();
            if (screenPositioner == null) screenPositioner = FindObjectOfType<IntruderScreenPositioner>();
            if (tapInputHandler == null) tapInputHandler = FindObjectOfType<TapInputHandler>();
            if (tapProgressManager == null) tapProgressManager = FindObjectOfType<TapProgressManager>();
            if (returnAnimationController == null) returnAnimationController = FindObjectOfType<ReturnAnimationController>();
            if (networkManager == null) networkManager = FindObjectOfType<NetworkManager>();
            if (playerLaserRenderer == null) playerLaserRenderer = FindObjectOfType<PlayerLaserRenderer>();
            if (globalLaserDisplay == null) globalLaserDisplay = FindObjectOfType<GlobalLaserDisplay>();
            if (mainHUD == null) mainHUD = FindObjectOfType<MainGameHUD>();
            if (celebrationOverlay == null) celebrationOverlay = FindObjectOfType<CelebrationOverlay>();
            if (codexUI == null) codexUI = FindObjectOfType<CodexUI>();
        }
#endif
    }

    /// <summary>
    /// Game state enum
    /// </summary>
    public enum GameState
    {
        Initializing,
        Playing,
        Paused,
        Celebration,
        Codex,
        Settings
    }
}
