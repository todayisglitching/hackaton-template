using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace testASP.Services;

/// <summary>
/// Усиленный сервис JWT токенов с дополнительными мерами безопасности
/// </summary>
public sealed class EnhancedJwtTokenService
{
    private readonly string _secret;
    private readonly ILogger<EnhancedJwtTokenService> _logger;
    private readonly ConcurrentDictionary<string, TokenInfo> _activeTokens = new();
    private readonly Timer _cleanupTimer;

    public EnhancedJwtTokenService(string secret, ILogger<EnhancedJwtTokenService> logger)
    {
        _secret = secret ?? throw new ArgumentNullException(nameof(secret));
        _logger = logger;
        
        // Проверка сложности секрета
        ValidateSecretStrength(secret);
        
        // Запускаем таймер очистки каждые 10 минут
        _cleanupTimer = new Timer(CleanupExpiredTokens, null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
    }

    /// <summary>
    /// Создание JWT токена с усиленными мерами безопасности
    /// </summary>
    public string CreateToken(int userId, TimeSpan expiration)
    {
        var tokenId = GenerateSecureTokenId();
        var issuedAt = DateTime.UtcNow;
        var expires = issuedAt.Add(expiration);
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("id", userId.ToString()),
                new Claim("jti", tokenId), // JWT ID для отслеживания
                new Claim("iat", ((int)(issuedAt - DateTime.UnixEpoch).TotalSeconds).ToString()), // Issued At
                new Claim("exp", ((int)(expires - DateTime.UnixEpoch).TotalSeconds).ToString()), // Expiration
                new Claim("iss", "SmartHomeAPI"), // Issuer
                new Claim("aud", "SmartHomeClient"), // Audience
                new Claim("sid", GenerateSessionId(userId)) // Session ID
            }),
            Expires = expires,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret)),
                SecurityAlgorithms.HmacSha256),
            NotBefore = issuedAt
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        // Сохраняем информацию о токене для отслеживания
        _activeTokens.TryAdd(tokenId, new TokenInfo
        {
            UserId = userId,
            TokenId = tokenId,
            IssuedAt = issuedAt,
            Expires = expires,
            LastUsed = issuedAt
        });

        _logger.LogInformation("Создан токен для пользователя {UserId}, TokenId: {TokenId}, Истекает: {Expires}", 
            userId, tokenId, expires);

        return tokenString;
    }

    /// <summary>
    /// Валидация JWT токена с дополнительными проверками
    /// </summary>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            
            // Настраиваем параметры валидации
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret)),
                ValidateIssuer = true,
                ValidIssuer = "SmartHomeAPI",
                ValidateAudience = true,
                ValidAudience = "SmartHomeClient",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero, // Без задержки времени
                RequireExpirationTime = true
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            
            // Дополнительные проверки безопасности
            if (!PerformAdditionalSecurityChecks(principal, validatedToken as JwtSecurityToken))
            {
                _logger.LogWarning("Дополнительные проверки безопасности не пройдены");
                return null;
            }

            // Обновляем время последнего использования токена
            UpdateTokenLastUsed(principal);

            return principal;
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogWarning("Токен истек");
            return null;
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            _logger.LogWarning("Неверная подпись токена");
            return null;
        }
        catch (SecurityTokenInvalidIssuerException)
        {
            _logger.LogWarning("Неверный издатель токена");
            return null;
        }
        catch (SecurityTokenInvalidAudienceException)
        {
            _logger.LogWarning("Неверная аудитория токена");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка валидации токена");
            return null;
        }
    }

    /// <summary>
    /// Отзыв токена (добавление в черный список)
    /// </summary>
    public bool RevokeToken(string tokenId)
    {
        if (_activeTokens.TryRemove(tokenId, out var tokenInfo))
        {
            _logger.LogInformation("Токен отозван: {TokenId}, Пользователь: {UserId}", 
                tokenId, tokenInfo.UserId);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Отзыв всех токенов пользователя
    /// </summary>
    public int RevokeAllUserTokens(int userId)
    {
        var userTokens = _activeTokens
            .Where(kvp => kvp.Value.UserId == userId)
            .Select(kvp => kvp.Key)
            .ToList();

        var revokedCount = 0;
        foreach (var tokenId in userTokens)
        {
            if (_activeTokens.TryRemove(tokenId, out _))
            {
                revokedCount++;
            }
        }

        _logger.LogInformation("Отозвано {Count} токенов пользователя {UserId}", revokedCount, userId);
        return revokedCount;
    }

    /// <summary>
    /// Проверка сложности секрета
    /// </summary>
    private void ValidateSecretStrength(string secret)
    {
        if (secret.Length < 32)
        {
            throw new ArgumentException("Секретный ключ должен быть не менее 32 символов");
        }

        if (secret.All(char.IsLetterOrDigit))
        {
            _logger.LogWarning("Секретный ключ должен содержать специальные символы для лучшей безопасности");
        }

        // Проверяем на использование в словаре простых паролей
        var commonSecrets = new[]
        {
            "password", "secret", "123456", "admin", "test", "qwerty", "abc123"
        };

        if (commonSecrets.Contains(secret.ToLower()))
        {
            throw new ArgumentException("Использован слишком простой секретный ключ");
        }
    }

    /// <summary>
    /// Генерация безопасного ID токена
    /// </summary>
    private static string GenerateSecureTokenId()
    {
        var bytes = RandomNumberGenerator.GetBytes(16);
        return Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "");
    }

    /// <summary>
    /// Генерация ID сессии
    /// </summary>
    private static string GenerateSessionId(int userId)
    {
        var time = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var random = RandomNumberGenerator.GetInt32(1000, 9999);
        return $"{userId}_{time}_{random}";
    }

    /// <summary>
    /// Дополнительные проверки безопасности токена
    /// </summary>
    private bool PerformAdditionalSecurityChecks(ClaimsPrincipal principal, JwtSecurityToken? token)
    {
        if (token == null)
        {
            return false;
        }

        // Проверяем наличие необходимых claims
        var requiredClaims = new[] { "jti", "sid", "id", "iat", "exp" };
        foreach (var claim in requiredClaims)
        {
            if (!principal.HasClaim(c => c.Type == claim))
            {
                _logger.LogWarning("Отсутствует необходимый claim: {Claim}", claim);
                return false;
            }
        }

        var tokenId = principal.FindFirst("jti")?.Value;
        if (string.IsNullOrEmpty(tokenId))
        {
            return false;
        }

        // Проверяем что токен не был отозван
        if (!_activeTokens.ContainsKey(tokenId))
        {
            _logger.LogWarning("Токен не найден в активных (возможно отозван): {TokenId}", tokenId);
            return false;
        }

        // Проверяем время последнего использования
        var tokenInfo = _activeTokens[tokenId];
        var timeSinceLastUse = DateTime.UtcNow - tokenInfo.LastUsed;
        
        // Если токен не использовался слишком долго, считаем его компрометированным
        if (timeSinceLastUse > TimeSpan.FromHours(24))
        {
            _logger.LogWarning("Токен не использовался слишком долго: {TokenId}", tokenId);
            RevokeToken(tokenId);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Обновление времени последнего использования токена
    /// </summary>
    private void UpdateTokenLastUsed(ClaimsPrincipal principal)
    {
        var tokenId = principal.FindFirst("jti")?.Value;
        if (!string.IsNullOrEmpty(tokenId) && _activeTokens.TryGetValue(tokenId, out var tokenInfo))
        {
            tokenInfo.LastUsed = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Очистка истекших токенов
    /// </summary>
    private void CleanupExpiredTokens(object? state)
    {
        var now = DateTime.UtcNow;
        var expiredTokens = _activeTokens
            .Where(kvp => now > kvp.Value.Expires)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var tokenId in expiredTokens)
        {
            _activeTokens.TryRemove(tokenId, out _);
        }

        // Также очищаем токены, которые не использовались долго
        var staleTokens = _activeTokens
            .Where(kvp => now - kvp.Value.LastUsed > TimeSpan.FromDays(7))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var tokenId in staleTokens)
        {
            _activeTokens.TryRemove(tokenId, out _);
        }

        _logger.LogDebug("Очищено {ExpiredCount} истекших и {StaleCount} неиспользуемых токенов", 
            expiredTokens.Count, staleTokens.Count);
    }

    /// <summary>
    /// Получение статистики активных токенов
    /// </summary>
    public TokenStatistics GetStatistics()
    {
        var now = DateTime.UtcNow;
        var stats = new TokenStatistics
        {
            TotalActiveTokens = _activeTokens.Count,
            TokensExpiringSoon = _activeTokens.Values.Count(t => t.Expires - now < TimeSpan.FromHours(1)),
            TokensNotUsedRecently = _activeTokens.Values.Count(t => now - t.LastUsed > TimeSpan.FromHours(12))
        };

        return stats;
    }
}

/// <summary>
/// Информация о токене
/// </summary>
public sealed class TokenInfo
{
    public int UserId { get; set; }
    public string TokenId { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public DateTime Expires { get; set; }
    public DateTime LastUsed { get; set; }
}

/// <summary>
/// Статистика токенов
/// </summary>
public sealed class TokenStatistics
{
    public int TotalActiveTokens { get; set; }
    public int TokensExpiringSoon { get; set; }
    public int TokensNotUsedRecently { get; set; }
}
