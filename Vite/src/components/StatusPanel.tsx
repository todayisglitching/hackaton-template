import './StatusPanel.css'

type Props = {
  status: string
  hint: string | null
  temperature: number | null
}

export function StatusPanel({ status, hint, temperature }: Props) {
  return (
    <div className="panel">
      <h2>Состояние</h2>
      <div className="status status--single">
        <strong>{status}</strong>
      </div>
      {hint && <p className="status-hint">{hint}</p>}
      <div className="status-temp">
        <div>
          <p className="status-temp__label">Температура</p>
          <strong className="status-temp__value">
            {temperature === null ? '—' : `${temperature}°C`}
          </strong>
        </div>
        {temperature !== null && (
          <div className="status-temp__bar">
            <div
              className="status-temp__fill"
              style={{ width: `${Math.min(100, temperature)}%` }}
            />
          </div>
        )}
      </div>
    </div>
  )
}
