export type AuthMode = 'login' | 'register'
export type Route = '/' | '/app'

export type AuthResponse = { token: string; refreshToken: string; userId: number }
export type ErrorResponse = { message: string }
export type MeResponse = { userId: number }

export type DeviceDto = { deviceId: string; name: string }
export type DeviceListResponse = { devices: DeviceDto[]; selectedDeviceId?: string | null }

// Типизация состояний WebSocket
export type WSStatus = 'idle' | 'connecting' | 'open' | 'closed';

// Типизация данных, отправляемых в облако
export interface KettleData {
    device: 'kettle';
    temp: number;
}

/**
 * Расширяем глобальный объект Window для поддержки Web Bluetooth,
 * если типы @types/web-bluetooth еще не подтянулись.
 */
declare global {
    interface Navigator {
        bluetooth: Bluetooth;
    }
}