import type { DeviceDto } from '../types'
import { Card, Button, Badge } from '@heroui/react'

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
    <Card className="w-full max-w-xs">
      <Card.Header className="flex flex-row items-center justify-between">
        <h3 className="text-lg font-semibold">Дашборд</h3>
        <Button onPress={onAdd} size="sm" variant="outline">
          Добавить
        </Button>
      </Card.Header>
      <div className="px-4 py-2">
        <div className="space-y-2">
          {devices.length === 0 && (
            <p className="text-sm text-muted-foreground text-center py-4">Устройств нет</p>
          )}
          {devices.map(device => (
            <div
              key={device.deviceId}
              className={`flex items-center justify-between p-3 rounded-lg cursor-pointer transition-colors ${
                selectedId === device.deviceId
                  ? 'bg-primary/10 border border-primary/20'
                  : 'hover:bg-muted/50'
              }`}
              onClick={() => onSelect(device.deviceId)}
            >
              <div className="flex-1">
                <div className="flex items-center gap-2">
                  <span className="font-medium">{device.name}</span>
                  {selectedId === device.deviceId && statusHint && (
                    <Badge size="sm" variant="secondary" className="ml-2">
                      {statusHint}
                    </Badge>
                  )}
                </div>
              </div>
              <Button
                size="sm"
                variant="ghost"
                onPress={() => onRemove(device.deviceId)}
                aria-label="Удалить устройство"
                className="text-muted-foreground hover:text-danger"
              >
                🗑
              </Button>
            </div>
          ))}
        </div>
      </div>
      <div className="p-4 border-t">
        <Button onPress={onSignOut} variant="outline" className="w-full">
          Выйти
        </Button>
      </div>
    </Card>
  )
}
