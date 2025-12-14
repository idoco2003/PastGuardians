using UnityEngine;
using PastGuardians.Data;
using System.Collections;

namespace PastGuardians.Gameplay
{
    /// <summary>
    /// Portal states
    /// </summary>
    public enum PortalState
    {
        Idle,       // Normal background portal
        Expanding,  // Expanding for return
        Open,       // Fully open showing Tempus Prime
        Sealing,    // Closing after return
        Closed      // Completely closed
    }

    /// <summary>
    /// Visual effect for time rift portals behind intruders
    /// </summary>
    public class TimePortalEffect : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ParticleSystem edgeParticles;
        [SerializeField] private ParticleSystem sealParticles;
        [SerializeField] private SpriteRenderer portalSprite;
        [SerializeField] private SpriteRenderer tempusPrimeSprite;
        [SerializeField] private Transform portalTransform;

        [Header("Settings")]
        [SerializeField] private float idleRadius = 1f;
        [SerializeField] private float expandedRadius = 2f;
        [SerializeField] private float expandDuration = 0.5f;
        [SerializeField] private float sealDuration = 0.5f;
        [SerializeField] private float rotationSpeed = 30f;

        [Header("Colors")]
        [SerializeField] private Color portalColor = Color.cyan;
        [SerializeField] private Color edgeColor = Color.white;

        // State
        private PortalState state = PortalState.Idle;
        private float currentRadius;
        private Coroutine animationCoroutine;

        // Properties
        public PortalState State => state;
        public Color PortalColor => portalColor;

        private void Awake()
        {
            SetupComponents();
        }

        private void Update()
        {
            // Rotate portal slowly
            if (portalTransform != null && state != PortalState.Closed)
            {
                portalTransform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
            }
        }

        /// <summary>
        /// Setup visual components
        /// </summary>
        private void SetupComponents()
        {
            if (portalTransform == null)
            {
                portalTransform = transform;
            }

            // Create portal sprite if not assigned
            if (portalSprite == null)
            {
                GameObject spriteObj = new GameObject("PortalSprite");
                spriteObj.transform.SetParent(transform);
                spriteObj.transform.localPosition = Vector3.zero;
                portalSprite = spriteObj.AddComponent<SpriteRenderer>();
                portalSprite.sprite = CreateCircleSprite();
            }

            // Create Tempus Prime glimpse sprite if not assigned
            if (tempusPrimeSprite == null)
            {
                GameObject glimpseObj = new GameObject("TempusPrimeGlimpse");
                glimpseObj.transform.SetParent(transform);
                glimpseObj.transform.localPosition = new Vector3(0, 0, 0.1f);
                tempusPrimeSprite = glimpseObj.AddComponent<SpriteRenderer>();
                tempusPrimeSprite.sprite = CreateCircleSprite();
                tempusPrimeSprite.color = new Color(1f, 0.9f, 0.6f, 0.3f);  // Golden tint
                tempusPrimeSprite.enabled = false;
            }

            // Create edge particles if not assigned
            if (edgeParticles == null)
            {
                GameObject particleObj = new GameObject("EdgeParticles");
                particleObj.transform.SetParent(transform);
                particleObj.transform.localPosition = Vector3.zero;
                edgeParticles = particleObj.AddComponent<ParticleSystem>();
                SetupEdgeParticles();
            }

            currentRadius = idleRadius;
            UpdateVisuals();
        }

        /// <summary>
        /// Create a simple circle sprite
        /// </summary>
        private Sprite CreateCircleSprite()
        {
            int size = 128;
            Texture2D texture = new Texture2D(size, size);

            Color[] colors = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f - 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist <= radius)
                    {
                        // Gradient from center to edge
                        float alpha = 1f - (dist / radius) * 0.5f;
                        colors[y * size + x] = new Color(1, 1, 1, alpha);
                    }
                    else
                    {
                        colors[y * size + x] = Color.clear;
                    }
                }
            }

            texture.SetPixels(colors);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        /// <summary>
        /// Setup edge particle system
        /// </summary>
        private void SetupEdgeParticles()
        {
            if (edgeParticles == null) return;

            var main = edgeParticles.main;
            main.loop = true;
            main.startLifetime = 1f;
            main.startSpeed = 0.5f;
            main.startSize = 0.1f;
            main.startColor = edgeColor;
            main.maxParticles = 50;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var emission = edgeParticles.emission;
            emission.rateOverTime = 20f;

            var shape = edgeParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = idleRadius;
            shape.radiusMode = ParticleSystemShapeMultiModeValue.Loop;
            shape.arcMode = ParticleSystemShapeMultiModeValue.Loop;

            var velocity = edgeParticles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.radial = -0.5f;  // Move toward center

            var colorOverLifetime = edgeParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(edgeColor, 0f), new GradientColorKey(portalColor, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;
        }

        /// <summary>
        /// Initialize portal with era color
        /// </summary>
        public void Initialize(Color eraColor)
        {
            portalColor = eraColor;
            UpdateVisuals();
            PlayIdleAnimation();
        }

        /// <summary>
        /// Initialize portal for intruder
        /// </summary>
        public void InitializeForIntruder(IntruderData data)
        {
            if (data != null)
            {
                Initialize(data.GetEraPortalColor());
            }
            else
            {
                Initialize(Color.cyan);
            }
        }

        /// <summary>
        /// Update visual elements
        /// </summary>
        private void UpdateVisuals()
        {
            // Update portal sprite
            if (portalSprite != null)
            {
                portalSprite.color = new Color(portalColor.r, portalColor.g, portalColor.b, 0.6f);
                portalSprite.transform.localScale = Vector3.one * currentRadius * 2f;
            }

            // Update Tempus Prime glimpse
            if (tempusPrimeSprite != null)
            {
                tempusPrimeSprite.transform.localScale = Vector3.one * currentRadius * 1.8f;
            }

            // Update particle shape
            if (edgeParticles != null)
            {
                var shape = edgeParticles.shape;
                shape.radius = currentRadius;

                var main = edgeParticles.main;
                main.startColor = edgeColor;
            }
        }

        /// <summary>
        /// Play idle animation
        /// </summary>
        public void PlayIdleAnimation()
        {
            state = PortalState.Idle;
            currentRadius = idleRadius;

            if (edgeParticles != null)
            {
                edgeParticles.Play();
            }

            UpdateVisuals();
        }

        /// <summary>
        /// Play expand animation
        /// </summary>
        public void PlayExpandAnimation(float duration = -1f)
        {
            if (duration < 0) duration = expandDuration;

            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }

            animationCoroutine = StartCoroutine(ExpandCoroutine(duration));
        }

        /// <summary>
        /// Expand animation coroutine
        /// </summary>
        private IEnumerator ExpandCoroutine(float duration)
        {
            state = PortalState.Expanding;
            float startRadius = currentRadius;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = EaseOutBack(t);  // Bouncy expand

                currentRadius = Mathf.Lerp(startRadius, expandedRadius, t);
                UpdateVisuals();

                yield return null;
            }

            currentRadius = expandedRadius;
            state = PortalState.Open;
            SetTempusPrimeVisibility(true);
            UpdateVisuals();

            animationCoroutine = null;
        }

        /// <summary>
        /// Play seal animation
        /// </summary>
        public void PlaySealAnimation(float duration = -1f)
        {
            if (duration < 0) duration = sealDuration;

            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }

            animationCoroutine = StartCoroutine(SealCoroutine(duration));
        }

        /// <summary>
        /// Seal animation coroutine
        /// </summary>
        private IEnumerator SealCoroutine(float duration)
        {
            state = PortalState.Sealing;
            SetTempusPrimeVisibility(false);

            float startRadius = currentRadius;
            float elapsed = 0f;

            // Play seal particles
            if (sealParticles != null)
            {
                sealParticles.Play();
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = EaseInBack(t);

                currentRadius = Mathf.Lerp(startRadius, 0f, t);
                UpdateVisuals();

                yield return null;
            }

            currentRadius = 0f;
            state = PortalState.Closed;

            if (edgeParticles != null)
            {
                edgeParticles.Stop();
            }

            UpdateVisuals();
            animationCoroutine = null;
        }

        /// <summary>
        /// Set Tempus Prime glimpse visibility
        /// </summary>
        public void SetTempusPrimeVisibility(bool visible)
        {
            if (tempusPrimeSprite != null)
            {
                tempusPrimeSprite.enabled = visible;
            }
        }

        /// <summary>
        /// Play light burst effect
        /// </summary>
        public void PlayLightBurst()
        {
            StartCoroutine(LightBurstCoroutine());
        }

        /// <summary>
        /// Light burst coroutine
        /// </summary>
        private IEnumerator LightBurstCoroutine()
        {
            if (portalSprite == null) yield break;

            Color originalColor = portalSprite.color;
            Color burstColor = Color.white;

            // Flash to white
            float elapsed = 0f;
            float duration = 0.15f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                portalSprite.color = Color.Lerp(originalColor, burstColor, elapsed / duration);
                yield return null;
            }

            // Flash back
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                portalSprite.color = Color.Lerp(burstColor, originalColor, elapsed / duration);
                yield return null;
            }

            portalSprite.color = originalColor;
        }

        /// <summary>
        /// Set portal color
        /// </summary>
        public void SetPortalColor(Color color)
        {
            portalColor = color;
            UpdateVisuals();
        }

        /// <summary>
        /// Reset portal to initial state
        /// </summary>
        public void Reset()
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
                animationCoroutine = null;
            }

            currentRadius = idleRadius;
            state = PortalState.Idle;
            SetTempusPrimeVisibility(false);
            UpdateVisuals();

            if (edgeParticles != null)
            {
                edgeParticles.Play();
            }
        }

        // Easing functions
        private float EaseOutBack(float t)
        {
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private float EaseInBack(float t)
        {
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            return c3 * t * t * t - c1 * t * t;
        }
    }
}
