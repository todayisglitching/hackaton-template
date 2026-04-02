const COOKIE_PREFIX = 'usmart_device_';
const MAX_AGE_SECONDS = 60 * 60 * 24 * 30;

type CachedDeviceState = {
  temperature?: number;
  connectedAt?: string;
  name?: string;
};

function getCookieKey(deviceKey: string) {
  return `${COOKIE_PREFIX}${encodeURIComponent(deviceKey)}`;
}

export function readCachedDeviceState(deviceKey: string): CachedDeviceState | null {
  const key = `${getCookieKey(deviceKey)}=`;
  const cookie = document.cookie
    .split('; ')
    .find((item) => item.startsWith(key));

  if (!cookie) {
    return null;
  }

  const value = cookie.slice(key.length);
  try {
    return JSON.parse(decodeURIComponent(value)) as CachedDeviceState;
  } catch {
    return null;
  }
}

export function writeCachedDeviceState(deviceKey: string, state: CachedDeviceState) {
  document.cookie = `${getCookieKey(deviceKey)}=${encodeURIComponent(JSON.stringify(state))}; Max-Age=${MAX_AGE_SECONDS}; Path=/; SameSite=Lax`;
}
