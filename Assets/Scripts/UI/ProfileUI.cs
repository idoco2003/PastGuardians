using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PastGuardians.Core;
using PastGuardians.Data;
using System;

namespace PastGuardians.UI
{
    /// <summary>
    /// Profile screen showing player stats and information
    /// </summary>
    public class ProfileUI : MonoBehaviour
    {
        public static ProfileUI Instance { get; private set; }

        [Header("Header")]
        [SerializeField] private Button backButton;

        [Header("Player Identity")]
        [SerializeField] private TextMeshProUGUI playerIdText;
        [SerializeField] private TextMeshProUGUI locationText;
        [SerializeField] private Image beamColorPreview;

        [Header("Rank Display")]
        [SerializeField] private TextMeshProUGUI rankTitleText;
        [SerializeField] private TextMeshProUGUI rankNumberText;
        [SerializeField] private Image rankBadge;
        [SerializeField] private Slider xpProgressBar;
        [SerializeField] private TextMeshProUGUI xpProgressText;
        [SerializeField] private TextMeshProUGUI xpToNextRankText;

        [Header("Currency")]
        [SerializeField] private TextMeshProUGUI coinCountText;

        [Header("Statistics")]
        [SerializeField] private TextMeshProUGUI totalReturnsText;
        [SerializeField] private TextMeshProUGUI bossReturnsText;
        [SerializeField] private TextMeshProUGUI totalTapsText;
        [SerializeField] private TextMeshProUGUI sessionsPlayedText;
        [SerializeField] private TextMeshProUGUI playTimeText;
        [SerializeField] private TextMeshProUGUI codexProgressText;

        [Header("Achievements Preview")]
        [SerializeField] private Transform achievementContainer;
        [SerializeField] private GameObject achievementBadgePrefab;

        [Header("Actions")]
        [SerializeField] private Button shareButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button resetDataButton;

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
            // Setup button listeners
            if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
            if (shareButton != null) shareButton.onClick.AddListener(OnShareClicked);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
            if (resetDataButton != null) resetDataButton.onClick.AddListener(OnResetDataClicked);

            // Subscribe to data changes
            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.OnXPChanged += HandleXPChanged;
                PlayerDataManager.Instance.OnRankChanged += HandleRankChanged;
                PlayerDataManager.Instance.OnCoinsChanged += HandleCoinsChanged;
                PlayerDataManager.Instance.OnDataLoaded += RefreshProfile;
            }
        }

        private void OnEnable()
        {
            RefreshProfile();
        }

        private void OnDestroy()
        {
            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.OnXPChanged -= HandleXPChanged;
                PlayerDataManager.Instance.OnRankChanged -= HandleRankChanged;
                PlayerDataManager.Instance.OnCoinsChanged -= HandleCoinsChanged;
                PlayerDataManager.Instance.OnDataLoaded -= RefreshProfile;
            }
        }

        /// <summary>
        /// Refresh all profile data
        /// </summary>
        public void RefreshProfile()
        {
            if (PlayerDataManager.Instance?.Data == null) return;

            var data = PlayerDataManager.Instance.Data;

            UpdateIdentity(data);
            UpdateRankDisplay(data);
            UpdateStatistics(data);
            UpdateAchievements(data);
        }

        /// <summary>
        /// Update player identity section
        /// </summary>
        private void UpdateIdentity(PlayerData data)
        {
            // Player ID (shortened for display)
            if (playerIdText != null)
            {
                string shortId = data.anonymousId.Length > 8
                    ? data.anonymousId.Substring(0, 8) + "..."
                    : data.anonymousId;
                playerIdText.text = $"Guardian #{shortId}";
            }

            // Location
            if (locationText != null)
            {
                locationText.text = data.GetLocationString();
            }

            // Beam color preview
            if (beamColorPreview != null)
            {
                beamColorPreview.color = data.selectedBeamColor;
            }
        }

        /// <summary>
        /// Update rank display section
        /// </summary>
        private void UpdateRankDisplay(PlayerData data)
        {
            int rank = data.currentRank;
            int xp = data.currentXP;

            // Rank title
            if (rankTitleText != null)
            {
                rankTitleText.text = PlayerRank.GetRankTitle(rank);
            }

            // Rank number
            if (rankNumberText != null)
            {
                rankNumberText.text = $"Rank {rank}";
            }

            // Rank badge color
            if (rankBadge != null)
            {
                rankBadge.color = PlayerRank.GetRankColor(rank);
            }

            // XP progress
            float progress = PlayerRank.GetProgressToNextRank(xp, rank);
            int nextRankXP = PlayerRank.GetXPForNextRank(rank);
            int currentRankXP = PlayerRank.GetXPForRank(rank);
            int xpIntoRank = xp - currentRankXP;
            int xpNeeded = nextRankXP - currentRankXP;

            if (xpProgressBar != null)
            {
                xpProgressBar.value = progress;
            }

            if (xpProgressText != null)
            {
                xpProgressText.text = $"{xp:N0} XP";
            }

            if (xpToNextRankText != null)
            {
                if (rank >= 10)
                {
                    xpToNextRankText.text = "Max Rank!";
                }
                else
                {
                    xpToNextRankText.text = $"{xpIntoRank:N0} / {xpNeeded:N0} to next rank";
                }
            }

            // Coins
            if (coinCountText != null)
            {
                coinCountText.text = $"{data.coins:N0}";
            }
        }

        /// <summary>
        /// Update statistics section
        /// </summary>
        private void UpdateStatistics(PlayerData data)
        {
            if (totalReturnsText != null)
            {
                totalReturnsText.text = $"{data.totalReturns:N0}";
            }

            if (bossReturnsText != null)
            {
                bossReturnsText.text = $"{data.bossReturns:N0}";
            }

            if (totalTapsText != null)
            {
                totalTapsText.text = $"{data.totalTaps:N0}";
            }

            if (sessionsPlayedText != null)
            {
                sessionsPlayedText.text = $"{data.sessionsPlayed:N0}";
            }

            if (playTimeText != null)
            {
                float hours = data.totalPlayTimeMinutes / 60f;
                if (hours >= 1)
                {
                    playTimeText.text = $"{hours:F1} hours";
                }
                else
                {
                    playTimeText.text = $"{data.totalPlayTimeMinutes:F0} min";
                }
            }

            if (codexProgressText != null)
            {
                int discovered = data.codexProgress.Count;
                codexProgressText.text = $"{discovered} discovered";
            }
        }

        /// <summary>
        /// Update achievements section
        /// </summary>
        private void UpdateAchievements(PlayerData data)
        {
            // Could show top achievements/badges here
            // For now, just show codex badges

            if (achievementContainer == null) return;

            // Clear existing
            foreach (Transform child in achievementContainer)
            {
                Destroy(child.gameObject);
            }

            // Show badges for each codex entry
            int badgeCount = 0;
            foreach (var entry in data.codexProgress)
            {
                CodexBadge badge = data.GetCodexBadge(entry.Key);
                if (badge != CodexBadge.None && badgeCount < 6)  // Limit display
                {
                    SpawnBadgeIcon(entry.Key, badge);
                    badgeCount++;
                }
            }
        }

        /// <summary>
        /// Spawn a badge icon
        /// </summary>
        private void SpawnBadgeIcon(string intruderId, CodexBadge badge)
        {
            if (achievementContainer == null) return;

            GameObject badgeObj;
            if (achievementBadgePrefab != null)
            {
                badgeObj = Instantiate(achievementBadgePrefab, achievementContainer);
            }
            else
            {
                badgeObj = new GameObject($"Badge_{intruderId}");
                badgeObj.transform.SetParent(achievementContainer);

                var image = badgeObj.AddComponent<Image>();
                image.color = GetBadgeColor(badge);

                var rectTransform = badgeObj.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(40, 40);
            }
        }

        /// <summary>
        /// Get color for badge level
        /// </summary>
        private Color GetBadgeColor(CodexBadge badge)
        {
            return badge switch
            {
                CodexBadge.Bronze => new Color(0.8f, 0.5f, 0.2f),
                CodexBadge.Silver => new Color(0.75f, 0.75f, 0.75f),
                CodexBadge.Gold => new Color(1f, 0.84f, 0f),
                _ => Color.gray
            };
        }

        // Event handlers
        private void HandleXPChanged(int oldXP, int newXP)
        {
            if (gameObject.activeInHierarchy)
            {
                RefreshProfile();
            }
        }

        private void HandleRankChanged(int oldRank, int newRank)
        {
            if (gameObject.activeInHierarchy)
            {
                RefreshProfile();
            }
        }

        private void HandleCoinsChanged(int newCoins)
        {
            if (coinCountText != null)
            {
                coinCountText.text = $"{newCoins:N0}";
            }
        }

        // Button handlers
        private void OnBackClicked()
        {
            BottomNavigationUI.Instance?.BackToPlay();
        }

        private void OnShareClicked()
        {
            // Generate shareable stats
            var data = PlayerDataManager.Instance?.Data;
            if (data == null) return;

            string shareText = $"I'm a {PlayerRank.GetRankTitle(data.currentRank)} in Past Guardians! " +
                             $"I've returned {data.totalReturns} intruders to Tempus Prime! " +
                             $"#PastGuardians #TempusPrime";

            Debug.Log($"[Profile] Share: {shareText}");

            // Could use native share sheet here
            // NativeShare.Share(shareText);
        }

        private void OnSettingsClicked()
        {
            // Open settings panel
            Debug.Log("[Profile] Settings clicked");
        }

        private void OnResetDataClicked()
        {
            // Confirm before reset
            Debug.Log("[Profile] Reset data requested - requires confirmation");
            // Would show confirmation dialog, then call:
            // PlayerDataManager.Instance?.ResetPlayerData();
        }

        /// <summary>
        /// Get formatted stats for display
        /// </summary>
        public string GetFormattedStats()
        {
            var data = PlayerDataManager.Instance?.Data;
            if (data == null) return "No data";

            return $"Rank: {PlayerRank.GetRankTitle(data.currentRank)}\n" +
                   $"XP: {data.currentXP:N0}\n" +
                   $"Returns: {data.totalReturns:N0}\n" +
                   $"Taps: {data.totalTaps:N0}";
        }
    }
}
