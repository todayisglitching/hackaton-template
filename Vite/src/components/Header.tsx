import { Surface } from '@heroui/react'

export function Header() {
  return (
    <Surface className="p-8 rounded-2xl bg-gradient-to-br from-primary/10 to-secondary/10">
      <div className="max-w-4xl">
        <p className="text-sm font-medium text-primary mb-2">Ростелеком • Smart Hub</p>
        <h1 className="text-4xl font-bold tracking-tight mb-4">Контроль температуры чайника</h1>
        <p className="text-lg text-muted-foreground mb-6">
          Интерфейс подключается к BLE-сервису `1234` и отправляет показания в облако через ASP.NET API.
        </p>
        <div className="inline-flex items-center gap-2 px-4 py-2 bg-primary/10 text-primary rounded-full text-sm font-medium">
          <span className="size-2 bg-primary rounded-full"></span>
          BLE • WebSocket • ASP.NET
        </div>
      </div>
    </Surface>
  )
}
