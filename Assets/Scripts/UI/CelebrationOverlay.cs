using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PastGuardians.Data;
using PastGuardians.Gameplay;
using System.Collections;
using System.Collections.Generic;

namespace PastGuardians.UI
{
    /// <summary>
    /// Full-screen celebration overlay when intruder is returned
    /// </summary>
    public class CelebrationOverlay : MonoBehaviour
    {
        public static CelebrationOverlay Instance { get; private set; }

        [Header("Main Panel")]
        [SerializeField] private GameObject celebrationPanel;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Title")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private string titleMessage = "RETURNED TO TEMPUS PRIME";

        [Header("Intruder Info")]
        [SerializeField] private Image intruderIcon;
        [SerializeField] private TextMeshProUGUI intruderNameText;
        [SerializeField] private TextMeshProUGUI intruderEraText;

        [Header("Contribution Stats")]
        [SerializeField] private TextMeshProUGUI contributionText;
        [SerializeField] private TextMeshProUGUI globalEffortText;
        [SerializeField] private TextMeshProUGUI countriesText;

        [Header("XP Display")]
        [SerializeField] private TextMeshProUGUI xpEarnedText;
        [SerializeField] private GameObject xpBreakdownPanel;
        [SerializeField] private TextMeshProUGUI xpBreakdownText;

        [Header("Rank Up")]
        [SerializeField] private GameObject rankUpPanel;
        [SerializeField] private TextMeshProUGUI oldRankText;
        [SerializeField] private TextMeshProUGUI newRankText;
        [SerializeField] private TextMeshProUGUI rankUnlockText;

        [Header("Continue Prompt")]
        [SerializeField] private TextMeshProUGUI continueText;
        [SerializeField] private string continueMessage = "TAP TO CONTINUE";

        [Header("Particles")]
        [SerializeField] private ParticleSystem confettiParticles;

        [Header("Animation Settings")]
        [SerializeField] private float slideInDuration = 0.5f;
        [SerializeField] private float statsCountDuration = 1f;
        [SerializeField] private float autoDismissDelay = 5f;
        [SerializeField] private AnimationCurve slideInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip celebrationSound;
        [SerializeField] private AudioClip rankUpSound;
        [SerializeField] private AudioClip xpTickSound;

        // State
        private bool isShowing;
        private IntruderReturnData currentData;
        private Coroutine dismissCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (celebrationPanel != null)
            {
                celebrationPanel.SetActive(false);
            }
        }

        private void Start()
        {
            // Subscribe to return events
            if (TapProgressManager.Instance != null)
            {
                TapProgressManager.Instance.OnIntruderReturned += HandleIntruderReturned;
            }
        }

        private void OnDestroy()
        {
            if (TapProgressManager.Instance != null)
            {
                TapProgressManager.Instance.OnIntruderReturned -= HandleIntruderReturned;
            }
        }

        private void Update()
        {
            // Check for tap to dismiss
            if (isShowing && (Input.touchCount > 0 || Input.GetMouseButtonDown(0)))
            {
                Hide();
            }
        }

        /// <summary>
        /// Handle intruder returned event
        /// </summary>
        private void HandleIntruderReturned(IntruderReturnData data)
        {
            Show(data);
        }

        /// <summary>
        /// Show celebration overlay
        /// </summary>
        public void Show(IntruderReturnData data)
        {
            if (data == null) return;

            currentData = data;
            isShowing = true;

            if (celebrationPanel != null)
            {
                celebrationPanel.SetActive(true);
            }

            // Play sound
            PlaySound(celebrationSound);

            // Play particles
            if (confettiParticles != null)
            {
                confettiParticles.Play();
            }

            // Start animation
            StartCoroutine(ShowAnimationCoroutine(data));

            // Auto dismiss
            if (dismissCoroutine != null)
            {
                StopCoroutine(dismissCoroutine);
            }
            dismissCoroutine = StartCoroutine(AutoDismissCoroutine());
        }

        /// <summary>
        /// Show animation coroutine
        /// </summary>
        private IEnumerator ShowAnimationCoroutine(IntruderReturnData data)
        {
            // Initial state
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            SetInitialValues();

            // Slide in / fade in
            float elapsed = 0f;
            while (elapsed < slideInDuration)
            {
                elapsed += Time.deltaTime;
                float t = slideInCurve.Evaluate(elapsed / slideInDuration);

                if (canvasGroup != null)
                {
                    canvasGroup.alpha = t;
                }

                yield return null;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            // Animate stats
            yield return StartCoroutine(AnimateStats(data));

            // Show rank up if applicable
            if (data.rankedUp)
            {
                yield return StartCoroutine(ShowRankUp(data));
            }

            // Show continue prompt
            if (continueText != null)
            {
                continueText.gameObject.SetActive(true);
                continueText.text = continueMessage;
            }
        }

        /// <summary>
        /// Set initial values before animation
        /// </summary>
        private void SetInitialValues()
        {
            // Title
            if (titleText != null)
            {
                titleText.text = titleMessage;
            }

            // Intruder info
            if (currentData.intruderType != null)
            {
                if (intruderNameText != null)
                {
                    intruderNameText.text = currentData.intruderType.displayName;
                }

                if (intruderEraText != null)
                {
                    intruderEraText.text = GetEraName(currentData.intruderType.era);
                    intruderEraText.color = currentData.intruderType.GetEraPortalColor();
                }
            }

            // Reset stats to 0
            if (contributionText != null) contributionText.text = "You contributed: 0%";
            if (globalEffortText != null) globalEffortText.text = "0 Guardians";
            if (countriesText != null) countriesText.text = "0 countries";
            if (xpEarnedText != null) xpEarnedText.text = "+0 XP";

            // Hide panels
            if (rankUpPanel != null) rankUpPanel.SetActive(false);
            if (continueText != null) continueText.gameObject.SetActive(false);
        }

        /// <summary>
        /// Animate stats counting up
        /// </summary>
        private IEnumerator AnimateStats(IntruderReturnData data)
        {
            float elapsed = 0f;

            // Calculate target values
            float targetContribution = data.totalTaps > 0 ? (float)data.playerTaps / data.totalTaps * 100f : 0f;
            int targetParticipants = data.participantCount;
            int targetCountries = data.countriesCount;
            int targetXP = data.xpEarned;

            while (elapsed < statsCountDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / statsCountDuration;

                // Contribution
                if (contributionText != null)
                {
                    float currentContrib = Mathf.Lerp(0f, targetContribution, t);
                    contributionText.text = $"You contributed: {currentContrib:F0}% ({Mathf.RoundToInt(data.playerTaps * t)} taps)";
                }

                // Global effort
                if (globalEffortText != null)
                {
                    int currentParticipants = Mathf.RoundToInt(Mathf.Lerp(0, targetParticipants, t));
                    globalEffortText.text = $"{currentParticipants} Guardian{(currentParticipants != 1 ? "s" : "")}";
                }

                // Countries
                if (countriesText != null)
                {
                    int currentCountries = Mathf.RoundToInt(Mathf.Lerp(0, targetCountries, t));
                    countriesText.text = $"from {currentCountries} {(currentCountries != 1 ? "countries" : "country")}";
                }

                // XP
                if (xpEarnedText != null)
                {
                    int currentXP = Mathf.RoundToInt(Mathf.Lerp(0, targetXP, t));
                    xpEarnedText.text = $"+{currentXP} XP";
                }

                // Play tick sound occasionally
                if (Mathf.RoundToInt(t * 20) % 5 == 0)
                {
                    PlaySound(xpTickSound, 0.3f);
                }

                yield return null;
            }

            // Set final values
            if (contributionText != null)
            {
                contributionText.text = $"You contributed: {targetContribution:F0}% ({data.playerTaps} taps)";
            }

            if (globalEffortText != null)
            {
                globalEffortText.text = $"{targetParticipants} Guardian{(targetParticipants != 1 ? "s" : "")}";
            }

            if (countriesText != null)
            {
                countriesText.text = $"from {targetCountries} {(targetCountries != 1 ? "countries" : "country")}";
            }

            if (xpEarnedText != null)
            {
                xpEarnedText.text = $"+{targetXP} XP";
            }

            // Show XP breakdown
            ShowXPBreakdown(data);
        }

        /// <summary>
        /// Show XP breakdown
        /// </summary>
        private void ShowXPBreakdown(IntruderReturnData data)
        {
            if (xpBreakdownPanel == null || xpBreakdownText == null) return;

            List<string> breakdown = new List<string>();
            breakdown.Add("Participation: +10 XP");

            float contribution = data.totalTaps > 0 ? (float)data.playerTaps / data.totalTaps : 0f;

            if (contribution >= 0.25f)
            {
                breakdown.Add("25%+ Contribution: +30 XP");
            }
            else if (contribution >= 0.10f)
            {
                breakdown.Add("10%+ Contribution: +15 XP");
            }

            if (data.wasFirstTapper)
            {
                breakdown.Add("First Tapper: +20 XP");
            }

            if (data.intruderType?.sizeClass == IntruderSize.Boss)
            {
                breakdown.Add("Boss Return: +100 XP");
            }

            xpBreakdownPanel.SetActive(true);
            xpBreakdownText.text = string.Join("\n", breakdown);
        }

        /// <summary>
        /// Show rank up animation
        /// </summary>
        private IEnumerator ShowRankUp(IntruderReturnData data)
        {
            if (rankUpPanel == null) yield break;

            yield return new WaitForSeconds(0.5f);

            PlaySound(rankUpSound);

            rankUpPanel.SetActive(true);

            if (oldRankText != null)
            {
                oldRankText.text = PlayerRank.GetRankTitle(data.oldRank);
            }

            if (newRankText != null)
            {
                newRankText.text = PlayerRank.GetRankTitle(data.newRank);
            }

            if (rankUnlockText != null)
            {
                rankUnlockText.text = $"Unlocked: {PlayerRank.GetRankUnlock(data.newRank)}";
            }
        }

        /// <summary>
        /// Auto dismiss coroutine
        /// </summary>
        private IEnumerator AutoDismissCoroutine()
        {
            yield return new WaitForSeconds(autoDismissDelay);
            Hide();
        }

        /// <summary>
        /// Hide celebration overlay
        /// </summary>
        public void Hide()
        {
            if (!isShowing) return;

            isShowing = false;

            if (dismissCoroutine != null)
            {
                StopCoroutine(dismissCoroutine);
                dismissCoroutine = null;
            }

            StartCoroutine(HideAnimationCoroutine());
        }

        /// <summary>
        /// Hide animation coroutine
        /// </summary>
        private IEnumerator HideAnimationCoroutine()
        {
            float elapsed = 0f;
            float duration = 0.3f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f - t;
                }

                yield return null;
            }

            if (celebrationPanel != null)
            {
                celebrationPanel.SetActive(false);
            }

            if (confettiParticles != null)
            {
                confettiParticles.Stop();
            }

            currentData = null;
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
                IntruderEra.HistoricalMachines => "Historical Machine",
                IntruderEra.LostCivilizations => "Lost Civilization",
                IntruderEra.TimeAnomalies => "Time Anomaly",
                _ => "Unknown Era"
            };
        }

        /// <summary>
        /// Play sound
        /// </summary>
        private void PlaySound(AudioClip clip, float volume = 1f)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip, volume);
            }
        }

        /// <summary>
        /// Check if overlay is showing
        /// </summary>
        public bool IsShowing => isShowing;
    }
}
