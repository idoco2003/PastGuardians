# PAST GUARDIANS
## Game Design Document v1.0
### "Protect the Sky. Unite the World. Send Them Back."

---

# 1. EXECUTIVE SUMMARY

## 1.1 Game Concept
**Past Guardians** is a mobile AR location-based cooperative game where players worldwide unite to protect Earth from time-displaced intruders appearing in the sky. Players tap their screens to generate protective laser shields that collectively push intruders back through time portals to their home - a parallel Earth called **"Tempus Prime"** where all historical eras coexist.

## 1.2 Core Fantasy
You look up at the sky through your phone. A massive pterodactyl is materializing through a shimmering time rift. You tap rapidly, and a brilliant laser beam shoots from your location toward the creature. On screen, you see other lasers converging from "São Paulo," "Tokyo," and "Berlin" - strangers around the world joining your fight. Together, your combined beams push the creature back through the portal. The world is safe. For now.

## 1.3 Target Audience
- **Primary:** Children ages 8-12
- **Secondary:** Families, casual mobile gamers
- **Compliance:** Full COPPA compliance required

## 1.4 Platform
- iOS and Android mobile devices
- AR Foundation (Unity)
- Requires GPS and camera access

## 1.5 Unique Selling Points
1. **Global Real-Time Cooperation** - See lasers from players worldwide with location labels
2. **Sky-Based AR** - Look UP at threats, not down at the ground
3. **Unified Purpose** - Everyone fights the same intruders together
4. **Non-Violent Heroism** - "Shield and send back" not "attack and destroy"
5. **Historical Wonder** - Creatures from all eras of Earth's past

---

# 2. GAME WORLD

## 2.1 Lore Overview

### The Premise
In 2024, scientists detected temporal anomalies in Earth's upper atmosphere. Creatures and machines from Earth's past began breaking through "time rifts" - tears in the fabric of time itself. These beings aren't evil; they're lost, displaced from their proper eras. But their presence destabilizes our timeline.

### Tempus Prime
A parallel version of Earth exists where all historical periods coexist simultaneously - dinosaurs roam alongside medieval knights and ancient automatons. This world is called **Tempus Prime** (Latin: "First Time"). The intruders belong there, not here.

### The Guardians
Players are **Guardians** - individuals who discovered they can generate "Temporal Shield Energy" through their devices. When Guardians tap their screens, they create focused beams that don't harm the intruders but gently push them back through the rifts to Tempus Prime.

### The Mission
Protect Earth's timeline by working together globally to return displaced beings to their proper home.

## 2.2 Visual Identity

### Sky Aesthetic
- Time rifts appear as shimmering circular portals with aurora-like edges
- Portal colors shift based on the era of the intruder (amber for prehistoric, purple for mythological, bronze for mechanical)
- Earth's sky remains visible behind AR elements

### Tempus Prime (Portal Destination)
- Visible through open portals
- Earth-like planet with mixed-era landscapes
- Dinosaurs visible alongside castles and ancient temples
- Warm, golden lighting suggesting a "eternal sunset" world

### Laser Beams
- Player's own laser: Bright white/blue, prominent
- Other players' lasers: Slightly transparent, color-coded by region
- Convergence point creates a bright "shield dome" around intruder

---

# 3. INTRUDER CLASSIFICATION

## 3.1 Era Categories

### 🦕 PREHISTORIC (Amber Portals)
Creatures from Earth's ancient past, 65+ million years ago.

| Intruder | Taps Required | Size Class | Behavior |
|----------|---------------|------------|----------|
| Pteranodon | 50 | Small | Quick, darting movements |
| Archaeopteryx | 30 | Tiny | Flock spawns (3-5 units) |
| Quetzalcoatlus | 200 | Large | Slow, majestic gliding |
| Mosasaurus (Sky Breach) | 500 | Boss | Rare, emerging from water-portal |
| T-Rex Head (Rift Peek) | 150 | Medium | Comical, head poking through rift |

### 🐉 MYTHOLOGICAL (Purple Portals)
Creatures from human mythology and legend.

| Intruder | Taps Required | Size Class | Behavior |
|----------|---------------|------------|----------|
| Griffin | 100 | Medium | Proud, circling patterns |
| Phoenix | 80 | Medium | Leaves fire trails (visual only) |
| Pegasus | 60 | Medium | Graceful, fast movement |
| Dragon (Eastern) | 300 | Large | Serpentine, long body |
| Dragon (Western) | 350 | Large | Winged, classic pose |
| Thunderbird | 150 | Medium | Storm effects around it |
| Roc | 400 | Boss | Massive shadow over area |

### ⚙️ HISTORICAL MACHINES (Bronze Portals)
Mechanical constructs from various eras.

| Intruder | Taps Required | Size Class | Behavior |
|----------|---------------|------------|----------|
| Da Vinci Ornithopter | 70 | Small | Flapping mechanical wings |
| Steampunk Airship | 200 | Large | Slow drifting |
| Clockwork Bird | 40 | Tiny | Erratic, mechanical chirps |
| Ancient War Balloon | 120 | Medium | Gentle floating |
| Tesla Drone | 90 | Small | Electric sparks visual |
| Icarus Wings (with pilot) | 100 | Medium | Struggling flight animation |

### 🏛️ LOST CIVILIZATIONS (Teal Portals)
Technology from civilizations lost to time.

| Intruder | Taps Required | Size Class | Behavior |
|----------|---------------|------------|----------|
| Atlantean Hovercraft | 150 | Medium | Silent, glowing propulsion |
| Mayan Sky Serpent | 180 | Medium | Stone texture, glowing glyphs |
| Egyptian Sun Disc | 100 | Small | Spinning, radiating light |
| Lemuria Crystal Ship | 250 | Large | Prismatic light effects |
| Mu Temple Fragment | 300 | Large | Floating architecture piece |

### ⏰ TIME ANOMALIES (Prismatic/Glitching Portals)
Pure temporal distortions - abstract entities.

| Intruder | Taps Required | Size Class | Behavior |
|----------|---------------|------------|----------|
| Time Echo | 60 | Small | Player silhouette from "past" |
| Paradox Sphere | 120 | Medium | Shows multiple timelines inside |
| Yesterday's Cloud | 80 | Medium | Weather from another day |
| Future Fragment | 200 | Large | Glitching, pixelated object |
| Epoch Rift | 500 | Boss | Massive tear needing global effort |

## 3.2 Size Classes

| Class | Screen Coverage | Base Altitude | Tap Window |
|-------|-----------------|---------------|------------|
| Tiny | 5-10% | Low (50-100m) | 30 seconds |
| Small | 10-20% | Low-Medium (100-300m) | 45 seconds |
| Medium | 20-35% | Medium (300-600m) | 60 seconds |
| Large | 35-50% | High (600-1000m) | 90 seconds |
| Boss | 50-75% | Very High (1000m+) | 180 seconds |

## 3.3 Spawn Configuration (Backend Adjustable)

```json
{
  "spawn_settings": {
    "global_intruder_cap": 100,
    "spawn_rate_per_minute": 5,
    "era_weights": {
      "prehistoric": 0.25,
      "mythological": 0.25,
      "historical_machines": 0.20,
      "lost_civilizations": 0.15,
      "time_anomalies": 0.15
    },
    "boss_spawn_chance": 0.02,
    "min_distance_between_intruders_km": 50
  }
}
```

---

# 4. CORE GAMEPLAY MECHANICS

## 4.1 The Tap-Shield System

### Basic Mechanic
1. Player sees intruder in AR sky view
2. Player taps and HOLDS on the intruder
3. Each tap generates "shield energy" visualized as a laser
4. Continuous tapping required - stopping allows intruder to resist
5. When total taps (from all players) reaches threshold, intruder returns to Tempus Prime

### Tap Registration
```
TAP RULES:
- Minimum tap interval: 100ms (prevents auto-clicker abuse)
- Maximum tap rate: 10 taps/second per player
- Tap must land within intruder hitbox (generous for children)
- Taps count globally in real-time
```

### Shield Energy Visualization
- Each tap creates a brief "pulse" on the laser beam
- Sustained tapping creates steady beam
- Beam intensity reflects tap frequency
- Beam color: Player's chosen color (customization)

## 4.2 Global Participation Display

### Laser Convergence View
When tapping an intruder, players see:
- Their own laser beam (prominent, center)
- Other players' laser beams from around the world
- Location labels floating near beam origins

### Location Label Format
```
PRIVACY-SAFE LOCATION DISPLAY:
- City level only: "Tokyo", "New York", "London"
- Country for smaller nations: "Singapore", "Luxembourg"
- No street names, landmarks, or precise coordinates
- Generic label: "[City], [Country]"
- Example: "São Paulo, Brazil"
```

### Participation Counter
- "47 Guardians defending" (real-time count)
- Flags or region indicators (optional visual)
- Tap contribution percentage (your taps / total taps)

## 4.3 Intruder Behavior

### Movement Patterns
- **Hover:** Stationary with gentle bob
- **Drift:** Slow horizontal movement
- **Circle:** Orbits a fixed sky point
- **Escape:** Moves away when close to tap threshold (creates urgency)
- **Approach:** Slowly descends toward ground (larger hitbox, easier)

### Resistance Mechanic
- Intruders have a "stability meter"
- Continuous tapping depletes stability
- If tapping stops, stability slowly regenerates
- Regeneration rate configurable per intruder type

```json
{
  "resistance_settings": {
    "stability_regen_rate": 0.5,
    "regen_delay_after_tap_seconds": 2.0,
    "escape_threshold": 0.8,
    "escape_speed_multiplier": 1.5
  }
}
```

## 4.4 Success State: Return to Tempus Prime

### Portal Animation Sequence
1. Intruder stops moving, glows with shield energy
2. Time portal expands behind/above intruder
3. Glimpse of Tempus Prime visible through portal
4. Intruder gently pulled backward into portal
5. Portal closes with satisfying "temporal seal" effect
6. All participating players see celebration UI

### Celebration Feedback
- Screen flash with "RETURNED TO TEMPUS PRIME"
- Player's contribution stats displayed
- Global participant count and locations listed
- XP/reward distribution

---

# 5. PLAYER PROGRESSION

## 5.1 Guardian Rank System

| Rank | Title | XP Required | Unlocks |
|------|-------|-------------|---------|
| 1 | Watcher | 0 | Basic beam (white) |
| 2 | Spotter | 500 | Beam color selection |
| 3 | Defender | 1,500 | Profile badge display |
| 4 | Protector | 3,500 | Beam trail effects |
| 5 | Shield Bearer | 7,000 | Regional leaderboard |
| 6 | Time Keeper | 12,000 | Rare spawn notifications |
| 7 | Rift Closer | 20,000 | Boss event priority |
| 8 | Era Walker | 35,000 | All beam customizations |
| 9 | Chrono Guard | 55,000 | Title: "Legendary Guardian" |
| 10 | Prime Guardian | 100,000 | Special golden beam |

## 5.2 XP Awards

| Action | XP Awarded |
|--------|------------|
| Participate in return (any taps) | 10 XP |
| Contribute 10%+ of taps | +15 XP bonus |
| Contribute 25%+ of taps | +30 XP bonus |
| First to tap an intruder | +20 XP |
| Return 5 intruders in one session | +50 XP bonus |
| Participate in Boss return | 100 XP |
| Daily login | 25 XP |

## 5.3 Collection System: Tempus Codex

Players collect "entries" for each intruder type they help return:
- First return: Unlock codex entry with lore
- 10 returns: Bronze badge
- 50 returns: Silver badge  
- 100 returns: Gold badge
- Codex shows 3D model viewer for each creature
- Lore entries teach real history/mythology (educational value)

---

# 6. MULTIPLAYER SYSTEMS

## 6.1 Real-Time Synchronization

### Server Architecture (High Level)
```
[PLAYER DEVICES] <--WebSocket--> [REGIONAL SERVERS] <---> [GLOBAL SYNC SERVER]
                                        |
                                        v
                                 [INTRUDER STATE DB]
                                        |
                                        v
                                 [ANALYTICS/BACKEND]
```

### Sync Requirements
- Intruder positions: Update every 500ms
- Tap counts: Real-time (WebSocket)
- Player locations: Update on tap (city-level only)
- Laser visuals: Client-side prediction with server validation

## 6.2 Intruder Assignment

### Global Intruder Pool
- All active intruders exist in a global database
- Each intruder has: ID, Type, Position (Lat/Long/Alt), TapProgress, SpawnTime
- Players see intruders within their "view range" (configurable, default 500km radius)
- Distant intruders appear smaller/higher in sky

### View Range Tiers
```json
{
  "view_ranges": {
    "local": {"radius_km": 50, "size_multiplier": 1.0},
    "regional": {"radius_km": 200, "size_multiplier": 0.7},
    "continental": {"radius_km": 500, "size_multiplier": 0.4},
    "global_boss": {"radius_km": 20000, "size_multiplier": 0.3}
  }
}
```

## 6.3 Laser Origin Display

### Data Sent Per Tap
```json
{
  "player_id": "anonymous_hash",
  "city": "Chicago",
  "country": "USA", 
  "tap_count": 1,
  "intruder_id": "int_28374",
  "timestamp": 1699999999
}
```

### Display Rules
- Show max 20 laser origins at once (performance)
- Prioritize: Highest contributors, geographically diverse
- Rotate displayed origins every 3 seconds
- Player's own laser always visible

### Laser Persistence (Backend Configurable)
```json
{
  "laser_display": {
    "own_laser_persist_seconds": 5.0,
    "other_laser_persist_seconds": 3.0,
    "fade_duration_seconds": 0.5,
    "max_simultaneous_lasers": 20
  }
}
```

---

# 7. BACKEND CONFIGURATION

## 7.1 Admin Dashboard Requirements

### Intruder Management
- Create/edit intruder types
- Set tap requirements per type
- Adjust spawn rates and weights
- Trigger manual spawns (events)
- View active intruders map

### Game Balance
- Adjust XP rewards
- Modify rank thresholds
- Configure laser visuals
- Set regional spawn biases

### Analytics
- Active player count (real-time)
- Taps per minute (global)
- Intruders returned per hour
- Geographic player distribution
- Session length averages

## 7.2 Configurable Parameters

```json
{
  "backend_config": {
    "gameplay": {
      "tap_cooldown_ms": 100,
      "max_taps_per_second": 10,
      "intruder_stability_regen_rate": 0.5,
      "escape_behavior_enabled": true
    },
    "display": {
      "laser_persist_self_seconds": 5.0,
      "laser_persist_others_seconds": 3.0,
      "max_visible_lasers": 20,
      "location_granularity": "city"
    },
    "spawning": {
      "global_intruder_cap": 100,
      "spawn_rate_per_minute": 5,
      "boss_spawn_interval_hours": 4,
      "min_intruder_spacing_km": 50
    },
    "progression": {
      "xp_per_participation": 10,
      "xp_bonus_10_percent": 15,
      "xp_bonus_25_percent": 30,
      "xp_first_tap": 20,
      "xp_boss_participation": 100
    }
  }
}
```

---

# 8. AR IMPLEMENTATION

## 8.1 Sky Detection

### Camera Orientation
- Game activates when phone points upward (>30° from horizontal)
- AR plane detection NOT required (sky-only view)
- Compass heading used for intruder positioning
- GPS used for player location and intruder distance

### Altitude Simulation
- Intruders placed at virtual altitude (not real AR depth)
- Parallax effect simulates distance when player moves
- Scale decreases with altitude to simulate distance

## 8.2 Intruder Rendering

### AR Placement
```csharp
// Pseudo-code for intruder positioning
Vector3 GetIntruderScreenPosition(Intruder intruder, PlayerLocation player)
{
    float bearing = CalculateBearing(player.lat, player.lon, intruder.lat, intruder.lon);
    float relativeAngle = bearing - player.compassHeading;
    float elevationAngle = CalculateElevation(intruder.altitude, intruder.distance);
    
    // Convert to screen space based on camera FOV
    return CameraToScreenPoint(relativeAngle, elevationAngle);
}
```

### LOD System
| Distance | Model Quality | Effects |
|----------|--------------|---------|
| < 100km | High poly, full animation | All VFX |
| 100-300km | Medium poly | Reduced VFX |
| 300-500km | Low poly, billboard | Minimal VFX |
| > 500km | Icon/silhouette only | None |

## 8.3 Portal Visuals

### Time Rift Appearance
- Circular distortion effect in sky
- Edge particles flowing inward
- Color based on era category
- Glimpse of Tempus Prime through portal

### Return Animation
- 3-second sequence
- Intruder model shrinks toward portal center
- Light burst on completion
- Portal seals with ripple effect

---

# 9. CHILD SAFETY & COPPA COMPLIANCE

## 9.1 Data Collection Policy

### Collected (Essential Only)
- Anonymous player ID (device-generated hash)
- City-level location (for gameplay only)
- Tap interactions (no personal data attached)
- Age gate confirmation

### NOT Collected
- Real names
- Precise location (street/address)
- Photos or camera data (AR is processed on-device)
- Social connections
- Chat or messaging data

## 9.2 Safety Features

### No Direct Communication
- No chat system
- No direct messaging
- No friend lists with communication
- Location labels are generic (city only)

### Age Gate
- Age verification on first launch
- If under 13: Enhanced privacy mode
- Parental consent flow for under-13 users

### Content Safety
- All intruders are fantastical/historical (not scary)
- "Send back home" framing (not violent)
- No death, destruction, or harm language
- Positive cooperative messaging

## 9.3 Session Limits (Optional Parental Control)

```json
{
  "parental_controls": {
    "daily_time_limit_minutes": 60,
    "break_reminder_interval_minutes": 30,
    "bedtime_lockout_enabled": false,
    "bedtime_start": "21:00",
    "bedtime_end": "07:00"
  }
}
```

---

# 10. MONETIZATION (Child-Safe)

## 10.1 Approach
- **NO loot boxes or gambling mechanics**
- **NO pay-to-win advantages**
- **NO ads to children**
- Cosmetic-only purchases
- Optional premium pass (requires parental approval)

## 10.2 Cosmetic Items

| Category | Examples | Price Range |
|----------|----------|-------------|
| Beam Colors | Rainbow, Galaxy, Nature | $0.99 - $1.99 |
| Beam Effects | Sparkles, Lightning, Hearts | $1.99 - $2.99 |
| Profile Frames | Era-themed borders | $0.99 |
| Titles | "Dragon Tamer", "Dino Expert" | $0.99 |

## 10.3 Guardian Pass (Monthly)
- $4.99/month
- 2x XP gain
- Exclusive cosmetics
- Early access to new intruder types
- No gameplay advantages

---

# 11. EVENTS & LIVE OPS

## 11.1 World Events

### Temporal Storm (Weekly)
- Increased spawn rates for 2 hours
- 3x XP during event
- Special "storm" visual theme
- Community goal: Return X intruders globally

### Era Invasion (Monthly)
- Single era dominates spawns for a weekend
- Era-specific cosmetic rewards
- Codex completion bonuses
- Example: "Mythological March" - all mythological creatures

### Boss Rush (Bi-Weekly)
- Guaranteed Boss spawn every 30 minutes
- Requires massive global coordination
- Top contributors earn exclusive rewards
- Server-wide celebration on completion

## 11.2 Seasonal Themes

| Season | Theme | Special Intruders |
|--------|-------|-------------------|
| Spring | "Rebirth Rift" | Baby dinosaurs, phoenixes |
| Summer | "Solar Surge" | Sun discs, fire creatures |
| Fall | "Harvest of Time" | Harvest golems, scarecrow bots |
| Winter | "Frozen Past" | Ice age creatures, frost machines |

---

# 12. TECHNICAL SPECIFICATIONS

## 12.1 Unity Configuration

### Version & Packages
- Unity 2022.3 LTS or Unity 6
- AR Foundation 5.x+
- ARCore XR Plugin (Android)
- ARKit XR Plugin (iOS)
- Netcode for GameObjects (multiplayer)

### Required Packages (via Package Manager)
```
com.unity.xr.arfoundation
com.unity.xr.arcore
com.unity.xr.arkit
com.unity.netcode.gameobjects
com.unity.services.authentication
com.unity.services.cloudcode
com.unity.services.cloudsave
```

## 12.2 Performance Targets

| Metric | Target |
|--------|--------|
| Frame Rate | 60 FPS |
| Load Time | < 5 seconds |
| Network Latency | < 200ms acceptable |
| Battery Usage | < 15% per hour |
| Memory | < 500MB |

## 12.3 Minimum Device Requirements

### Android
- Android 8.0+
- ARCore supported device
- 3GB RAM
- GPS and compass

### iOS
- iOS 13+
- ARKit supported device (iPhone 6S+)
- GPS and compass

---

# 13. DEVELOPMENT MILESTONES

## Phase 1: Core AR (Weeks 1-4)
- [ ] AR Foundation setup
- [ ] Sky detection and camera orientation
- [ ] Single intruder display (placeholder model)
- [ ] Basic tap detection
- [ ] Intruder return animation

## Phase 2: Tap Mechanics (Weeks 5-8)
- [ ] Tap-to-laser visualization
- [ ] Continuous tap requirement
- [ ] Stability/resistance system
- [ ] Local tap counting
- [ ] Success celebration UI

## Phase 3: Multiplayer Foundation (Weeks 9-14)
- [ ] Server architecture setup
- [ ] Real-time intruder sync
- [ ] Global tap aggregation
- [ ] Player location anonymization
- [ ] Multi-laser display

## Phase 4: Content & Polish (Weeks 15-20)
- [ ] All intruder models and animations
- [ ] Era-specific portals
- [ ] Tempus Prime visuals
- [ ] Progression system
- [ ] Codex/collection UI

## Phase 5: Live Ops & Launch (Weeks 21-24)
- [ ] Admin dashboard
- [ ] Event system
- [ ] Analytics integration
- [ ] COPPA compliance audit
- [ ] Soft launch and iteration

---

# 14. APPENDIX

## A. Glossary

| Term | Definition |
|------|------------|
| Guardian | Player/user |
| Intruder | Time-displaced creature or object |
| Tempus Prime | The "past world" parallel Earth |
| Time Rift | Portal through which intruders appear |
| Shield Energy | The laser/beam players generate |
| Temporal Seal | Successfully returning an intruder |
| Codex | Collection/encyclopedia of intruders |

## B. Reference Art Direction

### Color Palette
- **Sky/UI:** Deep blue (#0A1628), Light blue (#87CEEB)
- **Portals - Prehistoric:** Amber (#FFBF00)
- **Portals - Mythological:** Purple (#9B30FF)
- **Portals - Machines:** Bronze (#CD7F32)
- **Portals - Lost Civ:** Teal (#008080)
- **Portals - Anomalies:** Prismatic (rainbow shift)
- **Player Laser:** White/Blue (#00D4FF) default
- **Success:** Golden (#FFD700)

### Audio Direction
- Ethereal, wonder-filled ambient music
- Satisfying "pulse" sound on each tap
- Majestic creature sounds (not scary)
- Triumphant fanfare on successful return
- Gentle portal "whoosh" sounds

---

# Document Control

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | Dec 2024 | Ido/Claude | Initial GDD |

---

*"The past is not lost. It's waiting. And together, we send it home."*
