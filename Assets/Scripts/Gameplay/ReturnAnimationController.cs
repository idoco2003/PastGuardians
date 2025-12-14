using UnityEngine;
using PastGuardians.Data;
using System;
using System.Collections;

namespace PastGuardians.Gameplay
{
    /// <summary>
    /// Orchestrates the full return sequence when an intruder is defeated
    /// </summary>
    public class ReturnAnimationController : MonoBehaviour
    {
        public static ReturnAnimationController Instance { get; private set; }

        [Header("Timing (seconds)")]
        [SerializeField] private float glowStartTime = 0f;
        [SerializeField] private float portalExpandTime = 0.3f;
        [SerializeField] private float tempusVisibleTime = 0.5f;
        [SerializeField] private float shrinkStartTime = 1f;
        [SerializeField] private float enterPortalTime = 2f;
        [SerializeField] private float lightBurstTime = 2.3f;
        [SerializeField] private float sealStartTime = 2.5f;
        [SerializeField] private float sequenceEndTime = 3f;

        [Header("Shrink Settings")]
        [SerializeField] private float shrinkDuration = 1f;
        [SerializeField] private AnimationCurve shrinkCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        [Header("Glow Settings")]
        [SerializeField] private float glowIntensity = 2f;
        [SerializeField] private Color glowColor = Color.white;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip returnStartSound;
        [SerializeField] private AudioClip portalExpandSound;
        [SerializeField] private AudioClip enterPortalSound;
        [SerializeField] private AudioClip sealSound;
        [SerializeField] private AudioClip celebrationSound;

        [Header("Portal Prefab")]
        [SerializeField] private GameObject portalPrefab;

        // Events
        public event Action<Intruder> OnReturnSequenceStarted;
        public event Action<Intruder> OnReturnSequenceCompleted;

        // State
        private bool isPlaying;
        private Intruder currentIntruder;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }
        }

        private void Start()
        {
            // Subscribe to intruder return events
            if (IntruderSpawner.Instance != null)
            {
                // We'll be triggered directly by TapProgressManager or Intruder
            }
        }

        /// <summary>
        /// Play the full return sequence for an intruder
        /// </summary>
        public void PlayReturnSequence(Intruder intruder)
        {
            if (intruder == null || isPlaying) return;

            StartCoroutine(ReturnSequenceCoroutine(intruder));
        }

        /// <summary>
        /// Main return sequence coroutine
        /// </summary>
        private IEnumerator ReturnSequenceCoroutine(Intruder intruder)
        {
            isPlaying = true;
            currentIntruder = intruder;

            OnReturnSequenceStarted?.Invoke(intruder);
            Debug.Log($"[ReturnAnimation] Starting return sequence for {intruder.Data?.displayName}");

            // Get or create portal
            TimePortalEffect portal = GetOrCreatePortal(intruder);

            // Get intruder's renderer for glow effect
            Renderer intruderRenderer = intruder.GetComponentInChildren<Renderer>();
            Material originalMaterial = intruderRenderer?.material;
            Vector3 originalScale = intruder.transform.localScale;
            Vector3 originalPosition = intruder.transform.position;

            float elapsed = 0f;

            // [0.0s] Start - Intruder stops moving, starts glowing
            PlaySound(returnStartSound);
            if (intruderRenderer != null)
            {
                StartCoroutine(GlowEffect(intruderRenderer));
            }

            yield return new WaitForSeconds(portalExpandTime - elapsed);
            elapsed = portalExpandTime;

            // [0.3s] Portal expands
            PlaySound(portalExpandSound);
            portal?.PlayExpandAnimation();

            yield return new WaitForSeconds(tempusVisibleTime - elapsed);
            elapsed = tempusVisibleTime;

            // [0.5s] Tempus Prime visible through portal
            portal?.SetTempusPrimeVisibility(true);

            yield return new WaitForSeconds(shrinkStartTime - elapsed);
            elapsed = shrinkStartTime;

            // [1.0s] Intruder begins shrinking toward portal
            float shrinkElapsed = 0f;
            Vector3 portalPosition = portal != null ? portal.transform.position : originalPosition + Vector3.forward;

            while (shrinkElapsed < shrinkDuration)
            {
                shrinkElapsed += Time.deltaTime;
                float t = shrinkElapsed / shrinkDuration;
                float curveValue = shrinkCurve.Evaluate(t);

                // Shrink
                intruder.transform.localScale = originalScale * (1f - curveValue);

                // Move toward portal
                intruder.transform.position = Vector3.Lerp(originalPosition, portalPosition, curveValue);

                yield return null;
            }

            elapsed = enterPortalTime;

            // [2.0s] Intruder fully enters portal
            PlaySound(enterPortalSound);
            intruder.transform.localScale = Vector3.zero;

            yield return new WaitForSeconds(lightBurstTime - elapsed);
            elapsed = lightBurstTime;

            // [2.3s] Light burst
            portal?.PlayLightBurst();

            yield return new WaitForSeconds(sealStartTime - elapsed);
            elapsed = sealStartTime;

            // [2.5s] Portal begins sealing
            PlaySound(sealSound);
            portal?.PlaySealAnimation();

            yield return new WaitForSeconds(sequenceEndTime - elapsed);

            // [3.0s] Sequence complete
            PlaySound(celebrationSound);

            // Cleanup
            if (portal != null)
            {
                Destroy(portal.gameObject, 0.5f);
            }

            // Notify intruder
            intruder.CompleteReturn();

            // Trigger celebration
            TriggerCelebration(intruder);

            OnReturnSequenceCompleted?.Invoke(intruder);
            Debug.Log($"[ReturnAnimation] Return sequence completed for {intruder.Data?.displayName}");

            isPlaying = false;
            currentIntruder = null;
        }

        /// <summary>
        /// Get existing portal or create new one
        /// </summary>
        private TimePortalEffect GetOrCreatePortal(Intruder intruder)
        {
            // Check if intruder already has a portal
            TimePortalEffect existingPortal = intruder.GetComponentInChildren<TimePortalEffect>();
            if (existingPortal != null)
            {
                return existingPortal;
            }

            // Create new portal
            GameObject portalObj;
            if (portalPrefab != null)
            {
                portalObj = Instantiate(portalPrefab, intruder.transform.position + Vector3.forward * 0.5f, Quaternion.identity);
            }
            else
            {
                portalObj = new GameObject("TimePortal");
                portalObj.transform.position = intruder.transform.position + Vector3.forward * 0.5f;
            }

            TimePortalEffect portal = portalObj.GetComponent<TimePortalEffect>();
            if (portal == null)
            {
                portal = portalObj.AddComponent<TimePortalEffect>();
            }

            portal.InitializeForIntruder(intruder.Data);

            return portal;
        }

        /// <summary>
        /// Glow effect coroutine
        /// </summary>
        private IEnumerator GlowEffect(Renderer renderer)
        {
            if (renderer == null) yield break;

            Material mat = renderer.material;
            Color originalColor = mat.color;
            Color originalEmission = mat.HasProperty("_EmissionColor")
                ? mat.GetColor("_EmissionColor")
                : Color.black;

            float elapsed = 0f;
            float pulseDuration = 0.5f;

            while (isPlaying && currentIntruder != null)
            {
                elapsed += Time.deltaTime;
                float pulse = (Mathf.Sin(elapsed * Mathf.PI * 2f / pulseDuration) + 1f) / 2f;

                // Pulse emission
                if (mat.HasProperty("_EmissionColor"))
                {
                    Color emission = Color.Lerp(originalEmission, glowColor * glowIntensity, pulse);
                    mat.SetColor("_EmissionColor", emission);
                }

                // Pulse brightness
                Color color = Color.Lerp(originalColor, glowColor, pulse * 0.3f);
                mat.color = color;

                yield return null;
            }

            // Restore original
            mat.color = originalColor;
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", originalEmission);
            }
        }

        /// <summary>
        /// Trigger celebration UI
        /// </summary>
        private void TriggerCelebration(Intruder intruder)
        {
            // This will be handled by CelebrationOverlay via TapProgressManager events
            // The event flow: ReturnAnimationController -> Intruder.CompleteReturn()
            //                  -> TapProgressManager.HandleIntruderReturned()
            //                  -> CelebrationOverlay
        }

        /// <summary>
        /// Play sound effect
        /// </summary>
        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        /// <summary>
        /// Cancel current sequence
        /// </summary>
        public void CancelSequence()
        {
            if (isPlaying)
            {
                StopAllCoroutines();
                isPlaying = false;
                currentIntruder = null;
            }
        }

        /// <summary>
        /// Check if sequence is playing
        /// </summary>
        public bool IsPlaying => isPlaying;

        /// <summary>
        /// Get current intruder being animated
        /// </summary>
        public Intruder CurrentIntruder => currentIntruder;
    }
}
