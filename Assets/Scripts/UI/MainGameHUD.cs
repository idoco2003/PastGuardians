using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PastGuardians.Data;
using PastGuardians.Core;
using PastGuardians.Gameplay;

namespace PastGuardians.UI
{
    /// <summary>
    /// Main gameplay HUD overlay
    /// </summary>
    public class MainGameHUD : MonoBehaviour
    {
        public static MainGameHUD Instance { get; private set; }

        [Header("Player Info Panel")]
        [SerializeField] private TextMeshProUGUI rankTitleText;
        [SerializeField] private TextMeshProUGUI xpText;
        [SerializeField] private Slider xpProgressBar;
        [SerializeField] private Image rankBadge;

        [Header("Intruder Info Panel")]
        [SerializeField] private GameObject intruderInfoPanel;
        [SerializeField] private TextMeshProUGUI intruderNameText;
        [SerializeField] private TextMeshProUGUI intruderEraText;
        [SerializeField] private Slider tapProgressBar;
        [SerializeField] private TextMeshProUGUI tapProgressText;
        [SerializeField] private TextMeshProUGUI participantCountText;
        [SerializeField] private TextMeshProUGUI timeRemainingText;

        [Header("Contribution")]
        [SerializeField] private TextMeshProUGUI contributionText;
        [SerializeField] private TextMeshProUGUI potentialXPText;

        [Header("Look Up Prompt")]
        [SerializeField] private GameObject lookUpPrompt;
        [SerializeField] private TextMeshProUGUI lookUpText;

        [Header("Compass")]
        [SerializeField] private GameObject compassPanel;
        [SerializeField] private TextMeshProUGUI compassDirectionText;
        [SerializeField] private Transform compassNeedle;

        [Header("Settings")]
        [SerializeField] private float updateInterval = 0.1f;

        // Update timing
        private float lastUpdateTime;

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
            // Subscribe to events
            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.OnXPChanged += HandleXPChanged;
                PlayerDataManager.Instance.OnRankChanged += HandleRankChanged;
            }

            if (TapInputHandler.Instance != null)
            {
                TapInputHandler.Instance.OnTargetChanged += HandleTargetChanged;
            }

            if (AR.SkyCameraController.Instance != null)
            {
                AR.SkyCameraController.Instance.OnStartedLookingAtSky += HandleStartedLookingAtSky;
                AR.SkyCameraController.Instance.OnStoppedLookingAtSky += HandleStoppedLookingAtSky;
            }

            if (GlobalLaserDisplay.Instance != null)
            {
                GlobalLaserDisplay.Instance.OnParticipantCountChanged += HandleParticipantCountChanged;
            }

            // Initialize UI
            InitializeUI();
        }

        private void OnDestroy()
        {
            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.OnXPChanged -= HandleXPChanged;
                PlayerDataManager.Instance.OnRankChanged -= HandleRankChanged;
            }

            if (TapInputHandler.Instance != null)
            {
                TapInputHandler.Instance.OnTargetChanged -= HandleTargetChanged;
            }

            if (AR.SkyCameraController.Instance != null)
            {
                AR.SkyCameraController.Instance.OnStartedLookingAtSky -= HandleStartedLookingAtSky;
                AR.SkyCameraController.Instance.OnStoppedLookingAtSky -= HandleStoppedLookingAtSky;
            }

            if (GlobalLaserDisplay.Instance != null)
            {
                GlobalLaserDisplay.Instance.OnParticipantCountChanged -= HandleParticipantCountChanged;
            }
        }

        private void Update()
        {
            if (Time.time - lastUpdateTime > updateInterval)
            {
                lastUpdateTime = Time.time;
                UpdateDynamicElements();
            }
        }

        /// <summary>
        /// Initialize UI elements
        /// </summary>
        private void InitializeUI()
        {
            UpdatePlayerInfo();
            HideIntruderInfo();
            ShowLookUpPrompt(true);
        }

        /// <summary>
        /// Update player info display
        /// </summary>
        public void UpdatePlayerInfo()
        {
            if (PlayerDataManager.Instance?.Data == null) return;

            var data = PlayerDataManager.Instance.Data;

            // Rank title
            if (rankTitleText != null)
            {
                rankTitleText.text = PlayerRank.GetRankTitle(data.currentRank);
            }

            // XP text
            if (xpText != null)
            {
                int nextRankXP = PlayerRank.GetXPForNextRank(data.currentRank);
                xpText.text = $"{data.currentXP:N0} / {nextRankXP:N0} XP";
            }

            // XP progress bar
            if (xpProgressBar != null)
            {
                xpProgressBar.value = PlayerRank.GetProgressToNextRank(data.currentXP, data.currentRank);
            }

            // Rank badge color
            if (rankBadge != null)
            {
                rankBadge.color = PlayerRank.GetRankColor(data.currentRank);
            }
        }

        /// <summary>
        /// Show intruder info panel
        /// </summary>
        public void ShowIntruderInfo(Intruder intruder)
        {
            if (intruder == null || intruder.Data == null) return;

            if (intruderInfoPanel != null)
            {
                intruderInfoPanel.SetActive(true);
            }

            // Name
            if (intruderNameText != null)
            {
                intruderNameText.text = intruder.Data.displayName;
            }

            // Era
            if (intruderEraText != null)
            {
                intruderEraText.text = GetEraDisplayName(intruder.Data.era);
                intruderEraText.color = intruder.Data.GetEraPortalColor();
            }

            UpdateTapProgress(intruder);
        }

        /// <summary>
        /// Hide intruder info panel
        /// </summary>
        public void HideIntruderInfo()
        {
            if (intruderInfoPanel != null)
            {
                intruderInfoPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Update tap progress display
        /// </summary>
        public void UpdateTapProgress(Intruder intruder)
        {
            if (intruder == null) return;

            int current = intruder.TapProgress;
            int required = intruder.TotalTapsRequired;

            // Progress bar
            if (tapProgressBar != null)
            {
                tapProgressBar.value = intruder.ProgressPercent;
            }

            // Progress text
            if (tapProgressText != null)
            {
                tapProgressText.text = $"{current} / {required}";
            }

            // Time remaining
            if (timeRemainingText != null)
            {
                float time = intruder.TimeRemaining;
                if (time > 60)
                {
                    timeRemainingText.text = $"{time / 60f:F0}:{time % 60:00}";
                }
                else
                {
                    timeRemainingText.text = $"{time:F1}s";
                }

                // Color warning when low
                timeRemainingText.color = time < 10f ? Color.red : Color.white;
            }

            // Contribution
            UpdateContribution(intruder);
        }

        /// <summary>
        /// Update contribution display
        /// </summary>
        private void UpdateContribution(Intruder intruder)
        {
            if (TapProgressManager.Instance == null) return;

            float contribution = TapProgressManager.Instance.GetContributionPercent(intruder);
            int potentialXP = TapProgressManager.Instance.CalculatePotentialXP(intruder);

            if (contributionText != null)
            {
                contributionText.text = $"Your contribution: {contribution:P0}";
            }

            if (potentialXPText != null)
            {
                potentialXPText.text = potentialXP > 0 ? $"+{potentialXP} XP" : "";
            }
        }

        /// <summary>
        /// Update participant count display
        /// </summary>
        public void UpdateParticipantCount(int count)
        {
            if (participantCountText != null)
            {
                if (count <= 1)
                {
                    participantCountText.text = "You are defending!";
                }
                else
                {
                    participantCountText.text = $"{count} Guardians defending";
                }
            }
        }

        /// <summary>
        /// Show/hide look up prompt
        /// </summary>
        public void ShowLookUpPrompt(bool show)
        {
            if (lookUpPrompt != null)
            {
                lookUpPrompt.SetActive(show);
            }
        }

        /// <summary>
        /// Update dynamic elements
        /// </summary>
        private void UpdateDynamicElements()
        {
            // Update current target info
            Intruder target = TapInputHandler.Instance?.CurrentTarget;
            if (target != null && target.IsVisible)
            {
                UpdateTapProgress(target);
            }

            // Update compass
            UpdateCompass();
        }

        /// <summary>
        /// Update compass display
        /// </summary>
        private void UpdateCompass()
        {
            if (AR.SkyCameraController.Instance == null) return;

            float heading = AR.SkyCameraController.Instance.CurrentCompassHeading;

            if (compassDirectionText != null)
            {
                compassDirectionText.text = AR.SkyCameraController.Instance.GetCompassDirectionString();
            }

            if (compassNeedle != null)
            {
                compassNeedle.rotation = Quaternion.Euler(0, 0, -heading);
            }
        }

        /// <summary>
        /// Get display name for era
        /// </summary>
        private string GetEraDisplayName(IntruderEra era)
        {
            return era switch
            {
                IntruderEra.Prehistoric => "Prehistoric",
                IntruderEra.Mythological => "Mythological",
                IntruderEra.HistoricalMachines => "Historical Machine",
                IntruderEra.LostCivilizations => "Lost Civilization",
                IntruderEra.TimeAnomalies => "Time Anomaly",
                _ => "Unknown"
            };
        }

        // Event handlers
        private void HandleXPChanged(int oldXP, int newXP)
        {
            UpdatePlayerInfo();
        }

        private void HandleRankChanged(int oldRank, int newRank)
        {
            UpdatePlayerInfo();
            // Could trigger rank up animation here
        }

        private void HandleTargetChanged(Intruder newTarget)
        {
            if (newTarget != null)
            {
                ShowIntruderInfo(newTarget);
            }
            else
            {
                HideIntruderInfo();
            }
        }

        private void HandleStartedLookingAtSky()
        {
            ShowLookUpPrompt(false);
        }

        private void HandleStoppedLookingAtSky()
        {
            ShowLookUpPrompt(true);
        }

        private void HandleParticipantCountChanged(int count)
        {
            UpdateParticipantCount(count + 1);  // +1 for local player
        }

        /// <summary>
        /// Show notification message
        /// </summary>
        public void ShowNotification(string message, float duration = 2f)
        {
            // Could implement a notification system here
            Debug.Log($"[HUD Notification] {message}");
        }
    }
}
