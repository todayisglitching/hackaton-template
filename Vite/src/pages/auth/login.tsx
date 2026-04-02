import { Button, Input, TextField, toast } from "@heroui/react";
import '../../globals.css';
import { useAuth } from '../../hooks/auth';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';

const LoginPage = () => {
    const navigate = useNavigate();
    const { login, isLoading } = useAuth();
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');

    const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
        e.preventDefault();

        try {
            const result = await login(email, password);

            if (!result.success) {
                // Показываем точную ошибку от API, если она есть
                const errorMessage = result.error?.trim();
                if (errorMessage) {
                    // Если API вернуло конкретное сообщение — показываем его
                    toast.danger(errorMessage);
                } else {
                    // Если сообщение пустое или отсутствует — используем резервное
                    toast.danger('Ошибка авторизации. Проверьте данные и попробуйте снова');
                }
            } else {
                // Успешный вход — показываем уведомление
                toast.success('Успешный вход! Добро пожаловать!');
                navigate('/dashboard');
            }
        } catch (err) {
            console.error('Login error:', err);
            // Обработка непредвиденных ошибок (сетевые сбои и т. д.)
            toast.danger('Не удалось подключиться к серверу. Проверьте интернет‑соединение.');
        }
    };

    return (
        <div className="flex items-center justify-center h-screen">
            <div className="w-full max-w-3xs flex flex-col items-center gap-2">
                <form onSubmit={handleSubmit} className="w-full flex flex-col gap-3">
                    <TextField className="w-full" name="email" type="email">
                        <label>Email</label>
                        <Input
                            placeholder="every@killalldata.ru"
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                            required
                            disabled={isLoading} // Блокируем ввод во время загрузки
                        />
                    </TextField>
                    <TextField className="w-full" name="password" type="password">
                        <label>Пароль</label>
                        <Input
                            placeholder="··········"
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            required
                            disabled={isLoading} // Блокируем ввод во время загрузки
                        />
                    </TextField>
                    <Button
                        fullWidth={true}
                        type="submit"
                        isDisabled={isLoading}
                    >
                        {isLoading ? 'Вход...' : 'Войти'}
                    </Button>
                </form>
                <Button
                    size="sm"
                    variant="ghost"
                    isDisabled={isLoading}
                    onClick={() => navigate('/auth/register')}
                >
                    Зарегистрироваться
                </Button>
            </div>
        </div>
    );
};

export default LoginPage;
