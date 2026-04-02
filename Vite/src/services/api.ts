import type { MeResponse } from '../types';
import { login, me, register, revokeSession } from '../api/auth';
import { logout, refresh } from '../api/session';

export const apiService = {
  register,
  login,
  refresh,
  logout,
  getCurrentUser: (): Promise<MeResponse> => me(),
  revokeSession,
};

export type { MeResponse };
