using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using testASP.Models;
using testASP.Services.Interfaces;

namespace testASP.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public ActionResult<AuthResponse> Register([FromBody] RegisterRequest request)
    {
        return Ok(_auth.Register(request));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public ActionResult<AuthResponse> Login([FromBody] LoginRequest request)
    {
        return Ok(_auth.Login(request));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public ActionResult<AuthResponse> Refresh([FromBody] RefreshRequest request)
    {
        return Ok(_auth.Refresh(request));
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public IActionResult Logout([FromBody] LogoutRequest request)
    {
        _auth.Logout(request);
        return Ok();
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<MeResponse> Me()
    {
        return Ok(_auth.Me(User));
    }
}
