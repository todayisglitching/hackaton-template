import type { AuthMode } from '../types'
import { Card, Button, Input } from '@heroui/react'

type Props = {
  authMode: AuthMode
  email: string
  password: string
  onAuthModeChange: (mode: AuthMode) => void
  onEmailChange: (value: string) => void
  onPasswordChange: (value: string) => void
  onSubmit: () => void
}

export function AuthPanel({
  authMode,
  email,
  password,
  onAuthModeChange,
  onEmailChange,
  onPasswordChange,
  onSubmit
}: Props) {
  return (
    <Card className="max-w-md w-full p-6">
      <div className="flex flex-col gap-6">
        <h2 className="text-2xl font-bold">Авторизация</h2>
        <div className="flex gap-2">
          <Button
            variant={authMode === 'login' ? 'primary' : 'outline'}
            onPress={() => onAuthModeChange('login')}
            className="flex-1"
          >
            Вход
          </Button>
          <Button
            variant={authMode === 'register' ? 'primary' : 'outline'}
            onPress={() => onAuthModeChange('register')}
            className="flex-1"
          >
            Регистрация
          </Button>
        </div>

        <div className="flex flex-col gap-4">
          <Input
            type="email"
            aria-label="Email"
            placeholder="you@example.com"
            value={email}
            onChange={(event) => onEmailChange(event.target.value)}
            required
          />
          <Input
            type="password"
            aria-label="Пароль"
            placeholder="Минимум 6 символов"
            value={password}
            onChange={(event) => onPasswordChange(event.target.value)}
            required
          />
          
          <Button 
            onPress={onSubmit}
            className="w-full"
            variant="primary"
          >
            {authMode === 'register' ? 'Создать аккаунт' : 'Войти'}
          </Button>
        </div>
      </div>
    </Card>
  )
}
