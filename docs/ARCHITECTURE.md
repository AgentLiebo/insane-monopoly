# System Architecture

## Packages

- `src/engine`: pure TypeScript game logic, AI heuristics, config validation, and shared types.
- `data`: JSON-driven board, property, card, event, and rules data.
- `src/components`: React UI for board, player panels, event log, and future modal systems.
- `server/src`: Express API, Socket.io lobby runtime, and persistence adapters.
- `electron`: desktop launcher that loads the built Vite app.

## Data Flow

1. Client or server loads `defaultConfig` from JSON.
2. `createGame` creates a serializable `GameState`.
3. User actions call engine functions such as `rollAndMove` or API endpoints.
4. Server persists updated state and emits `game:state` over Socket.io.
5. Replay systems append the same command/result events to `replay_events`.

## Design Constraint

Game content is not embedded in UI components. New board spaces, properties, cards, and events should be introduced through JSON first, then implemented as generic effect handlers only when a new effect type is needed.
