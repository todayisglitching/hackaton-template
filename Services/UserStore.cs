using System.Collections.Concurrent;
using testASP.Models;
using Microsoft.Extensions.Logging;

namespace testASP.Services;

public sealed class UserStore
{
    private readonly ConcurrentDictionary<string, User> _usersByEmail = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<int, User> _usersById = new();
    private int _nextUserId = 7;
    private readonly ILogger<UserStore>? _logger;

    public UserStore(ILogger<UserStore>? logger = null)
    {
        _logger = logger;
    }

    public bool EmailExists(string email) => _usersByEmail.ContainsKey(email);

    public User Create(string email, string password)
    {
        // Используем тот же BCrypt что и в EnhancedPasswordService
        var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        var user = new User
        {
            Id = Interlocked.Increment(ref _nextUserId),
            Email = email,
            PasswordHash = hash
        };

        if (!_usersByEmail.TryAdd(email, user))
        {
            throw new InvalidOperationException("User already exists");
        }

        _usersById.TryAdd(user.Id, user);
        return user;
    }

    public User? FindByEmail(string email)
    {
        return _usersByEmail.TryGetValue(email, out var user) ? user : null;
    }

    public User? GetById(int id)
    {
        return _usersById.TryGetValue(id, out var user) ? user : null;
    }

    public bool ValidatePassword(User user, string password)
    {
        _logger?.LogInformation("Проверка пароля для пользователя {Email}", user.Email);
        
        try
        {
            // Используем тот же BCrypt что и в EnhancedPasswordService
            var result = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            _logger?.LogInformation("Результат проверки пароля: {Result}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка при проверке пароля для пользователя {Email}", user.Email);
            return false;
        }
    }
}
