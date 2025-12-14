using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace PastGuardians.UI
{
    /// <summary>
    /// Navigation tabs at bottom of screen
    /// </summary>
    public enum NavigationTab
    {
        Play,
        Codex,
        Shop,
        Profile
    }

    /// <summary>
    /// Bottom navigation bar for switching between game screens
    /// </summary>
    public class BottomNavigationUI : MonoBehaviour
    {
        public static BottomNavigationUI Instance { get; private set; }

        [Header("Tab Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button codexButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button profileButton;

        [Header("Tab Icons")]
        [SerializeField] private Image playIcon;
        [SerializeField] private Image codexIcon;
        [SerializeField] private Image shopIcon;
        [SerializeField] private Image profileIcon;

        [Header("Tab Labels")]
        [SerializeField] private TextMeshProUGUI playLabel;
        [SerializeField] private TextMeshProUGUI codexLabel;
        [SerializeField] private TextMeshProUGUI shopLabel;
        [SerializeField] private TextMeshProUGUI profileLabel;

        [Header("Panels")]
        [SerializeField] private GameObject playPanel;
        [SerializeField] private GameObject codexPanel;
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private GameObject profilePanel;

        [Header("Colors")]
        [SerializeField] private Color activeColor = new Color(0f, 0.83f, 1f);  // Cyan
        [SerializeField] private Color inactiveColor = new Color(0.5f, 0.5f, 0.5f);  // Gray

        [Header("Navigation Bar")]
        [SerializeField] private GameObject navigationBar;
        [SerializeField] private bool hideNavInPlayMode = true;

        // Current state
        private NavigationTab currentTab = NavigationTab.Play;

        // Events
        public event Action<NavigationTab> OnTabChanged;

        public NavigationTab CurrentTab => currentTab;

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
            if (playButton != null) playButton.onClick.AddListener(() => SwitchToTab(NavigationTab.Play));
            if (codexButton != null) codexButton.onClick.AddListener(() => SwitchToTab(NavigationTab.Codex));
            if (shopButton != null) shopButton.onClick.AddListener(() => SwitchToTab(NavigationTab.Shop));
            if (profileButton != null) profileButton.onClick.AddListener(() => SwitchToTab(NavigationTab.Profile));

            // Initialize to Play tab
            SwitchToTab(NavigationTab.Play);
        }

        /// <summary>
        /// Switch to a specific tab
        /// </summary>
        public void SwitchToTab(NavigationTab tab)
        {
            currentTab = tab;

            // Update panels
            UpdatePanelVisibility();

            // Update button states
            UpdateButtonStates();

            // Handle navigation bar visibility
            if (hideNavInPlayMode && navigationBar != null)
            {
                navigationBar.SetActive(tab != NavigationTab.Play);
            }

            OnTabChanged?.Invoke(tab);
            Debug.Log($"[BottomNav] Switched to {tab}");
        }

        /// <summary>
        /// Update which panel is visible
        /// </summary>
        private void UpdatePanelVisibility()
        {
            if (playPanel != null) playPanel.SetActive(currentTab == NavigationTab.Play);
            if (codexPanel != null) codexPanel.SetActive(currentTab == NavigationTab.Codex);
            if (shopPanel != null) shopPanel.SetActive(currentTab == NavigationTab.Shop);
            if (profilePanel != null) profilePanel.SetActive(currentTab == NavigationTab.Profile);
        }

        /// <summary>
        /// Update button visual states
        /// </summary>
        private void UpdateButtonStates()
        {
            // Play
            UpdateTabVisual(playIcon, playLabel, currentTab == NavigationTab.Play);

            // Codex
            UpdateTabVisual(codexIcon, codexLabel, currentTab == NavigationTab.Codex);

            // Shop
            UpdateTabVisual(shopIcon, shopLabel, currentTab == NavigationTab.Shop);

            // Profile
            UpdateTabVisual(profileIcon, profileLabel, currentTab == NavigationTab.Profile);
        }

        /// <summary>
        /// Update individual tab visual
        /// </summary>
        private void UpdateTabVisual(Image icon, TextMeshProUGUI label, bool isActive)
        {
            Color targetColor = isActive ? activeColor : inactiveColor;

            if (icon != null) icon.color = targetColor;
            if (label != null) label.color = targetColor;
        }

        /// <summary>
        /// Show the navigation bar
        /// </summary>
        public void ShowNavigationBar()
        {
            if (navigationBar != null)
            {
                navigationBar.SetActive(true);
            }
        }

        /// <summary>
        /// Hide the navigation bar
        /// </summary>
        public void HideNavigationBar()
        {
            if (navigationBar != null)
            {
                navigationBar.SetActive(false);
            }
        }

        /// <summary>
        /// Toggle navigation bar visibility
        /// </summary>
        public void ToggleNavigationBar()
        {
            if (navigationBar != null)
            {
                navigationBar.SetActive(!navigationBar.activeSelf);
            }
        }

        /// <summary>
        /// Go back to play mode
        /// </summary>
        public void BackToPlay()
        {
            SwitchToTab(NavigationTab.Play);
        }

        /// <summary>
        /// Check if currently in play mode
        /// </summary>
        public bool IsInPlayMode()
        {
            return currentTab == NavigationTab.Play;
        }
    }
}
