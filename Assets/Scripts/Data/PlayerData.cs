using System;
using System.Collections.Generic;
using UnityEngine;

namespace PastGuardians.Data
{
    /// <summary>
    /// Serializable player data for save/load
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        [Header("Identity")]
        [Tooltip("Anonymous device-generated ID")]
        public string anonymousId;

        [Tooltip("Privacy-safe city display")]
        public string displayCity;

        [Tooltip("Country for location display")]
        public string displayCountry;

        [Header("Progression")]
        public int currentXP;
        public int currentRank = 1;

        [Header("Customization")]
        public Color selectedBeamColor = new Color(0f, 0.83f, 1f); // Default blue
        public List<string> unlockedCosmetics = new List<string>();

        [Header("Currency")]
        public int coins;

        [Header("Statistics")]
        public int totalReturns;
        public int bossReturns;
        public int totalTaps;
        public int sessionsPlayed;
        public float totalPlayTimeMinutes;

        [Header("Codex Progress")]
        public SerializableDictionary<string, int> codexProgress = new SerializableDictionary<string, int>();

        [Header("Session")]
        public string lastPlayDate;
        public int returnsThisSession;

        /// <summary>
        /// Generate a new anonymous ID
        /// </summary>
        public static string GenerateAnonymousId()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 16);
        }

        /// <summary>
        /// Get formatted location string
        /// </summary>
        public string GetLocationString()
        {
            if (string.IsNullOrEmpty(displayCity) && string.IsNullOrEmpty(displayCountry))
                return "Unknown Location";

            if (string.IsNullOrEmpty(displayCity))
                return displayCountry;

            if (string.IsNullOrEmpty(displayCountry))
                return displayCity;

            return $"{displayCity}, {displayCountry}";
        }

        /// <summary>
        /// Record an intruder return
        /// </summary>
        public void RecordReturn(string intruderId, bool isBoss)
        {
            totalReturns++;
            returnsThisSession++;

            if (isBoss)
                bossReturns++;

            if (!codexProgress.ContainsKey(intruderId))
                codexProgress[intruderId] = 0;

            codexProgress[intruderId]++;
        }

        /// <summary>
        /// Get codex badge level for an intruder
        /// </summary>
        public CodexBadge GetCodexBadge(string intruderId)
        {
            if (!codexProgress.TryGetValue(intruderId, out int count))
                return CodexBadge.None;

            if (count >= 100) return CodexBadge.Gold;
            if (count >= 50) return CodexBadge.Silver;
            if (count >= 10) return CodexBadge.Bronze;
            return CodexBadge.None;
        }

        /// <summary>
        /// Check if today is a new day for daily login
        /// </summary>
        public bool IsNewDay()
        {
            if (string.IsNullOrEmpty(lastPlayDate))
                return true;

            return lastPlayDate != DateTime.Now.ToString("yyyy-MM-dd");
        }

        /// <summary>
        /// Update last play date to today
        /// </summary>
        public void UpdatePlayDate()
        {
            lastPlayDate = DateTime.Now.ToString("yyyy-MM-dd");
        }

        /// <summary>
        /// Reset session stats
        /// </summary>
        public void StartNewSession()
        {
            returnsThisSession = 0;
            sessionsPlayed++;
        }
    }

    /// <summary>
    /// Codex badge levels
    /// </summary>
    public enum CodexBadge
    {
        None,
        Bronze,  // 10 returns
        Silver,  // 50 returns
        Gold     // 100 returns
    }

    /// <summary>
    /// Serializable dictionary for Unity JSON serialization
    /// </summary>
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField] private List<TKey> keys = new List<TKey>();
        [SerializeField] private List<TValue> values = new List<TValue>();

        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            foreach (var kvp in this)
            {
                keys.Add(kvp.Key);
                values.Add(kvp.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            Clear();
            for (int i = 0; i < Mathf.Min(keys.Count, values.Count); i++)
            {
                this[keys[i]] = values[i];
            }
        }
    }
}
