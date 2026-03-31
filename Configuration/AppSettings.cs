namespace testASP.Configuration;

/// <summary>
/// Модель конфигурации приложения для JWT аутентификации
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// Настройки JWT токена
    /// </summary>
    public JwtSettings Jwt { get; set; } = new();
    
    /// <summary>
    /// Настройки CORS для фронтенда
    /// </summary>
    public CorsSettings Cors { get; set; } = new();
    
    /// <summary>
    /// Настройки сервера
    /// </summary>
    public ServerSettings Server { get; set; } = new();
    
    /// <summary>
    /// Настройки безопасности
    /// </summary>
    public SecuritySettings Security { get; set; } = new();
    
    /// <summary>
    /// Настройки базы данных
    /// </summary>
    public DatabaseSettings Database { get; set; } = new();
}

/// <summary>
/// Настройки JWT аутентификации
/// </summary>
public sealed class JwtSettings
{
    /// <summary>
    /// Секретный ключ для подписи JWT токенов
    /// В продакшене должен храниться в безопасном хранилище
    /// </summary>
    public string Secret { get; set; } = "Rostelecom_SmartHome_2026_Ultra_Secret";
    
    /// <summary>
    /// Время жизни access токена
    /// </summary>
    public int AccessTokenExpirationMinutes { get; set; } = 5;
    
    /// <summary>
    /// Время жизни refresh токена
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;
}

/// <summary>
/// Настройки CORS
/// </summary>
public sealed class CorsSettings
{
    /// <summary>
    /// Разрешенные origins для фронтенда
    /// </summary>
    public string[] AllowedOrigins { get; set; } = { "http://localhost:5173" };
    
    /// <summary>
    /// Имя политики CORS
    /// </summary>
    public string PolicyName { get; set; } = "VitePolicy";
}

/// <summary>
/// Настройки сервера
/// </summary>
public sealed class ServerSettings
{
    /// <summary>
    /// HTTP порт
    /// </summary>
    public int HttpPort { get; set; } = 5000;
    
    /// <summary>
    /// HTTPS порт
    /// </summary>
    public int HttpsPort { get; set; } = 5001;
}

/// <summary>
/// Настройки безопасности
/// </summary>
public sealed class SecuritySettings
{
    /// <summary>
    /// Максимальное количество запросов в окне времени
    /// </summary>
    public int MaxRequestsPerWindow { get; set; } = 5;

    /// <summary>
    /// Окно времени для rate limiting в минутах
    /// </summary>
    public int RateLimitWindowMinutes { get; set; } = 1;

    /// <summary>
    /// Максимальное количество нарушений перед блокировкой
    /// </summary>
    public int MaxViolationsBeforeBlock { get; set; } = 3;

    /// <summary>
    /// Длительность блокировки IP в минутах
    /// </summary>
    public int BlockDurationMinutes { get; set; } = 15;

    /// <summary>
    /// Включить проверку на утечки паролей
    /// </summary>
    public bool EnablePasswordBreachCheck { get; set; } = true;

    /// <summary>
    /// Включить усиленную JWT безопасность
    /// </summary>
    public bool EnableEnhancedJwtSecurity { get; set; } = true;
}

/// <summary>
/// Настройки базы данных
/// </summary>
public sealed class DatabaseSettings
{
    /// <summary>
    /// Строка подключения к базе данных
    /// </summary>
    public string ConnectionString { get; set; } = "Data Source=nocode.db";

    /// <summary>
    /// Провайдер базы данных (sqlite, postgresql, mysql)
    /// </summary>
    public string Provider { get; set; } = "sqlite";

    /// <summary>
    /// Включить миграции при запуске
    /// </summary>
    public bool AutoMigrate { get; set; } = true;

    /// <summary>
    /// Включить логирование SQL запросов
    /// </summary>
    public bool EnableSensitiveDataLogging { get; set; } = false;
}
