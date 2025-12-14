using UnityEngine;

namespace PastGuardians.Data
{
    /// <summary>
    /// Static helper class for Guardian rank calculations
    /// </summary>
    public static class PlayerRank
    {
        /// <summary>
        /// XP thresholds for each rank (0-indexed, rank 1 = index 0)
        /// </summary>
        public static readonly int[] RankThresholds =
        {
            0,      // Rank 1: Watcher
            500,    // Rank 2: Spotter
            1500,   // Rank 3: Defender
            3500,   // Rank 4: Protector
            7000,   // Rank 5: Shield Bearer
            12000,  // Rank 6: Time Keeper
            20000,  // Rank 7: Rift Closer
            35000,  // Rank 8: Era Walker
            55000,  // Rank 9: Chrono Guard
            100000  // Rank 10: Prime Guardian
        };

        /// <summary>
        /// Title names for each rank
        /// </summary>
        public static readonly string[] RankTitles =
        {
            "Watcher",
            "Spotter",
            "Defender",
            "Protector",
            "Shield Bearer",
            "Time Keeper",
            "Rift Closer",
            "Era Walker",
            "Chrono Guard",
            "Prime Guardian"
        };

        /// <summary>
        /// Rank unlocks description
        /// </summary>
        public static readonly string[] RankUnlocks =
        {
            "Basic beam (white)",
            "Beam color selection",
            "Profile badge display",
            "Beam trail effects",
            "Regional leaderboard",
            "Rare spawn notifications",
            "Boss event priority",
            "All beam customizations",
            "Title: Legendary Guardian",
            "Special golden beam"
        };

        /// <summary>
        /// Maximum rank level
        /// </summary>
        public const int MaxRank = 10;

        /// <summary>
        /// Get rank number for given XP (1-10)
        /// </summary>
        public static int GetRankForXP(int xp)
        {
            for (int i = RankThresholds.Length - 1; i >= 0; i--)
            {
                if (xp >= RankThresholds[i])
                    return i + 1; // Ranks are 1-indexed
            }
            return 1;
        }

        /// <summary>
        /// Get title string for given rank number
        /// </summary>
        public static string GetRankTitle(int rank)
        {
            int index = Mathf.Clamp(rank - 1, 0, RankTitles.Length - 1);
            return RankTitles[index];
        }

        /// <summary>
        /// Get unlock description for given rank
        /// </summary>
        public static string GetRankUnlock(int rank)
        {
            int index = Mathf.Clamp(rank - 1, 0, RankUnlocks.Length - 1);
            return RankUnlocks[index];
        }

        /// <summary>
        /// Get XP required for a specific rank
        /// </summary>
        public static int GetXPForRank(int rank)
        {
            int index = Mathf.Clamp(rank - 1, 0, RankThresholds.Length - 1);
            return RankThresholds[index];
        }

        /// <summary>
        /// Get XP required for next rank
        /// </summary>
        public static int GetXPForNextRank(int currentRank)
        {
            if (currentRank >= MaxRank)
                return RankThresholds[MaxRank - 1];

            return RankThresholds[currentRank]; // currentRank is 1-indexed, so this gets next
        }

        /// <summary>
        /// Get progress to next rank (0-1)
        /// </summary>
        public static float GetProgressToNextRank(int currentXP, int currentRank)
        {
            if (currentRank >= MaxRank)
                return 1f;

            int currentThreshold = RankThresholds[currentRank - 1];
            int nextThreshold = RankThresholds[currentRank];
            int xpInCurrentRank = currentXP - currentThreshold;
            int xpNeededForNext = nextThreshold - currentThreshold;

            return Mathf.Clamp01((float)xpInCurrentRank / xpNeededForNext);
        }

        /// <summary>
        /// Check if XP gain results in rank up
        /// </summary>
        public static bool WillRankUp(int currentXP, int xpGain, int currentRank)
        {
            if (currentRank >= MaxRank)
                return false;

            int newXP = currentXP + xpGain;
            return GetRankForXP(newXP) > currentRank;
        }

        /// <summary>
        /// Get color for rank badge
        /// </summary>
        public static Color GetRankColor(int rank)
        {
            return rank switch
            {
                1 => new Color(0.6f, 0.6f, 0.6f),   // Gray
                2 => new Color(0.8f, 0.8f, 0.8f),   // Light gray
                3 => new Color(0.0f, 0.7f, 0.3f),   // Green
                4 => new Color(0.0f, 0.5f, 0.8f),   // Blue
                5 => new Color(0.5f, 0.0f, 0.8f),   // Purple
                6 => new Color(0.8f, 0.4f, 0.0f),   // Orange
                7 => new Color(0.8f, 0.0f, 0.3f),   // Red
                8 => new Color(0.0f, 0.8f, 0.8f),   // Cyan
                9 => new Color(1.0f, 0.84f, 0.0f),  // Gold
                10 => new Color(1.0f, 0.95f, 0.8f), // Brilliant gold
                _ => Color.white
            };
        }
    }
}
