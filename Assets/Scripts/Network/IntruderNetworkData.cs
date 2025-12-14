using System;
using System.Collections.Generic;
using UnityEngine;
using PastGuardians.Data;
using PastGuardians.Gameplay;

namespace PastGuardians.Network
{
    /// <summary>
    /// Network data structure for intruders received from server
    /// </summary>
    [Serializable]
    public class IntruderNetworkData
    {
        public string intruderId;              // Unique instance ID
        public string intruderTypeId;          // Maps to IntruderData ScriptableObject
        public double latitude;
        public double longitude;
        public float altitude;
        public int currentTapProgress;
        public int totalTapsRequired;
        public long spawnTimestamp;            // Unix timestamp
        public long expiryTimestamp;           // Unix timestamp
        public List<ActiveTapperData> activeToppers;

        /// <summary>
        /// Calculate time remaining until expiry
        /// </summary>
        public float GetTimeRemaining()
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return Mathf.Max(0, expiryTimestamp - now);
        }

        /// <summary>
        /// Check if intruder has expired
        /// </summary>
        public bool IsExpired()
        {
            return GetTimeRemaining() <= 0;
        }

        /// <summary>
        /// Get progress percentage
        /// </summary>
        public float GetProgressPercent()
        {
            return totalTapsRequired > 0 ? (float)currentTapProgress / totalTapsRequired : 0f;
        }
    }

    /// <summary>
    /// Data for an active participant tapping an intruder
    /// </summary>
    [Serializable]
    public class ActiveTapperData
    {
        public string anonymousId;
        public string city;
        public string country;
        public int tapCount;
        public long lastTapTimestamp;
        public Color beamColor;

        /// <summary>
        /// Get city/country display string
        /// </summary>
        public string GetLocationString()
        {
            if (string.IsNullOrEmpty(city) && string.IsNullOrEmpty(country))
                return "Unknown";

            if (string.IsNullOrEmpty(city))
                return country;

            if (string.IsNullOrEmpty(country))
                return city;

            return $"{city}, {country}";
        }

        /// <summary>
        /// Check if tapper is recently active
        /// </summary>
        public bool IsRecentlyActive(float thresholdSeconds = 3f)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return (now - lastTapTimestamp) <= thresholdSeconds;
        }

        /// <summary>
        /// Convert to RemoteLaserData for display
        /// </summary>
        public RemoteLaserData ToRemoteLaserData()
        {
            return new RemoteLaserData
            {
                playerId = anonymousId,
                city = city,
                country = country,
                beamColor = beamColor,
                lastTapTime = lastTapTimestamp,
                tapCount = tapCount,
                originDirection = Vector2.zero  // Will be calculated by GlobalLaserDisplay
            };
        }
    }

    /// <summary>
    /// Network message for sending tap to server
    /// </summary>
    [Serializable]
    public class TapMessage
    {
        public string playerId;
        public string intruderId;
        public string city;
        public string country;
        public int tapCount;
        public long timestamp;
        public Color beamColor;

        public static TapMessage Create(string playerId, string intruderId, string city, string country, Color beamColor)
        {
            return new TapMessage
            {
                playerId = playerId,
                intruderId = intruderId,
                city = city,
                country = country,
                tapCount = 1,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                beamColor = beamColor
            };
        }
    }

    /// <summary>
    /// Network message for intruder progress update
    /// </summary>
    [Serializable]
    public class IntruderProgressUpdate
    {
        public string intruderId;
        public int totalTaps;
        public int participantCount;
        public List<ActiveTapperData> topContributors;
        public bool isReturned;
    }

    /// <summary>
    /// Network message for intruder returned event
    /// </summary>
    [Serializable]
    public class IntruderReturnedMessage
    {
        public string intruderId;
        public string intruderTypeId;
        public int totalTaps;
        public int participantCount;
        public int countriesCount;
        public List<string> topContributorCities;
        public long returnTimestamp;
    }

    /// <summary>
    /// Request for active intruders in area
    /// </summary>
    [Serializable]
    public class IntruderListRequest
    {
        public double latitude;
        public double longitude;
        public float rangeKm;
    }

    /// <summary>
    /// Response with list of active intruders
    /// </summary>
    [Serializable]
    public class IntruderListResponse
    {
        public List<IntruderNetworkData> intruders;
        public int totalGlobalIntruders;
        public int activePlayersCount;
    }

    /// <summary>
    /// Helper class for converting between network and local data
    /// </summary>
    public static class NetworkDataConverter
    {
        /// <summary>
        /// Convert network data to local intruder component
        /// </summary>
        public static void ApplyNetworkData(Intruder intruder, IntruderNetworkData netData)
        {
            if (intruder == null || netData == null) return;

            intruder.SetTapProgress(netData.currentTapProgress);
        }

        /// <summary>
        /// Convert local intruder to network data
        /// </summary>
        public static IntruderNetworkData ToNetworkData(Intruder intruder)
        {
            if (intruder == null || intruder.Data == null) return null;

            return new IntruderNetworkData
            {
                intruderId = intruder.InstanceId,
                intruderTypeId = intruder.Data.intruderId,
                latitude = intruder.Latitude,
                longitude = intruder.Longitude,
                altitude = intruder.Altitude,
                currentTapProgress = intruder.TapProgress,
                totalTapsRequired = intruder.TotalTapsRequired,
                spawnTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                expiryTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (long)intruder.TimeRemaining,
                activeToppers = new List<ActiveTapperData>()
            };
        }

        /// <summary>
        /// Convert active tappers to remote laser data list
        /// </summary>
        public static List<RemoteLaserData> ToRemoteLaserDataList(List<ActiveTapperData> tappers)
        {
            List<RemoteLaserData> lasers = new List<RemoteLaserData>();

            if (tappers == null) return lasers;

            foreach (var tapper in tappers)
            {
                lasers.Add(tapper.ToRemoteLaserData());
            }

            return lasers;
        }
    }
}
