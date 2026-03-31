# Smart Home API Documentation

## Overview

Smart Home API - современный RESTful API для управления умным домом с JWT аутентификацией и NoSQL хранилищем.

**Base URL**: `http://localhost:5000/api`  
**Authentication**: Bearer Token (JWT)  
**Content-Type**: `application/json`

## Authentication

### Регистрация пользователя

```http
POST /api/auth/register
```

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "MyStr0ng#Pass!"
}
```

**Success Response (201):**
```json
{
  "userId": 1,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "dGhpcy1pcy1yZWZyZXNoLXRva2Vu"
}
```

**Error Responses:**

| Status | Error | Description |
|--------|-------|-------------|
| 400 | `Пароль не соответствует требованиям: [error messages]` | Пароль слишком простой или содержит личную информацию |
| 409 | `Пользователь с указанным email уже существует` | Email уже зарегистрирован |
| 429 | `Too Many Requests` | Rate limiting превышен |
| 500 | `Internal Server Error` | Ошибка сервера |

---

### Вход пользователя

```http
POST /api/auth/login
```

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "MyStr0ng#Pass!"
}
```

**Success Response (200):**
```json
{
  "userId": 1,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "dGhpcy1pcy1yZWZyZXNoLXRva2Vu"
}
```

**Error Responses:**

| Status | Error | Description |
|--------|-------|-------------|
| 401 | `Неверный email или пароль` | Пользователь не найден или пароль неверный |
| 429 | `Too Many Requests` | Rate limiting превышен |
| 500 | `Internal Server Error` | Ошибка сервера |

---

### Обновление токена

```http
POST /api/auth/refresh
```

**Request Body:**
```json
{
  "refreshToken": "dGhpcy1pcy1yZWZyZXNoLXRva2Vu"
}
```

**Success Response (200):**
```json
{
  "userId": 1,
  "token": "new-jwt-access-token",
  "refreshToken": "new-refresh-token"
}
```

**Error Responses:**

| Status | Error | Description |
|--------|-------|-------------|
| 401 | `Refresh токен недействителен` | Токен истек или неверный |
| 401 | `Пользователь не найден` | Пользователь удален |
| 500 | `Internal Server Error` | Ошибка сервера |

---

### Выход пользователя

```http
POST /api/auth/logout
```

**Request Body:**
```json
{
  "refreshToken": "dGhpcy1pcy1yZWZyZXNoLXRva2Vu"
}
```

**Success Response (200):**
```json
{}
```

**Error Responses:**

| Status | Error | Description |
|--------|-------|-------------|
| 500 | `Internal Server Error` | Ошибка сервера |

---

### Получение информации о пользователе

```http
GET /api/auth/me
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Success Response (200):**
```json
{
  "id": 1,
  "email": "user@example.com"
}
```

**Error Responses:**

| Status | Error | Description |
|--------|-------|-------------|
| 401 | `Пользователь не аутентифицирован` | JWT токен отсутствует или неверный |
| 401 | `Пользователь не найден` | Пользователь удален |
| 500 | `Internal Server Error` | Ошибка сервера |

## Devices

### Получение списка устройств

```http
GET /api/devices
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Success Response (200):**
```json
{
  "devices": [
    {
      "id": 1,
      "deviceId": "device-123",
      "name": "Smart Light",
      "type": "light",
      "status": "online",
      "properties": "{}",
      "location": "Living Room",
      "manufacturer": "SmartHome Inc",
      "model": "SH-L1000",
      "firmwareVersion": "1.2.3",
      "createdAt": "2024-01-01T00:00:00Z",
      "updatedAt": "2024-01-01T00:00:00Z",
      "isEnabled": true
    }
  ]
}
```

**Error Responses:**

| Status | Error | Description |
|--------|-------|-------------|
| 401 | `Unauthorized` | Требуется аутентификация |
| 500 | `Internal Server Error` | Ошибка сервера |

---

### Добавление устройства

```http
POST /api/devices
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Request Body:**
```json
{
  "deviceId": "device-123",
  "name": "Smart Light",
  "type": "light",
  "properties": "{}",
  "location": "Living Room",
  "manufacturer": "SmartHome Inc",
  "model": "SH-L1000",
  "firmwareVersion": "1.2.3"
}
```

**Success Response (201):**
```json
{
  "id": 1,
  "deviceId": "device-123",
  "name": "Smart Light",
  "type": "light",
  "status": "offline",
  "properties": "{}",
  "location": "Living Room",
  "manufacturer": "SmartHome Inc",
  "model": "SH-L1000",
  "firmwareVersion": "1.2.3",
  "createdAt": "2024-01-01T00:00:00Z",
  "updatedAt": "2024-01-01T00:00:00Z",
  "isEnabled": true
}
```

**Error Responses:**

| Status | Error | Description |
|--------|-------|-------------|
| 400 | `Validation failed` | Неверные данные устройства |
| 401 | `Unauthorized` | Требуется аутентификация |
| 409 | `Device with this ID already exists` | Устройство с таким ID уже существует |
| 500 | `Internal Server Error` | Ошибка сервера |

---

### Удаление устройства

```http
DELETE /api/devices/{deviceId}
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Success Response (200):**
```json
{}
```

**Error Responses:**

| Status | Error | Description |
|--------|-------|-------------|
| 401 | `Unauthorized` | Требуется аутентификация |
| 404 | `Device not found` | Устройство не найдено |
| 500 | `Internal Server Error` | Ошибка сервера |

---

### Выбор устройства

```http
POST /api/devices/select
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Request Body:**
```json
{
  "deviceId": "device-123"
}
```

**Success Response (200):**
```json
{
  "id": 1,
  "deviceId": "device-123",
  "name": "Smart Light",
  "type": "light",
  "status": "online",
  "properties": "{}",
  "location": "Living Room",
  "manufacturer": "SmartHome Inc",
  "model": "SH-L1000",
  "firmwareVersion": "1.2.3",
  "createdAt": "2024-01-01T00:00:00Z",
  "updatedAt": "2024-01-01T00:00:00Z",
  "isEnabled": true
}
```

**Error Responses:**

| Status | Error | Description |
|--------|-------|-------------|
| 400 | `Validation failed` | Неверный deviceId |
| 401 | `Unauthorized` | Требуется аутентификация |
| 404 | `Device not found` | Устройство не найдено |
| 500 | `Internal Server Error` | Ошибка сервера |

## Security

### Получение статистики безопасности

```http
GET /api/security/statistics
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Success Response (200):**
```json
{
  "totalActiveTokens": 5,
  "tokensExpiringSoon": 1,
  "tokensNotUsedRecently": 2
}
```

**Error Responses:**

| Status | Error | Description |
|--------|-------|-------------|
| 401 | `Unauthorized` | Требуется аутентификация |
| 500 | `Internal Server Error` | Ошибка сервера |

---

### Отзыв всех токенов пользователя

```http
POST /api/security/revoke-all
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Success Response (200):**
```json
{
  "revokedCount": 3
}
```

**Error Responses:**

| Status | Error | Description |
|--------|-------|-------------|
| 401 | `Unauthorized` | Требуется аутентификация |
| 500 | `Internal Server Error` | Ошибка сервера |

---

### Валидация пароля

```http
POST /api/security/validate-password
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Request Body:**
```json
{
  "password": "MyStr0ng#Pass!",
  "email": "user@example.com"
}
```

**Success Response (200):**
```json
{
  "isValid": true,
  "errors": [],
  "strength": "Сильный",
  "complexityScore": 4
}
```

**Error Responses:**

| Status | Error | Description |
|--------|-------|-------------|
| 400 | `Validation failed` | Неверные данные |
| 401 | `Unauthorized` | Требуется аутентификация |
| 500 | `Internal Server Error` | Ошибка сервера |

---

### Генерация пароля

```http
POST /api/security/generate-password
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Request Body:**
```json
{
  "length": 16
}
```

**Success Response (200):**
```json
{
  "password": "Kj8#mP2$nQ9@xR5!",
  "strength": "Очень сильный"
}
```

**Error Responses:**

| Status | Error | Description |
|--------|-------|-------------|
| 400 | `Validation failed` | Неверная длина (минимум 8) |
| 401 | `Unauthorized` | Требуется аутентификация |
| 500 | `Internal Server Error` | Ошибка сервера |

---

### Информация о токене

```http
GET /api/security/token-info
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Success Response (200):**
```json
{
  "tokenId": "abc123",
  "userId": 1,
  "issuedAt": "2024-01-01T00:00:00Z",
  "expiresAt": "2024-01-01T00:05:00Z",
  "isExpired": false
}
```

**Error Responses:**

| Status | Error | Description |
|--------|-------|-------------|
| 401 | `Unauthorized` | Требуется аутентификация |
| 500 | `Internal Server Error` | Ошибка сервера |

## Common Error Responses

### Rate Limiting (429)
```json
{
  "error": "Too Many Requests",
  "message": "Превышен лимит запросов",
  "retryAfter": 60
}
```

### Validation Errors (400)
```json
{
  "error": "Validation Failed",
  "message": "Неверные данные запроса",
  "details": [
    {
      "field": "email",
      "message": "Неверный формат email"
    }
  ]
}
```

### Not Found (404)
```json
{
  "error": "Not Found",
  "message": "Ресурс не найден"
}
```

### Server Error (500)
```json
{
  "error": "Internal Server Error",
  "message": "Внутренняя ошибка сервера"
}
```

## Security Headers

All API responses include security headers:

- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `X-XSS-Protection: 1; mode=block`
- `Strict-Transport-Security: max-age=31536000`

## Rate Limiting

- **Limit**: 100 requests per minute
- **Window**: 1 minute
- **Block Duration**: 15 minutes after 50 violations
- **Headers**: `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset`

## Password Requirements

- **Minimum Length**: 8 characters
- **Complexity**: At least 1 uppercase, 1 lowercase, 1 digit, 1 special character
- **Personal Info**: Must not contain parts of email
- **Patterns**: Must not contain obvious sequences (123, abc, etc.)

## WebSocket Support

WebSocket connections are supported at:

```
ws://localhost:5000/ws
```

**Authentication**: JWT token in query parameter or header

**Message Format**:
```json
{
  "type": "device_update",
  "deviceId": "device-123",
  "data": { "status": "online" }
}
```
