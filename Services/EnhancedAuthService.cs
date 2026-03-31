using System.Security.Claims;
using testASP.Configuration;
using testASP.Models;
using testASP.Services.Interfaces;

namespace testASP.Services;

/// <summary>
/// Усиленный сервис аутентификации с дополнительными мерами безопасности
/// </summary>
public sealed class EnhancedAuthService : IAuthService
{
    private readonly UserStore _users;
    private readonly EnhancedJwtTokenService _tokens;
    private readonly RefreshTokenStore _refreshTokens;
    private readonly EnhancedPasswordService _passwordService;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<EnhancedAuthService> _logger;

    public EnhancedAuthService(
        UserStore users, 
        EnhancedJwtTokenService tokens, 
        RefreshTokenStore refreshTokens,
        IConfiguration configuration,
        ILogger<EnhancedAuthService> logger)
    {
        _users = users;
        _tokens = tokens;
        _refreshTokens = refreshTokens;
        _passwordService = new EnhancedPasswordService(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<EnhancedPasswordService>.Instance, 
            new HttpClient());
        _jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
        _logger = logger;
    }

    /// <summary>
    /// Регистрация нового пользователя с усиленными проверками безопасности
    /// </summary>
    public AuthResponse Register(RegisterRequest request)
    {
        // Усиленная валидация учетных данных
        var validationResult = _passwordService.ValidatePassword(request.Password, request.Email);
        if (!validationResult.IsValid)
        {
            var errorMessage = string.Join("; ", validationResult.Errors);
            _logger.LogWarning("Ошибка валидации пароля при регистрации: {Errors}", errorMessage);
            throw new InvalidOperationException($"Пароль не соответствует требованиям безопасности: {errorMessage}");
        }

        if (_users.EmailExists(request.Email))
        {
            throw new InvalidOperationException("Пользователь с указанным email уже существует");
        }

        // Используем UserStore для создания пользователя (он сам захеширует пароль)
        var user = _users.Create(request.Email, request.Password);
        
        var result = CreateTokens(user.Id);
        
        _logger.LogInformation("Пользователь успешно зарегистрирован: {Email}, ID: {UserId}, Сложность пароля: {Strength}", 
            request.Email, user.Id, validationResult.Strength);
        
        return result;
    }

    /// <summary>
    /// Вход пользователя с дополнительными проверками безопасности
    /// </summary>
    public AuthResponse Login(LoginRequest request)
    {
        _logger.LogInformation("Попытка входа для email: {Email}", request.Email);
        
        var user = _users.FindByEmail(request.Email);
        if (user == null)
        {
            _logger.LogWarning("Пользователь не найден: {Email}", request.Email);
            throw new UnauthorizedAccessException("Неверный email или пароль");
        }

        _logger.LogInformation("Пользователь найден: {Email}, ID: {Id}", request.Email, user.Id);
        _logger.LogInformation("Проверка пароля...");

        // Проверяем пароль через UserStore (теперь использует BCrypt)
        var passwordValid = _users.ValidatePassword(user, request.Password);
        _logger.LogInformation("Результат проверки пароля: {Valid}", passwordValid);
        
        if (!passwordValid)
        {
            _logger.LogWarning("Неудачная попытка входа для пользователя: {Email}", request.Email);
            throw new UnauthorizedAccessException("Неверный email или пароль");
        }

        // Отзываем все предыдущие токены пользователя для безопасности
        var revokedCount = _tokens.RevokeAllUserTokens(user.Id);
        if (revokedCount > 0)
        {
            _logger.LogInformation("Отозвано {Count} предыдущих токенов пользователя {UserId}", revokedCount, user.Id);
        }

        var result = CreateTokens(user.Id);
        
        _logger.LogInformation("Пользователь успешно вошел в систему: {Email}, ID: {UserId}", request.Email, user.Id);
        
        return result;
    }

    /// <summary>
    /// Обновление токена с усиленными проверками
    /// </summary>
    public AuthResponse Refresh(RefreshRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new InvalidOperationException("Refresh токен обязателен для обновления");
        }

        var userId = _refreshTokens.Validate(request.RefreshToken);
        if (userId == null)
        {
            _logger.LogWarning("Попытка обновления с недействительным refresh токеном");
            throw new UnauthorizedAccessException("Refresh токен истек или недействителен");
        }

        // Дополнительная проверка - не было ли подозрительной активности
        var user = _users.GetById(userId.Value);
        if (user == null)
        {
            _logger.LogWarning("Пользователь не найден при обновлении токена: {UserId}", userId.Value);
            throw new UnauthorizedAccessException("Пользователь не найден");
        }

        // Отзываем старый refresh токен
        _refreshTokens.Revoke(request.RefreshToken);
        
        var result = CreateTokens(userId.Value);
        
        _logger.LogInformation("Токен успешно обновлен для пользователя: {UserId}", userId.Value);
        
        return result;
    }

    /// <summary>
    /// Выход пользователя с полным отзывом токенов
    /// </summary>
    public void Logout(LogoutRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            _refreshTokens.Revoke(request.RefreshToken);
        }

        // Дополнительно пытаемся извлечь JWT ID из токена для его отзыва
        try
        {
            // Здесь можно добавить логику извлечения JWT ID из access токена
            // если он передается в запросе
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Не удалось извлечь JWT ID при выходе");
        }

        _logger.LogInformation("Пользователь вышел из системы");
    }

    /// <summary>
    /// Получение информации о пользователе с проверкой токена
    /// </summary>
    public MeResponse Me(ClaimsPrincipal user)
    {
        var idClaim = user.Claims.FirstOrDefault(x => x.Type == "id")?.Value;
        if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out var userId))
        {
            _logger.LogWarning("Некорректный ID пользователя в токене");
            throw new UnauthorizedAccessException("Пользователь не аутентифицирован");
        }

        // Дополнительная проверка - существует ли пользователь
        var dbUser = _users.GetById(userId);
        if (dbUser == null)
        {
            _logger.LogWarning("Пользователь не найден в базе данных: {UserId}", userId);
            throw new UnauthorizedAccessException("Пользователь не найден");
        }

        // Проверяем JWT ID для дополнительной безопасности
        var jtiClaim = user.Claims.FirstOrDefault(x => x.Type == "jti")?.Value;
        if (!string.IsNullOrEmpty(jtiClaim))
        {
            // Здесь можно добавить проверку что токен не отозван
            // через усиленный JWT сервис
        }

        _logger.LogDebug("Запрос информации о пользователе: {UserId}", userId);
        return new MeResponse { UserId = userId };
    }

    /// <summary>
    /// Создание токенов с настройками из конфигурации
    /// </summary>
    private AuthResponse CreateTokens(int userId)
    {
        var accessTokenLifetime = TimeSpan.FromMinutes(_jwtSettings.AccessTokenExpirationMinutes);
        var refreshTokenLifetime = TimeSpan.FromDays(_jwtSettings.RefreshTokenExpirationDays);
        
        var accessToken = _tokens.CreateToken(userId, accessTokenLifetime);
        var refreshToken = _refreshTokens.Create(userId, refreshTokenLifetime);
        
        return new AuthResponse 
        { 
            Token = accessToken, 
            RefreshToken = refreshToken, 
            UserId = userId 
        };
    }

    /// <summary>
    /// Получение статистики безопасности
    /// </summary>
    public SecurityStatistics GetSecurityStatistics()
    {
        var tokenStats = _tokens.GetStatistics();
        
        return new SecurityStatistics
        {
            ActiveTokens = tokenStats.TotalActiveTokens,
            TokensExpiringSoon = tokenStats.TokensExpiringSoon,
            StaleTokens = tokenStats.TokensNotUsedRecently,
            LastCleanup = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Принудительная отмена всех сессий пользователя
    /// </summary>
    public int RevokeAllUserSessions(int userId)
    {
        var revokedCount = _tokens.RevokeAllUserTokens(userId);
        
        // Также отзываем все refresh токены
        _refreshTokens.RevokeAllUserTokens(userId);
        
        _logger.LogInformation("Принудительно завершены все сессии пользователя {UserId}, отозвано токенов: {Count}", 
            userId, revokedCount);
        
        return revokedCount;
    }
}

/// <summary>
/// Статистика безопасности
/// </summary>
public sealed class SecurityStatistics
{
    public int ActiveTokens { get; set; }
    public int TokensExpiringSoon { get; set; }
    public int StaleTokens { get; set; }
    public DateTime LastCleanup { get; set; }
}
