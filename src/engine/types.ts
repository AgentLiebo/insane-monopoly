export type SpaceType = 'corner' | 'property' | 'card' | 'tax' | 'event';
export type PropertyKind = 'street' | 'railroad' | 'utility';
export type PlayerStatus = 'active' | 'jailed' | 'bankrupt' | 'spectator';
export type Difficulty = 'easy' | 'normal' | 'hard' | 'insane';

export interface BoardSpace {
  id: string;
  name: string;
  type: SpaceType;
  propertyId?: string;
  deck?: string;
  amount?: number;
  action?: string;
  eventId?: string;
}

export interface PropertyDefinition {
  id: string;
  name: string;
  set: string;
  kind: PropertyKind;
  price: number;
  mortgage: number;
  upgradeCost?: number;
  rent?: number[];
  multipliers?: number[];
}

export interface CardDefinition {
  id: string;
  deck: string;
  title: string;
  text: string;
  effect: Record<string, unknown>;
}

export interface EventDefinition {
  id: string;
  name: string;
  description: string;
  effect: Record<string, unknown>;
}

export interface RuleSet {
  name: string;
  version: string;
  sourceNote: string;
  playerLimits: { min: number; max: number };
  startingCash: number;
  salary: number;
  jail: { spaceId: string; maxTurns: number; bail: number; doublesEscape: boolean };
  auction: { enabled: boolean; minimumBid: number; bidIncrement: number; timerSeconds: number };
  houses: { maxHouses: number; hotelAfterHouses: number; insaneTowerAfterHotels: number };
  victory: { type: string; alternateNetWorthTarget: number };
  houseRules: Record<string, boolean | number | string>;
}

export interface PlayerState {
  id: string;
  name: string;
  color: string;
  cash: number;
  position: number;
  status: PlayerStatus;
  jailTurns: number;
  jailPasses: number;
  isAi: boolean;
  difficulty?: Difficulty;
  stats: { laps: number; rentPaid: number; rentCollected: number; cardsDrawn: number };
}

export interface OwnedPropertyState {
  propertyId: string;
  ownerId?: string;
  mortgaged: boolean;
  upgrades: number;
}

export interface GameConfig {
  board: BoardSpace[];
  properties: PropertyDefinition[];
  cards: CardDefinition[];
  events: EventDefinition[];
  rules: RuleSet;
}

export interface GameState {
  id: string;
  phase: 'lobby' | 'playing' | 'finished';
  turn: number;
  currentPlayerIndex: number;
  players: PlayerState[];
  ownership: Record<string, OwnedPropertyState>;
  chanceDeck: string[];
  chaosDeck: string[];
  freeParkingJackpot: number;
  log: string[];
  lastRoll?: [number, number];
  winnerId?: string;
}

export interface TurnResult {
  state: GameState;
  messages: string[];
  requiresChoice?: 'buy-or-auction' | 'trade' | 'bankruptcy';
}
