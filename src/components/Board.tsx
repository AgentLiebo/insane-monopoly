import type { BoardSpace, GameState } from '../engine/types';

interface BoardProps { board: BoardSpace[]; game: GameState; }

export function Board({ board, game }: BoardProps) {
  return <section className="board" aria-label="Insane Monopoly board">
    {board.map((space, index) => {
      const occupants = game.players.filter((player) => player.position === index && player.status !== 'bankrupt');
      return <article key={space.id} className={`space space-${space.type}`}>
        <span className="space-index">{index}</span>
        <strong>{space.name}</strong>
        <small>{space.type}</small>
        <div className="tokens">
          {occupants.map((player) => <span key={player.id} className="token" style={{ background: player.color }} title={player.name}>{player.name.slice(0, 1)}</span>)}
        </div>
      </article>;
    })}
  </section>;
}
