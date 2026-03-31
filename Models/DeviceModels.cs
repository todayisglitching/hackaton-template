namespace testASP.Models;

public sealed record DeviceCreateRequest(string Name, string DeviceId);
public sealed record DeviceSelectRequest(string DeviceId);

public sealed class DeviceDto
{
    public string DeviceId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public sealed class DeviceListResponse
{
    public List<DeviceDto> Devices { get; init; } = new();
    public string? SelectedDeviceId { get; init; }
}
