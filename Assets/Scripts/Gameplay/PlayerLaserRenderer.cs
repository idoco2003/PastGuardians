using UnityEngine;
using PastGuardians.Data;
using PastGuardians.Core;
using System.Collections;

namespace PastGuardians.Gameplay
{
    /// <summary>
    /// Renders the player's laser beam when tapping intruders
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class PlayerLaserRenderer : MonoBehaviour
    {
        public static PlayerLaserRenderer Instance { get; private set; }

        [Header("References")]
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private GameConfig gameConfig;

        [Header("Laser Settings")]
        [SerializeField] private float startWidth = 0.05f;
        [SerializeField] private float endWidth = 0.02f;
        [SerializeField] private float baseIntensity = 1f;
        [SerializeField] private float pulseIntensity = 1.5f;
        [SerializeField] private float pulseDuration = 0.1f;

        [Header("Origin")]
        [SerializeField] private Vector3 screenOriginOffset = new Vector3(0.5f, 0.1f, 0f);  // Normalized screen position
        [SerializeField] private float originDepth = 5f;

        [Header("Materials")]
        [SerializeField] private Material laserMaterial;
        [SerializeField] private Gradient defaultGradient;

        // State
        private bool isActive;
        private float currentIntensity;
        private float fadeTimer;
        private Vector3 currentTargetPosition;
        private Coroutine pulseCoroutine;

        // Colors
        private Color currentColor;

        // Properties
        public bool IsActive => isActive;
        public Color BeamColor => currentColor;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            SetupLineRenderer();
        }

        private void Start()
        {
            // Subscribe to tap events
            if (TapInputHandler.Instance != null)
            {
                TapInputHandler.Instance.OnTapRegistered += HandleTapRegistered;
                TapInputHandler.Instance.OnTargetChanged += HandleTargetChanged;
            }

            // Get player's beam color
            UpdateBeamColor();
        }

        private void OnDestroy()
        {
            if (TapInputHandler.Instance != null)
            {
                TapInputHandler.Instance.OnTapRegistered -= HandleTapRegistered;
                TapInputHandler.Instance.OnTargetChanged -= HandleTargetChanged;
            }
        }

        private void Update()
        {
            if (isActive)
            {
                UpdateLaserPosition();
                UpdateFade();
            }
        }

        /// <summary>
        /// Setup the LineRenderer component
        /// </summary>
        private void SetupLineRenderer()
        {
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
            }

            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = startWidth;
            lineRenderer.endWidth = endWidth;
            lineRenderer.useWorldSpace = true;

            // Create material if not assigned
            if (laserMaterial == null)
            {
                laserMaterial = new Material(Shader.Find("Sprites/Default"));
                laserMaterial.SetFloat("_Mode", 2);  // Fade mode
            }

            lineRenderer.material = laserMaterial;

            // Setup default gradient
            if (defaultGradient == null)
            {
                defaultGradient = new Gradient();
                defaultGradient.SetKeys(
                    new GradientColorKey[]
                    {
                        new GradientColorKey(Color.white, 0f),
                        new GradientColorKey(new Color(0f, 0.83f, 1f), 1f)  // Cyan-blue
                    },
                    new GradientAlphaKey[]
                    {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(0.8f, 1f)
                    }
                );
            }

            lineRenderer.colorGradient = defaultGradient;
            lineRenderer.enabled = false;
        }

        /// <summary>
        /// Update beam color from player data
        /// </summary>
        public void UpdateBeamColor()
        {
            if (PlayerDataManager.Instance?.Data != null)
            {
                currentColor = PlayerDataManager.Instance.Data.selectedBeamColor;
            }
            else if (gameConfig != null)
            {
                currentColor = gameConfig.defaultLaserColor;
            }
            else
            {
                currentColor = new Color(0f, 0.83f, 1f);  // Default cyan-blue
            }

            UpdateGradientColor(currentColor);
        }

        /// <summary>
        /// Update gradient with new color
        /// </summary>
        private void UpdateGradientColor(Color color)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(color, 0.3f),
                    new GradientColorKey(color, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(currentIntensity, 0.5f),
                    new GradientAlphaKey(currentIntensity * 0.8f, 1f)
                }
            );

            lineRenderer.colorGradient = gradient;
        }

        /// <summary>
        /// Handle tap registered
        /// </summary>
        private void HandleTapRegistered(Intruder target, int tapCount)
        {
            if (target == null) return;

            // Activate laser
            ActivateLaser(target.ScreenPosition);

            // Pulse effect
            PulseOnTap();

            // Reset fade timer
            fadeTimer = 0f;
        }

        /// <summary>
        /// Handle target changed
        /// </summary>
        private void HandleTargetChanged(Intruder newTarget)
        {
            if (newTarget == null)
            {
                // Start fade out
                fadeTimer = 0f;
            }
            else
            {
                ActivateLaser(newTarget.ScreenPosition);
            }
        }

        /// <summary>
        /// Activate laser targeting a position
        /// </summary>
        public void ActivateLaser(Vector2 targetScreenPos)
        {
            isActive = true;
            lineRenderer.enabled = true;
            currentIntensity = baseIntensity;
            fadeTimer = 0f;

            // Convert screen position to world position
            Vector3 targetWorld = Camera.main.ScreenToWorldPoint(
                new Vector3(targetScreenPos.x, targetScreenPos.y, 10f)
            );

            currentTargetPosition = targetWorld;
            UpdateLaserPosition();
        }

        /// <summary>
        /// Deactivate laser
        /// </summary>
        public void DeactivateLaser()
        {
            isActive = false;
            lineRenderer.enabled = false;

            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
                pulseCoroutine = null;
            }
        }

        /// <summary>
        /// Update laser positions
        /// </summary>
        private void UpdateLaserPosition()
        {
            // Calculate origin position (bottom center of screen)
            Vector3 originScreen = new Vector3(
                Screen.width * screenOriginOffset.x,
                Screen.height * screenOriginOffset.y,
                originDepth
            );
            Vector3 originWorld = Camera.main.ScreenToWorldPoint(originScreen);

            // Update target position if we have a target
            if (TapInputHandler.Instance?.CurrentTarget != null)
            {
                var target = TapInputHandler.Instance.CurrentTarget;
                Vector3 targetScreen = new Vector3(target.ScreenPosition.x, target.ScreenPosition.y, 10f);
                currentTargetPosition = Camera.main.ScreenToWorldPoint(targetScreen);
            }

            // Set line positions
            lineRenderer.SetPosition(0, originWorld);
            lineRenderer.SetPosition(1, currentTargetPosition);
        }

        /// <summary>
        /// Handle fade out
        /// </summary>
        private void UpdateFade()
        {
            float persistDuration = gameConfig?.ownLaserPersistSeconds ?? 5f;
            float fadeDuration = gameConfig?.laserFadeDuration ?? 0.5f;

            // Check if we're still tapping
            if (TapInputHandler.Instance != null && TapInputHandler.Instance.IsTapping)
            {
                fadeTimer = 0f;
                return;
            }

            fadeTimer += Time.deltaTime;

            // Start fading after persist duration
            if (fadeTimer > persistDuration)
            {
                float fadeProgress = (fadeTimer - persistDuration) / fadeDuration;
                currentIntensity = Mathf.Lerp(baseIntensity, 0f, fadeProgress);
                UpdateGradientColor(currentColor);

                if (fadeProgress >= 1f)
                {
                    DeactivateLaser();
                }
            }
        }

        /// <summary>
        /// Pulse effect on tap
        /// </summary>
        public void PulseOnTap()
        {
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
            }
            pulseCoroutine = StartCoroutine(PulseCoroutine());
        }

        /// <summary>
        /// Pulse animation coroutine
        /// </summary>
        private IEnumerator PulseCoroutine()
        {
            // Pulse up
            float elapsed = 0f;
            while (elapsed < pulseDuration / 2f)
            {
                elapsed += Time.deltaTime;
                currentIntensity = Mathf.Lerp(baseIntensity, pulseIntensity, elapsed / (pulseDuration / 2f));
                UpdateGradientColor(currentColor);

                // Also pulse width
                float widthMultiplier = Mathf.Lerp(1f, 1.5f, elapsed / (pulseDuration / 2f));
                lineRenderer.startWidth = startWidth * widthMultiplier;
                lineRenderer.endWidth = endWidth * widthMultiplier;

                yield return null;
            }

            // Pulse down
            elapsed = 0f;
            while (elapsed < pulseDuration / 2f)
            {
                elapsed += Time.deltaTime;
                currentIntensity = Mathf.Lerp(pulseIntensity, baseIntensity, elapsed / (pulseDuration / 2f));
                UpdateGradientColor(currentColor);

                float widthMultiplier = Mathf.Lerp(1.5f, 1f, elapsed / (pulseDuration / 2f));
                lineRenderer.startWidth = startWidth * widthMultiplier;
                lineRenderer.endWidth = endWidth * widthMultiplier;

                yield return null;
            }

            currentIntensity = baseIntensity;
            lineRenderer.startWidth = startWidth;
            lineRenderer.endWidth = endWidth;
            pulseCoroutine = null;
        }

        /// <summary>
        /// Set beam color
        /// </summary>
        public void SetBeamColor(Color color)
        {
            currentColor = color;
            UpdateGradientColor(color);
        }

        /// <summary>
        /// Update target position
        /// </summary>
        public void UpdateTargetPosition(Vector3 worldPosition)
        {
            currentTargetPosition = worldPosition;
        }

        /// <summary>
        /// Get debug info
        /// </summary>
        public string GetDebugInfo()
        {
            return $"Laser Active: {isActive}\n" +
                   $"Intensity: {currentIntensity:F2}\n" +
                   $"Fade Timer: {fadeTimer:F2}s";
        }
    }
}
