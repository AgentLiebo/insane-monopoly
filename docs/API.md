# API Documentation

## REST

- `GET /api/health` returns service health and game name.
- `GET /api/config` returns board, properties, cards, events, and rules.
- `GET /api/games` lists saved games.
- `POST /api/games` creates a game. Body: `{ "players": ["A", "B"], "aiPlayers": 2 }`.
- `GET /api/games/:id` loads a saved game.
- `POST /api/games/:id/roll` rolls and advances the current player. Optional body: `{ "dice": [3, 4] }`.

## Socket.io

- `lobby:create` creates a private lobby and invite code.
- `lobby:join` joins a lobby by invite code; supports spectator flag.
- `game:start` starts a lobby game.
- `game:roll` advances an online game turn.
- `chat:send` appends lobby chat.

## Persistence

SQLite is the default local adapter. The schema in `server/src/db/schema.sql` is intentionally portable to PostgreSQL with minor type changes for production hosting.
