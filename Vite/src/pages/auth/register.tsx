import { Button, Checkbox, CheckboxGroup, Description, Input, Label, TextField, Link, toast } from "@heroui/react";
import { useAuth } from '../../hooks/auth';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';

const RegisterPage = () => {
    const navigate = useNavigate();
    const { register, isLoading } = useAuth();
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [acceptTerms, setAcceptTerms] = useState(false);
    const [acceptPrivacy, setAcceptPrivacy] = useState(false);
    const [acceptMarketing, setAcceptMarketing] = useState(false);

    // Функции валидации
    const validateEmail = (email: string): string | null => {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!email) return 'Email обязателен для заполнения';
        if (!emailRegex.test(email)) return 'Введите корректный email-адрес';
        return null;
    };

    const validatePassword = (password: string): string | null => {
        if (!password) return 'Пароль обязателен для заполнения';
        if (password.length < 6) return 'Пароль должен содержать минимум 6 символов';
        if (!/[A-Z]/.test(password)) return 'Пароль должен содержать хотя бы одну заглавную букву';
        if (!/[a-z]/.test(password)) return 'Пароль должен содержать хотя бы одну строчную букву';
        if (!/\d/.test(password)) return 'Пароль должен содержать хотя бы одну цифру';
        return null;
    };

    const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
        e.preventDefault();

        // Валидация email
        const emailError = validateEmail(email);
        if (emailError) {
            toast.warning(emailError);
            return;
        }

        // Валидация пароля
        const passwordError = validatePassword(password);
        if (passwordError) {
            toast.warning(passwordError);
            return;
        }

        // Проверка принятия обязательных условий
        if (!acceptTerms || !acceptPrivacy) {
            toast.warning('Необходимо принять условия использования и политику приватности');
            return;
        }

        try {
            const result = await register(email, password);

            if (!result.success) {
                // Показываем любую ошибку от API
                toast.danger(result.error || 'Произошла ошибка при регистрации');
            } else {
                // Успешная регистрация — показываем успех
                toast.success('Регистрация успешна! Добро пожаловать!');
                navigate('/dashboard');
            }
        } catch (err) {
            // Обработка непредвиденных ошибок (например, сетевой сбой)
            toast.danger('Не удалось подключиться к серверу. Проверьте интернет-соединение.');
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
                            disabled={isLoading}
                        />
                    </TextField>

                    <TextField className="w-full" name="password" type="password">
                        <label>Пароль</label>
                        <Input
                            placeholder="··········"
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            required
                            disabled={isLoading}
                        />
                    </TextField>

                    <CheckboxGroup>
                        <Description>Я прочитал(-а) и согласен(-на) с</Description>
                        <Checkbox
                            value="on"
                            isSelected={acceptTerms}
                            onChange={() => setAcceptTerms(!acceptTerms)}
                        >
                            <Checkbox.Control>
                                <Checkbox.Indicator />
                            </Checkbox.Control>
                            <Checkbox.Content>
                                <Label>Условиями использования</Label>
                                <Link href="/tos">
                                    Прочитать
                                    <Link.Icon />
                                </Link>
                            </Checkbox.Content>
                        </Checkbox>
                        <Checkbox
                            value="pp"
                            isSelected={acceptPrivacy}
                            onChange={() => setAcceptPrivacy(!acceptPrivacy)}
                        >
                            <Checkbox.Control>
                                <Checkbox.Indicator />
                            </Checkbox.Control>
                            <Checkbox.Content>
                                <Label>Политикой приватности</Label>
                                <Link href="/pp">
                                    Прочитать
                                    <Link.Icon />
                                </Link>
                            </Checkbox.Content>
                        </Checkbox>
                        <Checkbox
                            value="market"
                            isSelected={acceptMarketing}
                            onChange={() => setAcceptMarketing(!acceptMarketing)}
                        >
                            <Checkbox.Control>
                                <Checkbox.Indicator />
                            </Checkbox.Control>
                            <Checkbox.Content>
                                <Label>Маркетинговыми рассылками</Label>
                                <Description>Рассылки от ПАО "Ростелеком" и наших партнёров</Description>
                            </Checkbox.Content>
                        </Checkbox>
                    </CheckboxGroup>

                    <Button
                        fullWidth={true}
                        type="submit"
                        isDisabled={isLoading || !acceptTerms || !acceptPrivacy}
                    >
                        {isLoading ? 'Регистрация...' : 'Зарегистрироваться'}
                    </Button>
                </form>
                <Button size="sm" variant="ghost" isDisabled={isLoading} onClick={() => navigate("/auth/login")}>
                    Вернуться обратно
                </Button>
            </div>
        </div>
    );
};

export default RegisterPage;