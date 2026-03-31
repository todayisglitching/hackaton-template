using Microsoft.Extensions.FileProviders;
using testASP.Configuration;
using testASP.Infrastructure;

namespace testASP.Configuration;

/// <summary>
/// Методы расширения для конфигурации pipeline приложения
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Конфигурация middleware pipeline
    /// </summary>
    /// <param name="app">Построитель приложения</param>
    /// <param name="configuration">Конфигурация приложения</param>
    public static void ConfigureApplication(this WebApplication app, IConfiguration configuration)
    {
        // Конфигурация статических файлов для SPA
        app.ConfigureStaticFiles();
        
        // Подключение middleware в правильном порядке
        app.UseMiddlewarePipeline(configuration);
        
        // Конфигурация эндпоинтов
        app.ConfigureEndpoints();
    }

    /// <summary>
    /// Конфигурация статических файлов для SPA (Vite)
    /// </summary>
    private static void ConfigureStaticFiles(this WebApplication app)
    {
        var spaConfig = app.Services.GetRequiredService<ISpaConfiguration>();
        
        if (spaConfig.IsSpaReady && spaConfig.FileProvider != null)
        {
            // Обслуживание файлов по умолчанию (index.html)
            app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = spaConfig.FileProvider });
            
            // Обслуживание статических файлов
            app.UseStaticFiles(new StaticFileOptions { FileProvider = spaConfig.FileProvider });
        }
    }

    /// <summary>
    /// Конфигурация middleware pipeline в правильном порядке
    /// </summary>
    private static void UseMiddlewarePipeline(this WebApplication app, IConfiguration configuration)
    {
        var corsSettings = configuration.GetSection("Cors").Get<CorsSettings>() ?? new CorsSettings();
        var securitySettings = configuration.GetSection("Security").Get<SecuritySettings>() ?? new SecuritySettings();
        
        // Security middleware должен быть первым для rate limiting
        if (securitySettings.MaxRequestsPerWindow > 0)
        {
            app.UseMiddleware<SecurityMiddleware>();
        }
        
        // CORS должен быть до аутентификации
        app.UseCors(corsSettings.PolicyName);
        
        // Аутентификация должна быть до авторизации
        app.UseAuthentication();
        
        // Авторизация
        app.UseAuthorization();
        
        // WebSocket поддержка
        app.UseWebSockets();
        
        // Обработка исключений (должна быть в конце)
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        
        // Логирование API запросов
        app.UseApiLogging();
    }

    /// <summary>
    /// Middleware для логирования API запросов
    /// </summary>
    private static void UseApiLogging(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            // Логируем только API запросы
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                app.Logger.LogInformation("API {Method} {Path}", 
                    context.Request.Method, 
                    context.Request.Path);
            }
            
            await next();
        });
    }

    /// <summary>
    /// Конфигурация эндпоинтов приложения
    /// </summary>
    private static void ConfigureEndpoints(this WebApplication app)
    {
        // Регистрация контроллеров
        app.MapControllers();
        
        // Обработка несуществующих API эндпоинтов
        app.Map("/api/{**rest}", () => Results.NotFound());
        
        // SPA fallback для клиентской маршрутизации
        app.ConfigureSpaFallback();
    }

    /// <summary>
    /// Конфигурация fallback для SPA приложений
    /// </summary>
    private static void ConfigureSpaFallback(this WebApplication app)
    {
        var spaConfig = app.Services.GetRequiredService<ISpaConfiguration>();
        
        if (spaConfig.IsSpaReady && spaConfig.FileProvider != null)
        {
            // Для готового SPA - отдаем index.html для всех не-API запросов
            app.MapFallbackToFile("index.html", 
                new StaticFileOptions { FileProvider = spaConfig.FileProvider! });
        }
        else
        {
            // Если SPA не собрано - возвращаем информационное сообщение
            app.MapFallback(async context =>
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsync(
                    "SPA build not found. Run Vite build to generate /Vite/dist.");
            });
        }
    }
}
