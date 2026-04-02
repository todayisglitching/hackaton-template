export type AuthMode = 'login' | 'register';
export type Route = '/' | '/app';

export type AuthResponse = {
  token: string;
  refreshToken: string;
  userId: number;
};

export type ErrorResponse = {
  message?: string;
  title?: string;
  detail?: string;
};

export type SessionInfo = {
  sessionId: string;
  deviceInfo: string;
  ipAddress: string;
  createdAt: string;
  lastUsed: string;
  expiresAt: string;
  isCurrent: boolean;
};

export type MeResponse = {
  userId: number;
  email: string;
  activeSessions: SessionInfo[];
};

export type DeviceDto = {
  id: number;
  deviceId: string;
  name: string;
  type: string;
  status: string;
  properties?: string;
  location?: string;
  manufacturer?: string;
  model?: string;
  firmwareVersion?: string;
  createdAt?: string;
  updatedAt?: string;
  isEnabled?: boolean;
};

export type DeviceCreateRequest = {
  deviceId: string;
  name: string;
  type: string;
  properties?: string;
  location?: string;
  manufacturer?: string;
  model?: string;
  firmwareVersion?: string;
};

export type DeviceListResponse = {
  devices: DeviceDto[];
  selectedDeviceId?: string | null;
};
