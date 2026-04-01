using System.Security.Claims;
using testASP.Models;
using testASP.Services.Interfaces;

namespace testASP.Services;

public sealed class DeviceService : IDeviceService
{
    private readonly DeviceStore _store;

    public DeviceService(DeviceStore store)
    {
        _store = store;
    }

    public DeviceListResponse GetDevices(ClaimsPrincipal user)
    {
        var userId = GetUserId(user);
        return _store.GetDevices(userId);
    }

    public DeviceDto AddDevice(ClaimsPrincipal user, DeviceCreateRequest request)
    {
        var userId = GetUserId(user);

        if (string.IsNullOrWhiteSpace(request.DeviceId) || string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("Имя и идентификатор устройства обязательны");
        }

        return _store.AddDevice(userId, request);
    }

    public void SelectDevice(ClaimsPrincipal user, DeviceSelectRequest request)
    {
        var userId = GetUserId(user);
        var ok = _store.SelectDevice(userId, request.DeviceId);
        if (!ok)
        {
            throw new InvalidOperationException("Устройство не найдено");
        }
    }

    public void RemoveDevice(ClaimsPrincipal user, string deviceId)
    {
        var userId = GetUserId(user);
        var ok = _store.RemoveDevice(userId, deviceId);
        if (!ok)
        {
            throw new InvalidOperationException("Устройство не найдено");
        }
    }

    private static int GetUserId(ClaimsPrincipal user)
    {
        var idClaim = user.Claims.FirstOrDefault(x => x.Type == "id")?.Value;
        if (!int.TryParse(idClaim, out var id))
        {
            throw new UnauthorizedAccessException();
        }
        return id;
    }
}
