# Insane Monopoly

Insane Monopoly is now focused on a standalone **offline Unity Windows game**: a polished digital Monopoly-style board game with automated turns, AI opponents, animated dice, automatic movement/rent/cards/auctions, and data-driven rules.

It is **not** trying to recreate Tabletop Simulator and does not require Steam, online services, or Tabletop Simulator dependencies.

## Run the Unity game in the editor

1. Install Unity 2022.3 LTS with the Windows desktop build module.
2. Open the `Unity/` folder as a Unity project.
3. Open `Assets/Scenes/Main.unity`.
4. Press **Play**.

## Build the Windows executable

From the Unity menu, run:

```text
Insane Monopoly > Build Windows EXE
```

The output path is:

```text
Unity/Builds/Windows/InsaneMonopoly.exe
```

See [`docs/OFFLINE_WINDOWS_GAME.md`](docs/OFFLINE_WINDOWS_GAME.md) for the product target and [`docs/UNITY_ARCHITECTURE.md`](docs/UNITY_ARCHITECTURE.md) for the rules-engine layout.

## Legacy web/server scaffold

The React/Node files remain as reference tooling and possible future services, but they are no longer the primary game target. The main deliverable is the offline Unity executable.
