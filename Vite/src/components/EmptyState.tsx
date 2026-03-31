import { Card } from '@heroui/react'

export function EmptyState() {
  return (
    <Card className="p-8 text-center">
      <div className="flex flex-col items-center gap-4">
        <div className="size-16 rounded-full bg-muted flex items-center justify-center">
          <svg
            className="size-8 text-muted-foreground"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
            xmlns="http://www.w3.org/2000/svg"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10"
            />
          </svg>
        </div>
        <div>
          <h2 className="text-xl font-semibold mb-2">Нет устройств</h2>
          <p className="text-muted-foreground">
            Добавьте чайник, чтобы начать получать температуру.
          </p>
        </div>
      </div>
    </Card>
  )
}
