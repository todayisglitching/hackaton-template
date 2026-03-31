import { apiRequest } from './http'
import type { AuthResponse, MeResponse } from '../types'

export async function register(email: string, password: string): Promise<AuthResponse> {
  return apiRequest<AuthResponse>('/api/auth/register', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password })
  })
}

export async function login(email: string, password: string): Promise<AuthResponse> {
  return apiRequest<AuthResponse>('/api/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password })
  })
}

export async function me(): Promise<MeResponse> {
  return apiRequest<MeResponse>('/api/auth/me')
}
