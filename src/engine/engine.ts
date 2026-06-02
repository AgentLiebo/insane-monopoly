import type { BoardSpace, CardDefinition, GameConfig, GameState, OwnedPropertyState, PlayerState, PropertyDefinition, TurnResult } from './types';

const colors = ['#ef4444', '#3b82f6', '#22c55e', '#eab308', '#a855f7', '#f97316', '#14b8a6', '#ec4899'];

export function createGame(config: GameConfig, playerNames: string[], aiPlayers = 0): GameState {
  const totalPlayers = playerNames.length + aiPlayers;
  if (totalPlayers < config.rules.playerLimits.min || totalPlayers > config.rules.playerLimits.max) {
    throw new Error(`Insane Monopoly supports ${config.rules.playerLimits.min}-${config.rules.playerLimits.max} players.`);
  }
  const players: PlayerState[] = [
    ...playerNames.map((name, index) => createPlayer(name, index, config.rules.startingCash, false)),
    ...Array.from({ length: aiPlayers }, (_, index) => createPlayer(`AI ${index + 1}`, playerNames.length + index, config.rules.startingCash, true))
  ];
  const ownership = Object.fromEntries(config.properties.map((property) => [property.id, { propertyId: property.id, mortgaged: false, upgrades: 0 } satisfies OwnedPropertyState]));
  return {
    id: crypto.randomUUID(),
    phase: 'playing',
    turn: 1,
    currentPlayerIndex: 0,
    players,
    ownership,
    chanceDeck: shuffle(config.cards.filter((card) => card.deck === 'chance').map((card) => card.id)),
    chaosDeck: shuffle(config.cards.filter((card) => card.deck === 'chaos').map((card) => card.id)),
    freeParkingJackpot: 0,
    log: [`Game started with ${totalPlayers} players.`]
  };
}

function createPlayer(name: string, index: number, cash: number, isAi: boolean): PlayerState {
  return { id: crypto.randomUUID(), name, color: colors[index], cash, position: 0, status: 'active', jailTurns: 0, jailPasses: 0, isAi, difficulty: isAi ? 'normal' : undefined, stats: { laps: 0, rentPaid: 0, rentCollected: 0, cardsDrawn: 0 } };
}

export function rollAndMove(config: GameConfig, state: GameState, dice: [number, number] = rollDice()): TurnResult {
  const messages: string[] = [];
  const player = currentPlayer(state);
  if (player.status !== 'active' && player.status !== 'jailed') return { state, messages: ['Inactive player skipped.'] };

  state.lastRoll = dice;
  messages.push(`${player.name} rolled ${dice[0]} + ${dice[1]}.`);

  if (player.status === 'jailed') {
    const doubles = dice[0] === dice[1];
    player.jailTurns += 1;
    if (doubles || player.jailTurns >= config.rules.jail.maxTurns) {
      player.status = 'active';
      player.jailTurns = 0;
      messages.push(`${player.name} escaped jail${doubles ? ' with doubles' : ' after serving time'}.`);
    } else {
      messages.push(`${player.name} remains in jail.`);
      return endTurn(state, messages);
    }
  }

  movePlayer(config, state, player, dice[0] + dice[1], messages);
  resolveSpace(config, state, player, messages);
  checkBankruptcyAndVictory(state, messages);
  return endTurn(state, messages);
}

export function buyProperty(config: GameConfig, state: GameState, playerId: string, propertyId: string): void {
  const player = state.players.find((candidate) => candidate.id === playerId);
  const property = config.properties.find((candidate) => candidate.id === propertyId);
  const owned = state.ownership[propertyId];
  if (!player || !property || !owned || owned.ownerId || player.cash < property.price) return;
  player.cash -= property.price;
  owned.ownerId = player.id;
  state.log.push(`${player.name} bought ${property.name} for ${property.price}.`);
}

export function calculateRent(config: GameConfig, state: GameState, property: PropertyDefinition, diceTotal = 0): number {
  const owned = state.ownership[property.id];
  if (!owned || owned.mortgaged) return 0;
  if (property.kind === 'utility') {
    const utilityCount = config.properties.filter((candidate) => candidate.kind === 'utility' && state.ownership[candidate.id]?.ownerId === owned.ownerId).length;
    return diceTotal * (property.multipliers?.[utilityCount > 1 ? 1 : 0] ?? 4);
  }
  if (property.kind === 'railroad') {
    const railCount = config.properties.filter((candidate) => candidate.kind === 'railroad' && state.ownership[candidate.id]?.ownerId === owned.ownerId).length;
    return property.rent?.[Math.max(0, railCount - 1)] ?? 25;
  }
  const setComplete = ownsSet(config, state, owned.ownerId, property.set);
  const baseRent = property.rent?.[owned.upgrades] ?? 0;
  return owned.upgrades === 0 && setComplete ? baseRent * 2 : baseRent;
}

function resolveSpace(config: GameConfig, state: GameState, player: PlayerState, messages: string[]): void {
  const space = config.board[player.position];
  messages.push(`${player.name} landed on ${space.name}.`);
  if (space.type === 'property' && space.propertyId) resolveProperty(config, state, player, space, messages);
  if (space.type === 'tax') payBank(state, player, space.amount ?? 0, messages, space.name);
  if (space.type === 'card' && space.deck) drawCard(config, state, player, space.deck, messages);
  if (space.action === 'goToJail') sendToJail(config, state, player, messages);
  if (space.action === 'freeParking') collectFreeParking(state, player, messages);
  if (space.type === 'event') resolveEvent(config, state, player, space.eventId, messages);
}

function resolveProperty(config: GameConfig, state: GameState, player: PlayerState, space: BoardSpace, messages: string[]): void {
  const property = config.properties.find((candidate) => candidate.id === space.propertyId);
  const owned = property ? state.ownership[property.id] : undefined;
  if (!property || !owned) return;
  if (!owned.ownerId) {
    messages.push(`${property.name} is available for ${property.price}.`);
    return;
  }
  if (owned.ownerId === player.id) return;
  const owner = state.players.find((candidate) => candidate.id === owned.ownerId);
  if (!owner) return;
  const rent = calculateRent(config, state, property, state.lastRoll?.reduce((a, b) => a + b, 0) ?? 0);
  transfer(player, owner, rent);
  player.stats.rentPaid += rent;
  owner.stats.rentCollected += rent;
  messages.push(`${player.name} paid ${owner.name} ${rent} rent for ${property.name}.`);
}

function drawCard(config: GameConfig, state: GameState, player: PlayerState, deck: string, messages: string[]): void {
  const deckKey = deck === 'chance' ? 'chanceDeck' : 'chaosDeck';
  const cardId = state[deckKey].shift();
  const card = config.cards.find((candidate) => candidate.id === cardId);
  if (!card) return;
  state[deckKey].push(card.id);
  player.stats.cardsDrawn += 1;
  messages.push(`${player.name} drew ${card.title}: ${card.text}`);
  applyCardEffect(config, state, player, card, messages);
}

function applyCardEffect(config: GameConfig, state: GameState, player: PlayerState, card: CardDefinition, messages: string[]): void {
  const effect = card.effect;
  if (effect.type === 'cash') player.cash += Number(effect.amount);
  if (effect.type === 'goToJail') sendToJail(config, state, player, messages);
  if (effect.type === 'jailPass') player.jailPasses += 1;
  if (effect.type === 'moveTo') {
    const destination = config.board.findIndex((space) => space.id === effect.spaceId);
    if (destination >= 0) player.position = destination;
    if (effect.collectSalary) player.cash += config.rules.salary;
  }
  if (effect.type === 'payEachPlayer') state.players.filter((p) => p.id !== player.id && p.status !== 'bankrupt').forEach((p) => transfer(player, p, Number(effect.amount)));
  if (effect.type === 'collectFromEachPlayer') state.players.filter((p) => p.id !== player.id && p.status !== 'bankrupt').forEach((p) => transfer(p, player, Number(effect.amount)));
}

function resolveEvent(config: GameConfig, state: GameState, player: PlayerState, eventId: string | undefined, messages: string[]): void {
  const event = config.events.find((candidate) => candidate.id === eventId);
  if (!event) return;
  messages.push(event.description);
  if (event.effect.type === 'collectJackpotFraction') {
    const payout = Math.floor(state.freeParkingJackpot * Number(event.effect.fraction));
    state.freeParkingJackpot -= payout;
    player.cash += payout;
  }
}

function movePlayer(config: GameConfig, state: GameState, player: PlayerState, steps: number, messages: string[]): void {
  const previous = player.position;
  player.position = (player.position + steps) % config.board.length;
  if (player.position < previous) {
    player.cash += config.rules.salary;
    player.stats.laps += 1;
    messages.push(`${player.name} passed GO and collected ${config.rules.salary}.`);
  }
}

function sendToJail(config: GameConfig, state: GameState, player: PlayerState, messages: string[]): void {
  const jailIndex = config.board.findIndex((space) => space.id === config.rules.jail.spaceId);
  player.position = Math.max(0, jailIndex);
  player.status = 'jailed';
  player.jailTurns = 0;
  messages.push(`${player.name} was sent to jail.`);
}

function payBank(state: GameState, player: PlayerState, amount: number, messages: string[], reason: string): void {
  player.cash -= amount;
  state.freeParkingJackpot += amount;
  messages.push(`${player.name} paid ${amount} for ${reason}.`);
}

function collectFreeParking(state: GameState, player: PlayerState, messages: string[]): void {
  const amount = state.freeParkingJackpot;
  player.cash += amount;
  state.freeParkingJackpot = 0;
  messages.push(`${player.name} collected ${amount} from Free Parking.`);
}

function endTurn(state: GameState, messages: string[]): TurnResult {
  state.log.push(...messages);
  const activeCount = state.players.filter((player) => player.status !== 'bankrupt').length;
  if (activeCount > 1) do state.currentPlayerIndex = (state.currentPlayerIndex + 1) % state.players.length; while (state.players[state.currentPlayerIndex].status === 'bankrupt');
  state.turn += 1;
  return { state, messages };
}

function checkBankruptcyAndVictory(state: GameState, messages: string[]): void {
  for (const player of state.players) {
    if (player.cash < 0) {
      player.status = 'bankrupt';
      for (const owned of Object.values(state.ownership)) if (owned.ownerId === player.id) owned.ownerId = undefined;
      messages.push(`${player.name} is bankrupt.`);
    }
  }
  const survivors = state.players.filter((player) => player.status !== 'bankrupt');
  if (survivors.length === 1) {
    state.phase = 'finished';
    state.winnerId = survivors[0].id;
    messages.push(`${survivors[0].name} wins Insane Monopoly!`);
  }
}

function currentPlayer(state: GameState): PlayerState {
  return state.players[state.currentPlayerIndex];
}

function ownsSet(config: GameConfig, state: GameState, ownerId: string | undefined, set: string): boolean {
  if (!ownerId) return false;
  const setProperties = config.properties.filter((property) => property.set === set && property.kind === 'street');
  return setProperties.length > 0 && setProperties.every((property) => state.ownership[property.id]?.ownerId === ownerId);
}

function transfer(from: PlayerState, to: PlayerState, amount: number): void {
  from.cash -= amount;
  to.cash += amount;
}

function rollDice(): [number, number] {
  return [Math.floor(Math.random() * 6) + 1, Math.floor(Math.random() * 6) + 1];
}

function shuffle<T>(items: T[]): T[] {
  return [...items].sort(() => Math.random() - 0.5);
}
