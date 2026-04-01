# SmartHome API Documentation

## Overview

Modern RESTful API for Smart Home system with enhanced authentication, session management, and device control.

**Base URL**: `http://localhost:5000`  
**Authentication**: Bearer JWT Token  
**Content-Type**: `application/json`

---

## Authentication

### Register User

Creates a new user account with BCrypt password hashing and validation.

```http
POST /api/auth/register
Content-Type: application/json
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
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpZCI6IjEiLCJqdGkiOiJhYmNkMTIzIiwiaWF0IjoxNzA0MDY0MDAwLCJleHAiOjE3MDQwNjQ2MDAsImlzcyI6IlNtYXJ0SG9tZUFQSSIsImF1ZCI6IlNtYXJ0SG9tZUNsaWVudCIsInNpZCI6IjFfMTcwNDA2NDAwMF8xMjM0IiwibmJmIjoxNzA0MDY0MDAwfQ.signature",
  "refreshToken": "dGhpcy1pcy1yZWZyZXNoLXRva2Vu",
  "userId": 1
}
```

**Error Responses:**
| Status | Error | Description |
|--------|-------|-------------|
| 400 | `Пароль не соответствует требованиям: [details]` | Password validation failed |
| 409 | `Пользователь с указанным email уже существует` | Email already registered |
| 429 | `Too Many Requests` | Rate limit exceeded |
| 500 | `Internal Server Error` | Server error |

---

### Login User

Authenticates user and returns JWT tokens.

```http
POST /api/auth/login
Content-Type: application/json
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
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpZCI6IjEiLCJqdGkiOiJhYmNkMTIzIiwiaWF0IjoxNzA0MDY0MDAwLCJleHAiOjE3MDQwNjQ2MDAsImlzcyI6IlNtYXJ0SG9tZUFQSSIsImF1ZCI6IlNtYXJ0SG9tZUNsaWVudCIsInNpZCI6IjFfMTcwNDA2NDAwMF8xMjM0IiwibmJmIjoxNzA0MDY0MDAwfQ.signature",
  "refreshToken": "dGhpcy1pcy1yZWZyZXNoLXRva2Vu",
  "userId": 1
}
```

**Error Responses:**
| Status | Error | Description |
|--------|-------|-------------|
| 401 | `Неверный email или пароль` | Invalid credentials |
| 429 | `Too Many Requests` | Rate limit exceeded |
| 500 | `Internal Server Error` | Server error |

---

### Refresh Token

Updates access token using refresh token.

```http
POST /api/auth/refresh
Content-Type: application/json
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
  "token": "new-access-token-here",
  "refreshToken": "new-refresh-token-here",
  "userId": 1
}
```

**Error Responses:**
| Status | Error | Description |
|--------|-------|-------------|
| 400 | `Refresh токен обязателен` | Refresh token not provided |
| 401 | `Refresh токен недействителен` | Invalid or expired refresh token |
| 500 | `Internal Server Error` | Server error |

---

### Get Current User with Active Sessions

Retrieves user information and all active sessions.

```http
GET /api/auth/me
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Success Response (200):**
```json
{
  "userId": 1,
  "email": "user@example.com",
  "activeSessions": [
    {
      "sessionId": "jti-abc123def456",
      "deviceInfo": "Unknown Device",
      "ipAddress": "Unknown IP",
      "createdAt": "2024-01-01T10:00:00Z",
      "lastUsed": "2024-01-01T12:00:00Z",
      "expiresAt": "2024-01-01T17:00:00Z",
      "isCurrent": true
    },
    {
      "sessionId": "jti-xyz789uvw012",
      "deviceInfo": "Unknown Device", 
      "ipAddress": "Unknown IP",
      "createdAt": "2024-01-01T09:00:00Z",
      "lastUsed": "2024-01-01T11:30:00Z",
      "expiresAt": "2024-01-01T16:30:00Z",
      "isCurrent": false
    }
  ]
}
```

**Error Responses:**
| Status | Error | Description |
|--------|-------|-------------|
| 401 | `Пользователь не аутентифицирован` | Invalid or expired token |
| 401 | `Пользователь не найден` | User deleted |
| 500 | `Internal Server Error` | Server error |

---

### Revoke Specific Session

Revokes a specific active session immediately.

```http
POST /api/auth/revoke-session
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
```

**Request Body:**
```json
{
  "sessionId": "jti-abc123def456"
}
```

**Success Response (200):**
```json
{}
```

**Error Responses:**
| Status | Error | Description |
|--------|-------|-------------|
| 400 | `Сессия не найдена` | Session ID not found |
| 401 | `Пользователь не аутентифицирован` | Invalid token |
| 500 | `Internal Server Error` | Server error |

---

### Logout

Logs out user by revoking refresh token.

```http
POST /api/auth/logout
Content-Type: application/json
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
| 401 | `Refresh токен недействителен` | Invalid refresh token |
| 500 | `Internal Server Error` | Server error |

---

## Devices

### Get Devices

Retrieves all devices for the authenticated user.

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
      "status": "offline",
      "properties": "{}",
      "location": "Living Room",
      "manufacturer": "SmartHome Inc",
      "model": "SH-L1000",
      "firmwareVersion": "1.2.3",
      "createdAt": "2024-01-01T00:00:00Z",
      "updatedAt": "2024-01-01T00:00:00Z",
      "isEnabled": true
    },
    {
      "id": 2,
      "deviceId": "sensor-456", 
      "name": "Temperature Sensor",
      "type": "sensor",
      "status": "online",
      "properties": "{\"temperature\": 22.5, \"humidity\": 45}",
      "location": "Bedroom",
      "manufacturer": "SensorTech",
      "model": "ST-T200",
      "firmwareVersion": "2.1.0",
      "createdAt": "2024-01-01T01:00:00Z",
      "updatedAt": "2024-01-01T12:00:00Z",
      "isEnabled": true
    }
  ],
  "selectedDeviceId": "device-123"
}
```

**Error Responses:**
| Status | Error | Description |
|--------|-------|-------------|
| 401 | `Пользователь не аутентифицирован` | Invalid token |
| 500 | `Internal Server Error` | Server error |

---

### Add Device

Adds a new device to the user's collection.

```http
POST /api/devices
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
```

**Request Body:**
```json
{
  "deviceId": "device-789",
  "name": "Smart Thermostat",
  "type": "thermostat",
  "properties": "{\"targetTemperature\": 22, \"mode\": \"auto\"}",
  "location": "Hallway",
  "manufacturer": "ClimateControl",
  "model": "CC-T300",
  "firmwareVersion": "3.0.1"
}
```

**Success Response (201):**
```json
{
  "id": 3,
  "deviceId": "device-789",
  "name": "Smart Thermostat",
  "type": "thermostat",
  "status": "offline",
  "properties": "{\"targetTemperature\": 22, \"mode\": \"auto\"}",
  "location": "Hallway",
  "manufacturer": "ClimateControl",
  "model": "CC-T300",
  "firmwareVersion": "3.0.1",
  "createdAt": "2024-01-01T13:00:00Z",
  "updatedAt": "2024-01-01T13:00:00Z",
  "isEnabled": true
}
```

**Error Responses:**
| Status | Error | Description |
|--------|-------|-------------|
| 400 | `Validation failed` | Invalid device data |
| 401 | `Пользователь не аутентифицирован` | Invalid token |
| 409 | `Device with this ID already exists` | Device ID conflict |
| 500 | `Internal Server Error` | Server error |

---

### Select Device

Sets a device as the currently selected device.

```http
POST /api/devices/select
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
```

**Request Body:**
```json
{
  "deviceId": "device-123"
}
```

**Success Response (200):**
```json
{}
```

**Error Responses:**
| Status | Error | Description |
|--------|-------|-------------|
| 400 | `Validation failed` | Invalid device ID |
| 401 | `Пользователь не аутентифицирован` | Invalid token |
| 404 | `Device not found` | Device doesn't exist |
| 500 | `Internal Server Error` | Server error |

---

### Delete Device

Removes a device from the user's collection.

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
| 401 | `Пользователь не аутентифицирован` | Invalid token |
| 404 | `Device not found` | Device doesn't exist |
| 500 | `Internal Server Error` | Server error |

---

## Health Check

### System Health

Checks overall system health status.

```http
GET /api/health
```

**Success Response (200):**
```json
{
  "status": "Healthy",
  "timestamp": "2024-01-01T12:00:00Z",
  "version": "1.0.0",
  "environment": "Development",
  "database": "Connected"
}
```

---

### Database Health

Checks database connectivity and initialization status.

```http
GET /api/health/database
```

**Success Response (200):**
```json
{
  "isHealthy": true,
  "status": "Healthy",
  "message": "База данных полностью готова к работе",
  "tablesExist": true,
  "collectionsExist": true
}
```

**Unhealthy Response (200):**
```json
{
  "isHealthy": false,
  "status": "Incomplete",
  "message": "База данных требует инициализации",
  "tablesExist": false,
  "collectionsExist": true
}
```

---

### Database Statistics

Returns detailed database statistics.

```http
GET /api/health/database/stats
```

**Success Response (200):**
```json
{
  "collectionCount": 5,
  "fieldCount": 23,
  "documentCount": 156,
  "userCount": 42,
  "databaseSize": 2048576,
  "formattedDatabaseSize": "2.0 MB"
}
```

---

## Security

### Security Statistics

Returns token usage statistics for security monitoring.

```http
GET /api/security/statistics
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Success Response (200):**
```json
{
  "totalActiveTokens": 5,
  "tokensExpiringSoon": 1,
  "tokensNotUsedRecently": 0
}
```

**Error Responses:**
| Status | Error | Description |
|--------|-------|-------------|
| 401 | `Пользователь не аутентифицирован` | Invalid token |
| 500 | `Internal Server Error` | Server error |

---

### Revoke All User Tokens

Revokes all tokens for the authenticated user.

```http
POST /api/security/revoke-all
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Success Response (200):**
```json
{
  "revokedCount": 3,
  "message": "Все токены успешно отозваны"
}
```

**Error Responses:**
| Status | Error | Description |
|--------|-------|-------------|
| 401 | `Пользователь не аутентифицирован` | Invalid token |
| 500 | `Internal Server Error` | Server error |

---

### Get Token Information

Returns detailed information about the current token.

```http
GET /api/security/token-info
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Success Response (200):**
```json
{
  "tokenId": "jti-abc123def456",
  "userId": 1,
  "issuedAt": "2024-01-01T10:00:00Z",
  "expiresAt": "2024-01-01T10:30:00Z",
  "sessionId": "1_1704060000_1234",
  "isExpired": false
}
```

**Error Responses:**
| Status | Error | Description |
|--------|-------|-------------|
| 401 | `Пользователь не аутентифицирован` | Invalid token |
| 500 | `Internal Server Error` | Server error |

---

### Generate Secure Password

Generates a cryptographically secure password.

```http
GET /api/security/generate-password?length=16
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Success Response (200):**
```json
{
  "password": "Kj8#mP2$nQ9@xR5!",
  "length": 16,
  "message": "Пароль сгенерирован. Сохраните его в надежном месте."
}
```

**Error Responses:**
| Status | Error | Description |
|--------|-------|-------------|
| 400 | `Длина пароля должна быть от 8 до 32 символов` | Invalid length |
| 401 | `Пользователь не аутентифицирован` | Invalid token |
| 500 | `Internal Server Error` | Server error |

---

### Validate Password

Validates password strength without saving it.

```http
POST /api/security/validate-password
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
```

**Request Body:**
```json
{
  "password": "MyTestPassword123!"
}
```

**Success Response (200):**
```json
{
  "isValid": true,
  "strength": "Сильный",
  "errors": []
}
```

**Error Responses:**
| Status | Error | Description |
|--------|-------|-------------|
| 401 | `Пользователь не аутентифицирован` | Invalid token |
| 500 | `Internal Server Error` | Server error |

---

## Common Error Responses

### Unauthorized (401)
```json
{
  "error": "Unauthorized",
  "message": "Пользователь не аутентифицирован"
}
```

### Bad Request (400)
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

### Too Many Requests (429)
```json
{
  "error": "Too Many Requests",
  "message": "Превышен лимит запросов",
  "retryAfter": 60
}
```

### Internal Server Error (500)
```json
{
  "error": "Internal Server Error",
  "message": "Внутренняя ошибка сервера"
}
```

---

## Security Features

### Token Configuration
- **Access Token Lifetime**: 30 minutes
- **Refresh Token Lifetime**: 7 days
- **Algorithm**: HS256
- **Token Tracking**: Enhanced with JTI and session management
- **Immediate Revocation**: Session and token revocation support

### Password Requirements
- **Minimum Length**: 8 characters
- **Complexity**: At least 1 uppercase, 1 lowercase, 1 digit, 1 special character
- **Personal Info**: Must not contain parts of email
- **Patterns**: Must not contain obvious sequences (123, abc, etc.)

### Rate Limiting
- **Limit**: 100 requests per minute per IP
- **Window**: 1 minute
- **Block Duration**: 15 minutes after 50 violations
- **Headers**: `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset`

### Security Headers
All responses include:
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `X-XSS-Protection: 1; mode=block`
- `Strict-Transport-Security: max-age=31536000`

---

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

---

## Implementation Details

### Authentication Flow
1. User registers/logs in → receives access + refresh tokens
2. Access token (30min) used for API calls  
3. Refresh token (7days) used to get new access tokens
4. Active sessions tracked with JTI claims
5. Sessions can be revoked individually or all at once

### Session Management
- Each JWT contains unique `jti` (JWT ID) claim
- Sessions tracked in `EnhancedJwtTokenService`
- Current session marked with `isCurrent: true`
- Immediate revocation via `/api/auth/revoke-session`

### Device Management
- Devices stored per user with full metadata
- Automatic selection of first added device
- Support for custom properties via JSON string
- Status tracking (online/offline)

### Database Architecture
- SQLite for relational data (users, devices)
- NoSQL for flexible collections (logs, events)
- Health monitoring for both systems
- Automatic initialization and migration support

---

## Quick Start Examples

### Complete Authentication Flow
```bash
# 1. Register
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"MyStr0ng#Pass!"}'

# 2. Login
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"MyStr0ng#Pass!"}'

# 3. Get user info with sessions
curl -X GET http://localhost:5000/api/auth/me \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"

# 4. Add device
curl -X POST http://localhost:5000/api/devices \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"light-1","name":"Living Room Light","type":"light","properties":"{}","location":"Living Room","manufacturer":"SmartHome","model":"SH-L1000","firmwareVersion":"1.0.0"}'

# 5. Get devices
curl -X GET http://localhost:5000/api/devices \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

---

## Version History

### v1.0.0 (Current)
- ✅ Modern authentication with BCrypt + Enhanced JWT
- ✅ Active session management and revocation
- ✅ Complete device management system
- ✅ Health monitoring and statistics
- ✅ Security features and rate limiting
- ✅ Comprehensive API documentation

---

*Last Updated: January 2024*  
*API Version: 1.0.0*  
*Status: Production Ready*
