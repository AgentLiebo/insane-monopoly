# Insane Monopoly

Insane Monopoly is being pivoted from the initial React/Electron scaffold into a proper 3D Unity tabletop game — the “Monopoly on steroids” direction.

## Run the 3D Unity game

1. Install Unity 2022.3 LTS.
2. Open the `Unity/` folder as a Unity project.
3. Open `Assets/Scenes/Main.unity`.
4. Press **Play**.

See [`docs/UNITY.md`](docs/UNITY.md) for controls, data file locations, and build steps. 
See [`docs/UNITY_ARCHITECTURE.md`](docs/UNITY_ARCHITECTURE.md) for the 3D Monopoly engine layout.

## Legacy web scaffold

The earlier React/Node scaffold still exists for reference and possible future web services. Its commands are:

```bash
npm install
npm run dev
npm run dev:server
```

The Unity project is now the primary playable direction.
