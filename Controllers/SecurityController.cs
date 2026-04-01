using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using testASP.Services;
using testASP.Models;

namespace testASP.Controllers;

/// <summary>
/// Контроллер для управления безопасностью и получения статистики
/// </summary>
[ApiController]
[Route("api/security")]
[Authorize]
public sealed class SecurityController : ControllerBase
{
    private readonly EnhancedJwtTokenService _jwtService;
    private readonly EnhancedPasswordService _passwordService;
    private readonly ILogger<SecurityController> _logger;

    public SecurityController(
        EnhancedJwtTokenService jwtService,
        EnhancedPasswordService passwordService,
        ILogger<SecurityController> logger)
    {
        _jwtService = jwtService;
        _passwordService = passwordService;
        _logger = logger;
    }

    /// <summary>
    /// Получение статистики безопасности
    /// </summary>
    /// <returns>Статистика активных токенов и безопасности</returns>
    /// <response code="200">Статистика успешно получена</response>
    /// <response code="401">Пользователь не аутентифицирован</response>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(TokenStatistics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<TokenStatistics> GetSecurityStatistics()
    {
        var statistics = _jwtService.GetStatistics();
        
        _logger.LogInformation("Запрос статистики безопасности от пользователя");
        
        return Ok(statistics);
    }

    /// <summary>
    /// Отзыв всех токенов текущего пользователя
    /// </summary>
    /// <returns>Количество отозванных токенов</returns>
    /// <response code="200">Все токены успешно отозваны</response>
    /// <response code="401">Пользователь не аутентифицирован</response>
    [HttpPost("revoke-all")]
    [ProducesResponseType(typeof(SecurityRevokeAllResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<SecurityRevokeAllResponse> RevokeAllTokens()
    {
        var userIdClaim = User.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized("Пользователь не аутентифицирован");
        }

        var revokedCount = _jwtService.RevokeAllUserTokens(userId);
        
        _logger.LogInformation("Пользователь {UserId} отозвал все свои токены ({Count} шт)", userId, revokedCount);
        
        return Ok(new SecurityRevokeAllResponse 
        { 
            RevokedCount = revokedCount,
            Message = "Все токены успешно отозваны"
        });
    }

    /// <summary>
    /// Валидация пароля без сохранения
    /// </summary>
    /// <param name="request">Запрос на валидацию пароля</param>
    /// <returns>Результат валидации пароля</returns>
    /// <response code="200">Пароль проверен</response>
    /// <response code="401">Пользователь не аутентифицирован</response>
    [HttpPost("validate-password")]
    [ProducesResponseType(typeof(PasswordValidationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<PasswordValidationResult> ValidatePassword([FromBody] ValidatePasswordRequest request)
    {
        var userIdClaim = User.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
        {
            return Unauthorized("Пользователь не аутентифицирован");
        }

        // Получаем email пользователя для проверки персональной информации
        var email = User.FindFirst("email")?.Value ?? "";
        
        var result = _passwordService.ValidatePassword(request.Password, email);
        
        _logger.LogInformation("Проверка сложности пароля пользователем {UserId}", userIdClaim);
        
        return Ok(result);
    }

    /// <summary>
    /// Генерация безопасного пароля
    /// </summary>
    /// <param name="length">Длина пароля (по умолчанию 16)</param>
    /// <returns>Сгенерированный пароль</returns>
    /// <response code="200">Пароль успешно сгенерирован</response>
    /// <response code="401">Пользователь не аутентифицирован</response>
    [HttpGet("generate-password")]
    [ProducesResponseType(typeof(GeneratePasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<GeneratePasswordResponse> GeneratePassword([FromQuery] int length = 16)
    {
        if (length < 8 || length > 32)
        {
            return BadRequest("Длина пароля должна быть от 8 до 32 символов");
        }

        var password = _passwordService.GenerateSecurePassword(length);
        
        _logger.LogInformation("Пользователь сгенерировал безопасный пароль длиной {Length}", length);
        
        return Ok(new GeneratePasswordResponse 
        { 
            Password = password,
            Length = password.Length,
            Message = "Пароль сгенерирован. Сохраните его в надежном месте."
        });
    }

    /// <summary>
    /// Получение информации о текущем токене
    /// </summary>
    /// <returns>Информация о токене</returns>
    /// <response code="200">Информация о токене получена</response>
    /// <response code="401">Пользователь не аутентифицирован</response>
    [HttpGet("token-info")]
    [ProducesResponseType(typeof(SecurityTokenInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<SecurityTokenInfo> GetTokenInfo()
    {
        var jti = User.FindFirst("jti")?.Value;
        var userId = User.FindFirst("id")?.Value;
        var iat = User.FindFirst("iat")?.Value;
        var exp = User.FindFirst("exp")?.Value;
        var sid = User.FindFirst("sid")?.Value;

        if (string.IsNullOrEmpty(jti) || string.IsNullOrEmpty(userId))
        {
            return Unauthorized("Некорректный токен");
        }

        var tokenInfo = new SecurityTokenInfo
        {
            TokenId = jti,
            UserId = int.Parse(userId),
            IssuedAt = iat != null ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(iat)).DateTime : null,
            ExpiresAt = exp != null ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(exp)).DateTime : null,
            SessionId = sid,
            IsExpired = exp != null && DateTimeOffset.FromUnixTimeSeconds(long.Parse(exp)).DateTime < DateTime.UtcNow
        };

        _logger.LogDebug("Запрос информации о токене {TokenId}", jti);
        
        return Ok(tokenInfo);
    }
}

/// <summary>
/// Запрос на валидацию пароля
/// </summary>
public class ValidatePasswordRequest
{
    /// <summary>
    /// Пароль для проверки
    /// </summary>
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Ответ на отзыв всех токенов
/// </summary>
public class SecurityRevokeAllResponse
{
    /// <summary>
    /// Количество отозванных токенов
    /// </summary>
    public int RevokedCount { get; set; }
    
    /// <summary>
    /// Сообщение об операции
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Ответ на генерацию пароля
/// </summary>
public class GeneratePasswordResponse
{
    /// <summary>
    /// Сгенерированный пароль
    /// </summary>
    public string Password { get; set; } = string.Empty;
    
    /// <summary>
    /// Длина пароля
    /// </summary>
    public int Length { get; set; }
    
    /// <summary>
    /// Сообщение
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Информация о токене
/// </summary>
public class SecurityTokenInfo
{
    /// <summary>
    /// ID токена
    /// </summary>
    public string TokenId { get; set; } = string.Empty;
    
    /// <summary>
    /// ID пользователя
    /// </summary>
    public int UserId { get; set; }
    
    /// <summary>
    /// Время выдачи
    /// </summary>
    public DateTime? IssuedAt { get; set; }
    
    /// <summary>
    /// Время истечения
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    
    /// <summary>
    /// ID сессии
    /// </summary>
    public string? SessionId { get; set; }
    
    /// <summary>
    /// Истек ли токен
    /// </summary>
    public bool IsExpired { get; set; }
}
