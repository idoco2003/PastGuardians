using UnityEngine;

namespace PastGuardians.Data
{
    /// <summary>
    /// Era categories for intruders - determines portal color and spawn grouping
    /// </summary>
    public enum IntruderEra
    {
        Prehistoric,        // Amber portals - dinosaurs and ancient creatures
        Mythological,       // Purple portals - griffins, dragons, phoenixes
        HistoricalMachines, // Bronze portals - Da Vinci ornithopters, steampunk
        LostCivilizations,  // Teal portals - Atlantean, Mayan, Egyptian
        TimeAnomalies       // Prismatic portals - paradoxes, echoes, rifts
    }

    /// <summary>
    /// Size classification affects altitude, tap window, and screen coverage
    /// </summary>
    public enum IntruderSize
    {
        Tiny,   // 5-10% screen, 30s window, 50-100m altitude
        Small,  // 10-20% screen, 45s window, 100-300m altitude
        Medium, // 20-35% screen, 60s window, 300-600m altitude
        Large,  // 35-50% screen, 90s window, 600-1000m altitude
        Boss    // 50-75% screen, 180s window, 1000m+ altitude
    }

    /// <summary>
    /// Movement patterns for intruder behavior in the sky
    /// </summary>
    public enum MovementPattern
    {
        Hover,    // Stationary with gentle bob
        Drift,    // Slow horizontal movement
        Circle,   // Orbits a fixed sky point
        Escape,   // Moves away when close to tap threshold
        Approach  // Slowly descends toward ground
    }

    /// <summary>
    /// ScriptableObject defining an intruder type's properties
    /// </summary>
    [CreateAssetMenu(fileName = "NewIntruder", menuName = "Past Guardians/Intruder Data", order = 1)]
    public class IntruderData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique identifier for this intruder type")]
        public string intruderId;

        [Tooltip("Display name shown to players")]
        public string displayName;

        [Header("Classification")]
        [Tooltip("Era category - determines portal color")]
        public IntruderEra era = IntruderEra.Prehistoric;

        [Tooltip("Size class - affects altitude and tap window")]
        public IntruderSize sizeClass = IntruderSize.Medium;

        [Header("Gameplay")]
        [Tooltip("Base number of taps required to return this intruder")]
        [Range(30, 500)]
        public int baseTapsRequired = 100;

        [Tooltip("How fast stability regenerates when not being tapped (0-1 per second)")]
        [Range(0f, 2f)]
        public float stabilityRegenRate = 0.5f;

        [Tooltip("Speed multiplier when escaping")]
        [Range(1f, 3f)]
        public float escapeSpeedMultiplier = 1.5f;

        [Tooltip("Base altitude in meters")]
        [Range(50f, 1500f)]
        public float baseAltitude = 300f;

        [Tooltip("Time window in seconds before intruder escapes")]
        public float tapWindowSeconds = 60f;

        [Tooltip("Movement behavior pattern")]
        public MovementPattern movementPattern = MovementPattern.Approach;

        [Header("Visuals")]
        [Tooltip("3D model prefab for this intruder")]
        public GameObject modelPrefab;

        [Tooltip("Portal color for this era")]
        public Color portalColor = new Color(1f, 0.75f, 0f); // Default amber

        [Header("Audio")]
        [Tooltip("Sound played when intruder spawns")]
        public AudioClip spawnSound;

        [Tooltip("Sound played when intruder returns to Tempus Prime")]
        public AudioClip returnSound;

        [Header("Codex")]
        [TextArea(3, 10)]
        [Tooltip("Lore description for the Tempus Codex")]
        public string codexDescription;

        /// <summary>
        /// Get the tap window based on size class
        /// </summary>
        public float GetTapWindow()
        {
            return sizeClass switch
            {
                IntruderSize.Tiny => 30f,
                IntruderSize.Small => 45f,
                IntruderSize.Medium => 60f,
                IntruderSize.Large => 90f,
                IntruderSize.Boss => 180f,
                _ => tapWindowSeconds
            };
        }

        /// <summary>
        /// Get default altitude range based on size class
        /// Now starts much higher (5000m) for dramatic descent
        /// </summary>
        public (float min, float max) GetAltitudeRange()
        {
            return sizeClass switch
            {
                IntruderSize.Tiny => (3000f, 4000f),
                IntruderSize.Small => (4000f, 5000f),
                IntruderSize.Medium => (5000f, 6000f),
                IntruderSize.Large => (6000f, 8000f),
                IntruderSize.Boss => (8000f, 10000f),
                _ => (5000f, 6000f)
            };
        }

        /// <summary>
        /// Get portal color based on era
        /// </summary>
        public Color GetEraPortalColor()
        {
            return era switch
            {
                IntruderEra.Prehistoric => new Color(1f, 0.75f, 0f),      // Amber #FFBF00
                IntruderEra.Mythological => new Color(0.61f, 0.19f, 1f),  // Purple #9B30FF
                IntruderEra.HistoricalMachines => new Color(0.8f, 0.5f, 0.2f), // Bronze #CD7F32
                IntruderEra.LostCivilizations => new Color(0f, 0.5f, 0.5f),    // Teal #008080
                IntruderEra.TimeAnomalies => Color.white, // Prismatic handled separately
                _ => portalColor
            };
        }
    }
}
