namespace testASP.Models;

public sealed record RegisterRequest(string Email, string Password);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record LogoutRequest(string RefreshToken);
public sealed record RevokeSessionRequest(string SessionId);

public sealed class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int UserId { get; set; }
}

public sealed class MeResponse
{
    public int UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public List<ActiveSession> ActiveSessions { get; init; } = new();
}

public sealed record ActiveSession
{
    public string SessionId { get; init; } = string.Empty;
    public string DeviceInfo { get; init; } = string.Empty;
    public string IpAddress { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime LastUsed { get; init; }
    public DateTime ExpiresAt { get; init; }
    public bool IsCurrent { get; init; }
}

public sealed record ErrorResponse(string Message);
