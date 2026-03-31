using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Net;
using testASP.Models;
using testASP.NoSqlDb;

namespace testASP.Tests;

/// <summary>
/// Базовые тесты API
/// </summary>
public sealed class BasicTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public BasicTests(WebApplicationFactory<Program> factory)
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

        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Add("User-Agent", "xUnit-Test-Client");
    }

    [Fact]
    public async Task Register_ValidUser_ReturnsAuthResponse()
    {
        // Arrange
        var request = new RegisterRequest($"test_{Guid.NewGuid()}@example.com", "MyStr0ng#Pass!");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();
        authResponse!.UserId.Should().BeGreaterThan(0);
        authResponse.Token.Should().NotBeEmpty();
        authResponse.RefreshToken.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        // Arrange
        var email = $"test_{Guid.NewGuid()}@example.com";
        var request = new RegisterRequest(email, "TestPassword123!");
        
        // Регистрируем первого пользователя
        await _client.PostAsJsonAsync("/api/auth/register", request);

        // Act - пытаемся зарегистрировать того же пользователя
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var email = $"test_{Guid.NewGuid()}@example.com";
        var registerRequest = new RegisterRequest(email, "MyStr0ng#Pass!");
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest(email, "MyStr0ng#Pass!");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();
        authResponse!.UserId.Should().BeGreaterThan(0);
        authResponse.Token.Should().NotBeEmpty();
        authResponse.RefreshToken.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest("nonexistent@example.com", "WrongPassword");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithValidToken_ReturnsUserInfo()
    {
        // Arrange
        var email = $"test_{Guid.NewGuid()}@example.com";
        var registerRequest = new RegisterRequest(email, "MyStr0ng#Pass!");
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.Token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var meResponse = await response.Content.ReadFromJsonAsync<MeResponse>();
        meResponse.Should().NotBeNull();
        meResponse!.UserId.Should().Be(authResponse.UserId);
    }

    [Fact]
    public async Task Devices_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/devices");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Devices_WithValidToken_ReturnsEmptyList()
    {
        // Arrange
        var email = $"test_{Guid.NewGuid()}@example.com";
        var registerRequest = new RegisterRequest(email, "MyStr0ng#Pass!");
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/devices");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.Token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var deviceList = await response.Content.ReadFromJsonAsync<DeviceListResponse>();
        deviceList.Should().NotBeNull();
        deviceList!.Devices.Should().BeEmpty();
        deviceList.SelectedDeviceId.Should().BeNull();
    }

    [Fact]
    public async Task Security_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/security/statistics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Security_WithValidToken_ReturnsStatistics()
    {
        // Arrange
        var email = $"test_{Guid.NewGuid()}@example.com";
        var registerRequest = new RegisterRequest(email, "MyStr0ng#Pass!");
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/security/statistics");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.Token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var statistics = await response.Content.ReadFromJsonAsync<object>();
        statistics.Should().NotBeNull();
    }

    [Fact]
    public async Task AddDevice_WithValidToken_ReturnsDevice()
    {
        // Arrange
        var email = $"test_{Guid.NewGuid()}@example.com";
        var registerRequest = new RegisterRequest(email, "MyStr0ng#Pass!");
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var deviceRequest = new DeviceCreateRequest("Test Device", $"device-{Guid.NewGuid()}");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/devices");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.Token);
        request.Content = JsonContent.Create(deviceRequest);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var device = await response.Content.ReadFromJsonAsync<DeviceDto>();
        device.Should().NotBeNull();
        device!.DeviceId.Should().Be(deviceRequest.DeviceId);
        device.Name.Should().Be(deviceRequest.Name);
    }

    [Fact]
    public async Task FullAuthFlow_CompleteScenario_WorksCorrectly()
    {
        // Arrange
        var email = $"integration_{Guid.NewGuid()}@example.com";
        var password = "MyStr0ng#Pass!";

        // Act & Assert - Регистрация
        var registerRequest = new RegisterRequest(email, password);
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var registerAuth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        registerAuth.Should().NotBeNull();

        // Act & Assert - Вход
        var loginRequest = new LoginRequest(email, password);
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginAuth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        loginAuth.Should().NotBeNull();
        loginAuth!.UserId.Should().Be(registerAuth!.UserId);

        // Act & Assert - Получение информации о пользователе
        var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        meRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginAuth.Token);
        var meResponse = await _client.SendAsync(meRequest);
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var meInfo = await meResponse.Content.ReadFromJsonAsync<MeResponse>();
        meInfo.Should().NotBeNull();
        meInfo!.UserId.Should().Be(loginAuth.UserId);

        // Act & Assert - Выход
        var logoutRequest = new LogoutRequest(loginAuth.RefreshToken);
        var logoutResponse = await _client.PostAsJsonAsync("/api/auth/logout", logoutRequest);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
