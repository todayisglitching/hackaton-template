import { useState, useContext, createContext } from 'react';
import type { ReactNode } from 'react';
import { apiService } from '../services/api';
import type { MeResponse } from '../services/api';

// Типы данных
interface User extends MeResponse {}

interface AuthContextType {
    user: User | null;
    token: string | null;
    isLoading: boolean;
    login: (email: string, password: string) => Promise<{ success: boolean; error?: string }>;
    register: (email: string, password: string) => Promise<{ success: boolean; error?: string }>;
    logout: () => void;
    refreshToken: () => Promise<boolean>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const useAuth = (): AuthContextType => {
    const context = useContext(AuthContext);
    if (context === undefined) {
        throw new Error('useAuth must be used within an AuthProvider');
    }
    return context;
};

export const AuthProvider = ({ children }: { children: ReactNode }) => {
    const [user, setUser] = useState<User | null>(null);
    const [token, setToken] = useState<string | null>(localStorage.getItem('token'));
    const [isLoading, setIsLoading] = useState<boolean>(false);

    const login = async (email: string, password: string): Promise<{ success: boolean; error?: string }> => {
        setIsLoading(true);
        try {
            const authData = await apiService.login(email, password);
            setToken(authData.token);
            localStorage.setItem('token', authData.token);
            const userData = await apiService.getCurrentUser(authData.token);
            setUser(userData);
            return { success: true };
        } catch (error: any) {
            let userFriendlyError = error.message;

            if (
                error.message.includes('HTTP error') ||
                error.message.includes('status:') ||
                error.message.includes('Ошибка сервера: код')
            ) {
                userFriendlyError = 'Ошибка при входе. Проверьте данные и попробуйте снова';
            }

            return {
                success: false,
                error: userFriendlyError,
            };
        } finally {
            setIsLoading(false);
        }
    };

    const register = async (email: string, password: string): Promise<{ success: boolean; error?: string }> => {
        setIsLoading(true);
        try {
            const authData = await apiService.register(email, password);
            setToken(authData.token);
            localStorage.setItem('token', authData.token);
            const userData = await apiService.getCurrentUser(authData.token);
            setUser(userData);
            return { success: true };
        } catch (error: any) {
            let userFriendlyError = error.message;

            if (
                error.message.includes('HTTP error') ||
                error.message.includes('status:') ||
                error.message.includes('Ошибка сервера: код')
            ) {
                userFriendlyError = 'Произошла ошибка при регистрации. Проверьте данные и попробуйте снова';
            }

            return {
                success: false,
                error: userFriendlyError,
            };
        } finally {
            setIsLoading(false);
        }
    };

    const logout = (): void => {
        if (token) {
            apiService.logout(token).catch(console.error);
        }
        setToken(null);
        setUser(null);
        localStorage.removeItem('token');
    };

    const refreshToken = async (): Promise<boolean> => {
        const refreshToken = localStorage.getItem('refreshToken');
        if (!refreshToken) return false;

        try {
            const data = await apiService.refresh(refreshToken);
            setToken(data.token);
            localStorage.setItem('token', data.token);
            localStorage.setItem('refreshToken', data.refreshToken);
            return true;
        } catch (error) {
            logout();
            return false;
        }
    };

    const value: AuthContextType = {
        user,
        token,
        isLoading,
        login,
        register,
        logout,
        refreshToken,
    };

    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};
