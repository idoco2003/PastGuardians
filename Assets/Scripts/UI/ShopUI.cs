using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PastGuardians.Core;
using PastGuardians.Data;
using System;
using System.Collections.Generic;

namespace PastGuardians.UI
{
    /// <summary>
    /// Data for a shop item
    /// </summary>
    [Serializable]
    public class ShopItem
    {
        public string itemId;
        public string displayName;
        public string description;
        public int price;
        public Sprite icon;
        public Color beamColor;  // For beam color items
        public bool isPremium;
        public int requiredRank;  // 0 = no requirement
    }

    /// <summary>
    /// Category of shop items
    /// </summary>
    public enum ShopCategory
    {
        BeamColors,
        BeamEffects,
        Badges,
        Special
    }

    /// <summary>
    /// Shop UI for purchasing cosmetics and beam colors
    /// </summary>
    public class ShopUI : MonoBehaviour
    {
        public static ShopUI Instance { get; private set; }

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI coinDisplayText;
        [SerializeField] private Button backButton;

        [Header("Category Tabs")]
        [SerializeField] private Button beamColorsTab;
        [SerializeField] private Button beamEffectsTab;
        [SerializeField] private Button badgesTab;
        [SerializeField] private Button specialTab;

        [Header("Item Grid")]
        [SerializeField] private Transform itemContainer;
        [SerializeField] private GameObject shopItemPrefab;

        [Header("Item Detail Panel")]
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private Image detailIcon;
        [SerializeField] private TextMeshProUGUI detailName;
        [SerializeField] private TextMeshProUGUI detailDescription;
        [SerializeField] private TextMeshProUGUI detailPrice;
        [SerializeField] private Button purchaseButton;
        [SerializeField] private Button equipButton;
        [SerializeField] private TextMeshProUGUI purchaseButtonText;

        [Header("Beam Preview")]
        [SerializeField] private Image beamPreviewImage;
        [SerializeField] private LineRenderer beamPreviewLine;

        [Header("Default Beam Colors")]
        [SerializeField] private List<ShopItem> defaultBeamColors = new List<ShopItem>();

        // State
        private ShopCategory currentCategory = ShopCategory.BeamColors;
        private ShopItem selectedItem;
        private List<GameObject> spawnedItems = new List<GameObject>();

        // Events
        public event Action<ShopItem> OnItemPurchased;
        public event Action<ShopItem> OnItemEquipped;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            InitializeDefaultItems();
        }

        private void Start()
        {
            // Setup button listeners
            if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
            if (beamColorsTab != null) beamColorsTab.onClick.AddListener(() => SwitchCategory(ShopCategory.BeamColors));
            if (beamEffectsTab != null) beamEffectsTab.onClick.AddListener(() => SwitchCategory(ShopCategory.BeamEffects));
            if (badgesTab != null) badgesTab.onClick.AddListener(() => SwitchCategory(ShopCategory.Badges));
            if (specialTab != null) specialTab.onClick.AddListener(() => SwitchCategory(ShopCategory.Special));
            if (purchaseButton != null) purchaseButton.onClick.AddListener(OnPurchaseClicked);
            if (equipButton != null) equipButton.onClick.AddListener(OnEquipClicked);

            // Subscribe to coin changes
            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.OnCoinsChanged += UpdateCoinDisplay;
            }

            // Initialize
            UpdateCoinDisplay(PlayerDataManager.Instance?.GetCoins() ?? 0);
            HideDetailPanel();
            SwitchCategory(ShopCategory.BeamColors);
        }

        private void OnDestroy()
        {
            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.OnCoinsChanged -= UpdateCoinDisplay;
            }
        }

        /// <summary>
        /// Initialize default beam color items
        /// </summary>
        private void InitializeDefaultItems()
        {
            if (defaultBeamColors.Count == 0)
            {
                // Add default beam colors
                defaultBeamColors.Add(new ShopItem
                {
                    itemId = "beam_cyan",
                    displayName = "Cyan Beam",
                    description = "The classic Guardian beam color",
                    price = 0,
                    beamColor = new Color(0f, 0.83f, 1f)
                });

                defaultBeamColors.Add(new ShopItem
                {
                    itemId = "beam_gold",
                    displayName = "Golden Ray",
                    description = "A prestigious golden beam",
                    price = 500,
                    beamColor = new Color(1f, 0.84f, 0f)
                });

                defaultBeamColors.Add(new ShopItem
                {
                    itemId = "beam_purple",
                    displayName = "Mystic Purple",
                    description = "Channel mythological energy",
                    price = 500,
                    beamColor = new Color(0.61f, 0.19f, 1f)
                });

                defaultBeamColors.Add(new ShopItem
                {
                    itemId = "beam_green",
                    displayName = "Nature's Light",
                    description = "The power of prehistoric forests",
                    price = 500,
                    beamColor = new Color(0.2f, 1f, 0.4f)
                });

                defaultBeamColors.Add(new ShopItem
                {
                    itemId = "beam_red",
                    displayName = "Crimson Blaze",
                    description = "Fierce and powerful",
                    price = 750,
                    beamColor = new Color(1f, 0.2f, 0.2f)
                });

                defaultBeamColors.Add(new ShopItem
                {
                    itemId = "beam_pink",
                    displayName = "Rose Glow",
                    description = "Soft and elegant",
                    price = 750,
                    beamColor = new Color(1f, 0.4f, 0.7f)
                });

                defaultBeamColors.Add(new ShopItem
                {
                    itemId = "beam_orange",
                    displayName = "Amber Pulse",
                    description = "Prehistoric power",
                    price = 750,
                    beamColor = new Color(1f, 0.5f, 0f)
                });

                defaultBeamColors.Add(new ShopItem
                {
                    itemId = "beam_white",
                    displayName = "Pure Light",
                    description = "The essence of Tempus Prime",
                    price = 1000,
                    beamColor = Color.white,
                    requiredRank = 5
                });

                defaultBeamColors.Add(new ShopItem
                {
                    itemId = "beam_rainbow",
                    displayName = "Prismatic Beam",
                    description = "Legendary shifting colors",
                    price = 2500,
                    beamColor = Color.white,  // Special handling for rainbow
                    requiredRank = 8,
                    isPremium = true
                });
            }
        }

        /// <summary>
        /// Switch to a shop category
        /// </summary>
        public void SwitchCategory(ShopCategory category)
        {
            currentCategory = category;
            RefreshItemGrid();
            HideDetailPanel();
        }

        /// <summary>
        /// Refresh the item grid display
        /// </summary>
        private void RefreshItemGrid()
        {
            // Clear existing items
            foreach (var item in spawnedItems)
            {
                if (item != null) Destroy(item);
            }
            spawnedItems.Clear();

            // Get items for current category
            List<ShopItem> items = GetItemsForCategory(currentCategory);

            // Spawn item buttons
            foreach (var item in items)
            {
                SpawnItemButton(item);
            }
        }

        /// <summary>
        /// Get items for a category
        /// </summary>
        private List<ShopItem> GetItemsForCategory(ShopCategory category)
        {
            switch (category)
            {
                case ShopCategory.BeamColors:
                    return defaultBeamColors;
                case ShopCategory.BeamEffects:
                    return new List<ShopItem>(); // TODO: Add beam effects
                case ShopCategory.Badges:
                    return new List<ShopItem>(); // TODO: Add badges
                case ShopCategory.Special:
                    return new List<ShopItem>(); // TODO: Add special items
                default:
                    return new List<ShopItem>();
            }
        }

        /// <summary>
        /// Spawn a button for a shop item
        /// </summary>
        private void SpawnItemButton(ShopItem item)
        {
            if (itemContainer == null) return;

            GameObject buttonObj;

            if (shopItemPrefab != null)
            {
                buttonObj = Instantiate(shopItemPrefab, itemContainer);
            }
            else
            {
                // Create simple button if no prefab
                buttonObj = new GameObject($"ShopItem_{item.itemId}");
                buttonObj.transform.SetParent(itemContainer);

                var image = buttonObj.AddComponent<Image>();
                image.color = item.beamColor;

                var button = buttonObj.AddComponent<Button>();

                var rectTransform = buttonObj.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(80, 80);
            }

            // Setup button click
            var btn = buttonObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => SelectItem(item));
            }

            // Show color preview for beam colors
            var img = buttonObj.GetComponent<Image>();
            if (img != null && currentCategory == ShopCategory.BeamColors)
            {
                img.color = item.beamColor;
            }

            // Show owned/equipped state
            bool isOwned = IsItemOwned(item);
            bool isEquipped = IsItemEquipped(item);

            // Add visual indicator if owned
            if (isOwned)
            {
                // Could add checkmark overlay here
            }

            spawnedItems.Add(buttonObj);
        }

        /// <summary>
        /// Select an item to view details
        /// </summary>
        public void SelectItem(ShopItem item)
        {
            selectedItem = item;
            ShowDetailPanel(item);
        }

        /// <summary>
        /// Show item detail panel
        /// </summary>
        private void ShowDetailPanel(ShopItem item)
        {
            if (detailPanel != null) detailPanel.SetActive(true);

            if (detailName != null) detailName.text = item.displayName;
            if (detailDescription != null) detailDescription.text = item.description;

            bool isOwned = IsItemOwned(item);
            bool isEquipped = IsItemEquipped(item);
            bool canAfford = PlayerDataManager.Instance?.GetCoins() >= item.price;
            bool meetsRankReq = PlayerDataManager.Instance?.Data?.currentRank >= item.requiredRank;

            // Update price display
            if (detailPrice != null)
            {
                if (isOwned)
                {
                    detailPrice.text = "Owned";
                    detailPrice.color = Color.green;
                }
                else if (item.price == 0)
                {
                    detailPrice.text = "Free";
                    detailPrice.color = Color.green;
                }
                else
                {
                    detailPrice.text = $"{item.price} Coins";
                    detailPrice.color = canAfford ? Color.white : Color.red;
                }
            }

            // Update buttons
            if (purchaseButton != null)
            {
                purchaseButton.gameObject.SetActive(!isOwned);
                purchaseButton.interactable = canAfford && meetsRankReq;
            }

            if (equipButton != null)
            {
                equipButton.gameObject.SetActive(isOwned);
                equipButton.interactable = !isEquipped;

                var equipText = equipButton.GetComponentInChildren<TextMeshProUGUI>();
                if (equipText != null)
                {
                    equipText.text = isEquipped ? "Equipped" : "Equip";
                }
            }

            // Update beam preview
            if (beamPreviewImage != null)
            {
                beamPreviewImage.color = item.beamColor;
            }
        }

        /// <summary>
        /// Hide detail panel
        /// </summary>
        private void HideDetailPanel()
        {
            if (detailPanel != null) detailPanel.SetActive(false);
            selectedItem = null;
        }

        /// <summary>
        /// Update coin display
        /// </summary>
        private void UpdateCoinDisplay(int coins)
        {
            if (coinDisplayText != null)
            {
                coinDisplayText.text = $"{coins:N0}";
            }
        }

        /// <summary>
        /// Check if item is owned
        /// </summary>
        private bool IsItemOwned(ShopItem item)
        {
            if (item.price == 0) return true;  // Free items are always owned
            return PlayerDataManager.Instance?.IsCosmeticUnlocked(item.itemId) ?? false;
        }

        /// <summary>
        /// Check if item is currently equipped
        /// </summary>
        private bool IsItemEquipped(ShopItem item)
        {
            if (currentCategory == ShopCategory.BeamColors)
            {
                var currentColor = PlayerDataManager.Instance?.Data?.selectedBeamColor ?? Color.cyan;
                return ColorsSimilar(currentColor, item.beamColor);
            }
            return false;
        }

        /// <summary>
        /// Check if two colors are similar
        /// </summary>
        private bool ColorsSimilar(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.1f &&
                   Mathf.Abs(a.g - b.g) < 0.1f &&
                   Mathf.Abs(a.b - b.b) < 0.1f;
        }

        /// <summary>
        /// Handle purchase button click
        /// </summary>
        private void OnPurchaseClicked()
        {
            if (selectedItem == null) return;

            if (PlayerDataManager.Instance == null) return;

            // Check if can afford
            if (PlayerDataManager.Instance.GetCoins() < selectedItem.price)
            {
                Debug.Log("[Shop] Not enough coins!");
                return;
            }

            // Check rank requirement
            if (PlayerDataManager.Instance.Data.currentRank < selectedItem.requiredRank)
            {
                Debug.Log($"[Shop] Requires rank {selectedItem.requiredRank}!");
                return;
            }

            // Purchase
            if (PlayerDataManager.Instance.SpendCoins(selectedItem.price))
            {
                PlayerDataManager.Instance.UnlockCosmetic(selectedItem.itemId);
                OnItemPurchased?.Invoke(selectedItem);
                Debug.Log($"[Shop] Purchased {selectedItem.displayName}!");

                // Refresh display
                ShowDetailPanel(selectedItem);
                RefreshItemGrid();
            }
        }

        /// <summary>
        /// Handle equip button click
        /// </summary>
        private void OnEquipClicked()
        {
            if (selectedItem == null) return;

            if (currentCategory == ShopCategory.BeamColors)
            {
                PlayerDataManager.Instance?.SetBeamColor(selectedItem.beamColor);
                OnItemEquipped?.Invoke(selectedItem);
                Debug.Log($"[Shop] Equipped {selectedItem.displayName}!");

                // Refresh display
                ShowDetailPanel(selectedItem);
            }
        }

        /// <summary>
        /// Handle back button click
        /// </summary>
        private void OnBackClicked()
        {
            BottomNavigationUI.Instance?.BackToPlay();
        }
    }
}
