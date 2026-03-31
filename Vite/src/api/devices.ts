import { apiRequest } from './http'
import type { DeviceDto, DeviceListResponse } from '../types'

export async function fetchDevices(): Promise<DeviceListResponse> {
  return apiRequest<DeviceListResponse>('/api/devices')
}

export async function addDevice(name: string, deviceId: string): Promise<DeviceDto> {
  return apiRequest<DeviceDto>('/api/devices', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name, deviceId })
  })
}

export async function selectDevice(deviceId: string): Promise<void> {
  await apiRequest<void>('/api/devices/select', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ deviceId })
  })
}

export async function removeDevice(deviceId: string): Promise<void> {
  await apiRequest<void>(`/api/devices/${encodeURIComponent(deviceId)}`, {
    method: 'DELETE'
  })
}
