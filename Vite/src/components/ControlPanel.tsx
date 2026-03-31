import { Card, Button, Alert } from '@heroui/react'

type Props = {
  onAddDevice: () => void
  supportsBle: boolean
  warning: string | null
}

export function ControlPanel({ onAddDevice, supportsBle, warning }: Props) {
  return (
    <Card className="w-full">
      <Card.Header>
        <h2 className="text-xl font-semibold">Управление</h2>
      </Card.Header>
      <div className="px-6 pb-6 space-y-6">
        <div className="space-y-4">
          <Button onPress={onAddDevice} className="w-full" variant="primary">
            Добавить устройство
          </Button>
        </div>

        {!supportsBle && (
          <Alert status="warning">
            <Alert.Indicator />
            <Alert.Content>
              <Alert.Title>Ограничение Bluetooth</Alert.Title>
              <Alert.Description>
                Bluetooth доступен только в Chromium-подобных браузерах (Chrome, Edge, Opera) с поддержкой Web Bluetooth.
              </Alert.Description>
            </Alert.Content>
          </Alert>
        )}

        {warning && (
          <Alert status="warning">
            <Alert.Indicator />
            <Alert.Content>
              <Alert.Title>Внимание</Alert.Title>
              <Alert.Description>{warning}</Alert.Description>
            </Alert.Content>
          </Alert>
        )}
      </div>
    </Card>
  )
}
