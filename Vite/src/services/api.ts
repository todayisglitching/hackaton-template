// Типы запросов
interface RegisterRequest {
    email: string;
    password: string;
}

interface LoginRequest {
    email: string;
    password: string;
}

// Типы ответов
interface AuthResponse {
    token: string;
    refreshToken: string;
    userId: number;
}

export interface MeResponse {
    userId: number;
    email: string;
    activeSessions: Array<{
        sessionId: string;
        deviceInfo: string;
        ipAddress: string;
        createdAt: string;
        lastUsed: string;
        expiresAt: string;
        isCurrent: boolean;
    }>;
}

// Базовый URL API (замените на реальный адрес вашего бэкенда)
const API_BASE_URL = '/api';

/**
 * Универсальная функция для выполнения API‑запросов
 * Любой статус, кроме 200, считается ошибкой
 */
const apiFetch = async <T>(
    endpoint: string,
    options: RequestInit = {}
): Promise<T> => {
    const response = await fetch(`${API_BASE_URL}${endpoint}`, {
        headers: {
            'Content-Type': 'application/json',
            ...options.headers,
        },
        ...options,
    });

    if (response.status !== 200) {
        try {
            const errorData = await response.json();
            if (errorData.message) {
                throw new Error(errorData.message);
            }
        } catch (jsonError) {
            // Игнорируем ошибку парсинга JSON, используем карту статусов
        }

        // Карта статусов с разными сообщениями для разных эндпоинтов
        const isRegister = endpoint.includes('/auth/register');
        const statusMessages: Record<number, string> = {
            400: isRegister
                ? 'Проверьте корректность email и пароля'
                : 'Неверные учётные данные',
            401: 'Неверный email или пароль',
            409: 'Пользователь с таким email уже существует',
            500: 'Внутренняя ошибка сервера. Попробуйте позже',
        };

        throw new Error(statusMessages[response.status] || `Ошибка сервера: код ${response.status}`);
    }

    return response.json();
};

export const apiService = {
    /**
     * Регистрация нового пользователя
     */
    register: async (email: string, password: string): Promise<AuthResponse> => {
        return apiFetch<AuthResponse>('/auth/register', {
            method: 'POST',
            body: JSON.stringify({ email, password }),
        });
    },

    /**
     * Вход пользователя в систему
     */
    login: async (email: string, password: string): Promise<AuthResponse> => {
        return apiFetch<AuthResponse>('/auth/login', {
            method: 'POST',
            body: JSON.stringify({ email, password }),
        });
    },

    /**
     * Обновление токенов с помощью refresh‑токена
     */
    refresh: async (refreshToken: string): Promise<AuthResponse> => {
        return apiFetch<AuthResponse>('/auth/refresh', {
            method: 'POST',
            body: JSON.stringify({ refreshToken }),
        });
    },

    /**
     * Выход пользователя из системы
     */
    logout: async (token: string): Promise<void> => {
        await apiFetch<void>('/auth/logout', {
            method: 'POST',
            body: JSON.stringify({ token }),
        });
    },

    /**
     * Получение информации о текущем пользователе
     */
    getCurrentUser: async (token: string): Promise<MeResponse> => {
        return apiFetch<MeResponse>('/auth/me', {
            headers: { Authorization: `Bearer ${token}` },
        });
    },

    /**
     * Отзыв конкретной сессии
     */
    revokeSession: async (token: string, sessionId: string): Promise<void> => {
        await apiFetch<void>('/auth/revoke-session', {
            method: 'POST',
            headers: { Authorization: `Bearer ${token}` },
            body: JSON.stringify({ sessionId }),
        });
    },
};
