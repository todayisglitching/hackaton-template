using System.Collections.Concurrent;
using testASP.Models;

namespace testASP.Services;

public sealed class UserStore
{
    private readonly ConcurrentDictionary<string, User> _usersByEmail = new(StringComparer.OrdinalIgnoreCase);
    private int _nextUserId = 7;

    public bool EmailExists(string email) => _usersByEmail.ContainsKey(email);

    public User Create(string email, string password)
    {
        var (hash, salt) = PasswordHasher.Hash(password);
        var user = new User
        {
            Id = Interlocked.Increment(ref _nextUserId),
            Email = email,
            PasswordHash = hash,
            PasswordSalt = salt
        };

        if (!_usersByEmail.TryAdd(email, user))
        {
            throw new InvalidOperationException("User already exists");
        }

        return user;
    }

    public User? FindByEmail(string email)
    {
        return _usersByEmail.TryGetValue(email, out var user) ? user : null;
    }

    public bool ValidatePassword(User user, string password)
    {
        return PasswordHasher.Verify(password, user.PasswordHash, user.PasswordSalt);
    }
}
