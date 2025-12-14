using UnityEngine;

namespace PastGuardians.Data
{
    /// <summary>
    /// Global game configuration - all backend-adjustable values
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Past Guardians/Game Config", order = 0)]
    public class GameConfig : ScriptableObject
    {
        [Header("Tap Settings")]
        [Tooltip("Minimum time between taps in milliseconds")]
        [Range(50, 500)]
        public int tapCooldownMs = 100;

        [Tooltip("Maximum taps per second allowed per player")]
        [Range(5, 20)]
        public int maxTapsPerSecond = 10;

        [Tooltip("Hitbox multiplier for easier tapping (child-friendly)")]
        [Range(1f, 3f)]
        public float tapHitboxMultiplier = 1.5f;

        [Header("Laser Display")]
        [Tooltip("How long player's own laser persists after stopping")]
        public float ownLaserPersistSeconds = 5f;

        [Tooltip("How long other players' lasers persist")]
        public float otherLaserPersistSeconds = 3f;

        [Tooltip("Fade out duration for lasers")]
        public float laserFadeDuration = 0.5f;

        [Tooltip("Maximum number of other player lasers to display")]
        [Range(5, 50)]
        public int maxVisibleLasers = 20;

        [Header("Intruder Behavior")]
        [Tooltip("How fast intruder stability regenerates (0-1 per second)")]
        [Range(0f, 2f)]
        public float stabilityRegenRate = 0.5f;

        [Tooltip("Delay after last tap before regen starts")]
        public float regenDelayAfterTap = 2f;

        [Tooltip("Stability threshold (0-1) at which intruder tries to escape")]
        [Range(0.5f, 0.95f)]
        public float escapeThreshold = 0.8f;

        [Tooltip("Enable escape behavior")]
        public bool escapeEnabled = true;

        [Header("Spawning")]
        [Tooltip("Maximum number of intruders active globally")]
        [Range(10, 500)]
        public int globalIntruderCap = 100;

        [Tooltip("Number of intruders spawned per minute")]
        [Range(1, 20)]
        public int spawnsPerMinute = 5;

        [Tooltip("Minimum distance between intruders in km")]
        public float minSpacingKm = 50f;

        [Tooltip("Chance of spawning a boss (0-1)")]
        [Range(0f, 0.1f)]
        public float bossSpawnChance = 0.02f;

        [Tooltip("Hours between guaranteed boss spawns")]
        public float bossSpawnIntervalHours = 4f;

        [Header("Era Spawn Weights")]
        [Range(0f, 1f)] public float prehistoricWeight = 0.25f;
        [Range(0f, 1f)] public float mythologicalWeight = 0.25f;
        [Range(0f, 1f)] public float historicalMachinesWeight = 0.20f;
        [Range(0f, 1f)] public float lostCivilizationsWeight = 0.15f;
        [Range(0f, 1f)] public float timeAnomaliesWeight = 0.15f;

        [Header("View Ranges (km)")]
        [Tooltip("Local range - full detail")]
        public float localRangeKm = 50f;

        [Tooltip("Regional range - medium detail")]
        public float regionalRangeKm = 200f;

        [Tooltip("Continental range - low detail")]
        public float continentalRangeKm = 500f;

        [Tooltip("Global boss range")]
        public float globalBossRangeKm = 20000f;

        [Header("View Range Size Multipliers")]
        public float localSizeMultiplier = 1.0f;
        public float regionalSizeMultiplier = 0.7f;
        public float continentalSizeMultiplier = 0.4f;
        public float globalSizeMultiplier = 0.3f;

        [Header("Coins - Per Tap Rewards")]
        [Tooltip("Coins awarded per tap on an intruder")]
        public int coinsPerTap = 10;

        [Tooltip("Bonus coins for completing a return")]
        public int coinsPerReturn = 100;

        [Tooltip("Bonus coins for boss return")]
        public int coinsBossReturn = 500;

        [Header("Progression - XP Awards")]
        [Tooltip("XP for participating in any return")]
        public int xpPerParticipation = 10;

        [Tooltip("Bonus XP for 10%+ contribution")]
        public int xpBonus10Percent = 15;

        [Tooltip("Bonus XP for 25%+ contribution")]
        public int xpBonus25Percent = 30;

        [Tooltip("XP for being first to tap")]
        public int xpFirstTap = 20;

        [Tooltip("XP for boss participation")]
        public int xpBossParticipation = 100;

        [Tooltip("XP for daily login")]
        public int xpDailyLogin = 25;

        [Tooltip("Bonus XP for returning 5 in one session")]
        public int xpSessionBonus = 50;

        [Header("Colors")]
        public Color defaultLaserColor = new Color(0f, 0.83f, 1f); // #00D4FF
        public Color successColor = new Color(1f, 0.84f, 0f);      // #FFD700 Gold
        public Color skyUIColor = new Color(0.04f, 0.09f, 0.16f);  // #0A1628 Deep blue

        /// <summary>
        /// Get tap cooldown in seconds
        /// </summary>
        public float TapCooldownSeconds => tapCooldownMs / 1000f;

        /// <summary>
        /// Get spawn interval in seconds
        /// </summary>
        public float SpawnIntervalSeconds => 60f / spawnsPerMinute;

        /// <summary>
        /// Get era weight for spawning
        /// </summary>
        public float GetEraWeight(IntruderEra era)
        {
            return era switch
            {
                IntruderEra.Prehistoric => prehistoricWeight,
                IntruderEra.Mythological => mythologicalWeight,
                IntruderEra.HistoricalMachines => historicalMachinesWeight,
                IntruderEra.LostCivilizations => lostCivilizationsWeight,
                IntruderEra.TimeAnomalies => timeAnomaliesWeight,
                _ => 0.2f
            };
        }

        /// <summary>
        /// Get size multiplier for distance tier
        /// </summary>
        public float GetSizeMultiplierForDistance(float distanceKm)
        {
            if (distanceKm <= localRangeKm) return localSizeMultiplier;
            if (distanceKm <= regionalRangeKm) return regionalSizeMultiplier;
            if (distanceKm <= continentalRangeKm) return continentalSizeMultiplier;
            return globalSizeMultiplier;
        }
    }
}
