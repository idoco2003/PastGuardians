using UnityEngine;
using PastGuardians.Data;
using System;
using System.Collections.Generic;

namespace PastGuardians.Gameplay
{
    /// <summary>
    /// Data for a remote player's laser
    /// </summary>
    [Serializable]
    public class RemoteLaserData
    {
        public string playerId;
        public string city;
        public string country;
        public Color beamColor;
        public float lastTapTime;
        public Vector2 originDirection;  // Normalized direction from intruder
        public int tapCount;
    }

    /// <summary>
    /// Individual laser visual for remote players
    /// </summary>
    public class RemoteLaserVisual
    {
        public string PlayerId;
        public LineRenderer LineRenderer;
        public UI.LocationLabelUI Label;
        public float SpawnTime;
        public float LastUpdateTime;
        public bool IsVisible;
        public BoxCollider Collider;
    }

    /// <summary>
    /// Data for laser interaction UI
    /// </summary>
    [Serializable]
    public class LaserData
    {
        public string playerId;
        public string city;
        public Color color;
    }

    /// <summary>
    /// Displays laser beams from other players worldwide
    /// </summary>
    public class GlobalLaserDisplay : MonoBehaviour
    {
        public static GlobalLaserDisplay Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private int maxVisibleLasers = 20;
        [SerializeField] private float rotationInterval = 3f;

        [Header("Visual Settings")]
        [SerializeField] private float laserAlpha = 0.5f;
        [SerializeField] private float laserWidth = 0.03f;
        [SerializeField] private float originDistance = 5f;  // Distance from intruder for origin point

        [Header("Prefabs")]
        [SerializeField] private GameObject laserPrefab;
        [SerializeField] private GameObject labelPrefab;

        [Header("Container")]
        [SerializeField] private Transform laserContainer;

        // Active remote lasers
        private List<RemoteLaserData> allRemoteLasers = new List<RemoteLaserData>();
        private List<RemoteLaserData> displayedLasers = new List<RemoteLaserData>();
        private Dictionary<string, RemoteLaserVisual> laserVisuals = new Dictionary<string, RemoteLaserVisual>();

        // Rotation timing
        private float lastRotationTime;

        // Events
        public event Action<int> OnParticipantCountChanged;

        // Properties
        public int ParticipantCount => allRemoteLasers.Count;
        public IReadOnlyList<RemoteLaserData> DisplayedLasers => displayedLasers;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (laserContainer == null)
            {
                laserContainer = new GameObject("RemoteLasers").transform;
                laserContainer.SetParent(transform);
            }
        }

        private void Update()
        {
            // Rotate displayed lasers periodically
            if (Time.time - lastRotationTime > rotationInterval)
            {
                lastRotationTime = Time.time;
                UpdateDisplayedLasers();
            }

            // Update laser positions
            UpdateLaserPositions();

            // Check for laser clicks/taps
            CheckLaserInteraction();

            // Cleanup stale lasers
            CleanupStaleLasers();
        }

        /// <summary>
        /// Check for player tapping on a laser beam
        /// </summary>
        private void CheckLaserInteraction()
        {
            // Check for touch or mouse click
            bool isTapping = false;
            Vector2 tapPosition = Vector2.zero;

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    isTapping = true;
                    tapPosition = touch.position;
                }
            }
            else if (Input.GetMouseButtonDown(0))
            {
                isTapping = true;
                tapPosition = Input.mousePosition;
            }

            if (!isTapping) return;

            // Raycast to check if hitting a laser
            Ray ray = Camera.main.ScreenPointToRay(tapPosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 100f);

            foreach (var hit in hits)
            {
                // Check if hit a laser collider
                foreach (var kvp in laserVisuals)
                {
                    if (kvp.Value.Collider != null && hit.collider == kvp.Value.Collider)
                    {
                        OnLaserClicked(kvp.Key);
                        return;
                    }
                }
            }

            // Alternative: Check proximity to laser lines on screen
            foreach (var kvp in laserVisuals)
            {
                if (kvp.Value.LineRenderer == null || !kvp.Value.LineRenderer.enabled) continue;

                if (IsPointNearLine(tapPosition, kvp.Value.LineRenderer))
                {
                    OnLaserClicked(kvp.Key);
                    return;
                }
            }
        }

        /// <summary>
        /// Check if a screen point is near a laser line
        /// </summary>
        private bool IsPointNearLine(Vector2 screenPoint, LineRenderer lr)
        {
            if (lr.positionCount < 2) return false;

            Vector3 start = Camera.main.WorldToScreenPoint(lr.GetPosition(0));
            Vector3 end = Camera.main.WorldToScreenPoint(lr.GetPosition(1));

            // Calculate distance from point to line segment
            float distance = DistancePointToLineSegment(screenPoint, new Vector2(start.x, start.y), new Vector2(end.x, end.y));

            // Threshold for "near" the laser (in pixels)
            float threshold = 50f;
            return distance < threshold;
        }

        /// <summary>
        /// Calculate distance from point to line segment
        /// </summary>
        private float DistancePointToLineSegment(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
        {
            Vector2 lineDir = lineEnd - lineStart;
            float lineLength = lineDir.magnitude;

            if (lineLength < 0.001f) return Vector2.Distance(point, lineStart);

            lineDir /= lineLength;

            Vector2 pointDir = point - lineStart;
            float projection = Vector2.Dot(pointDir, lineDir);

            if (projection < 0) return Vector2.Distance(point, lineStart);
            if (projection > lineLength) return Vector2.Distance(point, lineEnd);

            Vector2 closestPoint = lineStart + lineDir * projection;
            return Vector2.Distance(point, closestPoint);
        }

        /// <summary>
        /// Handle laser being clicked
        /// </summary>
        private void OnLaserClicked(string playerId)
        {
            RemoteLaserData laserData = allRemoteLasers.Find(l => l.playerId == playerId);
            if (laserData == null) return;

            // Create interaction data
            LaserData data = new LaserData
            {
                playerId = laserData.playerId,
                city = laserData.city,
                color = laserData.beamColor
            };

            // Show interaction UI
            UI.PlayerInteractionUI.Instance?.ShowForLaser(data);

            Debug.Log($"[GlobalLaserDisplay] Clicked laser from {laserData.city}");
        }

        /// <summary>
        /// Receive laser updates from server
        /// </summary>
        public void ReceiveLaserUpdate(List<RemoteLaserData> lasers)
        {
            int oldCount = allRemoteLasers.Count;

            // Update existing and add new
            foreach (var laser in lasers)
            {
                int index = allRemoteLasers.FindIndex(l => l.playerId == laser.playerId);
                if (index >= 0)
                {
                    allRemoteLasers[index] = laser;
                }
                else
                {
                    allRemoteLasers.Add(laser);
                }
            }

            if (allRemoteLasers.Count != oldCount)
            {
                OnParticipantCountChanged?.Invoke(allRemoteLasers.Count);
            }

            UpdateDisplayedLasers();
        }

        /// <summary>
        /// Add a single remote laser
        /// </summary>
        public void AddRemoteLaser(RemoteLaserData laser)
        {
            int index = allRemoteLasers.FindIndex(l => l.playerId == laser.playerId);
            if (index >= 0)
            {
                allRemoteLasers[index] = laser;
            }
            else
            {
                allRemoteLasers.Add(laser);
                OnParticipantCountChanged?.Invoke(allRemoteLasers.Count);
            }
        }

        /// <summary>
        /// Remove a remote laser
        /// </summary>
        public void RemoveRemoteLaser(string playerId)
        {
            allRemoteLasers.RemoveAll(l => l.playerId == playerId);
            DespawnLaserVisual(playerId);
            OnParticipantCountChanged?.Invoke(allRemoteLasers.Count);
        }

        /// <summary>
        /// Select which lasers to display
        /// </summary>
        private void UpdateDisplayedLasers()
        {
            int maxLasers = gameConfig?.maxVisibleLasers ?? maxVisibleLasers;
            displayedLasers = SelectLasersToDisplay(allRemoteLasers, maxLasers);

            // Spawn/despawn visuals as needed
            HashSet<string> displayedIds = new HashSet<string>();
            foreach (var laser in displayedLasers)
            {
                displayedIds.Add(laser.playerId);

                if (!laserVisuals.ContainsKey(laser.playerId))
                {
                    SpawnLaserVisual(laser);
                }
            }

            // Despawn lasers not in display list
            List<string> toRemove = new List<string>();
            foreach (var kvp in laserVisuals)
            {
                if (!displayedIds.Contains(kvp.Key))
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var id in toRemove)
            {
                DespawnLaserVisual(id);
            }
        }

        /// <summary>
        /// Select lasers prioritizing diversity and contribution
        /// </summary>
        private List<RemoteLaserData> SelectLasersToDisplay(List<RemoteLaserData> all, int maxCount)
        {
            if (all.Count <= maxCount)
                return new List<RemoteLaserData>(all);

            // Sort by tap count (contribution) descending
            var sorted = new List<RemoteLaserData>(all);
            sorted.Sort((a, b) => b.tapCount.CompareTo(a.tapCount));

            // Select with geographic diversity
            List<RemoteLaserData> selected = new List<RemoteLaserData>();
            HashSet<string> usedCountries = new HashSet<string>();

            // First pass: one per country
            foreach (var laser in sorted)
            {
                if (selected.Count >= maxCount) break;

                if (!usedCountries.Contains(laser.country))
                {
                    selected.Add(laser);
                    usedCountries.Add(laser.country);
                }
            }

            // Second pass: fill remaining slots with top contributors
            foreach (var laser in sorted)
            {
                if (selected.Count >= maxCount) break;

                if (!selected.Contains(laser))
                {
                    selected.Add(laser);
                }
            }

            return selected;
        }

        /// <summary>
        /// Spawn visual for a remote laser
        /// </summary>
        private void SpawnLaserVisual(RemoteLaserData data)
        {
            GameObject laserObj;

            if (laserPrefab != null)
            {
                laserObj = Instantiate(laserPrefab, laserContainer);
            }
            else
            {
                laserObj = new GameObject($"RemoteLaser_{data.playerId}");
                laserObj.transform.SetParent(laserContainer);
            }

            // Setup line renderer
            LineRenderer lr = laserObj.GetComponent<LineRenderer>();
            if (lr == null)
            {
                lr = laserObj.AddComponent<LineRenderer>();
            }

            lr.positionCount = 2;
            lr.startWidth = laserWidth;
            lr.endWidth = laserWidth * 0.5f;
            lr.useWorldSpace = true;

            // Set color with transparency
            Color color = data.beamColor;
            color.a = laserAlpha;

            Material mat = new Material(Shader.Find("Sprites/Default"));
            lr.material = mat;

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(laserAlpha, 0f), new GradientAlphaKey(laserAlpha * 0.5f, 1f) }
            );
            lr.colorGradient = gradient;

            // Create label
            UI.LocationLabelUI label = null;
            if (labelPrefab != null)
            {
                GameObject labelObj = Instantiate(labelPrefab, laserContainer);
                label = labelObj.GetComponent<UI.LocationLabelUI>();
                if (label != null)
                {
                    label.SetLabel(data.city, data.country);
                }
            }

            // Store visual
            RemoteLaserVisual visual = new RemoteLaserVisual
            {
                PlayerId = data.playerId,
                LineRenderer = lr,
                Label = label,
                SpawnTime = Time.time,
                LastUpdateTime = Time.time,
                IsVisible = true
            };

            laserVisuals[data.playerId] = visual;
        }

        /// <summary>
        /// Despawn a laser visual
        /// </summary>
        private void DespawnLaserVisual(string playerId)
        {
            if (laserVisuals.TryGetValue(playerId, out RemoteLaserVisual visual))
            {
                if (visual.LineRenderer != null)
                {
                    Destroy(visual.LineRenderer.gameObject);
                }
                if (visual.Label != null)
                {
                    Destroy(visual.Label.gameObject);
                }

                laserVisuals.Remove(playerId);
            }
        }

        /// <summary>
        /// Update laser positions based on current target
        /// </summary>
        private void UpdateLaserPositions()
        {
            Intruder target = TapInputHandler.Instance?.CurrentTarget;
            if (target == null || !target.IsVisible)
            {
                // Hide all lasers when no target
                foreach (var visual in laserVisuals.Values)
                {
                    if (visual.LineRenderer != null)
                    {
                        visual.LineRenderer.enabled = false;
                    }
                    if (visual.Label != null)
                    {
                        visual.Label.gameObject.SetActive(false);
                    }
                }
                return;
            }

            // Target position in world space
            Vector3 targetScreen = new Vector3(target.ScreenPosition.x, target.ScreenPosition.y, 10f);
            Vector3 targetWorld = Camera.main.ScreenToWorldPoint(targetScreen);

            int index = 0;
            foreach (var kvp in laserVisuals)
            {
                RemoteLaserVisual visual = kvp.Value;
                RemoteLaserData data = displayedLasers.Find(l => l.playerId == kvp.Key);

                if (visual.LineRenderer == null || data == null) continue;

                visual.LineRenderer.enabled = true;

                // Calculate origin position on screen edge based on direction
                Vector2 direction = data.originDirection;
                if (direction == Vector2.zero)
                {
                    // Distribute around the target
                    float angle = (index / (float)displayedLasers.Count) * 360f * Mathf.Deg2Rad;
                    direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                }

                // Calculate screen edge position
                Vector2 screenCenter = target.ScreenPosition;
                Vector2 edgePoint = GetScreenEdgePoint(screenCenter, direction);

                Vector3 originWorld = Camera.main.ScreenToWorldPoint(new Vector3(edgePoint.x, edgePoint.y, originDistance));

                visual.LineRenderer.SetPosition(0, originWorld);
                visual.LineRenderer.SetPosition(1, targetWorld);

                // Update label position
                if (visual.Label != null)
                {
                    visual.Label.gameObject.SetActive(true);
                    visual.Label.SetPosition(edgePoint);
                }

                index++;
            }
        }

        /// <summary>
        /// Get point on screen edge in direction from center
        /// </summary>
        private Vector2 GetScreenEdgePoint(Vector2 center, Vector2 direction)
        {
            direction = direction.normalized;

            float padding = 50f;  // Padding from edge

            // Calculate intersection with screen edges
            float maxX = Screen.width - padding;
            float maxY = Screen.height - padding;
            float minX = padding;
            float minY = padding;

            Vector2 edge = center;

            if (direction.x != 0)
            {
                float t1 = (minX - center.x) / direction.x;
                float t2 = (maxX - center.x) / direction.x;

                if (t1 > 0 && (t1 < t2 || t2 <= 0))
                {
                    edge = center + direction * t1;
                }
                else if (t2 > 0)
                {
                    edge = center + direction * t2;
                }
            }

            if (direction.y != 0)
            {
                float t1 = (minY - center.y) / direction.y;
                float t2 = (maxY - center.y) / direction.y;

                float t = (t1 > 0 && (t1 < t2 || t2 <= 0)) ? t1 : t2;
                if (t > 0)
                {
                    Vector2 candidate = center + direction * t;
                    if (Vector2.Distance(center, candidate) < Vector2.Distance(center, edge))
                    {
                        edge = candidate;
                    }
                }
            }

            // Clamp to screen
            edge.x = Mathf.Clamp(edge.x, minX, maxX);
            edge.y = Mathf.Clamp(edge.y, minY, maxY);

            return edge;
        }

        /// <summary>
        /// Cleanup lasers that haven't been updated recently
        /// </summary>
        private void CleanupStaleLasers()
        {
            float staleThreshold = gameConfig?.otherLaserPersistSeconds ?? 3f;
            float currentTime = Time.time;

            List<string> toRemove = new List<string>();

            foreach (var laser in allRemoteLasers)
            {
                if (currentTime - laser.lastTapTime > staleThreshold)
                {
                    toRemove.Add(laser.playerId);
                }
            }

            foreach (var id in toRemove)
            {
                allRemoteLasers.RemoveAll(l => l.playerId == id);
                DespawnLaserVisual(id);
            }

            if (toRemove.Count > 0)
            {
                OnParticipantCountChanged?.Invoke(allRemoteLasers.Count);
            }
        }

        /// <summary>
        /// Clear all remote lasers
        /// </summary>
        public void ClearAll()
        {
            foreach (var id in new List<string>(laserVisuals.Keys))
            {
                DespawnLaserVisual(id);
            }

            allRemoteLasers.Clear();
            displayedLasers.Clear();
            OnParticipantCountChanged?.Invoke(0);
        }

        /// <summary>
        /// Get unique countries from participants
        /// </summary>
        public int GetUniqueCountryCount()
        {
            HashSet<string> countries = new HashSet<string>();
            foreach (var laser in allRemoteLasers)
            {
                if (!string.IsNullOrEmpty(laser.country))
                {
                    countries.Add(laser.country);
                }
            }
            return countries.Count;
        }

        /// <summary>
        /// Get debug info
        /// </summary>
        public string GetDebugInfo()
        {
            return $"Total Participants: {allRemoteLasers.Count}\n" +
                   $"Displayed: {displayedLasers.Count}\n" +
                   $"Countries: {GetUniqueCountryCount()}";
        }
    }
}
