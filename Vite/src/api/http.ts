import type { ErrorResponse } from '../types'
import { getTokens, setTokens, clearTokens, refresh } from './session'

let isRefreshing = false
let pending: Array<(token: string | null) => void> = []

async function refreshAccessToken(): Promise<string | null> {
  if (isRefreshing) {
    return new Promise(resolve => pending.push(resolve))
  }

  isRefreshing = true
  try {
    const data = await refresh()
    setTokens(data.token, data.refreshToken)
    pending.forEach(resolve => resolve(data.token))
    pending = []
    return data.token
  } catch {
    clearTokens()
    pending.forEach(resolve => resolve(null))
    pending = []
    return null
  } finally {
    isRefreshing = false
  }
}

export async function apiRequest<T>(input: RequestInfo, init?: RequestInit): Promise<T> {
  if (import.meta.env.DEV) {
    console.info('[SmartHub][DEV] API request', input, init?.method ?? 'GET')
  }
  const run = async (token: string | null): Promise<Response> => {
    const headers = new Headers(init?.headers)
    if (token) headers.set('Authorization', `Bearer ${token}`)

    return fetch(input, {
      ...init,
      headers
    })
  }

  const { accessToken } = getTokens()
  let response = await run(accessToken)

  if (response.status === 401) {
    const newToken = await refreshAccessToken()
    if (newToken) {
      response = await run(newToken)
    }
  }

  if (import.meta.env.DEV) {
    console.info('[SmartHub][DEV] API response', response.status, response.url)
  }

  const contentType = response.headers.get('content-type') ?? ''

  if (!response.ok) {
    const rawText = await response.text().catch(() => '')
    try {
      const data = rawText
        ? (JSON.parse(rawText) as Partial<ErrorResponse> & { title?: string; detail?: string; Message?: string })
        : {}
      const message = data.message || data.Message || data.detail || data.title || rawText
      if (message) {
        throw new Error(message)
      }
      if (rawText) {
        throw new Error(rawText)
      }
    } catch (err) {
      if (err instanceof Error) {
        throw err
      }
    }
    throw new Error(`HTTP ${response.status}`)
  }

  if (contentType.includes('application/json')) {
    return (await response.json()) as T
  }

  return undefined as T
}
