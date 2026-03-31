using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using System.Text;
using testASP.Infrastructure;
using testASP.Services;

namespace testASP.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/ws")]
public sealed class WsController : ControllerBase
{
    private readonly WsConnectionManager _connections;
    private readonly IConfiguration _configuration;

    public WsController(WsConnectionManager connections, IConfiguration configuration)
    {
        _connections = connections;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task Get()
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var secret = _configuration["Jwt:Secret"] ?? "Rostelecom_SmartHome_2026_Ultra_Secret";
        var token = HttpContext.Request.Query["token"].ToString();
        var userId = JwtHelpers.TryGetUserIdFromToken(token, secret);
        if (userId == null)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        _connections.Add(userId.Value, webSocket);

        var buffer = new byte[1024 * 4];
        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result;
                try
                {
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }
                catch (WebSocketException)
                {
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"[CLOUD]: {message}");
                    if (message == "ping")
                    {
                        var pong = Encoding.UTF8.GetBytes("pong");
                        await webSocket.SendAsync(new ArraySegment<byte>(pong), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
            }
        }
        finally
        {
            _connections.Remove(userId.Value, webSocket);
        }
    }
}
