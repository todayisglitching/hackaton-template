using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using testASP.Models;
using testASP.Services.Interfaces;

namespace testASP.Controllers;

/// <summary>
/// Контроллер для аутентификации пользователей
/// Обрабатывает регистрацию, вход, обновление токенов и выход из системы
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService auth, ILogger<AuthController> logger)
    {
        _auth = auth;
        _logger = logger;
    }

    /// <summary>
    /// Регистрация нового пользователя
    /// </summary>
    /// <param name="request">Данные для регистрации</param>
    /// <returns>Ответ с токенами аутентификации</returns>
    /// <response code="200">Пользователь успешно зарегистрирован</response>
    /// <response code="400">Некорректные данные пользователя</response>
    /// <response code="409">Пользователь уже существует</response>
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public ActionResult<AuthResponse> Register([FromBody] RegisterRequest request)
    {
        try
        {
            _logger.LogInformation("Попытка регистрации пользователя с email: {Email}", request.Email);
            var result = _auth.Register(request);
            _logger.LogInformation("Пользователь успешно зарегистрирован с ID: {UserId}", result.UserId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Ошибка регистрации: {Error}", ex.Message);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Вход пользователя в систему
    /// </summary>
    /// <param name="request">Данные для входа</param>
    /// <returns>Ответ с токенами аутентификации</returns>
    /// <response code="200">Успешный вход</response>
    /// <response code="401">Неверные учетные данные</response>
    /// <response code="400">Некорректные данные</response>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<AuthResponse> Login([FromBody] LoginRequest request)
    {
        try
        {
            _logger.LogInformation("Попытка входа пользователя с email: {Email}", request.Email);
            var result = _auth.Login(request);
            _logger.LogInformation("Пользователь успешно вошел в систему с ID: {UserId}", result.UserId);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Ошибка входа: {Error}", ex.Message);
            return Unauthorized(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Ошибка входа: {Error}", ex.Message);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Обновление токенов с помощью refresh токена
    /// </summary>
    /// <param name="request">Запрос на обновление токена</param>
    /// <returns>Новая пара токенов</returns>
    /// <response code="200">Токены успешно обновлены</response>
    /// <response code="401">Refresh токен недействителен</response>
    /// <response code="400">Refresh токен не предоставлен</response>
    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<AuthResponse> Refresh([FromBody] RefreshRequest request)
    {
        try
        {
            _logger.LogInformation("Попытка обновления токена");
            var result = _auth.Refresh(request);
            _logger.LogInformation("Токен успешно обновлен для пользователя с ID: {UserId}", result.UserId);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Ошибка обновления токена: {Error}", ex.Message);
            return Unauthorized(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Ошибка обновления токена: {Error}", ex.Message);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Выход пользователя из системы
    /// </summary>
    /// <param name="request">Запрос на выход</param>
    /// <response code="200">Успешный выход</response>
    [AllowAnonymous]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Logout([FromBody] LogoutRequest request)
    {
        _logger.LogInformation("Попытка выхода из системы");
        _auth.Logout(request);
        _logger.LogInformation("Пользователь успешно вышел из системы");
        return Ok();
    }

    /// <summary>
    /// Получение информации о текущем пользователе
    /// </summary>
    /// <returns>Информация о пользователе</returns>
    /// <response code="200">Информация о пользователе</response>
    /// <response code="401">Пользователь не аутентифицирован</response>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<MeResponse> Me()
    {
        try
        {
            var result = _auth.Me(User);
            _logger.LogInformation("Получена информация о пользователе с ID: {UserId}", result.UserId);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Ошибка получения информации о пользователе: {Error}", ex.Message);
            return Unauthorized(ex.Message);
        }
    }
}
