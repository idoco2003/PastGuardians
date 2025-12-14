using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PastGuardians.UI
{
    /// <summary>
    /// Automatically creates the game UI at runtime
    /// Runs automatically when game starts - no setup needed!
    /// </summary>
    public class UIAutoSetup : MonoBehaviour
    {
        public static UIAutoSetup Instance { get; private set; }

        [Header("Colors")]
        private Color backgroundColor = new Color(0.05f, 0.08f, 0.12f, 0.95f);
        private Color primaryColor = new Color(0f, 0.83f, 1f);  // Cyan
        private Color textColor = Color.white;
        private Color inactiveColor = new Color(0.4f, 0.4f, 0.4f);

        // Created objects
        private Canvas mainCanvas;
        private GameObject bottomNavBar;
        private GameObject playPanel;
        private GameObject codexPanel;
        private GameObject shopPanel;
        private GameObject profilePanel;

        /// <summary>
        /// Automatically runs when game starts
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            // Check if already exists
            if (Instance != null) return;

            // Create the UIAutoSetup object
            GameObject setupObj = new GameObject("UIAutoSetup");
            UIAutoSetup setup = setupObj.AddComponent<UIAutoSetup>();
            DontDestroyOnLoad(setupObj);

            Debug.Log("[UIAutoSetup] Auto-initializing UI...");
        }

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
            CreateAllUI();
        }

        /// <summary>
        /// Create all UI elements
        /// </summary>
        [ContextMenu("Create All UI")]
        public void CreateAllUI()
        {
            CreateMainCanvas();
            CreatePanels();
            CreateBottomNavBar();
            SetupNavigation();

            Debug.Log("[UIAutoSetup] UI created successfully!");
        }

        /// <summary>
        /// Create the main canvas
        /// </summary>
        private void CreateMainCanvas()
        {
            // Check if canvas already exists
            mainCanvas = FindObjectOfType<Canvas>();
            if (mainCanvas != null)
            {
                Debug.Log("[UIAutoSetup] Using existing canvas");
                return;
            }

            GameObject canvasObj = new GameObject("MainCanvas");
            mainCanvas = canvasObj.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainCanvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            Debug.Log("[UIAutoSetup] Created main canvas");
        }

        /// <summary>
        /// Create the content panels
        /// </summary>
        private void CreatePanels()
        {
            // Play Panel (main game HUD)
            playPanel = CreatePanel("PlayPanel", true);
            CreatePlayPanelContent(playPanel);

            // Codex Panel
            codexPanel = CreatePanel("CodexPanel", false);
            CreateCodexPanelContent(codexPanel);

            // Shop Panel
            shopPanel = CreatePanel("ShopPanel", false);
            CreateShopPanelContent(shopPanel);

            // Profile Panel
            profilePanel = CreatePanel("ProfilePanel", false);
            CreateProfilePanelContent(profilePanel);
        }

        /// <summary>
        /// Create a basic panel
        /// </summary>
        private GameObject CreatePanel(string name, bool active)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(mainCanvas.transform, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(0, 120);  // Leave space for bottom nav
            rect.offsetMax = Vector2.zero;

            panel.SetActive(active);
            return panel;
        }

        /// <summary>
        /// Create Play panel content
        /// </summary>
        private void CreatePlayPanelContent(GameObject panel)
        {
            // Top info bar
            GameObject topBar = CreateRect(panel, "TopBar",
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, -120), new Vector2(0, 0));

            Image topBg = topBar.AddComponent<Image>();
            topBg.color = new Color(0, 0, 0, 0.5f);

            // Rank text
            GameObject rankObj = CreateTextObject(topBar, "RankText", "Time Watcher", 24);
            RectTransform rankRect = rankObj.GetComponent<RectTransform>();
            rankRect.anchorMin = new Vector2(0, 0.5f);
            rankRect.anchorMax = new Vector2(0, 0.5f);
            rankRect.anchoredPosition = new Vector2(20, 0);
            rankRect.sizeDelta = new Vector2(300, 40);

            // Coins text
            GameObject coinsObj = CreateTextObject(topBar, "CoinsText", "0 Coins", 20);
            RectTransform coinsRect = coinsObj.GetComponent<RectTransform>();
            coinsRect.anchorMin = new Vector2(1, 0.5f);
            coinsRect.anchorMax = new Vector2(1, 0.5f);
            coinsRect.anchoredPosition = new Vector2(-20, 0);
            coinsRect.sizeDelta = new Vector2(200, 40);
            coinsObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Right;

            // Center message
            GameObject centerMsg = CreateTextObject(panel, "CenterMessage", "Look up at the sky!", 28);
            RectTransform centerRect = centerMsg.GetComponent<RectTransform>();
            centerRect.anchorMin = new Vector2(0.5f, 0.5f);
            centerRect.anchorMax = new Vector2(0.5f, 0.5f);
            centerRect.sizeDelta = new Vector2(400, 60);
            centerMsg.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            // Add MainGameHUD component
            panel.AddComponent<MainGameHUD>();
        }

        /// <summary>
        /// Create Codex panel content
        /// </summary>
        private void CreateCodexPanelContent(GameObject panel)
        {
            // Background
            Image bg = panel.AddComponent<Image>();
            bg.color = backgroundColor;

            // Header
            GameObject header = CreateRect(panel, "Header",
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, -80), new Vector2(0, 0));

            CreateTextObject(header, "Title", "TEMPUS CODEX", 32)
                .GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            // Back button
            CreateBackButton(header);

            // Stats text
            GameObject stats = CreateTextObject(panel, "Stats", "Discovered: 0 / 20", 18);
            RectTransform statsRect = stats.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0.5f, 1);
            statsRect.anchorMax = new Vector2(0.5f, 1);
            statsRect.anchoredPosition = new Vector2(0, -100);

            // Grid area placeholder
            GameObject gridArea = CreateRect(panel, "GridArea",
                new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.85f),
                Vector2.zero, Vector2.zero);

            Image gridBg = gridArea.AddComponent<Image>();
            gridBg.color = new Color(0.1f, 0.1f, 0.15f, 0.5f);

            CreateTextObject(gridArea, "Placeholder", "Intruder collection will appear here", 20)
                .GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        }

        /// <summary>
        /// Create Shop panel content
        /// </summary>
        private void CreateShopPanelContent(GameObject panel)
        {
            // Background
            Image bg = panel.AddComponent<Image>();
            bg.color = backgroundColor;

            // Header
            GameObject header = CreateRect(panel, "Header",
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, -80), new Vector2(0, 0));

            CreateTextObject(header, "Title", "GUARDIAN SHOP", 32)
                .GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            CreateBackButton(header);

            // Coins display
            GameObject coins = CreateTextObject(header, "Coins", "0", 24);
            RectTransform coinsRect = coins.GetComponent<RectTransform>();
            coinsRect.anchorMin = new Vector2(1, 0.5f);
            coinsRect.anchorMax = new Vector2(1, 0.5f);
            coinsRect.anchoredPosition = new Vector2(-60, 0);
            coins.GetComponent<TextMeshProUGUI>().color = new Color(1f, 0.84f, 0f);  // Gold

            // Category tabs
            GameObject tabs = CreateRect(panel, "Tabs",
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, -140), new Vector2(0, -80));

            CreateTabButton(tabs, "BeamColors", "Beam Colors", 0);
            CreateTabButton(tabs, "Effects", "Effects", 1);
            CreateTabButton(tabs, "Badges", "Badges", 2);

            // Item grid placeholder
            GameObject gridArea = CreateRect(panel, "ItemGrid",
                new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.75f),
                Vector2.zero, Vector2.zero);

            // Create some sample beam color items
            CreateBeamColorItems(gridArea);

            // Add ShopUI component
            panel.AddComponent<ShopUI>();
        }

        /// <summary>
        /// Create sample beam color items
        /// </summary>
        private void CreateBeamColorItems(GameObject parent)
        {
            Color[] colors = new Color[]
            {
                new Color(0f, 0.83f, 1f),    // Cyan (free)
                new Color(1f, 0.84f, 0f),    // Gold
                new Color(0.61f, 0.19f, 1f), // Purple
                new Color(0.2f, 1f, 0.4f),   // Green
                new Color(1f, 0.2f, 0.2f),   // Red
                new Color(1f, 0.4f, 0.7f),   // Pink
            };

            string[] names = { "Cyan", "Gold", "Purple", "Green", "Red", "Pink" };
            int[] prices = { 0, 500, 500, 500, 750, 750 };

            for (int i = 0; i < colors.Length; i++)
            {
                int row = i / 3;
                int col = i % 3;

                GameObject item = new GameObject($"Item_{names[i]}");
                item.transform.SetParent(parent.transform, false);

                RectTransform itemRect = item.AddComponent<RectTransform>();
                itemRect.anchorMin = new Vector2(col * 0.33f + 0.02f, 1 - (row + 1) * 0.25f);
                itemRect.anchorMax = new Vector2((col + 1) * 0.33f - 0.02f, 1 - row * 0.25f - 0.02f);
                itemRect.offsetMin = Vector2.zero;
                itemRect.offsetMax = Vector2.zero;

                Image itemBg = item.AddComponent<Image>();
                itemBg.color = new Color(0.15f, 0.15f, 0.2f);

                Button btn = item.AddComponent<Button>();
                btn.targetGraphic = itemBg;

                // Color preview circle
                GameObject colorCircle = new GameObject("ColorCircle");
                colorCircle.transform.SetParent(item.transform, false);
                RectTransform circleRect = colorCircle.AddComponent<RectTransform>();
                circleRect.anchorMin = new Vector2(0.5f, 0.6f);
                circleRect.anchorMax = new Vector2(0.5f, 0.6f);
                circleRect.sizeDelta = new Vector2(60, 60);

                Image circleImg = colorCircle.AddComponent<Image>();
                circleImg.color = colors[i];

                // Price text
                string priceText = prices[i] == 0 ? "FREE" : $"{prices[i]}";
                GameObject priceObj = CreateTextObject(item, "Price", priceText, 16);
                RectTransform priceRect = priceObj.GetComponent<RectTransform>();
                priceRect.anchorMin = new Vector2(0.5f, 0.15f);
                priceRect.anchorMax = new Vector2(0.5f, 0.15f);
                priceObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
                if (prices[i] == 0)
                    priceObj.GetComponent<TextMeshProUGUI>().color = Color.green;
            }
        }

        /// <summary>
        /// Create Profile panel content
        /// </summary>
        private void CreateProfilePanelContent(GameObject panel)
        {
            // Background
            Image bg = panel.AddComponent<Image>();
            bg.color = backgroundColor;

            // Header
            GameObject header = CreateRect(panel, "Header",
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, -80), new Vector2(0, 0));

            CreateTextObject(header, "Title", "GUARDIAN PROFILE", 32)
                .GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            CreateBackButton(header);

            // Rank display
            GameObject rankSection = CreateRect(panel, "RankSection",
                new Vector2(0.1f, 0.7f), new Vector2(0.9f, 0.9f),
                Vector2.zero, Vector2.zero);

            Image rankBg = rankSection.AddComponent<Image>();
            rankBg.color = new Color(0.1f, 0.15f, 0.2f);

            CreateTextObject(rankSection, "RankTitle", "TIME WATCHER", 28)
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 20);

            CreateTextObject(rankSection, "RankNumber", "Rank 1", 20)
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -20);

            // Stats section
            CreateStatRow(panel, "TotalReturns", "Intruders Returned", "0", 0.55f);
            CreateStatRow(panel, "BossReturns", "Bosses Defeated", "0", 0.45f);
            CreateStatRow(panel, "TotalTaps", "Total Taps", "0", 0.35f);
            CreateStatRow(panel, "Coins", "Coins Earned", "0", 0.25f);

            // Add ProfileUI component
            panel.AddComponent<ProfileUI>();
        }

        /// <summary>
        /// Create a stat row
        /// </summary>
        private void CreateStatRow(GameObject parent, string name, string label, string value, float yPos)
        {
            GameObject row = CreateRect(parent, name,
                new Vector2(0.1f, yPos - 0.04f), new Vector2(0.9f, yPos + 0.04f),
                Vector2.zero, Vector2.zero);

            Image rowBg = row.AddComponent<Image>();
            rowBg.color = new Color(0.08f, 0.1f, 0.15f);

            GameObject labelObj = CreateTextObject(row, "Label", label, 18);
            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.5f);
            labelRect.anchorMax = new Vector2(0, 0.5f);
            labelRect.anchoredPosition = new Vector2(20, 0);
            labelObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;

            GameObject valueObj = CreateTextObject(row, "Value", value, 20);
            RectTransform valueRect = valueObj.GetComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(1, 0.5f);
            valueRect.anchorMax = new Vector2(1, 0.5f);
            valueRect.anchoredPosition = new Vector2(-20, 0);
            valueObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Right;
            valueObj.GetComponent<TextMeshProUGUI>().color = primaryColor;
        }

        /// <summary>
        /// Create the bottom navigation bar
        /// </summary>
        private void CreateBottomNavBar()
        {
            bottomNavBar = new GameObject("BottomNavBar");
            bottomNavBar.transform.SetParent(mainCanvas.transform, false);

            RectTransform navRect = bottomNavBar.AddComponent<RectTransform>();
            navRect.anchorMin = new Vector2(0, 0);
            navRect.anchorMax = new Vector2(1, 0);
            navRect.pivot = new Vector2(0.5f, 0);
            navRect.sizeDelta = new Vector2(0, 120);
            navRect.anchoredPosition = Vector2.zero;

            Image navBg = bottomNavBar.AddComponent<Image>();
            navBg.color = new Color(0.08f, 0.1f, 0.14f, 0.98f);

            // Create nav buttons
            CreateNavButton(bottomNavBar, "Play", "PLAY", 0, true);
            CreateNavButton(bottomNavBar, "Codex", "CODEX", 1, false);
            CreateNavButton(bottomNavBar, "Shop", "SHOP", 2, false);
            CreateNavButton(bottomNavBar, "Profile", "PROFILE", 3, false);

            // Add BottomNavigationUI component
            BottomNavigationUI navUI = bottomNavBar.AddComponent<BottomNavigationUI>();
        }

        /// <summary>
        /// Create a navigation button
        /// </summary>
        private GameObject CreateNavButton(GameObject parent, string name, string label, int index, bool isActive)
        {
            GameObject btnObj = new GameObject($"NavBtn_{name}");
            btnObj.transform.SetParent(parent.transform, false);

            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            float width = 1f / 4f;
            btnRect.anchorMin = new Vector2(index * width, 0);
            btnRect.anchorMax = new Vector2((index + 1) * width, 1);
            btnRect.offsetMin = new Vector2(5, 10);
            btnRect.offsetMax = new Vector2(-5, -10);

            Image btnBg = btnObj.AddComponent<Image>();
            btnBg.color = isActive ? new Color(primaryColor.r, primaryColor.g, primaryColor.b, 0.2f) : Color.clear;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnBg;

            // Icon placeholder (circle)
            GameObject icon = new GameObject("Icon");
            icon.transform.SetParent(btnObj.transform, false);
            RectTransform iconRect = icon.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.6f);
            iconRect.anchorMax = new Vector2(0.5f, 0.6f);
            iconRect.sizeDelta = new Vector2(40, 40);

            Image iconImg = icon.AddComponent<Image>();
            iconImg.color = isActive ? primaryColor : inactiveColor;

            // Label
            GameObject labelObj = CreateTextObject(btnObj, "Label", label, 14);
            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 0.2f);
            labelRect.anchorMax = new Vector2(0.5f, 0.2f);
            labelRect.sizeDelta = new Vector2(100, 30);

            TextMeshProUGUI labelText = labelObj.GetComponent<TextMeshProUGUI>();
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = isActive ? primaryColor : inactiveColor;

            return btnObj;
        }

        /// <summary>
        /// Create a tab button
        /// </summary>
        private GameObject CreateTabButton(GameObject parent, string name, string label, int index)
        {
            GameObject btn = new GameObject($"Tab_{name}");
            btn.transform.SetParent(parent.transform, false);

            RectTransform rect = btn.AddComponent<RectTransform>();
            float width = 1f / 3f;
            rect.anchorMin = new Vector2(index * width, 0);
            rect.anchorMax = new Vector2((index + 1) * width, 1);
            rect.offsetMin = new Vector2(5, 5);
            rect.offsetMax = new Vector2(-5, -5);

            Image bg = btn.AddComponent<Image>();
            bg.color = index == 0 ? primaryColor : new Color(0.2f, 0.2f, 0.25f);

            Button button = btn.AddComponent<Button>();
            button.targetGraphic = bg;

            CreateTextObject(btn, "Label", label, 16)
                .GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            return btn;
        }

        /// <summary>
        /// Create back button
        /// </summary>
        private GameObject CreateBackButton(GameObject parent)
        {
            GameObject btn = new GameObject("BackButton");
            btn.transform.SetParent(parent.transform, false);

            RectTransform rect = btn.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.5f);
            rect.anchorMax = new Vector2(0, 0.5f);
            rect.sizeDelta = new Vector2(80, 40);
            rect.anchoredPosition = new Vector2(50, 0);

            Image bg = btn.AddComponent<Image>();
            bg.color = new Color(0.3f, 0.3f, 0.35f);

            Button button = btn.AddComponent<Button>();
            button.targetGraphic = bg;

            CreateTextObject(btn, "Label", "< BACK", 16);

            return btn;
        }

        /// <summary>
        /// Setup navigation logic
        /// </summary>
        private void SetupNavigation()
        {
            // Get references
            BottomNavigationUI navUI = bottomNavBar.GetComponent<BottomNavigationUI>();
            if (navUI == null) return;

            // Wire up buttons manually since we can't use SerializeField at runtime
            Button[] navButtons = bottomNavBar.GetComponentsInChildren<Button>();

            for (int i = 0; i < navButtons.Length; i++)
            {
                int tabIndex = i;
                navButtons[i].onClick.AddListener(() => {
                    SwitchTab(tabIndex);
                });
            }
        }

        /// <summary>
        /// Switch to a tab
        /// </summary>
        private void SwitchTab(int index)
        {
            playPanel.SetActive(index == 0);
            codexPanel.SetActive(index == 1);
            shopPanel.SetActive(index == 2);
            profilePanel.SetActive(index == 3);

            // Update button visuals
            Button[] navButtons = bottomNavBar.GetComponentsInChildren<Button>();
            for (int i = 0; i < navButtons.Length; i++)
            {
                Image bg = navButtons[i].GetComponent<Image>();
                Image icon = navButtons[i].transform.Find("Icon")?.GetComponent<Image>();
                TextMeshProUGUI label = navButtons[i].GetComponentInChildren<TextMeshProUGUI>();

                bool isActive = (i == index);

                if (bg != null)
                    bg.color = isActive ? new Color(primaryColor.r, primaryColor.g, primaryColor.b, 0.2f) : Color.clear;
                if (icon != null)
                    icon.color = isActive ? primaryColor : inactiveColor;
                if (label != null)
                    label.color = isActive ? primaryColor : inactiveColor;
            }

            Debug.Log($"[UIAutoSetup] Switched to tab {index}");
        }

        /// <summary>
        /// Helper to create a RectTransform object
        /// </summary>
        private GameObject CreateRect(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            return obj;
        }

        /// <summary>
        /// Helper to create a text object
        /// </summary>
        private GameObject CreateTextObject(GameObject parent, string name, string text, int fontSize)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent.transform, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(300, 50);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = textColor;
            tmp.alignment = TextAlignmentOptions.Center;

            return textObj;
        }
    }
}
