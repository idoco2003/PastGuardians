using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace PastGuardians.UI
{
    /// <summary>
    /// Age verification gate for COPPA compliance
    /// Shown on first launch to verify player's age
    /// </summary>
    public class AgeGateUI : MonoBehaviour
    {
        public static AgeGateUI Instance { get; private set; }

        private const string AGE_VERIFIED_KEY = "PastGuardians_AgeVerified";
        private const string IS_CHILD_KEY = "PastGuardians_IsChild";

        [Header("Main Panel")]
        private GameObject ageGatePanel;
        private GameObject mainContent;

        [Header("Colors")]
        private Color backgroundColor = new Color(0.05f, 0.1f, 0.2f, 0.98f);
        private Color primaryColor = new Color(0f, 0.83f, 1f);
        private Color buttonColor = new Color(0.1f, 0.4f, 0.8f);

        // State
        private bool isVerified;
        private bool isChild;
        private Action onVerificationComplete;

        public bool IsVerified => isVerified;
        public bool IsChild => isChild;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            // Check if already verified
            if (PlayerPrefs.GetInt(AGE_VERIFIED_KEY, 0) == 1)
            {
                Debug.Log("[AgeGate] Already verified, skipping");
                return;
            }

            // Create age gate
            GameObject gateObj = new GameObject("AgeGateUI");
            gateObj.AddComponent<AgeGateUI>();
            DontDestroyOnLoad(gateObj);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Check saved verification
            isVerified = PlayerPrefs.GetInt(AGE_VERIFIED_KEY, 0) == 1;
            isChild = PlayerPrefs.GetInt(IS_CHILD_KEY, 0) == 1;
        }

        private void Start()
        {
            if (!isVerified)
            {
                CreateUI();
                ShowAgeGate();
            }
        }

        /// <summary>
        /// Create the UI elements
        /// </summary>
        private void CreateUI()
        {
            // Create Canvas
            GameObject canvasObj = new GameObject("AgeGateCanvas");
            canvasObj.transform.SetParent(transform);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // On top of everything

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Background panel
            ageGatePanel = new GameObject("AgeGatePanel");
            ageGatePanel.transform.SetParent(canvasObj.transform, false);

            RectTransform panelRect = ageGatePanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelBg = ageGatePanel.AddComponent<Image>();
            panelBg.color = backgroundColor;

            // Main content container
            mainContent = new GameObject("MainContent");
            mainContent.transform.SetParent(ageGatePanel.transform, false);

            RectTransform contentRect = mainContent.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.1f, 0.2f);
            contentRect.anchorMax = new Vector2(0.9f, 0.8f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            // Logo/Title
            CreateTitle();

            // Age question
            CreateAgeQuestion();
        }

        /// <summary>
        /// Create title section
        /// </summary>
        private void CreateTitle()
        {
            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(mainContent.transform, false);

            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.85f);
            titleRect.anchorMax = new Vector2(0.5f, 0.85f);
            titleRect.sizeDelta = new Vector2(600, 100);

            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "PAST GUARDIANS";
            titleText.fontSize = 48;
            titleText.color = primaryColor;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontStyle = FontStyles.Bold;

            // Subtitle
            GameObject subtitleObj = new GameObject("Subtitle");
            subtitleObj.transform.SetParent(mainContent.transform, false);

            RectTransform subtitleRect = subtitleObj.AddComponent<RectTransform>();
            subtitleRect.anchorMin = new Vector2(0.5f, 0.75f);
            subtitleRect.anchorMax = new Vector2(0.5f, 0.75f);
            subtitleRect.sizeDelta = new Vector2(600, 60);

            TextMeshProUGUI subtitleText = subtitleObj.AddComponent<TextMeshProUGUI>();
            subtitleText.text = "Protect the Sky. Unite the World.";
            subtitleText.fontSize = 24;
            subtitleText.color = Color.white;
            subtitleText.alignment = TextAlignmentOptions.Center;
        }

        /// <summary>
        /// Create age verification question
        /// </summary>
        private void CreateAgeQuestion()
        {
            // Question text
            GameObject questionObj = new GameObject("Question");
            questionObj.transform.SetParent(mainContent.transform, false);

            RectTransform questionRect = questionObj.AddComponent<RectTransform>();
            questionRect.anchorMin = new Vector2(0.5f, 0.55f);
            questionRect.anchorMax = new Vector2(0.5f, 0.55f);
            questionRect.sizeDelta = new Vector2(600, 80);

            TextMeshProUGUI questionText = questionObj.AddComponent<TextMeshProUGUI>();
            questionText.text = "Please verify your age to continue:";
            questionText.fontSize = 28;
            questionText.color = Color.white;
            questionText.alignment = TextAlignmentOptions.Center;

            // Age buttons
            CreateAgeButton("I am 13 or older", 0.4f, () => OnAgeSelected(false));
            CreateAgeButton("I am under 13", 0.25f, () => OnAgeSelected(true));

            // Privacy notice
            GameObject privacyObj = new GameObject("Privacy");
            privacyObj.transform.SetParent(mainContent.transform, false);

            RectTransform privacyRect = privacyObj.AddComponent<RectTransform>();
            privacyRect.anchorMin = new Vector2(0.5f, 0.08f);
            privacyRect.anchorMax = new Vector2(0.5f, 0.08f);
            privacyRect.sizeDelta = new Vector2(600, 100);

            TextMeshProUGUI privacyText = privacyObj.AddComponent<TextMeshProUGUI>();
            privacyText.text = "We collect only anonymous gameplay data.\nNo personal information is stored.\nCity-level location used for gameplay only.";
            privacyText.fontSize = 16;
            privacyText.color = new Color(0.7f, 0.7f, 0.7f);
            privacyText.alignment = TextAlignmentOptions.Center;
        }

        /// <summary>
        /// Create an age selection button
        /// </summary>
        private void CreateAgeButton(string text, float yPosition, Action onClick)
        {
            GameObject buttonObj = new GameObject($"Button_{text}");
            buttonObj.transform.SetParent(mainContent.transform, false);

            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, yPosition);
            buttonRect.anchorMax = new Vector2(0.5f, yPosition);
            buttonRect.sizeDelta = new Vector2(400, 70);

            Image buttonBg = buttonObj.AddComponent<Image>();
            buttonBg.color = buttonColor;

            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = buttonBg;

            // Button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = text;
            buttonText.fontSize = 24;
            buttonText.color = Color.white;
            buttonText.alignment = TextAlignmentOptions.Center;

            // Click handler
            button.onClick.AddListener(() => onClick?.Invoke());
        }

        /// <summary>
        /// Handle age selection
        /// </summary>
        private void OnAgeSelected(bool isUnder13)
        {
            isChild = isUnder13;
            isVerified = true;

            // Save to PlayerPrefs
            PlayerPrefs.SetInt(AGE_VERIFIED_KEY, 1);
            PlayerPrefs.SetInt(IS_CHILD_KEY, isChild ? 1 : 0);
            PlayerPrefs.Save();

            if (isChild)
            {
                ShowParentalNotice();
            }
            else
            {
                CompleteVerification();
            }

            Debug.Log($"[AgeGate] Age verified. IsChild: {isChild}");
        }

        /// <summary>
        /// Show parental consent notice for under 13
        /// </summary>
        private void ShowParentalNotice()
        {
            // Clear current content
            foreach (Transform child in mainContent.transform)
            {
                Destroy(child.gameObject);
            }

            // Parental notice title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(mainContent.transform, false);

            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.8f);
            titleRect.anchorMax = new Vector2(0.5f, 0.8f);
            titleRect.sizeDelta = new Vector2(600, 60);

            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "Welcome, Young Guardian!";
            titleText.fontSize = 32;
            titleText.color = primaryColor;
            titleText.alignment = TextAlignmentOptions.Center;

            // Notice text
            GameObject noticeObj = new GameObject("Notice");
            noticeObj.transform.SetParent(mainContent.transform, false);

            RectTransform noticeRect = noticeObj.AddComponent<RectTransform>();
            noticeRect.anchorMin = new Vector2(0.5f, 0.5f);
            noticeRect.anchorMax = new Vector2(0.5f, 0.5f);
            noticeRect.sizeDelta = new Vector2(600, 300);

            TextMeshProUGUI noticeText = noticeObj.AddComponent<TextMeshProUGUI>();
            noticeText.text = "Enhanced Privacy Mode Active\n\n" +
                              "Your privacy is protected:\n" +
                              "- No personal data collected\n" +
                              "- Location shown as city only\n" +
                              "- No chat or messaging\n" +
                              "- Anonymous gameplay\n\n" +
                              "Have fun protecting the sky!";
            noticeText.fontSize = 22;
            noticeText.color = Color.white;
            noticeText.alignment = TextAlignmentOptions.Center;

            // Continue button
            CreateAgeButton("Start Playing!", 0.15f, CompleteVerification);
        }

        /// <summary>
        /// Complete verification and close gate
        /// </summary>
        private void CompleteVerification()
        {
            onVerificationComplete?.Invoke();
            HideAgeGate();
        }

        /// <summary>
        /// Show the age gate
        /// </summary>
        public void ShowAgeGate(Action onComplete = null)
        {
            onVerificationComplete = onComplete;

            if (ageGatePanel != null)
            {
                ageGatePanel.SetActive(true);
            }
        }

        /// <summary>
        /// Hide the age gate
        /// </summary>
        public void HideAgeGate()
        {
            if (ageGatePanel != null)
            {
                Destroy(ageGatePanel.transform.parent.gameObject); // Destroy canvas
            }
            Destroy(gameObject);
        }

        /// <summary>
        /// Reset verification (for testing)
        /// </summary>
        public static void ResetVerification()
        {
            PlayerPrefs.DeleteKey(AGE_VERIFIED_KEY);
            PlayerPrefs.DeleteKey(IS_CHILD_KEY);
            PlayerPrefs.Save();
            Debug.Log("[AgeGate] Verification reset");
        }
    }
}
