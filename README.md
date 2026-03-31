# Smart Home API

ASP.NET Core Web API для управления умным домом с системой аутентификации, управления устройствами и расширенной безопасностью.

## 🚀 Возможности

- **Аутентификация и авторизация**: JWT токены с обновлением
- **Управление устройствами**: CRUD операции для устройств умного дома
- **Безопасность**: Rate limiting, валидация паролей, защита от атак
- **NoSQL база данных**: InMemory база данных для хранения данных
- **Автоматические тесты**: Комплексный набор тестов для всех функций

## 🛠️ Технологический стек

- **.NET 10.0**
- **ASP.NET Core** - Web API фреймворк
- **Entity Framework Core** - ORM с NoSQL провайдером
- **BCrypt.Net** - Хеширование паролей
- **xUnit** - Фреймворк для тестирования
- **FluentAssertions** - Удобные утверждения для тестов
- **Moq** - Фреймворк для мокирования

## 📋 Требования

- .NET 10.0 SDK
- Visual Studio 2022 или JetBrains Rider
- Git

## 🚀 Быстрый старт

### 1. Клонирование репозитория

```bash
git clone <repository-url>
cd hackaton-template
```

### 2. Запуск приложения

```bash
# Восстановление зависимостей
dotnet restore

# Запуск приложения
dotnet run --project testASP
```

Приложение будет доступно по адресу: `https://localhost:7000`

### 3. Запуск тестов

```bash
# Запуск всех тестов
dotnet test testASP.Tests

# Запуск конкретного теста
dotnet test testASP.Tests --filter "FullyQualifiedName~Register_ValidUser_ShouldWork"
```

## 📚 Документация API

### Аутентификация

#### Регистрация пользователя
```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecureP@ssw0rd!"
}
```

#### Вход в систему
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecureP@ssw0rd!"
}
```

#### Обновление токена
```http
POST /api/auth/refresh
Content-Type: application/json

{
  "refreshToken": "your-refresh-token"
}
```

#### Выход из системы
```http
POST /api/auth/logout
Content-Type: application/json

{
  "refreshToken": "your-refresh-token"
}
```

#### Получение информации о пользователе
```http
GET /api/auth/me
Authorization: Bearer your-jwt-token
```

### Управление устройствами

#### Получение списка устройств
```http
GET /api/devices
Authorization: Bearer your-jwt-token
```

#### Добавление устройства
```http
POST /api/devices
Authorization: Bearer your-jwt-token
Content-Type: application/json

{
  "name": "Smart Light",
  "deviceId": "light-001"
}
```

#### Выбор устройства
```http
POST /api/devices/select
Authorization: Bearer your-jwt-token
Content-Type: application/json

{
  "deviceId": "light-001"
}
```

#### Удаление устройства
```http
DELETE /api/devices/{deviceId}
Authorization: Bearer your-jwt-token
```

### Безопасность

#### Получение статистики безопасности
```http
GET /api/security/statistics
Authorization: Bearer your-jwt-token
```

#### Генерация пароля
```http
GET /api/security/generate-password?length=16
Authorization: Bearer your-jwt-token
```

#### Валидация пароля
```http
POST /api/security/validate-password
Authorization: Bearer your-jwt-token
Content-Type: application/json

{
  "password": "TestPassword123!"
}
```

#### Отзыв всех токенов
```http
POST /api/security/revoke-all
Authorization: Bearer your-jwt-token
```

#### Информация о токене
```http
GET /api/security/token-info
Authorization: Bearer your-jwt-token
```

## 🧪 Тестирование

Проект содержит комплексный набор автоматических тестов:

### Статус тестов
- ✅ **31 тест проходит**
- ⚠️ **17 тестов требуют доработки**

### Категории тестов

#### Аутентификация
- Регистрация пользователя
- Вход в систему
- Обновление токена
- Выход из системы
- Получение информации о пользователе

#### Управление устройствами
- CRUD операции с устройствами
- Изоляция данных пользователей
- Авторизация доступа

#### Безопасность
- Rate limiting
- Валидация паролей
- Генерация паролей
- Управление токенами

#### Интеграционные тесты
- Полный workflow пользователя
- Многопользовательская изоляция
- Комплексные сценарии

### Запуск тестов в Rider

1. Откройте решение в Rider
2. Нажмите правой кнопкой мыши на проект `testASP.Tests`
3. Выберите "Run All Tests"
4. Или запустите отдельные тесты через Test Explorer

## 🔧 Конфигурация

### Переменные окружения

- `ASPNETCORE_ENVIRONMENT` - окружение (Development/Production)
- `JWT_SECRET` - секрет для JWT токенов

### Настройки безопасности

- **Rate Limiting**: 100 запросов в минуту
- **Сложность пароля**: минимум 3 балла
- **Длина пароля**: 8-128 символов
- **Блокировка IP**: 50 нарушений → 15 минут блокировка

## 📁 Структура проекта

```
hackaton-template/
├── testASP/                 # Основной проект API
│   ├── Controllers/         # Контроллеры API
│   ├── Models/              # Модели данных
│   ├── Services/            # Бизнес-логика
│   ├── Infrastructure/      # Инфраструктура
│   └── NoSqlDb/            # NoSQL контекст и модели
├── testASP.Tests/           # Проект тестов
│   ├── Auth/               # Тесты аутентификации
│   ├── Devices/            # Тесты устройств
│   ├── Security/           # Тесты безопасности
│   ├── Integration/        # Интеграционные тесты
│   └── Helpers/            # Вспомогательные классы
├── .github/workflows/      # CI/CD конфигурация
└── README.md               # Документация
```

## 🚀 Разработка

### Добавление новых тестов

1. Создайте новый тестовый класс в соответствующей папке
2. Унаследуйте от `IClassFixture<WebApplicationFactory<Program>>`
3. Используйте `CreateTestClient()` для создания HTTP клиента
4. Используйте `RegisterUser()` для аутентификации

Пример:
```csharp
public class MyTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public MyTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MyTest_ShouldWork()
    {
        var client = CreateTestClient();
        var authResponse = await RegisterUser(client);
        
        // Ваш тестовый код
    }
}
```

### CI/CD

Проект настроен на автоматический запуск тестов через GitHub Actions:

- Сборка проекта
- Запуск тестов
- Генерация отчета о покрытии кода
- Сканирование безопасности
- Нагрузочное тестирование

## 🤝 Вклад в проект

1. Fork проекта
2. Создайте feature branch
3. Внесите изменения
4. Добавьте тесты
5. Отправьте pull request

## 📄 Лицензия

MIT License

## 🆘 Поддержка

Если у вас есть вопросы или проблемы:

1. Проверьте существующие issues
2. Создайте новый issue с подробным описанием
3. Укажите шаги для воспроизведения проблемы

---

**Разработано с ❤️ для хакатона**
