import { describe, expect, it } from 'vitest';
import { buyProperty, calculateRent, createGame, rollAndMove } from '../src/engine/engine';
import { defaultConfig, validateConfig } from '../src/engine/config';

it('validates bundled JSON configuration', () => {
  expect(() => validateConfig(defaultConfig)).not.toThrow();
});

describe('engine', () => {
  it('creates a 2-8 player game from JSON config', () => {
    const game = createGame(defaultConfig, ['A', 'B'], 1);
    expect(game.players).toHaveLength(3);
    expect(game.players[0].cash).toBe(defaultConfig.rules.startingCash);
  });

  it('moves players and pays salary when passing GO', () => {
    const game = createGame(defaultConfig, ['A', 'B']);
    game.players[0].position = defaultConfig.board.length - 2;
    const result = rollAndMove(defaultConfig, game, [1, 2]);
    expect(result.state.players[0].cash).toBe(defaultConfig.rules.startingCash + defaultConfig.rules.salary);
  });

  it('calculates rent for owned properties', () => {
    const game = createGame(defaultConfig, ['A', 'B']);
    const property = defaultConfig.properties[0];
    buyProperty(defaultConfig, game, game.players[0].id, property.id);
    expect(calculateRent(defaultConfig, game, property)).toBeGreaterThan(0);
  });
});
