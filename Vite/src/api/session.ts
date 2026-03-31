import type { AuthResponse, ErrorResponse } from '../types'

const ACCESS_KEY = 'access_token'
const REFRESH_KEY = 'refresh_token'

export function setTokens(accessToken: string, refreshToken: string) {
  localStorage.setItem(ACCESS_KEY, accessToken)
  localStorage.setItem(REFRESH_KEY, refreshToken)
}

export function clearTokens() {
  localStorage.removeItem(ACCESS_KEY)
  localStorage.removeItem(REFRESH_KEY)
}

export function getTokens() {
  return {
    accessToken: localStorage.getItem(ACCESS_KEY),
    refreshToken: localStorage.getItem(REFRESH_KEY)
  }
}

export async function refresh(): Promise<AuthResponse> {
  const { refreshToken } = getTokens()
  if (!refreshToken) throw new Error('Нет refresh token')

  const response = await fetch('/api/auth/refresh', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ refreshToken })
  })

  if (!response.ok) {
    const contentType = response.headers.get('content-type') ?? ''
    if (contentType.includes('application/json')) {
      const data = (await response.json()) as ErrorResponse
      throw new Error(data.message || `HTTP ${response.status}`)
    }
    throw new Error(`HTTP ${response.status}`)
  }

  return (await response.json()) as AuthResponse
}

export async function logout(): Promise<void> {
  const { refreshToken } = getTokens()
  clearTokens()
  if (!refreshToken) return
  await fetch('/api/auth/logout', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ refreshToken })
  })
}
