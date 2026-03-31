using System.Security.Claims;
using Microsoft.Extensions.Logging;
using testASP.Configuration;
using testASP.Models;
using testASP.Services.Interfaces;

namespace testASP.Services;

/// <summary>
/// Современный сервис аутентификации с единым BCrypt хешированием
/// </summary>
public sealed class ModernAuthService : IAuthService
{
    private readonly UserStore _users;
    private readonly EnhancedJwtTokenService _tokens;
    private readonly RefreshTokenStore _refreshTokens;
    private readonly ILogger<ModernAuthService> _logger;

    public ModernAuthService(
        UserStore users,
        EnhancedJwtTokenService tokens,
        RefreshTokenStore refreshTokens,
        ILogger<ModernAuthService> logger)
    {
        _users = users;
        _tokens = tokens;
        _refreshTokens = refreshTokens;
        _logger = logger;
    }

    /// <summary>
    /// Регистрация нового пользователя с валидацией пароля
    /// </summary>
    public AuthResponse Register(RegisterRequest request)
    {
        var validationResult = ValidatePasswordComplexity(request.Password, request.Email);
        if (!validationResult.IsValid)
        {
            var errorMessage = string.Join("; ", validationResult.Errors);
            _logger.LogWarning("Пароль не прошел валидацию: {Errors}", errorMessage);
            throw new InvalidOperationException($"Пароль не соответствует требованиям: {errorMessage}");
        }

        // Проверка существования пользователя
        if (_users.EmailExists(request.Email))
        {
            throw new InvalidOperationException("Пользователь с таким email уже существует");
        }

        // Создание пользователя с BCrypt хешированием
        var user = _users.Create(request.Email, request.Password);
        var result = CreateTokens(user.Id);
        
        _logger.LogInformation("Пользователь зарегистрирован: {Email}, ID: {UserId}", request.Email, user.Id);
        return result;
    }

    /// <summary>
    /// Вход пользователя с проверкой BCrypt хеша
    /// </summary>
    public AuthResponse Login(LoginRequest request)
    {
        var user = _users.FindByEmail(request.Email);
        if (user is null)
        {
            _logger.LogWarning("Попытка входа с несуществующим email: {Email}", request.Email);
            throw new UnauthorizedAccessException("Неверный email или пароль");
        }

        // Проверка пароля через BCrypt
        if (!_users.ValidatePassword(user, request.Password))
        {
            _logger.LogWarning("Неверный пароль для пользователя: {Email}", request.Email);
            throw new UnauthorizedAccessException("Неверный email или пароль");
        }

        // Отзыв старых токенов для безопасности
        _tokens.RevokeAllUserTokens(user.Id);
        
        var result = CreateTokens(user.Id);
        _logger.LogInformation("Пользователь вошел: {Email}, ID: {UserId}", request.Email, user.Id);
        return result;
    }

    /// <summary>
    /// Обновление токена доступа
    /// </summary>
    public AuthResponse Refresh(RefreshRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new InvalidOperationException("Refresh токен обязателен");
        }

        var userId = _refreshTokens.Validate(request.RefreshToken);
        if (userId is null)
        {
            _logger.LogWarning("Попытка обновления с невалидным refresh токеном");
            throw new UnauthorizedAccessException("Refresh токен недействителен");
        }

        var user = _users.GetById(userId.Value);
        if (user is null)
        {
            _logger.LogWarning("Пользователь не найден при обновлении токена: {UserId}", userId.Value);
            throw new UnauthorizedAccessException("Пользователь не найден");
        }

        // Отзыв старого токена
        _refreshTokens.Revoke(request.RefreshToken);
        
        var result = CreateTokens(userId.Value);
        _logger.LogInformation("Токен обновлен для пользователя: {UserId}", userId.Value);
        return result;
    }

    /// <summary>
    /// Выход пользователя с отзывом токена
    /// </summary>
    public void Logout(LogoutRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            _refreshTokens.Revoke(request.RefreshToken);
        }

        _logger.LogInformation("Пользователь вышел из системы");
    }

    /// <summary>
    /// Получение информации о пользователе
    /// </summary>
    public MeResponse Me(ClaimsPrincipal user)
    {
        var idClaim = user.Claims.FirstOrDefault(x => x.Type == "id")?.Value;
        if (!int.TryParse(idClaim, out var userId))
        {
            _logger.LogWarning("Некорректный ID пользователя в токене");
            throw new UnauthorizedAccessException("Пользователь не аутентифицирован");
        }

        var dbUser = _users.GetById(userId);
        if (dbUser is null)
        {
            _logger.LogWarning("Пользователь не найден: {UserId}", userId);
            throw new UnauthorizedAccessException("Пользователь не найден");
        }

        return new MeResponse { UserId = dbUser.Id };
    }

    /// <summary>
    /// Создание JWT токенов
    /// </summary>
    private AuthResponse CreateTokens(int userId)
    {
        var accessToken = _tokens.CreateToken(userId, TimeSpan.FromMinutes(5));
        var refreshToken = _refreshTokens.Create(userId, TimeSpan.FromDays(7));
        
        return new AuthResponse 
        { 
            UserId = userId, 
            Token = accessToken, 
            RefreshToken = refreshToken 
        };
    }

    /// <summary>
    /// Валидация сложности пароля
    /// </summary>
    private ModernPasswordValidationResult ValidatePasswordComplexity(string password, string email)
    {
        var errors = new List<string>();

        // Минимальная длина
        if (password.Length < 8)
            errors.Add("Минимальная длина пароля - 8 символов");

        // Проверка на сложность
        var hasUpper = password.Any(char.IsUpper);
        var hasLower = password.Any(char.IsLower);
        var hasDigit = password.Any(char.IsDigit);
        var hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));

        if (!hasUpper) errors.Add("Требуется хотя бы одна заглавная буква");
        if (!hasLower) errors.Add("Требуется хотя бы одна строчная буква");
        if (!hasDigit) errors.Add("Требуется хотя бы одна цифра");
        if (!hasSpecial) errors.Add("Требуется хотя бы один специальный символ");

        // Проверка на личную информацию
        var emailParts = email.Split('@')[0].Split('.', '-', '_');
        foreach (var part in emailParts.Where(p => p.Length >= 3))
        {
            if (password.Contains(part, StringComparison.OrdinalIgnoreCase))
                errors.Add("Пароль не должен содержать части email");
        }

        // Проверка на очевидные последовательности
        if (HasObviousPatterns(password))
            errors.Add("Пароль не должен содержать очевидные последовательности");

        return new ModernPasswordValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors,
            Strength = CalculatePasswordStrength(password)
        };
    }

    /// <summary>
    /// Проверка на очевидные последовательности
    /// </summary>
    private static bool HasObviousPatterns(string password)
    {
        // Проверка на последовательности (123, abc)
        for (int i = 0; i < password.Length - 2; i++)
        {
            var chars = password.Substring(i, 3);
            if (IsSequence(chars))
                return true;
        }

        // Проверка на повторения (aaa, 111)
        for (int i = 0; i < password.Length - 2; i++)
        {
            if (password[i] == password[i + 1] && password[i] == password[i + 2])
                return true;
        }

        return false;
    }

    /// <summary>
    /// Проверка является ли строка последовательностью
    /// </summary>
    private static bool IsSequence(string chars)
    {
        // Числовые последовательности
        if (int.TryParse(chars, out var num))
        {
            return (chars[1] - chars[0] == 1) && (chars[2] - chars[1] == 1) ||
                   (chars[1] - chars[0] == -1) && (chars[2] - chars[1] == -1);
        }

        // Буквенные последовательности
        if (chars.All(char.IsLetter))
        {
            return (chars[1] - chars[0] == 1) && (chars[2] - chars[1] == 1) ||
                   (chars[1] - chars[0] == -1) && (chars[2] - chars[1] == -1);
        }

        return false;
    }

    /// <summary>
    /// Расчет силы пароля
    /// </summary>
    private static string CalculatePasswordStrength(string password)
    {
        var score = 0;
        if (password.Length >= 12) score++;
        if (password.Length >= 16) score++;
        if (password.Any(char.IsUpper)) score++;
        if (password.Any(char.IsLower)) score++;
        if (password.Any(char.IsDigit)) score++;
        if (password.Any(c => !char.IsLetterOrDigit(c))) score++;

        return score switch
        {
            >= 5 => "Очень сильный",
            >= 4 => "Сильный",
            >= 3 => "Средний",
            >= 2 => "Слабый",
            _ => "Очень слабый"
        };
    }
}
