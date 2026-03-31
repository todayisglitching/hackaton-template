export type AuthMode = 'login' | 'register'
export type Route = '/' | '/app'

export type AuthResponse = { token: string; refreshToken: string; userId: number }
export type ErrorResponse = { message: string }
export type MeResponse = { userId: number }

export type DeviceDto = { deviceId: string; name: string }
export type DeviceListResponse = { devices: DeviceDto[]; selectedDeviceId?: string | null }
