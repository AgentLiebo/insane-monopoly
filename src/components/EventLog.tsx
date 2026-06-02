export function EventLog({ entries }: { entries: string[] }) {
  return <aside className="panel log"><h2>Event Log</h2>{entries.slice(-12).reverse().map((entry, index) => <p key={`${entry}-${index}`}>{entry}</p>)}</aside>;
}
