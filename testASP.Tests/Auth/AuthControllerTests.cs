using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Net;
using testASP.Models;
using testASP.NoSqlDb;
using testASP.Configuration;

namespace testASP.Tests.Auth;

/// <summary>
/// Тесты для контроллера аутентификации
/// </summary>
public sealed class AuthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthControllerTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("TestDb"));

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
        var request = new RegisterRequest("test@example.com", "MyStr0ng#Pass!");

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
        var request = new RegisterRequest("test@example.com", "MyStr0ng#Pass!");
        
        // Регистрируем первого пользователя
        await _client.PostAsJsonAsync("/api/auth/register", request);

        // Act - пытаемся зарегистрировать того же пользователя
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_InvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest("invalid-email", "TestPassword123!");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var registerRequest = new RegisterRequest("test@example.com", "MyStr0ng#Pass!");
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest("test@example.com", "MyStr0ng#Pass!");

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
    public async Task Login_InvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var loginRequest = new LoginRequest("invalid-email", "TestPassword123!");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Refresh_ValidToken_ReturnsNewTokens()
    {
        // Arrange
        var registerRequest = new RegisterRequest("test@example.com", "MyStr0ng#Pass!");
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var refreshRequest = new RefreshRequest(authResponse!.RefreshToken);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var newAuthResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        newAuthResponse.Should().NotBeNull();
        newAuthResponse!.UserId.Should().Be(authResponse.UserId);
        newAuthResponse.Token.Should().NotBe(authResponse.Token); // Новый токен должен отличаться
        newAuthResponse.RefreshToken.Should().NotBe(authResponse.RefreshToken); // Новый refresh токен должен отличаться
    }

    [Fact]
    public async Task Refresh_InvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var refreshRequest = new RefreshRequest("invalid-refresh-token");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_ValidToken_ReturnsUserInfo()
    {
        // Arrange
        var registerRequest = new RegisterRequest("test@example.com", "MyStr0ng#Pass!");
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.Token);
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var meResponse = await response.Content.ReadFromJsonAsync<MeResponse>();
        meResponse.Should().NotBeNull();
        meResponse!.UserId.Should().Be(authResponse.UserId);
    }

    [Fact]
    public async Task Me_InvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid-token");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_ValidToken_ReturnsOk()
    {
        // Arrange
        var registerRequest = new RegisterRequest("test@example.com", "MyStr0ng#Pass!");
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var logoutRequest = new LogoutRequest(authResponse!.RefreshToken);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/logout", logoutRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task FullAuthFlow_CompleteScenario_WorksCorrectly()
    {
        // Arrange
        var email = "fulltest@example.com";
        var password = "TestPassword123!";

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

        // Act & Assert - Обновление токена
        var refreshRequest = new RefreshRequest(loginAuth.RefreshToken);
        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshAuth = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>();
        refreshAuth.Should().NotBeNull();
        refreshAuth!.UserId.Should().Be(loginAuth.UserId);

        // Act & Assert - Выход
        var logoutRequest = new LogoutRequest(refreshAuth.RefreshToken);
        var logoutResponse = await _client.PostAsJsonAsync("/api/auth/logout", logoutRequest);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
