const SERVICE_UUID = '00001234-0000-1000-8000-00805f9b34fb';
const CHAR_UUID = '00000001-0000-1000-8000-00805f9b34fb';
const DEVICE_NAME_PREFIXES = ['Fake Kettle', 'Smart Kettle', 'Demo Kettle', 'Kettle', 'USmart'];

function delay(ms: number) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

function parseTextTemperature(dataView: DataView) {
  const bytes = new Uint8Array(dataView.buffer, dataView.byteOffset, dataView.byteLength);
  const text = new TextDecoder().decode(bytes).trim();
  const match = text.match(/-?\d+(?:[.,]\d+)?/);
  if (!match) {
    return null;
  }

  const parsed = Number(match[0].replace(',', '.'));
  return Number.isFinite(parsed) ? parsed : null;
}

function parseBinaryTemperature(dataView: DataView) {
  const candidates: number[] = [];

  if (dataView.byteLength >= 1) {
    candidates.push(dataView.getUint8(0));
    candidates.push(dataView.getInt8(0));
  }

  if (dataView.byteLength >= 2) {
    candidates.push(dataView.getUint16(0, true));
    candidates.push(dataView.getInt16(0, true));
    candidates.push(dataView.getUint16(0, false));
    candidates.push(dataView.getInt16(0, false));
    candidates.push(dataView.getUint16(0, true) / 10);
    candidates.push(dataView.getInt16(0, true) / 10);
  }

  if (dataView.byteLength >= 4) {
    candidates.push(dataView.getFloat32(0, true));
    candidates.push(dataView.getFloat32(0, false));
  }

  return candidates.find((value) => Number.isFinite(value) && value > -50 && value < 200) ?? null;
}

function extractTemperature(dataView: DataView) {
  return parseTextTemperature(dataView) ?? parseBinaryTemperature(dataView);
}

export function isChromiumBrowser(): boolean {
  const ua = navigator.userAgent;
  return /Chrome|Chromium|Edg|OPR/.test(ua);
}

export function supportsBluetooth(): boolean {
  return Boolean(navigator.bluetooth) && isChromiumBrowser();
}

export async function requestKettleDevice(): Promise<BluetoothDevice> {
  const filters: BluetoothLEScanFilter[] = [{ services: [SERVICE_UUID] }];

  for (const namePrefix of DEVICE_NAME_PREFIXES) {
    filters.push({ namePrefix });
  }

  return navigator.bluetooth.requestDevice({
    filters,
    optionalServices: [SERVICE_UUID],
  });
}

export async function findKnownDevice(deviceId: string): Promise<BluetoothDevice | null> {
  if (!navigator.bluetooth?.getDevices) {
    return null;
  }

  const devices = await navigator.bluetooth.getDevices();
  return devices.find((device) => device.id === deviceId) ?? null;
}

export async function connectToDevice(
  device: BluetoothDevice,
  onTemp: (value: number) => void
): Promise<BluetoothRemoteGATTCharacteristic> {
  try {
    if (!device.gatt) {
      throw new Error('Устройство не поддерживает GATT');
    }

    const server = await device.gatt.connect();
    await delay(700);

    const service = await server.getPrimaryService(SERVICE_UUID);
    await delay(150);
    const characteristic = await service.getCharacteristic(CHAR_UUID);

    const publishValue = (dataView: DataView) => {
      const nextValue = extractTemperature(dataView);
      if (nextValue === null) {
        console.warn('[SmartHub] Не удалось распарсить температуру из BLE-пакета', new Uint8Array(dataView.buffer, dataView.byteOffset, dataView.byteLength));
        return;
      }

      onTemp(nextValue);
    };

    if (characteristic.properties.read) {
      try {
        const buffer = await characteristic.readValue();
        publishValue(buffer);
      } catch (error) {
        console.warn('[SmartHub] Прямое чтение отклонено устройством', error);
      }
    }

    if (characteristic.properties.notify || characteristic.properties.indicate) {
      await characteristic.startNotifications();
      characteristic.addEventListener('characteristicvaluechanged', (event) => {
        const target = event.target as BluetoothRemoteGATTCharacteristic | null;
        const dataView = target?.value;
        if (!dataView || dataView.byteLength < 1) {
          console.warn('[SmartHub] Получен пустой BLE-пакет');
          return;
        }

        publishValue(dataView);
      });
    }

    if (!characteristic.properties.read && !characteristic.properties.notify && !characteristic.properties.indicate) {
      throw new Error('Характеристика не поддерживает чтение и уведомления');
    }

    return characteristic;
  } catch (error) {
    console.error('[SmartHub] Ошибка BLE:', error);
    if (device.gatt?.connected) {
      device.gatt.disconnect();
    }
    throw error;
  }
}

export { SERVICE_UUID, CHAR_UUID };

export async function requestInitialTemperature(characteristic: BluetoothRemoteGATTCharacteristic): Promise<number | null> {
  if (!characteristic.properties.read) {
    return null;
  }

  try {
    const value = await characteristic.readValue();
    return extractTemperature(value);
  } catch (error) {
    console.warn('[SmartHub] Не удалось повторно запросить температуру', error);
    return null;
  }
}
