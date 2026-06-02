import board from '../../data/board.json';
import cards from '../../data/cards.json';
import events from '../../data/events.json';
import properties from '../../data/properties.json';
import rules from '../../data/rules.json';
import type { GameConfig } from './types';

export const defaultConfig: GameConfig = { board: board as GameConfig['board'], cards: cards as GameConfig['cards'], events: events as GameConfig['events'], properties: properties as GameConfig['properties'], rules };

export function validateConfig(config: GameConfig): void {
  const propertyIds = new Set(config.properties.map((property) => property.id));
  for (const space of config.board) {
    if (space.type === 'property' && (!space.propertyId || !propertyIds.has(space.propertyId))) {
      throw new Error(`Board space ${space.id} references missing property ${space.propertyId}`);
    }
  }
}
