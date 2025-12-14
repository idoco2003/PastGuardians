# PAST GUARDIANS - Unity Implementation Guide
## Claude MCP Step-by-Step Instructions

---

# HOW TO USE THIS DOCUMENT

This document is designed for use with Claude (via Unity-MCP or Claude Code) to build the Past Guardians game. Copy relevant sections when prompting Claude for implementation.

---

# SECTION 1: PROJECT SETUP

## Step 1.1: Create New Unity Project

**Prompt for Claude:**
```
Create a new Unity project for "Past Guardians" - a mobile AR game.

Requirements:
- Unity 2022.3 LTS or Unity 6
- 3D Mobile template
- Project name: PastGuardians

Set up the following folder structure:
/Assets
  /Scripts
    /Core
    /AR
    /Gameplay
    /Network
    /UI
    /Data
  /Prefabs
    /Intruders
    /Effects
    /UI
  /Materials
  /Textures
  /Models
  /Audio
  /Scenes
  /ScriptableObjects
    /IntruderData
    /GameConfig
  /Resources

Create an initial MainScene with a basic AR camera setup.
```

## Step 1.2: Install Required Packages

**Prompt for Claude:**
```
Install the following packages for Past Guardians via Package Manager:

1. AR Foundation (com.unity.xr.arfoundation) - latest 5.x
2. ARCore XR Plugin (com.unity.xr.arcore)
3. ARKit XR Plugin (com.unity.xr.arkit)
4. Netcode for GameObjects (com.unity.netcode.gameobjects)
5. Unity Services Authentication
6. Unity Cloud Save
7. TextMeshPro (if not included)
8. Input System (new)

Update the Player Settings for:
- Target Android API 26+
- Target iOS 13+
- Enable ARCore/ARKit in XR settings
- Set orientation to Portrait
- Request permissions: Camera, Location, Internet
```

---

# SECTION 2: CORE DATA STRUCTURES

## Step 2.1: Intruder Data ScriptableObject

**Prompt for Claude:**
```
Create a ScriptableObject for defining intruder types in Past Guardians.

IntruderData.cs should include:
- string intruderId (unique identifier)
- string displayName
- IntruderEra era (enum: Prehistoric, Mythological, HistoricalMachines, LostCivilizations, TimeAnomalies)
- IntruderSize sizeClass (enum: Tiny, Small, Medium, Large, Boss)
- int baseTapsRequired (range 30-500)
- float stabilityRegenRate (how fast it recovers when not tapped)
- float escapeSpeedMultiplier
- float baseAltitude (50-1000 meters)
- float tapWindowSeconds (how long before it escapes)
- MovementPattern movementPattern (enum: Hover, Drift, Circle, Escape, Approach)
- GameObject modelPrefab
- AudioClip spawnSound
- AudioClip returnSound
- Color portalColor
- string codexDescription (lore text)

Also create IntruderEra and IntruderSize enums with appropriate values.

Create a few example ScriptableObject assets:
1. Pteranodon (Prehistoric, Small, 50 taps)
2. Griffin (Mythological, Medium, 100 taps)
3. Da Vinci Ornithopter (HistoricalMachines, Small, 70 taps)
```

## Step 2.2: Game Configuration ScriptableObject

**Prompt for Claude:**
```
Create a GameConfig ScriptableObject for Past Guardians that stores all backend-configurable values:

GameConfig.cs should include:

[Header("Tap Settings")]
- int tapCooldownMs = 100
- int maxTapsPerSecond = 10
- float tapHitboxMultiplier = 1.5f (generous for children)

[Header("Laser Display")]
- float ownLaserPersistSeconds = 5f
- float otherLaserPersistSeconds = 3f
- float laserFadeDuration = 0.5f
- int maxVisibleLasers = 20

[Header("Intruder Behavior")]
- float stabilityRegenRate = 0.5f
- float regenDelayAfterTap = 2f
- float escapeThreshold = 0.8f
- bool escapeEnabled = true

[Header("Spawning")]
- int globalIntruderCap = 100
- int spawnsPerMinute = 5
- float minSpacingKm = 50f
- float bossSpawnChance = 0.02f

[Header("Progression")]
- int xpPerParticipation = 10
- int xpBonus10Percent = 15
- int xpBonus25Percent = 30
- int xpFirstTap = 20
- int xpBossParticipation = 100

[Header("View Ranges")]
- float localRangeKm = 50f
- float regionalRangeKm = 200f
- float continentalRangeKm = 500f

Create a default GameConfig asset with these values.
```

## Step 2.3: Player Data Structure

**Prompt for Claude:**
```
Create player data classes for Past Guardians:

PlayerData.cs (serializable class, not MonoBehaviour):
- string anonymousId (generated on first launch)
- string displayCity (privacy-safe location)
- string displayCountry
- int currentXP
- int currentRank (1-10)
- Color selectedBeamColor
- List<string> unlockedCosmetics
- Dictionary<string, int> codexProgress (intruderId -> return count)
- int totalReturns
- int bossReturns
- DateTime lastPlayDate

PlayerRank.cs (static class or ScriptableObject):
- Define rank thresholds: 0, 500, 1500, 3500, 7000, 12000, 20000, 35000, 55000, 100000
- Define rank titles: "Watcher", "Spotter", "Defender", "Protector", "Shield Bearer", "Time Keeper", "Rift Closer", "Era Walker", "Chrono Guard", "Prime Guardian"
- Method: GetRankForXP(int xp) returns rank number
- Method: GetRankTitle(int rank) returns string

PlayerDataManager.cs (singleton):
- Save/load player data locally (PlayerPrefs or JSON file)
- Method: GenerateAnonymousId()
- Method: UpdateLocation(string city, string country)
- Method: AddXP(int amount)
- Method: RecordIntruderReturn(string intruderId, bool isBoss)
```

---

# SECTION 3: AR FOUNDATION SETUP

## Step 3.1: AR Session Configuration

**Prompt for Claude:**
```
Set up AR Foundation for Past Guardians sky-based AR:

Create ARSetupManager.cs:
- Initialize AR Session
- Check AR availability on device
- Request camera and location permissions
- Handle AR session state changes
- Display error UI if AR not supported

Key difference from typical AR: 
- We do NOT need plane detection (sky-only view)
- We DO need device orientation/compass
- We DO need GPS location

Configure AR Session to:
- Not detect planes
- Enable light estimation (optional, for visual polish)
- Set camera focus mode to Auto

Create a simple ARSessionOrigin setup with:
- AR Camera
- AR Session
- Custom component for compass/orientation reading
```

## Step 3.2: Sky Camera Controller

**Prompt for Claude:**
```
Create SkyCameraController.cs for Past Guardians:

Purpose: Detect when player is looking at the sky and calculate intruder positions.

Features:
- Read device gyroscope/accelerometer for orientation
- Read compass heading for direction player is facing
- Determine camera pitch angle (0° = horizon, 90° = straight up)
- Only activate gameplay when pitch > 30° (looking upward)
- Show "Look up to see the sky!" prompt when looking down

Properties to expose:
- float currentPitch (0-90)
- float currentCompassHeading (0-360)
- bool isLookingAtSky (pitch > threshold)
- float skyViewThreshold = 30f (configurable)

Events:
- OnStartedLookingAtSky
- OnStoppedLookingAtSky

Use Input.compass and Input.gyro (enable them on Start).
```

## Step 3.3: GPS Location Manager

**Prompt for Claude:**
```
Create LocationManager.cs singleton for Past Guardians:

Purpose: Get player's GPS location and convert to privacy-safe display.

Features:
- Initialize location services on Start
- Request location permissions
- Poll location at reasonable intervals (every 30 seconds)
- Convert coordinates to city/country using reverse geocoding
- Cache the city/country result (don't re-geocode constantly)

Properties:
- double currentLatitude
- double currentLongitude
- string currentCity (privacy-safe)
- string currentCountry
- bool locationAvailable
- bool locationPermissionGranted

Methods:
- StartLocationServices()
- StopLocationServices()
- GetCityCountryString() returns "City, Country"

Note: For reverse geocoding, you can:
1. Use Unity's LocationService for coordinates
2. Use a simple API call or local database for city lookup
3. Or use device native geocoding if available

Privacy: Never expose exact coordinates to network - only city-level.
```

---

# SECTION 4: INTRUDER SYSTEM

## Step 4.1: Intruder Runtime Component

**Prompt for Claude:**
```
Create Intruder.cs MonoBehaviour for Past Guardians:

Purpose: Runtime component attached to spawned intruder GameObjects.

Properties:
- IntruderData data (reference to ScriptableObject)
- string uniqueInstanceId
- double latitude, longitude
- float altitude
- int currentTapProgress (synced from server)
- int totalTapsRequired
- float currentStability (0-1, 1 = full health)
- bool isBeingTapped
- float timeSinceLastTap
- IntruderState state (enum: Active, Escaping, Returning, Gone)

Runtime calculated:
- float distanceFromPlayer (km)
- Vector2 screenPosition
- float visualScale (based on altitude/distance)

Methods:
- Initialize(IntruderData data, double lat, double lon, float alt)
- RegisterTap() - called when player taps
- UpdateStability(float deltaTime) - handles regen
- StartReturnSequence() - when taps complete
- UpdatePositionOnScreen(SkyCameraController camera)

Events:
- OnTapped(int newProgress)
- OnStabilityChanged(float newStability)
- OnReturnStarted()
- OnReturnCompleted()
```

## Step 4.2: Intruder Spawner

**Prompt for Claude:**
```
Create IntruderSpawner.cs for Past Guardians:

Purpose: Spawn intruders in the sky based on server data or local testing.

For local testing (offline mode):
- Spawn random intruders at random sky positions
- Use configured spawn rate from GameConfig
- Respect intruder cap

Properties:
- List<Intruder> activeIntruders
- GameConfig config reference
- List<IntruderData> availableIntruderTypes

Methods:
- SpawnIntruder(IntruderData type, double lat, double lon, float alt)
- DespawnIntruder(Intruder intruder)
- GetNearbyIntruders(double playerLat, double playerLon, float rangeKm)
- SpawnRandomForTesting() - debug/offline mode

Local spawn algorithm for testing:
1. Pick random IntruderData based on era weights
2. Generate position near player (within 50km)
3. Generate altitude based on size class
4. Instantiate prefab and initialize

Later: Replace local spawning with server-synced spawning.
```

## Step 4.3: Intruder Screen Positioning

**Prompt for Claude:**
```
Create IntruderScreenPositioner.cs for Past Guardians:

Purpose: Convert world position (lat/lon/alt) to screen position for AR display.

Input:
- Intruder's latitude, longitude, altitude
- Player's latitude, longitude
- Player's compass heading
- Player's camera pitch

Output:
- Vector2 screenPosition (where to render intruder)
- float scale (size multiplier based on distance)
- bool isVisible (within camera FOV)

Algorithm:
1. Calculate bearing from player to intruder
2. Calculate relative angle = bearing - compass heading
3. Calculate elevation angle based on altitude and distance
4. If relative angle within FOV and elevation within sky view: visible
5. Convert angles to screen coordinates

Methods:
- Vector2 WorldToScreen(double intLat, double intLon, float intAlt, LocationManager loc, SkyCameraController cam)
- float CalculateBearing(double lat1, double lon1, double lat2, double lon2)
- float CalculateDistance(double lat1, double lon1, double lat2, double lon2) // Haversine
- float GetScaleForDistance(float distanceKm, float altitude)

Use Haversine formula for distance calculation.
```

---

# SECTION 5: TAP SYSTEM

## Step 5.1: Tap Input Handler

**Prompt for Claude:**
```
Create TapInputHandler.cs for Past Guardians:

Purpose: Handle player tap input and detect which intruder (if any) is being tapped.

Features:
- Detect touch input (mobile) and mouse (editor testing)
- Raycast from touch position to find intruders
- Enforce tap cooldown (prevent auto-clickers)
- Track tap frequency for visual feedback
- Support multi-touch but count as single rapid taps

Properties:
- float tapCooldownSeconds (from GameConfig)
- int maxTapsPerSecond (from GameConfig)
- Intruder currentTarget (what player is tapping)
- int recentTapCount (for stats)
- float tapRate (taps per second, for visual intensity)

Methods:
- Update() - poll for touch input
- bool TryGetTappedIntruder(out Intruder intruder)
- void RegisterTap(Intruder target)
- bool IsTapAllowed() - checks cooldown

Events:
- OnTapRegistered(Intruder target, int tapCount)
- OnTargetChanged(Intruder newTarget)
- OnTapBlocked() - when cooldown prevents tap

Use Physics.Raycast or screen-space bounds checking for intruder detection.
```

## Step 5.2: Tap Progress Manager

**Prompt for Claude:**
```
Create TapProgressManager.cs for Past Guardians:

Purpose: Track tap progress on intruders and determine when they're returned.

Features:
- Track local tap contributions per intruder
- Receive global tap updates from server
- Trigger return sequence when threshold reached
- Award XP based on contribution percentage

Properties:
- Dictionary<string, int> localTapContributions (intruderId -> my taps)
- GameConfig config

Methods:
- void AddLocalTap(Intruder intruder)
- void ReceiveGlobalUpdate(string intruderId, int totalTaps) // from server
- float GetMyContributionPercent(Intruder intruder)
- int CalculateXPReward(Intruder intruder, bool wasFirstTapper)
- void HandleIntruderReturn(Intruder intruder)

Return trigger:
- When intruder.currentTapProgress >= intruder.totalTapsRequired
- Play return animation
- Calculate and award XP
- Update codex progress
- Trigger celebration UI
```

---

# SECTION 6: LASER VISUALIZATION

## Step 6.1: Player Laser Renderer

**Prompt for Claude:**
```
Create PlayerLaserRenderer.cs for Past Guardians:

Purpose: Render the player's own laser beam when tapping.

Features:
- Line from bottom of screen to tapped intruder
- Pulse effect on each tap
- Intensity based on tap rate
- Color based on player's selected beam color
- Fade out after laser persist time

Use LineRenderer component.

Properties:
- LineRenderer lineRenderer
- Color beamColor (from PlayerData)
- float persistDuration (from GameConfig)
- float currentIntensity
- bool isActive

Methods:
- void ActivateLaser(Vector3 targetPosition)
- void DeactivateLaser()
- void PulseOnTap() - brief intensity boost
- void UpdateLaserPosition(Vector3 targetPosition)
- void Update() - handle fade out

Visual settings:
- Start width: 0.05f
- End width: 0.02f
- Material: Additive/Glow shader
- Start position: Bottom center of screen (slightly above)
- End position: Intruder screen position
```

## Step 6.2: Global Laser Display

**Prompt for Claude:**
```
Create GlobalLaserDisplay.cs for Past Guardians:

Purpose: Show laser beams from other players around the world.

Features:
- Display up to 20 other players' lasers
- Each laser has location label ("Tokyo, Japan")
- Rotate which lasers are shown every 3 seconds
- Prioritize diverse geographic representation
- Lasers fade in/out smoothly

Data structure for other player lasers:
```csharp
[System.Serializable]
public class RemoteLaserData
{
    public string playerId;
    public string city;
    public string country;
    public Color beamColor;
    public float lastTapTime;
    public Vector2 originDirection; // relative angle from intruder
}
```

Methods:
- void ReceiveLaserUpdate(List<RemoteLaserData> lasers) // from server
- void UpdateDisplay() - manage which lasers to show
- void SpawnLaserVisual(RemoteLaserData data)
- void DespawnLaserVisual(string playerId)
- List<RemoteLaserData> SelectLasersToDisplay(List<RemoteLaserData> all)

Visual approach:
- Lasers originate from edge of screen (direction based on relative position)
- Converge toward the intruder
- Semi-transparent compared to player's laser
- Location label floats near laser origin
```

## Step 6.3: Location Label UI

**Prompt for Claude:**
```
Create LocationLabelUI.cs for Past Guardians:

Purpose: Display "City, Country" labels near laser origins.

Features:
- World-space canvas or screen-space positioned
- Follow laser origin point
- Fade in/out with laser
- Clean, readable font
- Semi-transparent background

Properties:
- TextMeshProUGUI labelText
- CanvasGroup canvasGroup (for fading)
- RectTransform rectTransform
- string displayText

Methods:
- void SetLabel(string city, string country)
- void SetPosition(Vector2 screenPos)
- void FadeIn(float duration)
- void FadeOut(float duration)
- void UpdatePosition(Vector2 screenPos) // for animation

Styling:
- Font: Clean sans-serif
- Size: Small but readable
- Background: Semi-transparent dark rounded rect
- Text color: White
- Format: "City, Country" or just "City" if same country as player
```

---

# SECTION 7: PORTAL & RETURN EFFECTS

## Step 7.1: Time Portal Effect

**Prompt for Claude:**
```
Create TimePortalEffect.cs for Past Guardians:

Purpose: Visual effect for the time rift portal behind intruders.

Features:
- Circular portal with swirling edge particles
- Color based on intruder's era
- Glimpse of Tempus Prime through center
- Expand animation when intruder returns
- Close/seal animation after return

Properties:
- ParticleSystem edgeParticles
- Material portalMaterial
- Color portalColor
- float portalRadius
- PortalState state (Idle, Expanding, Sealing)

Methods:
- void Initialize(Color eraColor)
- void PlayIdleAnimation()
- void PlayExpandAnimation(float duration)
- void PlaySealAnimation(float duration)
- void SetTempusPrimeVisibility(bool visible)

Visual layers:
1. Background: Tempus Prime texture (blurred, glimpse)
2. Middle: Swirling distortion shader
3. Edge: Particle ring flowing inward
4. Overlay: Shimmer/aurora effect
```

## Step 7.2: Return Animation Controller

**Prompt for Claude:**
```
Create ReturnAnimationController.cs for Past Guardians:

Purpose: Orchestrate the full return sequence when intruder is defeated.

Sequence (3 seconds total):
1. [0.0s] Intruder stops moving, starts glowing
2. [0.3s] Portal expands behind intruder
3. [0.5s] Tempus Prime visible through portal
4. [1.0s] Intruder begins shrinking toward portal
5. [2.0s] Intruder fully enters portal
6. [2.3s] Light burst at portal center
7. [2.5s] Portal begins sealing
8. [3.0s] Portal gone, celebration triggers

Methods:
- IEnumerator PlayReturnSequence(Intruder intruder)
- void TriggerCelebration(Intruder intruder)

Events:
- OnReturnSequenceStarted(Intruder)
- OnReturnSequenceCompleted(Intruder)

Audio cues:
- Return start: Ethereal whoosh
- Portal expand: Deep resonance
- Intruder enters: Reverse suction sound
- Seal: Satisfying "click" or "seal" sound
- Celebration: Triumphant chime
```

---

# SECTION 8: UI SYSTEMS

## Step 8.1: Main Game HUD

**Prompt for Claude:**
```
Create MainGameHUD.cs for Past Guardians:

Purpose: Primary gameplay UI overlay.

Elements:
- Top bar: Guardian rank, XP progress, player name
- Center: Reticle/target indicator when looking at intruder
- Bottom: Current intruder info (name, tap progress, participants)
- Corners: Mini-map or compass (optional)

Properties:
- TextMeshProUGUI rankText
- Slider xpProgressBar
- TextMeshProUGUI intruderNameText
- Slider tapProgressBar
- TextMeshProUGUI participantCountText
- GameObject lookUpPrompt (shown when not looking at sky)

Methods:
- void UpdatePlayerInfo(PlayerData data)
- void ShowIntruderInfo(Intruder intruder)
- void HideIntruderInfo()
- void UpdateTapProgress(int current, int required)
- void UpdateParticipantCount(int count)
- void ShowLookUpPrompt(bool show)

Style:
- Minimal, non-intrusive
- Glassmorphism aesthetic (semi-transparent panels)
- Glow effects on progress bars
- Sans-serif fonts, child-friendly readability
```

## Step 8.2: Celebration Overlay

**Prompt for Claude:**
```
Create CelebrationOverlay.cs for Past Guardians:

Purpose: Full-screen celebration when intruder is returned.

Display elements:
- "RETURNED TO TEMPUS PRIME" title
- Intruder icon/image
- Your contribution: "You contributed 15% (47 taps)"
- Global effort: "147 Guardians from 23 countries"
- XP earned: "+35 XP"
- Rank progress animation if leveled up
- "TAP TO CONTINUE" prompt

Animation:
- Slide in from top
- Particles/confetti effect
- Stats count up animation
- Auto-dismiss after 5 seconds or on tap

Methods:
- void Show(IntruderReturnData data)
- void Hide()
- IEnumerator AnimateStats(IntruderReturnData data)

Data class:
```csharp
public class IntruderReturnData
{
    public IntruderData intruderType;
    public int playerTaps;
    public int totalTaps;
    public int participantCount;
    public int countriesCount;
    public int xpEarned;
    public bool wasFirstTapper;
    public bool rankedUp;
}
```
```

## Step 8.3: Codex UI

**Prompt for Claude:**
```
Create CodexUI.cs for Past Guardians:

Purpose: Collection screen showing all discovered intruders.

Features:
- Grid of intruder cards
- Filter by era (tabs)
- Each card shows: icon, name, return count, badge (bronze/silver/gold)
- Tap card for detail view with lore
- Locked cards for undiscovered intruders (silhouette)

Main view methods:
- void PopulateCodex(Dictionary<string, int> progress, List<IntruderData> allTypes)
- void FilterByEra(IntruderEra era)
- void ShowAllEras()
- void OpenDetailView(IntruderData intruder)

Detail view elements:
- 3D model viewer (rotating)
- Name and era
- Lore/description text
- Player's return count
- Badge status
- "You've returned 47 of these!"

Badge thresholds:
- None: 0 returns
- Bronze: 10 returns
- Silver: 50 returns
- Gold: 100 returns
```

---

# SECTION 9: NETWORKING (FOUNDATION)

## Step 9.1: Network Manager

**Prompt for Claude:**
```
Create NetworkManager.cs singleton for Past Guardians:

Purpose: Handle all server communication for multiplayer sync.

Note: Initially implement with mock/stub methods. Real server integration later.

Features:
- WebSocket connection for real-time updates
- REST API calls for non-real-time data
- Automatic reconnection
- Offline mode fallback

Core methods (implement as stubs first):
- void Connect()
- void Disconnect()
- void SendTap(string intruderId, string anonymousPlayerId, string city, string country)
- void RequestActiveIntruders(double lat, double lon, float rangeKm)
- void SubscribeToIntruderUpdates(string intruderId)

Events (for game systems to subscribe):
- OnIntruderListReceived(List<IntruderNetworkData>)
- OnIntruderProgressUpdated(string intruderId, int totalTaps, List<RemoteLaserData>)
- OnIntruderReturned(string intruderId, IntruderReturnData)
- OnConnectionStateChanged(bool connected)

Mock implementation for testing:
- Simulate other players' taps (random intervals)
- Generate fake remote laser data
- Trigger return after threshold reached
```

## Step 9.2: Intruder Network Data

**Prompt for Claude:**
```
Create IntruderNetworkData.cs for Past Guardians:

Purpose: Data structure for intruders received from server.

```csharp
[System.Serializable]
public class IntruderNetworkData
{
    public string intruderId;
    public string intruderTypeId; // maps to IntruderData ScriptableObject
    public double latitude;
    public double longitude;
    public float altitude;
    public int currentTapProgress;
    public int totalTapsRequired;
    public long spawnTimestamp;
    public long expiryTimestamp;
    public List<ActiveTapperData> activeToppers;
}

[System.Serializable]
public class ActiveTapperData
{
    public string anonymousId;
    public string city;
    public string country;
    public int tapCount;
    public long lastTapTimestamp;
}
```

Converter methods:
- Intruder ToLocalIntruder(IntruderNetworkData netData, IntruderData type)
- IntruderNetworkData ToNetworkData(Intruder local)
```

---

# SECTION 10: TESTING & DEBUG

## Step 10.1: Debug Panel

**Prompt for Claude:**
```
Create DebugPanel.cs for Past Guardians:

Purpose: In-editor and development build debug tools.

Features:
- Toggle debug panel with 3-finger tap
- Spawn intruder button (pick type)
- Simulate other players tapping
- Force intruder return
- Display current GPS coordinates
- Display compass heading
- Display camera pitch
- Show/hide intruder hitboxes
- FPS counter
- Network status

Spawn controls:
- Dropdown: Select intruder type
- Button: Spawn at random nearby position
- Button: Spawn directly above player
- Slider: Set spawn altitude

Simulation controls:
- Toggle: Simulate global players
- Slider: Simulated tap rate (taps/second from "others")
- Button: Instant complete current intruder

Only compile in Development builds (#if DEVELOPMENT_BUILD || UNITY_EDITOR).
```

## Step 10.2: Mock Server

**Prompt for Claude:**
```
Create MockServer.cs for Past Guardians:

Purpose: Simulate server behavior for offline development/testing.

Features:
- Generate fake intruder spawns
- Simulate other players' taps
- Generate fake remote laser data
- Track tap progress locally
- Trigger return events

Configuration:
- int simulatedPlayerCount = 10
- float averageTapsPerSecond = 5f
- float spawnIntervalSeconds = 30f
- string[] fakeCities = {"Tokyo", "London", "New York", "Sydney", ...}

Methods:
- void StartSimulation()
- void StopSimulation()
- void SimulateTapBurst(string intruderId, int tapCount)
- List<RemoteLaserData> GenerateFakeLasers(int count)

Behavior:
- On intruder spawn: Start simulating other players tapping
- Random tap intervals (creates natural variation)
- Diverse city/country origins for lasers
- Eventually complete intruder based on simulated + real taps
```

---

# SECTION 11: IMPLEMENTATION ORDER

## Recommended Build Sequence

### Week 1-2: Foundation
1. Project setup (Section 1)
2. Core data structures (Section 2)
3. AR setup and sky camera (Section 3)

### Week 3-4: Core Loop
4. Intruder system basics (Section 4.1, 4.2)
5. Screen positioning (Section 4.3)
6. Tap input (Section 5.1)

### Week 5-6: Visuals
7. Player laser (Section 6.1)
8. Portal effects (Section 7.1)
9. Return animation (Section 7.2)

### Week 7-8: Polish & Test
10. Main HUD (Section 8.1)
11. Celebration overlay (Section 8.2)
12. Debug tools (Section 10)
13. Mock server (Section 10.2)

### Week 9-10: Multiplayer Foundation
14. Network manager stubs (Section 9)
15. Global laser display (Section 6.2, 6.3)
16. Tap progress sync (Section 5.2)

### Week 11-12: Content & Progression
17. Codex UI (Section 8.3)
18. Create all intruder ScriptableObjects
19. XP and rank system integration
20. Testing and iteration

---

# SECTION 12: QUICK REFERENCE

## File List (Scripts to Create)

### Core
- [ ] GameConfig.cs (ScriptableObject)
- [ ] IntruderData.cs (ScriptableObject)
- [ ] PlayerData.cs
- [ ] PlayerDataManager.cs
- [ ] PlayerRank.cs

### AR
- [ ] ARSetupManager.cs
- [ ] SkyCameraController.cs
- [ ] LocationManager.cs

### Gameplay
- [ ] Intruder.cs
- [ ] IntruderSpawner.cs
- [ ] IntruderScreenPositioner.cs
- [ ] TapInputHandler.cs
- [ ] TapProgressManager.cs

### Visuals
- [ ] PlayerLaserRenderer.cs
- [ ] GlobalLaserDisplay.cs
- [ ] LocationLabelUI.cs
- [ ] TimePortalEffect.cs
- [ ] ReturnAnimationController.cs

### UI
- [ ] MainGameHUD.cs
- [ ] CelebrationOverlay.cs
- [ ] CodexUI.cs

### Network
- [ ] NetworkManager.cs
- [ ] IntruderNetworkData.cs

### Debug
- [ ] DebugPanel.cs
- [ ] MockServer.cs

---

# PROMPTING TIPS FOR CLAUDE

## Best Practices

1. **One script at a time**: Ask Claude to implement one script fully before moving to the next.

2. **Include context**: When asking for a new script, mention which existing scripts it needs to reference.

3. **Specify Unity version**: Always mention "Unity 2022.3 LTS" or "Unity 6" for compatibility.

4. **Request complete files**: Ask for "complete, production-ready code" to avoid placeholders.

5. **Test incrementally**: After each major script, test in Unity before continuing.

## Example Prompts

**Starting a new script:**
```
Using Unity 2022.3 LTS and AR Foundation, create IntruderData.cs as described in the Past Guardians implementation guide Section 2.1. Include all properties, enums, and create a CreateAssetMenu attribute for easy ScriptableObject creation.
```

**Connecting scripts:**
```
Create TapInputHandler.cs that works with the existing Intruder.cs and SkyCameraController.cs. Reference the GameConfig for tap cooldown settings. Include complete touch and mouse input handling.
```

**Requesting visuals:**
```
Create PlayerLaserRenderer.cs using LineRenderer. The laser should pulse brighter on each tap and gradually fade after the persist duration. Include all shader/material setup code or specify what materials need to be created manually.
```

---

*Document Version: 1.0*
*For use with: Past Guardians GDD v1.0*
*Compatible with: Unity 2022.3 LTS, Unity 6*
