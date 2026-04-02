/* eslint-disable react-refresh/only-export-components */
import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { login as loginRequest, me, register as registerRequest, revokeSession as revokeSessionRequest } from '../api/auth';
import { clearTokens, getTokens, logout as logoutRequest, refresh as refreshRequest, setTokens } from '../api/session';
import type { MeResponse } from '../types';

type AuthResult = { success: true } | { success: false; error: string };

type AuthContextType = {
  user: MeResponse | null;
  isLoading: boolean;
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<AuthResult>;
  register: (email: string, password: string) => Promise<AuthResult>;
  logout: () => Promise<void>;
  reloadUser: () => Promise<void>;
  revokeSession: (sessionId: string) => Promise<AuthResult>;
};

const AuthContext = createContext<AuthContextType | undefined>(undefined);

function getErrorMessage(error: unknown) {
  if (error instanceof Error && error.message.trim()) {
    return error.message;
  }

  return 'Не удалось выполнить запрос. Попробуйте ещё раз.';
}

async function loadCurrentUser() {
  const profile = await me();
  localStorage.setItem('userEmail', profile.email);
  return profile;
}

export function useAuth(): AuthContextType {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }

  return context;
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<MeResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const clearAuth = useCallback(() => {
    clearTokens();
    localStorage.removeItem('userEmail');
    setUser(null);
  }, []);

  const reloadUser = useCallback(async () => {
    const profile = await loadCurrentUser();
    setUser(profile);
  }, []);

  useEffect(() => {
    let mounted = true;

    const bootstrap = async () => {
      const { accessToken, refreshToken } = getTokens();
      if (!accessToken && !refreshToken) {
        if (mounted) {
          setIsLoading(false);
        }
        return;
      }

      try {
        const profile = await loadCurrentUser();
        if (mounted) {
          setUser(profile);
        }
      } catch {
        if (!refreshToken) {
          clearAuth();
          if (mounted) {
            setIsLoading(false);
          }
          return;
        }

        try {
          const refreshed = await refreshRequest();
          setTokens(refreshed.token, refreshed.refreshToken);
          const profile = await loadCurrentUser();
          if (mounted) {
            setUser(profile);
          }
        } catch {
          clearAuth();
        }
      } finally {
        if (mounted) {
          setIsLoading(false);
        }
      }
    };

    void bootstrap();

    return () => {
      mounted = false;
    };
  }, [clearAuth]);

  const authenticate = useCallback(
    async (request: (email: string, password: string) => Promise<{ token: string; refreshToken: string }>, email: string, password: string): Promise<AuthResult> => {
      setIsLoading(true);
      try {
        const authData = await request(email, password);
        setTokens(authData.token, authData.refreshToken);
        const profile = await loadCurrentUser();
        setUser(profile);
        return { success: true };
      } catch (error) {
        return { success: false, error: getErrorMessage(error) };
      } finally {
        setIsLoading(false);
      }
    },
    []
  );

  const login = useCallback(
    async (email: string, password: string) => authenticate(loginRequest, email, password),
    [authenticate]
  );

  const register = useCallback(
    async (email: string, password: string) => authenticate(registerRequest, email, password),
    [authenticate]
  );

  const logout = useCallback(async () => {
    try {
      await logoutRequest();
    } finally {
      clearAuth();
    }
  }, [clearAuth]);

  const revokeSession = useCallback(async (sessionId: string): Promise<AuthResult> => {
    try {
      await revokeSessionRequest(sessionId);
      await reloadUser();
      return { success: true };
    } catch (error) {
      return { success: false, error: getErrorMessage(error) };
    }
  }, [reloadUser]);

  const value = useMemo<AuthContextType>(
    () => ({
      user,
      isLoading,
      isAuthenticated: Boolean(getTokens().accessToken),
      login,
      register,
      logout,
      reloadUser,
      revokeSession,
    }),
    [user, isLoading, login, logout, register, reloadUser, revokeSession]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
