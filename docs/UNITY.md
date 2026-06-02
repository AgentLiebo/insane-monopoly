# Unity Version

This repo now includes a Unity implementation in `Unity/` aimed at a standalone offline Windows Monopoly-style video game rather than a web app or tabletop sandbox.

## Requirements

- Unity `2022.3.50f1` or another Unity 2022.3 LTS editor.
- Windows desktop build module if you want the local `.exe` deliverable.

## Run in the Editor

1. Open Unity Hub.
2. Choose **Add project from disk**.
3. Select the `Unity/` folder in this repository.
4. Open `Assets/Scenes/Main.unity`.
5. Press **Play**.

The scene contains a single bootstrap object. At runtime it procedurally creates:

- A cleaner 3D Monopoly-style board with 40 spaces.
- A table/felt presentation layer, wood rails, raised property tiles, a center platform, card deck stacks, and corner monuments.
- Animated dice cubes.
- 2–8 player pawns with lights.
- Orbit/zoom camera controls.
- A temporary HUD with player cash, dice, Free Parking pot, controls, and event log while production UI is built.

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

## Build Windows EXE

From Unity Editor, use the included menu command:

```text
Insane Monopoly > Build Windows EXE
```

It writes the executable to:

```text
Unity/Builds/Windows/InsaneMonopoly.exe
```

You can also use **File > Build Settings** manually. The included `ProjectSettings/EditorBuildSettings.asset` already registers the main scene.

## What is implemented now

- Procedural 3D board/table generation with rails, deck stacks, corner monuments, and readable labels.
- Runtime JSON loading through StreamingAssets.
- Neon material generation and tile labels.
- Animated dice roll presentation.
- Pawn movement with hop animation.
- GO salary, taxes, Free Parking pot, Go To Jail, and card landing events.
- Property ownership, rent payment, railroad/utility rent scaling, mortgages, and bankruptcy checks.
- Full-set building rules with visible house and hotel markers.
- Jail payment/cards/doubles handling, sample trading, AI buy/build heuristics, and save JSON export.
- Temporary IMGUI HUD for fast iteration; production UI should move to UI Toolkit or uGUI.

## Next Unity milestones

- Replace primitive placeholders with modeled miniatures and VFX.
- Replace sample trade/export controls with production UI Toolkit panels.
- Add auctions, deed inspection, and manual mortgage/sell-house screens.
- Replace IMGUI with UI Toolkit or uGUI panels.
- Add sound, music, post-processing, and cinematic camera beats.
