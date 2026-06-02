import { useMemo, useState } from 'react';
import { Board } from './components/Board';
import { EventLog } from './components/EventLog';
import { PlayerPanel } from './components/PlayerPanel';
import { defaultConfig } from './engine/config';
import { createGame, rollAndMove } from './engine/engine';
import './styles/app.css';

export default function App() {
  const initial = useMemo(() => createGame(defaultConfig, ['Nova', 'Orion'], 2), []);
  const [game, setGame] = useState(initial);
  const [theme, setTheme] = useState('dark' as 'dark' | 'light');

  function roll() {
    const result = rollAndMove(defaultConfig, structuredClone(game));
    setGame({ ...result.state });
  }

  const current = game.players[game.currentPlayerIndex];
  return <main className={`app ${theme}`}>
    <header className="hero">
      <div>
        <p className="eyebrow">Standalone desktop + web board game</p>
        <h1>Insane Monopoly</h1>
        <p>Data-driven expanded Monopoly with chaos cards, event spaces, AI-ready valuation, lobbies, replays, and Electron packaging.</p>
      </div>
      <div className="actions">
        <button onClick={() => setTheme(theme === 'dark' ? 'light' : 'dark')}>{theme === 'dark' ? 'Light' : 'Dark'} theme</button>
        <button className="primary" onClick={roll}>Roll for {current.name}</button>
      </div>
    </header>
    <section className="layout">
      <PlayerPanel game={game} />
      <Board board={defaultConfig.board} game={game} />
      <EventLog entries={game.log} />
    </section>
    <section className="dock">
      <div><h2>Last roll</h2><p className="dice">{game.lastRoll ? game.lastRoll.join(' + ') : '—'}</p></div>
      <div><h2>Free Parking</h2><p>${game.freeParkingJackpot}</p></div>
      <div><h2>Systems</h2><p>Trading, auctions, save/load, spectators, and replay events are exposed through the engine/API modules.</p></div>
    </section>
  </main>;
}
