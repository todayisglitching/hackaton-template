using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using testASP.Models;
using testASP.Services.Interfaces;

namespace testASP.Controllers;

[ApiController]
[Authorize]
[Route("api/devices")]
public sealed class DeviceController : ControllerBase
{
    private readonly IDeviceService _devices;

    public DeviceController(IDeviceService devices)
    {
        _devices = devices;
    }

    [HttpGet]
    public ActionResult<DeviceListResponse> GetDevices()
    {
        return Ok(_devices.GetDevices(User));
    }

    [HttpPost]
    public ActionResult<DeviceDto> AddDevice([FromBody] DeviceCreateRequest request)
    {
        return Ok(_devices.AddDevice(User, request));
    }

    [HttpPost("select")]
    public IActionResult SelectDevice([FromBody] DeviceSelectRequest request)
    {
        _devices.SelectDevice(User, request);
        return Ok();
    }

    [HttpDelete("{deviceId}")]
    public IActionResult RemoveDevice(string deviceId)
    {
        _devices.RemoveDevice(User, deviceId);
        return Ok();
    }
}
