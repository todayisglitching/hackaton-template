using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using testASP.Configuration;
using testASP.Infrastructure;
using testASP.Models;
using testASP.Services;
using testASP.Services.Interfaces;
using testASP.NoSqlDb;

namespace testASP.Configuration;

/// <summary>
/// Методы расширения для регистрации сервисов
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрация всех необходимых сервисов приложения
    /// </summary>
    /// <param name="services">Коллекция сервисов</param>
    /// <param name="configuration">Конфигурация приложения</param>
    public static void AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Регистрация контроллеров
        services.AddControllers();
        
        // Регистрация базы данных
        services.AddDatabase(configuration);
        
        // Регистрация аутентификации и авторизации
        services.AddAuthenticationAndAuthorization(configuration);
        
        // Регистрация CORS
        services.AddCorsPolicy(configuration);
        
        // Регистрация JSON сериализации
        services.AddJsonSerialization();
        
        // Регистрация бизнес-сервисов
        services.AddBusinessServices(configuration);
        
        // Настройка Kestrel
        services.ConfigureKestrelServer(configuration);
    }

    /// <summary>
    /// Регистрация базы данных
    /// </summary>
    private static void AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var dbSettings = configuration.GetSection("Database").Get<DatabaseSettings>() ?? new DatabaseSettings();
        
        if (dbSettings.Provider.ToLower() == "sqlite")
        {
            services.AddDbContext<NoSqlDbContext>(options =>
                options.UseSqlite(dbSettings.ConnectionString, sqliteOptions =>
                {
                    sqliteOptions.MigrationsAssembly("testASP");
                }));
        }
        else
        {
            throw new NotSupportedException($"Провайдер базы данных '{dbSettings.Provider}' не поддерживается. Используйте 'sqlite'");
        }

        // Регистрация NoSQL сервиса (внутренний, не доступен через API)
        services.AddScoped<NoSqlService>();
        
        // В Native AOT не можем создавать базу данных здесь, так как это требует runtime model building
        // База данных будет создана позже в Program.cs после полной конфигурации сервисов
    }

    /// <summary>
    /// Регистрация JWT аутентификации и авторизации
    /// </summary>
    private static void AddAuthenticationAndAuthorization(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
        var jwtKey = Encoding.ASCII.GetBytes(jwtSettings.Secret);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // Валидация подписи токена
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(jwtKey),
                    
                    // Отключаем валидацию issuer и audience для упрощения
                    // В продакшене рекомендуется включить эти проверки
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    
                    // Исключаем задержку времени для более точной проверки срока действия
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();
    }

    /// <summary>
    /// Регистрация CORS политики для фронтенда
    /// </summary>
    private static void AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var corsSettings = configuration.GetSection("Cors").Get<CorsSettings>() ?? new CorsSettings();

        services.AddCors(options =>
        {
            options.AddPolicy(corsSettings.PolicyName, policy =>
            {
                policy.WithOrigins(corsSettings.AllowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });
    }

    /// <summary>
    /// Настройка JSON сериализации с использованием генерированного контекста
    /// </summary>
    private static void AddJsonSerialization(this IServiceCollection services)
    {
        // Настройка сериализации для HTTP JSON
        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonContext.Default));

        // Настройка сериализации для MVC
        services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
        {
            options.JsonSerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonContext.Default);
        });
    }

    /// <summary>
    /// Регистрация бизнес-сервисов и хранилищ
    /// </summary>
    private static void AddBusinessServices(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
        var securitySettings = configuration.GetSection("Security").Get<SecuritySettings>() ?? new SecuritySettings();

        // Добавляем HttpClient для EnhancedPasswordService
        services.AddHttpClient();

        // Синглтоны - сервисы с жизненным циклом всего приложения
        if (securitySettings.EnableEnhancedJwtSecurity)
        {
            services.AddSingleton<EnhancedJwtTokenService>(provider => 
            {
                var logger = provider.GetRequiredService<ILogger<EnhancedJwtTokenService>>();
                return new EnhancedJwtTokenService(jwtSettings.Secret, logger);
            });
        }
        else
        {
            services.AddSingleton(new JwtTokenService(jwtSettings.Secret));
        }
        
        services.AddSingleton<UserStore>();
        services.AddSingleton<DeviceStore>();
        services.AddSingleton<RefreshTokenStore>();
        services.AddSingleton<WsConnectionManager>();
        services.AddSingleton<EnhancedPasswordService>();

        // Scoped - сервисы с жизненным циклом HTTP запроса
        services.AddScoped<IAuthService>(provider => 
        {
            var users = provider.GetRequiredService<UserStore>();
            var refreshTokens = provider.GetRequiredService<RefreshTokenStore>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            
            var enhancedTokenService = provider.GetRequiredService<EnhancedJwtTokenService>();
            var logger = loggerFactory.CreateLogger<ModernAuthService>();
            return new ModernAuthService(users, enhancedTokenService, refreshTokens, logger);
        });
        services.AddScoped<IDeviceService, DeviceService>();
        
        // Регистрация безопасности
        services.Configure<SecurityOptions>(options =>
        {
            options.MaxRequestsPerWindow = securitySettings.MaxRequestsPerWindow;
            options.RateLimitWindow = TimeSpan.FromMinutes(securitySettings.RateLimitWindowMinutes);
            options.MaxViolationsBeforeBlock = securitySettings.MaxViolationsBeforeBlock;
            options.BlockDuration = TimeSpan.FromMinutes(securitySettings.BlockDurationMinutes);
        });
        
        // Регистрируем SecurityOptions как сервис для middleware
        services.AddSingleton<SecurityOptions>();
    }

    /// <summary>
    /// Настройка Kestrel сервера
    /// </summary>
    private static void ConfigureKestrelServer(this IServiceCollection services, IConfiguration configuration)
    {
        var serverSettings = configuration.GetSection("Server").Get<ServerSettings>() ?? new ServerSettings();

        services.Configure<KestrelServerOptions>(options =>
        {
            // HTTP порт для разработки
            options.ListenAnyIP(serverSettings.HttpPort);
            
            // HTTPS порт для продакшена
            //options.ListenAnyIP(serverSettings.HttpsPort, listenOptions => listenOptions.UseHttps());
        });
    }
}
