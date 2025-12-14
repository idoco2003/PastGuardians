using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using PastGuardians.Gameplay;
using PastGuardians.Data;

namespace PastGuardians.UI
{
    /// <summary>
    /// Toast notification system for game events
    /// Shows brief messages when intruders appear, rewards earned, etc.
    /// </summary>
    public class ToastNotification : MonoBehaviour
    {
        public static ToastNotification Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float displayDuration = 3f;
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.5f;

        // UI References
        private Canvas canvas;
        private GameObject toastPanel;
        private TextMeshProUGUI toastText;
        private Image toastBackground;
        private Image toastIcon;
        private CanvasGroup canvasGroup;

        // Queue for multiple toasts
        private Queue<ToastData> toastQueue = new Queue<ToastData>();
        private bool isShowingToast;

        // Colors for different eras
        private readonly Dictionary<IntruderEra, Color> eraColors = new Dictionary<IntruderEra, Color>
        {
            { IntruderEra.Prehistoric, new Color(1f, 0.75f, 0f) },      // Amber
            { IntruderEra.Mythological, new Color(0.6f, 0.2f, 1f) },    // Purple
            { IntruderEra.HistoricalMachines, new Color(0.8f, 0.5f, 0.2f) }, // Bronze
            { IntruderEra.LostCivilizations, new Color(0f, 0.5f, 0.5f) },    // Teal
            { IntruderEra.TimeAnomalies, new Color(0.8f, 0.4f, 0.8f) }       // Prismatic
        };

        private struct ToastData
        {
            public string message;
            public Color color;
            public float duration;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (Instance != null) return;

            GameObject obj = new GameObject("ToastNotification");
            obj.AddComponent<ToastNotification>();
            DontDestroyOnLoad(obj);
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
            CreateUI();
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        /// <summary>
        /// Create the toast UI
        /// </summary>
        private void CreateUI()
        {
            // Canvas
            GameObject canvasObj = new GameObject("ToastCanvas");
            canvasObj.transform.SetParent(transform);

            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500; // Above most UI but below age gate

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Toast Panel (at top of screen)
            toastPanel = new GameObject("ToastPanel");
            toastPanel.transform.SetParent(canvasObj.transform, false);

            RectTransform panelRect = toastPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.1f, 0.85f);
            panelRect.anchorMax = new Vector2(0.9f, 0.92f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Background
            toastBackground = toastPanel.AddComponent<Image>();
            toastBackground.color = new Color(0.1f, 0.1f, 0.2f, 0.9f);

            // Canvas Group for fading
            canvasGroup = toastPanel.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;

            // Icon (left side)
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(toastPanel.transform, false);

            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0f);
            iconRect.anchorMax = new Vector2(0.15f, 1f);
            iconRect.offsetMin = new Vector2(10, 10);
            iconRect.offsetMax = new Vector2(-5, -10);

            toastIcon = iconObj.AddComponent<Image>();
            toastIcon.color = Color.white;

            // Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(toastPanel.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.15f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = new Vector2(10, 5);
            textRect.offsetMax = new Vector2(-10, -5);

            toastText = textObj.AddComponent<TextMeshProUGUI>();
            toastText.fontSize = 24;
            toastText.color = Color.white;
            toastText.alignment = TextAlignmentOptions.Left;
            toastText.verticalAlignment = VerticalAlignmentOptions.Middle;

            // Hide initially
            toastPanel.SetActive(false);
        }

        /// <summary>
        /// Subscribe to game events
        /// </summary>
        private void SubscribeToEvents()
        {
            // Wait for spawner to be ready
            StartCoroutine(WaitAndSubscribe());
        }

        private IEnumerator WaitAndSubscribe()
        {
            // Wait for IntruderSpawner
            while (IntruderSpawner.Instance == null)
            {
                yield return new WaitForSeconds(0.5f);
            }

            IntruderSpawner.Instance.OnIntruderSpawned += OnIntruderSpawned;
            IntruderSpawner.Instance.OnIntruderReturned += OnIntruderReturned;
        }

        private void UnsubscribeFromEvents()
        {
            if (IntruderSpawner.Instance != null)
            {
                IntruderSpawner.Instance.OnIntruderSpawned -= OnIntruderSpawned;
                IntruderSpawner.Instance.OnIntruderReturned -= OnIntruderReturned;
            }
        }

        /// <summary>
        /// Handle new intruder spawned
        /// </summary>
        private void OnIntruderSpawned(Intruder intruder)
        {
            if (intruder?.Data == null) return;

            string eraName = intruder.Data.era switch
            {
                IntruderEra.Prehistoric => "Prehistoric",
                IntruderEra.Mythological => "Mythological",
                IntruderEra.HistoricalMachines => "Historical",
                IntruderEra.LostCivilizations => "Ancient",
                IntruderEra.TimeAnomalies => "Temporal",
                _ => "Unknown"
            };

            Color color = eraColors.GetValueOrDefault(intruder.Data.era, Color.white);

            string sizeWarning = intruder.Data.sizeClass switch
            {
                IntruderSize.Boss => " [BOSS]",
                IntruderSize.Large => " [Large]",
                _ => ""
            };

            ShowToast($"{eraName} intruder detected: {intruder.Data.displayName}{sizeWarning}", color);
        }

        /// <summary>
        /// Handle intruder returned
        /// </summary>
        private void OnIntruderReturned(Intruder intruder)
        {
            if (intruder?.Data == null) return;

            ShowToast($"{intruder.Data.displayName} returned to Tempus Prime!", Color.green, 2f);
        }

        /// <summary>
        /// Show a toast notification
        /// </summary>
        public void ShowToast(string message, Color? color = null, float duration = 0f)
        {
            toastQueue.Enqueue(new ToastData
            {
                message = message,
                color = color ?? Color.white,
                duration = duration > 0 ? duration : displayDuration
            });

            if (!isShowingToast)
            {
                StartCoroutine(ProcessToastQueue());
            }
        }

        /// <summary>
        /// Show intruder arrival toast
        /// </summary>
        public void ShowIntruderArrival(string intruderName, IntruderEra era)
        {
            Color color = eraColors.GetValueOrDefault(era, Color.white);
            ShowToast($"New intruder: {intruderName}", color);
        }

        /// <summary>
        /// Process toast queue
        /// </summary>
        private IEnumerator ProcessToastQueue()
        {
            isShowingToast = true;

            while (toastQueue.Count > 0)
            {
                ToastData data = toastQueue.Dequeue();

                // Update UI
                toastText.text = data.message;
                toastBackground.color = new Color(data.color.r * 0.3f, data.color.g * 0.3f, data.color.b * 0.3f, 0.9f);
                toastIcon.color = data.color;

                // Show panel
                toastPanel.SetActive(true);

                // Fade in
                float elapsed = 0f;
                while (elapsed < fadeInDuration)
                {
                    elapsed += Time.deltaTime;
                    canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                    yield return null;
                }
                canvasGroup.alpha = 1f;

                // Wait
                yield return new WaitForSeconds(data.duration);

                // Fade out
                elapsed = 0f;
                while (elapsed < fadeOutDuration)
                {
                    elapsed += Time.deltaTime;
                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
                    yield return null;
                }
                canvasGroup.alpha = 0f;

                // Small delay between toasts
                yield return new WaitForSeconds(0.2f);
            }

            toastPanel.SetActive(false);
            isShowingToast = false;
        }

        /// <summary>
        /// Clear all pending toasts
        /// </summary>
        public void ClearToasts()
        {
            toastQueue.Clear();
            StopAllCoroutines();
            isShowingToast = false;
            if (toastPanel != null)
            {
                toastPanel.SetActive(false);
            }
        }
    }
}
