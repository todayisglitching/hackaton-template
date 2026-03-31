import './ControlPanel.css'

type Props = {
  onAddDevice: () => void
  supportsBle: boolean
  warning: string | null
}

export function ControlPanel({ onAddDevice, supportsBle, warning }: Props) {
  return (
    <div className="panel">
      <h2>Управление</h2>
      <div className="controls">
        <button className="btn" onClick={onAddDevice}>Добавить устройство</button>
      </div>
      {!supportsBle && (
        <p className="warning">
          Bluetooth доступен только в Chromium-подобных браузерах (Chrome, Edge, Opera) с поддержкой Web Bluetooth.
        </p>
      )}
      {warning && <p className="warning">{warning}</p>}
    </div>
  )
}
