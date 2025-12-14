using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace PastGuardians.UI
{
    /// <summary>
    /// UI component for displaying location labels near laser origins
    /// </summary>
    public class LocationLabelUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image backgroundImage;

        [Header("Settings")]
        [SerializeField] private float fadeDuration = 0.3f;
        [SerializeField] private Vector2 offset = new Vector2(10f, 10f);

        [Header("Styling")]
        [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.7f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private float fontSize = 14f;

        // State
        private string currentCity;
        private string currentCountry;
        private Coroutine fadeCoroutine;

        // Properties
        public string City => currentCity;
        public string Country => currentCountry;
        public string DisplayText => GetDisplayText();

        private void Awake()
        {
            SetupComponents();
        }

        /// <summary>
        /// Setup UI components if not assigned
        /// </summary>
        private void SetupComponents()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (labelText == null)
            {
                labelText = GetComponentInChildren<TextMeshProUGUI>();
                if (labelText == null)
                {
                    GameObject textObj = new GameObject("LabelText");
                    textObj.transform.SetParent(transform);
                    labelText = textObj.AddComponent<TextMeshProUGUI>();
                }
            }

            // Apply styling
            labelText.color = textColor;
            labelText.fontSize = fontSize;
            labelText.alignment = TextAlignmentOptions.Center;

            if (backgroundImage != null)
            {
                backgroundImage.color = backgroundColor;
            }
        }

        /// <summary>
        /// Set the label text
        /// </summary>
        public void SetLabel(string city, string country)
        {
            currentCity = city;
            currentCountry = country;

            if (labelText != null)
            {
                labelText.text = GetDisplayText();
            }
        }

        /// <summary>
        /// Get formatted display text
        /// </summary>
        private string GetDisplayText()
        {
            if (string.IsNullOrEmpty(currentCity) && string.IsNullOrEmpty(currentCountry))
                return "Unknown";

            if (string.IsNullOrEmpty(currentCity))
                return currentCountry;

            if (string.IsNullOrEmpty(currentCountry))
                return currentCity;

            return $"{currentCity}, {currentCountry}";
        }

        /// <summary>
        /// Set position on screen
        /// </summary>
        public void SetPosition(Vector2 screenPos)
        {
            if (rectTransform == null) return;

            // Apply offset to avoid overlapping with laser origin
            Vector2 adjustedPos = screenPos + offset;

            // Keep within screen bounds
            float halfWidth = rectTransform.rect.width / 2f;
            float halfHeight = rectTransform.rect.height / 2f;

            adjustedPos.x = Mathf.Clamp(adjustedPos.x, halfWidth, Screen.width - halfWidth);
            adjustedPos.y = Mathf.Clamp(adjustedPos.y, halfHeight, Screen.height - halfHeight);

            rectTransform.position = adjustedPos;
        }

        /// <summary>
        /// Update position smoothly
        /// </summary>
        public void UpdatePosition(Vector2 screenPos, float smoothTime = 0.1f)
        {
            StartCoroutine(SmoothMoveCoroutine(screenPos, smoothTime));
        }

        /// <summary>
        /// Smooth position update coroutine
        /// </summary>
        private IEnumerator SmoothMoveCoroutine(Vector2 targetPos, float duration)
        {
            Vector2 startPos = rectTransform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                rectTransform.position = Vector2.Lerp(startPos, targetPos + offset, t);
                yield return null;
            }

            SetPosition(targetPos);
        }

        /// <summary>
        /// Fade in the label
        /// </summary>
        public void FadeIn(float duration = -1f)
        {
            if (duration < 0) duration = fadeDuration;

            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeCoroutine(0f, 1f, duration));
        }

        /// <summary>
        /// Fade out the label
        /// </summary>
        public void FadeOut(float duration = -1f)
        {
            if (duration < 0) duration = fadeDuration;

            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeCoroutine(canvasGroup.alpha, 0f, duration));
        }

        /// <summary>
        /// Fade animation coroutine
        /// </summary>
        private IEnumerator FadeCoroutine(float startAlpha, float endAlpha, float duration)
        {
            if (canvasGroup == null) yield break;

            float elapsed = 0f;
            canvasGroup.alpha = startAlpha;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = endAlpha;
            fadeCoroutine = null;

            // Disable if faded out
            if (endAlpha <= 0f)
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Show immediately
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
        }

        /// <summary>
        /// Hide immediately
        /// </summary>
        public void Hide()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Set text color
        /// </summary>
        public void SetTextColor(Color color)
        {
            textColor = color;
            if (labelText != null)
            {
                labelText.color = color;
            }
        }

        /// <summary>
        /// Set background color
        /// </summary>
        public void SetBackgroundColor(Color color)
        {
            backgroundColor = color;
            if (backgroundImage != null)
            {
                backgroundImage.color = color;
            }
        }

        /// <summary>
        /// Set font size
        /// </summary>
        public void SetFontSize(float size)
        {
            fontSize = size;
            if (labelText != null)
            {
                labelText.fontSize = size;
            }
        }
    }
}
