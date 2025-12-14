# Past Guardians

**Protect the Sky. Unite the World.**

A mobile AR location-based cooperative game where players worldwide work together to return time-displaced creatures to their home dimension, Tempus Prime.

## Overview

When you look up at the sky through your phone, you'll see creatures from different eras of history that have accidentally slipped through time rifts. By tapping the screen, you fire a beam of light that helps push them back through the portal. But here's the magic - you can see laser beams from other players around the world helping too!

## Features

- **Sky-Based AR**: Point your phone at the sky to see intruders - no surface detection needed
- **Global Cooperation**: See real-time laser beams from players worldwide
- **28+ Intruder Types**: Creatures from 5 eras:
  - Prehistoric (Amber) - Pteranodons, T-Rex, Quetzalcoatlus
  - Mythological (Purple) - Dragons, Phoenix, Griffin, Pegasus
  - Historical Machines (Bronze) - Da Vinci's Ornithopter, Airships
  - Lost Civilizations (Teal) - Atlantean craft, Mayan serpents
  - Time Anomalies (Prismatic) - Paradox spheres, time echoes
- **Progression System**: 10 ranks from Cadet to Temporal Guardian
- **Codex Collection**: Discover and collect all intruder types
- **COPPA Compliant**: Safe for all ages with enhanced privacy for children

## Tech Stack

- Unity 2022.3 LTS / Unity 6
- AR Foundation (ARCore + ARKit)
- C# Scripts with runtime initialization
- Procedural audio generation

## Project Structure

```
Assets/
├── Scripts/
│   ├── Core/           # Game managers, audio, initialization
│   ├── Data/           # ScriptableObjects (IntruderData, GameConfig)
│   ├── AR/             # AR setup, sky camera, location
│   ├── Gameplay/       # Intruders, spawning, tap handling
│   ├── Network/        # Multiplayer sync, mock server
│   ├── UI/             # HUD, navigation, shop, codex
│   └── DevTools/       # Debug tools, mock server
```

## Quick Start

1. Open project in Unity 2022.3+
2. Press Play - everything auto-initializes
3. Point camera at sky
4. Tap intruders to push them back!

## Setup MCP Servers (Optional)

For AI-assisted development with Gemini and Hugging Face:

```bash
# Create .mcp.json with your API keys
{
  "mcpServers": {
    "gemini": {
      "type": "stdio",
      "command": "npx",
      "args": ["-y", "gemini-mcp-tool"],
      "env": { "GEMINI_API_KEY": "your-key" }
    },
    "hfspace": {
      "type": "stdio",
      "command": "npx",
      "args": ["-y", "@llmindset/mcp-hfspace"],
      "env": { "HF_TOKEN": "your-token" }
    }
  }
}
```

## License

All rights reserved.

## Credits

Built with Claude Code
