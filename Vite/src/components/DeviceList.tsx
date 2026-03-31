import type { DeviceDto } from '../types'
import './DeviceList.css'

type Props = {
  devices: DeviceDto[]
  selectedId: string | null
  statusHint: string | null
  onSelect: (deviceId: string) => void
  onAdd: () => void
  onRemove: (deviceId: string) => void
  onSignOut: () => void
}

export function DeviceList({ devices, selectedId, statusHint, onSelect, onAdd, onRemove, onSignOut }: Props) {
  return (
    <aside className="device-list">
      <div className="device-list__header">
        <h3>Дашборд</h3>
        <button className="btn btn--ghost" onClick={onAdd}>Добавить</button>
      </div>
      <div className="device-list__items">
        {devices.length === 0 && <p className="device-list__empty">Устройств нет</p>}
        {devices.map(device => (
          <div
            key={device.deviceId}
            className={selectedId === device.deviceId ? 'device-list__item device-list__item--active' : 'device-list__item'}
          >
            <button className="device-list__select" onClick={() => onSelect(device.deviceId)}>
              <span>{device.name}</span>
              {selectedId === device.deviceId && statusHint && (
                <span className="device-list__status" title={statusHint}>
                  <span className="device-list__dot" /> {statusHint}
                </span>
              )}
            </button>
            <button
              className="device-list__remove"
              onClick={() => onRemove(device.deviceId)}
              aria-label="Удалить устройство"
              title="Удалить устройство"
            >
              🗑
            </button>
          </div>
        ))}
      </div>
      <button className="btn" onClick={onSignOut}>Выйти</button>
    </aside>
  )
}
