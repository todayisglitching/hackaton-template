using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using testASP.Services;

namespace testASP.Controllers;

/// <summary>
/// Контроллер для проверки здоровья системы
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly DatabaseHealthService _healthService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(DatabaseHealthService healthService, ILogger<HealthController> logger)
    {
        _healthService = healthService;
        _logger = logger;
    }

    /// <summary>
    /// Проверка здоровья базы данных
    /// </summary>
    [HttpGet("database")]
    public async Task<ActionResult<DatabaseHealthStatus>> GetDatabaseHealth()
    {
        try
        {
            var health = await _healthService.CheckHealthAsync();
            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при проверке здоровья базы данных");
            return StatusCode(500, new DatabaseHealthStatus
            {
                IsHealthy = false,
                Status = "Error",
                Message = "Внутренняя ошибка сервера"
            });
        }
    }

    /// <summary>
    /// Статистика базы данных
    /// </summary>
    [HttpGet("database/stats")]
    public async Task<ActionResult<DatabaseStats>> GetDatabaseStats()
    {
        try
        {
            var stats = await _healthService.GetStatsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении статистики базы данных");
            return StatusCode(500, new DatabaseStats());
        }
    }

    /// <summary>
    /// Общая проверка здоровья API
    /// </summary>
    [HttpGet]
    public ActionResult<ApiHealthStatus> GetApiHealth()
    {
        return Ok(new ApiHealthStatus
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
            Database = "Connected"
        });
    }
}

/// <summary>
/// Статус здоровья API
/// </summary>
public sealed class ApiHealthStatus
{
    public string Status { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string Version { get; init; } = string.Empty;
    public string Environment { get; init; } = string.Empty;
    public string Database { get; init; } = string.Empty;
}
