import type { AuthMode } from '../types'
import './AuthPanel.css'

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
    <section className="panel auth-panel">
      <h2>Авторизация</h2>
      <div className="auth">
        <div className="auth__tabs">
          <button
            className={authMode === 'login' ? 'tab tab--active' : 'tab'}
            onClick={() => onAuthModeChange('login')}
          >
            Вход
          </button>
          <button
            className={authMode === 'register' ? 'tab tab--active' : 'tab'}
            onClick={() => onAuthModeChange('register')}
          >
            Регистрация
          </button>
        </div>
        <label className="field">
          <span>Email</span>
          <input
            type="email"
            value={email}
            onChange={event => onEmailChange(event.target.value)}
            placeholder="you@example.com"
          />
        </label>
        <label className="field">
          <span>Пароль</span>
          <input
            type="password"
            value={password}
            onChange={event => onPasswordChange(event.target.value)}
            placeholder="Минимум 6 символов"
          />
        </label>
        <button className="btn btn--accent" onClick={onSubmit}>
          {authMode === 'register' ? 'Создать аккаунт' : 'Войти'}
        </button>
      </div>
    </section>
  )
}
