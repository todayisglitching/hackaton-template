using Microsoft.EntityFrameworkCore;
using testASP.NoSqlDb;

namespace testASP.Services;

/// <summary>
/// Сервис для проверки состояния базы данных
/// </summary>
public sealed class DatabaseHealthService
{
    private readonly NoSqlDbContext _context;
    private readonly ILogger<DatabaseHealthService> _logger;

    public DatabaseHealthService(NoSqlDbContext context, ILogger<DatabaseHealthService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Проверка состояния базы данных
    /// </summary>
    public async Task<DatabaseHealthStatus> CheckHealthAsync()
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync();
            if (!canConnect)
            {
                return new DatabaseHealthStatus
                {
                    IsHealthy = false,
                    Status = "Unreachable",
                    Message = "База данных недоступна"
                };
            }

            var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                return new DatabaseHealthStatus
                {
                    IsHealthy = false,
                    Status = "PendingMigrations",
                    Message = $"Требуются миграции: {string.Join(", ", pendingMigrations)}"
                };
            }

            // Проверка системных таблиц
            var tablesExist = await CheckSystemTablesAsync();
            
            // Проверка системных коллекций
            var collectionsExist = await CheckSystemCollectionsAsync();

            var isHealthy = tablesExist && collectionsExist;

            return new DatabaseHealthStatus
            {
                IsHealthy = isHealthy,
                Status = isHealthy ? "Healthy" : "Incomplete",
                Message = isHealthy 
                    ? "База данных полностью готова к работе" 
                    : "База данных требует инициализации",
                TablesExist = tablesExist,
                CollectionsExist = collectionsExist
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при проверке состояния базы данных");
            return new DatabaseHealthStatus
            {
                IsHealthy = false,
                Status = "Error",
                Message = $"Ошибка: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Проверка существования системных таблиц
    /// </summary>
    private async Task<bool> CheckSystemTablesAsync()
    {
        try
        {
            var tables = new[] { "DynamicCollections", "DynamicFields", "DynamicDocuments", "Users" };
            
            foreach (var table in tables)
            {
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();
                
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@p0";
                
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@p0";
                parameter.Value = table;
                command.Parameters.Add(parameter);
                
                var result = await command.ExecuteScalarAsync();
                var exists = Convert.ToInt32(result) > 0;
                
                await connection.CloseAsync();
                
                if (!exists)
                {
                    _logger.LogDebug("Таблица не найдена: {Table}", table);
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при проверке системных таблиц");
            return false;
        }
    }

    /// <summary>
    /// Проверка существования системных коллекций
    /// </summary>
    private async Task<bool> CheckSystemCollectionsAsync()
    {
        try
        {
            var systemCollections = new[] { "device_logs", "security_events" };
            
            foreach (var collectionName in systemCollections)
            {
                var exists = await _context.DynamicCollections
                    .AnyAsync(c => c.Name == collectionName && c.IsEnabled);
                
                if (!exists)
                {
                    _logger.LogDebug("Системная коллекция не найдена: {Collection}", collectionName);
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при проверке системных коллекций");
            return false;
        }
    }

    /// <summary>
    /// Получение статистики базы данных
    /// </summary>
    public async Task<DatabaseStats> GetStatsAsync()
    {
        try
        {
            var stats = new DatabaseStats
            {
                CollectionCount = await _context.DynamicCollections.CountAsync(),
                FieldCount = await _context.DynamicFields.CountAsync(),
                DocumentCount = await _context.DynamicDocuments.CountAsync(),
                UserCount = await _context.Users.CountAsync(),
                DatabaseSize = await GetDatabaseSizeAsync()
            };

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении статистики базы данных");
            return new DatabaseStats();
        }
    }

    /// <summary>
    /// Получение размера базы данных в байтах
    /// </summary>
    private async Task<long> GetDatabaseSizeAsync()
    {
        try
        {
            var connectionString = _context.Database.GetConnectionString();
            var dbPath = connectionString.Split("Data Source=")[1].Split(";")[0];
            
            if (File.Exists(dbPath))
            {
                var fileInfo = new FileInfo(dbPath);
                return fileInfo.Length;
            }

            return 0;
        }
        catch
        {
            return 0;
        }
    }
}

/// <summary>
/// Статус здоровья базы данных
/// </summary>
public sealed class DatabaseHealthStatus
{
    public bool IsHealthy { get; init; }
    public string Status { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public bool TablesExist { get; init; }
    public bool CollectionsExist { get; init; }
}

/// <summary>
/// Статистика базы данных
/// </summary>
public sealed class DatabaseStats
{
    public int CollectionCount { get; init; }
    public int FieldCount { get; init; }
    public int DocumentCount { get; init; }
    public int UserCount { get; init; }
    public long DatabaseSize { get; init; }
    public string FormattedDatabaseSize => DatabaseSize > 0 
        ? FormatBytes(DatabaseSize) 
        : "N/A";

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB" };
        int counter = 0;
        decimal number = bytes;
        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }
        return $"{number:n1} {suffixes[counter]}";
    }
}
