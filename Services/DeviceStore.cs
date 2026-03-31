using System.Collections.Concurrent;
using testASP.Models;

namespace testASP.Services;

public sealed class DeviceStore
{
    private sealed class UserDevices
    {
        public List<DeviceDto> Devices { get; } = new();
        public string? SelectedDeviceId { get; set; }
    }

    private readonly ConcurrentDictionary<int, UserDevices> _store = new();

    public DeviceListResponse GetDevices(int userId)
    {
        var data = _store.GetOrAdd(userId, _ => new UserDevices());
        lock (data)
        {
            return new DeviceListResponse
            {
                Devices = data.Devices.Select(d => new DeviceDto { DeviceId = d.DeviceId, Name = d.Name }).ToList(),
                SelectedDeviceId = data.SelectedDeviceId
            };
        }
    }

    public DeviceDto AddDevice(int userId, string name, string deviceId)
    {
        var data = _store.GetOrAdd(userId, _ => new UserDevices());
        lock (data)
        {
            var existing = data.Devices.FirstOrDefault(d => d.DeviceId == deviceId);
            if (existing != null) return existing;

            var device = new DeviceDto { DeviceId = deviceId, Name = name };
            data.Devices.Add(device);
            data.SelectedDeviceId ??= deviceId;
            return device;
        }
    }

    public bool SelectDevice(int userId, string deviceId)
    {
        var data = _store.GetOrAdd(userId, _ => new UserDevices());
        lock (data)
        {
            if (!data.Devices.Any(d => d.DeviceId == deviceId)) return false;
            data.SelectedDeviceId = deviceId;
            return true;
        }
    }

    public bool RemoveDevice(int userId, string deviceId)
    {
        var data = _store.GetOrAdd(userId, _ => new UserDevices());
        lock (data)
        {
            var removed = data.Devices.RemoveAll(d => d.DeviceId == deviceId) > 0;
            if (!removed) return false;
            if (data.SelectedDeviceId == deviceId)
            {
                data.SelectedDeviceId = data.Devices.FirstOrDefault()?.DeviceId;
            }
            return true;
        }
    }
}
