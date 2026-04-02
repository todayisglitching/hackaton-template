import type { AuthResponse, ErrorResponse } from '../types';

const ACCESS_KEY = 'token';
const REFRESH_KEY = 'refreshToken';
const LEGACY_ACCESS_KEY = 'access_token';
const LEGACY_REFRESH_KEY = 'refresh_token';

export function setTokens(accessToken: string, refreshToken: string) {
  localStorage.setItem(ACCESS_KEY, accessToken);
  localStorage.setItem(REFRESH_KEY, refreshToken);
  localStorage.removeItem(LEGACY_ACCESS_KEY);
  localStorage.removeItem(LEGACY_REFRESH_KEY);
}

export function clearTokens() {
  localStorage.removeItem(ACCESS_KEY);
  localStorage.removeItem(REFRESH_KEY);
  localStorage.removeItem(LEGACY_ACCESS_KEY);
  localStorage.removeItem(LEGACY_REFRESH_KEY);
}

export function getTokens() {
  return {
    accessToken: localStorage.getItem(ACCESS_KEY) ?? localStorage.getItem(LEGACY_ACCESS_KEY),
    refreshToken: localStorage.getItem(REFRESH_KEY) ?? localStorage.getItem(LEGACY_REFRESH_KEY),
  };
}

function extractErrorMessage(data: ErrorResponse | null, fallback: string) {
  return data?.message || data?.detail || data?.title || fallback;
}

export async function refresh(): Promise<AuthResponse> {
  const { refreshToken } = getTokens();
  if (!refreshToken) {
    throw new Error('Нет refresh token');
  }

  const response = await fetch('/api/auth/refresh', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ refreshToken }),
  });

  if (!response.ok) {
    const contentType = response.headers.get('content-type') ?? '';
    const data = contentType.includes('application/json')
      ? ((await response.json()) as ErrorResponse)
      : null;

    throw new Error(extractErrorMessage(data, `HTTP ${response.status}`));
  }

  return (await response.json()) as AuthResponse;
}

export async function logout(): Promise<void> {
  const { refreshToken } = getTokens();
  clearTokens();

  if (!refreshToken) {
    return;
  }

  await fetch('/api/auth/logout', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ refreshToken }),
  });
}
