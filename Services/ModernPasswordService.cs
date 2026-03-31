using Microsoft.Extensions.Logging;

namespace testASP.Services;

/// <summary>
/// Современный сервис паролей с BCrypt хешированием
/// </summary>
public sealed class ModernPasswordService
{
    private readonly ILogger<ModernPasswordService>? _logger;
    private const int WorkFactor = 12;

    public ModernPasswordService(ILogger<ModernPasswordService>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Хеширование пароля с BCrypt
    /// </summary>
    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        
        try
        {
            var hash = BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
            _logger?.LogDebug("Пароль успешно захеширован");
            return hash;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка хеширования пароля");
            throw new InvalidOperationException("Ошибка обработки пароля", ex);
        }
    }

    /// <summary>
    /// Проверка пароля с BCrypt
    /// </summary>
    public bool VerifyPassword(string password, string hashedPassword)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(hashedPassword);
        
        try
        {
            var result = BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            _logger?.LogDebug("Проверка пароля: {Result}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка проверки пароля");
            return false;
        }
    }

    /// <summary>
    /// Валидация сложности пароля
    /// </summary>
    public ModernPasswordValidationResult ValidatePassword(string password, string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var errors = new List<string>();

        // Базовые требования
        if (password.Length < 8)
            errors.Add("Минимальная длина - 8 символов");

        // Сложность
        if (!password.Any(char.IsUpper))
            errors.Add("Требуется заглавная буква");
        if (!password.Any(char.IsLower))
            errors.Add("Требуется строчная буква");
        if (!password.Any(char.IsDigit))
            errors.Add("Требуется цифра");
        if (!password.Any(c => !char.IsLetterOrDigit(c)))
            errors.Add("Требуется специальный символ");

        // Персональная информация
        if (ContainsPersonalInfo(password, email))
            errors.Add("Пароль содержит личную информацию");

        // Очевидные паттерны
        if (HasObviousPatterns(password))
            errors.Add("Пароль содержит очевидные последовательности");

        return new ModernPasswordValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors,
            Strength = CalculateStrength(password)
        };
    }

    /// <summary>
    /// Генерация сильного пароля
    /// </summary>
    public string GenerateStrongPassword(int length = 16)
    {
        if (length < 8) throw new ArgumentException("Минимальная длина - 8 символов");

        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*()_+-=[]{}|;:,.<>?";

        var allChars = upper + lower + digits + special;
        var random = new Random();
        var password = new char[length];

        // Гарантируем наличие всех типов символов
        password[0] = upper[random.Next(upper.Length)];
        password[1] = lower[random.Next(lower.Length)];
        password[2] = digits[random.Next(digits.Length)];
        password[3] = special[random.Next(special.Length)];

        // Заполняем остальные символы
        for (int i = 4; i < length; i++)
        {
            password[i] = allChars[random.Next(allChars.Length)];
        }

        // Перемешиваем
        return new string(password.OrderBy(_ => random.Next()).ToArray());
    }

    /// <summary>
    /// Проверка на личную информацию
    /// </summary>
    private static bool ContainsPersonalInfo(string password, string email)
    {
        var emailParts = email.Split('@')[0].Split('.', '-', '_');
        
        foreach (var part in emailParts.Where(p => p.Length >= 3))
        {
            if (password.Contains(part, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Проверка на очевидные паттерны
    /// </summary>
    private static bool HasObviousPatterns(string password)
    {
        // Последовательности
        for (int i = 0; i < password.Length - 2; i++)
        {
            var chars = password.Substring(i, 3);
            if (IsSequence(chars))
                return true;
        }

        // Повторения
        for (int i = 0; i < password.Length - 2; i++)
        {
            if (password[i] == password[i + 1] && password[i] == password[i + 2])
                return true;
        }

        return false;
    }

    /// <summary>
    /// Проверка на последовательность
    /// </summary>
    private static bool IsSequence(string chars)
    {
        // Числовые
        if (int.TryParse(chars, out var num))
        {
            return (chars[1] - chars[0] == 1) && (chars[2] - chars[1] == 1) ||
                   (chars[1] - chars[0] == -1) && (chars[2] - chars[1] == -1);
        }

        // Буквенные
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
    private static string CalculateStrength(string password)
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

/// <summary>
/// Результат валидации пароля
/// </summary>
public sealed class ModernPasswordValidationResult
{
    public bool IsValid { get; init; }
    public List<string> Errors { get; init; } = new();
    public string Strength { get; init; } = string.Empty;
}
