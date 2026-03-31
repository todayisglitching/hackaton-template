const SERVICE_UUID = '00001234-0000-1000-8000-00805f9b34fb';
const CHAR_UUID = '00000001-0000-1000-8000-00805f9b34fb';

export function isChromiumBrowser(): boolean {
    const ua = navigator.userAgent;
    return /Chrome|Chromium|Edg|OPR/.test(ua);
}

export function supportsBluetooth(): boolean {
    return Boolean(navigator?.bluetooth) && isChromiumBrowser();
}

/**
 * Запрос нового устройства через системное окно браузера
 */
export async function requestKettleDevice(): Promise<BluetoothDevice> {
    return navigator.bluetooth.requestDevice({
        filters: [{ services: [SERVICE_UUID] }]
    });
}

/**
 * Поиск устройства, на которое пользователь УЖЕ давал разрешение ранее
 */
export async function findKnownDevice(deviceId: string): Promise<BluetoothDevice | null> {
    if (!navigator.bluetooth?.getDevices) return null;
    const devices = await navigator.bluetooth.getDevices();
    return devices.find(d => d.id === deviceId) ?? null;
}

/**
 * Основная логика подключения с защитой от "гонки" GATT-команд
 */
export async function connectToDevice(
    device: BluetoothDevice,
    onTemp: (value: number) => void
): Promise<BluetoothRemoteGATTCharacteristic> {
    
    try {
        console.info('[SmartHub] Инициализация GATT...');
        const server = await device.gatt?.connect();
        if (!server) throw new Error('GATT server not found');
        
        await new Promise(r => setTimeout(r, 1200)); 

        const service = await server.getPrimaryService(SERVICE_UUID);
        await new Promise(r => setTimeout(r, 200));
        const characteristic = await service.getCharacteristic(CHAR_UUID);
        
        console.info('[SmartHub] Характеристика готова. Свойства:', characteristic.properties);

        // 1. Попытка чтения (если не сработает — игнорируем)
        if (characteristic.properties.read) {
            characteristic.readValue()
                .then(buf => {
                    const val = buf.getUint8(0);
                    console.info(`[SmartHub] ReadValue Success: ${val}`);
                    onTemp(val);
                })
                .catch(() => console.warn('[SmartHub] Прямое чтение отклонено девайсом'));
        }

        // 2. Включаем уведомления
        if (characteristic.properties.notify) {
            await characteristic.startNotifications();
            
            characteristic.addEventListener('characteristicvaluechanged', (event: any) => {
                const dataView = event.target.value as DataView;
                if (dataView && dataView.byteLength > 0) {
                    const val = dataView.getUint8(0);
                    
                    // ЭТОТ ЛОГ ПОКАЖЕТ, ЧТО ПРИШЛО ФИЗИЧЕСКИ
                    console.log(`%c[BLE RECEIVE] Value: ${val}`, 'color: #00ff00; font-weight: bold');
                    
                    onTemp(val);
                } else {
                    console.warn('[SmartHub] Получен пустой пакет данных');
                }
            });
            
            console.info('[SmartHub] Подписка активна.');
        }

        return characteristic;

    } catch (error) {
        console.error('[SmartHub] Ошибка BLE:', error);
        if (device.gatt?.connected) device.gatt.disconnect();
        throw error;
    }
}
