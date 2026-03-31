import './LogPanel.css'

type LogEntry = { id: number; text: string }

export function LogPanel({ logs }: { logs: LogEntry[] }) {
  return (
    <div className="panel panel--wide">
      <h2>Логи</h2>
      <div className="log">
        {logs.length === 0 && <span className="log__empty">Пока нет событий</span>}
        {logs.map(entry => (
          <div key={entry.id} className="log__line">{entry.text}</div>
        ))}
      </div>
    </div>
  )
}
