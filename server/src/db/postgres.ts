import type { GameState } from '../../../src/engine/types';
import type { GameStore } from './index';

export function createPostgresStore(pool: any): GameStore {
  return {
    async saveGame(state: GameState, mode = 'online') {
      await pool.query(
        `INSERT INTO games (id, mode, state_json, updated_at) VALUES ($1, $2, $3, CURRENT_TIMESTAMP)
         ON CONFLICT (id) DO UPDATE SET state_json = $3, updated_at = CURRENT_TIMESTAMP`,
        [state.id, mode, JSON.stringify(state)]
      );
    },
    async loadGame(id: string) {
      const result = await pool.query('SELECT state_json FROM games WHERE id = $1', [id]);
      return result.rows[0]?.state_json as GameState | undefined;
    },
    async listGames() {
      const result = await pool.query('SELECT id, updated_at as "updatedAt" FROM games ORDER BY updated_at DESC');
      return result.rows as Array<{ id: string; updatedAt: string }>;
    }
  } as unknown as GameStore;
}
