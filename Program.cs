using Microsoft.AspNetCore.Authentication.JwtBearer;
using testASP.Configuration;
using testASP.Infrastructure;
using testASP.Models;
using testASP.Services;
using testASP.NoSqlDb;

// Включаем улучшенные метаданные для Swagger/OpenAPI
AppContext.SetSwitch("Microsoft.AspNetCore.Mvc.ApiExplorer.IsEnhancedModelMetadataSupported", true);

var builder = WebApplication.CreateBuilder(args);

// Регистрация конфигурации приложения
builder.Services.Configure<AppSettings>(builder.Configuration);

// Регистрация SPA конфигурации
builder.Services.AddSingleton<ISpaConfiguration>(provider => 
    new SpaConfiguration(
        builder.Environment.ContentRootPath,
        AppContext.BaseDirectory));

// Регистрация всех сервисов приложения
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddScoped<DatabaseHealthService>();

var app = builder.Build();

// Проверка состояния базы данных и инициализация при необходимости
using (var scope = app.Services.CreateScope())
{
    var healthService = scope.ServiceProvider.GetRequiredService<DatabaseHealthService>();
    var healthStatus = await healthService.CheckHealthAsync();
    
    app.Logger.LogInformation("Статус базы данных: {Status} - {Message}", 
        healthStatus.Status, healthStatus.Message);

    if (!healthStatus.IsHealthy)
    {
        // База данных не готова - выполняем инициализацию
        var dbContext = scope.ServiceProvider.GetRequiredService<NoSqlDbContext>();
        
        if (!healthStatus.TablesExist)
        {
            app.Logger.LogInformation("Создание таблиц базы данных...");
            dbContext.Database.EnsureCreated();
        }

        if (!healthStatus.CollectionsExist)
        {
            app.Logger.LogInformation("Инициализация системных коллекций...");
            var noSqlService = scope.ServiceProvider.GetRequiredService<NoSqlService>();
            await noSqlService.InitializeSystemCollectionsAsync();
        }

        // Повторная проверка после инициализации
        var finalStatus = await healthService.CheckHealthAsync();
        app.Logger.LogInformation("Итоговый статус базы данных: {Status} - {Message}", 
            finalStatus.Status, finalStatus.Message);
    }
    else
    {
        app.Logger.LogInformation("База данных полностью готова к работе");
    }

    // Вывод статистики базы данных
    var stats = await healthService.GetStatsAsync();
    app.Logger.LogInformation("Статистика БД: {Collections} коллекций, {Fields} полей, {Documents} документов, {Users} пользователей, размер: {Size}", 
        stats.CollectionCount, stats.FieldCount, stats.DocumentCount, stats.UserCount, stats.FormattedDatabaseSize);
}

// Конфигурация middleware pipeline и эндпоинтов
app.ConfigureApplication(builder.Configuration);

app.Run();
