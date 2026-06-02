import Database from 'better-sqlite3';
import fs from 'node:fs';
import path from 'node:path';
import type { GameState } from '../../../src/engine/types';

export interface GameStore {
  saveGame(state: GameState, mode?: string): void;
  loadGame(id: string): GameState | undefined;
  listGames(): Array<{ id: string; updatedAt: string }>;
}

export function createSqliteStore(file = 'insane-monopoly.sqlite'): GameStore {
  const db = new Database(file);
  const schema = fs.readFileSync(path.resolve('server/src/db/schema.sql'), 'utf8');
  db.exec(schema);
  return {
    saveGame(state, mode = 'local') {
      db.prepare(`INSERT INTO games (id, mode, state_json, updated_at) VALUES (@id, @mode, @state, CURRENT_TIMESTAMP)
        ON CONFLICT(id) DO UPDATE SET state_json=@state, updated_at=CURRENT_TIMESTAMP`).run({ id: state.id, mode, state: JSON.stringify(state) });
    },
    loadGame(id) {
      const row = db.prepare('SELECT state_json FROM games WHERE id = ?').get(id) as { state_json: string } | undefined;
      return row ? JSON.parse(row.state_json) as GameState : undefined;
    },
    listGames() {
      return db.prepare('SELECT id, updated_at as updatedAt FROM games ORDER BY updated_at DESC').all() as Array<{ id: string; updatedAt: string }>;
    }
  };
}
