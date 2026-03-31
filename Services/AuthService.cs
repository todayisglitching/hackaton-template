using System.Security.Claims;
using testASP.Configuration;
using testASP.Models;
using testASP.Services.Interfaces;

namespace testASP.Services;

/// <summary>
/// Сервис аутентификации пользователей
/// Обрабатывает регистрацию, вход, обновление токенов и выход из системы
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly UserStore _users;
    private readonly JwtTokenService _tokens;
    private readonly RefreshTokenStore _refreshTokens;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        UserStore users, 
        JwtTokenService tokens, 
        RefreshTokenStore refreshTokens,
        IConfiguration configuration)
    {
        _users = users;
        _tokens = tokens;
        _refreshTokens = refreshTokens;
        _jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
    }

    /// <summary>
    /// Регистрация нового пользователя
    /// </summary>
    /// <param name="request">Данные для регистрации</param>
    /// <returns>Ответ с токенами аутентификации</returns>
    /// <exception cref="InvalidOperationException">Если пользователь уже существует или данные некорректны</exception>
    public AuthResponse Register(RegisterRequest request)
    {
        ValidateCredentials(request.Email, request.Password);

        if (_users.EmailExists(request.Email))
        {
            throw new InvalidOperationException("Пользователь с указанным email уже существует");
        }

        var user = _users.Create(request.Email, request.Password);
        return CreateTokens(user.Id);
    }

    /// <summary>
    /// Вход пользователя в систему
    /// </summary>
    /// <param name="request">Данные для входа</param>
    /// <returns>Ответ с токенами аутентификации</returns>
    /// <exception cref="UnauthorizedAccessException">Если email или пароль неверны</exception>
    public AuthResponse Login(LoginRequest request)
    {
        var user = _users.FindByEmail(request.Email);
        if (user == null || !_users.ValidatePassword(user, request.Password))
        {
            throw new UnauthorizedAccessException("Неверный email или пароль");
        }

        return CreateTokens(user.Id);
    }

    /// <summary>
    /// Обновление токенов с помощью refresh токена
    /// </summary>
    /// <param name="request">Запрос на обновление токена</param>
    /// <returns>Новая пара токенов</returns>
    /// <exception cref="InvalidOperationException">Если refresh токен не предоставлен</exception>
    /// <exception cref="UnauthorizedAccessException">Если refresh токен недействителен</exception>
    public AuthResponse Refresh(RefreshRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new InvalidOperationException("Refresh токен обязателен для обновления");
        }

        var userId = _refreshTokens.Validate(request.RefreshToken);
        if (userId == null)
        {
            throw new UnauthorizedAccessException("Refresh токен истек или недействителен");
        }

        // Отзываем старый refresh токен для безопасности
        _refreshTokens.Revoke(request.RefreshToken);
        return CreateTokens(userId.Value);
    }

    /// <summary>
    /// Выход пользователя из системы
    /// </summary>
    /// <param name="request">Запрос на выход</param>
    public void Logout(LogoutRequest request)
    {
        // Отзываем refresh токен если он предоставлен
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            _refreshTokens.Revoke(request.RefreshToken);
        }
        // Access токен не нужно отзывать - он истечет сам
    }

    /// <summary>
    /// Получение информации о текущем пользователе
    /// </summary>
    /// <param name="user">ClaimsPrincipal текущего пользователя</param>
    /// <returns>Информация о пользователе</returns>
    /// <exception cref="UnauthorizedAccessException">Если пользователь не аутентифицирован</exception>
    public MeResponse Me(ClaimsPrincipal user)
    {
        var idClaim = user.Claims.FirstOrDefault(x => x.Type == "id")?.Value;
        if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Пользователь не аутентифицирован");
        }

        return new MeResponse { UserId = userId };
    }

    /// <summary>
    /// Создание пары access и refresh токенов для пользователя
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <returns>Ответ с токенами</returns>
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
    /// Валидация учетных данных пользователя
    /// </summary>
    /// <param name="email">Email пользователя</param>
    /// <param name="password">Пароль пользователя</param>
    /// <exception cref="InvalidOperationException">Если данные не соответствуют требованиям</exception>
    private static void ValidateCredentials(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Email обязателен");
        }
        
        if (!email.Contains('@') || email.Length < 3)
        {
            throw new InvalidOperationException("Неверный формат email");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("Пароль обязателен");
        }
        
        if (password.Length < 6)
        {
            throw new InvalidOperationException("Пароль должен содержать не менее 6 символов");
        }
        
        // Можно добавить дополнительные проверки сложности пароля
        if (password.All(char.IsLetterOrDigit))
        {
            throw new InvalidOperationException("Пароль должен содержать хотя бы один специальный символ");
        }
    }
}
