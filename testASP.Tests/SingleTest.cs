using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Net;
using testASP.Models;
using testASP.NoSqlDb;

namespace testASP.Tests;

/// <summary>
/// Один базовый тест для проверки работоспособности
/// </summary>
public sealed class SingleTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SingleTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
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
    }

    [Fact]
    public async Task Register_ValidUser_ShouldWork()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("User-Agent", "xUnit-Test-Client");
        
        var request = new RegisterRequest($"test_{Guid.NewGuid()}@example.com", "MyStr0ng#Pass!");

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();
        authResponse!.UserId.Should().BeGreaterThan(0);
        authResponse.Token.Should().NotBeEmpty();
        authResponse.RefreshToken.Should().NotBeEmpty();
    }
}
