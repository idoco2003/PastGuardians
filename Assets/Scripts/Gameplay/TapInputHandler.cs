using UnityEngine;
using PastGuardians.Data;
using System;
using System.Collections.Generic;

namespace PastGuardians.Gameplay
{
    /// <summary>
    /// Handles touch/tap input and intruder targeting
    /// </summary>
    public class TapInputHandler : MonoBehaviour
    {
        public static TapInputHandler Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private GameConfig gameConfig;

        [Header("Targeting")]
        [SerializeField] private float targetingRadius = 100f;  // Pixel radius for targeting
        [SerializeField] private LayerMask intruderLayerMask = -1;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;

        // Tap state
        private float lastTapTime;
        private int tapsThisSecond;
        private float secondStartTime;
        private Intruder currentTarget;
        private int recentTapCount;
        private float tapRate;

        // Touch tracking
        private Dictionary<int, float> touchStartTimes = new Dictionary<int, float>();
        private bool isTapping;

        // Events
        public event Action<Intruder, int> OnTapRegistered;   // target, total taps on this target
        public event Action<Intruder> OnTargetChanged;         // new target (null if none)
        public event Action OnTapBlocked;                      // when cooldown blocks tap

        // Properties
        public Intruder CurrentTarget => currentTarget;
        public int RecentTapCount => recentTapCount;
        public float TapRate => tapRate;
        public bool IsTapping => isTapping;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Update()
        {
            // Reset per-second counter
            if (Time.time - secondStartTime >= 1f)
            {
                tapRate = tapsThisSecond;
                tapsThisSecond = 0;
                secondStartTime = Time.time;
            }

            // Handle input
            HandleInput();

            // Update target if not tapping
            if (!isTapping && currentTarget != null)
            {
                // Clear target after timeout
                if (Time.time - lastTapTime > 2f)
                {
                    SetTarget(null);
                }
            }
        }

        /// <summary>
        /// Handle touch and mouse input
        /// </summary>
        private void HandleInput()
        {
            // Touch input (mobile)
            if (Input.touchCount > 0)
            {
                isTapping = true;

                foreach (Touch touch in Input.touches)
                {
                    switch (touch.phase)
                    {
                        case TouchPhase.Began:
                            HandleTouchBegan(touch.position, touch.fingerId);
                            break;

                        case TouchPhase.Stationary:
                        case TouchPhase.Moved:
                            HandleTouchHeld(touch.position, touch.fingerId);
                            break;

                        case TouchPhase.Ended:
                        case TouchPhase.Canceled:
                            HandleTouchEnded(touch.fingerId);
                            break;
                    }
                }
            }
            // Mouse input (editor testing)
            else if (Input.GetMouseButton(0))
            {
                isTapping = true;

                if (Input.GetMouseButtonDown(0))
                {
                    HandleTouchBegan(Input.mousePosition, -1);
                }
                else
                {
                    HandleTouchHeld(Input.mousePosition, -1);
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isTapping = false;
                HandleTouchEnded(-1);
            }
            else
            {
                isTapping = false;
            }
        }

        /// <summary>
        /// Handle touch/click start
        /// </summary>
        private void HandleTouchBegan(Vector2 position, int fingerId)
        {
            touchStartTimes[fingerId] = Time.time;

            // Try to find and target an intruder
            Intruder hitIntruder = GetIntruderAtPosition(position);

            if (hitIntruder != null)
            {
                SetTarget(hitIntruder);
                TryRegisterTap(hitIntruder);
            }
        }

        /// <summary>
        /// Handle touch/click held
        /// </summary>
        private void HandleTouchHeld(Vector2 position, int fingerId)
        {
            // Try to register continuous taps
            if (currentTarget != null && currentTarget.IsVisible)
            {
                TryRegisterTap(currentTarget);
            }
            else
            {
                // Try to find new target
                Intruder hitIntruder = GetIntruderAtPosition(position);
                if (hitIntruder != null && hitIntruder != currentTarget)
                {
                    SetTarget(hitIntruder);
                    TryRegisterTap(hitIntruder);
                }
            }
        }

        /// <summary>
        /// Handle touch/click end
        /// </summary>
        private void HandleTouchEnded(int fingerId)
        {
            touchStartTimes.Remove(fingerId);

            // Keep target for a bit after release
        }

        /// <summary>
        /// Try to register a tap (respecting cooldown)
        /// </summary>
        private bool TryRegisterTap(Intruder target)
        {
            if (target == null || target.State == IntruderState.Gone || target.State == IntruderState.Returning)
                return false;

            // Check cooldown
            float cooldown = gameConfig?.TapCooldownSeconds ?? 0.1f;
            int maxTaps = gameConfig?.maxTapsPerSecond ?? 10;

            if (Time.time - lastTapTime < cooldown)
            {
                OnTapBlocked?.Invoke();
                return false;
            }

            if (tapsThisSecond >= maxTaps)
            {
                OnTapBlocked?.Invoke();
                return false;
            }

            // Register tap
            lastTapTime = Time.time;
            tapsThisSecond++;
            recentTapCount++;

            target.RegisterTap(1);

            OnTapRegistered?.Invoke(target, target.TapProgress);

            return true;
        }

        /// <summary>
        /// Set the current target
        /// </summary>
        private void SetTarget(Intruder target)
        {
            if (target == currentTarget) return;

            currentTarget = target;
            recentTapCount = 0;

            OnTargetChanged?.Invoke(target);

            if (target != null)
            {
                Debug.Log($"[TapInputHandler] Targeting: {target.Data?.displayName ?? "Unknown"}");
            }
        }

        /// <summary>
        /// Find intruder at screen position
        /// </summary>
        private Intruder GetIntruderAtPosition(Vector2 screenPosition)
        {
            if (IntruderSpawner.Instance == null) return null;

            float hitboxMultiplier = gameConfig?.tapHitboxMultiplier ?? 1.5f;
            float effectiveRadius = targetingRadius * hitboxMultiplier;

            Intruder closest = null;
            float closestDistance = float.MaxValue;

            foreach (var intruder in IntruderSpawner.Instance.ActiveIntruders)
            {
                if (intruder == null || !intruder.IsVisible) continue;
                if (intruder.State == IntruderState.Gone || intruder.State == IntruderState.Returning) continue;

                // Check screen distance
                float distance = Vector2.Distance(screenPosition, intruder.ScreenPosition);

                // Adjust effective radius based on intruder scale
                float adjustedRadius = effectiveRadius * intruder.VisualScale;

                if (distance <= adjustedRadius && distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = intruder;
                }
            }

            return closest;
        }

        /// <summary>
        /// Check if tap is currently allowed (cooldown)
        /// </summary>
        public bool IsTapAllowed()
        {
            float cooldown = gameConfig?.TapCooldownSeconds ?? 0.1f;
            int maxTaps = gameConfig?.maxTapsPerSecond ?? 10;

            return (Time.time - lastTapTime >= cooldown) && (tapsThisSecond < maxTaps);
        }

        /// <summary>
        /// Get time until next tap allowed
        /// </summary>
        public float GetCooldownRemaining()
        {
            float cooldown = gameConfig?.TapCooldownSeconds ?? 0.1f;
            return Mathf.Max(0f, cooldown - (Time.time - lastTapTime));
        }

        /// <summary>
        /// Force target an intruder
        /// </summary>
        public void ForceTarget(Intruder intruder)
        {
            SetTarget(intruder);
        }

        /// <summary>
        /// Clear current target
        /// </summary>
        public void ClearTarget()
        {
            SetTarget(null);
        }

        /// <summary>
        /// Get debug info
        /// </summary>
        public string GetDebugInfo()
        {
            return $"Target: {currentTarget?.Data?.displayName ?? "None"}\n" +
                   $"Tap Rate: {tapRate:F1}/s\n" +
                   $"This Second: {tapsThisSecond}\n" +
                   $"Cooldown: {GetCooldownRemaining():F2}s\n" +
                   $"Is Tapping: {isTapping}";
        }

        private void OnGUI()
        {
            if (!showDebugInfo) return;

            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                normal = { textColor = Color.yellow }
            };

            GUI.Label(new Rect(10, Screen.height - 150, 300, 150), GetDebugInfo(), style);
        }
    }
}
