# Unity Version

This repo now includes a Unity implementation in `Unity/` aimed at a real 3D tabletop game instead of the earlier flat web prototype.

## Requirements

- Unity `2022.3.50f1` or another Unity 2022.3 LTS editor.
- Desktop target modules for Windows, macOS, or Linux if you want packaged builds.

## Run in the Editor

1. Open Unity Hub.
2. Choose **Add project from disk**.
3. Select the `Unity/` folder in this repository.
4. Open `Assets/Scenes/Main.unity`.
5. Press **Play**.

The scene contains a single bootstrap object. At runtime it procedurally creates:

- A neon 3D Monopoly-style board with 40 spaces.
- Raised glowing property tiles and a center platform.
- Animated dice cubes.
- 2–8 player pawns with lights.
- Orbit/zoom camera controls.
- A prototype HUD with player cash, dice, Free Parking pot, and event log.

## Controls

- Click **ROLL THE CHAOS DICE** to advance the current player.
- Hold right mouse button and drag to orbit the board.
- Use the mouse wheel to zoom.

## Data-driven content

The board, cards, and rules are loaded from:

```text
Unity/Assets/StreamingAssets/insane-monopoly-config.json
```

Edit that JSON to change board spaces, rents, prices, card decks, tax amounts, and basic rules without touching C# code.

## Build

From Unity Editor:

1. Open **File > Build Settings**.
2. Confirm `Assets/Scenes/Main.unity` is in the scene list.
3. Pick your target platform.
4. Click **Build**.

The included `ProjectSettings/EditorBuildSettings.asset` already registers the main scene.

## What is implemented now

- Procedural 3D board generation.
- Runtime JSON loading through StreamingAssets.
- Neon material generation and tile labels.
- Animated dice roll presentation.
- Pawn movement with hop animation.
- GO salary, taxes, Free Parking pot, Go To Jail, and card landing events.
- Property ownership, rent payment, railroad/utility rent scaling, mortgages, and bankruptcy checks.
- Full-set building rules with visible house and hotel markers.
- Jail payment/cards/doubles handling, sample trading, AI buy/build heuristics, and save JSON export.
- Legacy IMGUI HUD for fast iteration.

## Next Unity milestones

- Replace primitive placeholders with modeled miniatures and VFX.
- Replace sample trade/export controls with production UI Toolkit panels.
- Add auctions, deed inspection, and manual mortgage/sell-house screens.
- Add ownership deeds, rent enforcement, auctions, mortgages, trades, and bankruptcy UI.
- Add Photon/Netcode multiplayer once the tabletop feel is locked.
- Replace IMGUI with UI Toolkit or uGUI panels.
- Add sound, music, post-processing, and cinematic camera beats.
