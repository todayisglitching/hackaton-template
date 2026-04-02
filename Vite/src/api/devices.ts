import type { DeviceCreateRequest, DeviceDto, DeviceListResponse } from '../types';
import { apiRequest } from './http';

function normalizePayload(payload: Partial<DeviceCreateRequest>): DeviceCreateRequest {
  return {
    deviceId: payload.deviceId?.trim() ?? '',
    name: payload.name?.trim() ?? '',
    type: payload.type?.trim() ?? 'ble-relay',
    properties: payload.properties ?? '{}',
    location: payload.location ?? '',
    manufacturer: payload.manufacturer ?? '',
    model: payload.model ?? '',
    firmwareVersion: payload.firmwareVersion ?? '',
  };
}

export async function fetchDevices(): Promise<DeviceListResponse> {
  return apiRequest<DeviceListResponse>('/api/devices');
}

export async function addDevice(payload: Partial<DeviceCreateRequest>): Promise<DeviceDto> {
  return apiRequest<DeviceDto>('/api/devices', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(normalizePayload(payload)),
  });
}

export async function selectDevice(deviceId: string): Promise<void> {
  await apiRequest<void>('/api/devices/select', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ deviceId }),
  });
}

export async function removeDevice(deviceId: string): Promise<void> {
  await apiRequest<void>(`/api/devices/${encodeURIComponent(deviceId)}`, {
    method: 'DELETE',
  });
}
