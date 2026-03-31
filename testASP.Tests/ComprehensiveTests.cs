using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Net;
using testASP.Models;
using testASP.NoSqlDb;

namespace testASP.Tests;

/// <summary>
/// Комплексные тесты для всех API эндпоинтов
/// </summary>
public sealed class ComprehensiveTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ComprehensiveTests(WebApplicationFactory<Program> factory)
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

    private HttpClient CreateTestClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("User-Agent", "xUnit-Test-Client");
        return client;
    }

    private async Task<AuthResponse> RegisterUser(HttpClient client, string email = null, string password = null)
    {
        email ??= $"test_{Guid.NewGuid()}@example.com";
        password ??= "MyStr0ng#Pass!"; // Сложный пароль без очевидных последовательностей
        
        var request = new RegisterRequest(email, password);
        var response = await client.PostAsJsonAsync("/api/auth/register", request);
        
        // Проверяем статус и получаем ответ
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Registration failed with status {response.StatusCode}: {errorContent}");
        }
        
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();
        return authResponse!;
    }

    #region Authentication Tests

    [Fact]
    public async Task Register_ValidUser_ShouldWork()
    {
        // Arrange
        var client = CreateTestClient();

        // Act
        var authResponse = await RegisterUser(client);

        // Assert
        authResponse.UserId.Should().BeGreaterThan(0);
        authResponse.Token.Should().NotBeEmpty();
        authResponse.RefreshToken.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Register_DuplicateEmail_ShouldReturnConflict()
    {
        // Arrange
        var client = CreateTestClient();
        var email = $"duplicate_{Guid.NewGuid()}@example.com";
        
        // Регистрируем первого пользователя
        await RegisterUser(client, email);

        // Act - пытаемся зарегистрировать того же пользователя
        var request = new RegisterRequest(email, "Test123456!");
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_ValidCredentials_ShouldWork()
    {
        // Arrange
        var client = CreateTestClient();
        var email = $"login_{Guid.NewGuid()}@example.com";
        var password = "TestPass123!";
        
        // Регистрируем пользователя
        await RegisterUser(client, email, password);

        // Act
        var loginRequest = new LoginRequest(email, password);
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();
        authResponse!.UserId.Should().BeGreaterThan(0);
        authResponse.Token.Should().NotBeEmpty();
        authResponse.RefreshToken.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Login_InvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var client = CreateTestClient();
        var loginRequest = new LoginRequest("nonexistent@example.com", "WrongPassword");

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithValidToken_ShouldReturnUserInfo()
    {
        // Arrange
        var client = CreateTestClient();
        var authResponse = await RegisterUser(client);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse.Token);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var meResponse = await response.Content.ReadFromJsonAsync<MeResponse>();
        meResponse.Should().NotBeNull();
        meResponse!.UserId.Should().Be(authResponse.UserId);
    }

    [Fact]
    public async Task Me_WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var client = CreateTestClient();

        // Act
        var response = await client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ValidToken_ShouldReturnNewTokens()
    {
        // Arrange
        var client = CreateTestClient();
        var authResponse = await RegisterUser(client);

        var refreshRequest = new RefreshRequest(authResponse.RefreshToken);

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var newAuthResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        newAuthResponse.Should().NotBeNull();
        newAuthResponse!.UserId.Should().Be(authResponse.UserId);
        newAuthResponse.Token.Should().NotBe(authResponse.Token);
        newAuthResponse.RefreshToken.Should().NotBe(authResponse.RefreshToken);
    }

    [Fact]
    public async Task Logout_ShouldWork()
    {
        // Arrange
        var client = CreateTestClient();
        var authResponse = await RegisterUser(client);

        var logoutRequest = new LogoutRequest(authResponse.RefreshToken);

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/logout", logoutRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Device Management Tests

    [Fact]
    public async Task GetDevices_WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var client = CreateTestClient();

        // Act
        var response = await client.GetAsync("/api/devices");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDevices_WithValidToken_ShouldReturnEmptyList()
    {
        // Arrange
        var client = CreateTestClient();
        var authResponse = await RegisterUser(client);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/devices");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse.Token);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var deviceList = await response.Content.ReadFromJsonAsync<DeviceListResponse>();
        deviceList.Should().NotBeNull();
        deviceList!.Devices.Should().BeEmpty();
        deviceList.SelectedDeviceId.Should().BeNull();
    }

    [Fact]
    public async Task AddDevice_WithValidToken_ShouldWork()
    {
        // Arrange
        var client = CreateTestClient();
        var authResponse = await RegisterUser(client);

        var deviceRequest = new DeviceCreateRequest("Test Device", $"device-{Guid.NewGuid()}");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/devices");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse.Token);
        request.Content = JsonContent.Create(deviceRequest);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var device = await response.Content.ReadFromJsonAsync<DeviceDto>();
        device.Should().NotBeNull();
        device!.DeviceId.Should().Be(deviceRequest.DeviceId);
        device.Name.Should().Be(deviceRequest.Name);
    }

    [Fact]
    public async Task AddDevice_WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var client = CreateTestClient();
        var deviceRequest = new DeviceCreateRequest("Test Device", "device-123");

        // Act
        var response = await client.PostAsJsonAsync("/api/devices", deviceRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SelectDevice_WithValidToken_ShouldWork()
    {
        // Arrange
        var client = CreateTestClient();
        var authResponse = await RegisterUser(client);

        // Добавляем устройство
        var deviceRequest = new DeviceCreateRequest("Test Device", $"device-{Guid.NewGuid()}");
        var addRequest = new HttpRequestMessage(HttpMethod.Post, "/api/devices");
        addRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse.Token);
        addRequest.Content = JsonContent.Create(deviceRequest);
        await client.SendAsync(addRequest);

        var selectRequest = new DeviceSelectRequest(deviceRequest.DeviceId);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/devices/select");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse.Token);
        request.Content = JsonContent.Create(selectRequest);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RemoveDevice_WithValidToken_ShouldWork()
    {
        // Arrange
        var client = CreateTestClient();
        var authResponse = await RegisterUser(client);

        // Добавляем устройство
        var deviceRequest = new DeviceCreateRequest("Test Device", $"device-{Guid.NewGuid()}");
        var addRequest = new HttpRequestMessage(HttpMethod.Post, "/api/devices");
        addRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse.Token);
        addRequest.Content = JsonContent.Create(deviceRequest);
        await client.SendAsync(addRequest);

        // Act
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/devices/{deviceRequest.DeviceId}");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse.Token);
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Security Tests

    [Fact]
    public async Task GetSecurityStatistics_WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var client = CreateTestClient();

        // Act
        var response = await client.GetAsync("/api/security/statistics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSecurityStatistics_WithValidToken_ShouldWork()
    {
        // Arrange
        var client = CreateTestClient();
        var authResponse = await RegisterUser(client);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/security/statistics");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse.Token);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var statistics = await response.Content.ReadFromJsonAsync<object>();
        statistics.Should().NotBeNull();
    }

    [Fact]
    public async Task GeneratePassword_WithValidToken_ShouldWork()
    {
        // Arrange
        var client = CreateTestClient();
        var authResponse = await RegisterUser(client);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/security/generate-password");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse.Token);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<object>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ValidatePassword_WithValidToken_ShouldWork()
    {
        // Arrange
        var client = CreateTestClient();
        var authResponse = await RegisterUser(client);

        var passwordRequest = new { Password = "TestPassword123!" };

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/security/validate-password");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse.Token);
        request.Content = JsonContent.Create(passwordRequest);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<object>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTokenInfo_WithValidToken_ShouldWork()
    {
        // Arrange
        var client = CreateTestClient();
        var authResponse = await RegisterUser(client);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/security/token-info");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse.Token);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenInfo = await response.Content.ReadFromJsonAsync<object>();
        tokenInfo.Should().NotBeNull();
    }

    [Fact]
    public async Task RevokeAllTokens_WithValidToken_ShouldWork()
    {
        // Arrange
        var client = CreateTestClient();
        var authResponse = await RegisterUser(client);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/security/revoke-all");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse.Token);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<object>();
        result.Should().NotBeNull();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task FullUserWorkflow_RegisterLoginDeviceLogout_ShouldWork()
    {
        // Arrange
        var client = CreateTestClient();
        var email = $"integration_{Guid.NewGuid()}@example.com";
        var password = "TestPass123!";

        // Act & Assert - Регистрация
        var registerRequest = new RegisterRequest(email, password);
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var registerAuth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        registerAuth.Should().NotBeNull();

        // Act & Assert - Вход
        var loginRequest = new LoginRequest(email, password);
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginAuth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        loginAuth.Should().NotBeNull();
        loginAuth!.UserId.Should().Be(registerAuth!.UserId);

        // Act & Assert - Получение информации о пользователе
        var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        meRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginAuth.Token);
        var meResponse = await client.SendAsync(meRequest);
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var meInfo = await meResponse.Content.ReadFromJsonAsync<MeResponse>();
        meInfo.Should().NotBeNull();
        meInfo!.UserId.Should().Be(loginAuth.UserId);

        // Act & Assert - Добавление устройства
        var deviceRequest = new DeviceCreateRequest("Integration Device", $"integration-device-{Guid.NewGuid()}");
        var deviceRequestMsg = new HttpRequestMessage(HttpMethod.Post, "/api/devices");
        deviceRequestMsg.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginAuth.Token);
        deviceRequestMsg.Content = JsonContent.Create(deviceRequest);
        var deviceResponse = await client.SendAsync(deviceRequestMsg);
        deviceResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act & Assert - Выход
        var logoutRequest = new LogoutRequest(loginAuth.RefreshToken);
        var logoutResponse = await client.PostAsJsonAsync("/api/auth/logout", logoutRequest);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MultipleUsers_Isolation_ShouldWork()
    {
        // Arrange - Создаем двух пользователей
        var client1 = CreateTestClient();
        var client2 = CreateTestClient();
        
        var user1Auth = await RegisterUser(client1);
        var user2Auth = await RegisterUser(client2);

        // Act & Assert - Пользователь 1 добавляет устройство
        var device1Request = new DeviceCreateRequest("User1 Device", $"user1-device-{Guid.NewGuid()}");
        var device1RequestMsg = new HttpRequestMessage(HttpMethod.Post, "/api/devices");
        device1RequestMsg.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user1Auth.Token);
        device1RequestMsg.Content = JsonContent.Create(device1Request);
        var device1Response = await client1.SendAsync(device1RequestMsg);
        device1Response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act & Assert - Пользователь 2 добавляет устройство
        var device2Request = new DeviceCreateRequest("User2 Device", $"user2-device-{Guid.NewGuid()}");
        var device2RequestMsg = new HttpRequestMessage(HttpMethod.Post, "/api/devices");
        device2RequestMsg.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user2Auth.Token);
        device2RequestMsg.Content = JsonContent.Create(device2Request);
        var device2Response = await client2.SendAsync(device2RequestMsg);
        device2Response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act & Assert - Пользователь 1 видит только свое устройство
        var user1DevicesRequest = new HttpRequestMessage(HttpMethod.Get, "/api/devices");
        user1DevicesRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user1Auth.Token);
        var user1DevicesResponse = await client1.SendAsync(user1DevicesRequest);
        user1DevicesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var user1Devices = await user1DevicesResponse.Content.ReadFromJsonAsync<DeviceListResponse>();
        user1Devices.Should().NotBeNull();
        user1Devices!.Devices.Should().HaveCount(1);

        // Act & Assert - Пользователь 2 видит только свое устройство
        var user2DevicesRequest = new HttpRequestMessage(HttpMethod.Get, "/api/devices");
        user2DevicesRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user2Auth.Token);
        var user2DevicesResponse = await client2.SendAsync(user2DevicesRequest);
        user2DevicesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var user2Devices = await user2DevicesResponse.Content.ReadFromJsonAsync<DeviceListResponse>();
        user2Devices.Should().NotBeNull();
        user2Devices!.Devices.Should().HaveCount(1);
    }

    #endregion
}
