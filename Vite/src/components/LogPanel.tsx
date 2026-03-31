import { Card } from '@heroui/react'

type LogEntry = { id: number; text: string }

export function LogPanel({ logs }: { logs: LogEntry[] }) {
  return (
    <Card className="w-full">
      <Card.Header>
        <h2 className="text-xl font-semibold">Логи</h2>
      </Card.Header>
      <div className="px-6 pb-6">
        <div className="h-64 overflow-y-auto rounded-lg border bg-muted/20 p-4">
          {logs.length === 0 && (
            <div className="flex h-full items-center justify-center text-muted-foreground">
              Пока нет событий
            </div>
          )}
          <div className="space-y-2">
            {logs.map(entry => (
              <div
                key={entry.id}
                className="rounded bg-background px-3 py-2 text-sm font-mono border"
              >
                <span className="text-muted-foreground">[{entry.id}]</span>{' '}
                {entry.text}
              </div>
            ))}
          </div>
        </div>
      </div>
    </Card>
  )
}
