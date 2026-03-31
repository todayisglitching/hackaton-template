import { Card, ProgressBar, Badge } from '@heroui/react'

type Props = {
  status: string
  hint: string | null
  temperature: number | null
}

export function StatusPanel({ status, hint, temperature }: Props) {
  return (
    <Card className="w-full">
      <Card.Header>
        <h2 className="text-xl font-semibold">Состояние</h2>
      </Card.Header>
      <div className="px-6 pb-6 space-y-6">
        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <span className="text-lg font-medium">{status}</span>
            {hint && (
              <Badge variant="secondary" size="sm">
                {hint}
              </Badge>
            )}
          </div>
          
          {hint && <p className="text-sm text-muted-foreground">{hint}</p>}
        </div>

        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm text-muted-foreground">Температура</p>
              <p className="text-3xl font-bold">
                {temperature === null ? '—' : `${temperature}°C`}
              </p>
            </div>
            {temperature !== null && (
              <div className="text-right">
                <span className="text-sm text-muted-foreground">Прогресс</span>
                <p className="text-lg font-semibold">{Math.min(100, temperature)}%</p>
              </div>
            )}
          </div>
          
          {temperature !== null && (
            <ProgressBar 
              aria-label="Температура" 
              value={Math.min(100, temperature)}
              className="w-full"
            >
              <ProgressBar.Track>
                <ProgressBar.Fill />
              </ProgressBar.Track>
            </ProgressBar>
          )}
        </div>
      </div>
    </Card>
  )
}
