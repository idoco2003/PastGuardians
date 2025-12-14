using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PastGuardians.Data;
using PastGuardians.Core;
using System.Collections.Generic;

namespace PastGuardians.UI
{
    /// <summary>
    /// Collection screen showing all discovered intruders
    /// </summary>
    public class CodexUI : MonoBehaviour
    {
        public static CodexUI Instance { get; private set; }

        [Header("Main Panel")]
        [SerializeField] private GameObject codexPanel;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Filter Tabs")]
        [SerializeField] private Button allErasButton;
        [SerializeField] private Button prehistoricButton;
        [SerializeField] private Button mythologicalButton;
        [SerializeField] private Button machinesButton;
        [SerializeField] private Button civilizationsButton;
        [SerializeField] private Button anomaliesButton;

        [Header("Grid")]
        [SerializeField] private Transform gridContainer;
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private int cardsPerRow = 3;

        [Header("Stats")]
        [SerializeField] private TextMeshProUGUI totalDiscoveredText;
        [SerializeField] private TextMeshProUGUI totalReturnsText;

        [Header("Detail View")]
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private Image detailIcon;
        [SerializeField] private TextMeshProUGUI detailNameText;
        [SerializeField] private TextMeshProUGUI detailEraText;
        [SerializeField] private TextMeshProUGUI detailDescriptionText;
        [SerializeField] private TextMeshProUGUI detailReturnCountText;
        [SerializeField] private Image detailBadgeIcon;
        [SerializeField] private TextMeshProUGUI detailBadgeText;
        [SerializeField] private Transform modelViewerContainer;
        [SerializeField] private Button closeDetailButton;

        [Header("Close Button")]
        [SerializeField] private Button closeButton;

        [Header("Data")]
        [SerializeField] private List<IntruderData> allIntruderTypes = new List<IntruderData>();

        // State
        private bool isOpen;
        private IntruderEra? currentFilter;
        private List<CodexCard> activeCards = new List<CodexCard>();
        private IntruderData selectedIntruder;

        // Badge colors
        private static readonly Color BronzeColor = new Color(0.8f, 0.5f, 0.2f);
        private static readonly Color SilverColor = new Color(0.75f, 0.75f, 0.8f);
        private static readonly Color GoldColor = new Color(1f, 0.84f, 0f);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (codexPanel != null)
            {
                codexPanel.SetActive(false);
            }
        }

        private void Start()
        {
            SetupButtons();
        }

        /// <summary>
        /// Setup button listeners
        /// </summary>
        private void SetupButtons()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(OnBackClicked);
            }

            if (closeDetailButton != null)
            {
                closeDetailButton.onClick.AddListener(CloseDetailView);
            }

            // Filter buttons
            if (allErasButton != null)
                allErasButton.onClick.AddListener(() => FilterByEra(null));

            if (prehistoricButton != null)
                prehistoricButton.onClick.AddListener(() => FilterByEra(IntruderEra.Prehistoric));

            if (mythologicalButton != null)
                mythologicalButton.onClick.AddListener(() => FilterByEra(IntruderEra.Mythological));

            if (machinesButton != null)
                machinesButton.onClick.AddListener(() => FilterByEra(IntruderEra.HistoricalMachines));

            if (civilizationsButton != null)
                civilizationsButton.onClick.AddListener(() => FilterByEra(IntruderEra.LostCivilizations));

            if (anomaliesButton != null)
                anomaliesButton.onClick.AddListener(() => FilterByEra(IntruderEra.TimeAnomalies));
        }

        /// <summary>
        /// Open the codex
        /// </summary>
        public void Open()
        {
            isOpen = true;

            if (codexPanel != null)
            {
                codexPanel.SetActive(true);
            }

            PopulateCodex();
            UpdateStats();
            CloseDetailView();
        }

        /// <summary>
        /// Close the codex
        /// </summary>
        public void Close()
        {
            isOpen = false;

            if (codexPanel != null)
            {
                codexPanel.SetActive(false);
            }

            CloseDetailView();
        }

        /// <summary>
        /// Toggle codex
        /// </summary>
        public void Toggle()
        {
            if (isOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        /// <summary>
        /// Populate codex grid
        /// </summary>
        public void PopulateCodex()
        {
            if (PlayerDataManager.Instance?.Data == null) return;

            var progress = PlayerDataManager.Instance.Data.codexProgress;

            // Clear existing cards
            ClearCards();

            // Filter intruder types
            List<IntruderData> filteredTypes = new List<IntruderData>();
            foreach (var type in allIntruderTypes)
            {
                if (currentFilter == null || type.era == currentFilter.Value)
                {
                    filteredTypes.Add(type);
                }
            }

            // Create cards
            foreach (var type in filteredTypes)
            {
                int returnCount = 0;
                progress.TryGetValue(type.intruderId, out returnCount);

                CreateCard(type, returnCount);
            }
        }

        /// <summary>
        /// Filter by era
        /// </summary>
        public void FilterByEra(IntruderEra? era)
        {
            currentFilter = era;
            PopulateCodex();
            UpdateFilterButtonHighlights();
        }

        /// <summary>
        /// Update filter button highlights
        /// </summary>
        private void UpdateFilterButtonHighlights()
        {
            // Could implement visual highlighting of selected filter
        }

        /// <summary>
        /// Clear all cards
        /// </summary>
        private void ClearCards()
        {
            foreach (var card in activeCards)
            {
                if (card != null && card.gameObject != null)
                {
                    Destroy(card.gameObject);
                }
            }
            activeCards.Clear();
        }

        /// <summary>
        /// Create a card for an intruder type
        /// </summary>
        private void CreateCard(IntruderData type, int returnCount)
        {
            if (gridContainer == null || cardPrefab == null) return;

            GameObject cardObj = Instantiate(cardPrefab, gridContainer);
            CodexCard card = cardObj.GetComponent<CodexCard>();

            if (card == null)
            {
                card = cardObj.AddComponent<CodexCard>();
            }

            bool isDiscovered = returnCount > 0;
            CodexBadge badge = GetBadgeForCount(returnCount);

            card.Setup(type, returnCount, isDiscovered, badge, OnCardClicked);
            activeCards.Add(card);
        }

        /// <summary>
        /// Handle card click
        /// </summary>
        private void OnCardClicked(IntruderData type)
        {
            OpenDetailView(type);
        }

        /// <summary>
        /// Open detail view for intruder
        /// </summary>
        public void OpenDetailView(IntruderData intruder)
        {
            if (intruder == null || detailPanel == null) return;

            selectedIntruder = intruder;
            detailPanel.SetActive(true);

            // Get return count
            int returnCount = 0;
            if (PlayerDataManager.Instance?.Data != null)
            {
                PlayerDataManager.Instance.Data.codexProgress.TryGetValue(intruder.intruderId, out returnCount);
            }

            bool isDiscovered = returnCount > 0;

            // Name
            if (detailNameText != null)
            {
                detailNameText.text = isDiscovered ? intruder.displayName : "???";
            }

            // Era
            if (detailEraText != null)
            {
                detailEraText.text = GetEraName(intruder.era);
                detailEraText.color = intruder.GetEraPortalColor();
            }

            // Description
            if (detailDescriptionText != null)
            {
                detailDescriptionText.text = isDiscovered
                    ? intruder.codexDescription
                    : "Return this intruder to unlock its lore.";
            }

            // Return count
            if (detailReturnCountText != null)
            {
                if (isDiscovered)
                {
                    detailReturnCountText.text = $"You've returned {returnCount} of these!";
                }
                else
                {
                    detailReturnCountText.text = "Not yet discovered";
                }
            }

            // Badge
            CodexBadge badge = GetBadgeForCount(returnCount);
            UpdateBadgeDisplay(badge, returnCount);

            // Model viewer
            SetupModelViewer(intruder, isDiscovered);
        }

        /// <summary>
        /// Close detail view
        /// </summary>
        public void CloseDetailView()
        {
            if (detailPanel != null)
            {
                detailPanel.SetActive(false);
            }

            selectedIntruder = null;

            // Clear model viewer
            if (modelViewerContainer != null)
            {
                foreach (Transform child in modelViewerContainer)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        /// <summary>
        /// Update badge display
        /// </summary>
        private void UpdateBadgeDisplay(CodexBadge badge, int returnCount)
        {
            if (detailBadgeIcon != null)
            {
                detailBadgeIcon.gameObject.SetActive(badge != CodexBadge.None);

                detailBadgeIcon.color = badge switch
                {
                    CodexBadge.Bronze => BronzeColor,
                    CodexBadge.Silver => SilverColor,
                    CodexBadge.Gold => GoldColor,
                    _ => Color.gray
                };
            }

            if (detailBadgeText != null)
            {
                switch (badge)
                {
                    case CodexBadge.Gold:
                        detailBadgeText.text = "GOLD - Master (100+)";
                        break;
                    case CodexBadge.Silver:
                        detailBadgeText.text = "SILVER - Expert (50+)";
                        break;
                    case CodexBadge.Bronze:
                        detailBadgeText.text = "BRONZE - Familiar (10+)";
                        break;
                    default:
                        int toNextBadge = 10 - returnCount;
                        detailBadgeText.text = returnCount > 0
                            ? $"{toNextBadge} more for Bronze badge"
                            : "Discover to start progress";
                        break;
                }
            }
        }

        /// <summary>
        /// Setup model viewer with intruder model
        /// </summary>
        private void SetupModelViewer(IntruderData intruder, bool isDiscovered)
        {
            if (modelViewerContainer == null) return;

            // Clear existing
            foreach (Transform child in modelViewerContainer)
            {
                Destroy(child.gameObject);
            }

            if (!isDiscovered || intruder.modelPrefab == null)
            {
                // Show silhouette or placeholder
                GameObject placeholder = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                placeholder.transform.SetParent(modelViewerContainer);
                placeholder.transform.localPosition = Vector3.zero;
                placeholder.transform.localScale = Vector3.one * 0.5f;

                var renderer = placeholder.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = isDiscovered ? intruder.GetEraPortalColor() : Color.black;
                }
            }
            else
            {
                // Instantiate actual model
                GameObject model = Instantiate(intruder.modelPrefab, modelViewerContainer);
                model.transform.localPosition = Vector3.zero;

                // Add rotation script
                model.AddComponent<ModelRotator>();
            }
        }

        /// <summary>
        /// Update stats display
        /// </summary>
        private void UpdateStats()
        {
            if (PlayerDataManager.Instance?.Data == null) return;

            var data = PlayerDataManager.Instance.Data;

            // Count discovered
            int discovered = 0;
            foreach (var kvp in data.codexProgress)
            {
                if (kvp.Value > 0)
                {
                    discovered++;
                }
            }

            if (totalDiscoveredText != null)
            {
                totalDiscoveredText.text = $"Discovered: {discovered}/{allIntruderTypes.Count}";
            }

            if (totalReturnsText != null)
            {
                totalReturnsText.text = $"Total Returns: {data.totalReturns:N0}";
            }
        }

        /// <summary>
        /// Get badge for return count
        /// </summary>
        private CodexBadge GetBadgeForCount(int count)
        {
            if (count >= 100) return CodexBadge.Gold;
            if (count >= 50) return CodexBadge.Silver;
            if (count >= 10) return CodexBadge.Bronze;
            return CodexBadge.None;
        }

        /// <summary>
        /// Get era display name
        /// </summary>
        private string GetEraName(IntruderEra era)
        {
            return era switch
            {
                IntruderEra.Prehistoric => "Prehistoric Era",
                IntruderEra.Mythological => "Mythological",
                IntruderEra.HistoricalMachines => "Historical Machines",
                IntruderEra.LostCivilizations => "Lost Civilizations",
                IntruderEra.TimeAnomalies => "Time Anomalies",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Add intruder type to codex
        /// </summary>
        public void RegisterIntruderType(IntruderData type)
        {
            if (type != null && !allIntruderTypes.Contains(type))
            {
                allIntruderTypes.Add(type);
            }
        }

        /// <summary>
        /// Check if codex is open
        /// </summary>
        public bool IsOpen => isOpen;

        /// <summary>
        /// Handle back button - return to play mode
        /// </summary>
        private void OnBackClicked()
        {
            Close();
            BottomNavigationUI.Instance?.BackToPlay();
        }
    }

    /// <summary>
    /// Individual codex card component
    /// </summary>
    public class CodexCard : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private Image badgeImage;
        [SerializeField] private Button button;
        [SerializeField] private GameObject lockedOverlay;

        private IntruderData intruderData;
        private System.Action<IntruderData> onClickCallback;

        public void Setup(IntruderData data, int returnCount, bool isDiscovered, CodexBadge badge, System.Action<IntruderData> onClick)
        {
            intruderData = data;
            onClickCallback = onClick;

            // Setup visuals
            if (nameText != null)
            {
                nameText.text = isDiscovered ? data.displayName : "???";
            }

            if (countText != null)
            {
                countText.text = isDiscovered ? returnCount.ToString() : "";
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = isDiscovered ? data.GetEraPortalColor() : Color.gray;
            }

            if (lockedOverlay != null)
            {
                lockedOverlay.SetActive(!isDiscovered);
            }

            if (badgeImage != null)
            {
                badgeImage.gameObject.SetActive(badge != CodexBadge.None);
                badgeImage.color = badge switch
                {
                    CodexBadge.Bronze => new Color(0.8f, 0.5f, 0.2f),
                    CodexBadge.Silver => new Color(0.75f, 0.75f, 0.8f),
                    CodexBadge.Gold => new Color(1f, 0.84f, 0f),
                    _ => Color.clear
                };
            }

            // Button
            if (button != null)
            {
                button.onClick.AddListener(OnClick);
            }
        }

        private void OnClick()
        {
            onClickCallback?.Invoke(intruderData);
        }
    }

    /// <summary>
    /// Simple model rotator for codex detail view
    /// </summary>
    public class ModelRotator : MonoBehaviour
    {
        [SerializeField] private float rotationSpeed = 30f;

        private void Update()
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }
}
