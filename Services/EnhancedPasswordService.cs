using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using BCrypt.Net;

namespace testASP.Services;

/// <summary>
/// Усиленный сервис для работы с паролями с проверкой на утечки и сложность
/// </summary>
public sealed class EnhancedPasswordService
{
    private readonly ILogger<EnhancedPasswordService> _logger;
    private readonly HttpClient _httpClient;
    private readonly HashSet<string> _commonPasswords;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromHours(1);
    private readonly Dictionary<string, CachedPasswordCheck> _passwordCache = new();

    public EnhancedPasswordService(ILogger<EnhancedPasswordService> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
        _commonPasswords = LoadCommonPasswords();
    }

    /// <summary>
    /// Хеширование пароля с использованием BCrypt
    /// </summary>
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Пароль не может быть пустым");
        }

        try
        {
            // Используем BCrypt с work factor 12 (высокая безопасность)
            var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

            _logger.LogDebug("Пароль успешно захеширован");
            return hash;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при хешировании пароля");
            throw new InvalidOperationException("Ошибка при обработке пароля", ex);
        }
    }

    /// <summary>
    /// Проверка пароля
    /// </summary>
    public bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hashedPassword))
        {
            return false;
        }

        try
        {
            var result = BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            _logger.LogDebug("Проверка пароля выполнена: {Result}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при проверке пароля");
            return false;
        }
    }

    /// <summary>
    /// Комплексная валидация пароля
    /// </summary>
    public PasswordValidationResult ValidatePassword(string password, string? email = null)
    {
        var result = new PasswordValidationResult { IsValid = true };

        // Базовые проверки
        if (string.IsNullOrWhiteSpace(password))
        {
            result.IsValid = false;
            result.Errors.Add("Пароль обязателен");
            return result;
        }

        // Длина
        if (password.Length < 8)
        {
            result.IsValid = false;
            result.Errors.Add("Пароль должен содержать минимум 8 символов");
        }

        if (password.Length > 128)
        {
            result.IsValid = false;
            result.Errors.Add("Пароль не должен превышать 128 символов");
        }

        // Сложность
        var complexityScore = CalculatePasswordComplexity(password);
        result.ComplexityScore = complexityScore;

        if (complexityScore < 3)
        {
            result.IsValid = false;
            result.Errors.Add("Пароль слишком простой. Добавьте заглавные буквы, цифры и специальные символы");
        }

        // Проверка на общие пароли
        if (IsCommonPassword(password))
        {
            result.IsValid = false;
            result.Errors.Add("Этот пароль слишком распространен, выберите другой");
        }

        // Проверка на личную информацию
        if (!string.IsNullOrEmpty(email) && ContainsPersonalInfo(password, email))
        {
            result.IsValid = false;
            result.Errors.Add("Пароль не должен содержать вашу личную информацию (email, имя)");
        }

        // Проверка на последовательности и повторения
        if (HasObviousPatterns(password))
        {
            result.IsValid = false;
            result.Errors.Add("Пароль содержит очевидные последовательности или повторения");
        }

        // Асинхронная проверка на утечки (не блокирующая)
        _ = Task.Run(async () => await CheckForBreachesAsync(password));

        return result;
    }

    /// <summary>
    /// Генерация безопасного пароля
    /// </summary>
    public string GenerateSecurePassword(int length = 16)
    {
        const string lowerChars = "abcdefghijklmnopqrstuvwxyz";
        const string upperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string digits = "0123456789";
        const string specialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";

        var allChars = lowerChars + upperChars + digits + specialChars;
        var password = new char[length];

        using var rng = RandomNumberGenerator.Create();

        // Гарантируем наличие каждого типа символов
        password[0] = lowerChars[GetRandomInt(rng, lowerChars.Length)];
        password[1] = upperChars[GetRandomInt(rng, upperChars.Length)];
        password[2] = digits[GetRandomInt(rng, digits.Length)];
        password[3] = specialChars[GetRandomInt(rng, specialChars.Length)];

        // Заполняем остальные символы
        for (int i = 4; i < length; i++)
        {
            password[i] = allChars[GetRandomInt(rng, allChars.Length)];
        }

        // Перемешиваем символы
        for (int i = password.Length - 1; i > 0; i--)
        {
            var j = GetRandomInt(rng, i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        _logger.LogInformation("Сгенерирован безопасный пароль длиной {Length}", length);
        return new string(password);
    }

    /// <summary>
    /// Расчет сложности пароля
    /// </summary>
    private static int CalculatePasswordComplexity(string password)
    {
        int score = 0;

        // Длина
        if (password.Length >= 8) score++;
        if (password.Length >= 12) score++;
        if (password.Length >= 16) score++;

        // Типы символов
        if (password.Any(char.IsLower)) score++;
        if (password.Any(char.IsUpper)) score++;
        if (password.Any(char.IsDigit)) score++;
        if (password.Any(c => !char.IsLetterOrDigit(c))) score++;

        // Дополнительные проверки
        if (password.Any(c => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(c))) score++;

        return score;
    }

    /// <summary>
    /// Загрузка списка общих паролей
    /// </summary>
    private static HashSet<string> LoadCommonPasswords()
    {
        // Базовый список общих паролей
        var commonPasswords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "password", "123456", "password123", "admin", "qwerty", "letmein",
            "welcome", "monkey", "1234567890", "password1", "abc123", "Password123",
            "123456789", "welcome123", "admin123", "root", "toor", "pass",
            "test", "guest", "user", "login", "master", "super", "hello",
            "freedom", "whatever", "qazwsx", "trustno1", "123qwe", "1q2w3e4r",
            "zxcvbnm", "123abc", "password12", "1234", "111111", "000000"
        };

        return commonPasswords;
    }

    /// <summary>
    /// Проверка на общий пароль
    /// </summary>
    private bool IsCommonPassword(string password)
    {
        return _commonPasswords.Contains(password);
    }

    /// <summary>
    /// Проверка на наличие личной информации в пароле
    /// </summary>
    private static bool ContainsPersonalInfo(string password, string email)
    {
        var emailParts = email.Split('@')[0].Split('.', '-', '_');
        
        foreach (var part in emailParts)
        {
            if (part.Length >= 3 && password.Contains(part, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Проверка на очевидные последовательности
    /// </summary>
    private static bool HasObviousPatterns(string password)
    {
        // Проверка на последовательности (123, abc, etc.)
        for (int i = 0; i < password.Length - 2; i++)
        {
            var chars = password.Substring(i, 3);
            if (IsSequence(chars))
            {
                return true;
            }
        }

        // Проверка на повторения (aaa, 111, etc.)
        for (int i = 0; i < password.Length - 2; i++)
        {
            if (password[i] == password[i + 1] && password[i] == password[i + 2])
            {
                return true;
            }
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
    /// Асинхронная проверка на утечки паролей через HaveIBeenPwned API
    /// </summary>
    private async Task CheckForBreachesAsync(string password)
    {
        var cacheKey = GetPasswordHash(password);
        
        // Проверяем кеш
        if (_passwordCache.TryGetValue(cacheKey, out var cached) && 
            DateTime.UtcNow - cached.CheckedAt < _cacheDuration)
        {
            if (cached.IsBreached)
            {
                _logger.LogWarning("Пароль найден в утечках (из кеша)");
            }
            return;
        }

        try
        {
            // Используем HaveIBeenPwned API для проверки
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            var hashPrefix = Convert.ToHexString(hash[..5]);
            var hashSuffix = Convert.ToHexString(hash[5..]);

            var response = await _httpClient.GetAsync($"https://api.pwnedpasswords.com/range/{hashPrefix}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var isBreached = content.Contains(hashSuffix, StringComparison.OrdinalIgnoreCase);

                // Сохраняем в кеш
                _passwordCache[cacheKey] = new CachedPasswordCheck
                {
                    IsBreached = isBreached,
                    CheckedAt = DateTime.UtcNow
                };

                if (isBreached)
                {
                    _logger.LogWarning("Пароль найден в утечках данных");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при проверке утечек пароля");
        }
    }

    /// <summary>
    /// Получение хеша пароля для кеширования
    /// </summary>
    private static string GetPasswordHash(string password)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Получение случайного числа
    /// </summary>
    private static int GetRandomInt(RandomNumberGenerator rng, int max)
    {
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        return Math.Abs(BitConverter.ToInt32(bytes, 0)) % max;
    }
}

/// <summary>
/// Результат валидации пароля
/// </summary>
public sealed class PasswordValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public int ComplexityScore { get; set; }
    public string Strength => ComplexityScore switch
    {
        >= 6 => "Очень сильный",
        >= 5 => "Сильный",
        >= 4 => "Средний",
        >= 3 => "Слабый",
        _ => "Очень слабый"
    };
}

/// <summary>
/// Кешированный результат проверки пароля
/// </summary>
internal sealed class CachedPasswordCheck
{
    public bool IsBreached { get; set; }
    public DateTime CheckedAt { get; set; }
}
