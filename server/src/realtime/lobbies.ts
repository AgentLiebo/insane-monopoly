import { createGame, rollAndMove } from '../../../src/engine/engine';
import { defaultConfig } from '../../../src/engine/config';
import type { GameStore } from '../db';

interface Lobby { id: string; inviteCode: string; hostSocketId: string; players: string[]; spectators: string[]; chat: string[]; }
const lobbies = new Map<string, Lobby>();
const invite = () => Math.random().toString(36).slice(2, 8).toUpperCase();

export function registerRealtime(io: any, store: GameStore): void {
  io.on('connection', (socket) => {
    socket.on('lobby:create', (_, reply) => {
      const lobby: Lobby = { id: crypto.randomUUID(), inviteCode: invite(), hostSocketId: socket.id, players: [socket.id], spectators: [], chat: [] };
      lobbies.set(lobby.id, lobby);
      socket.join(lobby.id);
      reply?.(lobby);
    });

    socket.on('lobby:join', ({ inviteCode, spectator }, reply) => {
      const lobby = [...lobbies.values()].find((candidate) => candidate.inviteCode === inviteCode);
      if (!lobby) return reply?.({ error: 'Lobby not found' });
      (spectator ? lobby.spectators : lobby.players).push(socket.id);
      socket.join(lobby.id);
      io.to(lobby.id).emit('lobby:update', lobby);
      reply?.(lobby);
    });

    socket.on('game:start', ({ lobbyId, names, aiPlayers }, reply) => {
      const lobby = lobbies.get(lobbyId);
      if (!lobby || lobby.hostSocketId !== socket.id) return reply?.({ error: 'Only host can start' });
      const game = createGame(defaultConfig, names, aiPlayers ?? 0);
      store.saveGame(game, 'online');
      io.to(lobby.id).emit('game:state', game);
      reply?.(game);
    });

    socket.on('game:roll', ({ gameId }, reply) => {
      const game = store.loadGame(gameId);
      if (!game) return reply?.({ error: 'Game not found' });
      const result = rollAndMove(defaultConfig, game);
      store.saveGame(result.state, 'online');
      io.emit('game:state', result.state);
      reply?.(result);
    });

    socket.on('chat:send', ({ lobbyId, message }) => {
      const lobby = lobbies.get(lobbyId);
      if (!lobby) return;
      lobby.chat.push(message);
      io.to(lobbyId).emit('chat:update', lobby.chat);
    });
  });
}
