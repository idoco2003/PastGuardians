using UnityEngine;
using PastGuardians.Data;
using System;

namespace PastGuardians.Core
{
    /// <summary>
    /// Singleton manager for player data persistence and operations
    /// </summary>
    public class PlayerDataManager : MonoBehaviour
    {
        public static PlayerDataManager Instance { get; private set; }

        private const string PLAYER_DATA_KEY = "PastGuardians_PlayerData";

        [Header("Configuration")]
        [SerializeField] private GameConfig gameConfig;

        [Header("Runtime Data")]
        [SerializeField] private PlayerData currentPlayerData;

        public PlayerData Data => currentPlayerData;
        public GameConfig Config => gameConfig;

        // Events
        public event Action<int, int> OnXPChanged;           // oldXP, newXP
        public event Action<int, int> OnRankChanged;         // oldRank, newRank
        public event Action<int> OnCoinsChanged;             // newCoinTotal
        public event Action<string, int> OnCodexUpdated;     // intruderId, newCount
        public event Action OnDataLoaded;
        public event Action OnDataSaved;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadPlayerData();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SavePlayerData();
            }
        }

        private void OnApplicationQuit()
        {
            SavePlayerData();
        }

        /// <summary>
        /// Load player data from persistent storage
        /// </summary>
        public void LoadPlayerData()
        {
            string json = PlayerPrefs.GetString(PLAYER_DATA_KEY, "");

            if (string.IsNullOrEmpty(json))
            {
                // Create new player data
                currentPlayerData = new PlayerData
                {
                    anonymousId = PlayerData.GenerateAnonymousId(),
                    currentRank = 1,
                    currentXP = 0
                };
                Debug.Log($"[PlayerDataManager] Created new player: {currentPlayerData.anonymousId}");
            }
            else
            {
                try
                {
                    currentPlayerData = JsonUtility.FromJson<PlayerData>(json);
                    Debug.Log($"[PlayerDataManager] Loaded player: {currentPlayerData.anonymousId}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[PlayerDataManager] Failed to load player data: {e.Message}");
                    currentPlayerData = new PlayerData
                    {
                        anonymousId = PlayerData.GenerateAnonymousId(),
                        currentRank = 1,
                        currentXP = 0
                    };
                }
            }

            // Check for daily login bonus
            if (currentPlayerData.IsNewDay())
            {
                currentPlayerData.UpdatePlayDate();
                currentPlayerData.StartNewSession();

                if (gameConfig != null)
                {
                    AddXP(gameConfig.xpDailyLogin, "Daily Login");
                }
            }

            OnDataLoaded?.Invoke();
        }

        /// <summary>
        /// Save player data to persistent storage
        /// </summary>
        public void SavePlayerData()
        {
            if (currentPlayerData == null) return;

            try
            {
                string json = JsonUtility.ToJson(currentPlayerData, true);
                PlayerPrefs.SetString(PLAYER_DATA_KEY, json);
                PlayerPrefs.Save();

                OnDataSaved?.Invoke();
                Debug.Log("[PlayerDataManager] Player data saved");
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerDataManager] Failed to save player data: {e.Message}");
            }
        }

        /// <summary>
        /// Update player location (city-level only for privacy)
        /// </summary>
        public void UpdateLocation(string city, string country)
        {
            if (currentPlayerData == null) return;

            currentPlayerData.displayCity = city;
            currentPlayerData.displayCountry = country;
            SavePlayerData();
        }

        /// <summary>
        /// Add XP and handle rank ups
        /// </summary>
        public void AddXP(int amount, string source = "")
        {
            if (currentPlayerData == null || amount <= 0) return;

            int oldXP = currentPlayerData.currentXP;
            int oldRank = currentPlayerData.currentRank;

            currentPlayerData.currentXP += amount;

            int newRank = PlayerRank.GetRankForXP(currentPlayerData.currentXP);

            if (newRank != oldRank)
            {
                currentPlayerData.currentRank = newRank;
                OnRankChanged?.Invoke(oldRank, newRank);
                Debug.Log($"[PlayerDataManager] Rank up! {PlayerRank.GetRankTitle(oldRank)} -> {PlayerRank.GetRankTitle(newRank)}");
            }

            OnXPChanged?.Invoke(oldXP, currentPlayerData.currentXP);

            if (!string.IsNullOrEmpty(source))
            {
                Debug.Log($"[PlayerDataManager] +{amount} XP from {source}");
            }

            SavePlayerData();
        }

        /// <summary>
        /// Record an intruder return and award XP
        /// </summary>
        public void RecordIntruderReturn(string intruderId, bool isBoss, int playerTaps, int totalTaps, bool wasFirstTapper)
        {
            if (currentPlayerData == null || gameConfig == null) return;

            // Record in codex
            currentPlayerData.RecordReturn(intruderId, isBoss);

            int oldCount = currentPlayerData.codexProgress.TryGetValue(intruderId, out int c) ? c - 1 : 0;
            OnCodexUpdated?.Invoke(intruderId, oldCount + 1);

            // Calculate XP
            int xpEarned = gameConfig.xpPerParticipation;

            // Contribution bonuses
            float contributionPercent = totalTaps > 0 ? (float)playerTaps / totalTaps : 0f;

            if (contributionPercent >= 0.25f)
            {
                xpEarned += gameConfig.xpBonus25Percent;
            }
            else if (contributionPercent >= 0.10f)
            {
                xpEarned += gameConfig.xpBonus10Percent;
            }

            // First tapper bonus
            if (wasFirstTapper)
            {
                xpEarned += gameConfig.xpFirstTap;
            }

            // Boss bonus
            if (isBoss)
            {
                xpEarned += gameConfig.xpBossParticipation;
            }

            // Session bonus (5 returns)
            if (currentPlayerData.returnsThisSession == 5)
            {
                xpEarned += gameConfig.xpSessionBonus;
            }

            AddXP(xpEarned, $"Return: {intruderId}");

            // Update total taps
            currentPlayerData.totalTaps += playerTaps;
        }

        /// <summary>
        /// Add coins to player
        /// </summary>
        public void AddCoins(int amount, string source = "")
        {
            if (currentPlayerData == null || amount <= 0) return;

            currentPlayerData.coins += amount;

            if (!string.IsNullOrEmpty(source))
            {
                Debug.Log($"[PlayerDataManager] +{amount} coins from {source}");
            }

            OnCoinsChanged?.Invoke(currentPlayerData.coins);
            SavePlayerData();
        }

        /// <summary>
        /// Spend coins (returns true if successful)
        /// </summary>
        public bool SpendCoins(int amount)
        {
            if (currentPlayerData == null || amount <= 0) return false;
            if (currentPlayerData.coins < amount) return false;

            currentPlayerData.coins -= amount;
            OnCoinsChanged?.Invoke(currentPlayerData.coins);
            SavePlayerData();
            return true;
        }

        /// <summary>
        /// Get current coin count
        /// </summary>
        public int GetCoins()
        {
            return currentPlayerData?.coins ?? 0;
        }

        /// <summary>
        /// Set beam color
        /// </summary>
        public void SetBeamColor(Color color)
        {
            if (currentPlayerData == null) return;

            currentPlayerData.selectedBeamColor = color;
            SavePlayerData();
        }

        /// <summary>
        /// Unlock a cosmetic item
        /// </summary>
        public void UnlockCosmetic(string cosmeticId)
        {
            if (currentPlayerData == null) return;

            if (!currentPlayerData.unlockedCosmetics.Contains(cosmeticId))
            {
                currentPlayerData.unlockedCosmetics.Add(cosmeticId);
                SavePlayerData();
            }
        }

        /// <summary>
        /// Check if a cosmetic is unlocked
        /// </summary>
        public bool IsCosmeticUnlocked(string cosmeticId)
        {
            return currentPlayerData?.unlockedCosmetics.Contains(cosmeticId) ?? false;
        }

        /// <summary>
        /// Get anonymous player ID
        /// </summary>
        public string GetPlayerId()
        {
            return currentPlayerData?.anonymousId ?? "unknown";
        }

        /// <summary>
        /// Get current rank title
        /// </summary>
        public string GetRankTitle()
        {
            return PlayerRank.GetRankTitle(currentPlayerData?.currentRank ?? 1);
        }

        /// <summary>
        /// Reset all player data (for testing)
        /// </summary>
        [ContextMenu("Reset Player Data")]
        public void ResetPlayerData()
        {
            PlayerPrefs.DeleteKey(PLAYER_DATA_KEY);
            PlayerPrefs.Save();
            LoadPlayerData();
            Debug.Log("[PlayerDataManager] Player data reset");
        }
    }
}
