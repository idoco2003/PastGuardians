using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PastGuardians.Gameplay;
using System;

namespace PastGuardians.UI
{
    /// <summary>
    /// Data about another player
    /// </summary>
    [Serializable]
    public class OtherPlayerData
    {
        public string playerId;
        public string displayName;
        public string city;
        public string country;
        public int rank;
        public Color beamColor;
        public int tapContribution;
    }

    /// <summary>
    /// UI panel for interacting with other players when clicking their laser
    /// </summary>
    public class PlayerInteractionUI : MonoBehaviour
    {
        public static PlayerInteractionUI Instance { get; private set; }

        [Header("Main Panel")]
        [SerializeField] private GameObject interactionPanel;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Player Info")]
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI playerLocationText;
        [SerializeField] private TextMeshProUGUI playerRankText;
        [SerializeField] private Image beamColorPreview;
        [SerializeField] private TextMeshProUGUI contributionText;

        [Header("Action Buttons")]
        [SerializeField] private Button inviteButton;
        [SerializeField] private Button challengeButton;
        [SerializeField] private Button closeButton;

        [Header("Invite Panel")]
        [SerializeField] private GameObject invitePanel;
        [SerializeField] private TextMeshProUGUI inviteStatusText;
        [SerializeField] private Button cancelInviteButton;

        [Header("Challenge Panel")]
        [SerializeField] private GameObject challengePanel;
        [SerializeField] private TextMeshProUGUI challengeDescriptionText;
        [SerializeField] private Button confirmChallengeButton;
        [SerializeField] private Button cancelChallengeButton;

        [Header("Settings")]
        [SerializeField] private float autoCloseDelay = 5f;

        // State
        private OtherPlayerData currentPlayer;
        private bool isOpen;
        private float openTime;

        // Events
        public event Action<OtherPlayerData> OnPlayerInvited;
        public event Action<OtherPlayerData> OnPlayerChallenged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (interactionPanel != null)
            {
                interactionPanel.SetActive(false);
            }
        }

        private void Start()
        {
            SetupButtons();
            HideAllSubPanels();
        }

        private void Update()
        {
            // Auto-close after delay if no interaction
            if (isOpen && Time.time - openTime > autoCloseDelay)
            {
                Close();
            }
        }

        /// <summary>
        /// Setup button listeners
        /// </summary>
        private void SetupButtons()
        {
            if (inviteButton != null) inviteButton.onClick.AddListener(OnInviteClicked);
            if (challengeButton != null) challengeButton.onClick.AddListener(OnChallengeClicked);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (cancelInviteButton != null) cancelInviteButton.onClick.AddListener(CancelInvite);
            if (confirmChallengeButton != null) confirmChallengeButton.onClick.AddListener(ConfirmChallenge);
            if (cancelChallengeButton != null) cancelChallengeButton.onClick.AddListener(CancelChallenge);
        }

        /// <summary>
        /// Show interaction panel for a player
        /// </summary>
        public void ShowForPlayer(OtherPlayerData player)
        {
            if (player == null) return;

            currentPlayer = player;
            isOpen = true;
            openTime = Time.time;

            if (interactionPanel != null)
            {
                interactionPanel.SetActive(true);
            }

            UpdatePlayerInfo();
            HideAllSubPanels();

            Debug.Log($"[PlayerInteraction] Showing panel for {player.displayName}");
        }

        /// <summary>
        /// Show interaction panel for a laser (extract player data from laser)
        /// </summary>
        public void ShowForLaser(Gameplay.LaserData laserData)
        {
            if (laserData == null) return;

            // Create player data from laser info
            OtherPlayerData player = new OtherPlayerData
            {
                playerId = laserData.playerId,
                displayName = $"Guardian",
                city = laserData.city,
                country = "",
                rank = UnityEngine.Random.Range(1, 10),  // Would come from server
                beamColor = laserData.color,
                tapContribution = UnityEngine.Random.Range(5, 50)  // Would come from server
            };

            ShowForPlayer(player);
        }

        /// <summary>
        /// Update player info display
        /// </summary>
        private void UpdatePlayerInfo()
        {
            if (currentPlayer == null) return;

            // Name
            if (playerNameText != null)
            {
                playerNameText.text = currentPlayer.displayName;
            }

            // Location
            if (playerLocationText != null)
            {
                string location = currentPlayer.city;
                if (!string.IsNullOrEmpty(currentPlayer.country))
                {
                    location += $", {currentPlayer.country}";
                }
                playerLocationText.text = location;
            }

            // Rank
            if (playerRankText != null)
            {
                playerRankText.text = $"Rank {currentPlayer.rank}";
            }

            // Beam color
            if (beamColorPreview != null)
            {
                beamColorPreview.color = currentPlayer.beamColor;
            }

            // Contribution
            if (contributionText != null)
            {
                contributionText.text = $"{currentPlayer.tapContribution} taps";
            }
        }

        /// <summary>
        /// Close the interaction panel
        /// </summary>
        public void Close()
        {
            isOpen = false;
            currentPlayer = null;

            if (interactionPanel != null)
            {
                interactionPanel.SetActive(false);
            }

            HideAllSubPanels();
        }

        /// <summary>
        /// Hide all sub-panels
        /// </summary>
        private void HideAllSubPanels()
        {
            if (invitePanel != null) invitePanel.SetActive(false);
            if (challengePanel != null) challengePanel.SetActive(false);
        }

        /// <summary>
        /// Handle invite button click
        /// </summary>
        private void OnInviteClicked()
        {
            if (currentPlayer == null) return;

            // Show invite confirmation
            if (invitePanel != null)
            {
                invitePanel.SetActive(true);
            }

            if (inviteStatusText != null)
            {
                inviteStatusText.text = $"Sending invite to {currentPlayer.displayName}...";
            }

            // Send invite (would go to server)
            SendInvite();
        }

        /// <summary>
        /// Send invite to player
        /// </summary>
        private void SendInvite()
        {
            if (currentPlayer == null) return;

            // In real implementation, this would send to server
            Debug.Log($"[PlayerInteraction] Sending invite to {currentPlayer.playerId}");

            // Simulate invite sent
            Invoke(nameof(InviteSent), 1f);
        }

        /// <summary>
        /// Called when invite is sent
        /// </summary>
        private void InviteSent()
        {
            if (inviteStatusText != null)
            {
                inviteStatusText.text = $"Invite sent to {currentPlayer?.displayName}!";
            }

            OnPlayerInvited?.Invoke(currentPlayer);

            // Close after delay
            Invoke(nameof(Close), 2f);
        }

        /// <summary>
        /// Cancel invite
        /// </summary>
        private void CancelInvite()
        {
            if (invitePanel != null)
            {
                invitePanel.SetActive(false);
            }
        }

        /// <summary>
        /// Handle challenge button click
        /// </summary>
        private void OnChallengeClicked()
        {
            if (currentPlayer == null) return;

            // Show challenge confirmation
            if (challengePanel != null)
            {
                challengePanel.SetActive(true);
            }

            if (challengeDescriptionText != null)
            {
                challengeDescriptionText.text =
                    $"Challenge {currentPlayer.displayName} to see who can tap faster!\n\n" +
                    $"The winner gets bonus XP and coins.";
            }
        }

        /// <summary>
        /// Confirm challenge
        /// </summary>
        private void ConfirmChallenge()
        {
            if (currentPlayer == null) return;

            // In real implementation, this would send to server
            Debug.Log($"[PlayerInteraction] Challenging {currentPlayer.playerId}");

            OnPlayerChallenged?.Invoke(currentPlayer);

            // Update UI
            if (challengeDescriptionText != null)
            {
                challengeDescriptionText.text =
                    $"Challenge sent to {currentPlayer.displayName}!\n\n" +
                    $"Waiting for response...";
            }

            if (confirmChallengeButton != null)
            {
                confirmChallengeButton.gameObject.SetActive(false);
            }

            // Close after delay
            Invoke(nameof(Close), 3f);
        }

        /// <summary>
        /// Cancel challenge
        /// </summary>
        private void CancelChallenge()
        {
            if (challengePanel != null)
            {
                challengePanel.SetActive(false);
            }

            if (confirmChallengeButton != null)
            {
                confirmChallengeButton.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Check if panel is open
        /// </summary>
        public bool IsOpen => isOpen;

        /// <summary>
        /// Reset auto-close timer (call when user interacts)
        /// </summary>
        public void ResetAutoCloseTimer()
        {
            openTime = Time.time;
        }
    }
}
