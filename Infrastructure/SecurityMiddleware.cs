using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace testASP.Infrastructure;

/// <summary>
/// Middleware для защиты API от злоупотреблений
/// Включает rate limiting, IP блокировку и проверку заголовков безопасности
/// </summary>
public sealed class SecurityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityMiddleware> _logger;
    private readonly SecurityOptions _options;
    
    // Rate limiting хранилище в памяти
    private static readonly ConcurrentDictionary<string, RateLimitInfo> _rateLimitStore = new();
    
    // Блокированные IP адреса
    private static readonly ConcurrentDictionary<string, DateTime> _blockedIps = new();
    
    // Таймер для очистки старых записей
    private readonly Timer _cleanupTimer;

    public SecurityMiddleware(RequestDelegate next, ILogger<SecurityMiddleware> logger, SecurityOptions options)
    {
        _next = next;
        _logger = logger;
        _options = options;
        
        // Запускаем таймер очистки каждые 5 минут
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = GetClientIpAddress(context);
        var path = context.Request.Path.Value?.ToLower() ?? "";
        
        // Проверяем заблокирован ли IP
        if (IsIpBlocked(clientIp))
        {
            _logger.LogWarning("Заблокированный IP попытался получить доступ: {IP}", clientIp);
            await WriteErrorResponse(context, StatusCodes.Status429TooManyRequests, "IP адрес заблокирован");
            return;
        }

        // Проверяем rate limiting для аутентификационных эндпоинтов
        if (IsAuthEndpoint(path))
        {
            if (!CheckRateLimit(clientIp, path))
            {
                _logger.LogWarning("Rate limit превышен для IP: {IP}, Path: {Path}", clientIp, path);
                
                // Блокируем IP при многократных превышениях
                await HandleRateLimitExceeded(clientIp, context);
                return;
            }
        }

        // Проверка заголовков безопасности
        if (!ValidateSecurityHeaders(context))
        {
            _logger.LogWarning("Некорректные заголовки безопасности от IP: {IP}", clientIp);
            await WriteErrorResponse(context, StatusCodes.Status400BadRequest, "Некорректные заголовки запроса");
            return;
        }

        // Проверка User-Agent
        if (!ValidateUserAgent(context))
        {
            _logger.LogWarning("Подозрительный User-Agent от IP: {IP}, UserAgent: {UA}", 
                clientIp, context.Request.Headers["User-Agent"].ToString());
            await WriteErrorResponse(context, StatusCodes.Status400BadRequest, "Доступ запрещен");
            return;
        }

        await _next(context);
    }

    /// <summary>
    /// Получение IP адреса клиента с учетом прокси
    /// </summary>
    private static string GetClientIpAddress(HttpContext context)
    {
        var ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(ip))
        {
            // X-Forwarded-For может содержать несколько IP, берем первый
            return ip.Split(',')[0].Trim();
        }

        ip = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(ip))
        {
            return ip;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>
    /// Проверка является ли эндпоинт аутентификационным
    /// </summary>
    private static bool IsAuthEndpoint(string path)
    {
        return path.Contains("/auth/login") || 
               path.Contains("/auth/register") || 
               path.Contains("/auth/refresh");
    }

    /// <summary>
    /// Проверка rate limiting
    /// </summary>
    private bool CheckRateLimit(string clientIp, string path)
    {
        var key = $"{clientIp}:{path}";
        var now = DateTime.UtcNow;
        
        var rateLimitInfo = _rateLimitStore.GetOrAdd(key, _ => new RateLimitInfo
        {
            Count = 0,
            FirstRequest = now,
            LastRequest = now
        });

        // Сбрасываем счетчик если прошло больше окна времени
        if (now - rateLimitInfo.FirstRequest > _options.RateLimitWindow)
        {
            rateLimitInfo.Count = 0;
            rateLimitInfo.FirstRequest = now;
        }

        rateLimitInfo.Count++;
        rateLimitInfo.LastRequest = now;

        return rateLimitInfo.Count <= _options.MaxRequestsPerWindow;
    }

    /// <summary>
    /// Обработка превышения rate limit
    /// </summary>
    private async Task HandleRateLimitExceeded(string clientIp, HttpContext context)
    {
        var key = $"{clientIp}:violations";
        var violations = _rateLimitStore.GetOrAdd(key, _ => new RateLimitInfo { Count = 0 });
        violations.Count++;

        // Блокируем IP при многократных нарушениях
        if (violations.Count >= _options.MaxViolationsBeforeBlock)
        {
            BlockIpAddress(clientIp, _options.BlockDuration);
            await WriteErrorResponse(context, StatusCodes.Status429TooManyRequests, 
                "Слишком много попыток. IP адрес временно заблокирован");
        }
        else
        {
            await WriteErrorResponse(context, StatusCodes.Status429TooManyRequests, 
                "Слишком много запросов. Попробуйте позже");
        }
    }

    /// <summary>
    /// Блокировка IP адреса
    /// </summary>
    private static void BlockIpAddress(string clientIp, TimeSpan duration)
    {
        _blockedIps.TryAdd(clientIp, DateTime.UtcNow.Add(duration));
    }

    /// <summary>
    /// Проверка заблокирован ли IP
    /// </summary>
    private static bool IsIpBlocked(string clientIp)
    {
        if (_blockedIps.TryGetValue(clientIp, out var blockExpiry))
        {
            if (DateTime.UtcNow > blockExpiry)
            {
                // Разблокируем если срок блокировки истек
                _blockedIps.TryRemove(clientIp, out _);
                return false;
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Валидация заголовков безопасности
    /// </summary>
    private static bool ValidateSecurityHeaders(HttpContext context)
    {
        var headers = context.Request.Headers;
        
        // Проверяем Content-Type для POST запросов
        if (context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            var contentType = headers["Content-Type"].FirstOrDefault();
            if (string.IsNullOrEmpty(contentType) || !contentType.Contains("application/json"))
            {
                return false;
            }
        }

        // Проверяем размер Content-Length
        if (headers.ContainsKey("Content-Length"))
        {
            if (int.TryParse(headers["Content-Length"], out var contentLength) && 
                contentLength > 10 * 1024 * 1024) // 10MB лимит
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Валидация User-Agent
    /// </summary>
    private static bool ValidateUserAgent(HttpContext context)
    {
        var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault();
        
        // Запрещаем пустые User-Agent
        if (string.IsNullOrEmpty(userAgent))
        {
            return false;
        }

        // Проверяем на известные боты и сканеры
        var blockedAgents = new[]
        {
            "curl", "wget", "python", "java", "bot", "crawler", "spider", "scraper"
        };

        return !blockedAgents.Any(agent => 
            userAgent.Contains(agent, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Отправка ошибки
    /// </summary>
    private static async Task WriteErrorResponse(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        
        var errorResponse = new
        {
            error = new
            {
                message = message,
                code = "SECURITY_VIOLATION",
                timestamp = DateTime.UtcNow,
                requestId = context.TraceIdentifier
            }
        };

        await context.Response.WriteAsJsonAsync(errorResponse);
    }

    /// <summary>
    /// Очистка устаревших записей
    /// </summary>
    private void CleanupExpiredEntries(object? state)
    {
        var now = DateTime.UtcNow;
        
        // Очищаем старые rate limit записи
        var expiredKeys = _rateLimitStore
            .Where(kvp => now - kvp.Value.LastRequest > _options.RateLimitWindow)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _rateLimitStore.TryRemove(key, out _);
        }

        // Очищаем истекшие блокировки IP
        var expiredBlocks = _blockedIps
            .Where(kvp => now > kvp.Value)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var ip in expiredBlocks)
        {
            _blockedIps.TryRemove(ip, out _);
        }

        _logger.LogDebug("Очищено {RateLimitCount} rate limit записей и {BlockedCount} IP блокировок", 
            expiredKeys.Count, expiredBlocks.Count);
    }
}

/// <summary>
/// Опции для Security middleware
/// </summary>
public sealed class SecurityOptions
{
    /// <summary>
    /// Максимальное количество запросов в окне времени
    /// </summary>
    public int MaxRequestsPerWindow { get; set; } = 100;

    /// <summary>
    /// Окно времени для rate limiting
    /// </summary>
    public TimeSpan RateLimitWindow { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Максимальное количество нарушений перед блокировкой
    /// </summary>
    public int MaxViolationsBeforeBlock { get; set; } = 50;

    /// <summary>
    /// Длительность блокировки IP
    /// </summary>
    public TimeSpan BlockDuration { get; set; } = TimeSpan.FromMinutes(15);
}

/// <summary>
/// Информация о rate limiting
/// </summary>
internal sealed class RateLimitInfo
{
    public int Count { get; set; }
    public DateTime FirstRequest { get; set; }
    public DateTime LastRequest { get; set; }
}
