using System.Security.Claims;
using testASP.Models;
using testASP.Services.Interfaces;

namespace testASP.Services;

public sealed class AuthService : IAuthService
{
    private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    private readonly UserStore _users;
    private readonly JwtTokenService _tokens;
    private readonly RefreshTokenStore _refreshTokens;

    public AuthService(UserStore users, JwtTokenService tokens, RefreshTokenStore refreshTokens)
    {
        _users = users;
        _tokens = tokens;
        _refreshTokens = refreshTokens;
    }

    public AuthResponse Register(RegisterRequest request)
    {
        ValidateCredentials(request.Email, request.Password);

        if (_users.EmailExists(request.Email))
        {
            throw new InvalidOperationException("Пользователь уже существует");
        }

        var user = _users.Create(request.Email, request.Password);
        return CreateTokens(user.Id);
    }

    public AuthResponse Login(LoginRequest request)
    {
        var user = _users.FindByEmail(request.Email);
        if (user == null || !_users.ValidatePassword(user, request.Password))
        {
            throw new UnauthorizedAccessException("Неверный email или пароль");
        }

        return CreateTokens(user.Id);
    }

    public AuthResponse Refresh(RefreshRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new InvalidOperationException("Refresh token обязателен");
        }

        var userId = _refreshTokens.Validate(request.RefreshToken);
        if (userId == null)
        {
            throw new UnauthorizedAccessException("Refresh token истек");
        }

        _refreshTokens.Revoke(request.RefreshToken);
        return CreateTokens(userId.Value);
    }

    public void Logout(LogoutRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            _refreshTokens.Revoke(request.RefreshToken);
        }
    }

    public MeResponse Me(ClaimsPrincipal user)
    {
        var idClaim = user.Claims.FirstOrDefault(x => x.Type == "id")?.Value;
        if (string.IsNullOrWhiteSpace(idClaim))
        {
            throw new UnauthorizedAccessException();
        }

        return new MeResponse { UserId = int.Parse(idClaim) };
    }

    private AuthResponse CreateTokens(int userId)
    {
        var accessToken = _tokens.CreateToken(userId, AccessTokenLifetime);
        var refreshToken = _refreshTokens.Create(userId, RefreshTokenLifetime);
        return new AuthResponse { Token = accessToken, RefreshToken = refreshToken, UserId = userId };
    }

    private static void ValidateCredentials(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            throw new InvalidOperationException("Неверный формат email");
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            throw new InvalidOperationException("Пароль должен быть не короче 6 символов");
        }
    }
}
