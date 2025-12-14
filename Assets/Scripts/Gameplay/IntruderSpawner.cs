using UnityEngine;
using PastGuardians.Data;
using PastGuardians.AR;
using System.Collections.Generic;
using System;

namespace PastGuardians.Gameplay
{
    /// <summary>
    /// Manages spawning and tracking of intruders
    /// </summary>
    public class IntruderSpawner : MonoBehaviour
    {
        public static IntruderSpawner Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private List<IntruderData> availableIntruderTypes = new List<IntruderData>();

        [Header("Prefab")]
        [SerializeField] private GameObject defaultIntruderPrefab;

        [Header("Spawning")]
        [SerializeField] private bool autoSpawnEnabled = true;
        [SerializeField] private float spawnCheckInterval = 5f;

        [Header("Debug")]
        [SerializeField] private bool debugMode = false;

        // Active intruders
        private List<Intruder> activeIntruders = new List<Intruder>();
        private Dictionary<string, Intruder> intruderById = new Dictionary<string, Intruder>();

        // Spawn timing
        private float lastSpawnCheckTime;
        private float timeSinceLastSpawn;

        // Events
        public event Action<Intruder> OnIntruderSpawned;
        public event Action<Intruder> OnIntruderDespawned;
        public event Action<Intruder> OnIntruderReturned;

        // Properties
        public IReadOnlyList<Intruder> ActiveIntruders => activeIntruders;
        public int ActiveIntruderCount => activeIntruders.Count;
        public GameConfig Config => gameConfig;

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
            // Check for spawning
            if (autoSpawnEnabled && Time.time - lastSpawnCheckTime > spawnCheckInterval)
            {
                lastSpawnCheckTime = Time.time;
                CheckForSpawn();
            }

            // Clean up destroyed intruders
            CleanupIntruders();
        }

        /// <summary>
        /// Check if we should spawn a new intruder
        /// </summary>
        private void CheckForSpawn()
        {
            if (gameConfig == null) return;
            if (activeIntruders.Count >= gameConfig.globalIntruderCap) return;
            if (LocationManager.Instance == null || !LocationManager.Instance.LocationAvailable) return;

            timeSinceLastSpawn += spawnCheckInterval;

            if (timeSinceLastSpawn >= gameConfig.SpawnIntervalSeconds)
            {
                timeSinceLastSpawn = 0f;
                SpawnRandomIntruder();
            }
        }

        /// <summary>
        /// Spawn a random intruder near the player
        /// </summary>
        public Intruder SpawnRandomIntruder()
        {
            if (availableIntruderTypes.Count == 0)
            {
                Debug.LogWarning("[IntruderSpawner] No intruder types available!");
                return null;
            }

            // Select intruder type based on era weights
            IntruderData selectedType = SelectIntruderType();
            if (selectedType == null) return null;

            // Generate position near player
            Vector2 offset = GetRandomSpawnOffset();
            double playerLat = LocationManager.Instance?.Latitude ?? 0;
            double playerLon = LocationManager.Instance?.Longitude ?? 0;

            // Convert offset to lat/lon (rough approximation)
            double lat = playerLat + (offset.y / 111f); // ~111km per degree latitude
            double lon = playerLon + (offset.x / (111f * Mathf.Cos((float)playerLat * Mathf.Deg2Rad)));

            // Get altitude based on size class
            var altRange = selectedType.GetAltitudeRange();
            float altitude = UnityEngine.Random.Range(altRange.min, altRange.max);

            return SpawnIntruder(selectedType, lat, lon, altitude);
        }

        /// <summary>
        /// Select intruder type based on era weights
        /// </summary>
        private IntruderData SelectIntruderType()
        {
            if (availableIntruderTypes.Count == 0) return null;

            // Check for boss spawn
            if (gameConfig != null && UnityEngine.Random.value < gameConfig.bossSpawnChance)
            {
                var bosses = availableIntruderTypes.FindAll(i => i.sizeClass == IntruderSize.Boss);
                if (bosses.Count > 0)
                {
                    return bosses[UnityEngine.Random.Range(0, bosses.Count)];
                }
            }

            // Weight by era
            float totalWeight = 0f;
            foreach (var intruder in availableIntruderTypes)
            {
                if (intruder.sizeClass != IntruderSize.Boss)
                {
                    totalWeight += gameConfig?.GetEraWeight(intruder.era) ?? 0.2f;
                }
            }

            float random = UnityEngine.Random.value * totalWeight;
            float accumulated = 0f;

            foreach (var intruder in availableIntruderTypes)
            {
                if (intruder.sizeClass == IntruderSize.Boss) continue;

                accumulated += gameConfig?.GetEraWeight(intruder.era) ?? 0.2f;
                if (random <= accumulated)
                {
                    return intruder;
                }
            }

            // Fallback to random
            return availableIntruderTypes[UnityEngine.Random.Range(0, availableIntruderTypes.Count)];
        }

        /// <summary>
        /// Get random spawn offset in km
        /// </summary>
        private Vector2 GetRandomSpawnOffset()
        {
            float minDistance = 1f;  // 1km minimum
            float maxDistance = gameConfig?.localRangeKm ?? 50f;

            float distance = UnityEngine.Random.Range(minDistance, maxDistance);
            float angle = UnityEngine.Random.value * 360f * Mathf.Deg2Rad;

            return new Vector2(
                Mathf.Cos(angle) * distance,
                Mathf.Sin(angle) * distance
            );
        }

        /// <summary>
        /// Spawn a specific intruder
        /// </summary>
        public Intruder SpawnIntruder(IntruderData type, double lat, double lon, float altitude, string instanceId = null)
        {
            if (type == null)
            {
                Debug.LogError("[IntruderSpawner] Cannot spawn null intruder type");
                return null;
            }

            // Create prefab
            GameObject prefab = type.modelPrefab ?? defaultIntruderPrefab;
            if (prefab == null)
            {
                // Create a simple default if no prefab
                prefab = CreateDefaultIntruderObject(type);
            }
            else
            {
                prefab = Instantiate(prefab, transform);
            }

            // Add Intruder component if not present
            Intruder intruder = prefab.GetComponent<Intruder>();
            if (intruder == null)
            {
                intruder = prefab.AddComponent<Intruder>();
            }

            // Initialize
            intruder.Initialize(type, lat, lon, altitude, instanceId);

            // Subscribe to events
            intruder.OnReturnCompleted += HandleIntruderReturned;
            intruder.OnTimeExpired += HandleIntruderExpired;

            // Track
            activeIntruders.Add(intruder);
            intruderById[intruder.InstanceId] = intruder;

            OnIntruderSpawned?.Invoke(intruder);
            Debug.Log($"[IntruderSpawner] Spawned {type.displayName} ({intruder.InstanceId})");

            return intruder;
        }

        /// <summary>
        /// Create a default intruder object if no prefab
        /// </summary>
        private GameObject CreateDefaultIntruderObject(IntruderData type)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = $"Intruder_{type.displayName}";
            obj.transform.SetParent(transform);

            // Set scale based on size class - DOUBLED for visibility
            float scale = type.sizeClass switch
            {
                IntruderSize.Tiny => 1f,
                IntruderSize.Small => 2f,
                IntruderSize.Medium => 4f,
                IntruderSize.Large => 6f,
                IntruderSize.Boss => 10f,
                _ => 2f
            };
            obj.transform.localScale = Vector3.one * scale;

            // Set color based on era
            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = type.GetEraPortalColor();
                renderer.material = mat;
            }

            return obj;
        }

        /// <summary>
        /// Handle intruder returned to Tempus Prime
        /// </summary>
        private void HandleIntruderReturned(Intruder intruder)
        {
            OnIntruderReturned?.Invoke(intruder);
            RemoveIntruder(intruder);
        }

        /// <summary>
        /// Handle intruder time expired
        /// </summary>
        private void HandleIntruderExpired(Intruder intruder)
        {
            OnIntruderDespawned?.Invoke(intruder);
            RemoveIntruder(intruder);
        }

        /// <summary>
        /// Remove intruder from tracking
        /// </summary>
        private void RemoveIntruder(Intruder intruder)
        {
            if (intruder == null) return;

            intruder.OnReturnCompleted -= HandleIntruderReturned;
            intruder.OnTimeExpired -= HandleIntruderExpired;

            activeIntruders.Remove(intruder);
            intruderById.Remove(intruder.InstanceId);

            // Destroy after short delay for animations
            if (intruder.gameObject != null)
            {
                Destroy(intruder.gameObject, 3f);
            }
        }

        /// <summary>
        /// Clean up null references
        /// </summary>
        private void CleanupIntruders()
        {
            activeIntruders.RemoveAll(i => i == null);
        }

        /// <summary>
        /// Get intruder by instance ID
        /// </summary>
        public Intruder GetIntruder(string instanceId)
        {
            intruderById.TryGetValue(instanceId, out Intruder intruder);
            return intruder;
        }

        /// <summary>
        /// Get intruders within range of player
        /// </summary>
        public List<Intruder> GetIntrudersInRange(float rangeKm)
        {
            List<Intruder> inRange = new List<Intruder>();

            if (LocationManager.Instance == null) return inRange;

            foreach (var intruder in activeIntruders)
            {
                if (intruder == null) continue;

                float distance = LocationManager.Instance.DistanceTo(intruder.Latitude, intruder.Longitude);
                if (distance <= rangeKm)
                {
                    inRange.Add(intruder);
                }
            }

            return inRange;
        }

        /// <summary>
        /// Get closest intruder to player
        /// </summary>
        public Intruder GetClosestIntruder()
        {
            if (LocationManager.Instance == null || activeIntruders.Count == 0)
                return null;

            Intruder closest = null;
            float closestDistance = float.MaxValue;

            foreach (var intruder in activeIntruders)
            {
                if (intruder == null) continue;

                float distance = LocationManager.Instance.DistanceTo(intruder.Latitude, intruder.Longitude);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = intruder;
                }
            }

            return closest;
        }

        /// <summary>
        /// Force despawn all intruders
        /// </summary>
        public void DespawnAll()
        {
            foreach (var intruder in activeIntruders.ToArray())
            {
                if (intruder != null)
                {
                    intruder.Despawn();
                }
            }

            activeIntruders.Clear();
            intruderById.Clear();
        }

        /// <summary>
        /// Spawn intruder directly above player (for testing)
        /// </summary>
        [ContextMenu("Spawn Test Intruder")]
        public void SpawnTestIntruder()
        {
            if (availableIntruderTypes.Count == 0)
            {
                Debug.LogWarning("[IntruderSpawner] No intruder types configured!");
                return;
            }

            var type = availableIntruderTypes[UnityEngine.Random.Range(0, availableIntruderTypes.Count)];
            double lat = LocationManager.Instance?.Latitude ?? 40.7128;
            double lon = LocationManager.Instance?.Longitude ?? -74.006;

            // Use proper altitude from intruder type (starts high, descends)
            var altRange = type.GetAltitudeRange();
            float alt = UnityEngine.Random.Range(altRange.min, altRange.max);

            SpawnIntruder(type, lat, lon, alt);
        }

        /// <summary>
        /// Add intruder type to available pool
        /// </summary>
        public void RegisterIntruderType(IntruderData type)
        {
            if (type != null && !availableIntruderTypes.Contains(type))
            {
                availableIntruderTypes.Add(type);
            }
        }

        /// <summary>
        /// Get debug info
        /// </summary>
        public string GetDebugInfo()
        {
            return $"Active Intruders: {activeIntruders.Count}\n" +
                   $"Types Available: {availableIntruderTypes.Count}\n" +
                   $"Auto Spawn: {autoSpawnEnabled}\n" +
                   $"Next Spawn In: {gameConfig?.SpawnIntervalSeconds - timeSinceLastSpawn:F1}s";
        }
    }
}
