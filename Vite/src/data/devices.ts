import type { DeviceDto } from '../types';
import { readCachedDeviceState } from '../utils/device-cache';

export type UiDeviceKind = 'sensor' | 'light' | 'relay';

export type UiDevice = {
  id: string;
  deviceKey: string;
  name: string;
  room: string;
  status: 'online' | 'offline';
  kind: UiDeviceKind;
  lastValue?: number;
  source: 'api' | 'demo' | 'ble';
  manufacturer?: string;
  model?: string;
  firmwareVersion?: string;
  description?: string;
  capabilities: string[];
  quickAction?: {
    label: string;
    action: string;
  };
};

export const demoDevices: UiDevice[] = [
  {
    id: 'demo-1',
    deviceKey: 'demo-1',
    name: 'Датчик температуры',
    room: 'Зал',
    status: 'online',
    lastValue: 24,
    kind: 'sensor',
    source: 'demo',
    manufacturer: 'USmart Labs',
    model: 'TH-01',
    firmwareVersion: '0.9.1',
    description: 'Показывает температуру и влажность в комнате.',
    capabilities: ['Температура', 'История', 'Алерты'],
  },
  {
    id: 'demo-2',
    deviceKey: 'demo-2',
    name: 'Умная лампа',
    room: 'Кухня',
    status: 'offline',
    kind: 'light',
    source: 'demo',
    manufacturer: 'USmart Labs',
    model: 'Luma Mini',
    firmwareVersion: '1.2.0',
    description: 'Поддерживает быстрое включение и базовые сцены освещения.',
    capabilities: ['Вкл / Выкл', 'Яркость', 'Сцены'],
    quickAction: { label: 'Быстрое действие', action: 'Включить свет' },
  },
  {
    id: 'demo-3',
    deviceKey: 'demo-3',
    name: 'Чайник',
    room: 'Кухня',
    status: 'online',
    kind: 'relay',
    source: 'demo',
    manufacturer: 'BLE Maker',
    model: 'HeatOne',
    firmwareVersion: '2.0.4',
    description: 'BLE-устройство для демонстрации подключения и удалённого запуска.',
    capabilities: ['Реле', 'Таймер', 'Сценарии'],
    quickAction: { label: 'Быстрое действие', action: 'Вскипятить' },
  },
];

function safeJsonParse(value?: string) {
  if (!value) {
    return null;
  }

  try {
    return JSON.parse(value) as Record<string, unknown>;
  } catch {
    return null;
  }
}

export function inferDeviceKind(type?: string): UiDeviceKind {
  const normalized = (type ?? '').toLowerCase();

  if (normalized.includes('sensor') || normalized.includes('temp')) {
    return 'sensor';
  }

  if (normalized.includes('light') || normalized.includes('lamp')) {
    return 'light';
  }

  return 'relay';
}

function getCapabilities(kind: UiDeviceKind) {
  if (kind === 'sensor') {
    return ['Температура', 'Статистика', 'Уведомления'];
  }

  if (kind === 'light') {
    return ['Вкл / Выкл', 'Яркость', 'Сцены'];
  }

  return ['Реле', 'Таймер', 'Сценарии'];
}

export function mapApiDeviceToUi(device: DeviceDto): UiDevice {
  const parsedProperties = safeJsonParse(device.properties);
  const kind = inferDeviceKind(device.type);
  const cachedState = readCachedDeviceState(device.deviceId);
  const lastValue = typeof parsedProperties?.temperature === 'number'
    ? parsedProperties.temperature
    : typeof parsedProperties?.value === 'number'
      ? parsedProperties.value
      : cachedState?.temperature;

  return {
    id: String(device.id),
    deviceKey: device.deviceId,
    name: device.name,
    room: device.location || 'Без комнаты',
    status: device.status === 'online' ? 'online' : 'offline',
    kind,
    lastValue,
    source: 'api',
    manufacturer: device.manufacturer,
    model: device.model,
    firmwareVersion: device.firmwareVersion,
    description: typeof parsedProperties?.description === 'string' ? parsedProperties.description : undefined,
    capabilities: getCapabilities(kind),
    quickAction: kind === 'sensor'
      ? undefined
      : { label: 'Быстрое действие', action: kind === 'light' ? 'Включить сцену' : 'Запустить' },
  };
}

export function createBleUiDevice(device: BluetoothDevice): UiDevice {
  return {
    id: device.id,
    deviceKey: device.id,
    name: device.name || 'BLE Device',
    room: 'BLE bridge',
    status: 'online',
    kind: 'relay',
    source: 'ble',
    manufacturer: 'Web Bluetooth',
    description: 'Подключено напрямую через Chromium и Web Bluetooth API.',
    lastValue: readCachedDeviceState(device.id)?.temperature,
    capabilities: ['BLE', 'Realtime', 'Ручной запуск'],
    quickAction: { label: 'Быстрое действие', action: 'Подключено' },
  };
}

export function findDeviceByKey(devices: UiDevice[], id?: string | null) {
  if (!id) {
    return null;
  }

  return devices.find((device) => device.deviceKey === id || device.id === id) ?? null;
}
