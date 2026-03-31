using System.Net;
using System.Text.Json;
using testASP.Models;

namespace testASP.Infrastructure;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("Unauthorized request: {Path}", context.Request.Path);
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await WriteJson(context, new ErrorResponse("Нет доступа"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Bad request: {Path}", context.Request.Path);
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await WriteJson(context, new ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Path}", context.Request.Path);
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await WriteJson(context, new ErrorResponse("Внутренняя ошибка сервера"));
        }
    }

    private static Task WriteJson(HttpContext context, ErrorResponse payload)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        return context.Response.WriteAsync(
            JsonSerializer.Serialize(payload, AppJsonContext.Default.ErrorResponse));
    }
}
