using System.Security.Claims;
using testASP.Models;

namespace testASP.Services.Interfaces;

public interface IDeviceService
{
    DeviceListResponse GetDevices(ClaimsPrincipal user);
    DeviceDto AddDevice(ClaimsPrincipal user, DeviceCreateRequest request);
    void SelectDevice(ClaimsPrincipal user, DeviceSelectRequest request);
    void RemoveDevice(ClaimsPrincipal user, string deviceId);
}
