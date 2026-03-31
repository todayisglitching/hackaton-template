# Smart Home Codebase Documentation

## Architecture Overview

Smart Home API построен на современной .NET архитектуре с четким разделением ответственности и использованием современных практик.

## Project Structure

```
hackaton-template/
├── Controllers/           # API контроллеры
├── Services/             # Бизнес-логика
├── Models/               # Модели данных
├── Infrastructure/        # Middleware и基础设施
├── Configuration/        # Конфигурация приложения
├── NoSqlDb/             # NoSQL система
├── Vite/                # Frontend (React + Vite)
└── Tests/               # Unit и интеграционные тесты
```

## Core Components

### 1. Controllers (`/Controllers/`)

#### AuthController
- **Роль**: Аутентификация и авторизация пользователей
- **Эндпоинты**: `/api/auth/*`
- **Зависимости**: `IAuthService`
- **Особенности**: JWT токены, refresh токены, валидация паролей

#### DeviceController  
- **Роль**: Управление устройствами умного дома
- **Эндпоинты**: `/api/devices/*`
- **Зависимости**: `IDeviceService`
- **Особенности**: CRUD операции, выбор активного устройства

#### SecurityController
- **Роль**: Безопасность и управление токенами
- **Эндпоинты**: `/api/security/*`
- **Зависимости**: `EnhancedJwtTokenService`, `EnhancedPasswordService`
- **Особенности**: Статистика, отзыва токенов, генерация паролей

#### WsController
- **Роль**: WebSocket соединения
- **Эндпоинты**: `/ws`
- **Зависимости**: `WsConnectionManager`
- **Особенности**: Real-time обновления устройств

### 2. Services (`/Services/`)

#### Modern Services (Рефакторинг)
- **ModernAuthService**: Упрощенная аутентификация с BCrypt
- **ModernUserStore**: Современное хранилище пользователей
- **ModernPasswordService**: Единый сервис паролей

#### Legacy Services (для совместимости)
- **EnhancedAuthService**: Полнофункциональная аутентификация
- **UserStore**: Базовое хранилище пользователей
- **EnhancedPasswordService**: Расширенная валидация паролей

#### Token Services
- **EnhancedJwtTokenService**: JWT токены с продвинутой безопасностью
- **RefreshTokenStore**: Хранилище refresh токенов

#### Business Services
- **DeviceService**: Логика управления устройствами
- **NoSqlService**: NoSQL операции

### 3. Models (`/Models/`)

#### AuthModels
```csharp
public record RegisterRequest(string Email, string Password);
public record LoginRequest(string Email, string Password);
public record AuthResponse(int UserId, string Token, string RefreshToken);
```

#### DeviceModels
```csharp
public record DeviceCreateRequest(string DeviceId, string Name, string Type);
public record DeviceSelectRequest(string DeviceId);
public record DeviceDto(/* device properties */);
```

#### Core Models
```csharp
public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
}

public class Device
{
    public int Id { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = "offline";
    // ... другие свойства
}
```

### 4. Infrastructure (`/Infrastructure/`)

#### SecurityMiddleware
- **Роль**: Rate limiting, User-Agent валидация, IP блокировка
- **Конфигурация**: `SecuritySettings` в appsettings.json
- **Особенности**: Защита от DDoS, ботов

#### ExceptionHandlingMiddleware
- **Роль**: Глобальная обработка исключений
- **Особенности**: Логирование, стандартизация ошибок

#### WsConnectionManager
- **Роль**: Управление WebSocket соединениями
- **Особенности**: Broadcast сообщений, управление подписками

### 5. Configuration (`/Configuration/`)

#### ServiceCollectionExtensions
- **Роль**: Регистрация DI сервисов
- **Особенности**: Модульная конфигурация

#### ApplicationBuilderExtensions
- **Роль**: Конфигурация middleware pipeline
- **Особенности**: Правильный порядок middleware

#### AppSettings
```csharp
public class SecuritySettings
{
    public int MaxRequestsPerWindow { get; set; } = 100;
    public int RateLimitWindowMinutes { get; set; } = 1;
    public int MaxViolationsBeforeBlock { get; set; } = 50;
}

public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; } = 5;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
```

## NoSQL System (`/NoSqlDb/`)

### Architecture
- **DbContext**: Entity Framework Core с SQLite
- **Dynamic Collections**: Гибкая схема данных
- **System Collections**: Предопределенные коллекции

### Core Models
```csharp
public class DynamicCollection
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Schema { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}

public class DynamicDocument
{
    public int Id { get; set; }
    public int CollectionId { get; set; }
    public string Data { get; set; } = string.Empty; // JSON
}
```

### System Collections
- **device_logs**: Логи устройств
- **security_events**: События безопасности

## Security Implementation

### Password Hashing
- **Algorithm**: BCrypt с work factor 12
- **Salt**: Автоматическая генерация BCrypt
- **Validation**: Сложность, личная информация, паттерны

### JWT Tokens
- **Algorithm**: HS256
- **Expiration**: 5 минут (access), 7 дней (refresh)
- **Claims**: ID, JTI, issued/expiry dates

### Rate Limiting
- **Limit**: 100 запросов/минуту
- **Block**: 15 минут после 50 нарушений
- **Per IP**: Уникальные лимиты на IP адрес

## Testing Strategy

### Test Structure
```
testASP.Tests/
├── Auth/                 # Тесты аутентификации
├── Devices/              # Тесты устройств
├── Security/             # Тесты безопасности
├── Integration/          # Интеграционные тесты
├── Helpers/              # Test utilities
└── BasicTests.cs        # Базовые тесты
```

### Test Patterns
- **WebApplicationFactory**: In-memory тестовый сервер
- **InMemory Database**: Изолированная база данных
- **Mock Services**: Unit тестирование бизнес-логики

### Key Test Files
- **ComprehensiveTests.cs**: Полнопоточные сценарии
- **AuthControllerTests.cs**: Тесты аутентификации
- **SecurityControllerTests.cs**: Тесты безопасности

## Frontend Integration

### Vite Configuration
```typescript
export default defineConfig({
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        secure: false,
        headers: {
          'User-Agent': 'Mozilla/5.0...'
        }
      }
    }
  }
})
```

### API Client
```typescript
// src/api/http.ts
export async function apiRequest<T>(input: RequestInfo, init?: RequestInit): Promise<T>
```

## Configuration Files

### appsettings.json
```json
{
  "Jwt": {
    "Secret": "Rostelecom_SmartHome_2026_Ultra_Secret",
    "AccessTokenExpirationMinutes": 5,
    "RefreshTokenExpirationDays": 7
  },
  "Security": {
    "MaxRequestsPerWindow": 100,
    "MaxViolationsBeforeBlock": 50
  },
  "Database": {
    "ConnectionString": "Data Source=nocode.db",
    "Provider": "sqlite"
  }
}
```

### Program.cs
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();
app.ConfigureApplication(builder.Configuration);
app.Run();
```

## Modern vs Legacy

### Modern Services (Рекомендуется)
- **ModernAuthService**: Упрощенный, чистый код
- **ModernUserStore**: Современные C# фичи
- **ModernPasswordService**: Единая ответственность

### Legacy Services (Совместимость)
- **EnhancedAuthService**: Полный функционал
- **UserStore**: Базовая реализация
- **EnhancedPasswordService**: Расширенная валидация

## Best Practices

### Code Style
- **C# 12**: Использование современных фич
- **Records**: Immutable модели
- **Null Safety**: ArgumentNullException.ThrowIfNullOrWhiteSpace
- **Dependency Injection**: Constructor injection

### Security
- **BCrypt**: Для хеширования паролей
- **JWT**: Для аутентификации
- **Rate Limiting**: Защита от DDoS
- **Input Validation**: Всегда валидировать входные данные

### Performance
- **ConcurrentDictionary**: Thread-safe хранилища
- **Async/Await**: Асинхронные операции
- **Connection Pooling**: EF Core оптимизации

### Testing
- **Arrange-Act-Assert**: Структура тестов
- **InMemory Database**: Быстрые тесты
- **Mock Objects**: Изоляция зависимостей

## Migration Guide

### From Legacy to Modern
1. Заменить `EnhancedAuthService` на `ModernAuthService`
2. Обновить DI регистрацию в `ServiceCollectionExtensions`
3. Адаптировать тесты под новые сервисы
4. Обновить документацию

### Breaking Changes
- **Constructor signatures**: Новые параметры
- **Method names**: Упрощенные имена
- **Error handling**: Улучшенные сообщения об ошибках

## Development Workflow

### 1. Setup
```bash
dotnet restore
dotnet build
dotnet run --project testASP
```

### 2. Testing
```bash
dotnet test testASP.Tests
```

### 3. Frontend
```bash
cd Vite
npm install
npm run dev
```

## Monitoring & Logging

### Logging Levels
- **Information**: Основные операции
- **Warning**: Ненормальные ситуации
- **Error**: Исключения и ошибки
- **Debug**: Детальная отладка

### Key Logs
- User registration/login
- Device operations
- Security events
- Performance metrics

## Performance Considerations

### Memory
- **ConcurrentDictionary**: Оптимально для concurrent access
- **String pooling**: Для часто используемых строк
- **Object pooling**: Для тяжелых объектов

### Database
- **SQLite**: Встроенная база данных
- **Connection pooling**: Автоматическое управление
- **Batch operations**: Для массовых операций

### Caching
- **In-memory**: Для статических данных
- **Token caching**: Для JWT валидации
- **Rate limiting**: Per IP кэширование

## Future Enhancements

### Planned Features
- **PostgreSQL**: Для production
- **Redis**: Для distributed caching
- **Message Queue**: Для async processing
- **GraphQL**: Для flexible API

### Architecture Evolution
- **Microservices**: Разделение на сервисы
- **Event Sourcing**: Для audit trails
- **CQRS**: Разделение read/write операций
- **Docker**: Containerization
