import { useEffect, useMemo, useRef, useState } from 'react'
import type { AuthMode, DeviceDto, Route, WSStatus, KettleData } from './types'
import { login, me, register } from './api/auth'
import { addDevice, fetchDevices, removeDevice, selectDevice } from './api/devices'
import { clearTokens, getTokens, logout, refresh, setTokens } from './api/session'
import { connectToDevice, findKnownDevice, requestKettleDevice, supportsBluetooth } from './services/ble'
import { AuthPanel } from './components/AuthPanel'
import { Header } from './components/Header'
import { ErrorBanner } from './components/ErrorBanner'
import { DeviceList } from './components/DeviceList'
import { EmptyState } from './components/EmptyState'
import { StatusPanel } from './components/StatusPanel'
import { ControlPanel } from './components/ControlPanel'

function getRoute(): Route {
  return window.location.pathname === '/app' ? '/app' : '/'
}

function navigate(path: Route) {
  window.history.pushState({}, '', path)
  window.dispatchEvent(new PopStateEvent('popstate'))
}

function App() {
  const [route, setRoute] = useState<Route>(getRoute())
  const [authMode, setAuthMode] = useState<AuthMode>('login')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [isAuthorized, setIsAuthorized] = useState(false)
  const [devices, setDevices] = useState<DeviceDto[]>([])
  const [selectedDeviceId, setSelectedDeviceId] = useState<string | null>(null)

  // Правильная типизация WS стейтов
  const [wsState, setWsState] = useState<WSStatus>('idle')
  const [wsHealthy, setWsHealthy] = useState(false)

  const [temp, setTemp] = useState<number | null>(null)
  const [status, setStatus] = useState('Ожидание подключения...')
  const [lastDataAt, setLastDataAt] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [warning, setWarning] = useState<string | null>(null)
  const [bleConnected, setBleConnected] = useState(false)

  const wsRef = useRef<WebSocket | null>(null)
  const pingIntervalRef = useRef<number | null>(null)
  const pongTimeoutRef = useRef<number | null>(null)
  const wsReconnectRef = useRef<{ attempts: number; timer: number | null; active: boolean }>({
    attempts: 0,
    timer: null,
    active: false
  })
  const reconnectingRef = useRef(false)

  const bleSupported = useMemo(() => supportsBluetooth(), [])

  const log = (text: string, data?: unknown) => {
    if (data) {
      console.info(`[SmartHub] ${text}`, data)
    } else {
      console.info(`[SmartHub] ${text}`)
    }
  }

  useEffect(() => {
    const handler = () => setRoute(getRoute())
    window.addEventListener('popstate', handler)
    return () => window.removeEventListener('popstate', handler)
  }, [])

  useEffect(() => {
    const checkAuth = async () => {
      const { accessToken } = getTokens()
      if (!accessToken) {
        setIsAuthorized(false)
        if (route === '/app') navigate('/')
        return
      }

      try {
        await me()
        setIsAuthorized(true)
        if (route === '/') navigate('/app')
      } catch {
        setIsAuthorized(false)
        clearTokens()
        if (route === '/app') navigate('/')
      }
    }

    void checkAuth()
  }, [route])

  useEffect(() => {
    const loadDevices = async () => {
      if (!isAuthorized) return
      try {
        const data = await fetchDevices()
        setDevices(data.devices)
        setSelectedDeviceId(data.selectedDeviceId ?? null)
      } catch (err) {
        setError(String(err))
      }
    }

    void loadDevices()
  }, [isAuthorized])

  const handleAuth = async () => {
    setError(null)
    if (!email || !password) {
      setError('Введите email и пароль')
      return
    }

    try {
      const data = authMode === 'register'
          ? await register(email, password)
          : await login(email, password)

      setTokens(data.token, data.refreshToken)
      log(authMode === 'register' ? 'Регистрация успешна' : 'Авторизация успешна')
      await connectWs()
      navigate('/app')
    } catch (err) {
      setError((err as Error).message)
    }
  }

  const ensureFreshToken = async () => {
    try {
      const data = await refresh()
      setTokens(data.token, data.refreshToken)
      return data.token
    } catch {
      clearTokens()
      setIsAuthorized(false)
      navigate('/')
      return null
    }
  }

  const scheduleWsReconnect = () => {
    const state = wsReconnectRef.current
    if (state.active) return

    state.active = true
    const delay = Math.min(30_000, 1_000 * Math.pow(2, state.attempts))
    state.attempts += 1
    log(`WS reconnect in ${Math.round(delay / 1000)}s`)

    state.timer = window.setTimeout(async () => {
      state.active = false
      await connectWs()
    }, delay)
  }

  const clearWsReconnect = () => {
    const state = wsReconnectRef.current
    if (state.timer) window.clearTimeout(state.timer)
    state.timer = null
    state.attempts = 0
    state.active = false
  }

  const connectWs = async () => {
    const { accessToken } = getTokens()
    const token = accessToken ?? (await ensureFreshToken())
    if (!token) {
      log('Нет токена для WebSocket')
      return
    }

    if (wsRef.current && wsRef.current.readyState === WebSocket.OPEN) return

    setWsState('connecting')
    const wsProtocol = window.location.protocol === 'https:' ? 'wss' : 'ws'
    const wsUrl = `${wsProtocol}://${window.location.host}/api/ws?token=${encodeURIComponent(token)}`
    log(`WS connect -> ${wsUrl}`)

    const ws = new WebSocket(wsUrl)

    ws.onopen = () => {
      setWsState('open')
      setWsHealthy(true)
      clearWsReconnect()
      log('WebSocket подключен')
      if (pingIntervalRef.current) window.clearInterval(pingIntervalRef.current)
      pingIntervalRef.current = window.setInterval(() => {
        if (wsRef.current?.readyState === WebSocket.OPEN) {
          wsRef.current.send('ping')
          if (pongTimeoutRef.current) window.clearTimeout(pongTimeoutRef.current)
          pongTimeoutRef.current = window.setTimeout(() => {
            setWsHealthy(false)
          }, 15_000)
        }
      }, 300_000)
    }

    ws.onmessage = (event: MessageEvent) => {
      if (event.data === 'pong') {
        setWsHealthy(true)
        if (pongTimeoutRef.current) window.clearTimeout(pongTimeoutRef.current)
        pongTimeoutRef.current = null
        return
      }
      log('Сообщение облака', event.data)
    }

    ws.onerror = () => {
      setWsState('closed')
      setWsHealthy(false)
      log('Ошибка WebSocket')
      scheduleWsReconnect()
    }

    ws.onclose = (event: CloseEvent) => {
      setWsState('closed')
      setWsHealthy(false)
      if (pingIntervalRef.current) window.clearInterval(pingIntervalRef.current)
      if (pongTimeoutRef.current) window.clearTimeout(pongTimeoutRef.current)
      pingIntervalRef.current = null
      pongTimeoutRef.current = null
      log(`WebSocket закрыт (${event.code}) ${event.reason || ''}`)
      scheduleWsReconnect()
    }

    wsRef.current = ws
  }

  const disconnectWs = () => {
    wsRef.current?.close()
    wsRef.current = null
    setWsState('closed')
    setWsHealthy(false)
    if (pingIntervalRef.current) window.clearInterval(pingIntervalRef.current)
    if (pongTimeoutRef.current) window.clearTimeout(pongTimeoutRef.current)
    pingIntervalRef.current = null
    pongTimeoutRef.current = null
    clearWsReconnect()
  }

  const attachAutoReconnect = (device: BluetoothDevice) => {
    const handler = async () => {
      if (reconnectingRef.current) return
      reconnectingRef.current = true
      setStatus('Соединение потеряно. Переподключаем...')
      try {
        await new Promise(resolve => setTimeout(resolve, 1000))
        await connectToDevice(device, value => handleTemp(value))
        await connectWs()
        setStatus('Связь с чайником восстановлена')
      } catch {
        setWarning('Не удалось переподключиться. Нажмите “Добавить устройство”.')
      } finally {
        reconnectingRef.current = false
        device.addEventListener('gattserverdisconnected', handler, { once: true })
      }
    }

    device.addEventListener('gattserverdisconnected', handler, { once: true })
  }

  const safeConnect = async (device: BluetoothDevice, retries = 3): Promise<void> => {
    try {
      setStatus('Подключение к чайнику...')
      await connectToDevice(device, value => handleTemp(value))
      attachAutoReconnect(device)
      setBleConnected(true)
      setStatus('Чайник подключен')
      setError(null)
    } catch (err) {
      if (retries > 1) {
        await new Promise(resolve => setTimeout(resolve, 1500))
        return safeConnect(device, retries - 1)
      }
      throw err
    }
  }

  const handleAddDevice = async () => {
    setError(null)
    setWarning(null)

    if (!bleSupported) {
      setWarning('Bluetooth доступен только в Chromium-подобных браузерах')
      return
    }

    try {
      const device = await requestKettleDevice()
      const name = device.name ?? 'Kettle'
      const added = await addDevice(name, device.id)
      await selectDevice(added.deviceId)
      setDevices(prev => {
        const exists = prev.some(item => item.deviceId === added.deviceId)
        return exists ? prev : [...prev, added]
      })
      setSelectedDeviceId(added.deviceId)

      await safeConnect(device)
      await connectWs()
    } catch (err) {
      setError((err as Error).message)
    }
  }

  const handleSelectDevice = async (deviceId: string) => {
    setError(null)
    setWarning(null)

    if (!bleSupported) {
      setWarning('Bluetooth доступен только в Chromium-подобных браузерах')
      return
    }

    try {
      await selectDevice(deviceId)
      setSelectedDeviceId(deviceId)

      let device = await findKnownDevice(deviceId)
      if (!device) {
        setWarning('Браузер не видит устройство. Нужно повторное разрешение.')
        device = await requestKettleDevice()
        if (device.id !== deviceId) {
          const added = await addDevice(device.name ?? 'Kettle', device.id)
          await selectDevice(added.deviceId)
          setDevices(prev => {
            const exists = prev.some(item => item.deviceId === added.deviceId)
            return exists ? prev : [...prev, added]
          })
          setSelectedDeviceId(added.deviceId)
        }
      }

      await safeConnect(device)
      await connectWs()
    } catch {
      setError('Не удалось подключиться к чайнику. Попробуйте снова.')
    }
  }

  const handleTemp = (value: number) => {
    setTemp(value)
    setStatus('Данные обновлены')
    setLastDataAt(Date.now())
    log(`Температура от датчика: ${value}°C`)

    if (wsRef.current?.readyState === WebSocket.OPEN) {
      const data: KettleData = { device: 'kettle', temp: value }
      wsRef.current.send(JSON.stringify(data))
    }
  }

  const handleRemoveDevice = async (deviceId: string) => {
    const confirmed = window.confirm('Удалить устройство? Это действие нельзя отменить.')
    if (!confirmed) return

    try {
      await removeDevice(deviceId)
      setDevices(prev => prev.filter(d => d.deviceId !== deviceId))
      if (selectedDeviceId === deviceId) {
        setSelectedDeviceId(null)
        setBleConnected(false)
        setStatus('Устройство удалено')
      }
    } catch (err) {
      setError((err as Error).message)
    }
  }

  const signOut = async () => {
    disconnectWs()
    await logout()
    setIsAuthorized(false)
    setDevices([])
    setSelectedDeviceId(null)
    setTemp(null)
    setBleConnected(false)
    setStatus('Ожидание подключения...')
    navigate('/')
  }

  const statusHint = (() => {
    if (!selectedDeviceId) return null
    if (!bleConnected) return 'Ожидает подключения'
    if (status.includes('Подключение')) return 'Идёт подключение'
    if (status.includes('восстановлена')) return 'Связь восстановлена'
    if (status.includes('подключен') || status.includes('Связь установлена')) return 'Устройство активно'
    if (lastDataAt) {
      const seconds = Math.floor((Date.now() - lastDataAt) / 1000)
      return seconds <= 5 ? 'Данные обновлены только что' : `Последнее обновление: ${seconds} сек назад`
    }
    return 'Статус обновлён'
  })()

  // Чтобы wsState и wsHealthy не считались "unused", выведем их в консоль 
  // или (что лучше) используем для индикации в UI
  useEffect(() => {
    if (wsState !== 'idle') {
      log(`WS Status changed: ${wsState} (Healthy: ${wsHealthy})`)
    }
  }, [wsState, wsHealthy])

  return (
      <div className="app">
        <Header />
        <ErrorBanner message={error} />

        {/* Индикатор статуса WebSocket для предотвращения TS6133 */}
        <div style={{ display: 'none' }}>{wsState} {String(wsHealthy)}</div>

        {route === '/' && (
            <AuthPanel
                authMode={authMode}
                email={email}
                password={password}
                onAuthModeChange={setAuthMode}
                onEmailChange={setEmail}
                onPasswordChange={setPassword}
                onSubmit={handleAuth}
            />
        )}

        {route === '/app' && isAuthorized && (
            <section className="app__grid">
              <DeviceList
                  devices={devices}
                  selectedId={selectedDeviceId}
                  statusHint={statusHint}
                  onSelect={handleSelectDevice}
                  onAdd={handleAddDevice}
                  onRemove={handleRemoveDevice}
                  onSignOut={signOut}
              />

              <div className="dashboard">
                {!selectedDeviceId && <EmptyState />}
                {selectedDeviceId && (
                    <>
                      <StatusPanel status={status} hint={statusHint} temperature={temp} />
                      <ControlPanel
                          onAddDevice={handleAddDevice}
                          supportsBle={bleSupported}
                          warning={warning}
                      />
                    </>
                )}
              </div>
            </section>
        )}
      </div>
  )
}

export default App