import type { GameState } from '../engine/types';

export function PlayerPanel({ game }: { game: GameState }) {
  return <aside className="panel">
    <h2>Players</h2>
    {game.players.map((player, index) => <div className={`player-card ${index === game.currentPlayerIndex ? 'active' : ''}`} key={player.id}>
      <span className="swatch" style={{ background: player.color }} />
      <div><strong>{player.name}</strong><small>{player.status}</small></div>
      <b>${player.cash}</b>
    </div>)}
  </aside>;
}
