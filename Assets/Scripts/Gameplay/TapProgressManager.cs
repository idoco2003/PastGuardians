using UnityEngine;
using PastGuardians.Data;
using PastGuardians.Core;
using System;
using System.Collections.Generic;

namespace PastGuardians.Gameplay
{
    /// <summary>
    /// Data for an intruder return event
    /// </summary>
    [Serializable]
    public class IntruderReturnData
    {
        public IntruderData intruderType;
        public string instanceId;
        public int playerTaps;
        public int totalTaps;
        public int participantCount;
        public int countriesCount;
        public int xpEarned;
        public bool wasFirstTapper;
        public bool rankedUp;
        public int oldRank;
        public int newRank;
        public List<string> topContributorCities;
    }

    /// <summary>
    /// Manages tap progress tracking and XP rewards
    /// </summary>
    public class TapProgressManager : MonoBehaviour
    {
        public static TapProgressManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private GameConfig gameConfig;

        // Local tap tracking per intruder
        private Dictionary<string, int> localTapContributions = new Dictionary<string, int>();
        private Dictionary<string, bool> wasFirstTapper = new Dictionary<string, bool>();
        private Dictionary<string, float> firstTapTime = new Dictionary<string, float>();

        // Events
        public event Action<Intruder, int, int> OnProgressUpdated;     // intruder, playerTaps, totalTaps
        public event Action<IntruderReturnData> OnIntruderReturned;
        public event Action<int> OnXPEarned;
        public event Action<int> OnCoinsEarned;                        // coins earned on tap

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
            // Subscribe to tap events
            if (TapInputHandler.Instance != null)
            {
                TapInputHandler.Instance.OnTapRegistered += HandleTapRegistered;
            }

            // Subscribe to intruder events
            if (IntruderSpawner.Instance != null)
            {
                IntruderSpawner.Instance.OnIntruderSpawned += HandleIntruderSpawned;
                IntruderSpawner.Instance.OnIntruderReturned += HandleIntruderReturned;
                IntruderSpawner.Instance.OnIntruderDespawned += HandleIntruderDespawned;
            }
        }

        private void OnDestroy()
        {
            if (TapInputHandler.Instance != null)
            {
                TapInputHandler.Instance.OnTapRegistered -= HandleTapRegistered;
            }

            if (IntruderSpawner.Instance != null)
            {
                IntruderSpawner.Instance.OnIntruderSpawned -= HandleIntruderSpawned;
                IntruderSpawner.Instance.OnIntruderReturned -= HandleIntruderReturned;
                IntruderSpawner.Instance.OnIntruderDespawned -= HandleIntruderDespawned;
            }
        }

        /// <summary>
        /// Handle tap registered event
        /// </summary>
        private void HandleTapRegistered(Intruder intruder, int newProgress)
        {
            if (intruder == null) return;

            string id = intruder.InstanceId;

            // Track local contribution
            if (!localTapContributions.ContainsKey(id))
            {
                localTapContributions[id] = 0;
            }
            localTapContributions[id]++;

            // Track first tapper
            if (!firstTapTime.ContainsKey(id))
            {
                firstTapTime[id] = Time.time;
                wasFirstTapper[id] = true;  // In local mode, always first
            }

            // Award coins per tap!
            int coinsPerTap = gameConfig?.coinsPerTap ?? 10;
            PlayerDataManager.Instance?.AddCoins(coinsPerTap, "Tap");
            OnCoinsEarned?.Invoke(coinsPerTap);

            int playerTaps = localTapContributions[id];
            OnProgressUpdated?.Invoke(intruder, playerTaps, intruder.TapProgress);
        }

        /// <summary>
        /// Handle intruder spawned
        /// </summary>
        private void HandleIntruderSpawned(Intruder intruder)
        {
            // Initialize tracking for this intruder
            string id = intruder.InstanceId;
            localTapContributions[id] = 0;
            wasFirstTapper[id] = false;
        }

        /// <summary>
        /// Handle intruder returned to Tempus Prime
        /// </summary>
        private void HandleIntruderReturned(Intruder intruder)
        {
            if (intruder == null) return;

            string id = intruder.InstanceId;
            int playerTaps = GetLocalTapContribution(id);

            // Only process if player participated
            if (playerTaps > 0)
            {
                ProcessReturn(intruder, playerTaps);
            }

            // Cleanup tracking
            CleanupIntruderData(id);
        }

        /// <summary>
        /// Handle intruder despawned (timeout)
        /// </summary>
        private void HandleIntruderDespawned(Intruder intruder)
        {
            if (intruder == null) return;
            CleanupIntruderData(intruder.InstanceId);
        }

        /// <summary>
        /// Process a return event and award XP
        /// </summary>
        private void ProcessReturn(Intruder intruder, int playerTaps)
        {
            if (gameConfig == null || PlayerDataManager.Instance == null)
            {
                Debug.LogWarning("[TapProgressManager] Missing config or player data manager");
                return;
            }

            string id = intruder.InstanceId;
            int totalTaps = intruder.TotalTapsRequired;
            bool isBoss = intruder.IsBoss;
            bool firstTapper = wasFirstTapper.TryGetValue(id, out bool first) && first;

            // Calculate contribution percentage
            float contributionPercent = totalTaps > 0 ? (float)playerTaps / totalTaps : 0f;

            // Calculate XP
            int xpEarned = gameConfig.xpPerParticipation;

            if (contributionPercent >= 0.25f)
            {
                xpEarned += gameConfig.xpBonus25Percent;
            }
            else if (contributionPercent >= 0.10f)
            {
                xpEarned += gameConfig.xpBonus10Percent;
            }

            if (firstTapper)
            {
                xpEarned += gameConfig.xpFirstTap;
            }

            if (isBoss)
            {
                xpEarned += gameConfig.xpBossParticipation;
            }

            // Check for session bonus
            var playerData = PlayerDataManager.Instance.Data;
            if (playerData != null && playerData.returnsThisSession == 4)  // About to be 5th
            {
                xpEarned += gameConfig.xpSessionBonus;
            }

            // Record the return and award XP
            int oldRank = playerData?.currentRank ?? 1;
            PlayerDataManager.Instance.RecordIntruderReturn(
                intruder.Data.intruderId,
                isBoss,
                playerTaps,
                totalTaps,
                firstTapper
            );
            int newRank = playerData?.currentRank ?? 1;

            // Create return data
            IntruderReturnData returnData = new IntruderReturnData
            {
                intruderType = intruder.Data,
                instanceId = id,
                playerTaps = playerTaps,
                totalTaps = totalTaps,
                participantCount = 1,  // Local mode only
                countriesCount = 1,
                xpEarned = xpEarned,
                wasFirstTapper = firstTapper,
                rankedUp = newRank > oldRank,
                oldRank = oldRank,
                newRank = newRank,
                topContributorCities = new List<string> { AR.LocationManager.Instance?.City ?? "Unknown" }
            };

            OnIntruderReturned?.Invoke(returnData);
            OnXPEarned?.Invoke(xpEarned);

            Debug.Log($"[TapProgressManager] Return completed! +{xpEarned} XP, {contributionPercent:P0} contribution");
        }

        /// <summary>
        /// Add local tap contribution
        /// </summary>
        public void AddLocalTap(Intruder intruder)
        {
            if (intruder == null) return;

            string id = intruder.InstanceId;
            if (!localTapContributions.ContainsKey(id))
            {
                localTapContributions[id] = 0;
            }
            localTapContributions[id]++;
        }

        /// <summary>
        /// Receive global tap update from server
        /// </summary>
        public void ReceiveGlobalUpdate(string intruderId, int totalTaps, int participantCount)
        {
            var intruder = IntruderSpawner.Instance?.GetIntruder(intruderId);
            if (intruder != null)
            {
                intruder.SetTapProgress(totalTaps);

                int playerTaps = GetLocalTapContribution(intruderId);
                OnProgressUpdated?.Invoke(intruder, playerTaps, totalTaps);
            }
        }

        /// <summary>
        /// Get local tap contribution for an intruder
        /// </summary>
        public int GetLocalTapContribution(string intruderId)
        {
            return localTapContributions.TryGetValue(intruderId, out int taps) ? taps : 0;
        }

        /// <summary>
        /// Get contribution percentage for an intruder
        /// </summary>
        public float GetContributionPercent(Intruder intruder)
        {
            if (intruder == null) return 0f;

            int playerTaps = GetLocalTapContribution(intruder.InstanceId);
            int totalTaps = intruder.TapProgress;

            return totalTaps > 0 ? (float)playerTaps / totalTaps : 0f;
        }

        /// <summary>
        /// Calculate potential XP reward for current progress
        /// </summary>
        public int CalculatePotentialXP(Intruder intruder)
        {
            if (intruder == null || gameConfig == null) return 0;

            int playerTaps = GetLocalTapContribution(intruder.InstanceId);
            if (playerTaps == 0) return 0;

            int totalTaps = intruder.TotalTapsRequired;
            float contributionPercent = totalTaps > 0 ? (float)playerTaps / totalTaps : 0f;

            int xp = gameConfig.xpPerParticipation;

            if (contributionPercent >= 0.25f)
                xp += gameConfig.xpBonus25Percent;
            else if (contributionPercent >= 0.10f)
                xp += gameConfig.xpBonus10Percent;

            if (wasFirstTapper.TryGetValue(intruder.InstanceId, out bool first) && first)
                xp += gameConfig.xpFirstTap;

            if (intruder.IsBoss)
                xp += gameConfig.xpBossParticipation;

            return xp;
        }

        /// <summary>
        /// Cleanup tracking data for an intruder
        /// </summary>
        private void CleanupIntruderData(string intruderId)
        {
            localTapContributions.Remove(intruderId);
            wasFirstTapper.Remove(intruderId);
            firstTapTime.Remove(intruderId);
        }

        /// <summary>
        /// Get debug info
        /// </summary>
        public string GetDebugInfo()
        {
            return $"Tracked Intruders: {localTapContributions.Count}\n" +
                   $"Total Local Taps: {GetTotalLocalTaps()}";
        }

        private int GetTotalLocalTaps()
        {
            int total = 0;
            foreach (var kvp in localTapContributions)
            {
                total += kvp.Value;
            }
            return total;
        }
    }
}
