namespace testASP.Models;

public sealed record DeviceCreateRequest(string DeviceId, string Name, string Type, string Properties, string Location, string Manufacturer, string Model, string FirmwareVersion);
public sealed record DeviceSelectRequest(string DeviceId);

public sealed class DeviceDto
{
    public int Id { get; init; }
    public string DeviceId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Status { get; init; } = "offline";
    public string Properties { get; init; } = "{}";
    public string Location { get; init; } = string.Empty;
    public string Manufacturer { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string FirmwareVersion { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
    public bool IsEnabled { get; init; } = true;
}

public sealed class DeviceListResponse
{
    public List<DeviceDto> Devices { get; init; } = new();
    public string? SelectedDeviceId { get; init; }
}
