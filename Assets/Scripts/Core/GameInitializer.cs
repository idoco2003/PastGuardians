using UnityEngine;
using UnityEngine.XR.ARFoundation;
using PastGuardians.Data;
using PastGuardians.AR;
using PastGuardians.Gameplay;
using PastGuardians.UI;
using PastGuardians.Network;
using PastGuardians.DevTools;
using System.Collections;
using System.Collections.Generic;

namespace PastGuardians.Core
{
    /// <summary>
    /// Master initializer that sets up the entire game at runtime
    /// Creates all necessary objects, managers, and configuration
    /// </summary>
    public class GameInitializer : MonoBehaviour
    {
        private static bool hasInitialized = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeBeforeScene()
        {
            if (hasInitialized) return;
            hasInitialized = true;

            Debug.Log("[GameInitializer] Starting Past Guardians initialization...");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeAfterScene()
        {
            // Create the initializer object
            GameObject initObj = new GameObject("GameInitializer");
            var initializer = initObj.AddComponent<GameInitializer>();
            DontDestroyOnLoad(initObj);
        }

        private void Start()
        {
            StartCoroutine(InitializeGame());
        }

        private IEnumerator InitializeGame()
        {
            Debug.Log("[GameInitializer] Setting up game systems...");

            // Step 1: Create GameConfig
            GameConfig config = CreateGameConfig();
            yield return null;

            // Step 2: Create IntruderData assets
            List<IntruderData> intruderTypes = CreateAllIntruderTypes();
            yield return null;

            // Step 3: Set up AR Scene
            SetupARScene();
            yield return null;

            // Step 4: Create Game Managers
            CreateGameManagers(config, intruderTypes);
            yield return null;

            // Step 5: Create Audio Manager
            CreateAudioManager();
            yield return null;

            Debug.Log("[GameInitializer] Game initialization complete!");
            Debug.Log($"[GameInitializer] Created {intruderTypes.Count} intruder types");
        }

        /// <summary>
        /// Create GameConfig with all default values
        /// </summary>
        private GameConfig CreateGameConfig()
        {
            GameConfig config = ScriptableObject.CreateInstance<GameConfig>();

            // Values are set in the ScriptableObject defaults
            Debug.Log("[GameInitializer] GameConfig created");
            return config;
        }

        /// <summary>
        /// Create all intruder type definitions
        /// </summary>
        private List<IntruderData> CreateAllIntruderTypes()
        {
            List<IntruderData> types = new List<IntruderData>();

            // ===== PREHISTORIC (Amber) =====
            types.Add(CreateIntruder("pteranodon", "Pteranodon", IntruderEra.Prehistoric, IntruderSize.Small, 50,
                "A flying reptile from the Late Cretaceous period. Not actually a dinosaur, but a pterosaur!"));

            types.Add(CreateIntruder("archaeopteryx", "Archaeopteryx", IntruderEra.Prehistoric, IntruderSize.Tiny, 30,
                "One of the first birds, with both feathers and teeth. A bridge between dinosaurs and modern birds."));

            types.Add(CreateIntruder("quetzalcoatlus", "Quetzalcoatlus", IntruderEra.Prehistoric, IntruderSize.Large, 200,
                "The largest flying animal ever! With a wingspan of 36 feet, it ruled the ancient skies."));

            types.Add(CreateIntruder("mosasaurus", "Mosasaurus", IntruderEra.Prehistoric, IntruderSize.Boss, 500,
                "A massive marine reptile breaching through a water-portal. The terror of ancient seas!"));

            types.Add(CreateIntruder("trex_peek", "T-Rex (Peeking)", IntruderEra.Prehistoric, IntruderSize.Medium, 150,
                "The king of dinosaurs, curiously poking its head through a time rift. Those tiny arms can't quite reach through!"));

            // ===== MYTHOLOGICAL (Purple) =====
            types.Add(CreateIntruder("griffin", "Griffin", IntruderEra.Mythological, IntruderSize.Medium, 100,
                "Half eagle, half lion - the majestic guardian of treasures from ancient legends."));

            types.Add(CreateIntruder("phoenix", "Phoenix", IntruderEra.Mythological, IntruderSize.Medium, 80,
                "The immortal firebird that rises from its own ashes. Its flames are warm, not harmful."));

            types.Add(CreateIntruder("pegasus", "Pegasus", IntruderEra.Mythological, IntruderSize.Medium, 60,
                "The divine winged horse of Greek mythology. Pure white and graceful in flight."));

            types.Add(CreateIntruder("dragon_eastern", "Eastern Dragon", IntruderEra.Mythological, IntruderSize.Large, 300,
                "A wise and benevolent serpentine dragon from Asian legends. Bringer of rain and good fortune."));

            types.Add(CreateIntruder("dragon_western", "Western Dragon", IntruderEra.Mythological, IntruderSize.Large, 350,
                "A powerful winged dragon from European tales. Impressive but not as scary as stories say!"));

            types.Add(CreateIntruder("thunderbird", "Thunderbird", IntruderEra.Mythological, IntruderSize.Medium, 150,
                "A legendary creature from Native American mythology. Its wingbeats create thunder!"));

            types.Add(CreateIntruder("roc", "Roc", IntruderEra.Mythological, IntruderSize.Boss, 400,
                "A bird so massive it could carry elephants! From Arabian mythology."));

            // ===== HISTORICAL MACHINES (Bronze) =====
            types.Add(CreateIntruder("ornithopter", "Da Vinci Ornithopter", IntruderEra.HistoricalMachines, IntruderSize.Small, 70,
                "Leonardo da Vinci's flying machine design, brought to life through time!"));

            types.Add(CreateIntruder("airship", "Steampunk Airship", IntruderEra.HistoricalMachines, IntruderSize.Large, 200,
                "A Victorian-era flying vessel powered by steam and imagination."));

            types.Add(CreateIntruder("clockwork_bird", "Clockwork Bird", IntruderEra.HistoricalMachines, IntruderSize.Tiny, 40,
                "A mechanical songbird with gears and springs. It still chirps its ancient tune!"));

            types.Add(CreateIntruder("war_balloon", "Ancient War Balloon", IntruderEra.HistoricalMachines, IntruderSize.Medium, 120,
                "An early military observation balloon, floating through from a past war."));

            types.Add(CreateIntruder("tesla_drone", "Tesla Drone", IntruderEra.HistoricalMachines, IntruderSize.Small, 90,
                "An electric flying device from Nikola Tesla's secret workshop. Sparks with energy!"));

            types.Add(CreateIntruder("icarus_wings", "Icarus Wings", IntruderEra.HistoricalMachines, IntruderSize.Medium, 100,
                "A brave pilot wearing wax and feather wings. Don't worry - our sky is safe for them!"));

            // ===== LOST CIVILIZATIONS (Teal) =====
            types.Add(CreateIntruder("atlantean_craft", "Atlantean Hovercraft", IntruderEra.LostCivilizations, IntruderSize.Medium, 150,
                "Advanced technology from the lost city of Atlantis. Runs on crystal power."));

            types.Add(CreateIntruder("mayan_serpent", "Mayan Sky Serpent", IntruderEra.LostCivilizations, IntruderSize.Medium, 180,
                "Quetzalcoatl's stone guardian, covered in glowing Mayan glyphs."));

            types.Add(CreateIntruder("egyptian_disc", "Egyptian Sun Disc", IntruderEra.LostCivilizations, IntruderSize.Small, 100,
                "A golden disc representing Ra, the sun god. It radiates warm light."));

            types.Add(CreateIntruder("lemuria_ship", "Lemurian Crystal Ship", IntruderEra.LostCivilizations, IntruderSize.Large, 250,
                "A crystalline vessel from the legendary continent of Lemuria."));

            types.Add(CreateIntruder("mu_temple", "Mu Temple Fragment", IntruderEra.LostCivilizations, IntruderSize.Large, 300,
                "A floating piece of an ancient temple from the lost continent of Mu."));

            // ===== TIME ANOMALIES (Prismatic) =====
            types.Add(CreateIntruder("time_echo", "Time Echo", IntruderEra.TimeAnomalies, IntruderSize.Small, 60,
                "A ghostly silhouette - an echo of someone from another time."));

            types.Add(CreateIntruder("paradox_sphere", "Paradox Sphere", IntruderEra.TimeAnomalies, IntruderSize.Medium, 120,
                "A mysterious orb showing glimpses of multiple possible timelines."));

            types.Add(CreateIntruder("yesterdays_cloud", "Yesterday's Cloud", IntruderEra.TimeAnomalies, IntruderSize.Medium, 80,
                "Weather from a different day, floating through the wrong time."));

            types.Add(CreateIntruder("future_fragment", "Future Fragment", IntruderEra.TimeAnomalies, IntruderSize.Large, 200,
                "A glitching, pixelated object from a time yet to come."));

            types.Add(CreateIntruder("epoch_rift", "Epoch Rift", IntruderEra.TimeAnomalies, IntruderSize.Boss, 500,
                "A massive tear in time itself! Requires global cooperation to seal."));

            Debug.Log($"[GameInitializer] Created {types.Count} intruder types");
            return types;
        }

        /// <summary>
        /// Helper to create an IntruderData instance
        /// </summary>
        private IntruderData CreateIntruder(string id, string name, IntruderEra era, IntruderSize size, int taps, string description)
        {
            IntruderData data = ScriptableObject.CreateInstance<IntruderData>();
            data.intruderId = id;
            data.displayName = name;
            data.era = era;
            data.sizeClass = size;
            data.baseTapsRequired = taps;
            data.codexDescription = description;
            data.movementPattern = MovementPattern.Approach; // Default to descending
            data.stabilityRegenRate = 0.5f;
            data.escapeSpeedMultiplier = 1.5f;

            // Set altitude based on size
            var altRange = data.GetAltitudeRange();
            data.baseAltitude = (altRange.min + altRange.max) / 2f;

            // Set tap window based on size
            data.tapWindowSeconds = data.GetTapWindow();

            // Portal color is determined by GetEraPortalColor()
            data.portalColor = data.GetEraPortalColor();

            return data;
        }

        /// <summary>
        /// Set up AR Session and XR Origin
        /// </summary>
        private void SetupARScene()
        {
            // Check if AR Session already exists
            ARSession existingSession = FindObjectOfType<ARSession>();
            if (existingSession != null)
            {
                Debug.Log("[GameInitializer] AR Session already exists");
                return;
            }

            // Create AR Session
            GameObject arSessionObj = new GameObject("AR Session");
            arSessionObj.AddComponent<ARSession>();
            arSessionObj.AddComponent<ARInputManager>();
            DontDestroyOnLoad(arSessionObj);

            // Check for existing camera/XR Origin
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                // Create XR Origin with camera
                GameObject xrOriginObj = new GameObject("XR Origin");

                // Camera Offset
                GameObject cameraOffset = new GameObject("Camera Offset");
                cameraOffset.transform.SetParent(xrOriginObj.transform);

                // AR Camera
                GameObject arCameraObj = new GameObject("AR Camera");
                arCameraObj.transform.SetParent(cameraOffset.transform);
                arCameraObj.tag = "MainCamera";

                Camera arCam = arCameraObj.AddComponent<Camera>();
                arCam.clearFlags = CameraClearFlags.SolidColor;
                arCam.backgroundColor = Color.black;
                arCam.nearClipPlane = 0.1f;
                arCam.farClipPlane = 1000f;

                arCameraObj.AddComponent<ARCameraManager>();
                arCameraObj.AddComponent<ARCameraBackground>();

                // Add TrackedPoseDriver for AR camera movement
                var poseDriver = arCameraObj.AddComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();

                DontDestroyOnLoad(xrOriginObj);
                Debug.Log("[GameInitializer] Created AR Camera setup");
            }
            else
            {
                // Ensure existing camera has AR components
                if (mainCam.GetComponent<ARCameraManager>() == null)
                {
                    mainCam.gameObject.AddComponent<ARCameraManager>();
                }
                if (mainCam.GetComponent<ARCameraBackground>() == null)
                {
                    mainCam.gameObject.AddComponent<ARCameraBackground>();
                }
                Debug.Log("[GameInitializer] Added AR components to existing camera");
            }

            Debug.Log("[GameInitializer] AR Scene setup complete");
        }

        /// <summary>
        /// Create all game manager objects
        /// </summary>
        private void CreateGameManagers(GameConfig config, List<IntruderData> intruderTypes)
        {
            // Game Managers container
            GameObject managersObj = new GameObject("GameManagers");
            DontDestroyOnLoad(managersObj);

            // Player Data Manager
            if (PlayerDataManager.Instance == null)
            {
                var pdm = managersObj.AddComponent<PlayerDataManager>();
                // Config will be assigned via serialized field or we set it here
                Debug.Log("[GameInitializer] Created PlayerDataManager");
            }

            // Location Manager
            if (LocationManager.Instance == null)
            {
                managersObj.AddComponent<LocationManager>();
                Debug.Log("[GameInitializer] Created LocationManager");
            }

            // Sky Camera Controller
            if (SkyCameraController.Instance == null)
            {
                managersObj.AddComponent<SkyCameraController>();
                Debug.Log("[GameInitializer] Created SkyCameraController");
            }

            // Intruder Spawner
            if (IntruderSpawner.Instance == null)
            {
                var spawner = managersObj.AddComponent<IntruderSpawner>();
                // Assign intruder types
                SetIntruderSpawnerTypes(spawner, intruderTypes);
                Debug.Log("[GameInitializer] Created IntruderSpawner");
            }

            // Intruder Screen Positioner
            if (IntruderScreenPositioner.Instance == null)
            {
                managersObj.AddComponent<IntruderScreenPositioner>();
                Debug.Log("[GameInitializer] Created IntruderScreenPositioner");
            }

            // Tap Input Handler
            if (TapInputHandler.Instance == null)
            {
                managersObj.AddComponent<TapInputHandler>();
                Debug.Log("[GameInitializer] Created TapInputHandler");
            }

            // Tap Progress Manager
            if (TapProgressManager.Instance == null)
            {
                managersObj.AddComponent<TapProgressManager>();
                Debug.Log("[GameInitializer] Created TapProgressManager");
            }

            // Player Laser Renderer
            if (PlayerLaserRenderer.Instance == null)
            {
                managersObj.AddComponent<PlayerLaserRenderer>();
                Debug.Log("[GameInitializer] Created PlayerLaserRenderer");
            }

            // Global Laser Display
            if (GlobalLaserDisplay.Instance == null)
            {
                managersObj.AddComponent<GlobalLaserDisplay>();
                Debug.Log("[GameInitializer] Created GlobalLaserDisplay");
            }

            // Return Animation Controller
            if (ReturnAnimationController.Instance == null)
            {
                managersObj.AddComponent<ReturnAnimationController>();
                Debug.Log("[GameInitializer] Created ReturnAnimationController");
            }

            // Network Manager (Mock)
            if (NetworkManager.Instance == null)
            {
                managersObj.AddComponent<NetworkManager>();
                Debug.Log("[GameInitializer] Created NetworkManager");
            }

            // Mock Server
            var mockServer = managersObj.AddComponent<DevTools.MockServer>();
            Debug.Log("[GameInitializer] Created MockServer");
        }

        /// <summary>
        /// Set intruder types on spawner via reflection (since it's a serialized field)
        /// </summary>
        private void SetIntruderSpawnerTypes(IntruderSpawner spawner, List<IntruderData> types)
        {
            var field = typeof(IntruderSpawner).GetField("availableIntruderTypes",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                field.SetValue(spawner, types);
            }
        }

        /// <summary>
        /// Create audio manager for sound effects
        /// </summary>
        private void CreateAudioManager()
        {
            if (AudioManager.Instance != null) return;

            GameObject audioObj = new GameObject("AudioManager");
            audioObj.AddComponent<AudioManager>();
            DontDestroyOnLoad(audioObj);
            Debug.Log("[GameInitializer] Created AudioManager");
        }
    }
}
