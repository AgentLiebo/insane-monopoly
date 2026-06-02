# Installation Guide

## Primary version: Unity 3D

This repo now ships a Unity 3D implementation under `Unity/`.

```text
Unity/Assets/Scenes/Main.unity
```

To run it:

1. Install Unity 2022.3 LTS.
2. Open the `Unity/` folder in Unity Hub.
3. Open `Assets/Scenes/Main.unity`.
4. Press **Play**.

See [`UNITY.md`](UNITY.md) for controls and build instructions.

## Legacy web/server scaffold

The original React/Node scaffold is still present for reference and possible backend services.

```bash
npm install
npm run test
npm run dev
npm run dev:server
```

Open the Vite URL for the web client. For desktop Electron development, run:
Open the Vite URL for the web client. For desktop development, run:

```bash
npm run electron:dev
```

For packaged Electron builds:
For packaged desktop builds:

```bash
npm run electron:package
```
