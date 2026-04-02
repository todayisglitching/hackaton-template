import { Button, Card, Chip, Input, Slider, toast } from '@heroui/react';
import { useEffect, useMemo, useState } from 'react';
import { useLocation, useNavigate, useParams } from 'react-router-dom';
import { fetchDevices, selectDevice } from '../../api/devices';
import { demoDevices, findDeviceByKey, mapApiDeviceToUi, type UiDevice } from '../../data/devices';
import { connectToDevice, findKnownDevice, requestInitialTemperature, requestKettleDevice, supportsBluetooth } from '../../services/ble';
import { readCachedDeviceState, writeCachedDeviceState } from '../../utils/device-cache';

const icons = {
  relay: (
    <svg viewBox="0 0 24 24" className="h-6 w-6" aria-hidden="true">
      <path d="M12 3v7m4.24-5.24A7 7 0 1 1 7.76 4.76" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
    </svg>
  ),
  sensor: (
    <svg viewBox="0 0 24 24" className="h-6 w-6" aria-hidden="true">
      <path d="M12 3a3 3 0 0 0-3 3v7.2a4 4 0 1 0 6 0V6a3 3 0 0 0-3-3z" fill="none" stroke="currentColor" strokeWidth="2" />
    </svg>
  ),
  light: (
    <svg viewBox="0 0 24 24" className="h-6 w-6" aria-hidden="true">
      <path d="M9 18h6m-5 3h4M12 3a6 6 0 0 0-3 11.2V16h6v-1.8A6 6 0 0 0 12 3z" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
    </svg>
  ),
};

type DeviceLocationState = {
  device?: UiDevice;
};

function isBleCandidate(device: UiDevice) {
  return device.source === 'ble' || device.manufacturer === 'Web Bluetooth' || device.room === 'BLE bridge';
}

const DevicePanelPage = ({ mode = 'user' }: { mode?: 'demo' | 'user' }) => {
  const { id } = useParams();
  const navigate = useNavigate();
  const location = useLocation();
  const state = (location.state as DeviceLocationState | null) ?? null;
  const [remoteDevices, setRemoteDevices] = useState<UiDevice[]>([]);
  const [isLoading, setIsLoading] = useState(mode === 'user');
  const [isConnecting, setIsConnecting] = useState(false);
  const [isOn, setIsOn] = useState(false);
  const [profileMode, setProfileMode] = useState('Авто');
  const [intensity, setIntensity] = useState(65);
  const [schedule, setSchedule] = useState('07:30');
  const [liveTemperature, setLiveTemperature] = useState<number | null>(null);
  const [bleStatus, setBleStatus] = useState('BLE не подключен');

  useEffect(() => {
    let mounted = true;

    if (mode === 'demo') {
      setRemoteDevices([]);
      setIsLoading(false);
      return () => {
        mounted = false;
      };
    }

    const loadDevices = async () => {
      setIsLoading(true);
      try {
        const response = await fetchDevices();
        const mapped = response.devices.map(mapApiDeviceToUi);
        if (mounted) {
          setRemoteDevices(mapped);
        }
      } catch {
        if (mounted) {
          setRemoteDevices([]);
        }
      } finally {
        if (mounted) {
          setIsLoading(false);
        }
      }
    };

    void loadDevices();

    return () => {
      mounted = false;
    };
  }, [mode]);

  const device = useMemo(() => {
    const fromRemote = findDeviceByKey(remoteDevices, id);
    const fromDemo = findDeviceByKey(demoDevices, id);
    return fromRemote ?? state?.device ?? fromDemo ?? null;
  }, [id, remoteDevices, state?.device]);

  useEffect(() => {
    if (!device) {
      return;
    }

    const cachedState = readCachedDeviceState(device.deviceKey);
    setLiveTemperature(cachedState?.temperature ?? device.lastValue ?? null);
    setIsOn(device.status === 'online');
    setIntensity(device.kind === 'light' ? 70 : device.kind === 'relay' ? 55 : 35);
    setProfileMode(device.kind === 'sensor' ? 'Наблюдение' : 'Авто');
    setBleStatus(
      isBleCandidate(device)
        ? cachedState?.temperature !== undefined
          ? `Температура: ${cachedState.temperature}°`
          : 'Готов к автоподключению'
        : 'BLE не требуется для этого устройства'
    );

    if (mode === 'user' && device.source === 'api') {
      void selectDevice(device.deviceKey).catch(() => {
        console.info('Не удалось отметить устройство как выбранное');
      });
    }
  }, [device, mode]);

  useEffect(() => {
    if (!device || !isBleCandidate(device)) {
      return;
    }

    if (!supportsBluetooth()) {
      setBleStatus('Bluetooth доступен только в Chromium-браузерах с включённым BLE.');
      return;
    }

    let cancelled = false;
    let reconnectTimer: ReturnType<typeof setTimeout> | null = null;
    let activeBluetoothDevice: BluetoothDevice | null = null;

    const handleTemp = (value: number) => {
      if (cancelled) {
        return;
      }

      setLiveTemperature(value);
      setIsOn(true);
      setBleStatus(`Температура: ${value}°`);
      writeCachedDeviceState(device.deviceKey, {
        temperature: value,
        connectedAt: new Date().toISOString(),
        name: device.name,
      });
    };

    const scheduleReconnect = () => {
      if (cancelled) {
        return;
      }

      setIsOn(false);
      setIsConnecting(true);
      setBleStatus('Переподключаемся к BLE-устройству...');

      reconnectTimer = setTimeout(() => {
        void autoConnect(false);
      }, 1800);
    };

    const autoConnect = async (allowPrompt: boolean) => {
      setIsConnecting(true);
      setBleStatus('Подключаемся к BLE-устройству...');

      try {
        let bluetoothDevice = activeBluetoothDevice;
        if (!bluetoothDevice) {
          bluetoothDevice = await findKnownDevice(device.deviceKey);
        }

        if (!bluetoothDevice) {
          if (!allowPrompt) {
            throw new Error('BLE device unavailable for silent reconnect');
          }

          setBleStatus('Нужно подтвердить доступ к BLE-устройству...');
          bluetoothDevice = await requestKettleDevice();
        }

        activeBluetoothDevice = bluetoothDevice;

        const characteristic = await connectToDevice(bluetoothDevice, handleTemp);
        const firstValue = await requestInitialTemperature(characteristic);
        if (typeof firstValue === 'number') {
          handleTemp(firstValue);
        } else {
          setBleStatus('Устройство подключено, ждём данные от чайника...');
        }

        bluetoothDevice.removeEventListener('gattserverdisconnected', scheduleReconnect);
        bluetoothDevice.addEventListener('gattserverdisconnected', scheduleReconnect);

      } catch (error) {
        console.error(error);
        if (allowPrompt) {
          setBleStatus('Не удалось подключиться к BLE-устройству.');
        } else {
          scheduleReconnect();
          return;
        }
      } finally {
        if (!cancelled) {
          setIsConnecting(false);
        }
      }
    };

    void autoConnect(true);

    return () => {
      cancelled = true;
      if (reconnectTimer) {
        clearTimeout(reconnectTimer);
      }
      if (activeBluetoothDevice) {
        activeBluetoothDevice.removeEventListener('gattserverdisconnected', scheduleReconnect);
      }
    };
  }, [device, mode]);

  const listRoute = mode === 'demo' ? '/demo' : '/dashboard';

  if (isLoading && !device) {
    return <Card className="rt-card p-8 text-sm text-[color:var(--muted)]">Загружаем карточку устройства...</Card>;
  }

  if (!device) {
    return (
      <Card className="rt-card overflow-hidden">
        <Card.Content className="space-y-4 p-6">
          <div className="rt-kicker">Device</div>
          <h1 className="rt-display text-3xl font-semibold">Устройство не найдено</h1>
          <p className="text-sm text-[color:var(--muted)]">Возможно, оно было удалено из API или открыто по устаревшей ссылке.</p>
          <Button className="rt-btn rt-btn-primary" onClick={() => navigate(listRoute)}>
            Вернуться к списку
          </Button>
        </Card.Content>
      </Card>
    );
  }

  const metricValue = typeof liveTemperature === 'number'
    ? `${liveTemperature}${device.kind === 'sensor' || isBleCandidate(device) ? '°' : ''}`
    : device.lastValue !== undefined
      ? `${device.lastValue}${device.kind === 'sensor' ? '°' : ''}`
      : 'Нет данных';

  return (
    <div className="space-y-6">
      <Card className="rt-card overflow-hidden">
        <Card.Content className="grid gap-8 p-6 md:p-8 xl:grid-cols-[0.92fr_1.08fr] xl:items-start">
          <div className="space-y-6">
            <div className="flex flex-wrap items-center gap-3">
              <Button className="rt-btn rt-btn-secondary" onClick={() => navigate(listRoute)}>
                К устройствам
              </Button>
              <Chip className="rt-pill text-xs">
                {mode === 'demo' ? 'Открытый demo flow' : device.source === 'api' ? 'Сохранено в API' : 'Локальный BLE'}
              </Chip>
            </div>

            <div>
              <div className="text-sm text-[color:var(--muted)]">{device.room}</div>
              <h1 className="rt-display mt-2 text-4xl font-semibold">{device.name}</h1>
              <p className="mt-4 text-sm leading-relaxed text-[color:var(--muted)] md:text-base">
                {device.description ?? 'Карточка устройства в едином интерфейсе USmart.'}
              </p>
            </div>

            <Card className="rt-card-soft">
              <Card.Content className="p-6">
                <div className="flex items-center justify-center rounded-[28px] border border-[color:var(--border)] bg-white/70 p-10 text-[color:var(--foreground)]">
                  {icons[device.kind]}
                </div>
                <div className="mt-5 flex flex-wrap gap-2 text-xs">
                  {device.capabilities.map((capability) => (
                    <span key={capability} className="rt-chip">
                      {capability}
                    </span>
                  ))}
                </div>
              </Card.Content>
            </Card>
          </div>

          <div className="space-y-4">
            <div className="grid gap-4 md:grid-cols-2">
              <Card className="rt-card-soft">
                <Card.Content className="p-5">
                  <div className="text-sm text-[color:var(--muted)]">Статус</div>
                  <div className="mt-2 text-2xl font-semibold">{isOn ? 'Активно' : 'Отключено'}</div>
                </Card.Content>
              </Card>
              <Card className="rt-card-soft">
                <Card.Content className="p-5">
                  <div className="text-sm text-[color:var(--muted)]">Последнее значение</div>
                  <div className="mt-2 text-2xl font-semibold">{metricValue}</div>
                </Card.Content>
              </Card>
            </div>

            <Card className="rt-card-soft">
              <Card.Content className="space-y-5 p-5">
                <div>
                  <div className="flex items-center justify-between gap-4">
                    <div className="text-sm text-[color:var(--muted)]">BLE статус</div>
                    {isBleCandidate(device) ? <Chip className="rt-pill text-xs">{isConnecting ? 'Подключение...' : 'Auto connect'}</Chip> : null}
                  </div>
                  <div className="mt-2 text-sm text-[color:var(--muted)]">{bleStatus}</div>
                </div>

                <div>
                  <div className="text-sm text-[color:var(--muted)]">Управление</div>
                  <div className="mt-3 flex flex-wrap gap-3">
                    <Button className={`rt-btn ${isOn ? 'rt-btn-primary' : 'rt-btn-secondary'}`} onClick={() => setIsOn((prev) => !prev)}>
                      {isOn ? 'Выключить' : 'Включить'}
                    </Button>
                    <Button
                      className="rt-btn rt-btn-secondary"
                      onClick={() => {
                        toast.success(mode === 'demo' ? 'Команда показана в демо-режиме.' : 'Команда отправлена в локальный протокол.');
                      }}
                    >
                      Отправить команду
                    </Button>
                  </div>
                </div>

                <div>
                  <div className="flex items-center justify-between gap-4 text-sm">
                    <span className="text-[color:var(--muted)]">Интенсивность</span>
                    <span className="font-semibold">{intensity}%</span>
                  </div>
                  <Slider className="mt-3" minValue={0} maxValue={100} value={intensity} onChange={(value) => setIntensity(value as number)} />
                </div>

                <div>
                  <div className="text-sm text-[color:var(--muted)]">Режим работы</div>
                  <div className="mt-3 flex flex-wrap gap-2">
                    {['Авто', 'Эко', 'Ночь'].map((item) => (
                      <Button key={item} className={`rt-btn ${profileMode === item ? 'rt-btn-accent' : 'rt-btn-secondary'}`} onClick={() => setProfileMode(item)}>
                        {item}
                      </Button>
                    ))}
                  </div>
                </div>
              </Card.Content>
            </Card>

            <Card className="rt-card-soft">
              <Card.Content className="grid gap-4 p-5 md:grid-cols-2">
                <div>
                  <div className="text-sm text-[color:var(--muted)]">Расписание</div>
                  <div className="mt-3 max-w-[180px]">
                    <Input type="time" value={schedule} onChange={(event) => setSchedule(event.target.value)} />
                  </div>
                </div>
                <div className="space-y-2 text-sm text-[color:var(--muted)]">
                  <div>Производитель: <span className="text-[color:var(--foreground)]">{device.manufacturer ?? 'USmart'}</span></div>
                  <div>Модель: <span className="text-[color:var(--foreground)]">{device.model ?? 'Prototype'}</span></div>
                  <div>Прошивка: <span className="text-[color:var(--foreground)]">{device.firmwareVersion ?? 'n/a'}</span></div>
                </div>
              </Card.Content>
            </Card>
          </div>
        </Card.Content>
      </Card>
    </div>
  );
};

export default DevicePanelPage;
