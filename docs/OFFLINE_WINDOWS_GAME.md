# Offline Windows Game Target

Insane Monopoly is now defined as a **standalone offline Unity game** first. The intended shipping artifact is a local Windows `.exe`, not a web service, Steam integration, or Tabletop Simulator recreation.

## Product Target

- Platform: Windows desktop.
- Engine: Unity 2022.3 LTS.
- Mode: local/offline play only.
- Players: 2–8, human and AI.
- Rules: standard Monopoly rules by default, with data-driven hooks for custom Insane Monopoly spaces, cards, taxes, events, and mechanics.

## Quality-of-life automation

The Unity runtime should automate the tasks that scripted tabletop mods normally make less painful:

- one-click dice rolling
- animated dice results
- automatic token movement
- automatic rent payment
- automatic card effects
- automatic property ownership tracking
- automatic house/hotel state tracking
- automatic auctions for declined properties
- automatic bankruptcy liquidation
- automatic turn progression

## Required gameplay systems

The game architecture should keep these systems isolated and testable:

```text
Game Bootstrap
 ├─ Board Builder
 ├─ Player/Pawn System
 ├─ Dice System
 ├─ Turn Controller
 ├─ Property Ledger
 ├─ Economy System
 ├─ Building System
 ├─ Card System
 ├─ Jail System
 ├─ Auction System
 ├─ Trading System
 ├─ AI System
 ├─ Bankruptcy System
 └─ Save System
```

## UI direction

The project should feel like a polished digital board game, not a generic tabletop sandbox. The UI roadmap is:

1. Replace IMGUI with UI Toolkit or uGUI.
2. Keep player cash/assets visible at all times.
3. Add a turn tracker and action prompts.
4. Add property management windows for buy/build/mortgage/sell.
5. Add a dedicated trade window.
6. Add a persistent game log.
7. Add visual effects for purchases, rent, jail, bankruptcy, and winning.

## Graphics direction

The current runtime uses procedural placeholder primitives so the project can build without copyrighted assets. Production art should replace those placeholders with:

- clean 3D board meshes
- readable property cards and board labels
- animated player tokens
- board edge/table presentation
- house and hotel models
- card flip, dice roll, purchase, jail, and win effects
- smooth orbit/zoom camera controls

## Data-driven rules

Unity loads rules from:

```text
Unity/Assets/StreamingAssets/insane-monopoly-config.json
```

That file is the modding surface for board spaces, property prices, rent ladders, house costs, mortgage values, tax amounts, card decks, and special events.

## Windows build

A Unity editor build menu is included:

```text
Insane Monopoly > Build Windows EXE
```

It builds:

```text
Unity/Builds/Windows/InsaneMonopoly.exe
```

Unity must have the Windows desktop build module installed locally for this to succeed.
