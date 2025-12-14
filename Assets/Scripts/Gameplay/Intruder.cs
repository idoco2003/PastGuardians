using UnityEngine;
using PastGuardians.Data;
using System;

namespace PastGuardians.Gameplay
{
    /// <summary>
    /// Current state of an intruder
    /// </summary>
    public enum IntruderState
    {
        Spawning,   // Just appeared, playing spawn animation
        Active,     // Normal state, can be tapped
        Escaping,   // Trying to flee (high stability drain)
        Returning,  // Being sent back to Tempus Prime
        Gone        // Returned or despawned
    }

    /// <summary>
    /// Runtime component for active intruders in the game world
    /// </summary>
    public class Intruder : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private IntruderData data;

        [Header("Instance")]
        [SerializeField] private string uniqueInstanceId;
        [SerializeField] private double latitude;
        [SerializeField] private double longitude;
        [SerializeField] private float altitude;

        [Header("Progress")]
        [SerializeField] private int currentTapProgress;
        [SerializeField] private int totalTapsRequired;
        [SerializeField] private float currentStability = 1f;  // 0-1, 1 = full

        [Header("State")]
        [SerializeField] private IntruderState state = IntruderState.Spawning;
        [SerializeField] private bool isBeingTapped;
        [SerializeField] private float timeSinceLastTap;
        [SerializeField] private float spawnTime;
        [SerializeField] private float timeRemaining;

        [Header("Calculated")]
        [SerializeField] private float distanceFromPlayer;
        [SerializeField] private Vector2 screenPosition;
        [SerializeField] private float visualScale = 1f;
        [SerializeField] private bool isVisible;

        // Movement
        private Vector3 basePosition;
        private float movementPhase;

        // Events
        public event Action<Intruder, int> OnTapped;                  // intruder, newProgress
        public event Action<Intruder, float> OnStabilityChanged;      // intruder, newStability
        public event Action<Intruder> OnReturnStarted;
        public event Action<Intruder> OnReturnCompleted;
        public event Action<Intruder> OnEscapeStarted;
        public event Action<Intruder> OnTimeExpired;

        // Properties
        public IntruderData Data => data;
        public string InstanceId => uniqueInstanceId;
        public double Latitude => latitude;
        public double Longitude => longitude;
        public float Altitude => altitude;
        public int TapProgress => currentTapProgress;
        public int TotalTapsRequired => totalTapsRequired;
        public float Stability => currentStability;
        public IntruderState State => state;
        public bool IsBeingTapped => isBeingTapped;
        public float TimeSinceLastTap => timeSinceLastTap;
        public float TimeRemaining => timeRemaining;
        public float DistanceFromPlayer => distanceFromPlayer;
        public Vector2 ScreenPosition => screenPosition;
        public float VisualScale => visualScale;
        public bool IsVisible => isVisible;
        public bool IsBoss => data != null && data.sizeClass == IntruderSize.Boss;

        /// <summary>
        /// Progress percentage (0-1)
        /// </summary>
        public float ProgressPercent => totalTapsRequired > 0 ? (float)currentTapProgress / totalTapsRequired : 0f;

        /// <summary>
        /// Initialize the intruder with data and position
        /// </summary>
        public void Initialize(IntruderData intruderData, double lat, double lon, float alt, string instanceId = null)
        {
            data = intruderData;
            latitude = lat;
            longitude = lon;
            altitude = alt;

            uniqueInstanceId = string.IsNullOrEmpty(instanceId)
                ? $"{intruderData.intruderId}_{Guid.NewGuid().ToString("N").Substring(0, 8)}"
                : instanceId;

            totalTapsRequired = intruderData.baseTapsRequired;
            currentTapProgress = 0;
            currentStability = 1f;
            timeRemaining = intruderData.GetTapWindow();
            spawnTime = Time.time;
            state = IntruderState.Spawning;
            movementPhase = UnityEngine.Random.value * Mathf.PI * 2f;

            // Position in world space (will be updated by screen positioner)
            basePosition = transform.position;

            // Start spawning animation
            Invoke(nameof(FinishSpawning), 1f);

            Debug.Log($"[Intruder] Initialized {data.displayName} at ({lat:F4}, {lon:F4}, {alt}m)");
        }

        private void FinishSpawning()
        {
            if (state == IntruderState.Spawning)
            {
                state = IntruderState.Active;
            }
        }

        private void Update()
        {
            if (state == IntruderState.Gone || state == IntruderState.Returning)
                return;

            // Update time tracking
            timeSinceLastTap += Time.deltaTime;

            // Update stability regeneration
            UpdateStability(Time.deltaTime);

            // Update time remaining
            UpdateTimeRemaining(Time.deltaTime);

            // Update movement
            UpdateMovement(Time.deltaTime);

            // Check for escape behavior
            CheckEscapeBehavior();
        }

        /// <summary>
        /// Handle stability regeneration
        /// </summary>
        private void UpdateStability(float deltaTime)
        {
            if (state != IntruderState.Active && state != IntruderState.Escaping)
                return;

            // Get config values
            float regenDelay = 2f;  // Default, override from GameConfig if available
            float regenRate = data?.stabilityRegenRate ?? 0.5f;

            if (Core.PlayerDataManager.Instance?.Config != null)
            {
                regenDelay = Core.PlayerDataManager.Instance.Config.regenDelayAfterTap;
                regenRate = Core.PlayerDataManager.Instance.Config.stabilityRegenRate;
            }

            // Regenerate stability if not being tapped recently
            if (timeSinceLastTap > regenDelay && currentStability < 1f)
            {
                float oldStability = currentStability;
                currentStability = Mathf.Min(1f, currentStability + regenRate * deltaTime);

                if (Mathf.Abs(currentStability - oldStability) > 0.01f)
                {
                    OnStabilityChanged?.Invoke(this, currentStability);
                }
            }

            isBeingTapped = timeSinceLastTap < 0.5f;
        }

        /// <summary>
        /// Update time remaining
        /// </summary>
        private void UpdateTimeRemaining(float deltaTime)
        {
            if (state != IntruderState.Active && state != IntruderState.Escaping)
                return;

            timeRemaining -= deltaTime;

            if (timeRemaining <= 0)
            {
                OnTimeExpired?.Invoke(this);
                state = IntruderState.Gone;
                Debug.Log($"[Intruder] {data.displayName} time expired!");
            }
        }

        /// <summary>
        /// Update movement based on pattern
        /// </summary>
        private void UpdateMovement(float deltaTime)
        {
            if (data == null) return;

            movementPhase += deltaTime;

            float speedMultiplier = state == IntruderState.Escaping ? (data.escapeSpeedMultiplier) : 1f;
            Vector3 offset = Vector3.zero;

            switch (data.movementPattern)
            {
                case MovementPattern.Hover:
                    // Gentle bob up and down
                    offset.y = Mathf.Sin(movementPhase * 2f) * 0.1f;
                    break;

                case MovementPattern.Drift:
                    // Slow horizontal movement
                    offset.x = Mathf.Sin(movementPhase * 0.5f) * 0.3f * speedMultiplier;
                    offset.y = Mathf.Sin(movementPhase * 1.5f) * 0.05f;
                    break;

                case MovementPattern.Circle:
                    // Circular orbit
                    offset.x = Mathf.Cos(movementPhase * 0.8f) * 0.4f * speedMultiplier;
                    offset.z = Mathf.Sin(movementPhase * 0.8f) * 0.4f * speedMultiplier;
                    offset.y = Mathf.Sin(movementPhase * 2f) * 0.05f;
                    break;

                case MovementPattern.Escape:
                    // Move away when being tapped
                    if (isBeingTapped)
                    {
                        offset.y = movementPhase * 0.1f * speedMultiplier;
                    }
                    else
                    {
                        offset.y = Mathf.Sin(movementPhase * 2f) * 0.1f;
                    }
                    break;

                case MovementPattern.Approach:
                    // Actually decrease altitude over time (descend toward player)
                    // Descent rate: ~50 meters per second, gets closer and appears larger
                    float descentRate = 50f * speedMultiplier;
                    float minAltitude = 500f;  // Don't go below 500m
                    altitude = Mathf.Max(minAltitude, altitude - descentRate * deltaTime);

                    // Add subtle bob while descending
                    offset.y = Mathf.Sin(movementPhase * 2f) * 0.05f;
                    break;
            }

            transform.position = basePosition + offset;
        }

        /// <summary>
        /// Check and trigger escape behavior
        /// </summary>
        private void CheckEscapeBehavior()
        {
            if (state != IntruderState.Active) return;

            float escapeThreshold = 0.8f;
            bool escapeEnabled = true;

            if (Core.PlayerDataManager.Instance?.Config != null)
            {
                escapeThreshold = Core.PlayerDataManager.Instance.Config.escapeThreshold;
                escapeEnabled = Core.PlayerDataManager.Instance.Config.escapeEnabled;
            }

            if (escapeEnabled && ProgressPercent >= escapeThreshold && state != IntruderState.Escaping)
            {
                state = IntruderState.Escaping;
                OnEscapeStarted?.Invoke(this);
                Debug.Log($"[Intruder] {data.displayName} is trying to escape!");
            }
        }

        /// <summary>
        /// Register a tap on this intruder
        /// </summary>
        public void RegisterTap(int tapCount = 1)
        {
            if (state == IntruderState.Gone || state == IntruderState.Returning)
                return;

            currentTapProgress += tapCount;
            timeSinceLastTap = 0f;
            isBeingTapped = true;

            // Drain stability on tap
            currentStability = Mathf.Max(0f, currentStability - 0.02f * tapCount);
            OnStabilityChanged?.Invoke(this, currentStability);

            OnTapped?.Invoke(this, currentTapProgress);

            // Check if complete
            if (currentTapProgress >= totalTapsRequired)
            {
                StartReturnSequence();
            }
        }

        /// <summary>
        /// Set tap progress from server sync
        /// </summary>
        public void SetTapProgress(int progress)
        {
            currentTapProgress = progress;

            if (currentTapProgress >= totalTapsRequired && state != IntruderState.Returning)
            {
                StartReturnSequence();
            }
        }

        /// <summary>
        /// Start the return to Tempus Prime sequence
        /// </summary>
        public void StartReturnSequence()
        {
            if (state == IntruderState.Returning || state == IntruderState.Gone)
                return;

            state = IntruderState.Returning;
            OnReturnStarted?.Invoke(this);
            Debug.Log($"[Intruder] {data.displayName} returning to Tempus Prime!");

            // The actual animation is handled by ReturnAnimationController
        }

        /// <summary>
        /// Called when return animation completes
        /// </summary>
        public void CompleteReturn()
        {
            state = IntruderState.Gone;
            OnReturnCompleted?.Invoke(this);
            Debug.Log($"[Intruder] {data.displayName} returned to Tempus Prime!");
        }

        /// <summary>
        /// Update position from screen positioner
        /// </summary>
        public void UpdateScreenPosition(Vector2 screenPos, float scale, bool visible, float distance)
        {
            screenPosition = screenPos;
            visualScale = scale;
            isVisible = visible;
            distanceFromPlayer = distance;

            // Update actual position based on screen position
            if (visible)
            {
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
                basePosition = worldPos;
                transform.localScale = Vector3.one * scale;
            }
        }

        /// <summary>
        /// Force despawn (timeout or server removal)
        /// </summary>
        public void Despawn()
        {
            state = IntruderState.Gone;
            Destroy(gameObject);
        }

        /// <summary>
        /// Get debug info
        /// </summary>
        public string GetDebugInfo()
        {
            return $"ID: {uniqueInstanceId}\n" +
                   $"Type: {data?.displayName ?? "Unknown"}\n" +
                   $"State: {state}\n" +
                   $"Progress: {currentTapProgress}/{totalTapsRequired} ({ProgressPercent:P0})\n" +
                   $"Stability: {currentStability:P0}\n" +
                   $"Time: {timeRemaining:F1}s\n" +
                   $"Distance: {distanceFromPlayer:F1}km";
        }
    }
}
