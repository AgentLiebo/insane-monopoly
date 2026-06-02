import type { GameConfig, GameState, PlayerState, PropertyDefinition } from './types';
import { calculateRent } from './engine';

export interface AiDecision {
  buy: boolean;
  bidLimit: number;
  tradeTargets: string[];
  rationale: string;
}

export function evaluateProperty(config: GameConfig, state: GameState, property: PropertyDefinition, player: PlayerState): number {
  const setSize = config.properties.filter((candidate) => candidate.set === property.set).length;
  const ownedInSet = config.properties.filter((candidate) => candidate.set === property.set && state.ownership[candidate.id]?.ownerId === player.id).length;
  const rentPotential = calculateRent(config, state, property, 7);
  const completionBonus = setSize > 0 ? (ownedInSet / setSize) * property.price * 0.65 : 0;
  const liquidityPenalty = player.cash < property.price * 1.5 ? property.price * 0.25 : 0;
  return property.price * 0.45 + rentPotential * 4 + completionBonus - liquidityPenalty;
}

export function decidePurchase(config: GameConfig, state: GameState, property: PropertyDefinition, player: PlayerState): AiDecision {
  const value = evaluateProperty(config, state, property, player);
  const riskMultiplier = player.difficulty === 'hard' || player.difficulty === 'insane' ? 1.15 : player.difficulty === 'easy' ? 0.8 : 1;
  const bidLimit = Math.floor(value * riskMultiplier);
  return {
    buy: player.cash - property.price > 150 && bidLimit >= property.price,
    bidLimit,
    tradeTargets: config.properties.filter((candidate) => candidate.set === property.set && state.ownership[candidate.id]?.ownerId !== player.id).map((candidate) => candidate.id),
    rationale: `Values ${property.name} at ${bidLimit} based on set completion, rent potential, and liquidity.`
  };
}
