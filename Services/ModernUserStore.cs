using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using testASP.Models;

namespace testASP.Services;

/// <summary>
/// Современное хранилище пользователей с BCrypt хешированием
/// </summary>
public sealed class ModernUserStore
{
    private readonly ConcurrentDictionary<string, User> _usersByEmail = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<int, User> _usersById = new();
    private int _nextUserId = 1;
    private readonly ILogger<ModernUserStore>? _logger;

    public ModernUserStore(ILogger<ModernUserStore>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Проверка существования email
    /// </summary>
    public bool EmailExists(string email) => _usersByEmail.ContainsKey(email);

    /// <summary>
    /// Создание нового пользователя с BCrypt хешированием
    /// </summary>
    public User Create(string email, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        var user = new User
        {
            Id = Interlocked.Increment(ref _nextUserId),
            Email = email,
            PasswordHash = hash
        };

        if (!_usersByEmail.TryAdd(email, user))
        {
            throw new InvalidOperationException("Пользователь с таким email уже существует");
        }

        _usersById.TryAdd(user.Id, user);
        _logger?.LogInformation("Создан пользователь: {Email}, ID: {Id}", email, user.Id);
        
        return user;
    }

    /// <summary>
    /// Поиск пользователя по email
    /// </summary>
    public User? FindByEmail(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        return _usersByEmail.TryGetValue(email, out var user) ? user : null;
    }

    /// <summary>
    /// Поиск пользователя по ID
    /// </summary>
    public User? GetById(int id)
    {
        return _usersById.TryGetValue(id, out var user) ? user : null;
    }

    /// <summary>
    /// Проверка пароля с BCrypt
    /// </summary>
    public bool ValidatePassword(User user, string password)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        try
        {
            var result = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            _logger?.LogDebug("Проверка пароля для {Email}: {Result}", user.Email, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка проверки пароля для {Email}", user.Email);
            return false;
        }
    }

    /// <summary>
    /// Удаление пользователя
    /// </summary>
    public bool Delete(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        
        var user = FindByEmail(email);
        if (user is null) return false;

        var removedByEmail = _usersByEmail.TryRemove(email, out _);
        var removedById = _usersById.TryRemove(user.Id, out _);
        
        if (removedByEmail && removedById)
        {
            _logger?.LogInformation("Удален пользователь: {Email}, ID: {Id}", email, user.Id);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Получение всех пользователей
    /// </summary>
    public IReadOnlyCollection<User> GetAll() => _usersById.Values.ToList().AsReadOnly();

    /// <summary>
    /// Обновление email пользователя
    /// </summary>
    public bool UpdateEmail(int userId, string newEmail)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newEmail);
        
        var user = GetById(userId);
        if (user is null) return false;

        if (EmailExists(newEmail)) return false;

        var oldEmail = user.Email;
        _usersByEmail.TryRemove(oldEmail, out _);
        
        // Создаем нового пользователя с обновленным email
        var updatedUser = user with { Email = newEmail };
        _usersByEmail.TryAdd(newEmail, updatedUser);
        _usersById.TryUpdate(userId, updatedUser, user);
        
        _logger?.LogInformation("Обновлен email пользователя: {OldEmail} -> {NewEmail}", oldEmail, newEmail);
        return true;
    }

    /// <summary>
    /// Обновление пароля пользователя
    /// </summary>
    public bool UpdatePassword(int userId, string newPassword)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newPassword);
        
        var user = GetById(userId);
        if (user is null) return false;

        // Создаем нового пользователя с обновленным паролем
        var updatedUser = user with { PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12) };
        
        _usersById.TryUpdate(userId, updatedUser, user);
        _usersByEmail.TryUpdate(user.Email, updatedUser, user);
        
        _logger?.LogInformation("Обновлен пароль пользователя: {Email}, ID: {Id}", user.Email, userId);
        return true;
    }
}
