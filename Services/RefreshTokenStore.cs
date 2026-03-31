using System.Collections.Concurrent;

namespace testASP.Services;

public sealed class RefreshTokenStore
{
    private sealed record RefreshTokenEntry(int UserId, DateTime ExpiresAtUtc);

    private readonly ConcurrentDictionary<string, RefreshTokenEntry> _tokens = new();

    public string Create(int userId, TimeSpan lifetime)
    {
        var token = Guid.NewGuid().ToString("N");
        var entry = new RefreshTokenEntry(userId, DateTime.UtcNow.Add(lifetime));
        _tokens[token] = entry;
        return token;
    }

    public int? Validate(string token)
    {
        if (!_tokens.TryGetValue(token, out var entry)) return null;
        if (DateTime.UtcNow > entry.ExpiresAtUtc)
        {
            _tokens.TryRemove(token, out _);
            return null;
        }
        return entry.UserId;
    }

    public void Revoke(string token)
    {
        _tokens.TryRemove(token, out _);
    }

    /// <summary>
    /// Отзыв всех refresh токенов пользователя
    /// </summary>
    public int RevokeAllUserTokens(int userId)
    {
        var userTokens = _tokens
            .Where(kvp => kvp.Value.UserId == userId)
            .Select(kvp => kvp.Key)
            .ToList();

        var revokedCount = 0;
        foreach (var token in userTokens)
        {
            if (_tokens.TryRemove(token, out _))
            {
                revokedCount++;
            }
        }

        return revokedCount;
    }
}
