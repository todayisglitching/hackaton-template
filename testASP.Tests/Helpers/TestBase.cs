using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Net;
using testASP.Models;
using testASP.NoSqlDb;

namespace testASP.Tests.Helpers;

/// <summary>
/// Базовый класс для тестов с общими методами
/// </summary>
public abstract class TestBase : IClassFixture<WebApplicationFactory<Program>>
{
    protected readonly WebApplicationFactory<Program> Factory;
    protected readonly HttpClient Client;

    protected TestBase(WebApplicationFactory<Program> factory)
    {
        Factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Удаляем существующий DbContext
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<NoSqlDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Добавляем InMemory DbContext для тестов
                services.AddDbContext<NoSqlDbContext>(options =>
                    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));

                // Настраиваем тестовые данные
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<NoSqlDbContext>();

                db.Database.EnsureCreated();
            });
        });

        Client = Factory.CreateClient();
    }

    /// <summary>
    /// Аутентифицирует пользователя и возвращает токен
    /// </summary>
    protected async Task<string> AuthenticateUser(string email = "test@example.com", string password = "TestPassword123!")
    {
        var registerRequest = new RegisterRequest(email, password);
        var registerResponse = await Client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        return authResponse!.Token;
    }

    /// <summary>
    /// Аутентифицирует пользователя и возвращает полный AuthResponse
    /// </summary>
    protected async Task<AuthResponse> AuthenticateUserFull(string email = "test@example.com", string password = "TestPassword123!")
    {
        var registerRequest = new RegisterRequest(email, password);
        var registerResponse = await Client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        return await registerResponse.Content.ReadFromJsonAsync<AuthResponse>()!;
    }

    /// <summary>
    /// Устанавливает токен в заголовки по умолчанию
    /// </summary>
    protected void SetAuthToken(string token)
    {
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Сбрасывает токен авторизации
    /// </summary>
    protected void ClearAuthToken()
    {
        Client.DefaultRequestHeaders.Authorization = null;
    }

    /// <summary>
    /// Создает тестового пользователя и возвращает его данные
    /// </summary>
    protected async Task<(string Email, string Password, AuthResponse Auth)> CreateTestUser()
    {
        var email = $"test_{Guid.NewGuid()}@example.com";
        var password = "TestPassword123!";
        var auth = await AuthenticateUserFull(email, password);
        return (email, password, auth);
    }

    /// <summary>
    /// Проверяет, что ответ содержит ошибку unauthorized
    /// </summary>
    protected void AssertUnauthorized(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Проверяет, что ответ содержит ошибку bad request
    /// </summary>
    protected void AssertBadRequest(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Проверяет, что ответ успешный (OK)
    /// </summary>
    protected void AssertOk(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Проверяет, что ответ содержит ошибку conflict
    /// </summary>
    protected void AssertConflict(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    /// <summary>
    /// Создает HttpClient без авторизации
    /// </summary>
    protected HttpClient CreateAnonymousClient()
    {
        return Factory.CreateClient();
    }
}

/// <summary>
/// Атрибут для маркировки медленных тестов
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SlowTestAttribute : Attribute
{
    public string Reason { get; }

    public SlowTestAttribute(string reason = "")
    {
        Reason = reason;
    }
}

/// <summary>
/// Атрибут для маркировки тестов требующих сети
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class NetworkTestAttribute : Attribute
{
}

/// <summary>
/// Атрибут для маркировки интеграционных тестов
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class IntegrationTestAttribute : Attribute
{
}
