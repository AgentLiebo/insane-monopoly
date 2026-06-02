import cors from 'cors';
import express from 'express';
import { createServer } from 'node:http';
import { Server } from 'socket.io';
import { createGame, rollAndMove } from '../../src/engine/engine';
import { defaultConfig, validateConfig } from '../../src/engine/config';
import { createSqliteStore } from './db';
import { registerRealtime } from './realtime/lobbies';

validateConfig(defaultConfig);
const app = express();
const httpServer = createServer(app);
const io = new Server(httpServer, { cors: { origin: '*' } });
const store = createSqliteStore(process.env.SQLITE_FILE ?? 'insane-monopoly.sqlite');

app.use(cors());
app.use(express.json());
app.get('/api/health', (_, res) => res.json({ ok: true, name: defaultConfig.rules.name }));
app.get('/api/config', (_, res) => res.json(defaultConfig));
app.get('/api/games', (_, res) => res.json(store.listGames()));
app.post('/api/games', (req, res) => {
  const game = createGame(defaultConfig, req.body.players ?? ['Player 1', 'Player 2'], req.body.aiPlayers ?? 0);
  store.saveGame(game);
  res.status(201).json(game);
});
app.get('/api/games/:id', (req, res) => {
  const game = store.loadGame(req.params.id);
  if (!game) return res.status(404).json({ error: 'Game not found' });
  res.json(game);
});
app.post('/api/games/:id/roll', (req, res) => {
  const game = store.loadGame(req.params.id);
  if (!game) return res.status(404).json({ error: 'Game not found' });
  const result = rollAndMove(defaultConfig, game, req.body.dice);
  store.saveGame(result.state);
  res.json(result);
});

registerRealtime(io, store);
const port = Number(process.env.PORT ?? 3000);
httpServer.listen(port, () => console.log(`Insane Monopoly API listening on ${port}`));
