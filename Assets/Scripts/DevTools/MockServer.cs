#if DEVELOPMENT_BUILD || UNITY_EDITOR
using UnityEngine;
using PastGuardians.Data;
using PastGuardians.Gameplay;
using PastGuardians.Network;
using System.Collections;
using System.Collections.Generic;

namespace PastGuardians.DevTools
{
    /// <summary>
    /// Simulates server behavior for offline development/testing
    /// </summary>
    public class MockServer : MonoBehaviour
    {
        public static MockServer Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private int simulatedPlayerCount = 10;
        [SerializeField] private float averageTapsPerSecond = 5f;
        [SerializeField] private float spawnIntervalSeconds = 30f;
        [SerializeField] private bool autoStartSimulation = true;

        [Header("Fake Player Data")]
        [SerializeField] private string[] fakeCities = new string[]
        {
            "Tokyo", "London", "New York", "Sydney", "Paris",
            "Berlin", "Mumbai", "São Paulo", "Cairo", "Toronto",
            "Seoul", "Singapore", "Dubai", "Moscow", "Rome",
            "Bangkok", "Mexico City", "Istanbul", "Amsterdam", "Vienna"
        };

        [SerializeField] private string[] fakeCountries = new string[]
        {
            "Japan", "UK", "USA", "Australia", "France",
            "Germany", "India", "Brazil", "Egypt", "Canada",
            "South Korea", "Singapore", "UAE", "Russia", "Italy",
            "Thailand", "Mexico", "Turkey", "Netherlands", "Austria"
        };

        // State
        private bool isRunning;
        private Coroutine simulationCoroutine;
        private Dictionary<string, List<SimulatedPlayer>> intruderParticipants = new Dictionary<string, List<SimulatedPlayer>>();

        // Properties
        public bool IsRunning => isRunning;
        public int SimulatedPlayerCount => simulatedPlayerCount;
        public float TapRate => averageTapsPerSecond;

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
            if (autoStartSimulation)
            {
                StartSimulation();
            }

            // Subscribe to intruder events
            if (IntruderSpawner.Instance != null)
            {
                IntruderSpawner.Instance.OnIntruderSpawned += HandleIntruderSpawned;
                IntruderSpawner.Instance.OnIntruderDespawned += HandleIntruderDespawned;
                IntruderSpawner.Instance.OnIntruderReturned += HandleIntruderReturned;
            }
        }

        private void OnDestroy()
        {
            if (IntruderSpawner.Instance != null)
            {
                IntruderSpawner.Instance.OnIntruderSpawned -= HandleIntruderSpawned;
                IntruderSpawner.Instance.OnIntruderDespawned -= HandleIntruderDespawned;
                IntruderSpawner.Instance.OnIntruderReturned -= HandleIntruderReturned;
            }

            StopSimulation();
        }

        /// <summary>
        /// Start simulation
        /// </summary>
        public void StartSimulation()
        {
            if (isRunning) return;

            isRunning = true;
            simulationCoroutine = StartCoroutine(SimulationLoop());
            UnityEngine.Debug.Log("[MockServer] Simulation started");
        }

        /// <summary>
        /// Stop simulation
        /// </summary>
        public void StopSimulation()
        {
            if (!isRunning) return;

            isRunning = false;

            if (simulationCoroutine != null)
            {
                StopCoroutine(simulationCoroutine);
                simulationCoroutine = null;
            }

            intruderParticipants.Clear();
            UnityEngine.Debug.Log("[MockServer] Simulation stopped");
        }

        /// <summary>
        /// Main simulation loop
        /// </summary>
        private IEnumerator SimulationLoop()
        {
            while (isRunning)
            {
                // Simulate taps on all active intruders
                SimulateTapsOnAllIntruders();

                // Update global laser display with fake lasers
                UpdateFakeLasers();

                yield return new WaitForSeconds(1f / averageTapsPerSecond);
            }
        }

        /// <summary>
        /// Handle new intruder spawned
        /// </summary>
        private void HandleIntruderSpawned(Intruder intruder)
        {
            if (intruder == null) return;

            // Create simulated participants for this intruder
            int participantCount = Random.Range(3, simulatedPlayerCount + 1);
            List<SimulatedPlayer> participants = new List<SimulatedPlayer>();

            for (int i = 0; i < participantCount; i++)
            {
                participants.Add(CreateSimulatedPlayer());
            }

            intruderParticipants[intruder.InstanceId] = participants;
        }

        /// <summary>
        /// Handle intruder despawned
        /// </summary>
        private void HandleIntruderDespawned(Intruder intruder)
        {
            if (intruder == null) return;
            intruderParticipants.Remove(intruder.InstanceId);
        }

        /// <summary>
        /// Handle intruder returned
        /// </summary>
        private void HandleIntruderReturned(Intruder intruder)
        {
            if (intruder == null) return;
            intruderParticipants.Remove(intruder.InstanceId);

            // Clear lasers for this intruder
            GlobalLaserDisplay.Instance?.ClearAll();
        }

        /// <summary>
        /// Simulate taps on all active intruders
        /// </summary>
        private void SimulateTapsOnAllIntruders()
        {
            if (IntruderSpawner.Instance == null) return;

            foreach (var intruder in IntruderSpawner.Instance.ActiveIntruders)
            {
                if (intruder == null) continue;
                if (intruder.State == IntruderState.Returning || intruder.State == IntruderState.Gone) continue;

                SimulateTapsOnIntruder(intruder);
            }
        }

        /// <summary>
        /// Simulate taps on a specific intruder
        /// </summary>
        private void SimulateTapsOnIntruder(Intruder intruder)
        {
            if (!intruderParticipants.TryGetValue(intruder.InstanceId, out List<SimulatedPlayer> participants))
                return;

            // Each participant has a chance to tap
            foreach (var player in participants)
            {
                // Random chance to tap this frame
                if (Random.value < player.tapFrequency)
                {
                    // Register tap (but not through the intruder to avoid double counting)
                    // Instead, just track for laser display
                    player.tapCount++;
                    player.lastTapTime = Time.time;
                }
            }
        }

        /// <summary>
        /// Update fake lasers for global display
        /// </summary>
        private void UpdateFakeLasers()
        {
            if (GlobalLaserDisplay.Instance == null) return;
            if (TapInputHandler.Instance?.CurrentTarget == null) return;

            var currentTarget = TapInputHandler.Instance.CurrentTarget;
            if (!intruderParticipants.TryGetValue(currentTarget.InstanceId, out List<SimulatedPlayer> participants))
                return;

            List<RemoteLaserData> lasers = new List<RemoteLaserData>();

            foreach (var player in participants)
            {
                // Only show recently active players
                if (Time.time - player.lastTapTime > 2f) continue;

                lasers.Add(new RemoteLaserData
                {
                    playerId = player.id,
                    city = player.city,
                    country = player.country,
                    beamColor = player.beamColor,
                    lastTapTime = player.lastTapTime,
                    tapCount = player.tapCount,
                    originDirection = player.direction
                });
            }

            GlobalLaserDisplay.Instance.ReceiveLaserUpdate(lasers);
        }

        /// <summary>
        /// Create a simulated player
        /// </summary>
        private SimulatedPlayer CreateSimulatedPlayer()
        {
            int cityIndex = Random.Range(0, fakeCities.Length);

            return new SimulatedPlayer
            {
                id = $"sim_{System.Guid.NewGuid().ToString("N").Substring(0, 8)}",
                city = fakeCities[cityIndex],
                country = fakeCountries[Mathf.Min(cityIndex, fakeCountries.Length - 1)],
                beamColor = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.8f, 1f),
                tapFrequency = Random.Range(0.1f, 0.5f),
                direction = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized,
                tapCount = 0,
                lastTapTime = Time.time
            };
        }

        /// <summary>
        /// Simulate a burst of taps on an intruder
        /// </summary>
        public void SimulateTapBurst(string intruderId, int tapCount)
        {
            var intruder = IntruderSpawner.Instance?.GetIntruder(intruderId);
            if (intruder != null)
            {
                intruder.RegisterTap(tapCount);
            }
        }

        /// <summary>
        /// Generate fake laser data
        /// </summary>
        public List<RemoteLaserData> GenerateFakeLasers(int count)
        {
            List<RemoteLaserData> lasers = new List<RemoteLaserData>();

            for (int i = 0; i < count; i++)
            {
                var player = CreateSimulatedPlayer();
                lasers.Add(new RemoteLaserData
                {
                    playerId = player.id,
                    city = player.city,
                    country = player.country,
                    beamColor = player.beamColor,
                    lastTapTime = Time.time,
                    tapCount = Random.Range(5, 50),
                    originDirection = player.direction
                });
            }

            return lasers;
        }

        /// <summary>
        /// Set simulated tap rate
        /// </summary>
        public void SetSimulatedTapRate(float rate)
        {
            averageTapsPerSecond = Mathf.Max(0.1f, rate);
        }

        /// <summary>
        /// Set simulated player count
        /// </summary>
        public void SetSimulatedPlayerCount(int count)
        {
            simulatedPlayerCount = Mathf.Max(1, count);
        }

        /// <summary>
        /// Get participant count for intruder
        /// </summary>
        public int GetParticipantCount(string intruderId)
        {
            if (intruderParticipants.TryGetValue(intruderId, out List<SimulatedPlayer> participants))
            {
                return participants.Count + 1;  // +1 for local player
            }
            return 1;
        }

        /// <summary>
        /// Get unique countries count for intruder
        /// </summary>
        public int GetUniqueCountries(string intruderId)
        {
            if (!intruderParticipants.TryGetValue(intruderId, out List<SimulatedPlayer> participants))
                return 1;

            HashSet<string> countries = new HashSet<string>();
            foreach (var p in participants)
            {
                countries.Add(p.country);
            }

            // Add local player's country
            if (AR.LocationManager.Instance != null)
            {
                countries.Add(AR.LocationManager.Instance.Country);
            }

            return countries.Count;
        }

        /// <summary>
        /// Get debug info
        /// </summary>
        public string GetDebugInfo()
        {
            return $"Running: {isRunning}\n" +
                   $"Simulated Players: {simulatedPlayerCount}\n" +
                   $"Tap Rate: {averageTapsPerSecond:F1}/s\n" +
                   $"Active Intruders with Participants: {intruderParticipants.Count}";
        }
    }

    /// <summary>
    /// Simulated player data
    /// </summary>
    public class SimulatedPlayer
    {
        public string id;
        public string city;
        public string country;
        public Color beamColor;
        public float tapFrequency;
        public Vector2 direction;
        public int tapCount;
        public float lastTapTime;
    }
}
#endif
